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
public sealed class NoAutomaticJourneyPublicationTests(PostgreSqlTestDatabaseFixture database, LocalOidcFixture oidc)
{
    [Fact]
    public async Task Incident_mutations_never_publish_journey_status_and_cancellation_remains_a_separate_authorized_operation()
    {
        await using var factory = await IncidentOwnerIntegrationTestHost.CreateAsync(database, oidc);
        using var client = factory.CreateClient();
        var subject = "f004-no-auto-controller-" + Guid.NewGuid().ToString("N");
        await IncidentOwnerTestSupport.GrantRoleAsync(factory.Services, subject, RoleCode.Controller);
        var token = oidc.CreateToken(subject);
        var journey = await IncidentOwnerIntegrationTestHost.InitializeJourneyAsync(factory.Services, "no-auto-" + Guid.NewGuid().ToString("N"));
        var incident = await IncidentOwnerTestSupport.CreateIncidentAsync(client, token, Guid.NewGuid().ToString("N"),
            [IncidentOwnerTestSupport.Link("Station", IncidentOwnerTestSupport.StationA), IncidentOwnerTestSupport.Link("Journey", journey)]);
        foreach (var (revision, state) in new[] { (1, "In Progress"), (2, "Resolved"), (3, "Closed") })
        {
            using var patch = await client.SendAsync(IncidentOwnerTestSupport.PatchIncident(incident, token,
                $"lifecycle-without-journey-publication-{revision}", revision, state));
            Assert.Equal(HttpStatusCode.OK, patch.StatusCode);
        }

        using (var updatesResponse = await client.GetAsync($"/api/journeys/{journey:D}/updates"))
        {
            updatesResponse.EnsureSuccessStatusCode();
            Assert.Empty(JsonDocument.Parse(await updatesResponse.Content.ReadAsStringAsync()).RootElement.EnumerateArray());
        }
        await using (var scope = factory.Services.CreateAsyncScope())
            Assert.Equal(1, await scope.ServiceProvider.GetRequiredService<SmartRailDbContext>().Journeys
                .Where(x => x.JourneyInstanceId == journey).Select(x => x.CurrentRevision).SingleAsync());

        // Only an explicit separately authorized Feature 003 operation can publish cancellation.
        await IncidentOwnerTestSupport.PublishJourneyChangeAsync(client, token, journey, 1, "Cancelled");
        await using var verify = factory.Services.CreateAsyncScope();
        Assert.Equal(2, await verify.ServiceProvider.GetRequiredService<SmartRailDbContext>().Journeys
            .Where(x => x.JourneyInstanceId == journey).Select(x => x.CurrentRevision).SingleAsync());
    }
}
