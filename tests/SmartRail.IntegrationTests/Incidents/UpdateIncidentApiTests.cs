using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using SmartRail.Application.Identity.Persistence;
using SmartRail.Domain.Identity;
using SmartRail.Domain.Incidents;
using SmartRail.Infrastructure.RailwayNetwork;
using SmartRail.IntegrationTests.Identity;
using SmartRail.IntegrationTests.RailwayNetwork;

namespace SmartRail.IntegrationTests.Incidents;

[Collection(IncidentPostgresCollection.Name)]
public sealed class UpdateIncidentApiTests(PostgreSqlTestDatabaseFixture database, LocalOidcFixture oidc)
{
    [Fact]
    public async Task Signed_patch_status_errors_are_safe_and_denied_updates_do_not_change_data()
    {
        await using var factory = await IncidentApiTestHost.CreateAsync(database, oidc,
            IncidentCreationTestSupport.ConfigureValidTestAuthorities);
        using var client = factory.CreateClient();
        var subject = "f004-patch-controller-" + Guid.NewGuid().ToString("N");
        await IncidentCreationTestSupport.GrantRoleAsync(factory.Services, subject, RoleCode.Controller);
        var stationId = await IncidentCreationTestSupport.AddPublishedStationAsync(factory.Services);
        var incidentId = await IncidentReadTestSupport.CreateIncidentAsync(client, oidc, subject, stationId,
            "patch-api-create-" + Guid.NewGuid());
        var token = oidc.CreateToken(subject);

        using (var noToken = new HttpRequestMessage(HttpMethod.Patch, $"/api/incidents/{incidentId}"))
        using (var response = await client.SendAsync(noToken))
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        using (var invalidToken = IncidentReadTestSupport.Patch(incidentId, 1, "In Progress", "not-a-jwt", "invalid-token"))
        using (var response = await client.SendAsync(invalidToken))
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        using (var malformed = new HttpRequestMessage(HttpMethod.Patch, $"/api/incidents/{incidentId}")
        {
            Content = new StringContent("{bad", Encoding.UTF8, "application/json")
        })
        {
            malformed.Headers.Add("Idempotency-Key", "malformed-patch");
            malformed.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await client.SendAsync(malformed);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        }
        using (var invalidState = await client.SendAsync(IncidentReadTestSupport.Patch(incidentId, 1, "Cancelled", token, "invalid-state")))
            Assert.Equal(HttpStatusCode.BadRequest, invalidState.StatusCode);
        using (var unknownIncident = await client.SendAsync(IncidentReadTestSupport.Patch(Guid.NewGuid(), 1, "In Progress", token, "unknown-incident")))
            Assert.Equal(HttpStatusCode.NotFound, unknownIncident.StatusCode);

        var deniedSubject = "f004-patch-no-role-" + Guid.NewGuid().ToString("N");
        using (var denied = await client.SendAsync(IncidentReadTestSupport.Patch(incidentId, 1, "In Progress",
                   oidc.CreateToken(deniedSubject), "unauthorized-patch")))
        {
            Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
            var body = await denied.Content.ReadAsStringAsync();
            Assert.DoesNotContain(subject, body, StringComparison.Ordinal);
            Assert.DoesNotContain("exception", body, StringComparison.OrdinalIgnoreCase);
        }

        using (var noop = await client.SendAsync(IncidentReadTestSupport.Patch(incidentId, 1, "Reported", token, "noop-patch")))
            Assert.Equal(HttpStatusCode.BadRequest, noop.StatusCode);
        using (var updated = await client.SendAsync(IncidentReadTestSupport.Patch(incidentId, 1, "In Progress", token, "first-patch")))
            Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        using (var stale = await client.SendAsync(IncidentReadTestSupport.Patch(incidentId, 1, "Resolved", token, "stale-patch")))
            Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartRailDbContext>();
        var incident = await db.Incidents.AsNoTracking().SingleAsync(x => x.IncidentId == incidentId);
        Assert.Equal(IncidentState.InProgress, incident.State);
        Assert.Equal(2, incident.Revision);
    }

    [Fact]
    public async Task Unavailable_current_authority_returns_503_without_mutation()
    {
        await using var factory = await IncidentApiTestHost.CreateAsync(database, oidc, services =>
        {
            IncidentCreationTestSupport.ConfigureValidTestAuthorities(services);
            services.RemoveAll<IRoleAssignmentRepository>();
            services.AddScoped<IRoleAssignmentRepository, UnavailableRoles>();
        });
        using var client = factory.CreateClient();
        var subject = "f004-patch-authority-down-" + Guid.NewGuid().ToString("N");
        var stationId = await IncidentCreationTestSupport.AddPublishedStationAsync(factory.Services);
        var incidentId = await SeedIncidentAsync(factory.Services, subject, stationId);
        using var response = await client.SendAsync(IncidentReadTestSupport.Patch(incidentId, 1, "In Progress",
            oidc.CreateToken(subject), "unavailable-authority-patch"));
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var json = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("exception", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("connection", json, StringComparison.OrdinalIgnoreCase);

        await using var scope = factory.Services.CreateAsyncScope();
        var incident = await scope.ServiceProvider.GetRequiredService<SmartRailDbContext>().Incidents
            .AsNoTracking().SingleAsync(x => x.IncidentId == incidentId);
        Assert.Equal(IncidentState.Reported, incident.State);
        Assert.Equal(1, incident.Revision);
    }

    private static async Task<Guid> SeedIncidentAsync(IServiceProvider services, string actor, Guid stationId)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartRailDbContext>();
        var now = DateTimeOffset.UtcNow;
        var incident = new SmartRail.Domain.Incidents.Entities.Incident(Guid.NewGuid(), IncidentTestCategories.TrackObstruction,
            "Lifecycle authority test", actor, now);
        var association = new SmartRail.Domain.Incidents.Entities.IncidentAssociation(Guid.NewGuid(), incident.IncidentId,
            SmartRail.Domain.Incidents.IncidentAssociationKind.Station, stationId, now, 1);
        db.Incidents.Add(incident);
        db.IncidentAssociations.Add(association);
        db.IncidentRevisions.Add(new SmartRail.Domain.Incidents.Entities.IncidentRevision(incident.IncidentId, 1,
            incident.State, actor, now, "[]"));
        await db.SaveChangesAsync();
        return incident.IncidentId;
    }

