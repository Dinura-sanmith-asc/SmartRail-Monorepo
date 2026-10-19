using SmartRail.Application.JourneysStatus.Boundaries;

namespace SmartRail.Infrastructure.JourneysStatus.Boundaries;

/// <summary>Fails closed until Incident Management provides its authorized context reader.</summary>
public sealed class UnavailableIncidentJourneyContextReader : IIncidentJourneyContextReader
{
    public Task<IncidentJourneyContextResult> GetAuthorizedContextAsync(
        string verifiedSubject, Guid journeyInstanceId, CancellationToken cancellationToken) =>
        Task.FromResult(new IncidentJourneyContextResult(IncidentJourneyContextStatus.Unavailable, []));
}
