using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartRail.Application.Identity.Authorization;
using SmartRail.Application.Incidents.References;
using SmartRail.Application.JourneysStatus.Initialization;
using SmartRail.Domain.Identity;
using SmartRail.Infrastructure;
using SmartRail.Infrastructure.Identity;
using SmartRail.Infrastructure.Incidents.References;
using SmartRail.Infrastructure.JourneysStatus.Initialization;
using SmartRail.Infrastructure.RailwayNetwork;
using SmartRail.IntegrationTests.Identity;
using SmartRail.IntegrationTests.JourneysStatus.Fixtures;
using SmartRail.IntegrationTests.RailwayNetwork;

namespace SmartRail.IntegrationTests.Incidents;

[Collection(IncidentPostgresCollection.Name)]
public sealed class IncidentBoundaryRegressionTests(PostgreSqlTestDatabaseFixture database, LocalOidcFixture oidc)
{
    [Fact]
    public async Task Catering_owner_outage_does_not_break_incidents_and_public_journey_projection_stays_private()
    {
        await using var factory = await IncidentOwnerIntegrationTestHost.CreateAsync(database, oidc);
        using var client = factory.CreateClient();
        var subject = "f004-boundary-controller-" + Guid.NewGuid().ToString("N");
        await IncidentOwnerTestSupport.GrantRoleAsync(factory.Services, subject, RoleCode.Controller);
        var token = oidc.CreateToken(subject);
        var journey = await IncidentOwnerIntegrationTestHost.InitializeJourneyAsync(factory.Services, "boundary-" + Guid.NewGuid().ToString("N"));
        const string hostileText = "<script>window.secret='untrusted'</script>";
        var incident = await IncidentOwnerTestSupport.CreateIncidentAsync(client, token, Guid.NewGuid().ToString("N"),
            [IncidentOwnerTestSupport.Link("Station", IncidentOwnerTestSupport.StationA), IncidentOwnerTestSupport.Link("Journey", journey)], hostileText);

        using (var read = new System.Net.Http.HttpRequestMessage(HttpMethod.Get, $"/api/incidents?incidentId={incident:D}"))
        {
            read.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            using var response = await client.SendAsync(read);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal(hostileText, Assert.Single(json.RootElement.EnumerateArray()).GetProperty("description").GetString());
        }

        using (var publicJourney = await client.GetAsync($"/api/journeys/{journey:D}"))
        {
            Assert.Equal(HttpStatusCode.OK, publicJourney.StatusCode);
            using var json = JsonDocument.Parse(await publicJourney.Content.ReadAsStringAsync());
            var snapshot = json.RootElement;
            Assert.False(snapshot.TryGetProperty("incidents", out _));
            Assert.False(snapshot.TryGetProperty("description", out _));
            Assert.DoesNotContain(hostileText, snapshot.ToString(), StringComparison.Ordinal);
        }

        // This real infrastructure default intentionally reports unavailable Catering evidence;
        // incident create/read and lifecycle operations have no dependency on it.
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            Assert.IsType<UnavailableOwnerEvidence>(scope.ServiceProvider.GetRequiredService<IPassengerOrderOwnershipEvidence>());
            Assert.IsType<Feature001IncidentReferenceVerifier>(scope.ServiceProvider.GetRequiredService<IFeature001IncidentReferenceVerifier>());
            Assert.IsType<Feature003IncidentReferenceVerifier>(scope.ServiceProvider.GetRequiredService<IFeature003IncidentReferenceVerifier>());
        }
        using (var close = await client.SendAsync(IncidentOwnerTestSupport.PatchIncident(incident, token,
                   "boundary-close", 1, state: "In Progress")))
            Assert.Equal(HttpStatusCode.OK, close.StatusCode);
    }

    [Fact]
    public void Normal_infrastructure_keeps_journey_initialization_denied_and_owner_adapters_real_or_fail_closed()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{SmartRailDbContext.ConnectionStringName}"] = "Host=127.0.0.1;Port=5433;Database=unused;Username=test;Password=test"
        }).Build();
        var services = new ServiceCollection();
        services.AddSmartRailInfrastructure(configuration);
        using var provider = services.BuildServiceProvider();
        Assert.IsType<DenyAllJourneyInitializationAuthority>(provider.GetRequiredService<IJourneyInitializationAuthority>());
        Assert.IsType<Feature001IncidentReferenceVerifier>(provider.GetRequiredService<IFeature001IncidentReferenceVerifier>());
        Assert.IsType<Feature003IncidentReferenceVerifier>(provider.GetRequiredService<IFeature003IncidentReferenceVerifier>());
        Assert.IsType<UnavailableOwnerEvidence>(provider.GetRequiredService<IPassengerOrderOwnershipEvidence>());
        Assert.DoesNotContain(services, item => item.ImplementationType?.Assembly == typeof(IncidentBoundaryRegressionTests).Assembly);
    }
}
