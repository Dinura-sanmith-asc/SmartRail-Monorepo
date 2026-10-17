using Microsoft.EntityFrameworkCore;
using SmartRail.Domain.RailwayNetwork.Entities;
using SmartRail.Infrastructure.RailwayNetwork;

namespace SmartRail.IntegrationTests.RailwayNetwork;

public sealed class RailwayNetworkFixtureLoaderTests(PostgreSqlTestDatabaseFixture fixture) : IClassFixture<PostgreSqlTestDatabaseFixture>
{
    [Fact]
    public async Task Fixture_loader_is_nonproduction_only_and_loads_published_domain_data()
    {
        var options = new DbContextOptionsBuilder<SmartRailDbContext>().UseNpgsql(fixture.ConnectionString).Options;
        await using var db = new SmartRailDbContext(options);
        await db.Database.MigrateAsync();
        var fixtureDirectory = Path.Combine(AppContext.BaseDirectory, "Fixtures", "RailwayNetwork");
        var loader = new RailwayNetworkFixtureLoader(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() => loader.LoadAsync("Production", fixtureDirectory));
        Assert.Empty(await db.Stations.ToListAsync());

        await loader.LoadAsync("Testing", fixtureDirectory);

        Assert.Equal(2, await db.Stations.CountAsync());
        Assert.Equal(1, await db.Routes.CountAsync());
        Assert.Equal(2, await db.RouteStops.CountAsync());
        var schedules = await db.ServiceScheduleRevisions.OrderBy(item => item.Revision).ToListAsync();
        Assert.Equal(2, schedules.Count);
        Assert.True(schedules[0].IsPublished);
        Assert.NotNull(schedules[0].SupersededAt);
        Assert.True(schedules[1].IsCurrentPublished);

        var completeDates = await db.ServiceDateManifests.Where(item => item.IsComplete).Select(item => item.OperationDate).ToListAsync();
        Assert.Contains(new DateOnly(2026, 10, 17), completeDates);
        Assert.Contains(new DateOnly(2026, 10, 18), completeDates);
        Assert.Equal(0, await db.ServiceOperationDates.CountAsync(item => item.OperationDate == new DateOnly(2026, 10, 18)));
        Assert.True(await db.ServiceDateManifests.AnyAsync(item => item.OperationDate == new DateOnly(2026, 10, 19) && !item.IsComplete));
        Assert.False(await db.ServiceDateManifests.AnyAsync(item => item.OperationDate == new DateOnly(2026, 10, 20)));
    }
}