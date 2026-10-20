using SmartRail.Application.Operations;

namespace SmartRail.Api.Contracts.Operations;

public sealed record OperationsOverviewResponse(DateOnly OperationalDate,
    OperationsOverviewMetrics Metrics, IReadOnlyList<OperationsJourneyResponse> Journeys);

public sealed record OperationsJourneyResponse(Guid JourneyId, Guid ServiceId, string ServiceCode,
    string? ServiceDisplayName, string? PublicServiceNumber, string RouteCode, string RouteName,
    DateOnly OperationDate, string Origin, string Destination, DateTimeOffset? ScheduledDeparture,
    DateTimeOffset? ScheduledArrival, string Status, decimal? RecordedDelayMinutes,
    string? LastReportedStation, DateTimeOffset LastUpdateAt, string? AssignedDriver)
{
    public static OperationsJourneyResponse From(OperationsJourneyItem item) => new(item.JourneyId,
        item.ServiceId, item.ServiceCode, item.ServiceDisplayName, item.PublicServiceNumber,
        item.RouteCode, item.RouteName, item.OperationDate, item.Origin, item.Destination,
        item.ScheduledDeparture, item.ScheduledArrival, item.Status, item.RecordedDelayMinutes,
        item.LastReportedStation, item.LastUpdateAt, item.AssignedDriver);
}
