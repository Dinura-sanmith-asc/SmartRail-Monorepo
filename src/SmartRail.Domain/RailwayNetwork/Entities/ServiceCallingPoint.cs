namespace SmartRail.Domain.RailwayNetwork.Entities;

public sealed class ServiceCallingPoint
{
    public Guid ScheduleId { get; }
    public Guid RouteStopId { get; }
    public int? ArrivalOffsetMinutes { get; }
    public int? DepartureOffsetMinutes { get; }

    public ServiceCallingPoint(Guid scheduleId, Guid routeStopId, int? arrivalOffsetMinutes, int? departureOffsetMinutes)
    {
        if (scheduleId == Guid.Empty) throw new ArgumentException("Schedule ID must be a non-empty UUID.", nameof(scheduleId));
        if (routeStopId == Guid.Empty) throw new ArgumentException("Route stop ID must be a non-empty UUID.", nameof(routeStopId));
        if (arrivalOffsetMinutes < 0) throw new ArgumentOutOfRangeException(nameof(arrivalOffsetMinutes), "Offsets cannot be negative.");
        if (departureOffsetMinutes < 0) throw new ArgumentOutOfRangeException(nameof(departureOffsetMinutes), "Offsets cannot be negative.");
        if (arrivalOffsetMinutes.HasValue && departureOffsetMinutes.HasValue && arrivalOffsetMinutes > departureOffsetMinutes)
            throw new ArgumentException("Arrival cannot be after departure at the same call.", nameof(arrivalOffsetMinutes));
        ScheduleId = scheduleId;
        RouteStopId = routeStopId;
        ArrivalOffsetMinutes = arrivalOffsetMinutes;
        DepartureOffsetMinutes = departureOffsetMinutes;
    }
}
