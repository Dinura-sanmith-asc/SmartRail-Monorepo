using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartRail.Domain.Identity;
using SmartRail.Infrastructure.RailwayNetwork;
using SmartRail.IntegrationTests.Identity;
using SmartRail.IntegrationTests.RailwayNetwork;

namespace SmartRail.IntegrationTests.Incidents;

[Collection(IncidentPostgresCollection.Name)]
public sealed class IncidentAssociationHistoryTests(PostgreSqlTestDatabaseFixture database, LocalOidcFixture oidc)
{
    [Fact]
    public async Task Journey_cancellation_preserves_incident_history_until_explicit_association_removal()
    {
        await using var factory = await IncidentOwnerIntegrationTestHost.CreateAsync(database, oidc);
        using var client = factory.CreateClient();
        var subject = "f004-history-controller-" + Guid.NewGuid().ToString("N");
        await IncidentOwnerTestSupport.GrantRoleAsync(factory.Services, subject, RoleCode.Controller);
        var token = oidc.CreateToken(subject);
        var journey = await IncidentOwnerIntegrationTestHost.InitializeJourneyAsync(factory.Services, "history-journey-" + Guid.NewGuid().ToString("N"));
        var incident = await IncidentOwnerTestSupport.CreateIncidentAsync(client, token, Guid.NewGuid().ToString("N"),
            [IncidentOwnerTestSupport.Link("Station", IncidentOwnerTestSupport.StationA), IncidentOwnerTestSupport.Link("Journey", journey)]);
        Guid journeyAssociationId;
        await using (var scope = factory.Services.CreateAsyncScope())
            journeyAssociationId = await scope.ServiceProvider.GetRequiredService<SmartRailDbContext>().IncidentAssociations
                .Where(x => x.IncidentId == incident && x.Kind == SmartRail.Domain.Incidents.IncidentAssociationKind.Journey)
                .Select(x => x.AssociationId).SingleAsync();

        await IncidentOwnerTestSupport.PublishJourneyChangeAsync(client, token, journey, 1, "Cancelled");
        using (var historical = new HttpRequestMessage(HttpMethod.Get, $"/api/incidents?journeyId={journey:D}"))
        {
            historical.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await client.SendAsync(historical);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal(incident, Assert.Single(json.RootElement.EnumerateArray()).GetProperty("incidentId").GetGuid());
        }

        using (var removal = await client.SendAsync(IncidentOwnerTestSupport.PatchIncident(incident, token,
                   "remove-history-link", 1, remove: [journeyAssociationId])))
            Assert.Equal(HttpStatusCode.OK, removal.StatusCode);
        using (var active = new HttpRequestMessage(HttpMethod.Get, $"/api/incidents?journeyId={journey:D}"))
        {
            active.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await client.SendAsync(active);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Empty(JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.EnumerateArray());
        }

        await using var verify = factory.Services.CreateAsyncScope();
        var db = verify.ServiceProvider.GetRequiredService<SmartRailDbContext>();
        var removed = await db.IncidentAssociations.AsNoTracking().SingleAsync(x => x.AssociationId == journeyAssociationId);
        Assert.False(removed.IsActive);
        Assert.Equal(2, removed.RemovedRevision);
        Assert.Single(await db.IncidentRevisions.AsNoTracking().Where(x => x.IncidentId == incident && x.Revision == 2).ToListAsync());
    }
}
