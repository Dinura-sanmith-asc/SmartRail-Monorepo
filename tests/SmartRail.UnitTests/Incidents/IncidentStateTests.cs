using SmartRail.Domain.Incidents;
using SmartRail.UnitTests.Incidents;

namespace SmartRail.UnitTests.Incidents;

public sealed class IncidentStateTests
{
    [Theory]
    [InlineData(IncidentState.Reported, IncidentState.InProgress)]
    [InlineData(IncidentState.InProgress, IncidentState.Resolved)]
    [InlineData(IncidentState.Resolved, IncidentState.Closed)]
    public void Allows_only_approved_forward_transitions(IncidentState current, IncidentState next) => Assert.True(IncidentLifecycle.CanTransition(current, next));

    [Theory]
    [InlineData(IncidentState.Reported, IncidentState.Resolved)]
    [InlineData(IncidentState.InProgress, IncidentState.Reported)]
    [InlineData(IncidentState.Resolved, IncidentState.InProgress)]
    [InlineData(IncidentState.Closed, IncidentState.Reported)]
    [InlineData(IncidentState.Closed, IncidentState.InProgress)]
    public void Rejects_reverse_skip_and_reopen_transitions(IncidentState current, IncidentState next) => Assert.False(IncidentLifecycle.CanTransition(current, next));

    [Fact]
    public void Transition_requires_current_revision_and_increments_once()
    {
        var incident = IncidentTestData.CreateIncident();
        Assert.Throws<InvalidOperationException>(() => incident.TransitionTo(IncidentState.InProgress, 2, IncidentTestData.Now.AddMinutes(1)));
        Assert.Equal(2, incident.TransitionTo(IncidentState.InProgress, 1, IncidentTestData.Now.AddMinutes(1)));
        Assert.Throws<InvalidOperationException>(() => incident.TransitionTo(IncidentState.Resolved, 1, IncidentTestData.Now.AddMinutes(2)));
    }
}
