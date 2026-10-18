using SmartRail.Application.Identity.Authorization;
using SmartRail.Application.Identity.Maintenance;
using SmartRail.Application.Identity.Persistence;
using SmartRail.Domain.Identity;
using SmartRail.Domain.Identity.Entities;

namespace SmartRail.UnitTests.Identity;

public sealed class IdentityMaintenanceAuthorizationTests
{
    [Theory]
    [InlineData(MaintenanceAuthorityOutcome.Denied)]
    [InlineData(MaintenanceAuthorityOutcome.Unavailable)]
    public async Task Denied_or_unavailable_authority_makes_no_role_or_scope_writes(MaintenanceAuthorityOutcome outcome)
    {
        var repos = new RecordingRepositories();
        var authority = new FakeAuthority(outcome);
        var now = DateTimeOffset.UtcNow;
        var handlers = new object[]
        {
            new GrantRoleAssignmentHandler(repos, authority),
            new RevokeRoleAssignmentHandler(repos, authority),
            new GrantStationScopeHandler(repos, authority),
            new RevokeStationScopeHandler(repos, authority)
        };
        var results = new[]
        {
            await ((GrantRoleAssignmentHandler)handlers[0]).HandleAsync(new("trusted-actor", "target", RoleCode.Driver, now, Guid.NewGuid(), Guid.NewGuid())),
            await ((RevokeRoleAssignmentHandler)handlers[1]).HandleAsync(new("trusted-actor", Guid.NewGuid(), now, Guid.NewGuid())),
            await ((GrantStationScopeHandler)handlers[2]).HandleAsync(new("trusted-actor", "target", Guid.NewGuid(), now, Guid.NewGuid(), Guid.NewGuid())),
            await ((RevokeStationScopeHandler)handlers[3]).HandleAsync(new("trusted-actor", Guid.NewGuid(), now, Guid.NewGuid()))
        };

        Assert.All(results, result => Assert.Equal(MaintenanceCommandOutcome.Denied, result.Outcome));
        Assert.Equal(4, authority.Calls);
        Assert.Equal(0, repos.Writes);
    }

    [Fact]
    public async Task Missing_authority_defaults_to_denial_without_persistence_calls()
    {
        var repos = new RecordingRepositories();
        var result = await new GrantRoleAssignmentHandler(repos).HandleAsync(
            new("trusted-actor", "target", RoleCode.Driver, DateTimeOffset.UtcNow, Guid.NewGuid(), Guid.NewGuid()));
        Assert.Equal(MaintenanceCommandOutcome.Denied, result.Outcome);
        Assert.Equal(0, repos.Writes);
    }

    [Fact]
    public async Task Explicitly_authorized_role_grant_calls_repository()
    {
        var repos = new RecordingRepositories();
        var result = await new GrantRoleAssignmentHandler(repos, new FakeAuthority(MaintenanceAuthorityOutcome.Allowed))
            .HandleAsync(new("trusted-actor", "target", RoleCode.Driver, DateTimeOffset.UtcNow, Guid.NewGuid(), Guid.NewGuid()));
        Assert.Equal(MaintenanceCommandOutcome.Completed, result.Outcome);
        Assert.Equal(1, repos.Writes);
    }

    private sealed class FakeAuthority(MaintenanceAuthorityOutcome outcome) : IIdentityMaintenanceAuthority
    {
        public int Calls { get; private set; }
        public Task<MaintenanceAuthorityDecision> AuthorizeAsync(string verifiedActorSubject, MaintenanceAction action, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(new MaintenanceAuthorityDecision(outcome));
        }
    }

    private sealed class RecordingRepositories : IRoleAssignmentRepository, IStationScopeRepository
    {
        public int Writes { get; private set; }
        public Task<IReadOnlyList<RoleCode>> GetActiveRolesAsync(string verifiedCognitoSubject, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<RoleCode>>([]);
        Task<IReadOnlyList<RoleAssignment>> IRoleAssignmentRepository.GetActiveAssignmentsAsync(string verifiedCognitoSubject, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<RoleAssignment>>([]);
        public Task<IReadOnlyList<Guid>> GetActiveStationIdsAsync(string verifiedCognitoSubject, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Guid>>([]);
        Task<IReadOnlyList<StationScopeAssignment>> IStationScopeRepository.GetActiveAssignmentsAsync(string verifiedCognitoSubject, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<StationScopeAssignment>>([]);
        public Task GrantAsync(RoleAssignment assignment, RoleAssignmentAudit audit, CancellationToken cancellationToken = default) { Writes++; return Task.CompletedTask; }
        public Task<bool> RevokeAsync(Guid assignmentId, string verifiedActorSubject, DateTimeOffset occurredAtUtc, Guid auditId, string? evidenceReference = null, CancellationToken cancellationToken = default) { Writes++; return Task.FromResult(true); }
        public Task GrantAsync(StationScopeAssignment assignment, StationScopeAssignmentAudit audit, CancellationToken cancellationToken = default) { Writes++; return Task.CompletedTask; }
    }
}
