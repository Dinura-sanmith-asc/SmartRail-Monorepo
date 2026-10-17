namespace SmartRail.Domain.RailwayNetwork.Entities;

public sealed class TrainService
{
    public Guid ServiceId { get; }
    public string ServiceCode { get; private set; }
    public Guid RouteId { get; private set; }
    public bool IsPublished { get; private set; }
    public int Version { get; private set; }

    public TrainService(Guid serviceId, string serviceCode, Guid routeId, bool isPublished = false, int version = 1)
    {
        if (serviceId == Guid.Empty) throw new ArgumentException("Service ID must be a non-empty UUID.", nameof(serviceId));
        if (routeId == Guid.Empty) throw new ArgumentException("Route ID must be a non-empty UUID.", nameof(routeId));
        if (version < 1) throw new ArgumentOutOfRangeException(nameof(version));
        ServiceId = serviceId;
        ServiceCode = Required(serviceCode, nameof(serviceCode));
        RouteId = routeId;
        IsPublished = isPublished;
        Version = version;
    }

    public void ChangeRoute(Guid routeId)
    {
        if (routeId == Guid.Empty) throw new ArgumentException("Route ID must be a non-empty UUID.", nameof(routeId));
        RouteId = routeId;
        Version++;
    }

    public void Rename(string serviceCode)
    {
        ServiceCode = Required(serviceCode, nameof(serviceCode));
        Version++;
    }

    public void Publish() => IsPublished = true;
    public void Unpublish() => IsPublished = false;

    private static string Required(string value, string parameter) =>
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Value is required.", parameter) : value.Trim();
}
