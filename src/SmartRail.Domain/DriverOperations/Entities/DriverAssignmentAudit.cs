namespace SmartRail.Domain.DriverOperations.Entities;

/// <summary>Immutable provenance of a grant or revocation.</summary>
public sealed class DriverAssignmentAudit
{
    private DriverAssignmentAudit() { }
    public Guid AuditId { get; private set; }
    public Guid AssignmentId { get; private set; }
    public DriverAssignmentAuditAction Action { get; private set; }
    public string ActorSubject { get; private set; } = string.Empty;
    public DateTimeOffset OccurredAtUtc { get; private set; }
    public string EvidenceReference { get; private set; } = string.Empty;

    public DriverAssignmentAudit(Guid auditId, Guid assignmentId, DriverAssignmentAuditAction action,
        string actorSubject, DateTimeOffset occurredAtUtc, string evidenceReference)
    {
        AuditId = DriverDataGuard.Id(auditId, nameof(auditId));
        AssignmentId = DriverDataGuard.Id(assignmentId, nameof(assignmentId));
        if (!Enum.IsDefined(action)) throw new ArgumentOutOfRangeException(nameof(action));
        Action = action;
        ActorSubject = DriverDataGuard.Subject(actorSubject, nameof(actorSubject));
        OccurredAtUtc = DriverDataGuard.Utc(occurredAtUtc, nameof(occurredAtUtc));
        EvidenceReference = DriverDataGuard.Text(evidenceReference, nameof(evidenceReference), 1000);
    }
}
