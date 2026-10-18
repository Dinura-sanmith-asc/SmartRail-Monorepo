namespace SmartRail.Api.Contracts.Identity;

public sealed record CurrentIdentityResponse(string Subject, IReadOnlyList<string> Roles)
{
    public static string ToExternalRoleLabel(SmartRail.Domain.Identity.RoleCode role) => role switch
    {
        SmartRail.Domain.Identity.RoleCode.Passenger => "Passenger",
        SmartRail.Domain.Identity.RoleCode.Driver => "Driver",
        SmartRail.Domain.Identity.RoleCode.StationStaff => "Station Staff",
        SmartRail.Domain.Identity.RoleCode.Controller => "Controller",
        _ => throw new ArgumentOutOfRangeException(nameof(role), "Unrecognized SmartRail role.")
    };
}
