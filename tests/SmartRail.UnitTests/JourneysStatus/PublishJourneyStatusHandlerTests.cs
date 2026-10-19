using SmartRail.Application.JourneysStatus.Boundaries;
using SmartRail.Application.JourneysStatus.Publication;
using SmartRail.Application.JourneysStatus.Persistence;
using SmartRail.Domain.Identity;
using SmartRail.Domain.JourneysStatus;
using SmartRail.Domain.JourneysStatus.Entities;
using SmartRail.UnitTests.JourneysStatus.Fixtures;

namespace SmartRail.UnitTests.JourneysStatus;

public sealed class PublishJourneyStatusHandlerTests
{
    [Fact]
    public async Task Controller_publication_appends_immutable_revision_with_server_audit()
    {
        var repository = new FakeRepository();
        var authorization = new FakeAuthorization();
        var handler = Create(repository, authorization);
        var result = await handler.PublishStatusAsync(StatusCommand(JourneyStatus.Delayed));

        Assert.Equal(JourneyPublicationOutcome.Published, result.Outcome);
        Assert.Equal(1, repository.AppendCount);
        Assert.Equal(2, result.State!.Revision.Revision);
        Assert.Equal(JourneyPublicationKind.Status, result.State.Revision.PublicationKind);
        Assert.Equal("verified-controller", result.State.Revision.PublishedBySubject);
        Assert.Equal("ControllerManual", result.State.Revision.Source.ToString());
        Assert.Single(authorization.Actions);
        Assert.Equal(PermissionAction.PublishOfficialJourneyStatus, authorization.Actions[0]);
    }

    [Fact]
    public async Task Cancellation_requires_both_approved_controller_actions()
    {
        var repository = new FakeRepository();
        var authorization = new FakeAuthorization { DenyCancellation = true };
        var result = await Create(repository, authorization).PublishStatusAsync(StatusCommand(JourneyStatus.Cancelled));

        Assert.Equal(JourneyPublicationOutcome.Unauthorized, result.Outcome);
        Assert.Equal(0, repository.AppendCount);
        Assert.Contains(PermissionAction.ConfirmJourneyCancellation, authorization.Actions);
    }

    [Fact]
    public async Task Unavailable_driver_owner_evidence_fails_closed_without_write()
    {
        var repository = new FakeRepository();
        var verifier = new FakeVerifier(DriverObservationVerification.Unavailable());
        var result = await Create(repository, new FakeAuthorization(), verifier)
            .PublishStatusAsync(StatusCommand(JourneyStatus.Delayed) with { DriverObservationId = Guid.NewGuid() });

        Assert.Equal(JourneyPublicationOutcome.Unavailable, result.Outcome);
        Assert.Equal(0, repository.AppendCount);
    }

    [Theory]
    [InlineData(DriverObservationVerificationOutcome.Malformed, JourneyPublicationOutcome.Invalid)]
    [InlineData(DriverObservationVerificationOutcome.MissingOrInaccessible, JourneyPublicationOutcome.NotFound)]
    [InlineData(DriverObservationVerificationOutcome.StaleOrIncompatible, JourneyPublicationOutcome.Conflict)]
    [InlineData(DriverObservationVerificationOutcome.Unavailable, JourneyPublicationOutcome.Unavailable)]
    public async Task Driver_evidence_failures_have_distinct_outcomes_and_never_write(
        DriverObservationVerificationOutcome verificationOutcome, JourneyPublicationOutcome expected)
    {
        var repository = new FakeRepository();
        var verifier = new FakeVerifier(new DriverObservationVerification(verificationOutcome));
        var result = await Create(repository, new FakeAuthorization(), verifier)
            .PublishStatusAsync(StatusCommand(JourneyStatus.Delayed) with { DriverObservationId = Guid.NewGuid() });
        Assert.Equal(expected, result.Outcome);
        Assert.Equal(0, repository.AppendCount);
    }

