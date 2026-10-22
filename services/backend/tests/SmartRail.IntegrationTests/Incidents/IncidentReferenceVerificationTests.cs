using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartRail.Domain.Identity;
using SmartRail.Infrastructure.RailwayNetwork;
using SmartRail.IntegrationTests.Identity;
using SmartRail.IntegrationTests.JourneysStatus.Fixtures;
using SmartRail.IntegrationTests.RailwayNetwork;

namespace SmartRail.IntegrationTests.Incidents;

[Collection(IncidentPostgresCollection.Name)]
public sealed class IncidentReferenceVerificationTests(PostgreSqlTestDatabaseFixture database, LocalOidcFixture oidc)
{
    [Fact]
    public async Task Unknown_stale_and_malformed_owner_references_are_rejected_without_incident_writes()
    {
        await using var factory = await IncidentOwnerIntegrationTestHost.CreateAsync(database, oidc);
        using var client = factory.CreateClient();
        const string subject = "f004-owner-negative-" + nameof(Unknown_stale_and_malformed_owner_references_are_rejected_without_incident_writes);
        await IncidentOwnerTestSupport.GrantRoleAsync(factory.Services, subject, RoleCode.Controller);
        var journeyId = await IncidentOwnerIntegrationTestHost.InitializeJourneyAsync(factory.Services, "cancelled-" + Guid.NewGuid().ToString("N"));
        var activeJourneyId = await IncidentOwnerIntegrationTestHost.InitializeJourneyAsync(factory.Services, "stale-disruption-" + Guid.NewGuid().ToString("N"));
        var token = oidc.CreateToken(subject);
        await IncidentOwnerTestSupport.PublishJourneyChangeAsync(client, token, journeyId, 1, "Cancelled");
        var activeUpdateId = await IncidentOwnerTestSupport.PublishJourneyChangeAsync(client, token, activeJourneyId, 1, "Delayed");

        using var missing = await PostAsync(client, token, "missing-owner", [IncidentOwnerTestSupport.Link("Station", Guid.NewGuid())]);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        using var unknownService = await PostAsync(client, token, "unknown-service", [IncidentOwnerTestSupport.Link("Service", Guid.NewGuid())]);
        Assert.Equal(HttpStatusCode.NotFound, unknownService.StatusCode);
        using var stale = await PostAsync(client, token, "stale-owner", [IncidentOwnerTestSupport.Link("Station", IncidentOwnerTestSupport.StationA), IncidentOwnerTestSupport.Link("Journey", journeyId)]);
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        using var malformed = await PostAsync(client, token, "malformed-owner", [IncidentOwnerTestSupport.Link("Journey", Guid.Empty)]);
        Assert.Equal(HttpStatusCode.BadRequest, malformed.StatusCode);
        using var malformedDisruption = await PostAsync(client, token, "malformed-disruption",
            [IncidentOwnerTestSupport.Link("Station", IncidentOwnerTestSupport.StationA), IncidentOwnerTestSupport.Link("Disruption", activeJourneyId)]);
        Assert.Equal(HttpStatusCode.BadRequest, malformedDisruption.StatusCode);
        using var staleDisruption = await PostAsync(client, token, "stale-disruption",
            [IncidentOwnerTestSupport.Link("Station", IncidentOwnerTestSupport.StationA), IncidentOwnerTestSupport.Link("Disruption", activeJourneyId, activeUpdateId, 99)]);
        Assert.Equal(HttpStatusCode.Conflict, staleDisruption.StatusCode);

        await using (var serviceScope = factory.Services.CreateAsyncScope())
        {
            var db = serviceScope.ServiceProvider.GetRequiredService<SmartRailDbContext>();
            db.TrainServices.Single(item => item.ServiceId == IncidentOwnerTestSupport.Service).Unpublish();
            await db.SaveChangesAsync();
        }
        using var unpublishedService = await PostAsync(client, token, "unpublished-service", [IncidentOwnerTestSupport.Link("Service", IncidentOwnerTestSupport.Service)]);
        Assert.Equal(HttpStatusCode.Conflict, unpublishedService.StatusCode);

        await using var scope = factory.Services.CreateAsyncScope();
        Assert.Empty(await scope.ServiceProvider.GetRequiredService<SmartRailDbContext>().Incidents.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task Required_owner_store_outage_maps_to_503_and_does_not_persist_unverified_link()
    {
        await using var factory = await IncidentOwnerIntegrationTestHost.CreateAsync(database, oidc);
        using var client = factory.CreateClient();
        const string subject = "f004-owner-unavailable-" + nameof(Required_owner_store_outage_maps_to_503_and_does_not_persist_unverified_link);
        await IncidentOwnerTestSupport.GrantRoleAsync(factory.Services, subject, RoleCode.Controller);
        var journeyId = await IncidentOwnerIntegrationTestHost.InitializeJourneyAsync(factory.Services, "unavailable-" + Guid.NewGuid().ToString("N"));
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SmartRailDbContext>();
            await db.Database.ExecuteSqlRawAsync("DROP TABLE journey_revision CASCADE");
        }

        using var response = await PostAsync(client, oidc.CreateToken(subject), "owner-outage",
            [IncidentOwnerTestSupport.Link("Station", IncidentOwnerTestSupport.StationA), IncidentOwnerTestSupport.Link("Journey", journeyId)]);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        await using var verify = factory.Services.CreateAsyncScope();
        Assert.Empty(await verify.ServiceProvider.GetRequiredService<SmartRailDbContext>().Incidents.AsNoTracking().ToListAsync());
    }

    private static Task<HttpResponseMessage> PostAsync(HttpClient client, string token, string key, object[] associations)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/incidents")
        {
            Content = JsonContent.Create(new { categoryCode = IncidentTestCategories.TrackObstruction,
                description = "Reference verification test", associations })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Idempotency-Key", key);
        return client.SendAsync(request);
    }
}
