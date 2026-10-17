using Microsoft.EntityFrameworkCore;
using SmartRail.Application.RailwayNetwork.Services;
using SmartRail.Domain.RailwayNetwork.Entities;
using SmartRail.Infrastructure.RailwayNetwork;
using SmartRail.Infrastructure.RailwayNetwork.Persistence;

namespace SmartRail.IntegrationTests.RailwayNetwork.Persistence;

public sealed class ServiceSearchRepositoryTests(PostgreSqlTestDatabaseFixture fixture) : IClassFixture<PostgreSqlTestDatabaseFixture>
{
    [Fact]
    public async Task Search_uses_exact_date_order_complete_empty_and_overnight_origin_departure()
    {
        await using var db = CreateContext();
        await db.Database.MigrateAsync();
        await new RailwayNetworkFixtureLoader(db).LoadAsync("Testing", FixtureDirectory);
        var origin = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var destination = Guid.Parse("10000000-0000-0000-0000-000000000002");
        var repo = new ServiceSearchRepository(db);

        var overnight = await repo.SearchAsync(new(origin, destination, new DateOnly(2026, 10, 17)));
        Assert.True(overnight.IsAvailable);
        var choice = Assert.Single(overnight.Services);
        Assert.Equal(2, choice.ScheduleRevision);
        Assert.Equal(new DateTimeOffset(2026, 10, 17, 23, 20, 0, TimeSpan.FromHours(5.5)), choice.Origin.ScheduledAt);
        Assert.Equal(new DateTimeOffset(2026, 10, 18, 1, 25, 0, TimeSpan.FromHours(5.5)), choice.Destination.ScheduledAt);

        var empty = await repo.SearchAsync(new(origin, destination, new DateOnly(2026, 10, 18)));
        Assert.True(empty.IsAvailable);
        Assert.Empty(empty.Services);
        Assert.False((await repo.SearchAsync(new(origin, destination, new DateOnly(2026, 10, 19)))).IsAvailable);
        Assert.False((await repo.SearchAsync(new(origin, destination, new DateOnly(2026, 10, 20)))).IsAvailable);
        var reversed = await repo.SearchAsync(new(destination, origin, new DateOnly(2026, 10, 17)));
        Assert.True(reversed.IsAvailable);
        Assert.Empty(reversed.Services);

        await AddSkippedCallingPointRouteAsync(db);
        var skipped = await repo.SearchAsync(new(Guid.Parse("12000000-0000-0000-0000-000000000001"),
            Guid.Parse("12000000-0000-0000-0000-000000000003"), new DateOnly(2026, 10, 21)));
        Assert.True(skipped.IsAvailable);
        Assert.Single(skipped.Services);
    }

    private static async Task AddSkippedCallingPointRouteAsync(SmartRailDbContext db)
    {
        var now = DateTimeOffset.UtcNow;
        var routeId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        var scheduleId = Guid.NewGuid();
        var stationIds = new[]
        {
            Guid.Parse("12000000-0000-0000-0000-000000000001"),
            Guid.Parse("12000000-0000-0000-0000-000000000002"),
            Guid.Parse("12000000-0000-0000-0000-000000000003")
        };
        db.Stations.AddRange(stationIds.Select((id, index) => new Station(id, $"SKIP-{index}", $"Skip Test {index}", true)));
        var route = new Route(routeId, "SKIP-ROUTE", "Route with an unserved intermediate station", true);
        var stops = stationIds.Select((stationId, index) => new RouteStop(Guid.NewGuid(), routeId, stationId, index + 1)).ToArray();
        var service = new TrainService(serviceId, "SKIP-SERVICE", routeId);
        var schedule = new ServiceScheduleRevision(scheduleId, serviceId, 1, new TimeOnly(8, 0), now.AddMinutes(-1));
        db.Routes.Add(route);
        db.RouteStops.AddRange(stops);
        db.TrainServices.Add(service);
        db.ServiceScheduleRevisions.Add(schedule);
        db.ServiceCallingPoints.AddRange(
            new ServiceCallingPoint(scheduleId, stops[0].RouteStopId, null, 0),
            new ServiceCallingPoint(scheduleId, stops[2].RouteStopId, 100, null));
        await db.SaveChangesAsync();
        await new SchedulePublicationRepository(db).PublishAsync(serviceId, scheduleId, now);
        await new ServiceDateManifestRepository(db).PublishAsync(
            new ServiceDateManifest(new DateOnly(2026, 10, 21), "skip-source", "skip-v1", true, now, "test-publisher", "sha256:skip"),
            [new ServiceOperationDate(scheduleId, new DateOnly(2026, 10, 21))]);
    }

    private SmartRailDbContext CreateContext() => new(new DbContextOptionsBuilder<SmartRailDbContext>()
        .UseNpgsql(fixture.ConnectionString).Options);

    private static string FixtureDirectory => Path.Combine(AppContext.BaseDirectory, "Fixtures", "RailwayNetwork");
}
