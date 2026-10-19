using SmartRail.Domain.Incidents;
using SmartRail.Domain.Incidents.Entities;

namespace SmartRail.UnitTests.Incidents;

internal static class IncidentTestData
{
    public const string TestCategory = "TEST_TRACK_OBSTRUCTION";
    public const string Subject = "unit-test-subject";
    public static readonly DateTimeOffset Now = new(2026, 10, 20, 8, 0, 0, TimeSpan.Zero);
    public static Incident CreateIncident() => new(Guid.NewGuid(), TestCategory, "A reported railway obstruction.", Subject, Now);
    public static IncidentRevision CreateRevision(Guid id, int revision = 1, IncidentState state = IncidentState.Reported,
        IncidentState? previous = null) => new(id, revision, state, Subject, Now, "[]", previous);
}
