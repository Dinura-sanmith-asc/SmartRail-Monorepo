using SmartRail.Domain.Incidents;
using SmartRail.Domain.Incidents.Entities;

namespace SmartRail.IntegrationTests.Incidents;

/// <summary>Explicit category values used only by isolated integration tests.</summary>
public static class IncidentTestCategories
{
    public const string TrackObstruction = "TEST_TRACK_OBSTRUCTION";
    public const string StationFacilities = "TEST_STATION_FACILITIES";
    public static IReadOnlySet<string> All { get; } = new HashSet<string>(StringComparer.Ordinal)
        { TrackObstruction, StationFacilities };
}

internal static class IncidentTestData
{
    public const string Subject = "integration-test-actor";
    public static readonly DateTimeOffset Now = new(2026, 10, 20, 8, 0, 0, TimeSpan.Zero);
    public static Incident CreateIncident(string? category = null) =>
        new(Guid.NewGuid(), category ?? IncidentTestCategories.TrackObstruction, "A test incident description.", Subject, Now);
    public static IncidentRevision CreateRevision(Incident incident, string? actor = null, string associationEvidence = "[]") =>
        new(incident.IncidentId, incident.Revision, incident.State, actor ?? Subject, incident.UpdatedAt, associationEvidence);
}
