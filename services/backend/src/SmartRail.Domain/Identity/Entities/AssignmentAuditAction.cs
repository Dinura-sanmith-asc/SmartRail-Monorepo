namespace SmartRail.Domain.Identity.Entities;

/// <summary>Supported lifecycle events for Identity role and station-scope assignments.</summary>
public enum AssignmentAuditAction
{
    Granted = 1,
    Revoked = 2
}
