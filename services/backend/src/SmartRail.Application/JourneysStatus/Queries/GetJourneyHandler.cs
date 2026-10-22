using SmartRail.Domain.JourneysStatus;
using SmartRail.Application.JourneysStatus.Projections;

namespace SmartRail.Application.JourneysStatus.Queries;

public sealed record GetJourneyQuery(Guid JourneyInstanceId);

public sealed class GetJourneyHandler(IJourneyReadRepository repository)
{
    public async Task<JourneyReadResult<JourneySnapshotView>> HandleAsync(GetJourneyQuery query,
        CancellationToken cancellationToken = default)
    {
        var result = await repository.ReadSnapshotAsync(query.JourneyInstanceId, cancellationToken);
        if (result.Outcome != JourneyReadOutcome.Found) return new(result.Outcome);
        if (result.Journey is null || result.Revision is null || result.Stops is null)
            return new(JourneyReadOutcome.Unavailable);

        DateTimeOffset? publishedAt = result.Revision.PublicationKind == JourneyPublicationKind.InitializationBaseline
            ? null : result.Revision.PublishedAt;
        var stationNames = result.PublishedStationNames ?? new Dictionary<Guid, string>();
        var stops = result.Stops.OrderBy(stop => stop.StopOrder).Select(stop => new JourneyStopView(
            stop.JourneyStopId, stop.StopOrder, stop.StationId,
            stationNames.TryGetValue(stop.StationId, out var name) ? name : throw new JourneyReadProjectionException("Published station name is unavailable."),
            stop.PlannedArrivalAt, stop.PlannedDepartureAt, stop.OperationalArrivalAt,
            stop.OperationalDepartureAt, stop.Platform, SafeTimingSource(stop.TimingSource), stop.TimingUpdatedAt,
            stop.AmbiguityState)).ToArray();
        var journey = result.Journey;
        var revision = result.Revision;
        return new(JourneyReadOutcome.Found, new JourneySnapshotView(journey.JourneyInstanceId,
            journey.ServiceId, journey.ScheduleId, journey.ScheduleRevision, journey.OperationDate,
            journey.FirstDepartureOccurrence, revision.Revision, revision.Status.ToString(),
            revision.Source.ToString(), publishedAt, publishedAt, stops));
    }

    private static string? SafeTimingSource(string? value) => value is "ControllerManual" or "ApprovedRailwayFeed"
        ? value : null;
}
