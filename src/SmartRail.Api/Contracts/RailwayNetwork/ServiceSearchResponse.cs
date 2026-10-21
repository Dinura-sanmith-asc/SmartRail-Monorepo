namespace SmartRail.Api.Contracts.RailwayNetwork;

public sealed record ServiceSearchResponse(
    Guid OriginStationId,
    Guid DestinationStationId,
    DateOnly TravelDate,
    IReadOnlyList<ServiceChoiceResponse> Services);

public sealed record ServiceChoiceResponse(
    Guid ServiceId,
    string ServiceCode,
    Guid RouteId,
    string RouteCode,
    int ScheduleRevision,
    ScheduledCallResponse Origin,
    ScheduledCallResponse Destination,
    string? DisplayName = null,
    string? PublicServiceNumber = null);

public sealed record ScheduledCallResponse(Guid StationId, string StationName, int StopOrder, DateTimeOffset ScheduledAt);
