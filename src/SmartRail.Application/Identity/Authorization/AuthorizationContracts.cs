using SmartRail.Domain.Identity;

namespace SmartRail.Application.Identity.Authorization;

/// <summary>The authorization subject supplied by the API only after token validation.</summary>
public sealed record VerifiedPrincipal(string CognitoSubject);

public enum AuthorizationOutcome { Allowed, Denied, Unavailable }
public sealed record AuthorizationDecision(AuthorizationOutcome Outcome, string ReasonCode)
{
    public static AuthorizationDecision Allowed() => new(AuthorizationOutcome.Allowed, "explicit-grant-and-evidence-verified");
    public static AuthorizationDecision Denied(string reason = "permission-or-scope-denied") => new(AuthorizationOutcome.Denied, reason);
    public static AuthorizationDecision Unavailable(string reason = "authorization-evidence-unavailable") => new(AuthorizationOutcome.Unavailable, reason);
}

public enum EvidenceOutcome { VerifiedMatch, VerifiedNoMatch, Missing, Stale, Revoked, Unavailable }
public sealed record EvidenceCheck(EvidenceOutcome Outcome)
{
    public bool IsMatch => Outcome == EvidenceOutcome.VerifiedMatch;
    public bool IsUnavailable => Outcome == EvidenceOutcome.Unavailable;
}

public enum ProtectedResourceKind { None, Station, Journey, Incident, CateringOrder }
/// <summary>Server-created target context; never bind this from caller-supplied role/scope claims.</summary>
public sealed record ProtectedResource(ProtectedResourceKind Kind, Guid ResourceId)
{
    public static ProtectedResource None { get; } = new(ProtectedResourceKind.None, Guid.Empty);
    public static ProtectedResource Station(Guid id) => new(ProtectedResourceKind.Station, id);
    public static ProtectedResource Journey(Guid id) => new(ProtectedResourceKind.Journey, id);
    public static ProtectedResource Incident(Guid id) => new(ProtectedResourceKind.Incident, id);
    public static ProtectedResource CateringOrder(Guid id) => new(ProtectedResourceKind.CateringOrder, id);
}

public sealed record AuthorizationRequest(VerifiedPrincipal Principal, PermissionAction Action, ProtectedResource Resource);
public sealed record ResolveResourceScopeQuery(VerifiedPrincipal Principal, ProtectedResource Resource, bool RequireStationAssignment = false);
public sealed record IncidentResourceEvidence(EvidenceOutcome Outcome, Guid? StationId = null, Guid? JourneyId = null);

/// <summary>Driver Operations owns and verifies current driver-to-journey assignments.</summary>
public interface IDriverJourneyAssignmentEvidence
{
    Task<EvidenceCheck> CheckAssignmentAsync(string verifiedDriverSubject, Guid journeyId, CancellationToken cancellationToken = default);
}

/// <summary>Incident Management owns incident relationships to their station or journey resources.</summary>
public interface IIncidentResourceEvidence
{
    Task<IncidentResourceEvidence> ResolveIncidentAsync(Guid incidentId, CancellationToken cancellationToken = default);
}

/// <summary>Catering Adapter owns order-owner associations; the owner identity is not returned to this module.</summary>
public interface IPassengerOrderOwnershipEvidence
{
    Task<EvidenceCheck> CheckOwnershipAsync(string verifiedPassengerSubject, Guid orderId, CancellationToken cancellationToken = default);
}

public enum MaintenanceAction { GrantRole, RevokeRole, GrantStationScope, RevokeStationScope }
public enum MaintenanceAuthorityOutcome { Allowed, Denied, Unavailable }
public sealed record MaintenanceAuthorityDecision(MaintenanceAuthorityOutcome Outcome);

/// <summary>Only an explicitly approved trusted process may authorize assignment changes.</summary>
public interface IIdentityMaintenanceAuthority
{
    Task<MaintenanceAuthorityDecision> AuthorizeAsync(string verifiedActorSubject, MaintenanceAction action, CancellationToken cancellationToken = default);
}

/// <summary>Safe default until an approved production maintenance authority is explicitly wired.</summary>
public sealed class DenyAllIdentityMaintenanceAuthority : IIdentityMaintenanceAuthority
{
    public Task<MaintenanceAuthorityDecision> AuthorizeAsync(string verifiedActorSubject, MaintenanceAction action, CancellationToken cancellationToken = default) =>
        Task.FromResult(new MaintenanceAuthorityDecision(MaintenanceAuthorityOutcome.Denied));
}
