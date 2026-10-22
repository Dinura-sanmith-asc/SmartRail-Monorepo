namespace SmartRail.Domain.RailwayNetwork.Entities;

/// <summary>An exact published date on which one schedule revision operates.</summary>
public sealed class ServiceOperationDate
{
    public Guid ScheduleId { get; }
    public DateOnly OperationDate { get; }

    public ServiceOperationDate(Guid scheduleId, DateOnly operationDate)
    {
        if (scheduleId == Guid.Empty) throw new ArgumentException("Schedule ID must be a non-empty UUID.", nameof(scheduleId));
        ScheduleId = scheduleId;
        OperationDate = operationDate;
    }
}
