using SmartRail.Domain.RailwayNetwork.Entities;

namespace SmartRail.Application.RailwayNetwork.Stations;

public sealed record StationImportRecord(
    Guid StationId,
    string SourceStationKey,
    string Name,
    bool IsPublished,
    IReadOnlyDictionary<string, string> VerifiedDetails);

public sealed record StationImportValidationResult(IReadOnlyList<string> Errors)
{
    public bool IsValid => Errors.Count == 0;
}

/// <summary>Validates a whole station import file before persistence is attempted.</summary>
public static class ImportStationsValidator
{
    public static StationImportValidationResult Validate(IReadOnlyCollection<StationImportRecord> records)
    {
        ArgumentNullException.ThrowIfNull(records);
        var errors = new List<string>();
        var sourceKeys = new HashSet<string>(StringComparer.Ordinal);
        var stationIds = new HashSet<Guid>();

        foreach (var (record, index) in records.Select((record, index) => (record, index)))
        {
            if (record is null)
            {
                errors.Add($"Station record at index {index} is required.");
                continue;
            }
            if (record.VerifiedDetails is null)
            {
                errors.Add($"Station record at index {index} requires a verified-details collection.");
                continue;
            }
            if (record.StationId == Guid.Empty) errors.Add($"Station record at index {index} requires a stable station ID.");
            else if (!stationIds.Add(record.StationId)) errors.Add($"Station ID '{record.StationId}' is duplicated.");
            if (string.IsNullOrWhiteSpace(record.SourceStationKey)) errors.Add($"Station record at index {index} requires a source identity.");
            else if (!sourceKeys.Add(record.SourceStationKey.Trim())) errors.Add($"Source station identity '{record.SourceStationKey.Trim()}' is duplicated.");
            if (string.IsNullOrWhiteSpace(record.Name)) errors.Add($"Station record at index {index} requires a name.");
            foreach (var (key, value) in record.VerifiedDetails)
            {
                if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
                    errors.Add($"Station record at index {index} contains an invalid verified detail.");
            }
        }

        return new StationImportValidationResult(errors.AsReadOnly());
    }
}