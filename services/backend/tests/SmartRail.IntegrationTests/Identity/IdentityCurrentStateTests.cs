using Microsoft.EntityFrameworkCore;
using SmartRail.Application.Identity.Persistence;
using SmartRail.Domain.Identity;
using SmartRail.Domain.Identity.Entities;
using SmartRail.Domain.RailwayNetwork.Entities;
using SmartRail.Infrastructure.Identity.Persistence;
using SmartRail.Infrastructure.RailwayNetwork;
using SmartRail.IntegrationTests.RailwayNetwork;

namespace SmartRail.IntegrationTests.Identity;

public sealed class IdentityCurrentStateTests(PostgreSqlTestDatabaseFixture fixture) : IClassFixture<PostgreSqlTestDatabaseFixture>
{
    [Fact]
    public async Task Current_lookups_observe_role_and_scope_revocation_on_the_next_query()
    {
        await using var db = CreateContext();
        await db.Database.MigrateAsync();
        var stationId = Guid.NewGuid();
        db.Stations.Add(new Station(stationId, "CURRENT-TEST", "Current Test"));
        await db.SaveChangesAsync();
        IRoleAssignmentRepository roles = new RoleAssignmentRepository(db);
        IStationScopeRepository scopes = new StationScopeRepository(db);
        var role = new RoleAssignment(Guid.NewGuid(), "subject-current", RoleCode.StationStaff, DateTimeOffset.UtcNow, "approver");
        var scope = new StationScopeAssignment(Guid.NewGuid(), "subject-current", stationId, DateTimeOffset.UtcNow, "approver");
        await roles.GrantAsync(role, RoleAssignmentAudit.ForGrant(Guid.NewGuid(), role));
        await scopes.GrantAsync(scope, StationScopeAssignmentAudit.ForGrant(Guid.NewGuid(), scope));
        Assert.Contains(RoleCode.StationStaff, await roles.GetActiveRolesAsync("subject-current"));
        Assert.Contains(stationId, await scopes.GetActiveStationIdsAsync("subject-current"));

        Assert.True(await roles.RevokeAsync(role.Id, "approver", DateTimeOffset.UtcNow.AddMinutes(1), Guid.NewGuid()));
        Assert.True(await scopes.RevokeAsync(scope.Id, "approver", DateTimeOffset.UtcNow.AddMinutes(1), Guid.NewGuid()));
        Assert.Empty(await roles.GetActiveRolesAsync("subject-current"));
        Assert.Empty(await scopes.GetActiveStationIdsAsync("subject-current"));
    }

    private SmartRailDbContext CreateContext() => new(new DbContextOptionsBuilder<SmartRailDbContext>().UseNpgsql(fixture.ConnectionString).Options);
}
