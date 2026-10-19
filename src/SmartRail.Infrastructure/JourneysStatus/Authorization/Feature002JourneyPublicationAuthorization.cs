using SmartRail.Application.Identity.Authorization;
using SmartRail.Application.JourneysStatus.Publication;
using SmartRail.Domain.Identity;

namespace SmartRail.Infrastructure.JourneysStatus.Authorization;

public sealed class Feature002JourneyPublicationAuthorization(AuthorizeOperationHandler authorization) : IJourneyPublicationAuthorization
{
    public async Task<PublicationAuthorizationDecision> AuthorizeAsync(string verifiedSubject, Guid journeyInstanceId,
        PermissionAction action, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(verifiedSubject) || journeyInstanceId == Guid.Empty)
            return new(PublicationAuthorizationOutcome.Denied);
        var decision = await authorization.HandleAsync(new(new VerifiedPrincipal(verifiedSubject), action,
            ProtectedResource.Journey(journeyInstanceId)), cancellationToken);
        return decision.Outcome switch
        {
            AuthorizationOutcome.Allowed => new(PublicationAuthorizationOutcome.Allowed),
            AuthorizationOutcome.Unavailable => new(PublicationAuthorizationOutcome.Unavailable),
            _ => new(PublicationAuthorizationOutcome.Denied)
        };
    }
}
