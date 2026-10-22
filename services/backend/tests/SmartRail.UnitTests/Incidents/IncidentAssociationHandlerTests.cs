using SmartRail.Application.Identity.Authorization;
using SmartRail.Application.Incidents.Authorization;
using SmartRail.Application.Incidents.Persistence;
using SmartRail.Application.Incidents.References;
using SmartRail.Application.Incidents.UseCases;
using SmartRail.Domain.Identity;
using SmartRail.Domain.Incidents;
using SmartRail.Domain.Incidents.Entities;

namespace SmartRail.UnitTests.Incidents;

public sealed class IncidentAssociationHandlerTests
{
    private static readonly Guid StationA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid StationB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid ServiceA = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    [Fact]
    public void Rejects_malformed_duplicate_and_incomplete_disruption_inputs()
    {
        Assert.False(UpdateIncidentInputValidator.IsValid(new(1, null,
            [new((IncidentReferenceKind)99, StationB)], [])));
        Assert.False(UpdateIncidentInputValidator.IsValid(new(1, null,
            [new(IncidentReferenceKind.Station, StationB), new(IncidentReferenceKind.Station, StationB)], [])));
        Assert.False(UpdateIncidentInputValidator.IsValid(new(1, null,
            [new(IncidentReferenceKind.Disruption, Guid.NewGuid(), Guid.NewGuid(), null)], [])));
        Assert.False(UpdateIncidentInputValidator.IsValid(new(1, null,
            [new(IncidentReferenceKind.Service, ServiceA, Guid.NewGuid(), 2)], [])));
        Assert.True(UpdateIncidentInputValidator.IsValid(new(1, null,
            [new(IncidentReferenceKind.Disruption, Guid.NewGuid(), Guid.NewGuid(), 2)], [])));
    }

    [Fact]
    public async Task Adds_verified_station_with_final_all_station_scope_and_audit_revision()
    {
        var deps = new Dependencies();
        deps.Repository.Add(IncidentTestData.CreateIncident(), StationA);

        var result = await deps.Handler.HandleAsync(Command(deps.Repository.Incident!.IncidentId,
            new(1, null, [new(IncidentReferenceKind.Station, StationB)], []), "add-station"));

        Assert.Equal(UpdateIncidentOutcome.Updated, result.Outcome);
        Assert.Contains(StationA, deps.Access.LastStations);
        Assert.Contains(StationB, deps.Access.LastStations);
        Assert.Equal(2, deps.Repository.LastRevision?.Revision);
        Assert.Null(deps.Repository.LastRevision?.PreviousState);
        Assert.Single(deps.Repository.Added);
        Assert.Equal(2, deps.Repository.Added[0].AddedRevision);
        Assert.Equal(2, result.Incident?.Revision);
        Assert.Equal(2, result.Incident?.Associations.Count);
    }

    [Fact]
    public async Task Does_not_persist_foreign_or_inactive_references()
    {
        var deps = new Dependencies();
        deps.Repository.Add(IncidentTestData.CreateIncident(), StationA);
        deps.Feature001.Result = new(IncidentReferenceOutcome.StaleOrInactive);

        var result = await deps.Handler.HandleAsync(Command(deps.Repository.Incident!.IncidentId,
            new(1, null, [new(IncidentReferenceKind.Service, ServiceA)], []), "inactive-service"));

        Assert.Equal(UpdateIncidentOutcome.Conflict, result.Outcome);
        Assert.Equal(0, deps.Repository.UpdateCalls);
        Assert.Equal(1, deps.Repository.Incident!.Revision);
    }

    [Theory]
    [InlineData(IncidentReferenceOutcome.MissingOrInaccessible, UpdateIncidentOutcome.NotFound)]
    [InlineData(IncidentReferenceOutcome.Unavailable, UpdateIncidentOutcome.Unavailable)]
    public async Task Does_not_persist_missing_or_unverifiable_owner_references(IncidentReferenceOutcome ownerResult,
        UpdateIncidentOutcome expected)
    {
        var deps = new Dependencies();
        deps.Repository.Add(IncidentTestData.CreateIncident(), StationA);
        deps.Feature001.Result = new(ownerResult);

        var result = await deps.Handler.HandleAsync(Command(deps.Repository.Incident!.IncidentId,
            new(1, null, [new(IncidentReferenceKind.Service, ServiceA)], []), "owner-result"));

        Assert.Equal(expected, result.Outcome);
        Assert.Equal(0, deps.Repository.UpdateCalls);
        Assert.Equal(1, deps.Repository.Incident!.Revision);
    }

