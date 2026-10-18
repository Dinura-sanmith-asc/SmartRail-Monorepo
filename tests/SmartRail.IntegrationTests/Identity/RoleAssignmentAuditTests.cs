using Microsoft.EntityFrameworkCore;
using SmartRail.Application.Identity.Persistence;
using SmartRail.Domain.Identity;
using SmartRail.Domain.Identity.Entities;
using SmartRail.Infrastructure.Identity.Persistence;
using SmartRail.Infrastructure.RailwayNetwork;
using SmartRail.IntegrationTests.RailwayNetwork;

namespace SmartRail.IntegrationTests.Identity;

public sealed class RoleAssignmentAuditTests(PostgreSqlTestDatabaseFixture fixture) : IClassFixture<PostgreSqlTestDatabaseFixture>
{
    [Fact]
    public async Task Grant_revoke_and_regrant_preserve_atomic_audit_history()
    {
        await using var db = CreateContext();
        await db.Database.MigrateAsync();
        IRoleAssignmentRepository repository = new RoleAssignmentRepository(db);
        var grantTime = DateTimeOffset.UtcNow;
        var revokeTime = grantTime.AddMinutes(1);
        var first = CreateRole(RoleCode.Controller, grantTime);
        var grantAuditId = Guid.NewGuid();
        await repository.GrantAsync(first, RoleAssignmentAudit.ForGrant(grantAuditId, first, "ticket-1"));
        Assert.True(await repository.RevokeAsync(first.Id, "approver-2", revokeTime, Guid.NewGuid(), "ticket-2"));
        var second = CreateRole(RoleCode.Controller, revokeTime.AddMinutes(1));
        await repository.GrantAsync(second, RoleAssignmentAudit.ForGrant(Guid.NewGuid(), second, "ticket-3"));

        var history = await db.RoleAssignmentAudits.AsNoTracking().OrderBy(x => x.OccurredAtUtc).ToListAsync();
        Assert.Collection(history,
            grant => { Assert.Equal(AssignmentAuditAction.Granted, grant.Action); Assert.Equal("approver-1", grant.ActorSubject); Assert.Equal("ticket-1", grant.EvidenceReference); },
            revoke => { Assert.Equal(AssignmentAuditAction.Revoked, revoke.Action); Assert.Equal("approver-2", revoke.ActorSubject); Assert.Equal("ticket-2", revoke.EvidenceReference); },
            regrant => { Assert.Equal(AssignmentAuditAction.Granted, regrant.Action); Assert.Equal(second.Id, regrant.RoleAssignmentId); });

        var duplicateAuditId = Guid.NewGuid();
        var collision = CreateRole(RoleCode.Driver);
        await repository.GrantAsync(collision, RoleAssignmentAudit.ForGrant(duplicateAuditId, collision));
        db.ChangeTracker.Clear();
        var shouldRollback = CreateRole(RoleCode.Passenger);
        await Assert.ThrowsAsync<DbUpdateException>(() => repository.GrantAsync(shouldRollback,
            RoleAssignmentAudit.ForGrant(duplicateAuditId, shouldRollback)));
        db.ChangeTracker.Clear();
        Assert.False(await db.RoleAssignments.AnyAsync(x => x.Id == shouldRollback.Id));
        Assert.Equal(4, await db.RoleAssignmentAudits.CountAsync());

        await Assert.ThrowsAsync<DbUpdateException>(() => repository.RevokeAsync(
            collision.Id, "approver-2", DateTimeOffset.UtcNow.AddMinutes(2), duplicateAuditId));
        db.ChangeTracker.Clear();
        Assert.Null((await db.RoleAssignments.SingleAsync(x => x.Id == collision.Id)).RevokedAtUtc);
        Assert.Equal(4, await db.RoleAssignmentAudits.CountAsync());
    }

    private SmartRailDbContext CreateContext() => new(new DbContextOptionsBuilder<SmartRailDbContext>().UseNpgsql(fixture.ConnectionString).Options);
    private static RoleAssignment CreateRole(RoleCode role, DateTimeOffset? grantedAt = null) => new(Guid.NewGuid(), "opaque-role-subject", role, grantedAt ?? DateTimeOffset.UtcNow, "approver-1");
}
