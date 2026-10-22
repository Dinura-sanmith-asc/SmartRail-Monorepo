using System.Net;
using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartRail.Domain.Identity;
using SmartRail.Infrastructure.RailwayNetwork;
using SmartRail.IntegrationTests.Identity;
using SmartRail.IntegrationTests.RailwayNetwork;

namespace SmartRail.IntegrationTests.Incidents;

[Collection(IncidentPostgresCollection.Name)]
public sealed class IncidentMultiStationAuthorizationTests(PostgreSqlTestDatabaseFixture database, LocalOidcFixture oidc)
{
    [Fact]
    public async Task Station_staff_must_cover_every_active_station_and_journey_links_do_not_expand_scope()
    {
        await using var factory = await IncidentOwnerIntegrationTestHost.CreateAsync(database, oidc);
        using var client = factory.CreateClient();
        var staff = "f004-scoped-staff-" + Guid.NewGuid().ToString("N");
        var controller = "f004-global-controller-" + Guid.NewGuid().ToString("N");
        await IncidentOwnerTestSupport.GrantRoleAsync(factory.Services, staff, RoleCode.StationStaff);
        await IncidentOwnerTestSupport.GrantRoleAsync(factory.Services, controller, RoleCode.Controller);
        await IncidentOwnerTestSupport.GrantStationScopeAsync(factory.Services, staff, IncidentOwnerTestSupport.StationA);
        var journeyId = await IncidentOwnerIntegrationTestHost.InitializeJourneyAsync(factory.Services, "scoped-journey-" + Guid.NewGuid().ToString("N"));

        var staffToken = oidc.CreateToken(staff);
        var incidentId = await IncidentOwnerTestSupport.CreateIncidentAsync(client, staffToken, Guid.NewGuid().ToString("N"),
        [
            IncidentOwnerTestSupport.Link("Station", IncidentOwnerTestSupport.StationA),
            IncidentOwnerTestSupport.Link("Journey", journeyId)
        ]);
        var outsideStation = await IncidentOwnerTestSupport.AddPublishedStationAsync(factory.Services);
        using (var denied = await client.SendAsync(IncidentOwnerTestSupport.PatchIncident(incidentId, staffToken,
                   "add-out-of-scope-station", 1, add: [IncidentOwnerTestSupport.Link("Station", outsideStation)])))
            Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);

        await IncidentOwnerTestSupport.GrantStationScopeAsync(factory.Services, staff, outsideStation);
        using (var allowed = await client.SendAsync(IncidentOwnerTestSupport.PatchIncident(incidentId, staffToken,
                   "add-now-in-scope-station", 1, add: [IncidentOwnerTestSupport.Link("Station", outsideStation)])))
            Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);

        using var controllerRead = new HttpRequestMessage(HttpMethod.Get, $"/api/incidents?incidentId={incidentId:D}");
        controllerRead.Headers.Authorization = new AuthenticationHeaderValue("Bearer", oidc.CreateToken(controller));
        using var visible = await client.SendAsync(controllerRead);
        Assert.Equal(HttpStatusCode.OK, visible.StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        Assert.Equal(2, await scope.ServiceProvider.GetRequiredService<SmartRailDbContext>().IncidentAssociations
            .AsNoTracking().CountAsync(x => x.IncidentId == incidentId && x.Kind == SmartRail.Domain.Incidents.IncidentAssociationKind.Station && x.RemovedRevision == null));
    }
}
