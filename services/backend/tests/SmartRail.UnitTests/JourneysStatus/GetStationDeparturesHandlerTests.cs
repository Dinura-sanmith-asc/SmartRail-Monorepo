using SmartRail.Application.JourneysStatus.Projections;
using SmartRail.Application.JourneysStatus.Queries;
using SmartRail.Application.JourneysStatus.Queries.GetStationDepartures;
using SmartRail.Domain.JourneysStatus;
using SmartRail.Domain.JourneysStatus.Entities;
using SmartRail.UnitTests.JourneysStatus.Fixtures;

namespace SmartRail.UnitTests.JourneysStatus;

public sealed class GetStationDeparturesHandlerTests
{
    private static readonly Guid Station = Guid.Parse("10000000-0000-0000-0000-000000000001");

    [Fact]
    public async Task Filters_by_Colombo_station_date_preserves_operation_date_and_sorts_known_departures()
    {
        var first = Candidate("50000000-0000-0000-0000-000000000001", 1,
            DateTimeOffset.Parse("2026-10-17T17:50:00Z")); // 23:20 Asia/Colombo
        var second = Candidate("50000000-0000-0000-0000-000000000002", 1,
            DateTimeOffset.Parse("2026-10-17T18:00:00Z"));
        var nextLocalDay = Candidate("50000000-0000-0000-0000-000000000003", 1,
            DateTimeOffset.Parse("2026-10-17T18:35:00Z")); // 00:05 on October 18
        var handler = new GetStationDeparturesHandler(new FakeRepository([second, nextLocalDay, first]));

        var result = await handler.HandleAsync(new(Station, "2026-10-17"));

        Assert.Equal(StationDeparturesOutcome.Success, result.Outcome);
        var departures = Assert.IsAssignableFrom<IReadOnlyList<StationDepartureProjection>>(result.Departures);
        Assert.Equal(new[] { first.Journey.JourneyInstanceId, second.Journey.JourneyInstanceId },
            departures.Select(item => item.JourneyInstanceId));
        Assert.All(departures, item => Assert.Equal(new DateOnly(2026, 10, 17), item.OperationDate));
        var overnight = await handler.HandleAsync(new(Station, "2026-10-18"));
        var nextDayDepartures = Assert.IsAssignableFrom<IReadOnlyList<StationDepartureProjection>>(overnight.Departures);
        Assert.Equal(new[] { nextLocalDay.Journey.JourneyInstanceId }, nextDayDepartures.Select(item => item.JourneyInstanceId));
        Assert.Equal(new DateOnly(2026, 10, 17), nextDayDepartures[0].OperationDate);
    }

    [Fact]
    public async Task Uses_verified_operational_time_only_for_diversion_stops_and_never_invents_missing_time()
    {
        var diversion = Candidate("50000000-0000-0000-0000-000000000004", 1,
            planned: null, diversion: true, operational: DateTimeOffset.Parse("2026-10-17T19:00:00Z"));
        var unverified = Candidate("50000000-0000-0000-0000-000000000005", 1,
            planned: null, diversion: true, operational: DateTimeOffset.Parse("2026-10-17T19:00:00Z"), timingSource: "Unknown");
        var unknown = Candidate("50000000-0000-0000-0000-000000000006", 1,
            planned: null, diversion: true);
        var handler = new GetStationDeparturesHandler(new FakeRepository([diversion, unverified, unknown]));

        var result = await handler.HandleAsync(new(Station, "2026-10-18"));

        var departure = Assert.Single(result.Departures!);
        Assert.Equal(diversion.Journey.JourneyInstanceId, departure.JourneyInstanceId);
        Assert.Null(departure.ScheduledDepartureAt);
        Assert.Equal(diversion.Stop.OperationalDepartureAt, departure.OperationalDepartureAt);
    }

    [Theory]
    [InlineData("2026-2-01")]
    [InlineData("2026-02-30")]
    [InlineData("2026-02-01T00:00:00Z")]
    [InlineData(null)]
    public async Task Rejects_missing_or_noncanonical_station_date(string? date)
    {
        var result = await new GetStationDeparturesHandler(new FakeRepository([])).HandleAsync(new(Station, date));
        Assert.Equal(StationDeparturesOutcome.InvalidDate, result.Outcome);
    }

    [Fact]
    public async Task Marks_identical_public_departure_descriptors_ambiguous_without_collapsing_instances()
    {
        var first = Candidate("50000000-0000-0000-0000-000000000007", 1,
            DateTimeOffset.Parse("2026-10-17T17:50:00Z"));
        var second = Candidate("50000000-0000-0000-0000-000000000008", 1,
            DateTimeOffset.Parse("2026-10-17T17:50:00Z"));
        var result = await new GetStationDeparturesHandler(new FakeRepository([first, second]))
            .HandleAsync(new(Station, "2026-10-17"));
        Assert.Equal(2, result.Departures!.Count);
        Assert.All(result.Departures, item => Assert.Equal("ambiguous", item.Ambiguity));
    }

    private static JourneyStationDepartureCandidate Candidate(string journeyId, int revision,
        DateTimeOffset? planned, bool diversion = false, DateTimeOffset? operational = null,
        string timingSource = "ControllerManual")
    {
        var id = Guid.Parse(journeyId);
        var journey = JourneyFixtureBuilder.Journey(id, "departure-" + journeyId);
        var publishedAt = DateTimeOffset.Parse("2026-10-16T12:00:00Z");
        var journeyRevision = new JourneyRevision(id, revision, JourneyStatus.Scheduled, publishedAt,
            "ScheduledBaseline");
        var stop = new JourneyRevisionStop(id, revision, Guid.NewGuid(), 1, Station, diversion ? null : Guid.NewGuid(),
            plannedDepartureAt: planned, operationalDepartureAt: operational,
            timingSource: operational.HasValue ? timingSource : null);
        return new(journey, journeyRevision, stop, "Colombo Fort");
    }

    private sealed class FakeRepository(IReadOnlyList<JourneyStationDepartureCandidate> candidates) : IJourneyReadRepository
    {
        public Task<JourneySnapshotReadResult> ReadSnapshotAsync(Guid journeyInstanceId, CancellationToken cancellationToken) =>
            Task.FromResult(new JourneySnapshotReadResult(JourneyReadOutcome.NotFound));
        public Task<JourneyHistoryReadResult> ReadHistoryAsync(Guid journeyInstanceId, CancellationToken cancellationToken) =>
            Task.FromResult(new JourneyHistoryReadResult(JourneyReadOutcome.NotFound));
        public Task<JourneyStationDeparturesReadResult> ReadStationDeparturesAsync(Guid stationId, CancellationToken cancellationToken) =>
            Task.FromResult(new JourneyStationDeparturesReadResult(JourneyReadOutcome.Found, candidates));
    }
}
