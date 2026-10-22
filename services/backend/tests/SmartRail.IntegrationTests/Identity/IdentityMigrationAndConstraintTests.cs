using Microsoft.EntityFrameworkCore;
using Npgsql;
using SmartRail.Domain.Identity;
using SmartRail.Domain.Identity.Entities;
using SmartRail.Domain.RailwayNetwork.Entities;
using SmartRail.Infrastructure.RailwayNetwork;
using SmartRail.IntegrationTests.RailwayNetwork;

namespace SmartRail.IntegrationTests.Identity;

public sealed class IdentityMigrationAndConstraintTests(PostgreSqlTestDatabaseFixture fixture) : IClassFixture<PostgreSqlTestDatabaseFixture>
{
    [Fact]
    public async Task Additive_migration_creates_identity_schema_and_required_constraints()
    {
        await using var db = CreateContext();
        await db.Database.MigrateAsync();
        var applied = await db.Database.GetAppliedMigrationsAsync();
        Assert.Contains(applied, migration => migration.EndsWith("_InitialRailwayNetwork", StringComparison.Ordinal));
        Assert.Contains(applied, migration => migration.EndsWith("_IdentityPermissions", StringComparison.Ordinal));

        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        var tables = await ReadSetAsync(connection, "SELECT table_name FROM information_schema.tables WHERE table_schema='public'");
        Assert.Contains("identity_role_assignment", tables);
        Assert.Contains("identity_role_assignment_audit", tables);
        Assert.Contains("identity_station_scope_assignment", tables);
        Assert.Contains("identity_station_scope_assignment_audit", tables);
        var constraints = await ReadSetAsync(connection, "SELECT conname FROM pg_constraint WHERE connamespace='public'::regnamespace");
        foreach (var constraint in new[] { "ck_identity_role_assignment_role", "ck_identity_role_assignment_revocation_pair", "ck_identity_station_scope_revocation_pair", "fk_identity_station_scope_station", "fk_identity_role_assignment_audit_assignment", "fk_identity_station_scope_audit_assignment" })
            Assert.Contains(constraint, constraints);
        var indexes = await ReadSetAsync(connection, "SELECT indexname FROM pg_indexes WHERE schemaname='public'");
        Assert.Contains("ux_identity_role_assignment_active_subject_role", indexes);
        Assert.Contains("ux_identity_station_scope_active_subject_station", indexes);
        Assert.Contains("ix_identity_role_assignment_audit_history", indexes);
        Assert.Contains("ix_identity_station_scope_audit_history", indexes);
    }

    [Fact]
    public async Task PostgreSql_rejects_duplicate_active_assignments_and_invalid_references()
    {
        await using var db = CreateContext();
        await db.Database.MigrateAsync();
        var stationId = Guid.NewGuid();
        db.Stations.Add(new Station(stationId, "IDENTITY-TEST", "Identity Test"));
        await db.SaveChangesAsync();

        var role = CreateRole("opaque-subject", RoleCode.Driver);
        db.RoleAssignments.Add(role);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        db.RoleAssignments.Add(CreateRole("opaque-subject", RoleCode.Driver));
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();

        db.StationScopeAssignments.Add(CreateScope("staff-subject", stationId));
        await db.SaveChangesAsync();
        db.StationScopeAssignments.Add(CreateScope("staff-subject", stationId));
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();

        db.StationScopeAssignments.Add(CreateScope("orphan-subject", Guid.NewGuid()));
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Audit_rows_cannot_be_updated_or_deleted_through_the_context()
    {
        await using var db = CreateContext();
        await db.Database.MigrateAsync();
        var assignment = CreateRole("audit-subject", RoleCode.Passenger);
        var audit = RoleAssignmentAudit.ForGrant(Guid.NewGuid(), assignment);
        db.RoleAssignments.Add(assignment);
        db.RoleAssignmentAudits.Add(audit);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var persisted = await db.RoleAssignmentAudits.SingleAsync();
        db.RoleAssignmentAudits.Remove(persisted);
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();
        Assert.Equal(1, await db.RoleAssignmentAudits.CountAsync());
    }

    private SmartRailDbContext CreateContext() => new(new DbContextOptionsBuilder<SmartRailDbContext>().UseNpgsql(fixture.ConnectionString).Options);
    private static RoleAssignment CreateRole(string subject, RoleCode role) => new(Guid.NewGuid(), subject, role, DateTimeOffset.UtcNow, "test-approver");
    private static StationScopeAssignment CreateScope(string subject, Guid stationId) => new(Guid.NewGuid(), subject, stationId, DateTimeOffset.UtcNow, "test-approver");

    private static async Task<HashSet<string>> ReadSetAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        var values = new HashSet<string>(StringComparer.Ordinal);
        while (await reader.ReadAsync()) values.Add(reader.GetString(0));
        return values;
    }
}
