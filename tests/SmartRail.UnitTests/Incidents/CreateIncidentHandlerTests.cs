using SmartRail.Application.Incidents.Authorization;
using SmartRail.Application.Incidents.Categories;
using SmartRail.Application.Identity.Authorization;
using SmartRail.Application.Incidents.Persistence;
using SmartRail.Application.Incidents.References;
using SmartRail.Application.Incidents.UseCases;
using SmartRail.Domain.Identity;
using SmartRail.Domain.Incidents.Entities;

namespace SmartRail.UnitTests.Incidents;

public sealed class CreateIncidentHandlerTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 10, 20, 10, 30, 0, TimeSpan.Zero);

    [Fact]
    public async Task Creates_one_revision_and_replays_original_projection()
    {
        var dependencies = new TestDependencies();
        var command = ValidCommand();

        var created = await dependencies.Handler.HandleAsync(command);
        var replay = await dependencies.Handler.HandleAsync(command);

        Assert.Equal(CreateIncidentOutcome.Created, created.Outcome);
        Assert.Equal(CreateIncidentOutcome.Replayed, replay.Outcome);
        Assert.Equal(created.Incident!.IncidentId, replay.Incident!.IncidentId);
        Assert.Equal(created.Incident.ReportedAt, replay.Incident.ReportedAt);
        Assert.Equal(created.Incident.Associations.ToArray(), replay.Incident.Associations.ToArray());
        Assert.Single(dependencies.Repository.Incidents);
        Assert.Equal(2, dependencies.Access.Calls);
    }

    [Fact]
    public async Task Conflicting_payload_with_same_key_returns_conflict_after_authorization()
    {
        var dependencies = new TestDependencies();
        await dependencies.Handler.HandleAsync(ValidCommand());
        var conflicting = ValidCommand() with
        {
            Input = ValidInput() with { Description = "Different report" }
        };

        var result = await dependencies.Handler.HandleAsync(conflicting);

        Assert.Equal(CreateIncidentOutcome.Conflict, result.Outcome);
        Assert.Equal(2, dependencies.Access.Calls);
        Assert.Single(dependencies.Repository.Incidents);
    }

    [Theory]
    [InlineData(AuthorizationOutcome.Denied, CreateIncidentOutcome.Forbidden)]
    [InlineData(AuthorizationOutcome.Unavailable, CreateIncidentOutcome.Unavailable)]
    public async Task Denied_or_unavailable_authority_never_writes(AuthorizationOutcome authority, CreateIncidentOutcome expected)
    {
        var dependencies = new TestDependencies();
        dependencies.Access.Decision = new AuthorizationDecision(authority, "test");

        var result = await dependencies.Handler.HandleAsync(ValidCommand());

        Assert.Equal(expected, result.Outcome);
        Assert.Empty(dependencies.Repository.Incidents);
        Assert.Empty(dependencies.Repository.Idempotency);
    }

    [Fact]
    public async Task Current_authorization_is_rechecked_before_idempotent_replay()
    {
        var dependencies = new TestDependencies();
        var command = ValidCommand();
        Assert.Equal(CreateIncidentOutcome.Created, (await dependencies.Handler.HandleAsync(command)).Outcome);
        dependencies.Access.Decision = new AuthorizationDecision(AuthorizationOutcome.Denied, "revoked");

        var result = await dependencies.Handler.HandleAsync(command);

        Assert.Equal(CreateIncidentOutcome.Forbidden, result.Outcome);
        Assert.Single(dependencies.Repository.Incidents);
    }

    [Fact]
    public async Task Unavailable_category_or_owner_authority_never_writes()
    {
        var categories = new TestDependencies();
        categories.Category = CategoryCheck.Unavailable();
        Assert.Equal(CreateIncidentOutcome.Unavailable, (await categories.Handler.HandleAsync(ValidCommand())).Outcome);
        Assert.Empty(categories.Repository.Incidents);

        var owner = new TestDependencies();
        owner.Feature001.Result = new(IncidentReferenceOutcome.Unavailable);
        Assert.Equal(CreateIncidentOutcome.Unavailable, (await owner.Handler.HandleAsync(ValidCommand())).Outcome);
        Assert.Empty(owner.Repository.Incidents);
    }

    [Fact]
    public async Task Production_like_unconfigured_category_catalog_fails_closed()
    {
        var dependencies = new TestDependencies { Category = CategoryCheck.Unavailable() };

        var result = await dependencies.Handler.HandleAsync(ValidCommand());

        Assert.Equal(CreateIncidentOutcome.Unavailable, result.Outcome);
        Assert.Empty(dependencies.Repository.Incidents);
    }

    private static CreateIncidentCommand ValidCommand() => new("verified-subject", "create-incident-key", ValidInput());
    private static CreateIncidentInput ValidInput() => new(IncidentTestData.TestCategory, "Track obstruction reported", null,
        [new(IncidentReferenceKind.Station, Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"))]);

    private sealed class TestDependencies
    {
        public MemoryRepository Repository { get; } = new();
        public TestAccessEvaluator Access { get; } = new();
        public TestCategoryCatalog CategoryCatalog { get; }
        public TestReferenceVerifier Feature001 { get; } = new();
        private readonly TestReferenceVerifier _feature003 = new();
        public CategoryCheck Category { get => CategoryCatalog.Result; set => CategoryCatalog.Result = value; }
        public CreateIncidentHandler Handler { get; }

        public TestDependencies()
        {
            CategoryCatalog = new TestCategoryCatalog();
            Handler = new CreateIncidentHandler(Repository, new DirectUnitOfWork(), Access, CategoryCatalog,
                Feature001, _feature003, new FixedTimeProvider(FixedNow));
        }
    }

    private sealed class TestAccessEvaluator : IIncidentAccessEvaluator
    {
        public int Calls { get; private set; }
        public AuthorizationDecision Decision { get; set; } = AuthorizationDecision.Allowed();
        public Task<AuthorizationDecision> AuthorizeAsync(string verifiedSubject, PermissionAction action,
            Guid? incidentId, IReadOnlyCollection<Guid> associatedStationIds, bool isCreation = false,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            Assert.Equal(PermissionAction.CreateIncident, action);
            Assert.True(isCreation);
            Assert.Equal("verified-subject", verifiedSubject);
            return Task.FromResult(Decision);
        }
    }

    private sealed class TestCategoryCatalog : IIncidentCategoryCatalog
    {
        public CategoryCheck Result { get; set; } = CategoryCheck.Approved();
        public Task<CategoryCheck> CheckAsync(string categoryCode, CancellationToken cancellationToken = default) => Task.FromResult(Result);
    }

    private sealed class TestReferenceVerifier : IFeature001IncidentReferenceVerifier, IFeature003IncidentReferenceVerifier
    {
        public IncidentReferenceCheck Result { get; set; } = new(IncidentReferenceOutcome.Verified);
        public Task<IncidentReferenceCheck> VerifyAsync(IncidentReference reference, CancellationToken cancellationToken = default) => Task.FromResult(Result);
    }

    private sealed class DirectUnitOfWork : IIncidentUnitOfWork
    {
        public Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default) => operation(cancellationToken);
    }

    private sealed class MemoryRepository : IIncidentRepository
    {
        public List<Incident> Incidents { get; } = [];
        public List<IncidentIdempotencyRecord> Idempotency { get; } = [];
        public Task<IncidentRecord?> FindAsync(Guid incidentId, CancellationToken cancellationToken = default) => Task.FromResult<IncidentRecord?>(null);
        public Task<IReadOnlyList<IncidentRecord>> ListAsync(IncidentFilters filters, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<IncidentRecord>>([]);
        public Task<IncidentIdempotencyRecord?> FindIdempotencyAsync(IdempotencyLookup lookup, CancellationToken cancellationToken = default) =>
            Task.FromResult(Idempotency.SingleOrDefault(x => x.ActorSubject == lookup.ActorSubject && x.Operation == lookup.Operation && x.IdempotencyKey == lookup.Key));
        public Task CreateAsync(Incident incident, IncidentRevision revision, IReadOnlyCollection<IncidentAssociation> associations,
            IncidentIdempotencyRecord idempotencyRecord, CancellationToken cancellationToken = default)
        {
            Incidents.Add(incident);
            Idempotency.Add(idempotencyRecord);
            return Task.CompletedTask;
        }
        public Task<bool> UpdateAsync(Incident incident, IncidentRevision revision, IReadOnlyCollection<IncidentAssociation> addAssociations,
            IReadOnlyCollection<IncidentAssociation> removeAssociations, IncidentIdempotencyRecord idempotencyRecord,
            int expectedRevision, CancellationToken cancellationToken = default) => Task.FromResult(false);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
