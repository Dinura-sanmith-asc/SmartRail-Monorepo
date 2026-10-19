using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartRail.Application.Incidents.Categories;
using SmartRail.Domain.Identity;
using SmartRail.Infrastructure.Incidents.Categories;
using SmartRail.Infrastructure.RailwayNetwork;
using SmartRail.IntegrationTests.Identity;
using SmartRail.IntegrationTests.RailwayNetwork;

namespace SmartRail.IntegrationTests.Incidents;

[Collection(IncidentPostgresCollection.Name)]
public sealed class IncidentLocalSmokeTests(PostgreSqlTestDatabaseFixture database, LocalOidcFixture oidc)
{
    [Fact]
    public async Task Signed_local_http_flow_covers_create_read_update_and_safe_failure_paths()
    {
        var key = Guid.NewGuid().ToString("N");
        await using var factory = await IncidentApiTestHost.CreateAsync(database, oidc, services =>
            services.Configure<IncidentCategoryCatalogOptions>(options => options.Codes = IncidentTestCategories.All.ToArray()));
        using var client = factory.CreateClient();
        var controller = "f004-smoke-controller-" + key;
        var passenger = "f004-smoke-passenger-" + key;
        await IncidentCreationTestSupport.GrantRoleAsync(factory.Services, controller, RoleCode.Controller);
        await IncidentCreationTestSupport.GrantRoleAsync(factory.Services, passenger, RoleCode.Passenger);
        var stationId = await IncidentCreationTestSupport.AddPublishedStationAsync(factory.Services);
        var controllerToken = oidc.CreateToken(controller);

        using (var initialRead = await client.SendAsync(IncidentReadTestSupport.Get(controllerToken)))
        {
            Assert.Equal(HttpStatusCode.OK, initialRead.StatusCode);
            using var json = JsonDocument.Parse(await initialRead.Content.ReadAsStringAsync());
            Assert.Equal(JsonValueKind.Array, json.RootElement.GetProperty("items").ValueKind);
        }

        using var created = await client.SendAsync(IncidentCreationTestSupport.Post(stationId.ToString(),
            token: controllerToken, key: "smoke-create-" + key));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var createdJson = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        var incidentId = createdJson.RootElement.GetProperty("incidentId").GetGuid();
        Assert.Equal("Reported", createdJson.RootElement.GetProperty("state").GetString());
        Assert.DoesNotContain(controller, createdJson.RootElement.GetRawText(), StringComparison.Ordinal);

        using (var read = await client.SendAsync(IncidentReadTestSupport.Get(controllerToken, $"?incidentId={incidentId:D}")))
        {
            Assert.Equal(HttpStatusCode.OK, read.StatusCode);
            using var readJson = JsonDocument.Parse(await read.Content.ReadAsStringAsync());
            Assert.Equal(incidentId, readJson.RootElement.GetProperty("items")[0].GetProperty("incidentId").GetGuid());
        }

        using var updated = await client.SendAsync(IncidentReadTestSupport.Patch(incidentId, 1, "In Progress",
            controllerToken, "smoke-update-" + key));
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        using var updatedJson = JsonDocument.Parse(await updated.Content.ReadAsStringAsync());
        Assert.Equal("In Progress", updatedJson.RootElement.GetProperty("state").GetString());

        using (var invalid = await client.SendAsync(IncidentReadTestSupport.Get("not-a-jwt")))
            Assert.Equal(HttpStatusCode.Unauthorized, invalid.StatusCode);
        using (var denied = await client.SendAsync(IncidentReadTestSupport.Get(oidc.CreateToken(passenger))))
            Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        using (var conflict = await client.SendAsync(IncidentReadTestSupport.Patch(incidentId, 1, "Resolved",
                   controllerToken, "smoke-stale-" + key)))
            Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartRailDbContext>();
        Assert.Equal("InProgress", (await db.Incidents.AsNoTracking()
            .SingleAsync(item => item.IncidentId == incidentId)).State.ToString());
    }

    [Fact]
    public async Task Unconfigured_default_category_authority_fails_closed_without_loading_test_values_or_writing()
    {
        var key = Guid.NewGuid().ToString("N");
        await using var factory = await IncidentApiTestHost.CreateAsync(database, oidc);
        using var client = factory.CreateClient();
        var controller = "f004-smoke-no-category-" + key;
        await IncidentCreationTestSupport.GrantRoleAsync(factory.Services, controller, RoleCode.Controller);
        var stationId = await IncidentCreationTestSupport.AddPublishedStationAsync(factory.Services);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var category = scope.ServiceProvider.GetRequiredService<IIncidentCategoryCatalog>();
            Assert.Equal(CategoryCheckOutcome.Unavailable,
                (await category.CheckAsync(IncidentTestCategories.TrackObstruction)).Outcome);
        }
        int before;
        await using (var scope = factory.Services.CreateAsyncScope())
            before = await scope.ServiceProvider.GetRequiredService<SmartRailDbContext>().Incidents.CountAsync();

        using var response = await client.SendAsync(IncidentCreationTestSupport.Post(stationId.ToString(),
            token: oidc.CreateToken(controller), key: "smoke-unconfigured-" + key));
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        await using (var scope = factory.Services.CreateAsyncScope())
            Assert.Equal(before, await scope.ServiceProvider.GetRequiredService<SmartRailDbContext>().Incidents.CountAsync());
    }
}
