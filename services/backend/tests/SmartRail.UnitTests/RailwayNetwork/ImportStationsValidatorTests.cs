using SmartRail.Application.RailwayNetwork.Stations;

namespace SmartRail.UnitTests.RailwayNetwork;

public sealed class ImportStationsValidatorTests
{
    [Fact]
    public void Validate_accepts_unique_records_with_source_identity_and_name()
    {
        var result = ImportStationsValidator.Validate([
            new StationImportRecord(Guid.NewGuid(), "SRC-A", "Station A", true, new Dictionary<string, string>()),
            new StationImportRecord(Guid.NewGuid(), "SRC-B", "Station B", false, new Dictionary<string, string>())
        ]);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_rejects_duplicate_source_keys_and_station_ids_before_persistence()
    {
        var stationId = Guid.NewGuid();
        var result = ImportStationsValidator.Validate([
            new StationImportRecord(stationId, "SRC-A", "Station A", true, new Dictionary<string, string>()),
            new StationImportRecord(stationId, " SRC-A ", "Station B", true, new Dictionary<string, string>())
        ]);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("duplicated", StringComparison.Ordinal));
        Assert.Equal(2, result.Errors.Count);
    }

    [Fact]
    public void Validate_rejects_missing_source_identity_name_and_blank_detail_values()
    {
        var result = ImportStationsValidator.Validate([
            new StationImportRecord(Guid.Empty, " ", " ", false, new Dictionary<string, string> { ["district"] = " " })
        ]);

        Assert.False(result.IsValid);
        Assert.Equal(4, result.Errors.Count);
    }
}