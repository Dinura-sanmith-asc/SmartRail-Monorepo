using Microsoft.EntityFrameworkCore;
using SmartRail.Application.RailwayNetwork.Stations;
using SmartRail.Infrastructure.RailwayNetwork;
using SmartRail.Infrastructure.RailwayNetwork.Persistence;

namespace SmartRail.IntegrationTests.RailwayNetwork.Persistence;

public sealed class StationImportTests(PostgreSqlTestDatabaseFixture fixture) : IClassFixture<PostgreSqlTestDatabaseFixture>
{
    [Fact]
    public async Task Denied_import_and_invalid_whole_batch_leave_station_and_audit_state_unchanged()
    {
        await using var db = CreateContext();
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
        await new RailwayNetworkFixtureLoader(db).LoadAsync("Testing", FixtureDirectory);
        var repository = new StationImportRepository(db);
        var before = await CountsAsync(db);
        var request = Request([new(Guid.NewGuid(), "NEW-SOURCE", "New station", true, new Dictionary<string, string>())]);

        var denied = await new ImportStationsHandler(new DenyAllStationImportAuthorization(), repository)
            .HandleAsync(new("unapproved-test-identity", request));
        Assert.True(denied.IsUnauthorized);
        Assert.Equal(before, await CountsAsync(db));

        var invalid = new StationImportRequest("invalid-batch", "sha256:invalid", "test-submitter", "test-approver",
            DateTimeOffset.UtcNow, [
                new(Guid.NewGuid(), "DUPLICATE-SOURCE", "First", true, new Dictionary<string, string>()),
                new(Guid.NewGuid(), " DUPLICATE-SOURCE ", "Second", true, new Dictionary<string, string>())
            ]);
        var validation = await new ImportStationsHandler(new AllowTestStationImportAuthorization(), repository)
            .HandleAsync(new("approved-test-identity", invalid));
        Assert.False(validation.IsUnauthorized);
        Assert.Null(validation.ImportRun);
        Assert.NotEmpty(validation.Errors);
        Assert.Equal(before, await CountsAsync(db));
    }

    [Fact]
    public async Task Authorized_import_is_idempotent_and_preserves_station_identity_and_minimal_audit()
    {
        await using var db = CreateContext();
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
        await new RailwayNetworkFixtureLoader(db).LoadAsync("Testing", FixtureDirectory);
        var repository = new StationImportRepository(db);
        var newId = Guid.NewGuid();
        var first = await repository.ApplyAsync(Request([
            new(newId, "TEST-IMPORT-UNIQUE", "Test station", true, new Dictionary<string, string> { ["district"] = "Colombo" })
        ], "import-create"));
        var repeat = await repository.ApplyAsync(Request([
            new(Guid.NewGuid(), "TEST-IMPORT-UNIQUE", "Test station", true, new Dictionary<string, string> { ["district"] = "Colombo" })
        ], "import-repeat"));

        var station = await db.Stations.SingleAsync(item => item.StationId == newId);
        Assert.Equal("Test station", station.Name);
        Assert.Equal("Colombo", station.VerifiedDetails["district"]);
        Assert.Equal(1, first.CreatedCount);
        Assert.Equal(1, repeat.UnchangedCount);
        var update = await repository.ApplyAsync(Request([
            new(Guid.NewGuid(), "TEST-IMPORT-UNIQUE", "Test station", true, new Dictionary<string, string> { ["district"] = "Galle" })
        ], "import-update-details"));
        await db.Entry(station).ReloadAsync();
        Assert.Equal(1, update.UpdatedCount);
        Assert.Equal("Galle", station.VerifiedDetails["district"]);
        Assert.Equal(1, await db.StationChanges.CountAsync(item => item.ImportRunId == update.ImportRunId));
        Assert.Equal(2, await db.RouteStops.Join(db.Stations, stop => stop.StationId, station => station.StationId,
            (stop, station) => stop).CountAsync());
        Assert.Equal(1, await db.StationChanges.CountAsync(item => item.ImportRunId == first.ImportRunId));
        Assert.Equal(0, await db.StationChanges.CountAsync(item => item.ImportRunId == repeat.ImportRunId));
        Assert.DoesNotContain(typeof(SmartRail.Domain.RailwayNetwork.Entities.StationChange).GetProperties(),
            property => property.Name.Contains("Snapshot", StringComparison.OrdinalIgnoreCase));
    }

    private SmartRailDbContext CreateContext() => new(new DbContextOptionsBuilder<SmartRailDbContext>()
        .UseNpgsql(fixture.ConnectionString).Options);

    private static Task<(int Stations, int Runs, int Changes)> CountsAsync(SmartRailDbContext db) =>
        Task.FromResult((db.Stations.Count(), db.StationImportRuns.Count(), db.StationChanges.Count()));

    private static StationImportRequest Request(IReadOnlyCollection<StationImportRecord> rows, string revision = "test-import") =>
        new(revision, "sha256:test-import", "test-submitter", "test-approver", DateTimeOffset.UtcNow, rows);

    private static string FixtureDirectory => Path.Combine(AppContext.BaseDirectory, "Fixtures", "RailwayNetwork");

    private sealed class AllowTestStationImportAuthorization : IStationImportAuthorization
    {
        public ValueTask<bool> IsAuthorizedAsync(string invocationIdentity, string submitter, string approver,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(invocationIdentity == "approved-test-identity" && submitter != approver);
    }
}
