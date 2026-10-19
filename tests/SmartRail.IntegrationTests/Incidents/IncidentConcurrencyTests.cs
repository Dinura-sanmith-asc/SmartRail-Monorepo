using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartRail.Domain.Identity;
using SmartRail.Domain.Incidents;
using SmartRail.Infrastructure.RailwayNetwork;
using SmartRail.IntegrationTests.Identity;
using SmartRail.IntegrationTests.RailwayNetwork;

namespace SmartRail.IntegrationTests.Incidents;

[Collection(IncidentPostgresCollection.Name)]
public sealed class IncidentConcurrencyTests(PostgreSqlTestDatabaseFixture database, LocalOidcFixture oidc)
{
    [Fact]
    public async Task Two_updates_from_the_same_revision_allow_at_most_one_mutation()
    {
        await using var factory = await IncidentApiTestHost.CreateAsync(database, oidc,
            IncidentCreationTestSupport.ConfigureValidTestAuthorities);
        using var client = factory.CreateClient();
        var subject = "f004-concurrency-" + Guid.NewGuid().ToString("N");
        await IncidentCreationTestSupport.GrantRoleAsync(factory.Services, subject, RoleCode.Controller);
        var stationId = await IncidentCreationTestSupport.AddPublishedStationAsync(factory.Services);
        var incidentId = await IncidentReadTestSupport.CreateIncidentAsync(client, oidc, subject, stationId,
            "concurrency-create-" + Guid.NewGuid());
        var token = oidc.CreateToken(subject);
        using var updateA = IncidentReadTestSupport.Patch(incidentId, 1, "In Progress", token, "concurrent-a-" + Guid.NewGuid());
        using var updateB = IncidentReadTestSupport.Patch(incidentId, 1, "In Progress", token, "concurrent-b-" + Guid.NewGuid());

        var responses = await Task.WhenAll(client.SendAsync(updateA), client.SendAsync(updateB));
        using var responseA = responses[0];
        using var responseB = responses[1];

        var bodyA = await responseA.Content.ReadAsStringAsync();
        var bodyB = await responseB.Content.ReadAsStringAsync();
        Assert.Contains(new[] { responseA.StatusCode, responseB.StatusCode }, status => status == HttpStatusCode.OK);
        Assert.True(new[] { responseA.StatusCode, responseB.StatusCode }.Contains(HttpStatusCode.Conflict),
            $"Expected one conflict response. Actual: {(int)responseA.StatusCode} {bodyA}; {(int)responseB.StatusCode} {bodyB}");
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartRailDbContext>();
        var incident = await db.Incidents.AsNoTracking().SingleAsync(x => x.IncidentId == incidentId);
        Assert.Equal(IncidentState.InProgress, incident.State);
        Assert.Equal(2, incident.Revision);
        Assert.Equal(2, await db.IncidentRevisions.CountAsync(x => x.IncidentId == incidentId));
        Assert.Equal(2, await db.IncidentIdempotencyRecords.CountAsync(x => x.IncidentId == incidentId));
    }
}
