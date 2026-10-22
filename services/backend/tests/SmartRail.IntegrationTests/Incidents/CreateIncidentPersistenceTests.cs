using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartRail.Application.Incidents.Persistence;
using SmartRail.Domain.Identity;
using SmartRail.Domain.Incidents;
using SmartRail.Domain.Incidents.Entities;
using SmartRail.Infrastructure.RailwayNetwork;
using SmartRail.IntegrationTests.Identity;
using SmartRail.IntegrationTests.RailwayNetwork;

namespace SmartRail.IntegrationTests.Incidents;

[Collection(IncidentPostgresCollection.Name)]
public sealed class CreateIncidentPersistenceTests(PostgreSqlTestDatabaseFixture database, LocalOidcFixture oidc)
{
    [Fact]
    public async Task Signed_controller_report_persists_server_identity_time_links_revision_and_retry_result()
    {
        await using var factory = await IncidentApiTestHost.CreateAsync(database, oidc,
            IncidentCreationTestSupport.ConfigureValidTestAuthorities);
        using var client = factory.CreateClient();
        const string subject = "f004-create-controller";
        await IncidentCreationTestSupport.GrantRoleAsync(factory.Services, subject, RoleCode.Controller);
        var stationId = await IncidentCreationTestSupport.AddPublishedStationAsync(factory.Services);

        using var response = await client.SendAsync(IncidentCreationTestSupport.Post(stationId.ToString(), token: oidc.CreateToken(subject)));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var incidentId = body.RootElement.GetProperty("incidentId").GetGuid();
        Assert.Equal("Reported", body.RootElement.GetProperty("state").GetString());
        Assert.Equal("IncidentManagement", body.RootElement.GetProperty("source").GetString());
        Assert.True(body.RootElement.GetProperty("reportedAt").GetDateTimeOffset() <= DateTimeOffset.UtcNow);
        Assert.False(body.RootElement.TryGetProperty("reporterSubject", out _));
        Assert.False(body.RootElement.TryGetProperty("actorSubject", out _));

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartRailDbContext>();
        var incident = await db.Incidents.AsNoTracking().SingleAsync(x => x.IncidentId == incidentId);
        Assert.Equal("f004-create-controller", incident.ReporterSubject);
        Assert.Equal(1, incident.Revision);
        Assert.Equal("TEST_TRACK_OBSTRUCTION", incident.CategoryCode);
        Assert.Single(await db.IncidentAssociations.AsNoTracking().Where(x => x.IncidentId == incidentId).ToListAsync());
        Assert.Single(await db.IncidentRevisions.AsNoTracking().Where(x => x.IncidentId == incidentId).ToListAsync());
        var retry = await db.IncidentIdempotencyRecords.AsNoTracking().SingleAsync(x => x.IncidentId == incidentId);
        Assert.Equal("POST /api/incidents", retry.Operation);
        Assert.Equal(201, retry.HttpStatusCode);
        Assert.DoesNotContain(subject, retry.ResponseJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Unknown_owner_reference_returns_not_found_and_no_incident_is_written()
    {
        await using var factory = await IncidentApiTestHost.CreateAsync(database, oidc,
            IncidentCreationTestSupport.ConfigureValidTestAuthorities);
        using var client = factory.CreateClient();
        const string subject = "f004-create-unknown-owner";
        await IncidentCreationTestSupport.GrantRoleAsync(factory.Services, subject, RoleCode.Controller);
        var before = await CountIncidentsAsync(factory.Services);

        using var response = await client.SendAsync(IncidentCreationTestSupport.Post(Guid.NewGuid().ToString(), token: oidc.CreateToken(subject)));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(before, await CountIncidentsAsync(factory.Services));
    }

    [Fact]
    public async Task Globally_granted_controller_can_create_service_only_report()
    {
        await using var factory = await IncidentApiTestHost.CreateAsync(database, oidc,
            IncidentCreationTestSupport.ConfigureValidTestAuthorities);
        using var client = factory.CreateClient();
        const string subject = "f004-service-only-controller";
        await IncidentCreationTestSupport.GrantRoleAsync(factory.Services, subject, RoleCode.Controller);
        var serviceId = await IncidentCreationTestSupport.AddPublishedServiceAsync(factory.Services);

        using var response = await client.SendAsync(IncidentCreationTestSupport.PostService(serviceId.ToString(), token: oidc.CreateToken(subject)));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(serviceId, body.RootElement.GetProperty("associations")[0].GetProperty("resourceId").GetGuid());
    }

    [Fact]
    public async Task Denied_role_and_missing_credentials_do_not_create_incidents()
    {
        await using var factory = await IncidentApiTestHost.CreateAsync(database, oidc,
            IncidentCreationTestSupport.ConfigureValidTestAuthorities);
        using var client = factory.CreateClient();
        var stationId = await IncidentCreationTestSupport.AddPublishedStationAsync(factory.Services);
        var before = await CountIncidentsAsync(factory.Services);

        using (var denied = await client.SendAsync(IncidentCreationTestSupport.Post(stationId.ToString(), token: oidc.CreateToken("no-create-role"))))
            Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        using (var unauthorized = await client.SendAsync(IncidentCreationTestSupport.Post(stationId.ToString())))
            Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);

        Assert.Equal(before, await CountIncidentsAsync(factory.Services));
    }

    [Fact]
    public async Task Database_failure_rolls_back_incident_revision_association_and_idempotency_record()
    {
        await using var factory = await IncidentApiTestHost.CreateAsync(database, oidc);
        await using var scope = factory.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var db = services.GetRequiredService<SmartRailDbContext>();
        var repository = services.GetRequiredService<IIncidentRepository>();
        var unitOfWork = services.GetRequiredService<IIncidentUnitOfWork>();
        var now = DateTimeOffset.UtcNow;
        var incident = new Incident(Guid.NewGuid(), "TEST_ROLLBACK", "Rollback probe", "verified-actor", now);
        var association = new IncidentAssociation(Guid.NewGuid(), incident.IncidentId, IncidentAssociationKind.Station,
            Guid.NewGuid(), now, 1); // Deliberately missing Station FK causes PostgreSQL write failure.
        var evidence = "[]";
        var revision = new IncidentRevision(incident.IncidentId, 1, incident.State, "verified-actor", now, evidence);
        var retry = new IncidentIdempotencyRecord(Guid.NewGuid(), "verified-actor", "POST /api/incidents", "rollback-key",
            "AA", incident.IncidentId, 201, "{}", now);

        await Assert.ThrowsAnyAsync<Exception>(() => unitOfWork.ExecuteAsync(async cancellationToken =>
        {
            await repository.CreateAsync(incident, revision, [association], retry, cancellationToken);
            return true;
        }));

        db.ChangeTracker.Clear();
        Assert.False(await db.Incidents.AnyAsync(x => x.IncidentId == incident.IncidentId));
        Assert.False(await db.IncidentRevisions.AnyAsync(x => x.IncidentId == incident.IncidentId));
        Assert.False(await db.IncidentAssociations.AnyAsync(x => x.IncidentId == incident.IncidentId));
        Assert.False(await db.IncidentIdempotencyRecords.AnyAsync(x => x.Id == retry.Id));
    }

    private static async Task<int> CountIncidentsAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<SmartRailDbContext>().Incidents.CountAsync();
    }
}