    [Fact]
    public async Task Route_diversion_keeps_retained_identity_and_issues_new_identity_for_added_stop()
    {
        var repository = new FakeRepository();
        var retained = repository.State.Stops[0];
        var result = await Create(repository, new FakeAuthorization()).PublishStatusAsync(new(
            repository.State.Journey.JourneyInstanceId, 1, "verified-controller", null,
            [new(retained.JourneyStopId, retained.StationId, 1, retained.RouteStopId, retained.PlannedArrivalAt,
                retained.PlannedDepartureAt, null, null, null),
             new(null, Guid.Parse("10000000-0000-0000-0000-000000000003"), 2, null, null, null, null, null, "4")], null, null));
        Assert.Equal(JourneyPublicationOutcome.Published, result.Outcome);
        Assert.Equal(retained.JourneyStopId, result.State!.Stops[0].JourneyStopId);
        Assert.NotEqual(Guid.Empty, result.State.Stops[1].JourneyStopId);
        Assert.NotEqual(retained.JourneyStopId, result.State.Stops[1].JourneyStopId);
        Assert.Equal("routeDiversion", result.State.Revision.PublicChangeType);
    }

    [Fact]
    public async Task Invalid_lifecycle_and_stale_revision_leave_state_unchanged()
    {
        var repository = new FakeRepository(JourneyStatus.Cancelled, includeOfficialCurrent: true);
        var handler = Create(repository, new FakeAuthorization());
        var invalid = await handler.PublishStatusAsync(StatusCommand(JourneyStatus.Running) with { ExpectedJourneyRevision = 2 });
        var stale = await handler.PublishStatusAsync(StatusCommand(JourneyStatus.Delayed) with { ExpectedJourneyRevision = 3 });

        Assert.Equal(JourneyPublicationOutcome.Invalid, invalid.Outcome);
        Assert.Equal(0, repository.AppendCount);
        Assert.Equal(JourneyPublicationOutcome.Conflict, stale.Outcome);
    }

    [Theory]
    [InlineData(JourneyStatus.Scheduled, JourneyStatus.Running, true)]
    [InlineData(JourneyStatus.Scheduled, JourneyStatus.Delayed, true)]
    [InlineData(JourneyStatus.Scheduled, JourneyStatus.Interrupted, true)]
    [InlineData(JourneyStatus.Scheduled, JourneyStatus.Cancelled, true)]
    [InlineData(JourneyStatus.Running, JourneyStatus.Delayed, true)]
    [InlineData(JourneyStatus.Running, JourneyStatus.Interrupted, true)]
    [InlineData(JourneyStatus.Running, JourneyStatus.Completed, true)]
    [InlineData(JourneyStatus.Running, JourneyStatus.Terminated, true)]
    [InlineData(JourneyStatus.Delayed, JourneyStatus.Running, true)]
    [InlineData(JourneyStatus.Delayed, JourneyStatus.Interrupted, true)]
    [InlineData(JourneyStatus.Delayed, JourneyStatus.Completed, true)]
    [InlineData(JourneyStatus.Delayed, JourneyStatus.Terminated, true)]
    [InlineData(JourneyStatus.Interrupted, JourneyStatus.Running, true)]
    [InlineData(JourneyStatus.Interrupted, JourneyStatus.Delayed, true)]
    [InlineData(JourneyStatus.Interrupted, JourneyStatus.Completed, true)]
    [InlineData(JourneyStatus.Interrupted, JourneyStatus.Terminated, true)]
    [InlineData(JourneyStatus.Scheduled, JourneyStatus.Completed, false)]
    [InlineData(JourneyStatus.Running, JourneyStatus.Cancelled, false)]
    [InlineData(JourneyStatus.Delayed, JourneyStatus.Cancelled, false)]
    [InlineData(JourneyStatus.Interrupted, JourneyStatus.Cancelled, false)]
    public async Task Status_handler_enforces_the_complete_approved_lifecycle(JourneyStatus current,
        JourneyStatus next, bool allowed)
    {
        var existingUpdate = current != JourneyStatus.Scheduled;
        var repository = new FakeRepository(current, includeOfficialCurrent: existingUpdate);
        var result = await Create(repository, new FakeAuthorization()).PublishStatusAsync(StatusCommand(next) with
        {
            ExpectedJourneyRevision = existingUpdate ? 2 : 1
        });
        Assert.Equal(allowed ? JourneyPublicationOutcome.Published : JourneyPublicationOutcome.Invalid, result.Outcome);
        Assert.Equal(allowed ? 1 : 0, repository.AppendCount);
    }

