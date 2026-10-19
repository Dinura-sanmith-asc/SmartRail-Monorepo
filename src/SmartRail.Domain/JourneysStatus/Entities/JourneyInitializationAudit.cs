using SmartRail.Domain.JourneysStatus;

namespace SmartRail.Domain.JourneysStatus.Entities;

public sealed class JourneyInitializationAudit
{
    public Guid JourneyInitializationAuditId { get; private set; }
    public string SourceSystem { get; private set; } = string.Empty;
    public string SourceInstanceKey { get; private set; } = string.Empty;
    public Guid? JourneyInstanceId { get; private set; }
    public string? ActorSubject { get; private set; }
    public JourneyInitializationDecision Decision { get; private set; }
    public Guid? ServiceId { get; private set; }
    public Guid? ScheduleId { get; private set; }
    public int? ScheduleRevision { get; private set; }
    public DateOnly? OperationDate { get; private set; }
    public TimeOnly? FirstDepartureOccurrence { get; private set; }
    public string Provenance { get; private set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; private set; }

    private JourneyInitializationAudit() { }

    public JourneyInitializationAudit(Guid auditId, string sourceSystem, string sourceInstanceKey,
        JourneyInitializationDecision decision, DateTimeOffset occurredAt, string provenance,
        Guid? journeyInstanceId = null, string? actorSubject = null, Guid? serviceId = null,
        Guid? scheduleId = null, int? scheduleRevision = null, DateOnly? operationDate = null,
        TimeOnly? firstDepartureOccurrence = null)
    {
        if (auditId == Guid.Empty) throw new ArgumentException("Audit ID is required.", nameof(auditId));
        JourneyInitializationAuditId = auditId;
        SourceSystem = Required(sourceSystem, nameof(sourceSystem), 100);
        SourceInstanceKey = Required(sourceInstanceKey, nameof(sourceInstanceKey), 200);
        if (occurredAt == default) throw new ArgumentException("Audit timestamp is required.", nameof(occurredAt));
        if (decision == JourneyInitializationDecision.Accepted && (!journeyInstanceId.HasValue || string.IsNullOrWhiteSpace(actorSubject)))
            throw new ArgumentException("Accepted audit requires journey and verified actor evidence.");
        if (scheduleRevision is <= 0) throw new ArgumentOutOfRangeException(nameof(scheduleRevision));
        Decision = decision;
        OccurredAt = occurredAt;
        Provenance = Required(provenance, nameof(provenance), 1000);
        JourneyInstanceId = journeyInstanceId;
        ActorSubject = string.IsNullOrWhiteSpace(actorSubject) ? null : actorSubject.Trim();
        ServiceId = serviceId;
        ScheduleId = scheduleId;
        ScheduleRevision = scheduleRevision;
        OperationDate = operationDate;
        FirstDepartureOccurrence = firstDepartureOccurrence;
    }

    private static string Required(string value, string name, int maxLength) =>
        string.IsNullOrWhiteSpace(value) || value.Trim().Length > maxLength
            ? throw new ArgumentException($"{name} is required and bounded.", name) : value.Trim();
}
