namespace SmartRail.Domain.DriverOperations.Entities;

/// <summary>Immutable driver evidence, never authoritative railway state.</summary>
public sealed class DriverObservation
{
    private DriverObservation() { }
    public Guid ObservationId { get; private set; }
    public Guid AssignmentId { get; private set; }
    public Guid JourneyInstanceId { get; private set; }
    public string DriverSubject { get; private set; } = string.Empty;
    public Guid RequestId { get; private set; }
    public string Text { get; private set; } = string.Empty;
    public DateTimeOffset ReportedAtUtc { get; private set; }
    public int JourneyRevision { get; private set; }

    public DriverObservation(Guid observationId, Guid assignmentId, Guid journeyInstanceId, string driverSubject,
        Guid requestId, string text, DateTimeOffset reportedAtUtc, int journeyRevision)
    {
        ObservationId = DriverDataGuard.Id(observationId, nameof(observationId));
        AssignmentId = DriverDataGuard.Id(assignmentId, nameof(assignmentId));
        JourneyInstanceId = DriverDataGuard.Id(journeyInstanceId, nameof(journeyInstanceId));
        DriverSubject = DriverDataGuard.Subject(driverSubject, nameof(driverSubject));
        RequestId = DriverDataGuard.Id(requestId, nameof(requestId));
        Text = DriverDataGuard.Text(text, nameof(text), 4000);
        ReportedAtUtc = DriverDataGuard.Utc(reportedAtUtc, nameof(reportedAtUtc));
        if (journeyRevision < 1) throw new ArgumentOutOfRangeException(nameof(journeyRevision));
        JourneyRevision = journeyRevision;
    }
}
