using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SmartRail.Domain.Identity;
using SmartRail.Domain.Incidents;
using SmartRail.Domain.Incidents.Entities;
using SmartRail.Infrastructure.RailwayNetwork;
using SmartRail.IntegrationTests.Identity;
using SmartRail.IntegrationTests.RailwayNetwork;

namespace SmartRail.IntegrationTests.Incidents;

[Collection(IncidentPostgresCollection.Name)]
public sealed class ListIncidentsApiTests(PostgreSqlTestDatabaseFixture database, LocalOidcFixture oidc)
{
    [Fact]
    public async Task Signed_controller_can_list_incidents_and_invalid_or_missing_access_is_safe()
    {
        await using var factory = await IncidentApiTestHost.CreateAsync(database, oidc,
            IncidentCreationTestSupport.ConfigureValidTestAuthorities);
        using var client = factory.CreateClient();
        const string subject = "f004-list-controller-" + nameof(Signed_controller_can_list_incidents_and_invalid_or_missing_access_is_safe);
        await IncidentCreationTestSupport.GrantRoleAsync(factory.Services, subject, RoleCode.Controller);
        var stationId = await IncidentCreationTestSupport.AddPublishedStationAsync(factory.Services);
        var incidentId = await IncidentReadTestSupport.CreateIncidentAsync(client, oidc, subject, stationId, "list-controller-" + Guid.NewGuid());

        using var response = await client.SendAsync(IncidentReadTestSupport.Get(oidc.CreateToken(subject)));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Contains(body.RootElement.GetProperty("items").EnumerateArray(), item => item.GetProperty("incidentId").GetGuid() == incidentId);
        Assert.DoesNotContain("reporterSubject", body.RootElement.GetRawText(), StringComparison.OrdinalIgnoreCase);

        using var noToken = await client.GetAsync("/api/incidents");
        Assert.Equal(HttpStatusCode.Unauthorized, noToken.StatusCode);
        using var invalid = new HttpRequestMessage(HttpMethod.Get, "/api/incidents");
        invalid.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-jwt");
        using var invalidResponse = await client.SendAsync(invalid);
        Assert.Equal(HttpStatusCode.Unauthorized, invalidResponse.StatusCode);
        using var malformedFilter = await client.SendAsync(IncidentReadTestSupport.Get(oidc.CreateToken(subject), "?stationId=not-a-guid"));
        Assert.Equal(HttpStatusCode.BadRequest, malformedFilter.StatusCode);
    }

    [Fact]
    public async Task Combined_filters_are_conjunctive_and_authorized_no_match_is_empty()
    {
        await using var factory = await IncidentApiTestHost.CreateAsync(database, oidc,
            IncidentCreationTestSupport.ConfigureValidTestAuthorities);
        using var client = factory.CreateClient();
        const string subject = "f004-list-filters-" + nameof(Combined_filters_are_conjunctive_and_authorized_no_match_is_empty);
        await IncidentCreationTestSupport.GrantRoleAsync(factory.Services, subject, RoleCode.Controller);
        var stationA = await IncidentCreationTestSupport.AddPublishedStationAsync(factory.Services);
        var stationB = await IncidentCreationTestSupport.AddPublishedStationAsync(factory.Services);
        var incidentA = await IncidentReadTestSupport.CreateIncidentAsync(client, oidc, subject, stationA, "filter-a-" + Guid.NewGuid());
        var incidentB = await IncidentReadTestSupport.CreateIncidentAsync(client, oidc, subject, stationB, "filter-b-" + Guid.NewGuid());

        using var exact = await client.SendAsync(IncidentReadTestSupport.Get(oidc.CreateToken(subject), $"?incidentId={incidentA}&stationId={stationA}"));
        Assert.Equal(HttpStatusCode.OK, exact.StatusCode);
        using var exactBody = JsonDocument.Parse(await exact.Content.ReadAsStringAsync());
        Assert.Single(exactBody.RootElement.GetProperty("items").EnumerateArray());

        using var contradictory = await client.SendAsync(IncidentReadTestSupport.Get(oidc.CreateToken(subject), $"?incidentId={incidentA}&stationId={stationB}"));
        Assert.Equal(HttpStatusCode.OK, contradictory.StatusCode);
        Assert.Equal("{\"items\":[]}", await contradictory.Content.ReadAsStringAsync());

        using var noMatches = await client.SendAsync(IncidentReadTestSupport.Get(oidc.CreateToken(subject), $"?incidentId={Guid.NewGuid()}"));
        Assert.Equal(HttpStatusCode.OK, noMatches.StatusCode);
        Assert.Equal("{\"items\":[]}", await noMatches.Content.ReadAsStringAsync());
        Assert.NotEqual(incidentA, incidentB);
    }

