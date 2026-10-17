namespace SmartRail.Domain.RailwayNetwork.Entities;

/// <summary>Immutable timetable values with explicit publication and supersession lifecycle.</summary>
public sealed class ServiceScheduleRevision
{
    public const string RailwayTimeZoneId = "Asia/Colombo";

    public Guid ScheduleId { get; }
    public Guid ServiceId { get; }
    public int Revision { get; }
    public TimeOnly FirstDepartureLocalTime { get; }
    public string TimeZoneId => RailwayTimeZoneId;
    public bool IsPublished { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset? PublishedAt { get; private set; }
    public DateTimeOffset? SupersededAt { get; private set; }
    public bool IsCurrentPublished => IsPublished && SupersededAt is null;

    public ServiceScheduleRevision(
        Guid scheduleId,
        Guid serviceId,
        int revision,
        TimeOnly firstDepartureLocalTime,
        DateTimeOffset createdAt,
        bool isPublished = false,
        DateTimeOffset? publishedAt = null,
        DateTimeOffset? supersededAt = null)
    {
        if (scheduleId == Guid.Empty) throw new ArgumentException("Schedule ID must be a non-empty UUID.", nameof(scheduleId));
        if (serviceId == Guid.Empty) throw new ArgumentException("Service ID must be a non-empty UUID.", nameof(serviceId));
        if (revision <= 0) throw new ArgumentOutOfRangeException(nameof(revision), "Revision must be positive.");
        if (isPublished != publishedAt.HasValue) throw new ArgumentException("Published state and publication instant must agree.", nameof(publishedAt));
        if (supersededAt.HasValue && (!isPublished || supersededAt < publishedAt))
            throw new ArgumentException("A superseded revision must have been published first.", nameof(supersededAt));

        ScheduleId = scheduleId;
        ServiceId = serviceId;
        Revision = revision;
        FirstDepartureLocalTime = firstDepartureLocalTime;
        CreatedAt = createdAt;
        IsPublished = isPublished;
        PublishedAt = publishedAt;
        SupersededAt = supersededAt;
    }

    public void Publish(DateTimeOffset publishedAt)
    {
        if (IsPublished || SupersededAt.HasValue) throw new InvalidOperationException("Only an unpublished revision can be published.");
        if (publishedAt < CreatedAt) throw new ArgumentOutOfRangeException(nameof(publishedAt), "Publication cannot precede creation.");
        IsPublished = true;
        PublishedAt = publishedAt;
    }

    public void Supersede(DateTimeOffset supersededAt)
    {
        if (!IsCurrentPublished || !PublishedAt.HasValue) throw new InvalidOperationException("Only a current published revision can be superseded.");
        if (supersededAt < PublishedAt.Value) throw new ArgumentOutOfRangeException(nameof(supersededAt), "Supersession cannot precede publication.");
        SupersededAt = supersededAt;
    }
}
