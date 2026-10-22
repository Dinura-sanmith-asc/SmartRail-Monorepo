namespace SmartRail.Domain.DriverOperations.Entities;

/// <summary>One authoritative driver/journey assignment lifecycle. Re-grant uses a new identity.</summary>
public sealed class DriverJourneyAssignment
{
    private DriverJourneyAssignment() { }
    public Guid AssignmentId { get; private set; }
    public string DriverSubject { get; private set; } = string.Empty;
    public Guid JourneyInstanceId { get; private set; }
    public DateTimeOffset AssignedAtUtc { get; private set; }
    public string AssignedBySubject { get; private set; } = string.Empty;
    public string SourceReference { get; private set; } = string.Empty;
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public string? RevokedBySubject { get; private set; }
    public bool IsActive => RevokedAtUtc is null && RevokedBySubject is null;

    public DriverJourneyAssignment(Guid assignmentId, string driverSubject, Guid journeyInstanceId,
        DateTimeOffset assignedAtUtc, string assignedBySubject, string sourceReference)
    {
        AssignmentId = DriverDataGuard.Id(assignmentId, nameof(assignmentId));
        DriverSubject = DriverDataGuard.Subject(driverSubject, nameof(driverSubject));
        JourneyInstanceId = DriverDataGuard.Id(journeyInstanceId, nameof(journeyInstanceId));
        AssignedAtUtc = DriverDataGuard.Utc(assignedAtUtc, nameof(assignedAtUtc));
        AssignedBySubject = DriverDataGuard.Subject(assignedBySubject, nameof(assignedBySubject));
        SourceReference = DriverDataGuard.Text(sourceReference, nameof(sourceReference), 1000);
    }

    public void Revoke(string actorSubject, DateTimeOffset revokedAtUtc)
    {
        if (!IsActive) throw new InvalidOperationException("Assignment is already revoked; re-grant requires a new lifecycle.");
        var actor = DriverDataGuard.Subject(actorSubject, nameof(actorSubject));
        var at = DriverDataGuard.Utc(revokedAtUtc, nameof(revokedAtUtc));
        if (at < AssignedAtUtc) throw new ArgumentException("Revocation cannot precede assignment.", nameof(revokedAtUtc));
        RevokedBySubject = actor;
        RevokedAtUtc = at;
    }
}
