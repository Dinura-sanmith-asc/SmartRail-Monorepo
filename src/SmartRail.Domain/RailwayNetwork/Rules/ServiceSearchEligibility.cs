using SmartRail.Domain.RailwayNetwork.Entities;

namespace SmartRail.Domain.RailwayNetwork.Rules;

public enum ServiceSearchEligibilityStatus
{
    Eligible,
    NotEligible,
    Unavailable
}

public sealed record ServiceSearchEligibilityResult(ServiceSearchEligibilityStatus Status, DateOnly? OperationDate, string? Reason)
{
    public bool IsEligible => Status == ServiceSearchEligibilityStatus.Eligible;
}

public static class ServiceSearchEligibility
{
    private const int MinutesPerDay = 24 * 60;

    /// <summary>Maps the requested origin's local departure date to the schedule's first-call operation date.</summary>
    public static DateOnly CalculateOperationDate(DateOnly requestedOriginDate, TimeOnly firstDepartureLocalTime, int originDepartureOffsetMinutes)
    {
        if (originDepartureOffsetMinutes < 0) throw new ArgumentOutOfRangeException(nameof(originDepartureOffsetMinutes));
        var totalMinutes = firstDepartureLocalTime.Hour * 60 + firstDepartureLocalTime.Minute + originDepartureOffsetMinutes;
        var originDayOffset = totalMinutes / MinutesPerDay;
        return requestedOriginDate.AddDays(-originDayOffset);
    }

    public static ServiceSearchEligibilityResult Evaluate(
        DateOnly requestedOriginDate,
        ServiceScheduleRevision schedule,
        ServiceDateManifest? manifest,
        ServiceOperationDate? operationDate,
        ServiceCallingPoint? originCall,
        RouteStop? originStop,
        ServiceCallingPoint? destinationCall,
        RouteStop? destinationStop)
    {
        ArgumentNullException.ThrowIfNull(schedule);
        if (!schedule.IsCurrentPublished) return new(ServiceSearchEligibilityStatus.NotEligible, null, "Schedule revision is not currently published.");
        if (manifest is null || !manifest.IsComplete) return new(ServiceSearchEligibilityStatus.Unavailable, null, "Complete operation-date publication is unavailable.");
        if (originCall is null || destinationCall is null || originStop is null || destinationStop is null)
            return new(ServiceSearchEligibilityStatus.NotEligible, null, "The service does not have both selected calling points.");
        if (originCall.ScheduleId != schedule.ScheduleId || destinationCall.ScheduleId != schedule.ScheduleId)
            return new(ServiceSearchEligibilityStatus.NotEligible, null, "Calling points do not belong to the current schedule revision.");
        if (originCall.RouteStopId != originStop.RouteStopId || destinationCall.RouteStopId != destinationStop.RouteStopId)
            return new(ServiceSearchEligibilityStatus.NotEligible, null, "Calling points do not match the selected route stops.");
        if (originStop.RouteId != destinationStop.RouteId || originStop.StopOrder >= destinationStop.StopOrder)
            return new(ServiceSearchEligibilityStatus.NotEligible, null, "Origin must precede destination on the route.");
        if (!originCall.DepartureOffsetMinutes.HasValue || !destinationCall.ArrivalOffsetMinutes.HasValue)
            return new(ServiceSearchEligibilityStatus.Unavailable, null, "Required origin departure or destination arrival time is unavailable.");

        var derivedOperationDate = CalculateOperationDate(requestedOriginDate, schedule.FirstDepartureLocalTime, originCall.DepartureOffsetMinutes.Value);
        if (manifest.OperationDate != derivedOperationDate)
            return new(ServiceSearchEligibilityStatus.Unavailable, derivedOperationDate, "Completeness evidence does not cover the derived operation date.");
        if (operationDate is null) return new(ServiceSearchEligibilityStatus.NotEligible, derivedOperationDate, "The complete publication confirms no operation for this service and date.");
        if (operationDate.ScheduleId != schedule.ScheduleId || operationDate.OperationDate != derivedOperationDate)
            return new(ServiceSearchEligibilityStatus.Unavailable, derivedOperationDate, "Service operation evidence does not match the schedule and date.");

        return new(ServiceSearchEligibilityStatus.Eligible, derivedOperationDate, null);
    }
}
