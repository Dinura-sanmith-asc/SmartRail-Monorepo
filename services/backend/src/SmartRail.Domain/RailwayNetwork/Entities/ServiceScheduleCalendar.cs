namespace SmartRail.Domain.RailwayNetwork.Entities;

public sealed class ServiceScheduleCalendar
{
    public Guid ScheduleId { get; }
    public DateOnly ValidFrom { get; }
    public DateOnly ValidThrough { get; }
    public int WeekdaysMask { get; }

    public ServiceScheduleCalendar(Guid scheduleId, DateOnly validFrom, DateOnly validThrough, int weekdaysMask)
    {
        if (scheduleId == Guid.Empty) throw new ArgumentException("Schedule ID is required.", nameof(scheduleId));
        if (validThrough < validFrom) throw new ArgumentException("End date must follow start date.", nameof(validThrough));
        if (weekdaysMask < 1 || weekdaysMask > 127) throw new ArgumentOutOfRangeException(nameof(weekdaysMask));
        ScheduleId = scheduleId;
        ValidFrom = validFrom;
        ValidThrough = validThrough;
        WeekdaysMask = weekdaysMask;
    }

    public bool RunsOn(DateOnly date) => date >= ValidFrom && date <= ValidThrough &&
        (WeekdaysMask & (1 << (int)date.DayOfWeek)) != 0;
}

public sealed class ServiceScheduleDateException(Guid scheduleId, DateOnly date, bool runs)
{
    public Guid ScheduleId { get; } = scheduleId;
    public DateOnly Date { get; } = date;
    public bool Runs { get; } = runs;
}
