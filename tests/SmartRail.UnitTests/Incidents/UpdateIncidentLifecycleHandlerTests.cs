using SmartRail.Application.Identity.Authorization;
using SmartRail.Application.Incidents.Authorization;
using SmartRail.Application.Incidents.Persistence;
using SmartRail.Application.Incidents.UseCases;
using SmartRail.Domain.Identity;
using SmartRail.Domain.Incidents;
using SmartRail.Domain.Incidents.Entities;

namespace SmartRail.UnitTests.Incidents;

public sealed class UpdateIncidentLifecycleHandlerTests
{
    [Fact]
    public async Task Accepts_all_three_forward_transitions_and_appends_one_revision_each()
    {
        var repository = new FakeIncidentRepository();
        var handler = CreateHandler(repository);
        var incident = IncidentTestData.CreateIncident();
        repository.SetIncident(incident);

        var first = await handler.HandleAsync(Command(incident, 1, IncidentState.InProgress, "forward-1"));
        var second = await handler.HandleAsync(Command(incident, 2, IncidentState.Resolved, "forward-2"));
        var third = await handler.HandleAsync(Command(incident, 3, IncidentState.Closed, "forward-3"));

        Assert.Equal(UpdateIncidentOutcome.Updated, first.Outcome);
        Assert.Equal(UpdateIncidentOutcome.Updated, second.Outcome);
        Assert.Equal(UpdateIncidentOutcome.Updated, third.Outcome);
        Assert.Equal(IncidentState.Closed, incident.State);
        Assert.Equal(4, incident.Revision);
        Assert.Equal(3, repository.Revisions.Count);
        Assert.Equal(IncidentState.Reported, repository.Revisions[0].PreviousState);
        Assert.Equal(IncidentState.InProgress, repository.Revisions[0].State);
        Assert.Equal(IncidentState.InProgress, repository.Revisions[1].PreviousState);
        Assert.Equal(IncidentState.Closed, repository.Revisions[2].State);
    }

    [Theory]
    [InlineData(IncidentState.Reported, IncidentState.Resolved)]
    [InlineData(IncidentState.InProgress, IncidentState.Reported)]
    [InlineData(IncidentState.Resolved, IncidentState.InProgress)]
    [InlineData(IncidentState.Closed, IncidentState.Reported)]
    [InlineData(IncidentState.Closed, IncidentState.InProgress)]
    [InlineData(IncidentState.Closed, IncidentState.Resolved)]
    public async Task Rejects_illegal_reverse_or_terminal_transitions_without_mutation(IncidentState current, IncidentState next)
    {
        var repository = new FakeIncidentRepository();
        var incident = IncidentTestData.CreateIncident();
        if (current != IncidentState.Reported)
            incident.TransitionTo(IncidentState.InProgress, 1, IncidentTestData.Now.AddMinutes(1));
        if (current is IncidentState.Resolved or IncidentState.Closed)
            incident.TransitionTo(IncidentState.Resolved, 2, IncidentTestData.Now.AddMinutes(2));
        if (current == IncidentState.Closed)
            incident.TransitionTo(IncidentState.Closed, 3, IncidentTestData.Now.AddMinutes(3));
        repository.SetIncident(incident);
        var originalRevision = incident.Revision;
        var handler = CreateHandler(repository);

        var result = await handler.HandleAsync(Command(incident, incident.Revision, next, "illegal"));

        Assert.Equal(UpdateIncidentOutcome.Invalid, result.Outcome);
        Assert.Equal(current, incident.State);
        Assert.Equal(originalRevision, incident.Revision);
        Assert.Empty(repository.Revisions);
        Assert.Empty(repository.IdempotencyRecords);
    }

    [Fact]
    public async Task Rejects_same_state_noop_and_stale_revision()
    {
        var repository = new FakeIncidentRepository();
        var incident = IncidentTestData.CreateIncident();
        repository.SetIncident(incident);
        var handler = CreateHandler(repository);

        var noop = await handler.HandleAsync(Command(incident, 1, IncidentState.Reported, "noop"));
        var stale = await handler.HandleAsync(Command(incident, 2, IncidentState.InProgress, "stale"));

        Assert.Equal(UpdateIncidentOutcome.Invalid, noop.Outcome);
        Assert.Equal(UpdateIncidentOutcome.Conflict, stale.Outcome);
        Assert.Equal(IncidentState.Reported, incident.State);
        Assert.Equal(1, incident.Revision);
        Assert.Empty(repository.Revisions);
    }

    [Theory]
    [InlineData(AuthorizationOutcome.Denied, UpdateIncidentOutcome.Forbidden)]
    [InlineData(AuthorizationOutcome.Unavailable, UpdateIncidentOutcome.Unavailable)]
    public async Task Denied_or_unavailable_authority_never_writes(AuthorizationOutcome decision, UpdateIncidentOutcome expected)
    {
        var repository = new FakeIncidentRepository();
        var incident = IncidentTestData.CreateIncident();
        repository.SetIncident(incident);
        var handler = CreateHandler(repository, decision);

        var result = await handler.HandleAsync(Command(incident, 1, IncidentState.InProgress, "denied"));

        Assert.Equal(expected, result.Outcome);
        Assert.Equal(IncidentState.Reported, incident.State);
        Assert.Equal(1, incident.Revision);
        Assert.Empty(repository.Revisions);
        Assert.Empty(repository.IdempotencyRecords);
    }

