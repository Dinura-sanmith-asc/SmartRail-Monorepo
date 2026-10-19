using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartRail.Domain.Identity;
using SmartRail.Domain.Identity.Entities;
using SmartRail.Infrastructure.RailwayNetwork;
using SmartRail.IntegrationTests.Identity;
using SmartRail.IntegrationTests.RailwayNetwork;

namespace SmartRail.IntegrationTests.Incidents;

[Collection(IncidentPostgresCollection.Name)]
public sealed class CreateIncidentIdempotencyTests(PostgreSqlTestDatabaseFixture database, LocalOidcFixture oidc)
{
    [Fact]
    public async Task Concurrent_identical_posts_create_once_and_return_same_result()
    {
        await using var factory = await IncidentApiTestHost.CreateAsync(database, oidc,
            IncidentCreationTestSupport.ConfigureValidTestAuthorities);
        using var client = factory.CreateClient();
        const string subject = "f004-create-race-controller";
        await IncidentCreationTestSupport.GrantRoleAsync(factory.Services, subject, RoleCode.Controller);
        var stationId = await IncidentCreationTestSupport.AddPublishedStationAsync(factory.Services);
        var token = oidc.CreateToken(subject);

        using var requestA = IncidentCreationTestSupport.Post(stationId.ToString(), token: token, key: "f004-race-key");
        using var requestB = IncidentCreationTestSupport.Post(stationId.ToString(), token: token, key: "f004-race-key");
        var responses = await Task.WhenAll(client.SendAsync(requestA), client.SendAsync(requestB));
        using var responseA = responses[0];
        using var responseB = responses[1];

        Assert.Equal(HttpStatusCode.Created, responseA.StatusCode);
        Assert.Equal(HttpStatusCode.Created, responseB.StatusCode);
        var jsonA = await responseA.Content.ReadAsStringAsync();
        var jsonB = await responseB.Content.ReadAsStringAsync();
        using var first = System.Text.Json.JsonDocument.Parse(jsonA);
        using var second = System.Text.Json.JsonDocument.Parse(jsonB);
        Assert.Equal(first.RootElement.GetProperty("incidentId").GetGuid(), second.RootElement.GetProperty("incidentId").GetGuid());

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartRailDbContext>();
        Assert.Equal(1, await db.Incidents.CountAsync(x => x.ReporterSubject == subject));
        Assert.Equal(1, await db.IncidentIdempotencyRecords.CountAsync(x => x.ActorSubject == subject && x.IdempotencyKey == "f004-race-key"));
    }

    [Fact]
    public async Task Same_key_with_different_payload_conflicts_without_second_write()
    {
        await using var factory = await IncidentApiTestHost.CreateAsync(database, oidc,
            IncidentCreationTestSupport.ConfigureValidTestAuthorities);
        using var client = factory.CreateClient();
        const string subject = "f004-create-conflict-controller";
        await IncidentCreationTestSupport.GrantRoleAsync(factory.Services, subject, RoleCode.Controller);
        var stationId = await IncidentCreationTestSupport.AddPublishedStationAsync(factory.Services);
        var token = oidc.CreateToken(subject);

        using var first = await client.SendAsync(IncidentCreationTestSupport.Post(stationId.ToString(), token: token, key: "f004-conflict-key"));
        using var second = await client.SendAsync(IncidentCreationTestSupport.Post(stationId.ToString(), description: "Changed report", token: token, key: "f004-conflict-key"));

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        Assert.Equal(1, await scope.ServiceProvider.GetRequiredService<SmartRailDbContext>().Incidents.CountAsync(x => x.ReporterSubject == subject));
    }

    [Fact]
    public async Task Replay_after_role_revocation_is_denied_before_stored_result_is_read()
    {
        await using var factory = await IncidentApiTestHost.CreateAsync(database, oidc,
            IncidentCreationTestSupport.ConfigureValidTestAuthorities);
        using var client = factory.CreateClient();
        const string subject = "f004-create-revoked-controller";
        var assignmentId = await IncidentCreationTestSupport.GrantRoleAsync(factory.Services, subject, RoleCode.Controller);
        var stationId = await IncidentCreationTestSupport.AddPublishedStationAsync(factory.Services);
        var token = oidc.CreateToken(subject);

        using (var created = await client.SendAsync(IncidentCreationTestSupport.Post(stationId.ToString(), token: token, key: "f004-revoke-key")))
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        await RevokeRoleAsync(factory.Services, assignmentId);
        using var replay = await client.SendAsync(IncidentCreationTestSupport.Post(stationId.ToString(), token: token, key: "f004-revoke-key"));

        Assert.Equal(HttpStatusCode.Forbidden, replay.StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        Assert.Equal(1, await scope.ServiceProvider.GetRequiredService<SmartRailDbContext>().Incidents.CountAsync(x => x.ReporterSubject == subject));
    }

    private static async Task RevokeRoleAsync(IServiceProvider services, Guid assignmentId)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartRailDbContext>();
        var assignment = await db.RoleAssignments.SingleAsync(x => x.Id == assignmentId);
        assignment.Revoke("f004-test-approver", DateTimeOffset.UtcNow);
        db.RoleAssignmentAudits.Add(RoleAssignmentAudit.ForRevocation(Guid.NewGuid(), assignment));
        await db.SaveChangesAsync();
    }
}
