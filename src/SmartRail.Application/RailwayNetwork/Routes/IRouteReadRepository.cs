namespace SmartRail.Application.RailwayNetwork.Routes;

public sealed record RouteStopView(int StopOrder, Guid StationId, string StationName);
public sealed record RouteStopsView(Guid RouteId, string RouteCode, string Name, IReadOnlyList<RouteStopView> Stops);

public interface IRouteReadRepository
{
    Task<RouteStopsView?> GetPublishedStopsAsync(Guid routeId, CancellationToken cancellationToken = default);
}
