using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartRail.Domain.RailwayNetwork.Entities;
using SmartRail.Infrastructure.RailwayNetwork;
using SmartRail.IntegrationTests.RailwayNetwork;

namespace SmartRail.IntegrationTests.RailwayNetwork.Api;

public sealed class RailwayNetworkFailureTests(PostgreSqlTestDatabaseFixture fixture) : IClassFixture<PostgreSqlTestDatabaseFixture>
{
    [Fact]
    public async Task Search_distinguishes_invalid_empty_and_unavailable_evidence()
    {
        using var factory = await RailwayNetworkApiTestHost.CreateAsync(fixture);
        using var client = factory.CreateClient();
        var unpublishedStationId = Guid.NewGuid();
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SmartRailDbContext>();
            db.Stations.Add(new Station(unpublishedStationId, "UNPUBLISHED-TEST", "Unpublished test station", isPublished: false));
            await db.SaveChangesAsync();
        }
        var valid = "/api/services?origin=10000000-0000-0000-0000-000000000001&destination=10000000-0000-0000-0000-000000000002";

        await AssertProblemAsync(client, "/api/services", HttpStatusCode.BadRequest);
        await AssertProblemAsync(client, valid + "&date=2026-02-30", HttpStatusCode.BadRequest);
        await AssertProblemAsync(client, "/api/services?origin=not-a-guid&destination=10000000-0000-0000-0000-000000000002&date=2026-10-17", HttpStatusCode.BadRequest);
        await AssertProblemAsync(client, "/api/services?origin=10000000-0000-0000-0000-000000000001&destination=10000000-0000-0000-0000-000000000001&date=2026-10-17", HttpStatusCode.BadRequest);
        await AssertProblemAsync(client, "/api/services?origin=aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa&destination=10000000-0000-0000-0000-000000000002&date=2026-10-17", HttpStatusCode.BadRequest);
        await AssertProblemAsync(client, $"/api/services?origin=10000000-0000-0000-0000-000000000001&destination={unpublishedStationId}&date=2026-10-17", HttpStatusCode.BadRequest);
        await AssertProblemAsync(client, "/api/services?origin=10000000-0000-0000-0000-000000000002&destination=10000000-0000-0000-0000-000000000001&date=2026-10-17", HttpStatusCode.OK, expectProblem: false);

        var completeEmpty = await client.GetAsync(valid + "&date=2026-10-18");
        Assert.Equal(HttpStatusCode.OK, completeEmpty.StatusCode);
        using (var emptyDoc = JsonDocument.Parse(await completeEmpty.Content.ReadAsStringAsync()))
            Assert.Empty(emptyDoc.RootElement.GetProperty("services").EnumerateArray());

        await AssertProblemAsync(client, valid + "&date=2026-10-19", HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task Missing_route_and_service_return_not_found_problem_details()
    {
        using var factory = await RailwayNetworkApiTestHost.CreateAsync(fixture);
        using var client = factory.CreateClient();
        await AssertProblemAsync(client, "/api/routes/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/stops", HttpStatusCode.NotFound);
        await AssertProblemAsync(client, "/api/services/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", HttpStatusCode.NotFound);
    }

    private static async Task AssertProblemAsync(HttpClient client, string path, HttpStatusCode expected, bool expectProblem = true)
    {
        using var response = await client.GetAsync(path);
        Assert.Equal(expected, response.StatusCode);
        if (!expectProblem) return;
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal((int)expected, document.RootElement.GetProperty("status").GetInt32());
    }
}
