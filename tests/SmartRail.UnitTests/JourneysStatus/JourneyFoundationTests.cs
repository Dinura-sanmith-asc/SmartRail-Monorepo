using SmartRail.Domain.JourneysStatus;
using SmartRail.Domain.JourneysStatus.Entities;
using SmartRail.Domain.JourneysStatus.Rules;
using SmartRail.Application.JourneysStatus.Initialization;
using SmartRail.Application.JourneysStatus.Persistence;

namespace SmartRail.UnitTests.JourneysStatus;

public sealed class JourneyFoundationTests
{
    [Theory]
    [InlineData(JourneyStatus.Scheduled, JourneyStatus.Running, true)]
    [InlineData(JourneyStatus.Scheduled, JourneyStatus.Cancelled, true)]
    [InlineData(JourneyStatus.Running, JourneyStatus.Completed, true)]
    [InlineData(JourneyStatus.Delayed, JourneyStatus.Running, true)]
    [InlineData(JourneyStatus.Interrupted, JourneyStatus.Terminated, true)]
    [InlineData(JourneyStatus.Cancelled, JourneyStatus.Running, false)]
    [InlineData(JourneyStatus.Completed, JourneyStatus.Delayed, false)]
    [InlineData(JourneyStatus.Terminated, JourneyStatus.Running, false)]
    public void Lifecycle_enforces_approved_transitions(JourneyStatus current, JourneyStatus next, bool allowed) =>
        Assert.Equal(allowed, JourneyLifecycle.CanTransition(current, next));

    [Fact]
    public void Terminal_state_can_only_be_corrected_through_linked_correction()
    {
        Assert.False(JourneyLifecycle.CanTransition(JourneyStatus.Cancelled, JourneyStatus.Running));
        Assert.True(JourneyLifecycle.CanTransition(JourneyStatus.Cancelled, JourneyStatus.Running, isLinkedCorrection: true));
    }

    [Fact]
    public void Journey_identity_is_independent_of_schedule_composite()
    {
        var service = Guid.NewGuid(); var schedule = Guid.NewGuid(); var date = new DateOnly(2026, 10, 17);
        var first = NewJourney(Guid.NewGuid(), service, schedule, date, "source-a");
        var second = NewJourney(Guid.NewGuid(), service, schedule, date, "source-b");
        Assert.NotEqual(first.JourneyInstanceId, second.JourneyInstanceId);
        Assert.Equal(first.ScheduleId, second.ScheduleId);
        Assert.Equal(first.OperationDate, second.OperationDate);
        Assert.Equal(first.FirstDepartureOccurrence, second.FirstDepartureOccurrence);
    }

    [Fact]
    public void Initialization_replay_compares_source_key_and_linkage_without_collapsing_distinct_ids()
    {
        Assert.True(JourneyInitializationRules.IsIdempotentReplay("feed", "instance-1", "feed", "instance-1",
            Guid.NewGuid(), Guid.NewGuid(), Guid.Parse("30000000-0000-0000-0000-000000000001"), Guid.Parse("30000000-0000-0000-0000-000000000001"),
            Guid.Parse("31000000-0000-0000-0000-000000000001"), Guid.Parse("31000000-0000-0000-0000-000000000001"),
            1, 1, new(2026, 10, 17), new(2026, 10, 17), new(23, 20), new(23, 20), hasAuthoritativeRequestedJourneyId: false));
        Assert.False(JourneyInitializationRules.IsIdempotentReplay("feed", "instance-1", "feed", "instance-1",
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            1, 1, new(2026, 10, 17), new(2026, 10, 17), new(23, 20), new(23, 20), hasAuthoritativeRequestedJourneyId: true));
    }

    [Fact]
    public void Diversion_rejects_duplicate_and_foreign_retained_stop_ids()
    {
        var retained = Guid.NewGuid();
        Assert.Throws<ArgumentException>(() => JourneyDiversionRules.Validate(
            [new(retained, 1), new(retained, 2)], new HashSet<Guid> { retained }));
        Assert.Throws<ArgumentException>(() => JourneyDiversionRules.Validate(
            [new(Guid.NewGuid(), 1)], new HashSet<Guid> { retained }));
    }

    [Fact]
    public void Revision_requires_update_id_for_official_publication_and_baseline_has_none()
    {
        var journeyId = Guid.NewGuid();
        _ = new JourneyRevision(journeyId, 1, JourneyStatus.Scheduled, DateTimeOffset.UtcNow, "ScheduledBaseline");
        Assert.Throws<ArgumentException>(() => new JourneyRevision(journeyId, 2, JourneyStatus.Delayed,
            DateTimeOffset.UtcNow, "ControllerManual", publicationKind: JourneyPublicationKind.Status, previousRevision: 1,
            publishedBySubject: "verified-subject"));
        var update = Guid.NewGuid();
        Assert.Equal(update, new JourneyRevision(journeyId, 2, JourneyStatus.Delayed, DateTimeOffset.UtcNow,
            "ControllerManual", "status", update, JourneyPublicationKind.Status, 1, "verified-subject").UpdateId);
    }

    [Fact]
    public async Task Deny_all_initializer_is_default_and_denied_command_never_writes()
    {
        var db = new FakeInitializer();
        var deniedAuthority = new DenyAuthority();
        var handler = new InitializeJourneyHandler(deniedAuthority, new TestIdIssuer(),
            new TestManifestValidator(), new FakeScheduleVerifier(), db, TimeProvider.System);
        var result = await handler.HandleAsync(Command());
        Assert.Equal(JourneyInitializationOutcome.Denied, result.Outcome);
        Assert.Equal(0, db.WriteCount);
        Assert.False((await deniedAuthority.AuthorizeAsync(Command(), default)).IsAuthorized);
    }

