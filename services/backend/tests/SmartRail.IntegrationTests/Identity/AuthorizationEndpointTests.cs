using System.Net;
using Microsoft.Extensions.DependencyInjection;
using SmartRail.Application.Identity.Authorization;
using SmartRail.Application.Identity.Persistence;
using SmartRail.Domain.Identity;
using SmartRail.IntegrationTests.RailwayNetwork;

namespace SmartRail.IntegrationTests.Identity;

public sealed class AuthorizationEndpointTests(PostgreSqlTestDatabaseFixture database, LocalOidcFixture oidc) : IClassFixture<PostgreSqlTestDatabaseFixture>, IClassFixture<LocalOidcFixture>
{
    [Fact]
    public async Task Explicit_role_union_scope_ownership_and_unavailable_evidence_are_enforced_without_denied_writes()
    {
        using var factory = await IdentityApiTestHost.CreateAsync(database, oidc);
        using var client = factory.CreateClient();
        var subject = "probe-" + Guid.NewGuid().ToString("N");
        var token = oidc.CreateToken(subject, extraClaims: [new("groups", "Controller")]);
        await IdentityTestData.GrantRoleAsync(factory.Services, subject, RoleCode.Driver);
        await IdentityTestData.GrantRoleAsync(factory.Services, subject, RoleCode.Passenger);

        using (var union = await client.SendAsync(IdentityTestData.Bearer(HttpMethod.Post,
            IdentityTestData.ProbeUrl(PermissionAction.CreateCateringOrder, ProtectedResourceKind.None), token)))
            Assert.Equal(HttpStatusCode.OK, union.StatusCode);
        using (var denied = await client.SendAsync(IdentityTestData.Bearer(HttpMethod.Post,
            IdentityTestData.ProbeUrl(PermissionAction.PublishOfficialJourneyStatus, ProtectedResourceKind.None), token)))
            Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        Assert.Equal(1, factory.Services.GetRequiredService<ProbeWriteCounter>().Count);

        var controllerSubject = "controller-" + Guid.NewGuid().ToString("N");
        await IdentityTestData.GrantRoleAsync(factory.Services, controllerSubject, RoleCode.Controller);
        using (var controllerOnly = await client.SendAsync(IdentityTestData.Bearer(HttpMethod.Post,
            IdentityTestData.ProbeUrl(PermissionAction.PublishOfficialJourneyStatus, ProtectedResourceKind.None), oidc.CreateToken(controllerSubject))))
            Assert.Equal(HttpStatusCode.OK, controllerOnly.StatusCode);

        var driverSubject = "driver-" + Guid.NewGuid().ToString("N");
        await IdentityTestData.GrantRoleAsync(factory.Services, driverSubject, RoleCode.Driver);
        var driverToken = oidc.CreateToken(driverSubject);
        factory.Services.GetRequiredService<IdentityTestEvidence>().DriverOutcome = EvidenceOutcome.VerifiedMatch;
        using (var assigned = await client.GetAsync(IdentityTestData.ProbeUrl(PermissionAction.ViewDriverAssignments, ProtectedResourceKind.Journey, Guid.NewGuid()), driverToken))
            Assert.Equal(HttpStatusCode.OK, assigned.StatusCode);
        factory.Services.GetRequiredService<IdentityTestEvidence>().DriverOutcome = EvidenceOutcome.VerifiedNoMatch;
        using (var unassigned = await client.GetAsync(IdentityTestData.ProbeUrl(PermissionAction.ViewDriverAssignments, ProtectedResourceKind.Journey, Guid.NewGuid()), driverToken))
            Assert.Equal(HttpStatusCode.Forbidden, unassigned.StatusCode);
        factory.Services.GetRequiredService<IdentityTestEvidence>().DriverOutcome = EvidenceOutcome.Unavailable;
        using (var unavailable = await client.GetAsync(IdentityTestData.ProbeUrl(PermissionAction.ViewDriverAssignments, ProtectedResourceKind.Journey, Guid.NewGuid()), driverToken))
            Assert.Equal(HttpStatusCode.ServiceUnavailable, unavailable.StatusCode);

        var passengerSubject = "passenger-" + Guid.NewGuid().ToString("N");
        await IdentityTestData.GrantRoleAsync(factory.Services, passengerSubject, RoleCode.Passenger);
        var orderUrl = IdentityTestData.ProbeUrl(PermissionAction.ViewOwnCateringOrder, ProtectedResourceKind.CateringOrder, Guid.NewGuid());
        var passengerToken = oidc.CreateToken(passengerSubject);
        factory.Services.GetRequiredService<IdentityTestEvidence>().OrderOutcome = EvidenceOutcome.VerifiedMatch;
        using (var ownOrder = await client.GetAsync(orderUrl, passengerToken)) Assert.Equal(HttpStatusCode.OK, ownOrder.StatusCode);
        factory.Services.GetRequiredService<IdentityTestEvidence>().OrderOutcome = EvidenceOutcome.VerifiedNoMatch;
        using (var otherOrder = await client.GetAsync(orderUrl, passengerToken)) Assert.Equal(HttpStatusCode.Forbidden, otherOrder.StatusCode);

        var staffSubject = "staff-" + Guid.NewGuid().ToString("N");
        await IdentityTestData.GrantRoleAsync(factory.Services, staffSubject, RoleCode.StationStaff);
        var stationId = await IdentityTestData.AddStationAndGrantScopeAsync(factory.Services, staffSubject);
        using (var scoped = await client.GetAsync(IdentityTestData.ProbeUrl(PermissionAction.ViewOperationsDashboard, ProtectedResourceKind.Station, stationId), oidc.CreateToken(staffSubject)))
            Assert.Equal(HttpStatusCode.OK, scoped.StatusCode);
        using (var otherStation = await client.GetAsync(IdentityTestData.ProbeUrl(PermissionAction.ViewOperationsDashboard, ProtectedResourceKind.Station, Guid.NewGuid()), oidc.CreateToken(staffSubject)))
            Assert.Equal(HttpStatusCode.Forbidden, otherStation.StatusCode);

        factory.Services.GetRequiredService<IdentityTestEvidence>().IncidentOutcome = EvidenceOutcome.VerifiedMatch;
        factory.Services.GetRequiredService<IdentityTestEvidence>().IncidentStationId = stationId;
        using (var scopedIncident = await client.GetAsync(IdentityTestData.ProbeUrl(PermissionAction.ViewIncidents, ProtectedResourceKind.Incident, Guid.NewGuid()), oidc.CreateToken(staffSubject)))
            Assert.Equal(HttpStatusCode.OK, scopedIncident.StatusCode);
        factory.Services.GetRequiredService<IdentityTestEvidence>().IncidentStationId = Guid.NewGuid();
        using (var wrongIncidentStation = await client.GetAsync(IdentityTestData.ProbeUrl(PermissionAction.ViewIncidents, ProtectedResourceKind.Incident, Guid.NewGuid()), oidc.CreateToken(staffSubject)))
            Assert.Equal(HttpStatusCode.Forbidden, wrongIncidentStation.StatusCode);

        using var unavailableScopeFactory = await IdentityApiTestHost.CreateAsync(database, oidc, unavailableStationScopes: true);
        var unavailableStaff = "staff-down-" + Guid.NewGuid().ToString("N");
        await IdentityTestData.GrantRoleAsync(unavailableScopeFactory.Services, unavailableStaff, RoleCode.StationStaff);
        using var unavailableScopeClient = unavailableScopeFactory.CreateClient();
        using var scopeUnavailable = await unavailableScopeClient.GetAsync(IdentityTestData.ProbeUrl(PermissionAction.ViewOperationsDashboard, ProtectedResourceKind.Station, Guid.NewGuid()), oidc.CreateToken(unavailableStaff));
        Assert.Equal(HttpStatusCode.ServiceUnavailable, scopeUnavailable.StatusCode);
    }
}

internal static class HttpClientProbeExtensions
{
    public static Task<HttpResponseMessage> GetAsync(this HttpClient client, string url, string token) =>
        client.SendAsync(IdentityTestData.Bearer(HttpMethod.Get, url, token));
}
