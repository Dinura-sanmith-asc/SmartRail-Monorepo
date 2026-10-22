namespace SmartRail.Domain.RailwayNetwork.Entities;

public sealed class Route
{
    public Guid RouteId { get; }
    public string RouteCode { get; private set; }
    public string Name { get; private set; }
    public bool IsPublished { get; private set; }
    public int Version { get; private set; }

    public Route(Guid routeId, string routeCode, string name, bool isPublished = false, int version = 1)
    {
        if (routeId == Guid.Empty) throw new ArgumentException("Route ID must be a non-empty UUID.", nameof(routeId));
        if (version < 1) throw new ArgumentOutOfRangeException(nameof(version));
        RouteId = routeId;
        RouteCode = Required(routeCode, nameof(routeCode));
        Name = Required(name, nameof(name));
        IsPublished = isPublished;
        Version = version;
    }

    public void Rename(string routeCode, string name)
    {
        RouteCode = Required(routeCode, nameof(routeCode));
        Name = Required(name, nameof(name));
        Version++;
    }

    public void Publish() => IsPublished = true;
    public void Unpublish() => IsPublished = false;

    private static string Required(string value, string parameter) =>
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Value is required.", parameter) : value.Trim();
}