    [Fact]
    public async Task Reauthorizes_before_returning_an_idempotent_replay()
    {
        var repository = new FakeIncidentRepository();
        var incident = IncidentTestData.CreateIncident();
        repository.SetIncident(incident);
        var allowed = CreateHandler(repository);
        Assert.Equal(UpdateIncidentOutcome.Updated,
            (await allowed.HandleAsync(Command(incident, 1, IncidentState.InProgress, "same-key"))).Outcome);

        var denied = CreateHandler(repository, AuthorizationOutcome.Denied);
        var replay = await denied.HandleAsync(Command(incident, 1, IncidentState.InProgress, "same-key"));

        Assert.Equal(UpdateIncidentOutcome.Forbidden, replay.Outcome);
        Assert.Single(repository.Revisions);
    }

    private static UpdateIncidentHandler CreateHandler(FakeIncidentRepository repository,
        AuthorizationOutcome outcome = AuthorizationOutcome.Allowed) => new(repository,
        new InlineUnitOfWork(), new FixedAccessEvaluator(outcome), new TestReferenceVerifier(), new TestReferenceVerifier(),
        new FixedTimeProvider(IncidentTestData.Now.AddHours(1)));

    private static UpdateIncidentCommand Command(Incident incident, int revision, IncidentState state, string key) =>
        new(incident.IncidentId, IncidentTestData.Subject, key, new UpdateIncidentInput(revision, state));

    private sealed class FixedAccessEvaluator(AuthorizationOutcome outcome) : IIncidentAccessEvaluator
    {
        public Task<AuthorizationDecision> AuthorizeAsync(string verifiedSubject, PermissionAction action, Guid? incidentId,
            IReadOnlyCollection<Guid> associatedStationIds, bool isCreation = false, CancellationToken cancellationToken = default) =>
            Task.FromResult(outcome switch
            {
                AuthorizationOutcome.Allowed => AuthorizationDecision.Allowed(),
                AuthorizationOutcome.Unavailable => AuthorizationDecision.Unavailable("test-unavailable"),
                _ => AuthorizationDecision.Denied("test-denied")
            });
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class TestReferenceVerifier : SmartRail.Application.Incidents.References.IFeature001IncidentReferenceVerifier,
        SmartRail.Application.Incidents.References.IFeature003IncidentReferenceVerifier
    {
        public Task<SmartRail.Application.Incidents.References.IncidentReferenceCheck> VerifyAsync(
            SmartRail.Application.Incidents.References.IncidentReference reference, CancellationToken cancellationToken = default) =>
            Task.FromResult(new SmartRail.Application.Incidents.References.IncidentReferenceCheck(
                SmartRail.Application.Incidents.References.IncidentReferenceOutcome.Verified));
    }

    private sealed class InlineUnitOfWork : IIncidentUnitOfWork
    {
        public Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default) => operation(cancellationToken);
    }

    private sealed class FakeIncidentRepository : IIncidentRepository
    {
        private readonly Dictionary<Guid, Incident> _incidents = [];
        public List<IncidentRevision> Revisions { get; } = [];
        public List<IncidentIdempotencyRecord> IdempotencyRecords { get; } = [];

        public void SetIncident(Incident incident) => _incidents[incident.IncidentId] = incident;
        public Task<IncidentRecord?> FindAsync(Guid incidentId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_incidents.TryGetValue(incidentId, out var incident)
                ? new IncidentRecord(incident, [new IncidentAssociation(Guid.NewGuid(), incidentId,
                    SmartRail.Domain.Incidents.IncidentAssociationKind.Station, Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
                    IncidentTestData.Now, 1)])
                : null);
        public Task<IReadOnlyList<IncidentRecord>> ListAsync(IncidentFilters filters, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<IncidentRecord>>([]);
        public Task<IncidentIdempotencyRecord?> FindIdempotencyAsync(IdempotencyLookup lookup, CancellationToken cancellationToken = default) =>
            Task.FromResult(IdempotencyRecords.SingleOrDefault(x => x.ActorSubject == lookup.ActorSubject && x.Operation == lookup.Operation && x.IdempotencyKey == lookup.Key));
        public Task CreateAsync(Incident incident, IncidentRevision revision, IReadOnlyCollection<IncidentAssociation> associations,
            IncidentIdempotencyRecord idempotencyRecord, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<bool> UpdateAsync(Incident incident, IncidentRevision revision, IReadOnlyCollection<IncidentAssociation> addAssociations,
            IReadOnlyCollection<IncidentAssociation> removeAssociations, IncidentIdempotencyRecord idempotencyRecord,
            int expectedRevision, CancellationToken cancellationToken = default)
        {
            if (!_incidents.TryGetValue(incident.IncidentId, out var current) || current.Revision != expectedRevision + 1)
                return Task.FromResult(false);
            Revisions.Add(revision);
            IdempotencyRecords.Add(idempotencyRecord);
            return Task.FromResult(true);
        }
    }
}
