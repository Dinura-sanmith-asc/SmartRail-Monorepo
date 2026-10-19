using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartRail.Domain.Identity;
using SmartRail.Infrastructure.RailwayNetwork;
using SmartRail.IntegrationTests.Identity;
using SmartRail.IntegrationTests.JourneysStatus.Fixtures;
using SmartRail.IntegrationTests.RailwayNetwork;

namespace SmartRail.IntegrationTests.Incidents;

[Collection(IncidentPostgresCollection.Name)]
public sealed class IncidentFeatureRegressionTests(PostgreSqlTestDatabaseFixture database, LocalOidcFixture oidc)
{
    [Fact]
    public async Task Feature001_002_and_003_contracts_and_authority_remain_independent_of_incident_writes()
    {
        await using var factory = await IncidentOwnerIntegrationTestHost.CreateAsync(database, oidc);
        using var client = factory.CreateClient();
        var journeyId = await IncidentOwnerIntegrationTestHost.InitializeJourneyAsync(factory.Services, "f004-regression-journey");
        const string controller = "f004-regression-controller";
        const string passenger = "f004-regression-passenger";
        await IncidentOwnerTestSupport.GrantRoleAsync(factory.Services, controller, RoleCode.Controller);
        await IncidentOwnerTestSupport.GrantRoleAsync(factory.Services, passenger, RoleCode.Passenger);

        using (var railway = await client.GetAsync("/api/stations"))
            Assert.Equal(HttpStatusCode.OK, railway.StatusCode);
        using (var invalidOptionalCredential = new HttpRequestMessage(HttpMethod.Get, "/api/stations"))
        {
            invalidOptionalCredential.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-jwt");
            using var response = await client.SendAsync(invalidOptionalCredential);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
        using (var me = await client.GetAsync("/api/me"))
            Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);

        using var journeyBeforeResponse = await client.GetAsync($"/api/journeys/{journeyId:D}");
        Assert.Equal(HttpStatusCode.OK, journeyBeforeResponse.StatusCode);
        using var journeyBeforeJson = JsonDocument.Parse(await journeyBeforeResponse.Content.ReadAsStringAsync());
        Assert.Equal(1, journeyBeforeJson.RootElement.GetProperty("revision").GetInt32());
        Assert.DoesNotContain("reporterSubject", journeyBeforeJson.RootElement.GetRawText(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("incidentAssociations", journeyBeforeJson.RootElement.GetRawText(), StringComparison.OrdinalIgnoreCase);

        var token = oidc.CreateToken(controller);
        var incidentId = await IncidentOwnerTestSupport.CreateIncidentAsync(client, token, "f004-regression-create",
            [IncidentOwnerTestSupport.Link("Station", IncidentOwnerTestSupport.StationA)], "Regression-only incident text");
        using (var list = await client.SendAsync(IncidentReadTestSupport.Get(token)))
        {
            Assert.Equal(HttpStatusCode.OK, list.StatusCode);
            var json = await list.Content.ReadAsStringAsync();
            Assert.Contains(incidentId.ToString(), json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(controller, json, StringComparison.Ordinal);
            Assert.DoesNotContain("reporterSubject", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("privateAuditEvidence", json, StringComparison.OrdinalIgnoreCase);
        }

        using (var denied = await client.SendAsync(IncidentReadTestSupport.Get(oidc.CreateToken(passenger))))
            Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);

        using var journeyAfterResponse = await client.GetAsync($"/api/journeys/{journeyId:D}");
        Assert.Equal(HttpStatusCode.OK, journeyAfterResponse.StatusCode);
        using var journeyAfterJson = JsonDocument.Parse(await journeyAfterResponse.Content.ReadAsStringAsync());
        Assert.Equal(1, journeyAfterJson.RootElement.GetProperty("revision").GetInt32());
        Assert.DoesNotContain(incidentId.ToString(), journeyAfterJson.RootElement.GetRawText(), StringComparison.OrdinalIgnoreCase);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartRailDbContext>();
        Assert.Equal(1, await db.Journeys.AsNoTracking().Where(item => item.JourneyInstanceId == journeyId)
            .Select(item => item.CurrentRevision).SingleAsync());
    }
}
