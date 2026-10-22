using Microsoft.EntityFrameworkCore;
using Npgsql;
using SmartRail.Application.RailwayNetwork.Stations;
using SmartRail.Domain.RailwayNetwork.Entities;
using SmartRail.Infrastructure.RailwayNetwork;
using SmartRail.Infrastructure.RailwayNetwork.Persistence;

namespace SmartRail.IntegrationTests.RailwayNetwork.Persistence;

public sealed class RailwayNetworkMigrationTests(PostgreSqlTestDatabaseFixture fixture) : IClassFixture<PostgreSqlTestDatabaseFixture>
{
    private SmartRailDbContext CreateContext() => new(new DbContextOptionsBuilder<SmartRailDbContext>()
        .UseNpgsql(fixture.ConnectionString).Options);

    [Fact]
    public async Task Initial_migration_creates_railway_tables_audit_relations_and_constraints()
    {
        await using var db = CreateContext();
        await db.Database.MigrateAsync();

        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using (var versionCommand = new NpgsqlCommand("SELECT current_setting('server_version_num')::integer", connection))
        {
            var serverVersion = (int)(await versionCommand.ExecuteScalarAsync())!;
            Assert.InRange(serverVersion, 180000, 189999);
        }

        var tables = await ReadSetAsync(connection,
            "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' AND table_type = 'BASE TABLE'");
        Assert.Contains("station", tables);
        Assert.Contains("route", tables);
        Assert.Contains("route_stop", tables);
        Assert.Contains("train_service", tables);
        Assert.Contains("service_schedule_revision", tables);
        Assert.Contains("service_calling_point", tables);
        Assert.Contains("service_date_manifest", tables);
        Assert.Contains("service_operation_date", tables);
        Assert.Contains("station_import_run", tables);
        Assert.Contains("station_change", tables);
        Assert.Contains("__EFMigrationsHistory", tables);

        var importRunColumns = await ReadSetAsync(connection,
            "SELECT column_name FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'station_import_run'");
        foreach (var column in new[] { "import_run_id", "source_revision", "content_hash", "submitted_by", "approved_by", "started_at", "completed_at", "created_count", "updated_count", "unchanged_count" })
            Assert.Contains(column, importRunColumns);
        var stationChangeColumns = await ReadSetAsync(connection,
            "SELECT column_name FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'station_change'");
        foreach (var column in new[] { "station_change_id", "import_run_id", "station_id", "action", "changed_field_names", "changed_at", "changed_by" })
            Assert.Contains(column, stationChangeColumns);

        var constraints = await ReadSetAsync(connection,
            "SELECT conname FROM pg_constraint WHERE connamespace = 'public'::regnamespace");
        Assert.Contains("pk_station_import_run", constraints);
        Assert.Contains("pk_station_change", constraints);
        Assert.Contains("fk_station_change_import_run", constraints);
        Assert.Contains("fk_station_change_station", constraints);
        Assert.Contains("ck_station_import_run_counts_nonnegative", constraints);
        Assert.Contains("ck_station_import_run_time_order", constraints);
        Assert.Contains("ck_station_change_action", constraints);
        Assert.Contains("ck_route_stop_order_positive", constraints);
        Assert.Contains("ck_schedule_revision_positive", constraints);
        Assert.Contains("ck_calling_point_arrival_nonnegative", constraints);
        Assert.Contains("ck_calling_point_departure_nonnegative", constraints);
        Assert.Contains("ck_service_date_manifest_complete_evidence", constraints);

        var restrictiveForeignKeys = await ReadSetAsync(connection,
            "SELECT conname FROM pg_constraint WHERE contype = 'f' AND confdeltype = 'r'");
        Assert.Contains("fk_station_change_import_run", restrictiveForeignKeys);
        Assert.Contains("fk_station_change_station", restrictiveForeignKeys);
        Assert.Contains("fk_route_stop_route", restrictiveForeignKeys);
        Assert.Contains("fk_route_stop_station", restrictiveForeignKeys);

        var indexes = await ReadSetAsync(connection,
            "SELECT indexname FROM pg_indexes WHERE schemaname = 'public'");
        Assert.Contains("ux_schedule_current_published_service", indexes);
        Assert.Contains("ux_route_stop_route_order", indexes);
        Assert.Contains("ix_station_import_run_source_started", indexes);
        Assert.Contains("ix_station_change_import_run", indexes);
        Assert.Contains("ix_station_change_station_changed_at", indexes);
    }