    [Fact]
    public async Task Incident_station_and_journey_filters_are_combined_together()
    {
        await using var factory = await IncidentApiTestHost.CreateAsync(database, oidc);
        using var client = factory.CreateClient();
        const string subject = "f004-list-journey-filter-" + nameof(Incident_station_and_journey_filters_are_combined_together);
        await IncidentCreationTestSupport.GrantRoleAsync(factory.Services, subject, RoleCode.Controller);
        var stationId = await IncidentCreationTestSupport.AddPublishedStationAsync(factory.Services);
        var journeyId = Guid.NewGuid();
        var incidentId = await SeedIncidentWithStationAndJourneyAsync(factory.Services, subject, stationId, journeyId);

        using var matched = await client.SendAsync(IncidentReadTestSupport.Get(oidc.CreateToken(subject),
            $"?incidentId={incidentId}&stationId={stationId}&journeyId={journeyId}"));
        Assert.Equal(HttpStatusCode.OK, matched.StatusCode);
        using var matchedBody = JsonDocument.Parse(await matched.Content.ReadAsStringAsync());
        Assert.Single(matchedBody.RootElement.GetProperty("items").EnumerateArray());

        using var mismatch = await client.SendAsync(IncidentReadTestSupport.Get(oidc.CreateToken(subject),
            $"?incidentId={incidentId}&stationId={stationId}&journeyId={Guid.NewGuid()}"));
        Assert.Equal(HttpStatusCode.OK, mismatch.StatusCode);
        Assert.Equal("{\"items\":[]}", await mismatch.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Station_staff_need_current_scope_for_every_station_linked_to_an_incident()
    {
        await using var factory = await IncidentApiTestHost.CreateAsync(database, oidc,
            IncidentCreationTestSupport.ConfigureValidTestAuthorities);
        using var client = factory.CreateClient();
        const string controller = "f004-list-multistation-controller-" + nameof(Station_staff_need_current_scope_for_every_station_linked_to_an_incident);
        const string staff = "f004-list-multistation-staff-" + nameof(Station_staff_need_current_scope_for_every_station_linked_to_an_incident);
        await IncidentCreationTestSupport.GrantRoleAsync(factory.Services, controller, RoleCode.Controller);
        await IncidentCreationTestSupport.GrantRoleAsync(factory.Services, staff, RoleCode.StationStaff);
        var stationA = await IncidentCreationTestSupport.AddPublishedStationAsync(factory.Services);
        var stationB = await IncidentCreationTestSupport.AddPublishedStationAsync(factory.Services);
        var incidentId = await CreateMultiStationIncidentAsync(factory.Services, controller, stationA, stationB);
        await IncidentReadTestSupport.GrantStationScopeAsync(factory.Services, staff, stationA);

        using var deniedByScope = await client.SendAsync(IncidentReadTestSupport.Get(oidc.CreateToken(staff), $"?incidentId={incidentId}"));
        Assert.Equal(HttpStatusCode.OK, deniedByScope.StatusCode);
        Assert.Equal("{\"items\":[]}", await deniedByScope.Content.ReadAsStringAsync());

        await IncidentReadTestSupport.GrantStationScopeAsync(factory.Services, staff, stationB);
        using var allowed = await client.SendAsync(IncidentReadTestSupport.Get(oidc.CreateToken(staff), $"?incidentId={incidentId}"));
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        using var body = JsonDocument.Parse(await allowed.Content.ReadAsStringAsync());
        Assert.Single(body.RootElement.GetProperty("items").EnumerateArray());
    }

    [Fact]
    public async Task Revoked_controller_grant_is_enforced_on_the_next_read()
    {
        await using var factory = await IncidentApiTestHost.CreateAsync(database, oidc);
        using var client = factory.CreateClient();
        const string subject = "f004-list-revoked-controller-" + nameof(Revoked_controller_grant_is_enforced_on_the_next_read);
        var assignmentId = await IncidentCreationTestSupport.GrantRoleAsync(factory.Services, subject, RoleCode.Controller);
        var token = oidc.CreateToken(subject);

        using (var before = await client.SendAsync(IncidentReadTestSupport.Get(token)))
            Assert.Equal(HttpStatusCode.OK, before.StatusCode);
        await IncidentReadTestSupport.RevokeRoleAsync(factory.Services, assignmentId);
        using var after = await client.SendAsync(IncidentReadTestSupport.Get(token));
        Assert.Equal(HttpStatusCode.Forbidden, after.StatusCode);
    }

    [Fact]
    public async Task Role_mapping_unavailability_returns_503_and_unassigned_identity_gets_403()
    {
        const string subject = "f004-list-unavailable-" + nameof(Role_mapping_unavailability_returns_503_and_unassigned_identity_gets_403);
        await using (var factory = await IncidentApiTestHost.CreateAsync(database, oidc, services =>
        {
            services.RemoveAll<SmartRail.Application.Identity.Persistence.IRoleAssignmentRepository>();
            services.AddScoped<SmartRail.Application.Identity.Persistence.IRoleAssignmentRepository, UnavailableRoles>();
        }))
        using (var client = factory.CreateClient())
        using (var unavailable = await client.SendAsync(IncidentReadTestSupport.Get(oidc.CreateToken(subject))))
            Assert.Equal(HttpStatusCode.ServiceUnavailable, unavailable.StatusCode);

        await using var noRoleFactory = await IncidentApiTestHost.CreateAsync(database, oidc);
        using var noRoleClient = noRoleFactory.CreateClient();
        using var denied = await noRoleClient.SendAsync(IncidentReadTestSupport.Get(oidc.CreateToken(subject + "-no-role")));
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
    }

    [Fact]
    public async Task Current_station_scope_or_database_unavailability_fails_safely()
    {
        const string subject = "f004-list-scope-down-" + nameof(Current_station_scope_or_database_unavailability_fails_safely);
        await using (var factory = await IncidentApiTestHost.CreateAsync(database, oidc, services =>
        {
            services.RemoveAll<SmartRail.Application.Identity.Persistence.IStationScopeRepository>();
            services.AddScoped<SmartRail.Application.Identity.Persistence.IStationScopeRepository, UnavailableStationScopes>();
        }))
        using (var client = factory.CreateClient())
        {
            await IncidentCreationTestSupport.GrantRoleAsync(factory.Services, subject, RoleCode.StationStaff);
            var stationId = await IncidentCreationTestSupport.AddPublishedStationAsync(factory.Services);
            await SeedIncidentWithStationAndJourneyAsync(factory.Services, "fixture-reporter", stationId, Guid.NewGuid());
            using var response = await client.SendAsync(IncidentReadTestSupport.Get(oidc.CreateToken(subject)));
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        }

        await using var unavailableDatabase = await IncidentApiTestHost.CreateAsync(database, oidc, services =>
        {
            services.RemoveAll<SmartRail.Application.Incidents.Persistence.IIncidentRepository>();
            services.AddScoped<SmartRail.Application.Incidents.Persistence.IIncidentRepository, UnavailableIncidentRepository>();
        });
        using var unavailableClient = unavailableDatabase.CreateClient();
        const string controller = "f004-list-db-down-" + nameof(Current_station_scope_or_database_unavailability_fails_safely);
        await IncidentCreationTestSupport.GrantRoleAsync(unavailableDatabase.Services, controller, RoleCode.Controller);
        using var databaseResponse = await unavailableClient.SendAsync(IncidentReadTestSupport.Get(oidc.CreateToken(controller)));
        Assert.Equal(HttpStatusCode.ServiceUnavailable, databaseResponse.StatusCode);

    }

    private static async Task<Guid> CreateMultiStationIncidentAsync(IServiceProvider services, string subject, Guid stationA, Guid stationB)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartRailDbContext>();
        var now = DateTimeOffset.UtcNow;
        var incident = new Incident(Guid.NewGuid(), IncidentTestCategories.TrackObstruction,
            "Multi-station incident", subject, now);
        var associations = new[]
        {
            new IncidentAssociation(Guid.NewGuid(), incident.IncidentId, IncidentAssociationKind.Station, stationA, now, 1),
            new IncidentAssociation(Guid.NewGuid(), incident.IncidentId, IncidentAssociationKind.Station, stationB, now, 1),
            new IncidentAssociation(Guid.NewGuid(), incident.IncidentId, IncidentAssociationKind.Journey, Guid.NewGuid(), now, 1)
        };
        var evidence = System.Text.Json.JsonSerializer.Serialize(new { kind = "Report", stationA, stationB });
        db.Incidents.Add(incident);
        db.IncidentAssociations.AddRange(associations);
        db.IncidentRevisions.Add(new IncidentRevision(incident.IncidentId, 1, incident.State, subject, now, evidence));
        await db.SaveChangesAsync();
        return incident.IncidentId;
    }

    private static async Task<Guid> SeedIncidentWithStationAndJourneyAsync(IServiceProvider services, string subject, Guid stationId, Guid journeyId)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartRailDbContext>();
        var now = DateTimeOffset.UtcNow;
        var incident = new Incident(Guid.NewGuid(), IncidentTestCategories.TrackObstruction, "Journey-filter test", subject, now);
        var associations = new[]
        {
            new IncidentAssociation(Guid.NewGuid(), incident.IncidentId, IncidentAssociationKind.Station, stationId, now, 1),
            new IncidentAssociation(Guid.NewGuid(), incident.IncidentId, IncidentAssociationKind.Journey, journeyId, now, 1)
        };
        db.Incidents.Add(incident);
        db.IncidentAssociations.AddRange(associations);
        db.IncidentRevisions.Add(new IncidentRevision(incident.IncidentId, 1, incident.State, subject, now,
            System.Text.Json.JsonSerializer.Serialize(new { stationId, journeyId })));
        await db.SaveChangesAsync();
        return incident.IncidentId;
    }

