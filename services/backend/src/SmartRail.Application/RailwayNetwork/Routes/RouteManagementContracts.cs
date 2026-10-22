namespace SmartRail.Application.RailwayNetwork.Routes;

public sealed record RouteManagementStopView(Guid RouteStopId, Guid StationId, string StationName, int StopOrder);
public sealed record RouteManagementView(Guid RouteId, string RouteCode, string Name, bool IsPublished,
    IReadOnlyList<RouteManagementStopView> Stops);
public sealed record SaveRouteRequest(string RouteCode, string Name, IReadOnlyList<Guid> StationIds);

public interface IRouteManagementRepository
{
    Task<IReadOnlyList<RouteManagementView>> ListAsync(CancellationToken cancellationToken = default);
    Task<RouteManagementView?> CreateAsync(SaveRouteRequest request, CancellationToken cancellationToken = default);
    Task<RouteManagementView?> UpdateAsync(Guid routeId, SaveRouteRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid routeId, CancellationToken cancellationToken = default);
}
