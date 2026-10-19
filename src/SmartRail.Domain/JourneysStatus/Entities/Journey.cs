namespace SmartRail.Domain.JourneysStatus.Entities;

public sealed class Journey
{
    public Guid JourneyInstanceId { get; private set; }
    public Guid ServiceId { get; private set; }
    public Guid ScheduleId { get; private set; }
    public int ScheduleRevision { get; private set; }
    public DateOnly OperationDate { get; private set; }
    public TimeOnly FirstDepartureOccurrence { get; private set; }
    public string SourceSystem { get; private set; } = string.Empty;
    public string SourceInstanceKey { get; private set; } = string.Empty;
    public DateTimeOffset InitializedAt { get; private set; }
    public string InitializedBySubject { get; private set; } = string.Empty;
    public string InitializationProvenance { get; private set; } = string.Empty;
    public int CurrentRevision { get; private set; }

    private Journey() { }

    public Journey(Guid journeyInstanceId, Guid serviceId, Guid scheduleId, int scheduleRevision,
        DateOnly operationDate, TimeOnly firstDepartureOccurrence, string sourceSystem,
        string sourceInstanceKey, DateTimeOffset initializedAt, string initializedBySubject,
        string initializationProvenance, int currentRevision = 1)
    {
        if (journeyInstanceId == Guid.Empty) throw new ArgumentException("Journey instance ID is required.", nameof(journeyInstanceId));
        if (serviceId == Guid.Empty) throw new ArgumentException("Service ID is required.", nameof(serviceId));
        if (scheduleId == Guid.Empty) throw new ArgumentException("Schedule ID is required.", nameof(scheduleId));
        if (scheduleRevision <= 0) throw new ArgumentOutOfRangeException(nameof(scheduleRevision));
        if (operationDate == default) throw new ArgumentException("Operation date is required.", nameof(operationDate));
        if (currentRevision <= 0) throw new ArgumentOutOfRangeException(nameof(currentRevision));
        JourneyInstanceId = journeyInstanceId;
        ServiceId = serviceId;
        ScheduleId = scheduleId;
        ScheduleRevision = scheduleRevision;
        OperationDate = operationDate;
        FirstDepartureOccurrence = firstDepartureOccurrence;
        SourceSystem = Required(sourceSystem, nameof(sourceSystem), 100);
        SourceInstanceKey = Required(sourceInstanceKey, nameof(sourceInstanceKey), 200);
        if (initializedAt == default) throw new ArgumentException("Initialization time is required.", nameof(initializedAt));
        InitializedAt = initializedAt;
        InitializedBySubject = Required(initializedBySubject, nameof(initializedBySubject), 300);
        InitializationProvenance = Required(initializationProvenance, nameof(initializationProvenance), 1000);
        CurrentRevision = currentRevision;
    }

    public void AdvanceRevision(int expectedRevision)
    {
        if (CurrentRevision != expectedRevision) throw new InvalidOperationException("Journey revision is stale.");
        CurrentRevision = checked(CurrentRevision + 1);
    }

    private static string Required(string value, string name, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > maxLength)
            throw new ArgumentException($"{name} is required and must be at most {maxLength} characters.", name);
        return value.Trim();
    }
}
