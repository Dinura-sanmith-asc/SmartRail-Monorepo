using System.Collections.Frozen;

namespace SmartRail.Domain.Identity;

/// <summary>Actions represented by the approved SmartRail RBAC matrix.</summary>
public enum PermissionAction
{
    SearchTrainsStationsAndSchedules = 1,
    ViewPublishedJourneyStatus = 2,
    ViewStationDepartures = 3,
    ViewOperationsDashboard = 4,
    ViewDriverAssignments = 5,
    SubmitDriverProgressObservation = 6,
    ReviewDriverObservations = 7,
    PublishOfficialJourneyStatus = 8,
    ChangePlatformOrOperationalTiming = 9,
    ConfirmJourneyCancellation = 10,
    CreateIncident = 11,
    ViewIncidents = 12,
    UpdateIncidentLifecycle = 13,
    DiscoverCateringOptions = 14,
    CreateCateringOrder = 15,
    ViewOwnCateringOrder = 16,
    ManageStations = 17
}

/// <summary>
/// Contains only role/action grants from the approved RBAC matrix. These checks
/// do not replace required resource scope or ownership authorization.
/// </summary>
public static class RolePermissionCatalog
{
    private static readonly FrozenSet<PermissionAction> PublicGrants =
    new[]
    {
        PermissionAction.SearchTrainsStationsAndSchedules,
        PermissionAction.ViewPublishedJourneyStatus,
        PermissionAction.ViewStationDepartures,
        PermissionAction.DiscoverCateringOptions
    }.ToFrozenSet();

    private static readonly FrozenDictionary<RoleCode, FrozenSet<PermissionAction>> RoleGrants =
        new Dictionary<RoleCode, FrozenSet<PermissionAction>>
        {
            [RoleCode.Passenger] = new[]
            {
                PermissionAction.SearchTrainsStationsAndSchedules,
                PermissionAction.ViewPublishedJourneyStatus,
                PermissionAction.ViewStationDepartures,
                PermissionAction.DiscoverCateringOptions,
                PermissionAction.CreateCateringOrder,
                PermissionAction.ViewOwnCateringOrder
            }.ToFrozenSet(),
            [RoleCode.Driver] = new[]
            {
                PermissionAction.SearchTrainsStationsAndSchedules,
                PermissionAction.ViewPublishedJourneyStatus,
                PermissionAction.ViewStationDepartures,
                PermissionAction.ViewDriverAssignments,
                PermissionAction.SubmitDriverProgressObservation,
                PermissionAction.DiscoverCateringOptions
            }.ToFrozenSet(),
            [RoleCode.StationStaff] = new[]
            {
                PermissionAction.SearchTrainsStationsAndSchedules,
                PermissionAction.ViewPublishedJourneyStatus,
                PermissionAction.ViewStationDepartures,
                PermissionAction.ViewOperationsDashboard,
                PermissionAction.CreateIncident,
                PermissionAction.ViewIncidents,
                PermissionAction.UpdateIncidentLifecycle,
                PermissionAction.DiscoverCateringOptions
            }.ToFrozenSet(),
            [RoleCode.Controller] = new[]
            {
                PermissionAction.SearchTrainsStationsAndSchedules,
                PermissionAction.ViewPublishedJourneyStatus,
                PermissionAction.ViewStationDepartures,
                PermissionAction.ViewOperationsDashboard,
                PermissionAction.ViewDriverAssignments,
                PermissionAction.ReviewDriverObservations,
                PermissionAction.PublishOfficialJourneyStatus,
                PermissionAction.ChangePlatformOrOperationalTiming,
                PermissionAction.ConfirmJourneyCancellation,
                PermissionAction.CreateIncident,
                PermissionAction.ViewIncidents,
                PermissionAction.UpdateIncidentLifecycle,
                PermissionAction.ManageStations,
                PermissionAction.DiscoverCateringOptions
            }.ToFrozenSet()
        }.ToFrozenDictionary();

    public static bool IsGranted(RoleCode role, PermissionAction action) =>
        RoleGrants.TryGetValue(role, out var grants) && grants.Contains(action);

    /// <summary>Returns whether any active role explicitly grants this action.</summary>
    public static bool IsGranted(IEnumerable<RoleCode> activeRoles, PermissionAction action)
    {
        ArgumentNullException.ThrowIfNull(activeRoles);
        return activeRoles.Any(role => IsGranted(role, action));
    }

    /// <summary>Returns the union of explicit grants for the supplied active roles.</summary>
    public static IReadOnlySet<PermissionAction> GetGrantedActions(IEnumerable<RoleCode> activeRoles)
    {
        ArgumentNullException.ThrowIfNull(activeRoles);
        return activeRoles
            .Where(RoleGrants.ContainsKey)
            .SelectMany(role => RoleGrants[role])
            .ToFrozenSet();
    }

    /// <summary>Public is a separate anonymous access class, never an assignable role.</summary>
    public static bool IsPubliclyGranted(PermissionAction action) => PublicGrants.Contains(action);
}
