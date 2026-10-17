using System.Net;
using SmartRail.IntegrationTests.RailwayNetwork;

namespace SmartRail.IntegrationTests.RailwayNetwork.Api;

public sealed class RailwayNetworkEndpointTests(PostgreSqlTestDatabaseFixture fixture) : IClassFixture<PostgreSqlTestDatabaseFixture>
{
    [Fact]
    public async Task Four_approved_get_routes_are_anonymous_and_station_write_route_is_absent()
    {
        using var factory = await RailwayNetworkApiTestHost.CreateAsync(fixture);
        using var client = factory.CreateClient();
        Assert.Null(client.DefaultRequestHeaders.Authorization);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/stations")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/routes/20000000-0000-0000-0000-000000000001/stops")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/services?origin=10000000-0000-0000-0000-000000000001&destination=10000000-0000-0000-0000-000000000002&date=2026-10-17")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/services/30000000-0000-0000-0000-000000000001")).StatusCode);

        var stationWrite = await client.PostAsync("/api/stations", new StringContent("{}"));
        Assert.NotEqual(HttpStatusCode.OK, stationWrite.StatusCode);
        Assert.NotEqual(HttpStatusCode.Created, stationWrite.StatusCode);
        Assert.NotEqual(HttpStatusCode.NoContent, stationWrite.StatusCode);
    }
}
