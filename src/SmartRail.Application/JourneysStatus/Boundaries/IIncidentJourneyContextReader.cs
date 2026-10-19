namespace SmartRail.Application.JourneysStatus.Boundaries;

public interface IIncidentJourneyContextReader
{
    Task<IncidentJourneyContextResult> GetAuthorizedContextAsync(string verifiedSubject, Guid journeyInstanceId, CancellationToken cancellationToken);
}
