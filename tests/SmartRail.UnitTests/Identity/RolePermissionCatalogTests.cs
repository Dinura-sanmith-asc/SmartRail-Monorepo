using SmartRail.Domain.Identity;

namespace SmartRail.UnitTests.Identity;

public sealed class RolePermissionCatalogTests
{
    private static readonly (RoleCode Role, PermissionAction Action)[] ApprovedRoleGrants =
    [
        (RoleCode.Passenger, PermissionAction.SearchTrainsStationsAndSchedules),
        (RoleCode.Passenger, PermissionAction.ViewPublishedJourneyStatus),
        (RoleCode.Passenger, PermissionAction.ViewStationDepartures),
        (RoleCode.Passenger, PermissionAction.DiscoverCateringOptions),
        (RoleCode.Passenger, PermissionAction.CreateCateringOrder),
        (RoleCode.Passenger, PermissionAction.ViewOwnCateringOrder),
        (RoleCode.Driver, PermissionAction.SearchTrainsStationsAndSchedules),
        (RoleCode.Driver, PermissionAction.ViewPublishedJourneyStatus),
        (RoleCode.Driver, PermissionAction.ViewStationDepartures),
        (RoleCode.Driver, PermissionAction.ViewDriverAssignments),
        (RoleCode.Driver, PermissionAction.SubmitDriverProgressObservation),
        (RoleCode.Driver, PermissionAction.DiscoverCateringOptions),
        (RoleCode.StationStaff, PermissionAction.SearchTrainsStationsAndSchedules),
        (RoleCode.StationStaff, PermissionAction.ViewPublishedJourneyStatus),
        (RoleCode.StationStaff, PermissionAction.ViewStationDepartures),
        (RoleCode.StationStaff, PermissionAction.ViewOperationsDashboard),
        (RoleCode.StationStaff, PermissionAction.CreateIncident),
        (RoleCode.StationStaff, PermissionAction.ViewIncidents),
        (RoleCode.StationStaff, PermissionAction.UpdateIncidentLifecycle),
        (RoleCode.StationStaff, PermissionAction.DiscoverCateringOptions),
        (RoleCode.Controller, PermissionAction.SearchTrainsStationsAndSchedules),
        (RoleCode.Controller, PermissionAction.ViewPublishedJourneyStatus),
        (RoleCode.Controller, PermissionAction.ViewStationDepartures),
        (RoleCode.Controller, PermissionAction.ViewOperationsDashboard),
        (RoleCode.Controller, PermissionAction.ViewDriverAssignments),
        (RoleCode.Controller, PermissionAction.ReviewDriverObservations),
        (RoleCode.Controller, PermissionAction.PublishOfficialJourneyStatus),
        (RoleCode.Controller, PermissionAction.ChangePlatformOrOperationalTiming),
        (RoleCode.Controller, PermissionAction.ConfirmJourneyCancellation),
        (RoleCode.Controller, PermissionAction.CreateIncident),
        (RoleCode.Controller, PermissionAction.ViewIncidents),
        (RoleCode.Controller, PermissionAction.UpdateIncidentLifecycle),
        (RoleCode.Controller, PermissionAction.DiscoverCateringOptions)
    ];

    private static readonly PermissionAction[] ApprovedPublicGrants =
    [
        PermissionAction.SearchTrainsStationsAndSchedules,
        PermissionAction.ViewPublishedJourneyStatus,
        PermissionAction.ViewStationDepartures,
        PermissionAction.DiscoverCateringOptions
    ];

    [Fact]
    public void Every_role_action_cell_matches_the_approved_RBAC_matrix()
    {
        foreach (var role in Enum.GetValues<RoleCode>())
        foreach (var action in Enum.GetValues<PermissionAction>())
        {
            var expected = ApprovedRoleGrants.Contains((role, action));
            Assert.Equal(expected, RolePermissionCatalog.IsGranted(role, action));
        }
    }

    [Fact]
    public void Public_grants_are_separate_from_assignable_roles_and_match_the_matrix()
    {
        Assert.DoesNotContain("Public", Enum.GetNames<RoleCode>());
        foreach (var action in Enum.GetValues<PermissionAction>())
        {
            Assert.Equal(ApprovedPublicGrants.Contains(action), RolePermissionCatalog.IsPubliclyGranted(action));
        }
    }

    [Fact]
    public void Multiple_active_roles_union_only_their_explicit_grants()
    {
        RoleCode[] roles = [RoleCode.Driver, RoleCode.StationStaff];
        var grants = RolePermissionCatalog.GetGrantedActions(roles);

        Assert.Contains(PermissionAction.ViewDriverAssignments, grants);
        Assert.Contains(PermissionAction.ViewOperationsDashboard, grants);
        Assert.Contains(PermissionAction.SubmitDriverProgressObservation, grants);
        Assert.Contains(PermissionAction.CreateIncident, grants);
        Assert.DoesNotContain(PermissionAction.ConfirmJourneyCancellation, grants);
        Assert.False(RolePermissionCatalog.IsGranted(roles, PermissionAction.ConfirmJourneyCancellation));
    }

    [Fact]
    public void Unknown_role_or_action_and_empty_roles_fail_closed()
    {
        Assert.Empty(RolePermissionCatalog.GetGrantedActions([]));
        Assert.Empty(RolePermissionCatalog.GetGrantedActions([(RoleCode)99]));
        Assert.False(RolePermissionCatalog.IsGranted((RoleCode)99, PermissionAction.SearchTrainsStationsAndSchedules));
        Assert.False(RolePermissionCatalog.IsGranted(RoleCode.Controller, (PermissionAction)99));
        Assert.False(RolePermissionCatalog.IsGranted(Array.Empty<RoleCode>(), PermissionAction.ConfirmJourneyCancellation));
    }

    [Fact]
    public void Returned_grant_union_cannot_mutate_the_catalog()
    {
        var grants = RolePermissionCatalog.GetGrantedActions([RoleCode.Passenger]);

        Assert.IsAssignableFrom<IReadOnlySet<PermissionAction>>(grants);
        Assert.Throws<NotSupportedException>(() => ((ISet<PermissionAction>)grants).Add(PermissionAction.ConfirmJourneyCancellation));
        Assert.False(RolePermissionCatalog.IsGranted(RoleCode.Passenger, PermissionAction.ConfirmJourneyCancellation));
    }
}
