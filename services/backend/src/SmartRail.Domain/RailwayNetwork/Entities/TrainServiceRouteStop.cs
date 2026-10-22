namespace SmartRail.Domain.RailwayNetwork.Entities;

/// <summary>A station served by a train service, ordered by its position on the associated route.</summary>
public sealed class TrainServiceRouteStop(Guid serviceId, Guid routeStopId, int stopOrder)
{
    public Guid ServiceId { get; } = serviceId == Guid.Empty ? throw new ArgumentException("Service ID must be a non-empty UUID.", nameof(serviceId)) : serviceId;
    public Guid RouteStopId { get; } = routeStopId == Guid.Empty ? throw new ArgumentException("Route stop ID must be a non-empty UUID.", nameof(routeStopId)) : routeStopId;
    public int StopOrder { get; } = stopOrder < 1 ? throw new ArgumentOutOfRangeException(nameof(stopOrder)) : stopOrder;
}
