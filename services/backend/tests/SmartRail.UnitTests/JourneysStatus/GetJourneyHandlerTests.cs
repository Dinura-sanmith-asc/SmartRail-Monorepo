using SmartRail.Application.JourneysStatus.Queries;
using SmartRail.Application.JourneysStatus.Projections;
using SmartRail.Domain.JourneysStatus;
using SmartRail.Domain.JourneysStatus.Entities;
using SmartRail.UnitTests.JourneysStatus.Fixtures;

namespace SmartRail.UnitTests.JourneysStatus;

public sealed class GetJourneyHandlerTests
{
    [Fact]
    public async Task Projects_authoritative_identity_linkage_ordered_stops_and_unknown_eta()
    {
        var journey = JourneyFixtureBuilder.Journey();
        var revision = new JourneyRevision(journey.JourneyInstanceId, 1, JourneyStatus.Scheduled,
            DateTimeOffset.Parse("2026-10-16T12:00:00Z"), "ScheduledBaseline");
        var later = Stop(journey.JourneyInstanceId, 1, 2);
        var earlier = Stop(journey.JourneyInstanceId, 1, 1);
        var repository = new FakeReadRepository
        {
            Snapshot = new(JourneyReadOutcome.Found, journey, revision, [later, earlier],
                new Dictionary<Guid, string> { [earlier.StationId] = "Central", [later.StationId] = "North" })
        };

        var result = await new GetJourneyHandler(repository).HandleAsync(new(journey.JourneyInstanceId));

        Assert.Equal(JourneyReadOutcome.Found, result.Outcome);
        Assert.NotNull(result.Value);
        Assert.Equal(journey.JourneyInstanceId, result.Value.JourneyInstanceId);
        Assert.Equal(journey.ServiceId, result.Value.ServiceId);
        Assert.Equal(journey.ScheduleId, result.Value.ScheduleId);
        Assert.Equal(journey.ScheduleRevision, result.Value.ScheduleRevision);
        Assert.Equal(journey.OperationDate, result.Value.OperationDate);
        Assert.Equal(journey.FirstDepartureOccurrence, result.Value.FirstDepartureOccurrence);
        Assert.Equal("ScheduledBaseline", result.Value.Source);
        Assert.Null(result.Value.PublishedAt);
        Assert.Null(result.Value.LastUpdatedAt);
        Assert.Null(result.Value.Eta);
        Assert.Equal([1, 2], result.Value.Stops.Select(item => item.StopOrder));
        Assert.Equal("Central", result.Value.Stops[0].StationName);
        Assert.Equal("none", result.Value.Stops[0].Ambiguity);
    }

    [Fact]
    public async Task Distinguishes_missing_journey_from_unavailable_current_revision()
    {
        var repository = new FakeReadRepository
        {
            Snapshot = new(JourneyReadOutcome.NotFound),
            History = new(JourneyReadOutcome.Unavailable)
        };
        var detail = new GetJourneyHandler(repository);
        var history = new GetJourneyUpdatesHandler(repository);

        Assert.Equal(JourneyReadOutcome.NotFound,
            (await detail.HandleAsync(new(Guid.NewGuid()))).Outcome);
        repository = new FakeReadRepository { Snapshot = new(JourneyReadOutcome.Unavailable) };
        Assert.Equal(JourneyReadOutcome.Unavailable,
            (await new GetJourneyHandler(repository).HandleAsync(new(Guid.NewGuid()))).Outcome);
        Assert.Equal(JourneyReadOutcome.Unavailable,
            (await history.HandleAsync(new(Guid.NewGuid()))).Outcome);
    }

    private static JourneyRevisionStop Stop(Guid journeyId, int revision, int order) =>
        new(journeyId, revision, Guid.Parse($"40000000-0000-0000-0000-{order:000000000000}"), order,
            Guid.Parse($"10000000-0000-0000-0000-{order:000000000000}"),
            plannedDepartureAt: order == 1 ? DateTimeOffset.Parse("2026-10-17T17:50:00Z") : null);

    internal sealed class FakeReadRepository : IJourneyReadRepository
    {
        public JourneySnapshotReadResult Snapshot { get; init; } = new(JourneyReadOutcome.NotFound);
        public JourneyHistoryReadResult History { get; init; } = new(JourneyReadOutcome.NotFound);
        public JourneyStationDeparturesReadResult Departures { get; init; } = new(JourneyReadOutcome.NotFound);
        public Task<JourneySnapshotReadResult> ReadSnapshotAsync(Guid journeyInstanceId, CancellationToken cancellationToken) => Task.FromResult(Snapshot);
        public Task<JourneyHistoryReadResult> ReadHistoryAsync(Guid journeyInstanceId, CancellationToken cancellationToken) => Task.FromResult(History);
        public Task<JourneyStationDeparturesReadResult> ReadStationDeparturesAsync(Guid stationId, CancellationToken cancellationToken) => Task.FromResult(Departures);
    }
}
