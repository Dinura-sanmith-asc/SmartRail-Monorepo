using SmartRail.Domain.Incidents;
using SmartRail.UnitTests.Incidents;

namespace SmartRail.UnitTests.Incidents;

public sealed class IncidentAggregateTests
{
    [Fact]
    public void Requires_report_fields_and_verified_actor()
    {
        Assert.Throws<ArgumentException>(() => new SmartRail.Domain.Incidents.Entities.Incident(Guid.NewGuid(), " ", "valid", "actor", IncidentTestData.Now));
        Assert.Throws<ArgumentException>(() => new SmartRail.Domain.Incidents.Entities.Incident(Guid.NewGuid(), IncidentTestData.TestCategory, " ", "actor", IncidentTestData.Now));
        Assert.Throws<ArgumentException>(() => new SmartRail.Domain.Incidents.Entities.Incident(Guid.NewGuid(), IncidentTestData.TestCategory, "valid", " ", IncidentTestData.Now));
    }

    [Fact]
    public void Starts_reported_at_revision_one_and_keeps_occurrence_time_separate()
    {
        var occurred = IncidentTestData.Now.AddHours(-2);
        var incident = new SmartRail.Domain.Incidents.Entities.Incident(Guid.NewGuid(), IncidentTestData.TestCategory, "Report", "subject", IncidentTestData.Now, occurred);
        Assert.Equal(IncidentState.Reported, incident.State);
        Assert.Equal(1, incident.Revision);
        Assert.Equal(IncidentTestData.Now, incident.ReportedAt);
        Assert.Equal(occurred, incident.OccurredAt);
        Assert.NotEqual(incident.ReportedAt, incident.OccurredAt);
    }

    [Fact]
    public void Rejects_non_utc_or_default_report_timestamp()
    {
        Assert.Throws<ArgumentException>(() => new SmartRail.Domain.Incidents.Entities.Incident(Guid.NewGuid(), IncidentTestData.TestCategory, "Report", "subject", default));
        Assert.Throws<ArgumentException>(() => new SmartRail.Domain.Incidents.Entities.Incident(Guid.NewGuid(), IncidentTestData.TestCategory, "Report", "subject", IncidentTestData.Now.ToOffset(TimeSpan.FromHours(5))));
    }
}
