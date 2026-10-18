namespace SmartRail.Domain.Identity.Entities;

/// <summary>
/// One station-scope lifecycle for an opaque Cognito subject and an existing
/// Railway Network station ID. Scope alone does not grant an action.
/// </summary>
public sealed class StationScopeAssignment
{
    public Guid Id { get; }
    public string CognitoSubject { get; }
    public Guid StationId { get; }
    public DateTimeOffset GrantedAtUtc { get; }
    public string GrantedBySubject { get; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public string? RevokedBySubject { get; private set; }
    public bool IsActive => RevokedAtUtc is null && RevokedBySubject is null;

    public StationScopeAssignment(
        Guid id,
        string cognitoSubject,
        Guid stationId,
        DateTimeOffset grantedAtUtc,
        string grantedBySubject)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Station-scope assignment ID must be a non-empty UUID.", nameof(id));
        }

        if (stationId == Guid.Empty)
        {
            throw new ArgumentException("Station ID must be a non-empty UUID.", nameof(stationId));
        }

        Id = id;
        CognitoSubject = RoleAssignment.RequireOpaqueSubject(cognitoSubject, nameof(cognitoSubject));
        StationId = stationId;
        GrantedAtUtc = RoleAssignment.RequireUtc(grantedAtUtc, nameof(grantedAtUtc));
        GrantedBySubject = RoleAssignment.RequireOpaqueSubject(grantedBySubject, nameof(grantedBySubject));
    }

    /// <summary>Closes this station-scope lifecycle. Reassignment creates a new instance.</summary>
    public void Revoke(string revokedBySubject, DateTimeOffset revokedAtUtc)
    {
        if (!IsActive)
        {
            throw new InvalidOperationException("A station-scope assignment can be revoked only once.");
        }

        var actor = RoleAssignment.RequireOpaqueSubject(revokedBySubject, nameof(revokedBySubject));
        var revokedAt = RoleAssignment.RequireUtc(revokedAtUtc, nameof(revokedAtUtc));
        if (revokedAt < GrantedAtUtc)
        {
            throw new ArgumentOutOfRangeException(nameof(revokedAtUtc), "Revocation cannot precede the grant.");
        }

        RevokedBySubject = actor;
        RevokedAtUtc = revokedAt;
    }
}
