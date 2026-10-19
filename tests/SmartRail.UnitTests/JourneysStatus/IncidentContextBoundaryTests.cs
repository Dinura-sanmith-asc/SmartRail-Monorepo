using SmartRail.Application.JourneysStatus.Boundaries;
using SmartRail.Application.JourneysStatus.Projections;

namespace SmartRail.UnitTests.JourneysStatus;

public sealed class IncidentContextBoundaryTests
{
    [Fact]
    public async Task Incident_owner_port_distinguishes_verified_no_context_from_unavailable()
    {
        var reader = new RecordingIncidentReader();
        var subject = "verified-operations-subject";
        var journey = Guid.NewGuid();

        var noContext = await reader.GetAuthorizedContextAsync(subject, journey, default);
        reader.Result = new(IncidentJourneyContextStatus.Unavailable, []);
        var unavailable = await reader.GetAuthorizedContextAsync(subject, journey, default);

        Assert.Equal(IncidentJourneyContextStatus.NoContext, noContext.Status);
        Assert.Equal(IncidentJourneyContextStatus.Unavailable, unavailable.Status);
        Assert.Equal(subject, reader.Subject);
        Assert.Equal(journey, reader.JourneyId);
    }

    [Fact]
    public void Public_journey_projection_has_no_incident_fields_or_mutation_surface()
    {
        Assert.DoesNotContain(typeof(JourneySnapshotView).GetProperties(),
            property => property.Name.Contains("Incident", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(typeof(JourneyUpdateView).GetProperties(),
            property => property.Name.Contains("Incident", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(typeof(JourneySnapshotView).GetProperties(),
            property => property.Name.Contains("Subject", StringComparison.OrdinalIgnoreCase)
                || property.Name.Contains("Observation", StringComparison.OrdinalIgnoreCase)
                || property.Name.Contains("Evidence", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(typeof(JourneyUpdateView).GetProperties(),
            property => property.Name.Contains("Subject", StringComparison.OrdinalIgnoreCase)
                || property.Name.Contains("Observation", StringComparison.OrdinalIgnoreCase)
                || property.Name.Contains("Evidence", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(typeof(JourneySnapshotView).GetMethods(), method =>
            method.Name.StartsWith("Set", StringComparison.Ordinal) || method.Name.StartsWith("Update", StringComparison.Ordinal));
    }

    private sealed class RecordingIncidentReader : IIncidentJourneyContextReader
    {
        public string? Subject { get; private set; }
        public Guid JourneyId { get; private set; }
        public IncidentJourneyContextResult Result { get; set; } = new(IncidentJourneyContextStatus.NoContext, []);

        public Task<IncidentJourneyContextResult> GetAuthorizedContextAsync(string verifiedSubject,
            Guid journeyInstanceId, CancellationToken cancellationToken)
        {
            Subject = verifiedSubject;
            JourneyId = journeyInstanceId;
            return Task.FromResult(Result);
        }
    }
}
