using System.Security.Claims;
using System.Text.Json.Serialization;
using Microsoft.OpenApi;
using SmartRail.Api.Errors;
using SmartRail.Application.JourneysStatus.Queries;
using SmartRail.Application.JourneysStatus.Publication;
using SmartRail.Domain.JourneysStatus;

namespace SmartRail.Api.Endpoints.JourneysStatus;

public static class JourneyPublicationEndpoints
{
    public static IEndpointRouteBuilder MapJourneysStatusPublicationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPatch("/api/operations/journeys/{id}/status", PublishStatusAsync)
            .WithName("PublishJourneyStatusOrDiversion").WithTags("Journey operations")
            .WithSummary("Publish an official journey status or route diversion")
            .WithDescription("Resolves the verified caller's current Feature 002 grants for this operation. The approved Controller grant has global journey scope; the target journey and operation are validated. Cancellation also requires ConfirmJourneyCancellation. Actor and publication time are server-derived.")
            .AddOpenApiOperationTransformer((operation, context, _) =>
            {
                if (context.Document is { } document) AddCognitoSecurity(operation, document);
                return Task.CompletedTask;
            })
            .Produces<JourneySnapshotResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest, "application/problem+json")
            .ProducesProblem(StatusCodes.Status401Unauthorized, "application/problem+json")
            .ProducesProblem(StatusCodes.Status403Forbidden, "application/problem+json")
            .ProducesProblem(StatusCodes.Status404NotFound, "application/problem+json")
            .ProducesProblem(StatusCodes.Status409Conflict, "application/problem+json")
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable, "application/problem+json")
            .RequireAuthorization();

        endpoints.MapPatch("/api/operations/journeys/{id}/stops/{stopId}", UpdateStopAsync)
            .WithName("PublishJourneyStopChange").WithTags("Journey operations")
            .WithSummary("Publish a stop platform or operational timing change")
            .WithDescription("Resolves the verified caller's current Feature 002 grants for this operation. The approved Controller grant has global journey scope; the target journey and stop are validated. Only platform and operational timing are changed.")
            .AddOpenApiOperationTransformer((operation, context, _) =>
            {
                if (context.Document is { } document) AddCognitoSecurity(operation, document);
                return Task.CompletedTask;
            })
            .Produces<JourneySnapshotResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest, "application/problem+json")
            .ProducesProblem(StatusCodes.Status401Unauthorized, "application/problem+json")
            .ProducesProblem(StatusCodes.Status403Forbidden, "application/problem+json")
            .ProducesProblem(StatusCodes.Status404NotFound, "application/problem+json")
            .ProducesProblem(StatusCodes.Status409Conflict, "application/problem+json")
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable, "application/problem+json")
            .RequireAuthorization();
        return endpoints;
    }

    private static void AddCognitoSecurity(OpenApiOperation operation, OpenApiDocument document)
    {
        operation.Security ??= [];
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("CognitoAccessToken", document)] = []
        });
    }

    private static async Task<IResult> PublishStatusAsync(Guid id, PublishJourneyStatusRequest request,
        HttpContext context, JourneyPublicationHandler handler, GetJourneyHandler readHandler, CancellationToken cancellationToken)
    {
        var subject = context.User.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(subject))
            return ProblemDetailsConfiguration.Create(StatusCodes.Status401Unauthorized, "Authentication is required.", "authentication_required");
        if (!Enum.TryParse<JourneyStatus>(request.Status, false, out var status) && request.Status is not null)
            return Problem(400);
        var command = new PublishJourneyStatusCommand(id, request.ExpectedJourneyRevision,
            subject, request.Status is null ? null : status,
            request.RouteDiversion?.Stops?.Select(x => new DivertedStopInput(x.JourneyStopId, x.StationId, x.StopOrder,
                x.RouteStopId, x.PlannedArrivalAt, x.PlannedDepartureAt, x.OperationalArrivalAt,
                x.OperationalDepartureAt, x.Platform)).ToArray(), request.DriverObservationId, request.CorrectionOfUpdateId);
        return await MapResult(await handler.PublishStatusAsync(command, cancellationToken), id, readHandler, cancellationToken);
    }

    private static async Task<IResult> UpdateStopAsync(Guid id, Guid stopId, PublishJourneyStopChangeRequest request,
        HttpContext context, JourneyPublicationHandler handler, GetJourneyHandler readHandler, CancellationToken cancellationToken)
    {
        var subject = context.User.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(subject))
            return ProblemDetailsConfiguration.Create(StatusCodes.Status401Unauthorized, "Authentication is required.", "authentication_required");
        var command = new PublishJourneyStopCommand(id, stopId, request.ExpectedJourneyRevision,
            subject, request.Platform, request.OperationalArrivalAt,
            request.OperationalDepartureAt, request.DriverObservationId, request.CorrectionOfUpdateId,
            request.PlatformSpecified, request.OperationalArrivalSpecified, request.OperationalDepartureSpecified);
        return await MapResult(await handler.UpdateStopAsync(command, cancellationToken), id, readHandler, cancellationToken);
    }

    private static Task<IResult> MapResult(JourneyPublicationResult result, Guid journeyId,
        GetJourneyHandler readHandler, CancellationToken cancellationToken) => JourneyReadProblemMapping.Safe(async () =>
    {
        if (result.Outcome == JourneyPublicationOutcome.Published && result.State is not null)
        {
            var current = await readHandler.HandleAsync(new(journeyId), cancellationToken);
            return current.Outcome == SmartRail.Application.JourneysStatus.Queries.JourneyReadOutcome.Found && current.Value is not null
                ? Results.Ok(GetJourneyEndpoint.ToResponse(current.Value)) : Problem(503);
        }

        return result.Outcome switch
        {
            JourneyPublicationOutcome.Invalid => Problem(400),
            JourneyPublicationOutcome.Unauthorized => Problem(403),
            JourneyPublicationOutcome.NotFound => Problem(404),
            JourneyPublicationOutcome.Conflict => Problem(409),
            JourneyPublicationOutcome.Unavailable => Problem(503),
            _ => Problem(503)
        };
    });

    private static IResult Problem(int status) => status switch
    {
        400 => ProblemDetailsConfiguration.Create(400, "The publication request is invalid.", "invalid_request"),
        403 => ProblemDetailsConfiguration.Create(403, "The caller is not authorized for this journey operation.", "permission_denied"),
        404 => ProblemDetailsConfiguration.Create(404, "The requested journey resource was not found.", "not_found"),
        409 => ProblemDetailsConfiguration.Create(409, "The journey changed or the requested update conflicts with current state.", "journey_conflict"),
        _ => ProblemDetailsConfiguration.Create(503, "A required authorization or owner service is unavailable.", "source_unavailable")
    };

}

