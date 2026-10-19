using SmartRail.Api.Contracts.RailwayNetwork;
using SmartRail.Application.JourneysStatus.Queries;
using SmartRail.Application.JourneysStatus.Projections;

namespace SmartRail.Api.Endpoints.JourneysStatus;

public static class GetJourneyUpdatesEndpoint
{
    public static RouteHandlerBuilder MapGetJourneyUpdates(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/api/journeys/{id}/updates", HandleAsync)
            .WithName("GetJourneyUpdates").WithTags("Journeys")
            .WithSummary("Get published updates for a journey")
            .WithDescription("Returns complete public operational history in ascending revision order. Initialization and private audit events are excluded; a valid journey with no updates returns an empty array.")
            .Produces<IReadOnlyList<JourneyUpdateResponse>>(StatusCodes.Status200OK)
            .Produces<ProblemResponse>(StatusCodes.Status400BadRequest, contentType: "application/problem+json")
            .Produces<ProblemResponse>(StatusCodes.Status404NotFound, contentType: "application/problem+json")
            .Produces<ProblemResponse>(StatusCodes.Status503ServiceUnavailable, contentType: "application/problem+json")
            .AllowAnonymous();

    private static Task<IResult> HandleAsync(string? id, GetJourneyUpdatesHandler handler,
        CancellationToken cancellationToken) => JourneyReadProblemMapping.Safe(async () =>
    {
        if (!Guid.TryParse(id, out var journeyId)) return JourneyReadProblemMapping.InvalidId("id");
        var result = await handler.HandleAsync(new GetJourneyUpdatesQuery(journeyId), cancellationToken);
        return result.Outcome switch
        {
            JourneyReadOutcome.NotFound => JourneyReadProblemMapping.NotFound(),
            JourneyReadOutcome.Unavailable => JourneyReadProblemMapping.Unavailable(),
            _ when result.Value is not null => Results.Ok(result.Value.Select(ToResponse).ToArray()),
            _ => JourneyReadProblemMapping.Unavailable()
        };
    });

    private static JourneyUpdateResponse ToResponse(JourneyUpdateView view) => new(view.UpdateId,
        view.JourneyInstanceId, view.Revision, view.Kind, view.Status, view.Source, view.PublishedAt,
        view.CorrectionOfUpdateId, ToResponse(view.PublicChange));

    private static JourneyPublicChangeResponse ToResponse(JourneyPublicChangeView view) => view switch
    {
        StatusPublicChangeView status => new StatusPublicChangeResponse(status.StatusValue),
        StopPublicChangeView stop => new StopPublicChangeResponse(stop.JourneyStopId, stop.Platform,
            stop.OperationalArrivalAt, stop.OperationalDepartureAt),
        RouteDiversionPublicChangeView diversion => new RouteDiversionPublicChangeResponse(
            diversion.DivertedStops.Select(item => new DivertedStopResponse(item.JourneyStopId,
                item.StationId, item.StopOrder, item.ScheduledArrivalAt, item.ScheduledDepartureAt,
                item.OperationalArrivalAt, item.OperationalDepartureAt, item.Platform)).ToArray()),
        CorrectionPublicChangeView correction => new CorrectionPublicChangeResponse(ToResponse(correction.CorrectedChange)),
        _ => throw new JourneyReadProjectionException("Unsupported public update change projection.")
    };
}
