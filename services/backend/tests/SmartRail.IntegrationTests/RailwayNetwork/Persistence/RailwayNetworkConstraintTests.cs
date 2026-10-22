using Microsoft.EntityFrameworkCore;
using SmartRail.Domain.RailwayNetwork.Entities;
using SmartRail.Infrastructure.RailwayNetwork;

namespace SmartRail.IntegrationTests.RailwayNetwork.Persistence;

public sealed class RailwayNetworkConstraintTests(PostgreSqlTestDatabaseFixture fixture) : IClassFixture<PostgreSqlTestDatabaseFixture>
{
    [Fact]
    public async Task PostgreSql_enforces_railway_foreign_keys_unique_keys_order_and_current_revision()
    {
        await using var db = CreateContext();
        await db.Database.MigrateAsync();
        await new RailwayNetworkFixtureLoader(db).LoadAsync("Testing", FixtureDirectory);
        var routeId = Guid.Parse("20000000-0000-0000-0000-000000000001");
        var stationId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var serviceId = Guid.Parse("30000000-0000-0000-0000-000000000001");
        var existingStopOrder = await db.RouteStops.Where(item => item.RouteId == routeId).Select(item => item.StopOrder).FirstAsync();

        db.RouteStops.Add(new RouteStop(Guid.NewGuid(), Guid.NewGuid(), stationId, 8));
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();

        db.TrainServices.Add(new TrainService(Guid.NewGuid(), "INVALID-ROUTE", Guid.NewGuid()));
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();

        db.RouteStops.Add(new RouteStop(Guid.NewGuid(), routeId, stationId, existingStopOrder));
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();

        var current = await db.ServiceScheduleRevisions.SingleAsync(item => item.ServiceId == serviceId && item.IsPublished && item.SupersededAt == null);
        db.ServiceScheduleRevisions.Add(new ServiceScheduleRevision(Guid.NewGuid(), serviceId, current.Revision,
            new TimeOnly(23, 20), DateTimeOffset.UtcNow));
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();

        var newCurrent = new ServiceScheduleRevision(Guid.NewGuid(), serviceId, 3,
            new TimeOnly(23, 20), DateTimeOffset.UtcNow, isPublished: true, publishedAt: DateTimeOffset.UtcNow);
        db.ServiceScheduleRevisions.Add(newCurrent);
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());

        await using var command = db.Database.GetDbConnection().CreateCommand();
        if (command.Connection!.State != System.Data.ConnectionState.Open) await command.Connection.OpenAsync();
        command.CommandText = "INSERT INTO route_stop (route_stop_id, route_id, station_id, stop_order) VALUES (@id, @route, @station, 0)";
        AddParameter(command, "id", Guid.NewGuid());
        AddParameter(command, "route", routeId);
        AddParameter(command, "station", stationId);
        await Assert.ThrowsAnyAsync<Exception>(() => command.ExecuteNonQueryAsync());
    }

    private SmartRailDbContext CreateContext() => new(new DbContextOptionsBuilder<SmartRailDbContext>()
        .UseNpgsql(fixture.ConnectionString).Options);

    private static string FixtureDirectory => Path.Combine(AppContext.BaseDirectory, "Fixtures", "RailwayNetwork");

    private static void AddParameter(System.Data.Common.DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
