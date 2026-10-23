using SmartRail.Application.Identity.Authorization;
using SmartRail.Application.Incidents.UseCases;
using SmartRail.Application.Incidents.Persistence;
using SmartRail.Domain.JourneysStatus.Entities;
using SmartRail.Domain.RailwayNetwork.Entities;

namespace SmartRail.Application.Operations;

public sealed record OperationsOverviewQuery(string? VerifiedSubject, DateOnly OperationalDate, Guid? StationId = null);
public enum OperationsOverviewOutcome { Listed, Unauthorized, Forbidden, Unavailable }

public sealed record OperationsOverviewMetrics(int TotalScheduled, int Running, int Delayed,
    int OpenIncidents, int Cancelled, int Completed);

public sealed record OperationsJourneyItem(Guid JourneyId, Guid ServiceId, string ServiceCode,
    string? ServiceDisplayName, string? PublicServiceNumber, string RouteCode, string RouteName,
    DateOnly OperationDate, string Origin, string Destination, DateTimeOffset? ScheduledDeparture,
    DateTimeOffset? ScheduledArrival, string Status, decimal? RecordedDelayMinutes,
    string? LastReportedStation, DateTimeOffset LastUpdateAt, string? AssignedDriver);

public sealed record OperationsOverviewResult(OperationsOverviewOutcome Outcome,
    DateOnly OperationalDate, OperationsOverviewMetrics? Metrics = null,
    IReadOnlyList<OperationsJourneyItem>? Journeys = null);

public sealed record OperationsJourneyStop(JourneyRevisionStop Stop, string StationName);
public sealed record OperationsJourneyRecord(Journey Journey, JourneyRevision Revision,
    TrainService Service, Route Route, IReadOnlyList<OperationsJourneyStop> Stops);

/// <summary>Read-only source for the current published journey state and railway reference data.</summary>
public interface IOperationsOverviewRepository
{
    Task<IReadOnlyList<OperationsJourneyRecord>> ReadForDateAsync(DateOnly operationalDate,
        CancellationToken cancellationToken = default);
}

public sealed class OperationsOverviewHandler(
    AuthorizeOperationHandler authorization,
    IOperationsOverviewRepository repository,
    ListIncidentsHandler incidents)
{
    public async Task<OperationsOverviewResult> HandleAsync(OperationsOverviewQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (string.IsNullOrWhiteSpace(query.VerifiedSubject))
            return new(OperationsOverviewOutcome.Unauthorized, query.OperationalDate);

        var resource = query.StationId is { } stationId
            ? ProtectedResource.Station(stationId)
            : ProtectedResource.None;
        var decision = await authorization.HandleAsync(new(new VerifiedPrincipal(query.VerifiedSubject),
            SmartRail.Domain.Identity.PermissionAction.ViewOperationsDashboard, resource), cancellationToken);
        if (decision.Outcome == AuthorizationOutcome.Unavailable)
            return new(OperationsOverviewOutcome.Unavailable, query.OperationalDate);
        if (decision.Outcome != AuthorizationOutcome.Allowed)
            return new(OperationsOverviewOutcome.Forbidden, query.OperationalDate);

        try
        {
            var records = await repository.ReadForDateAsync(query.OperationalDate, cancellationToken);
            if (query.StationId is { } selectedStation)
                records = records.Where(record => record.Stops.Any(stop => stop.Stop.StationId == selectedStation)).ToArray();

            var items = records.Select(ToItem)
                .OrderBy(item => item.ScheduledDeparture)
                .ThenBy(item => item.JourneyId)
                .ToArray();
            var incidentResult = await incidents.HandleAsync(new(query.VerifiedSubject,
                new IncidentFilters(StationId: query.StationId)), cancellationToken);
            if (incidentResult.Outcome == ListIncidentsOutcome.Unavailable)
                return new(OperationsOverviewOutcome.Unavailable, query.OperationalDate);
            if (incidentResult.Outcome != ListIncidentsOutcome.Listed)
                return new(OperationsOverviewOutcome.Forbidden, query.OperationalDate);

            var metrics = new OperationsOverviewMetrics(items.Length,
                items.Count(item => item.Status == "Running"),
                items.Count(item => item.Status == "Delayed" || item.RecordedDelayMinutes > 0),
                incidentResult.Incidents.Count(item => item.State is "Reported" or "InProgress" or "In Progress"),
                items.Count(item => item.Status == "Cancelled"),
                items.Count(item => item.Status == "Completed"));
            return new(OperationsOverviewOutcome.Listed, query.OperationalDate, metrics, items);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch { return new(OperationsOverviewOutcome.Unavailable, query.OperationalDate); }
    }

    private static OperationsJourneyItem ToItem(OperationsJourneyRecord record)
    {
        var orderedStops = record.Stops.OrderBy(item => item.Stop.StopOrder).ToArray();
        var first = orderedStops[0];
        var last = orderedStops[^1];
        var observations = orderedStops.SelectMany(item => new[]
        {
            item.Stop.TimingSource is "ControllerManual" or "StationStaffManual" or "ApprovedRailwayFeed" && item.Stop.TimingUpdatedAt.HasValue && item.Stop.OperationalArrivalAt.HasValue
                ? (At: item.Stop.TimingUpdatedAt.Value, Stop: item, Actual: item.Stop.OperationalArrivalAt.Value, Planned: item.Stop.PlannedArrivalAt)
                : ((DateTimeOffset At, OperationsJourneyStop Stop, DateTimeOffset Actual, DateTimeOffset? Planned)?)null,
            item.Stop.TimingSource is "ControllerManual" or "StationStaffManual" or "ApprovedRailwayFeed" && item.Stop.TimingUpdatedAt.HasValue && item.Stop.OperationalDepartureAt.HasValue
                ? (At: item.Stop.TimingUpdatedAt.Value, Stop: item, Actual: item.Stop.OperationalDepartureAt.Value, Planned: item.Stop.PlannedDepartureAt)
                : ((DateTimeOffset At, OperationsJourneyStop Stop, DateTimeOffset Actual, DateTimeOffset? Planned)?)null
        }).Where(item => item.HasValue).Select(item => item!.Value).OrderBy(item => item.At).ToArray();
        var latestObservation = observations.LastOrDefault();
        decimal? delay = observations.Length == 0 ? null : latestObservation.Planned.HasValue
            ? Math.Max(0m, (decimal)(latestObservation.Actual - latestObservation.Planned.Value).Ticks / TimeSpan.TicksPerMinute)
            : null;
        var latestStation = observations.Length == 0 ? null : latestObservation.Stop.StationName;
        var lastUpdate = record.Revision.UpdateId.HasValue ? record.Revision.PublishedAt : record.Journey.InitializedAt;

        return new OperationsJourneyItem(record.Journey.JourneyInstanceId, record.Service.ServiceId,
            record.Service.ServiceCode, record.Service.DisplayName, record.Service.PublicServiceNumber,
            record.Route.RouteCode, record.Route.Name, record.Journey.OperationDate, first.StationName,
            last.StationName, first.Stop.PlannedDepartureAt, last.Stop.PlannedArrivalAt,
            record.Revision.Status.ToString(), delay, latestStation, lastUpdate, null);
    }
}
