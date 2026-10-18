using SmartRail.Application.Identity.Persistence;

namespace SmartRail.Application.Identity.Authorization;

public sealed record ResourceScopeResult(AuthorizationOutcome Outcome, string ReasonCode, Guid? RelatedStationId = null)
{
    public static ResourceScopeResult Allowed(Guid? relatedStationId = null) => new(AuthorizationOutcome.Allowed, "resource-evidence-verified", relatedStationId);
    public static ResourceScopeResult Denied(string reason) => new(AuthorizationOutcome.Denied, reason);
    public static ResourceScopeResult Unavailable() => new(AuthorizationOutcome.Unavailable, "resource-evidence-unavailable");
}

public sealed class ResolveResourceScopeHandler(
    IStationScopeRepository stationScopes,
    IDriverJourneyAssignmentEvidence driverAssignments,
    IIncidentResourceEvidence incidents,
    IPassengerOrderOwnershipEvidence orderOwnership)
{
    public async Task<ResourceScopeResult> HandleAsync(ResolveResourceScopeQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.Principal is null || string.IsNullOrWhiteSpace(query.Principal.CognitoSubject))
            return ResourceScopeResult.Denied("verified-principal-required");

        try
        {
            switch (query.Resource.Kind)
            {
                case ProtectedResourceKind.None:
                    return ResourceScopeResult.Allowed();
                case ProtectedResourceKind.Station:
                    return await CheckStationAsync(query.Principal.CognitoSubject, query.Resource.ResourceId, cancellationToken);
                case ProtectedResourceKind.Journey:
                    return await FromCheck(await driverAssignments.CheckAssignmentAsync(query.Principal.CognitoSubject, query.Resource.ResourceId, cancellationToken));
                case ProtectedResourceKind.CateringOrder:
                    return await FromCheck(await orderOwnership.CheckOwnershipAsync(query.Principal.CognitoSubject, query.Resource.ResourceId, cancellationToken));
                case ProtectedResourceKind.Incident:
                    return await CheckIncidentAsync(query.Principal.CognitoSubject, query.Resource.ResourceId, query.RequireStationAssignment, cancellationToken);
                default:
                    return ResourceScopeResult.Denied("unknown-resource-kind");
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch { return ResourceScopeResult.Unavailable(); }
    }

    private async Task<ResourceScopeResult> CheckIncidentAsync(string subject, Guid incidentId, bool requireStationAssignment, CancellationToken cancellationToken)
    {
        if (incidentId == Guid.Empty) return ResourceScopeResult.Denied("incident-required");
        var incident = await incidents.ResolveIncidentAsync(incidentId, cancellationToken);
        if (incident.Outcome == EvidenceOutcome.Unavailable) return ResourceScopeResult.Unavailable();
        if (incident.Outcome != EvidenceOutcome.VerifiedMatch) return ResourceScopeResult.Denied("incident-resource-not-verified");
        if (!requireStationAssignment) return ResourceScopeResult.Allowed(incident.StationId);
        if (!incident.StationId.HasValue || incident.StationId.Value == Guid.Empty)
            return ResourceScopeResult.Denied("incident-station-not-verified");
        return await CheckStationAsync(subject, incident.StationId.Value, cancellationToken);
    }

    private async Task<ResourceScopeResult> CheckStationAsync(string subject, Guid stationId, CancellationToken cancellationToken)
    {
        if (stationId == Guid.Empty) return ResourceScopeResult.Denied("station-required");
        var stationIds = await stationScopes.GetActiveStationIdsAsync(subject, cancellationToken);
        return stationIds.Contains(stationId)
            ? ResourceScopeResult.Allowed()
            : ResourceScopeResult.Denied("station-scope-not-active");
    }

    private static Task<ResourceScopeResult> FromCheck(EvidenceCheck evidence) => Task.FromResult(
        evidence.Outcome == EvidenceOutcome.Unavailable
            ? ResourceScopeResult.Unavailable()
            : evidence.IsMatch ? ResourceScopeResult.Allowed() : ResourceScopeResult.Denied("resource-ownership-or-assignment-not-verified"));
}