    [Fact]
    public async Task Terminal_correction_must_reference_an_earlier_update_on_this_journey()
    {
        var repository = new FakeRepository(JourneyStatus.Cancelled, includeOfficialCurrent: true);
        var updateId = repository.State.Revision.UpdateId!.Value;
        var command = StatusCommand(JourneyStatus.Running) with
        {
            ExpectedJourneyRevision = 2,
            CorrectionOfUpdateId = updateId
        };
        var result = await Create(repository, new FakeAuthorization()).PublishStatusAsync(command);
        Assert.Equal(JourneyPublicationOutcome.Published, result.Outcome);
        Assert.Equal(JourneyPublicationKind.Correction, result.State!.Revision.PublicationKind);
        Assert.Equal(updateId, result.State.Revision.CorrectionOfUpdateId);
    }

    [Fact]
    public async Task Stop_update_copies_snapshot_and_changes_only_targeted_stop_fields()
    {
        var repository = new FakeRepository();
        var target = repository.State.Stops[0];
        var other = repository.State.Stops[1];
        var result = await Create(repository, new FakeAuthorization()).UpdateStopAsync(new(
            repository.State.Journey.JourneyInstanceId, target.JourneyStopId, 1, "verified-controller",
            "Platform 3", null, new DateTimeOffset(2026, 10, 17, 18, 5, 0, TimeSpan.Zero), null, null));

        Assert.Equal(JourneyPublicationOutcome.Published, result.Outcome);
        Assert.Equal("Platform 3", result.State!.Stops.Single(x => x.JourneyStopId == target.JourneyStopId).Platform);
        Assert.Null(result.State.Stops.Single(x => x.JourneyStopId == other.JourneyStopId).Platform);
        Assert.Equal(2, result.State.Stops.Count);
        Assert.Equal(JourneyPublicationKind.StopChange, result.State.Revision.PublicationKind);
    }

    [Fact]
    public async Task Denied_authorization_never_reads_or_writes_publication_state()
    {
        var repository = new FakeRepository();
        var auth = new FakeAuthorization { DenyAll = true };
        var result = await Create(repository, auth).PublishStatusAsync(StatusCommand(JourneyStatus.Delayed));
        Assert.Equal(JourneyPublicationOutcome.Unauthorized, result.Outcome);
        Assert.Equal(0, repository.ReadCount);
        Assert.Equal(0, repository.AppendCount);
    }

    private static JourneyPublicationHandler Create(FakeRepository repository, FakeAuthorization authorization,
        FakeVerifier? verifier = null) => new(repository, authorization, verifier ?? new FakeVerifier(DriverObservationVerification.Verified()),
        new FixedTimeProvider(new DateTimeOffset(2026, 10, 19, 4, 0, 0, TimeSpan.Zero)));

    private static PublishJourneyStatusCommand StatusCommand(JourneyStatus status) =>
        new(Guid.Parse("40000000-0000-0000-0000-000000000001"), 1, "verified-controller", status, null, null, null);

    private sealed class FakeAuthorization : IJourneyPublicationAuthorization
    {
        public List<PermissionAction> Actions { get; } = [];
        public bool DenyAll { get; init; }
        public bool DenyCancellation { get; init; }
        public Task<PublicationAuthorizationDecision> AuthorizeAsync(string verifiedSubject, Guid journeyInstanceId,
            PermissionAction action, CancellationToken cancellationToken = default)
        {
            Actions.Add(action);
            return Task.FromResult(new PublicationAuthorizationDecision(DenyAll || DenyCancellation && action == PermissionAction.ConfirmJourneyCancellation
                ? PublicationAuthorizationOutcome.Denied : PublicationAuthorizationOutcome.Allowed));
        }
    }

