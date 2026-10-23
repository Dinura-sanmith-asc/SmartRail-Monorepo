using SmartRail.Domain.Identity;

namespace SmartRail.Application.Identity.Authorization;

/// <summary>Requires an explicit role/action grant and the evidence applicable to that role's grant.</summary>
public sealed class AuthorizeOperationHandler(
    ResolveCurrentRolesHandler roleResolver,
    ResolveResourceScopeHandler scopeResolver)
{
    public async Task<AuthorizationDecision> HandleAsync(AuthorizationRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Principal is null || string.IsNullOrWhiteSpace(request.Principal.CognitoSubject))
            return AuthorizationDecision.Denied("verified-principal-required");

        var roleResult = await roleResolver.HandleAsync(new(request.Principal), cancellationToken);
        if (roleResult.Outcome == RoleResolutionOutcome.Unavailable)
            return AuthorizationDecision.Unavailable("role-mapping-unavailable");

        var candidateRoles = roleResult.Roles.Where(role => RolePermissionCatalog.IsGranted(role, request.Action)).ToArray();
        if (candidateRoles.Length == 0) return AuthorizationDecision.Denied("no-active-role-grants-action");

        var incidentAction = request.Action is PermissionAction.ViewIncidents or PermissionAction.UpdateIncidentLifecycle;
        ResourceScopeResult? incidentEvidence = null;
        if (request.Resource.Kind == ProtectedResourceKind.Incident && incidentAction)
        {
            incidentEvidence = await scopeResolver.HandleAsync(new(request.Principal, request.Resource), cancellationToken);
            if (incidentEvidence.Outcome == AuthorizationOutcome.Unavailable)
                return AuthorizationDecision.Unavailable(incidentEvidence.ReasonCode);
            if (incidentEvidence.Outcome == AuthorizationOutcome.Denied)
                return AuthorizationDecision.Denied(incidentEvidence.ReasonCode);
        }

        var sawUnavailableEvidence = false;
        foreach (var role in candidateRoles)
        {
            ProtectedResource? requiredScope = null;
            if (role == RoleCode.StationStaff && request.Action is PermissionAction.ViewOperationsDashboard or PermissionAction.CreateIncident or PermissionAction.ViewIncidents or PermissionAction.UpdateIncidentLifecycle or PermissionAction.ChangePlatformOrOperationalTiming)
            {
                if (request.Resource.Kind == ProtectedResourceKind.Incident && incidentAction)
                {
                    if (incidentEvidence?.RelatedStationId is not { } incidentStationId || incidentStationId == Guid.Empty)
                        continue;
                    requiredScope = ProtectedResource.Station(incidentStationId);
                }
                else if (request.Action == PermissionAction.CreateIncident && request.Resource.Kind == ProtectedResourceKind.Station)
                    requiredScope = request.Resource;
                else if (request.Action == PermissionAction.ViewOperationsDashboard && request.Resource.Kind == ProtectedResourceKind.Station)
                    requiredScope = request.Resource;
                else if (request.Action is PermissionAction.ViewIncidents or PermissionAction.UpdateIncidentLifecycle && request.Resource.Kind == ProtectedResourceKind.Station)
                    requiredScope = request.Resource;
                else if (request.Action == PermissionAction.ChangePlatformOrOperationalTiming && request.Resource.Kind == ProtectedResourceKind.Station)
                    requiredScope = request.Resource;
                else
                    continue;
            }
            else if (role == RoleCode.Driver && request.Action is PermissionAction.ViewDriverAssignments or PermissionAction.SubmitDriverProgressObservation)
            {
                if (request.Resource.Kind != ProtectedResourceKind.Journey) continue;
                requiredScope = request.Resource;
            }
            else if (role == RoleCode.Passenger && request.Action == PermissionAction.ViewOwnCateringOrder)
            {
                if (request.Resource.Kind != ProtectedResourceKind.CateringOrder) continue;
                requiredScope = request.Resource;
            }

            if (requiredScope is null)
            {
                if (role == RoleCode.StationStaff && request.Action is PermissionAction.ViewOperationsDashboard or PermissionAction.CreateIncident or PermissionAction.ViewIncidents or PermissionAction.UpdateIncidentLifecycle or PermissionAction.ChangePlatformOrOperationalTiming)
                    continue;
                if (role == RoleCode.Driver && request.Action is PermissionAction.ViewDriverAssignments or PermissionAction.SubmitDriverProgressObservation)
                    continue;
                if (role == RoleCode.Passenger && request.Action == PermissionAction.ViewOwnCateringOrder)
                    continue;
                if (incidentAction && request.Resource.Kind is not (ProtectedResourceKind.Incident or ProtectedResourceKind.Station) &&
                    request.Action is PermissionAction.ViewIncidents or PermissionAction.UpdateIncidentLifecycle)
                    continue;
                return AuthorizationDecision.Allowed();
            }

            var scopeResult = await scopeResolver.HandleAsync(new(request.Principal, requiredScope), cancellationToken);
            if (scopeResult.Outcome == AuthorizationOutcome.Allowed) return AuthorizationDecision.Allowed();
            if (scopeResult.Outcome == AuthorizationOutcome.Unavailable) sawUnavailableEvidence = true;
        }

        if (sawUnavailableEvidence) return AuthorizationDecision.Unavailable();
        if (request.Resource.Kind != ProtectedResourceKind.None && request.Resource.ResourceId == Guid.Empty)
            return AuthorizationDecision.Denied("invalid-resource-context");
        return AuthorizationDecision.Denied("required-resource-evidence-not-verified");
    }
}
