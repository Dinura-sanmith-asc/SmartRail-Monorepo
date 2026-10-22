namespace SmartRail.Application.JourneysStatus.Boundaries;

public interface IFeature002JourneyAuthorization
{
    Task<bool> IsCurrentControllerAuthorizedAsync(string verifiedSubject, Guid journeyInstanceId, string action, CancellationToken cancellationToken);
}
