using System.Net;
using System.Text.Json;
using SmartRail.IntegrationTests.RailwayNetwork;

namespace SmartRail.IntegrationTests.RailwayNetwork.Api;

public sealed class ServiceSearchEndpointTests(PostgreSqlTestDatabaseFixture fixture) : IClassFixture<PostgreSqlTestDatabaseFixture>
{
    [Fact]
    public async Task Overnight_search_uses_origin_departure_date_and_exposes_planned_times_only()
    {
        using var factory = await RailwayNetworkApiTestHost.CreateAsync(fixture);
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/services?origin=10000000-0000-0000-0000-000000000001&destination=10000000-0000-0000-0000-000000000002&date=2026-10-17");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(content);
        var choice = Assert.Single(document.RootElement.GetProperty("services").EnumerateArray());
        Assert.Equal(2, choice.GetProperty("scheduleRevision").GetInt32());
        Assert.Equal("2026-10-17T23:20:00+05:30", choice.GetProperty("origin").GetProperty("scheduledAt").GetString());
        Assert.Equal("2026-10-18T01:25:00+05:30", choice.GetProperty("destination").GetProperty("scheduledAt").GetString());
        Assert.DoesNotContain("liveStatus", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("journeyId", content, StringComparison.OrdinalIgnoreCase);
    }
}