    private sealed class FakeVerifier(DriverObservationVerification result) : IDriverObservationVerifier
    {
        public Task<DriverObservationVerification> VerifyAsync(string verifiedActorSubject, Guid observationId,
            Guid journeyInstanceId, CancellationToken cancellationToken = default) => Task.FromResult(result);
    }

    private sealed class FakeRepository : IJourneyPublicationRepository
    {
        public JourneyPublicationState State { get; private set; }
        public int ReadCount { get; private set; }
        public int AppendCount { get; private set; }
        public FakeRepository(JourneyStatus status = JourneyStatus.Scheduled, bool includeOfficialCurrent = false)
        {
            var journeyId = Guid.Parse("40000000-0000-0000-0000-000000000001");
            var currentRevision = includeOfficialCurrent ? 2 : 1;
            var journey = new Journey(journeyId, Guid.Parse("30000000-0000-0000-0000-000000000001"),
                Guid.Parse("31000000-0000-0000-0000-000000000002"), 2, new DateOnly(2026, 10, 17),
                new TimeOnly(23, 20), "unit-test-feed", "journey-fixture-1", DateTimeOffset.Parse("2026-10-16T12:00:00Z"),
                "verified-test-actor", "manifest-rev-1", currentRevision);
            var stops = new[]
            {
                new JourneyRevisionStop(journey.JourneyInstanceId, currentRevision, Guid.Parse("41000000-0000-0000-0000-000000000001"), 1, Guid.Parse("10000000-0000-0000-0000-000000000001")),
                new JourneyRevisionStop(journey.JourneyInstanceId, currentRevision, Guid.Parse("41000000-0000-0000-0000-000000000002"), 2, Guid.Parse("10000000-0000-0000-0000-000000000002"))
            };
            var baseline = new JourneyRevision(journey.JourneyInstanceId, 1, JourneyStatus.Scheduled, DateTimeOffset.Parse("2026-10-17T12:00:00Z"), "ScheduledBaseline");
            var revision = includeOfficialCurrent
                ? new JourneyRevision(journey.JourneyInstanceId, 2, status, DateTimeOffset.Parse("2026-10-17T13:00:00Z"), "ControllerManual",
                    "status", Guid.Parse("42000000-0000-0000-0000-000000000001"), JourneyPublicationKind.Status, 1, "verified-controller")
                : baseline;
            State = new(journey, revision, stops, includeOfficialCurrent ? [baseline, revision] : [baseline]);
        }
        public Task<JourneyPublicationState?> FindCurrentAsync(Guid journeyInstanceId, CancellationToken cancellationToken = default)
        {
            ReadCount++;
            return Task.FromResult<JourneyPublicationState?>(journeyInstanceId == State.Journey.JourneyInstanceId ? State : null);
        }
        public Task<bool> HasEarlierUpdateAsync(Guid journeyInstanceId, Guid updateId, int beforeRevision, CancellationToken cancellationToken = default) =>
            Task.FromResult(State.History.Any(x => x.JourneyInstanceId == journeyInstanceId && x.Revision < beforeRevision && x.UpdateId == updateId));
        public Task<JourneyRevisionAppendResult> AppendAsync(Guid journeyInstanceId, int expectedRevision,
            JourneyRevision revision, IReadOnlyCollection<JourneyRevisionStop> stops, CancellationToken cancellationToken = default)
        {
            AppendCount++;
            var current = new Journey(journeyInstanceId, State.Journey.ServiceId, State.Journey.ScheduleId,
                State.Journey.ScheduleRevision, State.Journey.OperationDate, State.Journey.FirstDepartureOccurrence,
                State.Journey.SourceSystem, State.Journey.SourceInstanceKey, State.Journey.InitializedAt,
                State.Journey.InitializedBySubject, State.Journey.InitializationProvenance, revision.Revision);
            State = new(current, revision, stops.OrderBy(x => x.StopOrder).ToArray(), State.History.Append(revision).ToArray());
            return Task.FromResult(new JourneyRevisionAppendResult(JourneyRevisionAppendOutcome.Appended, revision.Revision));
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset time) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => time;
    }
}
