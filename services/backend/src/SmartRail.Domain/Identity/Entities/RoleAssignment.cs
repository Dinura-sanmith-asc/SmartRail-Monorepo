using SmartRail.Domain.Identity;

namespace SmartRail.Domain.Identity.Entities;

/// <summary>
/// One grant lifecycle for a recognized SmartRail role and opaque Cognito subject.
/// A later grant after revocation must be represented by a new assignment.
/// </summary>
public sealed class RoleAssignment
{
    public Guid Id { get; }
    public string CognitoSubject { get; }
    public RoleCode RoleCode { get; }
    public DateTimeOffset GrantedAtUtc { get; }
    public string GrantedBySubject { get; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public string? RevokedBySubject { get; private set; }
    public bool IsActive => RevokedAtUtc is null && RevokedBySubject is null;

    public RoleAssignment(
        Guid id,
        string cognitoSubject,
        RoleCode roleCode,
        DateTimeOffset grantedAtUtc,
        string grantedBySubject)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Role assignment ID must be a non-empty UUID.", nameof(id));
        }

        if (!Enum.IsDefined(roleCode))
        {
            throw new ArgumentOutOfRangeException(nameof(roleCode), "Role code must be a recognized assignable SmartRail role.");
        }

        Id = id;
        CognitoSubject = RequireOpaqueSubject(cognitoSubject, nameof(cognitoSubject));
        RoleCode = roleCode;
        GrantedAtUtc = RequireUtc(grantedAtUtc, nameof(grantedAtUtc));
        GrantedBySubject = RequireOpaqueSubject(grantedBySubject, nameof(grantedBySubject));
    }

    /// <summary>Closes this assignment lifecycle. A revoked assignment cannot be reactivated.</summary>
    public void Revoke(string revokedBySubject, DateTimeOffset revokedAtUtc)
    {
        if (!IsActive)
        {
            throw new InvalidOperationException("A role assignment can be revoked only once.");
        }

        var actor = RequireOpaqueSubject(revokedBySubject, nameof(revokedBySubject));
        var revokedAt = RequireUtc(revokedAtUtc, nameof(revokedAtUtc));
        if (revokedAt < GrantedAtUtc)
        {
            throw new ArgumentOutOfRangeException(nameof(revokedAtUtc), "Revocation cannot precede the grant.");
        }

        RevokedBySubject = actor;
        RevokedAtUtc = revokedAt;
    }

    internal static string RequireOpaqueSubject(string value, string parameterName) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Verified subject is required.", parameterName)
            : value;

    internal static DateTimeOffset RequireUtc(DateTimeOffset value, string parameterName)
    {
        if (value == default || value.Offset != TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(parameterName, "A non-default UTC timestamp is required.");
        }

        return value;
    }
}
