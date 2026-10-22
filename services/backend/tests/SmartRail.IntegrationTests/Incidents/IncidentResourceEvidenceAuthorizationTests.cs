using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SmartRail.Application.Identity.Authorization;
using SmartRail.Application.Incidents.Authorization;
using SmartRail.Domain.Identity;
using SmartRail.Domain.Identity.Entities;
using SmartRail.Domain.Incidents;
using SmartRail.Domain.Incidents.Entities;
using SmartRail.Domain.RailwayNetwork.Entities;
using SmartRail.Infrastructure.RailwayNetwork;
using SmartRail.IntegrationTests.Identity;
using SmartRail.IntegrationTests.RailwayNetwork;

namespace SmartRail.IntegrationTests.Incidents;

[Collection(IncidentPostgresCollection.Name)]
public sealed class IncidentResourceEvidenceAuthorizationTests(
    PostgreSqlTestDatabaseFixture database, LocalOidcFixture oidc) : IAsyncLifetime
{
    private Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> _factory = null!;

    public async Task InitializeAsync() => _factory = await IncidentApiTestHost.CreateAsync(database, oidc);
    public async Task DisposeAsync() => await _factory.DisposeAsync();

    [Fact]
    public async Task Genuine_adapter_and_current_controller_grant_allow_existing_incident()
    {
        const string subject = "f004-controller-active";
        await GrantRoleAsync(subject, RoleCode.Controller);
        var station = await AddStationAsync();
        var incident = await AddIncidentAsync(station);

        await using var scope = _factory.Services.CreateAsyncScope();
        var evidence = scope.ServiceProvider.GetRequiredService<IIncidentResourceEvidence>();
        var resolved = await evidence.ResolveIncidentAsync(incident);
        Assert.Equal(EvidenceOutcome.VerifiedMatch, resolved.Outcome);
        Assert.Equal(station, resolved.StationId);

        var auth = scope.ServiceProvider.GetRequiredService<AuthorizeOperationHandler>();
        var decision = await auth.HandleAsync(new(new VerifiedPrincipal(subject), PermissionAction.ViewIncidents,
            ProtectedResource.Incident(incident)));
        Assert.Equal(AuthorizationOutcome.Allowed, decision.Outcome);
    }

    [Fact]
    public async Task Controller_global_create_grant_allows_service_only_report()
    {
        const string subject = "f004-controller-service-only-create";
        await GrantRoleAsync(subject, RoleCode.Controller);
        await using var scope = _factory.Services.CreateAsyncScope();
        var evaluator = new IncidentAccessEvaluator(scope.ServiceProvider.GetRequiredService<AuthorizeOperationHandler>());

        var decision = await evaluator.AuthorizeAsync(subject, PermissionAction.CreateIncident, null,
            Array.Empty<Guid>(), isCreation: true);

        Assert.Equal(AuthorizationOutcome.Allowed, decision.Outcome);
    }

    [Fact]
    public async Task Station_staff_service_only_report_is_denied_without_station_scope()
    {
        const string subject = "f004-staff-service-only-create";
        await GrantRoleAsync(subject, RoleCode.StationStaff);
        await using var scope = _factory.Services.CreateAsyncScope();
        var evaluator = new IncidentAccessEvaluator(scope.ServiceProvider.GetRequiredService<AuthorizeOperationHandler>());

        var decision = await evaluator.AuthorizeAsync(subject, PermissionAction.CreateIncident, null,
            Array.Empty<Guid>(), isCreation: true);

        Assert.Equal(AuthorizationOutcome.Denied, decision.Outcome);
    }

    [Fact]
    public async Task Station_staff_requires_scope_for_every_associated_station()
    {
        const string subject = "f004-station-staff-scoped";
        await GrantRoleAsync(subject, RoleCode.StationStaff);
        var inScope = await AddStationAsync(subject);
        var outOfScope = await AddStationAsync();
        var incident = await AddIncidentAsync(inScope, outOfScope);
        var singleStationIncident = await AddIncidentAsync(inScope);

        await using var scope = _factory.Services.CreateAsyncScope();
        var evaluator = new IncidentAccessEvaluator(scope.ServiceProvider.GetRequiredService<AuthorizeOperationHandler>());
        var decision = await evaluator.AuthorizeAsync(subject, PermissionAction.ViewIncidents, incident, new[] { inScope, outOfScope });
        Assert.Equal(AuthorizationOutcome.Denied, decision.Outcome);

        var inScopeDecision = await evaluator.AuthorizeAsync(subject, PermissionAction.ViewIncidents, singleStationIncident, new[] { inScope });
        Assert.Equal(AuthorizationOutcome.Allowed, inScopeDecision.Outcome);
    }

    [Fact]
    public async Task Revoked_controller_is_denied_and_missing_incident_is_not_verified()
    {
        const string subject = "f004-controller-revoked";
        await GrantRoleAsync(subject, RoleCode.Controller, revoked: true);
        await using var scope = _factory.Services.CreateAsyncScope();
        var auth = scope.ServiceProvider.GetRequiredService<AuthorizeOperationHandler>();
        var denied = await auth.HandleAsync(new(new VerifiedPrincipal(subject), PermissionAction.ViewIncidents,
            ProtectedResource.Incident(Guid.NewGuid())));
        Assert.Equal(AuthorizationOutcome.Denied, denied.Outcome);

        var evidence = scope.ServiceProvider.GetRequiredService<IIncidentResourceEvidence>();
        var missing = await evidence.ResolveIncidentAsync(Guid.NewGuid());
        Assert.Equal(EvidenceOutcome.Missing, missing.Outcome);
    }

    [Fact]
    public async Task Unavailable_incident_authority_fails_closed()
    {
        await using var factory = await IncidentApiTestHost.CreateAsync(database, oidc, services =>
        {
            services.RemoveAll<IIncidentResourceEvidence>();
            services.AddScoped<IIncidentResourceEvidence, UnavailableIncidentEvidence>();
        });
        const string subject = "f004-controller-evidence-down";
        await GrantRoleAsync(subject, RoleCode.Controller);
        await using var scope = factory.Services.CreateAsyncScope();
        var auth = scope.ServiceProvider.GetRequiredService<AuthorizeOperationHandler>();
        var result = await auth.HandleAsync(new(new VerifiedPrincipal(subject), PermissionAction.ViewIncidents,
            ProtectedResource.Incident(Guid.NewGuid())));
        Assert.Equal(AuthorizationOutcome.Unavailable, result.Outcome);
    }

    private async Task GrantRoleAsync(string subject, RoleCode role, bool revoked = false)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartRailDbContext>();
        var granted = DateTimeOffset.UtcNow.AddMinutes(-2);
        var assignment = new RoleAssignment(Guid.NewGuid(), subject, role, granted, "f004-test-approver");
        db.RoleAssignments.Add(assignment);
        db.RoleAssignmentAudits.Add(RoleAssignmentAudit.ForGrant(Guid.NewGuid(), assignment));
        if (revoked)
        {
            assignment.Revoke("f004-test-approver", DateTimeOffset.UtcNow);
            db.RoleAssignmentAudits.Add(RoleAssignmentAudit.ForRevocation(Guid.NewGuid(), assignment));
        }
        await db.SaveChangesAsync();
    }

    private async Task<Guid> AddStationAsync(string? scopedSubject = null)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartRailDbContext>();
        var stationId = Guid.NewGuid();
        db.Stations.Add(new Station(stationId, "F004-" + Guid.NewGuid().ToString("N")[..10], "Incident test station"));
        if (scopedSubject is not null)
        {
            var assignment = new StationScopeAssignment(Guid.NewGuid(), scopedSubject, stationId, DateTimeOffset.UtcNow, "f004-test-approver");
            db.StationScopeAssignments.Add(assignment);
            db.StationScopeAssignmentAudits.Add(StationScopeAssignmentAudit.ForGrant(Guid.NewGuid(), assignment));
        }
        await db.SaveChangesAsync();
        return stationId;
    }

    private async Task<Guid> AddIncidentAsync(params Guid[] stationIds)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartRailDbContext>();
        var incident = IncidentTestData.CreateIncident();
        db.Incidents.Add(incident);
        db.IncidentRevisions.Add(IncidentTestData.CreateRevision(incident));
        foreach (var stationId in stationIds)
            db.IncidentAssociations.Add(new IncidentAssociation(Guid.NewGuid(), incident.IncidentId, IncidentAssociationKind.Station,
                stationId, incident.ReportedAt, 1));
        await db.SaveChangesAsync();
        return incident.IncidentId;
    }

    private sealed class UnavailableIncidentEvidence : IIncidentResourceEvidence
    {
        public Task<IncidentResourceEvidence> ResolveIncidentAsync(Guid incidentId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new IncidentResourceEvidence(EvidenceOutcome.Unavailable));
    }
}
