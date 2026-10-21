namespace SmartRail.Domain.RailwayNetwork.Entities;

public sealed class TrainService
{
    public Guid ServiceId { get; }
    public string ServiceCode { get; private set; }
    /// <summary>Operator-facing published service name; null when the timetable has no verified name.</summary>
    public string? DisplayName { get; private set; }
    /// <summary>Published public train/service number, distinct from both ServiceCode and ServiceId.</summary>
    public string? PublicServiceNumber { get; private set; }
    public Guid RouteId { get; private set; }
    public bool IsPublished { get; private set; }
    public int Version { get; private set; }

    public TrainService(Guid serviceId, string serviceCode, Guid routeId, bool isPublished = false, int version = 1,
        string? displayName = null, string? publicServiceNumber = null)
    {
        if (serviceId == Guid.Empty) throw new ArgumentException("Service ID must be a non-empty UUID.", nameof(serviceId));
        if (routeId == Guid.Empty) throw new ArgumentException("Route ID must be a non-empty UUID.", nameof(routeId));
        if (version < 1) throw new ArgumentOutOfRangeException(nameof(version));
        ServiceId = serviceId;
        ServiceCode = Required(serviceCode, nameof(serviceCode));
        DisplayName = Optional(displayName, nameof(displayName), 200);
        PublicServiceNumber = Optional(publicServiceNumber, nameof(publicServiceNumber), 50);
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

    public void SetPublicIdentity(string? displayName, string? publicServiceNumber)
    {
        DisplayName = Optional(displayName, nameof(displayName), 200);
        PublicServiceNumber = Optional(publicServiceNumber, nameof(publicServiceNumber), 50);
        Version++;
    }

    public void UpdateDetails(string serviceCode, Guid routeId, string? displayName, string? publicServiceNumber)
    {
        if (routeId == Guid.Empty) throw new ArgumentException("Route ID must be a non-empty UUID.", nameof(routeId));
        ServiceCode = Required(serviceCode, nameof(serviceCode));
        DisplayName = Optional(displayName, nameof(displayName), 200);
        PublicServiceNumber = Optional(publicServiceNumber, nameof(publicServiceNumber), 50);
        RouteId = routeId;
        Version++;
    }

    public void Publish() => IsPublished = true;
    public void Unpublish() => IsPublished = false;

    private static string Required(string value, string parameter) =>
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Value is required.", parameter) : value.Trim();

    private static string? Optional(string? value, string parameter, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        if (normalized.Length > maximumLength)
            throw new ArgumentException($"Value must be at most {maximumLength} characters.", parameter);
        return normalized;
    }
}