    [Fact]
    public async Task Rejects_duplicate_active_and_foreign_removal_ids_without_writes()
    {
        var deps = new Dependencies();
        deps.Repository.Add(IncidentTestData.CreateIncident(), StationA);
        var duplicate = await deps.Handler.HandleAsync(Command(deps.Repository.Incident!.IncidentId,
            new(1, null, [new(IncidentReferenceKind.Station, StationA)], []), "duplicate"));
        var foreignRemoval = await deps.Handler.HandleAsync(Command(deps.Repository.Incident.IncidentId,
            new(1, null, null, [Guid.NewGuid()]), "foreign-remove"));

        Assert.Equal(UpdateIncidentOutcome.Invalid, duplicate.Outcome);
        Assert.Equal(UpdateIncidentOutcome.NotFound, foreignRemoval.Outcome);
        Assert.Equal(0, deps.Repository.UpdateCalls);
    }

    [Fact]
    public async Task Rejects_removing_and_readding_the_same_association_in_one_revision()
    {
        var deps = new Dependencies();
        deps.Repository.Add(IncidentTestData.CreateIncident(), StationA);
        deps.Repository.AddAssociation(IncidentAssociationKind.Service, ServiceA);
        var stationAssociationId = deps.Repository.Associations.Single(x => x.Kind == IncidentAssociationKind.Station).AssociationId;

        var result = await deps.Handler.HandleAsync(Command(deps.Repository.Incident!.IncidentId,
            new(1, null, [new(IncidentReferenceKind.Station, StationA)], [stationAssociationId]), "replace-same-link"));

        Assert.Equal(UpdateIncidentOutcome.Invalid, result.Outcome);
        Assert.Equal(0, deps.Repository.UpdateCalls);
    }

    [Fact]
    public async Task Removes_station_only_when_another_valid_station_or_service_anchor_remains()
    {
        var deps = new Dependencies();
        deps.Repository.Add(IncidentTestData.CreateIncident(), StationA);
        deps.Repository.AddAssociation(IncidentAssociationKind.Service, ServiceA);
        var stationAssociationId = deps.Repository.Associations.Single(x => x.Kind == IncidentAssociationKind.Station).AssociationId;

        var result = await deps.Handler.HandleAsync(Command(deps.Repository.Incident!.IncidentId,
            new(1, null, null, [stationAssociationId]), "remove-station"));

        Assert.Equal(UpdateIncidentOutcome.Updated, result.Outcome);
        Assert.Equal(2, deps.Repository.Removed.Single().RemovedRevision);
        Assert.Single(result.Incident!.Associations);
        Assert.Equal("Service", result.Incident.Associations[0].Kind);
    }

    [Fact]
    public async Task Reauthorizes_and_replays_association_removal_after_the_link_is_no_longer_active()
    {
        var deps = new Dependencies();
        deps.Repository.Add(IncidentTestData.CreateIncident(), StationA);
        deps.Repository.AddAssociation(IncidentAssociationKind.Station, StationB);
        deps.Repository.AddAssociation(IncidentAssociationKind.Service, ServiceA);
        var removedId = deps.Repository.Associations.Single(x => x.ResourceId == StationB).AssociationId;
        var request = new UpdateIncidentInput(1, null, null, [removedId]);
        var incidentId = deps.Repository.Incident!.IncidentId;

        var first = await deps.Handler.HandleAsync(Command(incidentId, request, "remove-retry"));
        var replay = await deps.Handler.HandleAsync(Command(incidentId, request, "remove-retry"));

        Assert.Equal(UpdateIncidentOutcome.Updated, first.Outcome);
        Assert.Equal(UpdateIncidentOutcome.Replayed, replay.Outcome);
        Assert.Equal(2, replay.Incident?.Revision);
        Assert.Equal(1, deps.Repository.UpdateCalls);
        Assert.Equal(2, deps.Access.Calls);
    }

