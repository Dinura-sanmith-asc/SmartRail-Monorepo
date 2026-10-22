namespace SmartRail.Domain.Identity.Entities;

/// <summary>Immutable evidence for one role-assignment lifecycle event.</summary>
public sealed class RoleAssignmentAudit
{
    public Guid Id { get; }
    public Guid RoleAssignmentId { get; }
    public AssignmentAuditAction Action { get; }
    public string ActorSubject { get; }
    public DateTimeOffset OccurredAtUtc { get; }
    public string? EvidenceReference { get; }

    public RoleAssignmentAudit(
        Guid id,
        Guid roleAssignmentId,
        AssignmentAuditAction action,
        string actorSubject,
        DateTimeOffset occurredAtUtc,
        string? evidenceReference = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Role assignment audit ID must be a non-empty UUID.", nameof(id));
        }

        if (roleAssignmentId == Guid.Empty)
        {
            throw new ArgumentException("Role assignment ID must be a non-empty UUID.", nameof(roleAssignmentId));
        }

        if (!Enum.IsDefined(action))
        {
            throw new ArgumentOutOfRangeException(nameof(action), "Only granted and revoked actions are supported.");
        }

        ActorSubject = RoleAssignment.RequireOpaqueSubject(actorSubject, nameof(actorSubject));
        OccurredAtUtc = RoleAssignment.RequireUtc(occurredAtUtc, nameof(occurredAtUtc));

        Id = id;
        RoleAssignmentId = roleAssignmentId;
        Action = action;
        EvidenceReference = RequireOptionalEvidence(evidenceReference);
    }

    public static RoleAssignmentAudit ForGrant(Guid id, RoleAssignment assignment, string? evidenceReference = null)
    {
        ArgumentNullException.ThrowIfNull(assignment);
        return new RoleAssignmentAudit(id, assignment.Id, AssignmentAuditAction.Granted,
            assignment.GrantedBySubject, assignment.GrantedAtUtc, evidenceReference);
    }

    public static RoleAssignmentAudit ForRevocation(Guid id, RoleAssignment assignment, string? evidenceReference = null)
    {
        ArgumentNullException.ThrowIfNull(assignment);
        if (assignment.RevokedAtUtc is null || assignment.RevokedBySubject is null)
        {
            throw new ArgumentException("A revoke audit requires a revoked assignment.", nameof(assignment));
        }

        return new RoleAssignmentAudit(id, assignment.Id, AssignmentAuditAction.Revoked,
            assignment.RevokedBySubject, assignment.RevokedAtUtc.Value, evidenceReference);
    }

    private static string? RequireOptionalEvidence(string? evidenceReference)
    {
        if (evidenceReference is null) return null;
        if (string.IsNullOrWhiteSpace(evidenceReference))
        {
            throw new ArgumentException("Evidence reference must be omitted or contain non-whitespace text.", nameof(evidenceReference));
        }

        return evidenceReference.Trim();
    }
}