    [Fact]
    public async Task Authorized_initializer_uses_server_time_and_atomic_repository_port()
    {
        var db = new FakeInitializer();
        var time = new FrozenTimeProvider(new DateTimeOffset(2026, 10, 19, 4, 0, 0, TimeSpan.Zero));
        var command = Command();
        var handler = new InitializeJourneyHandler(new AllowAuthority(), new TestIdIssuer(),
            new TestManifestValidator(), new FakeScheduleVerifier(), db, time);
        var result = await handler.HandleAsync(command);
        Assert.Equal(JourneyInitializationOutcome.Created, result.Outcome);
        Assert.Equal(1, db.WriteCount);
        Assert.Equal(time.GetUtcNow(), db.LastRecord!.Journey.InitializedAt);
        Assert.Equal(time.GetUtcNow(), db.LastRecord.AcceptedAudit.OccurredAt);
        Assert.Null(db.LastRecord.Baseline.UpdateId);
        Assert.Equal(2, db.LastRecord.Stops.Count);
    }

    [Fact]
    public async Task Schedule_evidence_unavailable_is_not_reported_as_invalid_input()
    {
        var db = new FakeInitializer();
        var handler = new InitializeJourneyHandler(new AllowAuthority(), new TestIdIssuer(),
            new TestManifestValidator(), new FakeScheduleVerifier(unavailable: true), db, TimeProvider.System);
        Assert.Equal(JourneyInitializationOutcome.Unavailable, (await handler.HandleAsync(Command())).Outcome);
        Assert.Equal(0, db.WriteCount);
    }

    private static InitializeJourneyCommand Command() => new(null,
        Guid.Parse("30000000-0000-0000-0000-000000000001"), Guid.Parse("31000000-0000-0000-0000-000000000002"), 2,
        new DateOnly(2026, 10, 17), new TimeOnly(23, 20), "test-feed", "instance-1", "manifest-rev-1");

    private static Journey NewJourney(Guid id, Guid service, Guid schedule, DateOnly date, string source) =>
        new(id, service, schedule, 1, date, new TimeOnly(23, 20), source, "instance", DateTimeOffset.UtcNow,
            "verified-subject", "manifest");

    private sealed class AllowAuthority : IJourneyInitializationAuthority
    {
        public Task<JourneyInitializationAuthorityResult> AuthorizeAsync(InitializeJourneyCommand command, CancellationToken cancellationToken) =>
            Task.FromResult(new JourneyInitializationAuthorityResult(JourneyInitializationAuthorityStatus.Authorized,
                "verified-test-actor", command.SourceSystem));
    }

    private sealed class DenyAuthority : IJourneyInitializationAuthority
    {
        public Task<JourneyInitializationAuthorityResult> AuthorizeAsync(InitializeJourneyCommand command, CancellationToken cancellationToken) =>
            Task.FromResult(new JourneyInitializationAuthorityResult(JourneyInitializationAuthorityStatus.Denied, null, null));
    }

    private sealed class TestIdIssuer : IJourneyInstanceIdIssuer
    {
        public Guid Issue(string sourceSystem, string sourceInstanceKey, Guid? authoritativeId) => authoritativeId ?? Guid.NewGuid();
    }

    private sealed class TestManifestValidator : IJourneyManifestValidator
    {
        public bool IsValid(string? sourceSystem, string? sourceInstanceKey, string? provenance) =>
            !string.IsNullOrWhiteSpace(sourceSystem) && !string.IsNullOrWhiteSpace(sourceInstanceKey) && !string.IsNullOrWhiteSpace(provenance);
    }

    private sealed class FakeScheduleVerifier(bool unavailable = false) : IJourneyScheduleLinkVerifier
    {
        public Task<JourneyScheduleLinkResult> VerifyAsync(JourneyScheduleLinkRequest request, CancellationToken cancellationToken)
        {
            if (unavailable) return Task.FromResult(new JourneyScheduleLinkResult(ScheduleLinkVerificationStatus.Unavailable, null));
            var firstStation = Guid.Parse("10000000-0000-0000-0000-000000000001");
            var secondStation = Guid.Parse("10000000-0000-0000-0000-000000000002");
            var stops = new[] { new JourneyScheduleStop(Guid.NewGuid(), firstStation, Guid.NewGuid(), 1, null, null),
                new JourneyScheduleStop(Guid.NewGuid(), secondStation, Guid.NewGuid(), 2, null, null) };
            return Task.FromResult(new JourneyScheduleLinkResult(ScheduleLinkVerificationStatus.Verified,
                new JourneyScheduleLink(stops, DateTimeOffset.MinValue)));
        }
    }

    private sealed class FakeInitializer : IJourneyInitializationRepository
    {
        public int WriteCount { get; private set; }
        public JourneyInitializationRecord? LastRecord { get; private set; }
        public Task<JourneyInitializationStoreResult> InitializeAsync(JourneyInitializationRecord record, CancellationToken cancellationToken)
        {
            WriteCount++; LastRecord = record;
            return Task.FromResult(new JourneyInitializationStoreResult(JourneyInitializationStoreOutcome.Created, record.Journey));
        }
        public Task AppendConflictAuditAsync(JourneyInitializationAudit audit, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FrozenTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
