using System.Net;
using Microsoft.Extensions.DependencyInjection;
using SmartRail.Application.Identity.Authorization;
using SmartRail.Domain.Identity;
using SmartRail.IntegrationTests.RailwayNetwork;

namespace SmartRail.IntegrationTests.Identity;

public sealed class AuthorizationMatrixTests(PostgreSqlTestDatabaseFixture database, LocalOidcFixture oidc) : IClassFixture<PostgreSqlTestDatabaseFixture>, IClassFixture<LocalOidcFixture>
{
    [Fact]
    public async Task Complete_approved_role_action_matrix_is_enforced_through_api_with_current_provider_evidence()
    {
        using var factory = await IdentityApiTestHost.CreateAsync(database, oidc);
        using var client = factory.CreateClient();
        var subjects = Enum.GetValues<RoleCode>().ToDictionary(role => role, role => "matrix-" + role + "-" + Guid.NewGuid().ToString("N"));
        foreach (var (role, subject) in subjects) await IdentityTestData.GrantRoleAsync(factory.Services, subject, role);
        var stationId = await IdentityTestData.AddStationAndGrantScopeAsync(factory.Services, subjects[RoleCode.StationStaff]);
        var evidence = factory.Services.GetRequiredService<IdentityTestEvidence>();
        evidence.DriverOutcome = EvidenceOutcome.VerifiedMatch;
        evidence.IncidentOutcome = EvidenceOutcome.VerifiedMatch;
        evidence.IncidentStationId = stationId;
        evidence.OrderOutcome = EvidenceOutcome.VerifiedMatch;

        var expected = new Dictionary<RoleCode, HashSet<PermissionAction>>
        {
            [RoleCode.Passenger] = [PermissionAction.SearchTrainsStationsAndSchedules, PermissionAction.ViewPublishedJourneyStatus, PermissionAction.ViewStationDepartures, PermissionAction.DiscoverCateringOptions, PermissionAction.CreateCateringOrder, PermissionAction.ViewOwnCateringOrder],
            [RoleCode.Driver] = [PermissionAction.SearchTrainsStationsAndSchedules, PermissionAction.ViewPublishedJourneyStatus, PermissionAction.ViewStationDepartures, PermissionAction.ViewDriverAssignments, PermissionAction.SubmitDriverProgressObservation, PermissionAction.DiscoverCateringOptions],
            [RoleCode.StationStaff] = [PermissionAction.SearchTrainsStationsAndSchedules, PermissionAction.ViewPublishedJourneyStatus, PermissionAction.ViewStationDepartures, PermissionAction.ViewOperationsDashboard, PermissionAction.CreateIncident, PermissionAction.ViewIncidents, PermissionAction.UpdateIncidentLifecycle, PermissionAction.DiscoverCateringOptions],
            [RoleCode.Controller] = [PermissionAction.SearchTrainsStationsAndSchedules, PermissionAction.ViewPublishedJourneyStatus, PermissionAction.ViewStationDepartures, PermissionAction.ViewOperationsDashboard, PermissionAction.ViewDriverAssignments, PermissionAction.ReviewDriverObservations, PermissionAction.PublishOfficialJourneyStatus, PermissionAction.ChangePlatformOrOperationalTiming, PermissionAction.ConfirmJourneyCancellation, PermissionAction.CreateIncident, PermissionAction.ViewIncidents, PermissionAction.UpdateIncidentLifecycle, PermissionAction.DiscoverCateringOptions]
        };

        foreach (var role in Enum.GetValues<RoleCode>())
        foreach (var action in Enum.GetValues<PermissionAction>())
        {
            var resource = ResourceFor(role, action, stationId);
            using var response = await client.GetAsync(IdentityTestData.ProbeUrl(action, resource.Kind, resource.ResourceId), oidc.CreateToken(subjects[role]));
            var expectedStatus = expected[role].Contains(action) ? HttpStatusCode.OK : HttpStatusCode.Forbidden;
            Assert.True(response.StatusCode == expectedStatus,
                $"{role}/{action} should be {(int)expectedStatus}, received {(int)response.StatusCode}.");
        }
        var union = RolePermissionCatalog.GetGrantedActions([RoleCode.Passenger, RoleCode.Driver]);
        Assert.Equal(expected[RoleCode.Passenger].Union(expected[RoleCode.Driver]).Order(), union.Order());
        Assert.Empty(RolePermissionCatalog.GetGrantedActions([(RoleCode)999]));
    }

    private static ProtectedResource ResourceFor(RoleCode role, PermissionAction action, Guid stationId) =>
        role switch
        {
            RoleCode.StationStaff when action is PermissionAction.ViewOperationsDashboard or PermissionAction.CreateIncident => ProtectedResource.Station(stationId),
            RoleCode.StationStaff when action is PermissionAction.ViewIncidents or PermissionAction.UpdateIncidentLifecycle => ProtectedResource.Incident(Guid.NewGuid()),
            RoleCode.Driver when action is PermissionAction.ViewDriverAssignments or PermissionAction.SubmitDriverProgressObservation => ProtectedResource.Journey(Guid.NewGuid()),
            RoleCode.Passenger when action == PermissionAction.ViewOwnCateringOrder => ProtectedResource.CateringOrder(Guid.NewGuid()),
            RoleCode.Controller when action is PermissionAction.ViewIncidents or PermissionAction.UpdateIncidentLifecycle => ProtectedResource.Incident(Guid.NewGuid()),
            _ => ProtectedResource.None
        };
}
