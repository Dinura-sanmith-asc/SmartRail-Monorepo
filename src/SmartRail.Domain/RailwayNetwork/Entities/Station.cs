namespace SmartRail.Domain.RailwayNetwork.Entities;

/// <summary>A stable railway reference station. Source identity is intentionally internal.</summary>
public sealed class Station
{
    private readonly Dictionary<string, string> _verifiedDetails;

    public Guid StationId { get; }
    public string Name { get; private set; }
    public bool IsPublished { get; private set; }
    public IReadOnlyDictionary<string, string> VerifiedDetails => _verifiedDetails;
    public int Version { get; private set; }

    internal string SourceStationKey { get; private set; }

    public Station(
        Guid stationId,
        string sourceStationKey,
        string name,
        bool isPublished = false,
        IReadOnlyDictionary<string, string>? verifiedDetails = null,
        int version = 1)
    {
        if (stationId == Guid.Empty) throw new ArgumentException("Station ID must be a non-empty UUID.", nameof(stationId));
        if (string.IsNullOrWhiteSpace(sourceStationKey)) throw new ArgumentException("Source station key is required.", nameof(sourceStationKey));
        if (version < 1) throw new ArgumentOutOfRangeException(nameof(version), "Version must be positive.");

        StationId = stationId;
        SourceStationKey = sourceStationKey.Trim();
        Name = RequireText(name, nameof(name));
        IsPublished = isPublished;
        Version = version;
        _verifiedDetails = CopyAndValidateDetails(verifiedDetails);
    }

    public void UpdateVerifiedDetails(string name, IReadOnlyDictionary<string, string> verifiedDetails)
    {
        Name = RequireText(name, nameof(name));
        ArgumentNullException.ThrowIfNull(verifiedDetails);
        _verifiedDetails.Clear();
        foreach (var item in CopyAndValidateDetails(verifiedDetails)) _verifiedDetails.Add(item.Key, item.Value);
        Version++;
    }

    public void Publish() => IsPublished = true;
    public void Unpublish() => IsPublished = false;

    private static string RequireText(string value, string parameterName) =>
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Value is required.", parameterName) : value.Trim();

    private static Dictionary<string, string> CopyAndValidateDetails(IReadOnlyDictionary<string, string>? details)
    {
        var copy = new Dictionary<string, string>(StringComparer.Ordinal);
        if (details is null) return copy;
        foreach (var (key, value) in details)
        {
            var normalizedKey = RequireText(key, nameof(details));
            var normalizedValue = RequireText(value, nameof(details));
            if (!copy.TryAdd(normalizedKey, normalizedValue)) throw new ArgumentException("Detail keys must be unique.", nameof(details));
        }
        return copy;
    }
}
