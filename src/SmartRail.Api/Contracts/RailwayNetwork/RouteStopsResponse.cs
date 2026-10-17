namespace SmartRail.Api.Contracts.RailwayNetwork;

public sealed record RouteStopsResponse(Guid RouteId, string RouteCode, string Name, IReadOnlyList<RouteStopResponse> Stops);
public sealed record RouteStopResponse(int StopOrder, Guid StationId, string StationName);
