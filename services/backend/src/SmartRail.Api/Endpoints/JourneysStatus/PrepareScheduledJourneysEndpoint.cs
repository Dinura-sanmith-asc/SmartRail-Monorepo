using System.Globalization;
using System.Security.Claims;
using SmartRail.Api.Errors;
using SmartRail.Application.Identity.Authorization;
using SmartRail.Domain.Identity;
using SmartRail.Infrastructure.JourneysStatus.Initialization;

namespace SmartRail.Api.Endpoints.JourneysStatus;

public static class PrepareScheduledJourneysEndpoint
{
    public static IEndpointRouteBuilder MapPrepareScheduledJourneys(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/operations/journeys/prepare", HandleAsync)
            .WithName("PrepareScheduledJourneys")
            .WithTags("Journey operations")
            .WithSummary("Create missing dated journeys from published timetables")
            .RequireAuthorization();
        return endpoints;
    }

    private static async Task<IResult> HandleAsync(HttpContext context,
        AuthorizeOperationHandler authorization, ScheduledJourneyMaterializer materializer,
        CancellationToken cancellationToken)
    {
        if (!context.Request.Query.TryGetValue("date", out var values) || values.Count != 1 ||
            !DateOnly.TryParseExact(values[0], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ||
            date == DateOnly.MinValue)
            return ProblemDetailsConfiguration.Create(400, "A date is required in YYYY-MM-DD format.", "invalid_operation_date");
        Guid? stationId = null;
        if (context.Request.Query.TryGetValue("stationId", out var stationValues))
        {
            if (stationValues.Count != 1 || !Guid.TryParse(stationValues[0], out var parsed) || parsed == Guid.Empty)
                return ProblemDetailsConfiguration.Create(400, "A valid station ID is required.", "invalid_station_id");
            stationId = parsed;
        }

        var subject = context.User.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(subject))
            return ProblemDetailsConfiguration.Create(401, "Authentication is required.", "authentication_required");
        var decision = await authorization.HandleAsync(new(new VerifiedPrincipal(subject),
            stationId.HasValue ? PermissionAction.ViewOperationsDashboard : PermissionAction.ManageStations,
            stationId.HasValue ? ProtectedResource.Station(stationId.Value) : ProtectedResource.None), cancellationToken);
        if (decision.Outcome == AuthorizationOutcome.Unavailable)
            return ProblemDetailsConfiguration.Create(503, "Authorization is temporarily unavailable.", "authorization_unavailable");
        if (decision.Outcome != AuthorizationOutcome.Allowed)
            return ProblemDetailsConfiguration.Create(403, "Permission is denied.", "permission_denied");

        try
        {
            // A stop shortly after midnight may belong to a journey that started the previous day.
            var previous = await materializer.PrepareAsync(date.AddDays(-1), stationId, cancellationToken);
            var selected = await materializer.PrepareAsync(date, stationId, cancellationToken);
            return Results.Ok(new { date, created = previous.Created + selected.Created,
                replanned = previous.Replanned + selected.Replanned,
                needsReview = previous.NeedsReview + selected.NeedsReview });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (InvalidOperationException)
        {
            return ProblemDetailsConfiguration.Create(503, "Scheduled journeys could not be prepared.", "journey_preparation_unavailable");
        }
    }
}