    private static UpdateIncidentCommand Command(Guid id, UpdateIncidentInput input, string key) =>
        new(id, IncidentTestData.Subject, key, input);

    private sealed class Dependencies
    {
        public FakeRepository Repository { get; } = new();
        public FakeAccess Access { get; } = new();
        public FakeVerifier Feature001 { get; } = new();
        private readonly FakeVerifier _feature003 = new();
        public UpdateIncidentHandler Handler { get; }
        public Dependencies() => Handler = new(Repository, new InlineUnitOfWork(), Access, Feature001, _feature003,
            new FixedTimeProvider(IncidentTestData.Now.AddHours(1)));
    }

    private sealed class FakeAccess : IIncidentAccessEvaluator
    {
        public int Calls { get; private set; }
        public IReadOnlyCollection<Guid> LastStations { get; private set; } = [];
        public Task<AuthorizationDecision> AuthorizeAsync(string verifiedSubject, PermissionAction action, Guid? incidentId,
            IReadOnlyCollection<Guid> associatedStationIds, bool isCreation = false, CancellationToken cancellationToken = default)
        {
            Calls++;
            LastStations = associatedStationIds.ToArray();
            return Task.FromResult(AuthorizationDecision.Allowed());
        }
    }

    private sealed class FakeVerifier : IFeature001IncidentReferenceVerifier, IFeature003IncidentReferenceVerifier
    {
        public IncidentReferenceCheck Result { get; set; } = new(IncidentReferenceOutcome.Verified);
        public Task<IncidentReferenceCheck> VerifyAsync(IncidentReference reference, CancellationToken cancellationToken = default) => Task.FromResult(Result);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class InlineUnitOfWork : IIncidentUnitOfWork
    {
        public Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default) => operation(cancellationToken);
    }

    private sealed class FakeRepository : IIncidentRepository
    {
        public Incident? Incident { get; private set; }
        public List<IncidentAssociation> Associations { get; } = [];
        public List<IncidentAssociation> Added { get; } = [];
        public List<IncidentAssociation> Removed { get; } = [];
        public List<IncidentIdempotencyRecord> Idempotency { get; } = [];
        public IncidentRevision? LastRevision { get; private set; }
        public int UpdateCalls { get; private set; }
        public void Add(Incident incident, Guid stationId)
        {
            Incident = incident;
            Associations.Add(new(Guid.NewGuid(), incident.IncidentId, IncidentAssociationKind.Station, stationId, IncidentTestData.Now, 1));
        }
        public void AddAssociation(IncidentAssociationKind kind, Guid resourceId) =>
            Associations.Add(new(Guid.NewGuid(), Incident!.IncidentId, kind, resourceId, IncidentTestData.Now, 1));
        public Task<IncidentRecord?> FindAsync(Guid incidentId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Incident?.IncidentId == incidentId ? new IncidentRecord(Incident, Associations.Where(x => x.IsActive).ToArray()) : null);
        public Task<IReadOnlyList<IncidentRecord>> ListAsync(IncidentFilters filters, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<IncidentRecord>>([]);
        public Task<IncidentIdempotencyRecord?> FindIdempotencyAsync(IdempotencyLookup lookup, CancellationToken cancellationToken = default) =>
            Task.FromResult(Idempotency.SingleOrDefault(x => x.ActorSubject == lookup.ActorSubject && x.Operation == lookup.Operation && x.IdempotencyKey == lookup.Key));
        public Task CreateAsync(Incident incident, IncidentRevision revision, IReadOnlyCollection<IncidentAssociation> associations, IncidentIdempotencyRecord idempotencyRecord, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<bool> UpdateAsync(Incident incident, IncidentRevision revision, IReadOnlyCollection<IncidentAssociation> addAssociations,
            IReadOnlyCollection<IncidentAssociation> removeAssociations, IncidentIdempotencyRecord idempotencyRecord, int expectedRevision, CancellationToken cancellationToken = default)
        {
            UpdateCalls++;
            LastRevision = revision;
            Added.AddRange(addAssociations);
            Removed.AddRange(removeAssociations);
            Associations.AddRange(addAssociations);
            Idempotency.Add(idempotencyRecord);
            return Task.FromResult(true);
        }
    }
}
