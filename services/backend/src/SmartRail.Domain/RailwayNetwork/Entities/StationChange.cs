namespace SmartRail.Domain.RailwayNetwork.Entities;

public enum StationChangeAction
{
    Created = 1,
    Updated = 2
}

/// <summary>Minimal field-level summary of a station change made by a controlled import.</summary>
public sealed class StationChange
{
    private readonly string[] _changedFieldNames;

    public Guid StationChangeId { get; }
    public Guid ImportRunId { get; }
    public Guid StationId { get; }
    public StationChangeAction Action { get; }
    public IReadOnlyList<string> ChangedFieldNames => Array.AsReadOnly(_changedFieldNames);
    public DateTimeOffset ChangedAt { get; }
    public string ChangedBy { get; }

    public StationChange(
        Guid stationChangeId,
        Guid importRunId,
        Guid stationId,
        StationChangeAction action,
        IReadOnlyList<string> changedFieldNames,
        DateTimeOffset changedAt,
        string changedBy)
    {
        if (stationChangeId == Guid.Empty) throw new ArgumentException("Station change ID must be a non-empty UUID.", nameof(stationChangeId));
        if (importRunId == Guid.Empty) throw new ArgumentException("Import run ID must be a non-empty UUID.", nameof(importRunId));
        if (stationId == Guid.Empty) throw new ArgumentException("Station ID must be a non-empty UUID.", nameof(stationId));
        if (!Enum.IsDefined(action)) throw new ArgumentOutOfRangeException(nameof(action), "Only created and updated changes are recorded.");
        ArgumentNullException.ThrowIfNull(changedFieldNames);
        var fields = changedFieldNames.Select((field, index) => Required(field, $"{nameof(changedFieldNames)}[{index}]")).ToArray();
        if (fields.Length == 0) throw new ArgumentException("At least one changed field must be recorded.", nameof(changedFieldNames));
        if (fields.Distinct(StringComparer.Ordinal).Count() != fields.Length)
            throw new ArgumentException("Changed field names must be unique.", nameof(changedFieldNames));

        StationChangeId = stationChangeId;
        ImportRunId = importRunId;
        StationId = stationId;
        Action = action;
        _changedFieldNames = fields;
        ChangedAt = changedAt;
        ChangedBy = Required(changedBy, nameof(changedBy));
    }

    private static string Required(string value, string parameterName) =>
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Value is required.", parameterName) : value.Trim();
}