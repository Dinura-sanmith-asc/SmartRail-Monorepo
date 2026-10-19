using SmartRail.Application.Identity.Authorization;
using SmartRail.Domain.Identity;

namespace SmartRail.Application.Incidents.Authorization;

/// <summary>Applies current Feature 002 role grants and station scope to incident resources.</summary>
public sealed class IncidentAccessEvaluator(AuthorizeOperationHandler authorization)
{
    public async Task<AuthorizationDecision> AuthorizeAsync(string verifiedSubject, PermissionAction action,
        Guid? incidentId, IReadOnlyCollection<Guid> associatedStationIds, bool isCreation = false,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(verifiedSubject)) return AuthorizationDecision.Denied("verified-principal-required");
        ArgumentNullException.ThrowIfNull(associatedStationIds);
        var principal = new VerifiedPrincipal(verifiedSubject);
        if (isCreation)
        {
            if (associatedStationIds.Count == 0) return AuthorizationDecision.Denied("station-scope-required");
            AuthorizationDecision? unavailable = null;
            foreach (var stationId in associatedStationIds)
            {
                if (stationId == Guid.Empty) continue;
                var decision = await authorization.HandleAsync(new(principal, action, ProtectedResource.Station(stationId)), cancellationToken);
                if (decision.Outcome == AuthorizationOutcome.Allowed) return decision;
                if (decision.Outcome == AuthorizationOutcome.Unavailable) unavailable = decision;
            }
            return unavailable ?? AuthorizationDecision.Denied("no-reported-station-in-scope");
        }

        if (!incidentId.HasValue || incidentId == Guid.Empty) return AuthorizationDecision.Denied("incident-required");
        // A no-resource check permits only explicit global grants (currently Controller for these actions).
        var global = await authorization.HandleAsync(new(principal, action, ProtectedResource.None), cancellationToken);
        if (global.Outcome == AuthorizationOutcome.Allowed) return global;
        if (global.Outcome == AuthorizationOutcome.Unavailable) return global;
        if (associatedStationIds.Count == 0) return AuthorizationDecision.Denied("incident-station-scope-required");
        foreach (var stationId in associatedStationIds)
        {
            if (stationId == Guid.Empty) return AuthorizationDecision.Denied("invalid-incident-station");
            var decision = await authorization.HandleAsync(new(principal, action, ProtectedResource.Station(stationId)), cancellationToken);
            if (decision.Outcome != AuthorizationOutcome.Allowed) return decision;
        }
        return AuthorizationDecision.Allowed();
    }
}
