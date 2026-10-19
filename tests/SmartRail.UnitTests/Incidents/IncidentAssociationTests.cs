using SmartRail.Domain.Incidents;
using SmartRail.Domain.Incidents.Entities;
using SmartRail.UnitTests.Incidents;

namespace SmartRail.UnitTests.Incidents;

public sealed class IncidentAssociationTests
{
    [Fact]
    public void Requires_disruption_update_identity_and_revision_together()
    {
        var id = Guid.NewGuid();
        Assert.Throws<ArgumentException>(() => new IncidentAssociation(Guid.NewGuid(), id, IncidentAssociationKind.Disruption, Guid.NewGuid(), IncidentTestData.Now, 1));
        Assert.Throws<ArgumentException>(() => new IncidentAssociation(Guid.NewGuid(), id, IncidentAssociationKind.Station, Guid.NewGuid(), IncidentTestData.Now, 1, Guid.NewGuid(), 1));
    }

    [Fact]
    public void Removal_closes_history_once_and_requires_later_revision()
    {
        var link = new IncidentAssociation(Guid.NewGuid(), Guid.NewGuid(), IncidentAssociationKind.Journey, Guid.NewGuid(), IncidentTestData.Now, 1);
        Assert.Throws<ArgumentOutOfRangeException>(() => link.Remove(1, 1, IncidentTestData.Now.AddMinutes(1)));
        link.Remove(1, 2, IncidentTestData.Now.AddMinutes(1));
        Assert.False(link.IsActive);
        Assert.Equal(2, link.RemovedRevision);
        Assert.Throws<InvalidOperationException>(() => link.Remove(2, 3, IncidentTestData.Now.AddMinutes(2)));
    }
}