    [Fact]
    public async Task Station_import_persists_audit_and_rolls_back_all_writes_on_audit_failure()
    {
        await using var db = CreateContext();
        await db.Database.MigrateAsync();
        var repository = new StationImportRepository(db);
        var stationId = Guid.NewGuid();
        var run = await repository.ApplyAsync(Request([
            new StationImportRecord(stationId, "TEST-AUDIT-1", "Audit Station", true,
                new Dictionary<string, string> { ["district"] = "Colombo" })
        ]));

        Assert.Equal(1, run.CreatedCount);
        Assert.Equal(0, run.UpdatedCount);
        Assert.Equal(0, run.UnchangedCount);
        Assert.Equal(1, await db.StationImportRuns.CountAsync(item => item.ImportRunId == run.ImportRunId));
        var createdChange = await db.StationChanges.SingleAsync(item => item.ImportRunId == run.ImportRunId);
        Assert.Equal(StationChangeAction.Created, createdChange.Action);
        Assert.Contains("name", createdChange.ChangedFieldNames);
        Assert.Equal("fixture-approver", createdChange.ChangedBy);

        db.ChangeTracker.Clear();
        var unchanged = await repository.ApplyAsync(Request([
            new StationImportRecord(Guid.NewGuid(), "TEST-AUDIT-1", "Audit Station", true,
                new Dictionary<string, string>())
        ]));
        Assert.Equal(stationId, (await db.Stations.SingleAsync(item => item.StationId == stationId)).StationId);
        Assert.Equal(1, unchanged.UnchangedCount);
        Assert.Equal(1, await db.StationChanges.CountAsync(item => item.ImportRunId == run.ImportRunId));

        db.ChangeTracker.Clear();
        var updateRun = await repository.ApplyAsync(Request([
            new StationImportRecord(Guid.NewGuid(), "TEST-AUDIT-1", "Audit Station Updated", true,
                new Dictionary<string, string> { ["district"] = "Galle", ["municipality"] = "Galle" })
        ]));
        var updatedStation = await db.Stations.SingleAsync(item => item.StationId == stationId);
        Assert.Equal(1, updateRun.UpdatedCount);
        Assert.Equal("Audit Station Updated", updatedStation.Name);
        Assert.Equal("Galle", updatedStation.VerifiedDetails["district"]);
        Assert.Equal("Galle", updatedStation.VerifiedDetails["municipality"]);
        Assert.Equal(2, await db.StationChanges.CountAsync(item => item.StationId == stationId));

        await using (var setup = new NpgsqlConnection(fixture.ConnectionString))
        {
            await setup.OpenAsync();
            await using var command = new NpgsqlCommand("""
                CREATE FUNCTION reject_station_change_insert() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN RAISE EXCEPTION 'forced audit write failure'; END $$;
                CREATE TRIGGER reject_station_change_insert BEFORE INSERT ON station_change
                FOR EACH ROW EXECUTE FUNCTION reject_station_change_insert();
                """, setup);
            await command.ExecuteNonQueryAsync();
        }

        var rejectedStationId = Guid.NewGuid();
        await Assert.ThrowsAsync<DbUpdateException>(() => repository.ApplyAsync(Request([
            new StationImportRecord(rejectedStationId, "TEST-AUDIT-ROLLBACK", "Rollback Station", true,
                new Dictionary<string, string>())
        ], sourceRevision: "fixture-rollback")));
        db.ChangeTracker.Clear();
        Assert.False(await db.Stations.AnyAsync(item => item.StationId == rejectedStationId));
        Assert.False(await db.StationImportRuns.AnyAsync(item => item.SourceRevision == "fixture-rollback"));
        Assert.False(await db.StationChanges.AnyAsync(item => item.StationId == rejectedStationId));
    }

    private static StationImportRequest Request(IReadOnlyCollection<StationImportRecord> records, string sourceRevision = "fixture-audit") =>
        new(sourceRevision, "sha256:fixture-audit", "fixture-submitter", "fixture-approver",
            DateTimeOffset.UtcNow, records);

    private static async Task<HashSet<string>> ReadSetAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        var values = new HashSet<string>(StringComparer.Ordinal);
        while (await reader.ReadAsync()) values.Add(reader.GetString(0));
        return values;
    }
}