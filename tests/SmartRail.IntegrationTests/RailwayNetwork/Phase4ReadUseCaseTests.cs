using Microsoft.EntityFrameworkCore;
using SmartRail.Application.RailwayNetwork.Routes;
using SmartRail.Application.RailwayNetwork.Services;
using SmartRail.Application.RailwayNetwork.Stations;
using SmartRail.Infrastructure.RailwayNetwork;
using SmartRail.Infrastructure.RailwayNetwork.Persistence;

namespace SmartRail.IntegrationTests.RailwayNetwork;

public sealed class Phase4ReadUseCaseTests(PostgreSqlTestDatabaseFixture fixture) : IClassFixture<PostgreSqlTestDatabaseFixture>
{
    [Fact]
    public async Task Published_reads_search_order_date_completeness_and_overnight_times_are_persisted()
    {
        var options = new DbContextOptionsBuilder<SmartRailDbContext>().UseNpgsql(fixture.ConnectionString).Options;
        await using var db = new SmartRailDbContext(options);
        await db.Database.MigrateAsync();
        await new RailwayNetworkFixtureLoader(db).LoadAsync("Testing", Path.Combine(AppContext.BaseDirectory, "Fixtures", "RailwayNetwork"));

        var stations = new StationReadRepository(db);
        var stationResult = await new ListStationsHandler(stations).HandleAsync(new());
        Assert.Equal(2, stationResult.Items.Count);
        var origin = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var destination = Guid.Parse("10000000-0000-0000-0000-000000000002");

        var route = await new GetRouteStopsHandler(new RouteReadRepository(db)).HandleAsync(
            new(Guid.Parse("20000000-0000-0000-0000-000000000001")));
        Assert.Equal([origin, destination], route.Route!.Stops.Select(item => item.StationId));

        var serviceId = Guid.Parse("30000000-0000-0000-0000-000000000001");
        var detail = await new GetServiceHandler(new ServiceReadRepository(db)).HandleAsync(new(serviceId));
        Assert.Equal(2, detail.Service!.ScheduleRevision);

        var search = new SearchServicesHandler(new SearchServicesValidator(stations), new ServiceSearchRepository(db));
        var overnight = await search.HandleAsync(new(origin, destination, "2026-10-17"));
        Assert.Equal(ServiceSearchOutcome.Available, overnight.Outcome);
        var choice = Assert.Single(overnight.Services);
        Assert.Equal(new DateTimeOffset(2026, 10, 17, 23, 20, 0, TimeSpan.FromHours(5.5)), choice.Origin.ScheduledAt);
        Assert.Equal(new DateTimeOffset(2026, 10, 18, 1, 25, 0, TimeSpan.FromHours(5.5)), choice.Destination.ScheduledAt);

        var empty = await search.HandleAsync(new(origin, destination, "2026-10-18"));
        Assert.Equal(ServiceSearchOutcome.Available, empty.Outcome);
        Assert.Empty(empty.Services);
        var incomplete = await search.HandleAsync(new(origin, destination, "2026-10-19"));
        Assert.Equal(ServiceSearchOutcome.Unavailable, incomplete.Outcome);
        var absent = await search.HandleAsync(new(origin, destination, "2026-10-20"));
        Assert.Equal(ServiceSearchOutcome.Unavailable, absent.Outcome);
        var invalid = await search.HandleAsync(new(origin, origin, "2026-10-17"));
        Assert.Equal(ServiceSearchOutcome.Invalid, invalid.Outcome);
    }
}
