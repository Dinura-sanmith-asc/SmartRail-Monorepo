using SmartRail.Application.JourneysStatus.Queries;
using SmartRail.Application.JourneysStatus.Queries.GetStationDepartures;
using SmartRail.Domain.JourneysStatus;
using SmartRail.Domain.JourneysStatus.Entities;
using SmartRail.UnitTests.JourneysStatus.Fixtures;

namespace SmartRail.UnitTests.JourneysStatus;

public sealed class JourneyProjectionConsistencyTests
{
    [Fact]
    public async Task Station_view_uses_one_candidate_revision_for_status_source_and_publication_time()
    {
        var journeyId = Guid.NewGuid();
        var journey = JourneyFixtureBuilder.Journey(journeyId, "revision-consistency");
        var publishedAt = DateTimeOffset.Parse("2026-10-17T13:15:00Z");
        var revision = new JourneyRevision(journeyId, 2, JourneyStatus.Delayed, publishedAt,
            "ControllerManual", "status", Guid.NewGuid(), JourneyPublicationKind.Status, 1,
            "private-controller-subject");
        var stop = new JourneyRevisionStop(journeyId, 2, Guid.NewGuid(), 1,
            Guid.Parse("10000000-0000-0000-0000-000000000001"), Guid.NewGuid(),
            plannedDepartureAt: DateTimeOffset.Parse("2026-10-17T17:50:00Z"));
        var candidate = new JourneyStationDepartureCandidate(journey, revision, stop, "Colombo Fort");
        var result = await new GetStationDeparturesHandler(new SingleCandidateRepository(candidate))
            .HandleAsync(new(stop.StationId, "2026-10-17"));
        var view = Assert.Single(result.Departures!);
        Assert.Equal(revision.Revision, view.Revision);
        Assert.Equal(revision.Status.ToString(), view.Status);
        Assert.Equal(revision.Source.ToString(), view.Source);
        Assert.Equal(revision.PublishedAt, view.UpdatedAt);
    }

    private sealed class SingleCandidateRepository(JourneyStationDepartureCandidate candidate) : IJourneyReadRepository
    {
        public Task<JourneySnapshotReadResult> ReadSnapshotAsync(Guid journeyInstanceId, CancellationToken cancellationToken) =>
            Task.FromResult(new JourneySnapshotReadResult(JourneyReadOutcome.NotFound));
        public Task<JourneyHistoryReadResult> ReadHistoryAsync(Guid journeyInstanceId, CancellationToken cancellationToken) =>
            Task.FromResult(new JourneyHistoryReadResult(JourneyReadOutcome.NotFound));
        public Task<JourneyStationDeparturesReadResult> ReadStationDeparturesAsync(Guid stationId, CancellationToken cancellationToken) =>
            Task.FromResult(new JourneyStationDeparturesReadResult(JourneyReadOutcome.Found, [candidate]));
    }
}
