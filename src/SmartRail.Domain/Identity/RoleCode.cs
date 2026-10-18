namespace SmartRail.Domain.Identity;

/// <summary>
/// An assignable SmartRail application role. Public access is an anonymous
/// access class, not an identity role.
/// </summary>
public enum RoleCode
{
    Passenger = 1,
    Driver = 2,
    StationStaff = 3,
    Controller = 4
}
