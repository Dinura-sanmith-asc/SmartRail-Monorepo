using SmartRail.Domain.JourneysStatus;

namespace SmartRail.Domain.JourneysStatus.Entities;

public sealed class JourneyRevision
{
    public Guid JourneyInstanceId { get; private set; }
    public int Revision { get; private set; }
    public Guid? UpdateId { get; private set; }
    public JourneyStatus Status { get; private set; }
    public JourneyPublicationKind PublicationKind { get; private set; }
    public int? PreviousRevision { get; private set; }
    public DateTimeOffset PublishedAt { get; private set; }
    public string? PublishedBySubject { get; private set; }
    public JourneySource Source { get; private set; }
    public Guid? DriverObservationId { get; private set; }
    public Guid? CorrectionOfUpdateId { get; private set; }
    public string PublicChangeType { get; private set; } = string.Empty;
    public string PrivateAuditEvidence { get; private set; } = string.Empty;

    private JourneyRevision() { }

    public JourneyRevision(Guid journeyInstanceId, int revision, JourneyStatus status,
        DateTimeOffset publishedAt, string source, string? publicChangeType = null,
        Guid? updateId = null, JourneyPublicationKind publicationKind = JourneyPublicationKind.InitializationBaseline,
        int? previousRevision = null, string? publishedBySubject = null, Guid? driverObservationId = null,
        Guid? correctionOfUpdateId = null, string? privateAuditEvidence = null)
    {
        if (journeyInstanceId == Guid.Empty) throw new ArgumentException("Journey instance ID is required.", nameof(journeyInstanceId));
        if (revision <= 0) throw new ArgumentOutOfRangeException(nameof(revision));
        if (publishedAt == default) throw new ArgumentException("Revision timestamp is required.", nameof(publishedAt));
        if (previousRevision.HasValue && (previousRevision.Value <= 0 || previousRevision.Value >= revision))
            throw new ArgumentException("Previous revision must be a lower positive revision.", nameof(previousRevision));
        if (publicationKind == JourneyPublicationKind.InitializationBaseline && updateId.HasValue)
            throw new ArgumentException("Initialization baseline cannot have a public UpdateId.", nameof(updateId));
        if (publicationKind != JourneyPublicationKind.InitializationBaseline && !updateId.HasValue)
            throw new ArgumentException("Official publication requires an UpdateId.", nameof(updateId));
        if (publicationKind == JourneyPublicationKind.Correction && !correctionOfUpdateId.HasValue)
            throw new ArgumentException("Correction must reference an earlier UpdateId.", nameof(correctionOfUpdateId));
        if (publicationKind != JourneyPublicationKind.InitializationBaseline && string.IsNullOrWhiteSpace(publishedBySubject))
            throw new ArgumentException("Official publication requires a verified actor subject.", nameof(publishedBySubject));
        JourneyInstanceId = journeyInstanceId;
        Revision = revision;
        Status = status;
        PublishedAt = publishedAt;
        Source = ParseSource(source);
        PublicChangeType = publicChangeType?.Trim() ?? string.Empty;
        UpdateId = updateId;
        PublicationKind = publicationKind;
        PreviousRevision = previousRevision;
        PublishedBySubject = publishedBySubject?.Trim();
        DriverObservationId = driverObservationId;
        CorrectionOfUpdateId = correctionOfUpdateId;
        PrivateAuditEvidence = privateAuditEvidence?.Trim() ?? string.Empty;
    }

    private static JourneySource ParseSource(string value) => Enum.TryParse<JourneySource>(value, false, out var parsed)
        ? parsed : throw new ArgumentException("Unknown journey source.", nameof(value));
}
