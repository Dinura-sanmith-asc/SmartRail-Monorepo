namespace SmartRail.Api.Contracts.RailwayNetwork;

public sealed record ServiceDetailResponse(
    Guid ServiceId,
    string ServiceCode,
    RouteReferenceResponse Route,
    ScheduleReferenceResponse Schedule);

public sealed record RouteReferenceResponse(Guid RouteId, string RouteCode, string Name);
public sealed record ScheduleReferenceResponse(int Revision, TimeOnly FirstDepartureTimeLocal);
