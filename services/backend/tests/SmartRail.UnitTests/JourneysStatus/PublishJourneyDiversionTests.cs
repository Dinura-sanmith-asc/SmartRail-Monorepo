using SmartRail.Application.JourneysStatus.Boundaries;
using SmartRail.Application.JourneysStatus.Publication;
using SmartRail.Application.JourneysStatus.Persistence;
using SmartRail.Domain.Identity;
using SmartRail.Domain.JourneysStatus;
using SmartRail.Domain.JourneysStatus.Entities;
using SmartRail.UnitTests.JourneysStatus.Fixtures;

namespace SmartRail.UnitTests.JourneysStatus;

public sealed class PublishJourneyDiversionTests
{
    [Fact]
    public async Task Retains_existing_stop_ids_generates_new_ids_and_keeps_removed_stop_in_prior_revision()
    {
        var repository = new FakeRepository();
        var handler = Create(repository);
        var origin = repository.State.Stops[0];
        var replacement = new[]
        {
            new DivertedStopInput(origin.JourneyStopId, origin.StationId, 1, origin.RouteStopId,
                origin.PlannedArrivalAt, origin.PlannedDepartureAt, null, null, null),
            new DivertedStopInput(null, Guid.Parse("10000000-0000-0000-0000-000000000003"), 2,
                null, null, null, null, null, null)
        };

        var result = await handler.PublishStatusAsync(new(repository.State.Journey.JourneyInstanceId,
            1, "verified-controller", null, replacement, null, null));

        Assert.Equal(JourneyPublicationOutcome.Published, result.Outcome);
        Assert.Equal(origin.JourneyStopId, repository.State.Stops[0].JourneyStopId);
        Assert.NotEqual(Guid.Empty, repository.State.Stops[1].JourneyStopId);
        Assert.NotEqual(origin.JourneyStopId, repository.State.Stops[1].JourneyStopId);
        Assert.Equal(2, repository.PriorRevisionStops.Count);
        Assert.Contains(repository.PriorRevisionStops, stop => stop.JourneyStopId ==
            Guid.Parse("41000000-0000-0000-0000-000000000002"));
        Assert.Equal(repository.State.Stops.Select(item => item.StopOrder), new[] { 1, 2 });
    }

    [Fact]
    public async Task Rejects_duplicate_or_foreign_stop_references_without_writing()
    {
        foreach (var duplicate in new[] { true, false })
        {
            var repository = new FakeRepository();
            var handler = Create(repository);
            var retained = repository.State.Stops[0];
            var firstId = duplicate ? retained.JourneyStopId : Guid.NewGuid();
            var inputs = new[]
            {
                new DivertedStopInput(firstId, retained.StationId, 1, null, null, null, null, null, null),
                new DivertedStopInput(duplicate ? retained.JourneyStopId : null,
                    Guid.Parse("10000000-0000-0000-0000-000000000003"), 2, null, null, null, null, null, null)
            };
            var result = await handler.PublishStatusAsync(new(repository.State.Journey.JourneyInstanceId,
                1, "verified-controller", null, inputs, null, null));
            Assert.Equal(JourneyPublicationOutcome.Invalid, result.Outcome);
            Assert.Equal(0, repository.AppendCount);
            Assert.Equal(1, repository.State.Journey.CurrentRevision);
        }
    }

    private static JourneyPublicationHandler Create(FakeRepository repository) => new(repository,
        new AllowAuthorization(), new VerifiedObservation(), new FixedTimeProvider(DateTimeOffset.Parse("2026-10-17T12:00:00Z")));

    private sealed class AllowAuthorization : IJourneyPublicationAuthorization
    {
        public Task<PublicationAuthorizationDecision> AuthorizeAsync(string verifiedSubject, Guid journeyInstanceId,
            PermissionAction action, CancellationToken cancellationToken = default) =>
            Task.FromResult(new PublicationAuthorizationDecision(PublicationAuthorizationOutcome.Allowed));
    }
    private sealed class VerifiedObservation : IDriverObservationVerifier
    {
        public Task<DriverObservationVerification> VerifyAsync(string verifiedActorSubject, Guid observationId,
            Guid journeyInstanceId, CancellationToken cancellationToken = default) => Task.FromResult(DriverObservationVerification.Verified());
    }
    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
    private sealed class FakeRepository : IJourneyPublicationRepository
    {
        private readonly JourneyRevision baseline;
        public JourneyPublicationState State { get; private set; }
        public IReadOnlyList<JourneyRevisionStop> PriorRevisionStops { get; private set; } = [];
        public int AppendCount { get; private set; }

        public FakeRepository()
        {
            var journey = JourneyFixtureBuilder.Journey();
            var first = new JourneyRevisionStop(journey.JourneyInstanceId, 1,
                Guid.Parse("41000000-0000-0000-0000-000000000001"), 1,
                Guid.Parse("10000000-0000-0000-0000-000000000001"), Guid.NewGuid(),
                plannedDepartureAt: DateTimeOffset.Parse("2026-10-17T17:50:00Z"));
            var second = new JourneyRevisionStop(journey.JourneyInstanceId, 1,
                Guid.Parse("41000000-0000-0000-0000-000000000002"), 2,
                Guid.Parse("10000000-0000-0000-0000-000000000002"), Guid.NewGuid());
            baseline = new(journey.JourneyInstanceId, 1, JourneyStatus.Scheduled,
                DateTimeOffset.Parse("2026-10-17T12:00:00Z"), "ScheduledBaseline");
            State = new(journey, baseline, [first, second], [baseline]);
        }
        public Task<JourneyPublicationState?> FindCurrentAsync(Guid journeyInstanceId, CancellationToken cancellationToken = default) =>
            Task.FromResult<JourneyPublicationState?>(journeyInstanceId == State.Journey.JourneyInstanceId ? State : null);
        public Task<bool> HasEarlierUpdateAsync(Guid journeyInstanceId, Guid updateId, int beforeRevision, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<JourneyRevisionAppendResult> AppendAsync(Guid journeyInstanceId, int expectedRevision,
            JourneyRevision revision, IReadOnlyCollection<JourneyRevisionStop> stops, CancellationToken cancellationToken = default)
        {
            AppendCount++;
            PriorRevisionStops = State.Stops;
            var old = State.Journey;
            var journey = new Journey(old.JourneyInstanceId, old.ServiceId, old.ScheduleId, old.ScheduleRevision,
                old.OperationDate, old.FirstDepartureOccurrence, old.SourceSystem, old.SourceInstanceKey,
                old.InitializedAt, old.InitializedBySubject, old.InitializationProvenance, revision.Revision);
            State = new(journey, revision, stops.OrderBy(stop => stop.StopOrder).ToArray(), [baseline, revision]);
            return Task.FromResult(new JourneyRevisionAppendResult(JourneyRevisionAppendOutcome.Appended, revision.Revision));
        }
    }
}
