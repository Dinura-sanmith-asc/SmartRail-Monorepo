using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SmartRail.Application.Identity.Persistence;
using SmartRail.Domain.Identity.Entities;
using SmartRail.Domain.Identity;
using SmartRail.Infrastructure.RailwayNetwork;
using SmartRail.IntegrationTests.Identity;
using SmartRail.IntegrationTests.RailwayNetwork;

namespace SmartRail.IntegrationTests.Incidents;

[Collection(IncidentPostgresCollection.Name)]
public sealed class UpdateIncidentIdempotencyTests(PostgreSqlTestDatabaseFixture database, LocalOidcFixture oidc)
{
    [Fact]
    public async Task Concurrent_identical_patch_replays_original_result_once()
    {
        await using var factory = await IncidentApiTestHost.CreateAsync(database, oidc,
            IncidentCreationTestSupport.ConfigureValidTestAuthorities);
        using var client = factory.CreateClient();
        var subject = "f004-patch-idempotency-race-" + Guid.NewGuid().ToString("N");
        await IncidentCreationTestSupport.GrantRoleAsync(factory.Services, subject, RoleCode.Controller);
        var stationId = await IncidentCreationTestSupport.AddPublishedStationAsync(factory.Services);
        var incidentId = await IncidentReadTestSupport.CreateIncidentAsync(client, oidc, subject, stationId,
            "patch-idempotency-create-" + Guid.NewGuid());
        var token = oidc.CreateToken(subject);
        const string key = "patch-identical-replay-key";
        using var requestA = IncidentReadTestSupport.Patch(incidentId, 1, "In Progress", token, key);
        using var requestB = IncidentReadTestSupport.Patch(incidentId, 1, "In Progress", token, key);

        var responses = await Task.WhenAll(client.SendAsync(requestA), client.SendAsync(requestB));
        using var responseA = responses[0];
        using var responseB = responses[1];

        Assert.Equal(HttpStatusCode.OK, responseA.StatusCode);
        Assert.Equal(HttpStatusCode.OK, responseB.StatusCode);
        using var jsonA = JsonDocument.Parse(await responseA.Content.ReadAsStringAsync());
        using var jsonB = JsonDocument.Parse(await responseB.Content.ReadAsStringAsync());
        Assert.Equal(jsonA.RootElement.GetProperty("incidentId").GetGuid(), jsonB.RootElement.GetProperty("incidentId").GetGuid());
        Assert.Equal(jsonA.RootElement.GetProperty("revision").GetInt32(), jsonB.RootElement.GetProperty("revision").GetInt32());
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartRailDbContext>();
        Assert.Equal(2, (await db.Incidents.SingleAsync(x => x.IncidentId == incidentId)).Revision);
        Assert.Equal(2, await db.IncidentRevisions.CountAsync(x => x.IncidentId == incidentId));
        Assert.Equal(2, await db.IncidentIdempotencyRecords.CountAsync(x => x.IncidentId == incidentId));
    }

    [Fact]
    public async Task Conflicting_key_reuse_returns_409_without_another_revision()
    {
        await using var factory = await IncidentApiTestHost.CreateAsync(database, oidc,
            IncidentCreationTestSupport.ConfigureValidTestAuthorities);
        using var client = factory.CreateClient();
        var subject = "f004-patch-idempotency-conflict-" + Guid.NewGuid().ToString("N");
        await IncidentCreationTestSupport.GrantRoleAsync(factory.Services, subject, RoleCode.Controller);
        var stationId = await IncidentCreationTestSupport.AddPublishedStationAsync(factory.Services);
        var incidentId = await IncidentReadTestSupport.CreateIncidentAsync(client, oidc, subject, stationId,
            "patch-idempotency-conflict-create-" + Guid.NewGuid());
        var token = oidc.CreateToken(subject);

        using var first = await client.SendAsync(IncidentReadTestSupport.Patch(incidentId, 1, "In Progress", token, "patch-conflict-key"));
        using var second = await client.SendAsync(IncidentReadTestSupport.Patch(incidentId, 1, "Resolved", token, "patch-conflict-key"));

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartRailDbContext>();
        Assert.Equal(2, (await db.Incidents.SingleAsync(x => x.IncidentId == incidentId)).Revision);
        Assert.Equal(2, await db.IncidentRevisions.CountAsync(x => x.IncidentId == incidentId));
    }

