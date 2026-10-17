namespace SmartRail.Domain.RailwayNetwork.Entities;

/// <summary>Minimal audit evidence for one controlled station reference-data import.</summary>
public sealed class StationImportRun
{
    public Guid ImportRunId { get; }
    public string SourceRevision { get; }
    public string ContentHash { get; }
    public string SubmittedBy { get; }
    public string ApprovedBy { get; }
    public DateTimeOffset StartedAt { get; }
    public DateTimeOffset CompletedAt { get; }
    public int CreatedCount { get; }
    public int UpdatedCount { get; }
    public int UnchangedCount { get; }

    public StationImportRun(
        Guid importRunId,
        string sourceRevision,
        string contentHash,
        string submittedBy,
        string approvedBy,
        DateTimeOffset startedAt,
        DateTimeOffset completedAt,
        int createdCount,
        int updatedCount,
        int unchangedCount)
    {
        if (importRunId == Guid.Empty) throw new ArgumentException("Import run ID must be a non-empty UUID.", nameof(importRunId));
        if (completedAt < startedAt) throw new ArgumentException("Import completion cannot precede its start.", nameof(completedAt));
        if (createdCount < 0) throw new ArgumentOutOfRangeException(nameof(createdCount), "Import counts cannot be negative.");
        if (updatedCount < 0) throw new ArgumentOutOfRangeException(nameof(updatedCount), "Import counts cannot be negative.");
        if (unchangedCount < 0) throw new ArgumentOutOfRangeException(nameof(unchangedCount), "Import counts cannot be negative.");

        ImportRunId = importRunId;
        SourceRevision = Required(sourceRevision, nameof(sourceRevision));
        ContentHash = Required(contentHash, nameof(contentHash));
        SubmittedBy = Required(submittedBy, nameof(submittedBy));
        ApprovedBy = Required(approvedBy, nameof(approvedBy));
        StartedAt = startedAt;
        CompletedAt = completedAt;
        CreatedCount = createdCount;
        UpdatedCount = updatedCount;
        UnchangedCount = unchangedCount;
    }

    private static string Required(string value, string parameterName) =>
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Value is required.", parameterName) : value.Trim();
}