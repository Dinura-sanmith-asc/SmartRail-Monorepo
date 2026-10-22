namespace SmartRail.Domain.RailwayNetwork.Entities;

public sealed class RouteStop
{
    public Guid RouteStopId { get; }
    public Guid RouteId { get; }
    public Guid StationId { get; }
    public int StopOrder { get; private set; }

    public RouteStop(Guid routeStopId, Guid routeId, Guid stationId, int stopOrder)
    {
        if (routeStopId == Guid.Empty) throw new ArgumentException("Route stop ID must be a non-empty UUID.", nameof(routeStopId));
        if (routeId == Guid.Empty) throw new ArgumentException("Route ID must be a non-empty UUID.", nameof(routeId));
        if (stationId == Guid.Empty) throw new ArgumentException("Station ID must be a non-empty UUID.", nameof(stationId));
        if (stopOrder <= 0) throw new ArgumentOutOfRangeException(nameof(stopOrder), "Stop order must be positive.");
        RouteStopId = routeStopId;
        RouteId = routeId;
        StationId = stationId;
        StopOrder = stopOrder;
    }

    public void Reorder(int stopOrder)
    {
        if (stopOrder <= 0) throw new ArgumentOutOfRangeException(nameof(stopOrder), "Stop order must be positive.");
        StopOrder = stopOrder;
    }
}
