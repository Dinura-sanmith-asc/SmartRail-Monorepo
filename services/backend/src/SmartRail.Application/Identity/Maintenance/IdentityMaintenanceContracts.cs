using SmartRail.Domain.Identity.Entities;

namespace SmartRail.Application.Identity.Maintenance;

public enum MaintenanceCommandOutcome { Completed, Denied }
public sealed record MaintenanceCommandResult(MaintenanceCommandOutcome Outcome, string ReasonCode)
{
    public static MaintenanceCommandResult Completed() => new(MaintenanceCommandOutcome.Completed, "completed");
    public static MaintenanceCommandResult Denied(string reason = "maintenance-authority-not-approved") => new(MaintenanceCommandOutcome.Denied, reason);
}

public sealed record GrantRoleAssignmentCommand(string ActorSubject, string TargetSubject, Domain.Identity.RoleCode RoleCode,
    DateTimeOffset GrantedAtUtc, Guid AssignmentId, Guid AuditId, string? EvidenceReference = null);
public sealed record RevokeRoleAssignmentCommand(string ActorSubject, Guid AssignmentId, DateTimeOffset RevokedAtUtc,
    Guid AuditId, string? EvidenceReference = null);
public sealed record GrantStationScopeCommand(string ActorSubject, string TargetSubject, Guid StationId,
    DateTimeOffset GrantedAtUtc, Guid AssignmentId, Guid AuditId, string? EvidenceReference = null);
public sealed record RevokeStationScopeCommand(string ActorSubject, Guid AssignmentId, DateTimeOffset RevokedAtUtc,
    Guid AuditId, string? EvidenceReference = null);

internal static class MaintenanceCommandGuard
{
    public static async Task<bool> IsAuthorizedAsync(Authorization.IIdentityMaintenanceAuthority? authority,
        string actorSubject, Authorization.MaintenanceAction action, CancellationToken cancellationToken)
    {
        if (authority is null || string.IsNullOrWhiteSpace(actorSubject)) return false;
        try
        {
            var decision = await authority.AuthorizeAsync(actorSubject, action, cancellationToken);
            return decision.Outcome == Authorization.MaintenanceAuthorityOutcome.Allowed;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch { return false; }
    }
}
