using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartRail.Domain.Identity;
using SmartRail.Infrastructure.RailwayNetwork;
using SmartRail.IntegrationTests.Identity;
using SmartRail.IntegrationTests.RailwayNetwork;

namespace SmartRail.IntegrationTests.Incidents;

[Collection(IncidentPostgresCollection.Name)]
public sealed class IncidentCreationCheckpointTests(PostgreSqlTestDatabaseFixture database, LocalOidcFixture oidc)
{
    [Fact]
    public async Task Signed_post_creates_persisted_incident_and_unconfigured_catalogue_creates_nothing()
    {
        await using var factory = await IncidentApiTestHost.CreateAsync(database, oidc,
            IncidentCreationTestSupport.ConfigureValidTestAuthorities);
        using var client = factory.CreateClient();
        const string subject = "f004-creation-checkpoint";
        await IncidentCreationTestSupport.GrantRoleAsync(factory.Services, subject, RoleCode.Controller);
        var stationId = await IncidentCreationTestSupport.AddPublishedStationAsync(factory.Services);

        using var success = await client.SendAsync(IncidentCreationTestSupport.Post(stationId.ToString(), token: oidc.CreateToken(subject), key: "checkpoint-success"));
        Assert.Equal(HttpStatusCode.Created, success.StatusCode);
        using var body = System.Text.Json.JsonDocument.Parse(await success.Content.ReadAsStringAsync());
        var incidentId = body.RootElement.GetProperty("incidentId").GetGuid();
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SmartRailDbContext>();
            Assert.True(await db.Incidents.AnyAsync(x => x.IncidentId == incidentId && x.ReporterSubject == subject));
        }

        await using var unconfiguredFactory = await IncidentApiTestHost.CreateAsync(database, oidc,
            IncidentCreationTestSupport.ConfigureTestOwnerVerifier);
        using var unconfiguredClient = unconfiguredFactory.CreateClient();
        const string unconfiguredSubject = "f004-creation-checkpoint-no-catalog";
        await IncidentCreationTestSupport.GrantRoleAsync(unconfiguredFactory.Services, unconfiguredSubject, RoleCode.Controller);
        var secondStationId = await IncidentCreationTestSupport.AddPublishedStationAsync(unconfiguredFactory.Services);
        int before;
        await using (var scope = unconfiguredFactory.Services.CreateAsyncScope())
            before = await scope.ServiceProvider.GetRequiredService<SmartRailDbContext>().Incidents.CountAsync();

        using var unavailable = await unconfiguredClient.SendAsync(IncidentCreationTestSupport.Post(secondStationId.ToString(),
            token: oidc.CreateToken(unconfiguredSubject), key: "checkpoint-unconfigured"));
        Assert.Equal(HttpStatusCode.ServiceUnavailable, unavailable.StatusCode);
        await using (var scope = unconfiguredFactory.Services.CreateAsyncScope())
            Assert.Equal(before, await scope.ServiceProvider.GetRequiredService<SmartRailDbContext>().Incidents.CountAsync());
    }
}