public sealed record PublishJourneyStatusRequest(int ExpectedJourneyRevision, string? Status,
    RouteDiversionRequest? RouteDiversion, Guid? DriverObservationId, Guid? CorrectionOfUpdateId);
public sealed record RouteDiversionRequest(IReadOnlyList<DivertedStopRequest>? Stops);
public sealed record DivertedStopRequest(Guid? JourneyStopId, Guid StationId, int StopOrder, Guid? RouteStopId,
    DateTimeOffset? PlannedArrivalAt, DateTimeOffset? PlannedDepartureAt, DateTimeOffset? OperationalArrivalAt,
    DateTimeOffset? OperationalDepartureAt, string? Platform);
public sealed class PublishJourneyStopChangeRequest
{
    private string? _platform;
    private DateTimeOffset? _arrival;
    private DateTimeOffset? _departure;
    public int ExpectedJourneyRevision { get; init; }
    [JsonIgnore] public bool PlatformSpecified { get; private set; }
    [JsonIgnore] public bool OperationalArrivalSpecified { get; private set; }
    [JsonIgnore] public bool OperationalDepartureSpecified { get; private set; }
    public string? Platform { get => _platform; set { _platform = value; PlatformSpecified = true; } }
    public DateTimeOffset? OperationalArrivalAt { get => _arrival; set { _arrival = value; OperationalArrivalSpecified = true; } }
    public DateTimeOffset? OperationalDepartureAt { get => _departure; set { _departure = value; OperationalDepartureSpecified = true; } }
    public Guid? DriverObservationId { get; init; }
    public Guid? CorrectionOfUpdateId { get; init; }
}
