namespace SmartRail.Domain.RailwayNetwork.Entities;

/// <summary>Publication completeness evidence for one schedule operation date.</summary>
public sealed class ServiceDateManifest
{
    public DateOnly OperationDate { get; }
    public string SourceRevision { get; }
    public string FixtureVersion { get; }
    public bool IsComplete { get; }
    public DateTimeOffset? PublishedAt { get; }
    public string? PublishedBy { get; }
    public string ContentHash { get; }

    public ServiceDateManifest(
        DateOnly operationDate,
        string sourceRevision,
        string fixtureVersion,
        bool isComplete,
        DateTimeOffset? publishedAt,
        string? publishedBy,
        string contentHash)
    {
        OperationDate = operationDate;
        SourceRevision = Required(sourceRevision, nameof(sourceRevision));
        FixtureVersion = Required(fixtureVersion, nameof(fixtureVersion));
        IsComplete = isComplete;
        if (publishedAt.HasValue != !string.IsNullOrWhiteSpace(publishedBy))
            throw new ArgumentException("Publication instant and publisher must either both be present or both be absent.");
        if (isComplete && (!publishedAt.HasValue || string.IsNullOrWhiteSpace(contentHash)))
            throw new ArgumentException("Complete date evidence requires publication evidence and a content hash.");
        PublishedAt = publishedAt;
        PublishedBy = publishedBy?.Trim();
        ContentHash = Required(contentHash, nameof(contentHash));
    }

    private static string Required(string value, string parameter) =>
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Value is required.", parameter) : value.Trim();
}
