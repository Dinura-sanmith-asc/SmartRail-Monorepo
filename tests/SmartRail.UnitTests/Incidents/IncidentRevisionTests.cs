using SmartRail.Domain.Incidents;
using SmartRail.Domain.Incidents.Entities;
using SmartRail.UnitTests.Incidents;

namespace SmartRail.UnitTests.Incidents;

public sealed class IncidentRevisionTests
{
    [Fact]
    public void Records_transition_and_rejects_invalid_transition_evidence()
    {
        var id = Guid.NewGuid();
        var evidence = IncidentTestData.CreateRevision(id, 2, IncidentState.InProgress, IncidentState.Reported);
        Assert.Equal(IncidentState.Reported, evidence.PreviousState);
        Assert.Equal(IncidentState.InProgress, evidence.State);
        Assert.Throws<ArgumentException>(() => IncidentTestData.CreateRevision(id, 2, IncidentState.Closed, IncidentState.Reported));
        Assert.Throws<ArgumentException>(() => new IncidentRevision(id, 1, IncidentState.Reported, " ", IncidentTestData.Now, "[]"));
    }
}
