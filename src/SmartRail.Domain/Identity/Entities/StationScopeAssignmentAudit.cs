namespace SmartRail.Domain.Identity.Entities;

/// <summary>Immutable evidence for one station-scope assignment lifecycle event.</summary>
public sealed class StationScopeAssignmentAudit
{
    public Guid Id { get; }
    public Guid StationScopeAssignmentId { get; }
    public AssignmentAuditAction Action { get; }
    public string ActorSubject { get; }
    public DateTimeOffset OccurredAtUtc { get; }
    public string? EvidenceReference { get; }

    public StationScopeAssignmentAudit(
        Guid id,
        Guid stationScopeAssignmentId,
        AssignmentAuditAction action,
        string actorSubject,
        DateTimeOffset occurredAtUtc,
        string? evidenceReference = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Station-scope audit ID must be a non-empty UUID.", nameof(id));
        }

        if (stationScopeAssignmentId == Guid.Empty)
        {
            throw new ArgumentException("Station-scope assignment ID must be a non-empty UUID.", nameof(stationScopeAssignmentId));
        }

        if (!Enum.IsDefined(action))
        {
            throw new ArgumentOutOfRangeException(nameof(action), "Only granted and revoked actions are supported.");
        }

        ActorSubject = RoleAssignment.RequireOpaqueSubject(actorSubject, nameof(actorSubject));
        OccurredAtUtc = RoleAssignment.RequireUtc(occurredAtUtc, nameof(occurredAtUtc));

        Id = id;
        StationScopeAssignmentId = stationScopeAssignmentId;
        Action = action;
        EvidenceReference = RequireOptionalEvidence(evidenceReference);
    }

    public static StationScopeAssignmentAudit ForGrant(
        Guid id,
        StationScopeAssignment assignment,
        string? evidenceReference = null)
    {
        ArgumentNullException.ThrowIfNull(assignment);
        return new StationScopeAssignmentAudit(id, assignment.Id, AssignmentAuditAction.Granted,
            assignment.GrantedBySubject, assignment.GrantedAtUtc, evidenceReference);
    }

    public static StationScopeAssignmentAudit ForRevocation(
        Guid id,
        StationScopeAssignment assignment,
        string? evidenceReference = null)
    {
        ArgumentNullException.ThrowIfNull(assignment);
        if (assignment.RevokedAtUtc is null || assignment.RevokedBySubject is null)
        {
            throw new ArgumentException("A revoke audit requires a revoked assignment.", nameof(assignment));
        }

        return new StationScopeAssignmentAudit(id, assignment.Id, AssignmentAuditAction.Revoked,
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
