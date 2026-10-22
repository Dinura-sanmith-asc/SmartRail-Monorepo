using SmartRail.Application.Incidents.References;
using SmartRail.Application.Incidents.UseCases;

namespace SmartRail.UnitTests.Incidents;

public sealed class CreateIncidentValidatorTests
{
    [Fact]
    public void Accepts_required_fields_and_station_anchor()
    {
        var result = CreateIncidentValidator.Validate(new(
            IncidentTestData.TestCategory,
            "Track obstruction reported",
            null,
            [new(IncidentReferenceKind.Station, Guid.NewGuid())]));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Rejects_missing_fields_and_missing_station_or_service_anchor()
    {
        var result = CreateIncidentValidator.Validate(new(null, " ", null,
            [new(IncidentReferenceKind.Journey, Guid.NewGuid())]));

        Assert.False(result.IsValid);
        Assert.Contains("categoryCode", result.Errors.Keys);
        Assert.Contains("description", result.Errors.Keys);
        Assert.Contains("associations", result.Errors.Keys);
    }

    [Fact]
    public void Rejects_empty_ids_duplicates_and_invalid_disruption_evidence()
    {
        var station = new IncidentReference(IncidentReferenceKind.Station, Guid.NewGuid());
        var result = CreateIncidentValidator.Validate(new(
            IncidentTestData.TestCategory,
            "Track obstruction reported",
            null,
            [station, station, new(IncidentReferenceKind.Disruption, Guid.NewGuid())]));

        Assert.False(result.IsValid);
        Assert.Contains("associations", result.Errors.Keys);
    }
}
