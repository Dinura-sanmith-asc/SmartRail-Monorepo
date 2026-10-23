using System.Globalization;
using SmartRail.Application.JourneysStatus.Projections;
using SmartRail.Domain.JourneysStatus;

namespace SmartRail.Application.JourneysStatus.Queries.GetStationDepartures;

public enum StationDeparturesOutcome { Success, InvalidDate, NotFound, Unavailable }
public sealed record GetStationDeparturesQuery(Guid StationId, string? Date);
public sealed record GetStationDeparturesResult(StationDeparturesOutcome Outcome,
    IReadOnlyList<StationDepartureProjection>? Departures = null);

/// <summary>Projects only known departure times using the station-local Colombo calendar date.</summary>
public sealed class GetStationDeparturesHandler(IJourneyReadRepository repository)
{
    private static readonly TimeSpan ColomboOffset = TimeSpan.FromHours(5.5);

    public async Task<GetStationDeparturesResult> HandleAsync(GetStationDeparturesQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.StationId == Guid.Empty || query.Date is null ||
            !DateOnly.TryParseExact(query.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var requestedDate) ||
            requestedDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) != query.Date)
            return new(StationDeparturesOutcome.InvalidDate);

        var read = await repository.ReadStationDeparturesAsync(query.StationId, cancellationToken);
        if (read.Outcome == JourneyReadOutcome.NotFound) return new(StationDeparturesOutcome.NotFound);
        if (read.Outcome != JourneyReadOutcome.Found || read.Candidates is null)
            return new(StationDeparturesOutcome.Unavailable);

        var projected = new List<(StationDepartureProjection View, DateTimeOffset EffectiveInstant)>();
        foreach (var candidate in read.Candidates)
        {
            var stop = candidate.Stop;
            DateTimeOffset? scheduled = stop.PlannedDepartureAt;
            DateTimeOffset? operational = stop.OperationalDepartureAt is not null &&
                stop.TimingSource is "ControllerManual" or "StationStaffManual" or "ApprovedRailwayFeed"
                ? stop.OperationalDepartureAt : null;
            var scheduledArrival = stop.PlannedArrivalAt;
            var operationalArrival = stop.OperationalArrivalAt is not null &&
                stop.TimingSource is "ControllerManual" or "StationStaffManual" or "ApprovedRailwayFeed"
                ? stop.OperationalArrivalAt : null;
            var effective = scheduled ?? scheduledArrival ?? (stop.RouteStopId is null ? operational ?? operationalArrival : null);
            if (!effective.HasValue || DateOnly.FromDateTime(effective.Value.ToOffset(ColomboOffset).DateTime) != requestedDate)
                continue;

            var revision = candidate.Revision;
            var updatedAt = revision.PublicationKind == JourneyPublicationKind.InitializationBaseline
                ? candidate.Journey.InitializedAt : revision.PublishedAt;
            projected.Add((new(candidate.Journey.JourneyInstanceId, candidate.Journey.ServiceId,
                candidate.Journey.OperationDate, requestedDate, revision.Revision, revision.Status.ToString(),
                stop.StationId, candidate.StationName, stop.StopOrder, scheduled, operational,
                stop.Platform, revision.Source.ToString(), updatedAt, stop.AmbiguityState,
                scheduledArrival, operationalArrival, candidate.ServiceCode, candidate.DisplayName,
                candidate.PublicServiceNumber, candidate.OriginName, candidate.DestinationName), effective.Value));
        }

        var ambiguousKeys = projected.GroupBy(item => SelectionKey(item.View))
            .Where(group => group.Count() > 1).Select(group => group.Key).ToHashSet();

        var results = projected.OrderBy(item => item.EffectiveInstant)
            .ThenBy(item => item.View.JourneyInstanceId)
            .Select(item => item.View with
            {
                Ambiguity = item.View.Ambiguity == "ambiguous" || ambiguousKeys.Contains(SelectionKey(item.View))
                    ? "ambiguous" : "none"
            }).ToArray();
        return new(StationDeparturesOutcome.Success, results);
    }

    private static (Guid ServiceId, DateOnly OperationDate, DateOnly StationDate, Guid StationId,
        int StopOrder, DateTimeOffset? Scheduled, DateTimeOffset? Operational, string? Platform) SelectionKey(
        StationDepartureProjection item) => (item.ServiceId, item.OperationDate, item.StationLocalDepartureDate,
        item.StationId, item.StopOrder, item.ScheduledDepartureAt, item.OperationalDepartureAt, item.Platform);
}