    private sealed class UnavailableRoles : SmartRail.Application.Identity.Persistence.IRoleAssignmentRepository
    {
        public Task<IReadOnlyList<RoleCode>> GetActiveRolesAsync(string verifiedCognitoSubject, CancellationToken cancellationToken = default) => throw new InvalidOperationException("test-only unavailable authority");
        public Task<IReadOnlyList<SmartRail.Domain.Identity.Entities.RoleAssignment>> GetActiveAssignmentsAsync(string verifiedCognitoSubject, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task GrantAsync(SmartRail.Domain.Identity.Entities.RoleAssignment assignment, SmartRail.Domain.Identity.Entities.RoleAssignmentAudit audit, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> RevokeAsync(Guid assignmentId, string verifiedActorSubject, DateTimeOffset occurredAtUtc, Guid auditId, string? evidenceReference = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class UnavailableStationScopes : SmartRail.Application.Identity.Persistence.IStationScopeRepository
    {
        public Task<IReadOnlyList<Guid>> GetActiveStationIdsAsync(string verifiedCognitoSubject, CancellationToken cancellationToken = default) => throw new InvalidOperationException("test-only unavailable authority");
        public Task<IReadOnlyList<SmartRail.Domain.Identity.Entities.StationScopeAssignment>> GetActiveAssignmentsAsync(string verifiedCognitoSubject, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task GrantAsync(SmartRail.Domain.Identity.Entities.StationScopeAssignment assignment, SmartRail.Domain.Identity.Entities.StationScopeAssignmentAudit audit, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> RevokeAsync(Guid assignmentId, string verifiedActorSubject, DateTimeOffset occurredAtUtc, Guid auditId, string? evidenceReference = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class UnavailableIncidentRepository : SmartRail.Application.Incidents.Persistence.IIncidentRepository
    {
        private static Exception Failure() => new InvalidOperationException("test-only persistence failure");
        public Task<SmartRail.Application.Incidents.Persistence.IncidentRecord?> FindAsync(Guid incidentId, CancellationToken cancellationToken = default) => Task.FromException<SmartRail.Application.Incidents.Persistence.IncidentRecord?>(Failure());
        public Task<IReadOnlyList<SmartRail.Application.Incidents.Persistence.IncidentRecord>> ListAsync(SmartRail.Application.Incidents.Persistence.IncidentFilters filters, CancellationToken cancellationToken = default) => Task.FromException<IReadOnlyList<SmartRail.Application.Incidents.Persistence.IncidentRecord>>(Failure());
        public Task<SmartRail.Domain.Incidents.Entities.IncidentIdempotencyRecord?> FindIdempotencyAsync(SmartRail.Application.Incidents.Persistence.IdempotencyLookup lookup, CancellationToken cancellationToken = default) => Task.FromException<SmartRail.Domain.Incidents.Entities.IncidentIdempotencyRecord?>(Failure());
        public Task CreateAsync(SmartRail.Domain.Incidents.Entities.Incident incident, SmartRail.Domain.Incidents.Entities.IncidentRevision revision, IReadOnlyCollection<SmartRail.Domain.Incidents.Entities.IncidentAssociation> associations, SmartRail.Domain.Incidents.Entities.IncidentIdempotencyRecord idempotencyRecord, CancellationToken cancellationToken = default) => Task.FromException(Failure());
        public Task<bool> UpdateAsync(SmartRail.Domain.Incidents.Entities.Incident incident, SmartRail.Domain.Incidents.Entities.IncidentRevision revision, IReadOnlyCollection<SmartRail.Domain.Incidents.Entities.IncidentAssociation> addAssociations, IReadOnlyCollection<SmartRail.Domain.Incidents.Entities.IncidentAssociation> removeAssociations, SmartRail.Domain.Incidents.Entities.IncidentIdempotencyRecord idempotencyRecord, int expectedRevision, CancellationToken cancellationToken = default) => Task.FromException<bool>(Failure());
    }
}
