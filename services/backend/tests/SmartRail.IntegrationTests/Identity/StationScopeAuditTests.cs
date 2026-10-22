using Microsoft.EntityFrameworkCore;
using SmartRail.Application.Identity.Persistence;
using SmartRail.Domain.Identity.Entities;
using SmartRail.Domain.RailwayNetwork.Entities;
using SmartRail.Infrastructure.Identity.Persistence;
using SmartRail.Infrastructure.RailwayNetwork;
using SmartRail.IntegrationTests.RailwayNetwork;

namespace SmartRail.IntegrationTests.Identity;

public sealed class StationScopeAuditTests(PostgreSqlTestDatabaseFixture fixture) : IClassFixture<PostgreSqlTestDatabaseFixture>
{
    [Fact]
    public async Task Station_scope_grant_revoke_and_regrant_preserve_atomic_audit_history()
    {
        await using var db = CreateContext();
        await db.Database.MigrateAsync();
        var stationId = Guid.NewGuid();
        db.Stations.Add(new Station(stationId, "SCOPE-TEST", "Scope Test"));
        await db.SaveChangesAsync();
        IStationScopeRepository repository = new StationScopeRepository(db);
        var grantTime = DateTimeOffset.UtcNow;
        var revokeTime = grantTime.AddMinutes(1);
        var first = CreateScope(stationId, grantTime);
        await repository.GrantAsync(first, StationScopeAssignmentAudit.ForGrant(Guid.NewGuid(), first, "station-ticket-1"));
        Assert.True(await repository.RevokeAsync(first.Id, "scope-approver-2", revokeTime, Guid.NewGuid(), "station-ticket-2"));
        var second = CreateScope(stationId, revokeTime.AddMinutes(1));
        await repository.GrantAsync(second, StationScopeAssignmentAudit.ForGrant(Guid.NewGuid(), second, "station-ticket-3"));

        var history = await db.StationScopeAssignmentAudits.AsNoTracking().OrderBy(x => x.OccurredAtUtc).ToListAsync();
        Assert.Collection(history,
            grant => { Assert.Equal(AssignmentAuditAction.Granted, grant.Action); Assert.Equal("scope-approver-1", grant.ActorSubject); Assert.Equal("station-ticket-1", grant.EvidenceReference); },
            revoke => { Assert.Equal(AssignmentAuditAction.Revoked, revoke.Action); Assert.Equal("scope-approver-2", revoke.ActorSubject); Assert.Equal("station-ticket-2", revoke.EvidenceReference); },
            regrant => { Assert.Equal(AssignmentAuditAction.Granted, regrant.Action); Assert.Equal(second.Id, regrant.StationScopeAssignmentId); });

        db.ChangeTracker.Clear();
        var duplicateAuditId = Guid.NewGuid();
        var collision = CreateScope(stationId, subject: "collision-subject");
        await repository.GrantAsync(collision, StationScopeAssignmentAudit.ForGrant(duplicateAuditId, collision));
        db.ChangeTracker.Clear();
        var shouldRollback = CreateScope(stationId, subject: "rollback-subject");
        await Assert.ThrowsAsync<DbUpdateException>(() => repository.GrantAsync(shouldRollback,
            StationScopeAssignmentAudit.ForGrant(duplicateAuditId, shouldRollback)));
        db.ChangeTracker.Clear();
        Assert.False(await db.StationScopeAssignments.AnyAsync(x => x.Id == shouldRollback.Id));
        Assert.Equal(4, await db.StationScopeAssignmentAudits.CountAsync());

        await Assert.ThrowsAsync<DbUpdateException>(() => repository.RevokeAsync(
            collision.Id, "scope-approver-2", DateTimeOffset.UtcNow.AddMinutes(2), duplicateAuditId));
        db.ChangeTracker.Clear();
        Assert.Null((await db.StationScopeAssignments.SingleAsync(x => x.Id == collision.Id)).RevokedAtUtc);
        var audit = await db.StationScopeAssignmentAudits.SingleAsync(x => x.Id == duplicateAuditId);
        db.StationScopeAssignmentAudits.Remove(audit);
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();
        Assert.Equal(4, await db.StationScopeAssignmentAudits.CountAsync());
    }

    private SmartRailDbContext CreateContext() => new(new DbContextOptionsBuilder<SmartRailDbContext>().UseNpgsql(fixture.ConnectionString).Options);
    private static StationScopeAssignment CreateScope(Guid stationId, DateTimeOffset? grantedAt = null, string subject = "opaque-staff-subject") => new(Guid.NewGuid(), subject, stationId, grantedAt ?? DateTimeOffset.UtcNow, "scope-approver-1");
}