    [Fact]
    public async Task Closed_incident_is_visible_through_authorized_get_only_and_exact_three_routes_are_registered()
    {
        await using var factory = await IncidentApiTestHost.CreateAsync(database, oidc,
            IncidentCreationTestSupport.ConfigureValidTestAuthorities);
        using var client = factory.CreateClient();
        var controller = "f004-patch-closed-read-" + Guid.NewGuid().ToString("N");
        await IncidentCreationTestSupport.GrantRoleAsync(factory.Services, controller, RoleCode.Controller);
        var stationId = await IncidentCreationTestSupport.AddPublishedStationAsync(factory.Services);
        var incidentId = await IncidentReadTestSupport.CreateIncidentAsync(client, oidc, controller, stationId,
            "closed-read-create-" + Guid.NewGuid());
        var token = oidc.CreateToken(controller);
        foreach (var (revision, state) in new[] { (1, "In Progress"), (2, "Resolved"), (3, "Closed") })
        {
            using var response = await client.SendAsync(IncidentReadTestSupport.Patch(incidentId, revision, state,
                token, $"closed-read-{revision}-{Guid.NewGuid():N}"));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        using var authorized = await client.SendAsync(IncidentReadTestSupport.Get(token, $"?incidentId={incidentId}"));
        Assert.Equal(HttpStatusCode.OK, authorized.StatusCode);
        using var authorizedJson = JsonDocument.Parse(await authorized.Content.ReadAsStringAsync());
        var closed = Assert.Single(authorizedJson.RootElement.GetProperty("items").EnumerateArray());
        Assert.Equal(incidentId, closed.GetProperty("incidentId").GetGuid());
        Assert.Equal("Closed", closed.GetProperty("state").GetString());
        Assert.Equal(4, closed.GetProperty("revision").GetInt32());

        using var unauthorized = await client.SendAsync(IncidentReadTestSupport.Get(oidc.CreateToken("no-role-" + Guid.NewGuid())));
        Assert.Equal(HttpStatusCode.Forbidden, unauthorized.StatusCode);
        Assert.DoesNotContain(incidentId.ToString(), await unauthorized.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);

        var incidentRoutes = factory.Services.GetServices<EndpointDataSource>()
            .SelectMany(source => source.Endpoints.OfType<RouteEndpoint>())
            .Where(endpoint => endpoint.RoutePattern.RawText?.Contains("api/incidents", StringComparison.OrdinalIgnoreCase) == true)
            .Select(endpoint => (Path: endpoint.RoutePattern.RawText, Methods: endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? []))
            .ToArray();
        Assert.Equal(3, incidentRoutes.Length);
        Assert.Contains(incidentRoutes, endpoint => endpoint.Methods.Contains("GET") && endpoint.Path!.TrimEnd('/').EndsWith("api/incidents", StringComparison.Ordinal));
        Assert.Contains(incidentRoutes, endpoint => endpoint.Methods.Contains("POST") && endpoint.Path!.TrimEnd('/').EndsWith("api/incidents", StringComparison.Ordinal));
        Assert.Contains(incidentRoutes, endpoint => endpoint.Methods.Contains("PATCH") && endpoint.Path!.EndsWith("api/incidents/{id:guid}", StringComparison.Ordinal));
    }

    private sealed class UnavailableRoles : IRoleAssignmentRepository
    {
        public Task<IReadOnlyList<RoleCode>> GetActiveRolesAsync(string verifiedCognitoSubject, CancellationToken cancellationToken = default) => throw new InvalidOperationException("test-only role authority unavailable");
        public Task<IReadOnlyList<SmartRail.Domain.Identity.Entities.RoleAssignment>> GetActiveAssignmentsAsync(string verifiedCognitoSubject, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task GrantAsync(SmartRail.Domain.Identity.Entities.RoleAssignment assignment, SmartRail.Domain.Identity.Entities.RoleAssignmentAudit audit, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> RevokeAsync(Guid assignmentId, string verifiedActorSubject, DateTimeOffset occurredAtUtc, Guid auditId, string? evidenceReference = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
