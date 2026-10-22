using SmartRail.Api.Contracts.RailwayNetwork;
using SmartRail.Application.JourneysStatus.Queries;
using SmartRail.Application.JourneysStatus.Projections;

namespace SmartRail.Api.Endpoints.JourneysStatus;

public static class GetJourneyEndpoint
{
    public static RouteHandlerBuilder MapGetJourney(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/api/journeys/{id}", HandleAsync)
            .WithName("GetJourney").WithTags("Journeys")
            .WithSummary("Get a published dated journey")
            .WithDescription("Returns one internally consistent current journey revision. Missing operational values are unknown; they are not inferred.")
            .Produces<JourneySnapshotResponse>(StatusCodes.Status200OK)
            .Produces<ProblemResponse>(StatusCodes.Status400BadRequest, contentType: "application/problem+json")
            .Produces<ProblemResponse>(StatusCodes.Status404NotFound, contentType: "application/problem+json")
            .Produces<ProblemResponse>(StatusCodes.Status503ServiceUnavailable, contentType: "application/problem+json")
            .AllowAnonymous();

    private static Task<IResult> HandleAsync(string? id, GetJourneyHandler handler, CancellationToken cancellationToken) =>
        JourneyReadProblemMapping.Safe(async () =>
        {
            if (!Guid.TryParse(id, out var journeyId)) return JourneyReadProblemMapping.InvalidId("id");
            var result = await handler.HandleAsync(new GetJourneyQuery(journeyId), cancellationToken);
            return result.Outcome switch
            {
                JourneyReadOutcome.NotFound => JourneyReadProblemMapping.NotFound(),
                JourneyReadOutcome.Unavailable => JourneyReadProblemMapping.Unavailable(),
                _ when result.Value is not null => Results.Ok(ToResponse(result.Value)),
                _ => JourneyReadProblemMapping.Unavailable()
            };
        });

    public static JourneySnapshotResponse ToResponse(JourneySnapshotView view) => new(
        view.JourneyInstanceId, view.ServiceId, view.ScheduleId, view.ScheduleRevision,
        view.OperationDate, view.FirstDepartureOccurrence, view.Revision, view.Status,
        view.Stops.Select(stop => new JourneyStopResponse(stop.JourneyStopId, stop.StopOrder,
            stop.StationId, stop.StationName, stop.PlannedArrivalAt, stop.PlannedDepartureAt,
            stop.OperationalArrivalAt, stop.OperationalDepartureAt, stop.Platform, stop.TimingSource,
            stop.TimingUpdatedAt, stop.Ambiguity)).ToArray(), view.Source, view.PublishedAt,
        view.LastUpdatedAt, view.Eta is null ? null : new JourneyEstimateResponse(view.Eta.Value,
            view.Eta.Source, view.Eta.UpdatedAt, view.Eta.Freshness));
}
