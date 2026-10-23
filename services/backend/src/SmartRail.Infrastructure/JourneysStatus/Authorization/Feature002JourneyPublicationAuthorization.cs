using SmartRail.Application.Identity.Authorization;
using SmartRail.Application.JourneysStatus.Publication;
using SmartRail.Domain.Identity;
using SmartRail.Domain.JourneysStatus;

namespace SmartRail.Infrastructure.JourneysStatus.Authorization;

public sealed class Feature002JourneyPublicationAuthorization(AuthorizeOperationHandler authorization) : IJourneyPublicationAuthorization
{
    public async Task<PublicationAuthorizationDecision> AuthorizeAsync(string verifiedSubject, Guid journeyInstanceId,
        PermissionAction action, CancellationToken cancellationToken = default)
    {
        return await AuthorizeResourceAsync(verifiedSubject, journeyInstanceId, action,
            ProtectedResource.Journey(journeyInstanceId), cancellationToken);
    }

    public async Task<PublicationAuthorizationDecision> AuthorizeStopAsync(string verifiedSubject, Guid journeyInstanceId,
        Guid stationId, CancellationToken cancellationToken = default)
    {
        var controller = await AuthorizeResourceAsync(verifiedSubject, journeyInstanceId,
            PermissionAction.ChangePlatformOrOperationalTiming, ProtectedResource.Journey(journeyInstanceId), cancellationToken);
        if (controller.Outcome == PublicationAuthorizationOutcome.Allowed)
            return controller with { Source = JourneySource.ControllerManual };
        if (controller.Outcome == PublicationAuthorizationOutcome.Unavailable) return controller;

        var staff = await AuthorizeResourceAsync(verifiedSubject, journeyInstanceId,
            PermissionAction.ChangePlatformOrOperationalTiming, ProtectedResource.Station(stationId), cancellationToken);
        return staff.Outcome == PublicationAuthorizationOutcome.Allowed
            ? staff with { Source = JourneySource.StationStaffManual } : staff;
    }

    private async Task<PublicationAuthorizationDecision> AuthorizeResourceAsync(string verifiedSubject, Guid journeyInstanceId,
        PermissionAction action, ProtectedResource resource, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(verifiedSubject) || journeyInstanceId == Guid.Empty)
            return new(PublicationAuthorizationOutcome.Denied);
        var decision = await authorization.HandleAsync(new(new VerifiedPrincipal(verifiedSubject), action,
            resource), cancellationToken);
        return decision.Outcome switch
        {
            AuthorizationOutcome.Allowed => new(PublicationAuthorizationOutcome.Allowed),
            AuthorizationOutcome.Unavailable => new(PublicationAuthorizationOutcome.Unavailable),
            _ => new(PublicationAuthorizationOutcome.Denied)
        };
    }
}