    [Fact]
    public async Task Revoked_role_cannot_retrieve_stored_patch_replay()
    {
        await using var factory = await IncidentApiTestHost.CreateAsync(database, oidc,
            IncidentCreationTestSupport.ConfigureValidTestAuthorities);
        using var client = factory.CreateClient();
        var subject = "f004-patch-idempotency-revoke-" + Guid.NewGuid().ToString("N");
        var assignmentId = await IncidentCreationTestSupport.GrantRoleAsync(factory.Services, subject, RoleCode.Controller);
        var stationId = await IncidentCreationTestSupport.AddPublishedStationAsync(factory.Services);
        var incidentId = await IncidentReadTestSupport.CreateIncidentAsync(client, oidc, subject, stationId,
            "patch-idempotency-revoke-create-" + Guid.NewGuid());
        var token = oidc.CreateToken(subject);

        using (var first = await client.SendAsync(IncidentReadTestSupport.Patch(incidentId, 1, "In Progress", token, "patch-revoked-key")))
            Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        await IncidentReadTestSupport.RevokeRoleAsync(factory.Services, assignmentId);
        using var replay = await client.SendAsync(IncidentReadTestSupport.Patch(incidentId, 1, "In Progress", token, "patch-revoked-key"));

        Assert.Equal(HttpStatusCode.Forbidden, replay.StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartRailDbContext>();
        Assert.Equal(2, (await db.Incidents.SingleAsync(x => x.IncidentId == incidentId)).Revision);
        Assert.Equal(2, await db.IncidentRevisions.CountAsync(x => x.IncidentId == incidentId));
    }

    [Fact]
    public async Task Unavailable_role_mapping_cannot_retrieve_stored_patch_replay()
    {
        var gate = new RoleAvailabilityGate();
        await using var factory = await IncidentApiTestHost.CreateAsync(database, oidc, services =>
        {
            IncidentCreationTestSupport.ConfigureValidTestAuthorities(services);
            services.RemoveAll<IRoleAssignmentRepository>();
            services.AddSingleton(gate);
            services.AddScoped<IRoleAssignmentRepository>(provider => new ToggleRoles(
                provider.GetRequiredService<SmartRailDbContext>(), provider.GetRequiredService<RoleAvailabilityGate>()));
        });
        using var client = factory.CreateClient();
        var subject = "f004-role-replay-unavailable-" + Guid.NewGuid().ToString("N");
        await IncidentCreationTestSupport.GrantRoleAsync(factory.Services, subject, RoleCode.Controller);
        var stationId = await IncidentCreationTestSupport.AddPublishedStationAsync(factory.Services);
        var incidentId = await IncidentReadTestSupport.CreateIncidentAsync(client, oidc, subject, stationId,
            "role-unavailable-create-" + Guid.NewGuid());
        var token = oidc.CreateToken(subject);

        using (var initial = await client.SendAsync(IncidentReadTestSupport.Patch(incidentId, 1,
                   "In Progress", token, "role-unavailable-inprogress")))
            Assert.Equal(HttpStatusCode.OK, initial.StatusCode);
        gate.Unavailable = true;
        using var replay = await client.SendAsync(IncidentReadTestSupport.Patch(incidentId, 1,
            "In Progress", token, "role-unavailable-inprogress"));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, replay.StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartRailDbContext>();
        Assert.Equal(2, (await db.Incidents.SingleAsync(x => x.IncidentId == incidentId)).Revision);
        Assert.Equal(2, await db.IncidentRevisions.CountAsync(x => x.IncidentId == incidentId));
    }

    [Fact]
    public async Task Unavailable_station_scope_cannot_retrieve_stored_patch_replay()
    {
        var gate = new ScopeAvailabilityGate();
        await using var factory = await IncidentApiTestHost.CreateAsync(database, oidc, services =>
        {
            IncidentCreationTestSupport.ConfigureValidTestAuthorities(services);
            services.RemoveAll<IStationScopeRepository>();
            services.AddSingleton(gate);
            services.AddScoped<IStationScopeRepository>(provider => new ToggleStationScopes(
                provider.GetRequiredService<SmartRailDbContext>(), provider.GetRequiredService<ScopeAvailabilityGate>()));
        });
        using var client = factory.CreateClient();
        var controller = "f004-scope-replay-controller-" + Guid.NewGuid().ToString("N");
        var staff = "f004-scope-replay-staff-" + Guid.NewGuid().ToString("N");
        await IncidentCreationTestSupport.GrantRoleAsync(factory.Services, controller, RoleCode.Controller);
        await IncidentCreationTestSupport.GrantRoleAsync(factory.Services, staff, RoleCode.StationStaff);
        var stationId = await IncidentCreationTestSupport.AddPublishedStationAsync(factory.Services);
        var incidentId = await IncidentReadTestSupport.CreateIncidentAsync(client, oidc, controller, stationId,
            "scope-replay-create-" + Guid.NewGuid());
        await IncidentReadTestSupport.GrantStationScopeAsync(factory.Services, staff, stationId);
        var staffToken = oidc.CreateToken(staff);

        using (var initial = await client.SendAsync(IncidentReadTestSupport.Patch(incidentId, 1,
                   "In Progress", staffToken, "scope-replay-inprogress")))
            Assert.Equal(HttpStatusCode.OK, initial.StatusCode);

        gate.Unavailable = true;
        using var replay = await client.SendAsync(IncidentReadTestSupport.Patch(incidentId, 1,
            "In Progress", staffToken, "scope-replay-inprogress"));
        Assert.Equal(HttpStatusCode.ServiceUnavailable, replay.StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartRailDbContext>();
        Assert.Equal(2, (await db.Incidents.SingleAsync(x => x.IncidentId == incidentId)).Revision);
        Assert.Equal(2, await db.IncidentRevisions.CountAsync(x => x.IncidentId == incidentId));
    }

    [Fact]
    public async Task Revoked_station_scope_cannot_retrieve_stored_patch_replay()
    {
        await using var factory = await IncidentApiTestHost.CreateAsync(database, oidc,
            IncidentCreationTestSupport.ConfigureValidTestAuthorities);
        using var client = factory.CreateClient();
        var controller = "f004-scope-revoke-controller-" + Guid.NewGuid().ToString("N");
        var staff = "f004-scope-revoke-staff-" + Guid.NewGuid().ToString("N");
        await IncidentCreationTestSupport.GrantRoleAsync(factory.Services, controller, RoleCode.Controller);
        await IncidentCreationTestSupport.GrantRoleAsync(factory.Services, staff, RoleCode.StationStaff);
        var stationId = await IncidentCreationTestSupport.AddPublishedStationAsync(factory.Services);
        var incidentId = await IncidentReadTestSupport.CreateIncidentAsync(client, oidc, controller, stationId,
            "scope-revoke-create-" + Guid.NewGuid());
        var assignment = await IncidentReadTestSupport.GrantStationScopeAsync(factory.Services, staff, stationId);
        var staffToken = oidc.CreateToken(staff);

        using (var initial = await client.SendAsync(IncidentReadTestSupport.Patch(incidentId, 1,
                   "In Progress", staffToken, "scope-revoke-inprogress")))
            Assert.Equal(HttpStatusCode.OK, initial.StatusCode);
        await IncidentReadTestSupport.RevokeStationScopeAsync(factory.Services, assignment);
        using var replay = await client.SendAsync(IncidentReadTestSupport.Patch(incidentId, 1,
            "In Progress", staffToken, "scope-revoke-inprogress"));

        Assert.Equal(HttpStatusCode.Forbidden, replay.StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartRailDbContext>();
        Assert.Equal(2, (await db.Incidents.SingleAsync(x => x.IncidentId == incidentId)).Revision);
        Assert.Equal(2, await db.IncidentRevisions.CountAsync(x => x.IncidentId == incidentId));
    }

    private sealed class ScopeAvailabilityGate { public bool Unavailable { get; set; } }
    private sealed class RoleAvailabilityGate { public bool Unavailable { get; set; } }

    private sealed class ToggleRoles(SmartRailDbContext db, RoleAvailabilityGate gate) : IRoleAssignmentRepository
    {
        public async Task<IReadOnlyList<RoleCode>> GetActiveRolesAsync(string verifiedCognitoSubject, CancellationToken cancellationToken = default)
        {
            if (gate.Unavailable) throw new InvalidOperationException("test-only unavailable role mapping");
            return await db.RoleAssignments.AsNoTracking()
                .Where(x => x.CognitoSubject == verifiedCognitoSubject && x.RevokedAtUtc == null && x.RevokedBySubject == null)
                .Select(x => x.RoleCode).Distinct().ToArrayAsync(cancellationToken);
        }
        public Task<IReadOnlyList<RoleAssignment>> GetActiveAssignmentsAsync(string verifiedCognitoSubject, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task GrantAsync(RoleAssignment assignment, RoleAssignmentAudit audit, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<bool> RevokeAsync(Guid assignmentId, string verifiedActorSubject, DateTimeOffset occurredAtUtc, Guid auditId,
            string? evidenceReference = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class ToggleStationScopes(SmartRailDbContext db, ScopeAvailabilityGate gate) : IStationScopeRepository
    {
        public Task<IReadOnlyList<Guid>> GetActiveStationIdsAsync(string verifiedCognitoSubject, CancellationToken cancellationToken = default) =>
            gate.Unavailable ? Task.FromException<IReadOnlyList<Guid>>(new InvalidOperationException("test-only unavailable")) :
            new SmartRail.Infrastructure.Identity.Persistence.StationScopeRepository(db).GetActiveStationIdsAsync(verifiedCognitoSubject, cancellationToken);
        public Task<IReadOnlyList<SmartRail.Domain.Identity.Entities.StationScopeAssignment>> GetActiveAssignmentsAsync(string verifiedCognitoSubject, CancellationToken cancellationToken = default) =>
            gate.Unavailable ? Task.FromException<IReadOnlyList<SmartRail.Domain.Identity.Entities.StationScopeAssignment>>(new InvalidOperationException("test-only unavailable")) :
            new SmartRail.Infrastructure.Identity.Persistence.StationScopeRepository(db).GetActiveAssignmentsAsync(verifiedCognitoSubject, cancellationToken);
        public Task GrantAsync(SmartRail.Domain.Identity.Entities.StationScopeAssignment assignment, SmartRail.Domain.Identity.Entities.StationScopeAssignmentAudit audit, CancellationToken cancellationToken = default) =>
            new SmartRail.Infrastructure.Identity.Persistence.StationScopeRepository(db).GrantAsync(assignment, audit, cancellationToken);
        public Task<bool> RevokeAsync(Guid assignmentId, string verifiedActorSubject, DateTimeOffset occurredAtUtc, Guid auditId,
            string? evidenceReference = null, CancellationToken cancellationToken = default) =>
            new SmartRail.Infrastructure.Identity.Persistence.StationScopeRepository(db).RevokeAsync(assignmentId, verifiedActorSubject, occurredAtUtc, auditId, evidenceReference, cancellationToken);
    }
}
