using SmartRail.Application.Identity.Authorization;
using SmartRail.Application.Identity.Persistence;

namespace SmartRail.Application.Identity.Maintenance;

public sealed class RevokeRoleAssignmentHandler(IRoleAssignmentRepository repository, IIdentityMaintenanceAuthority? authority = null)
{
    public async Task<MaintenanceCommandResult> HandleAsync(RevokeRoleAssignmentCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (!await MaintenanceCommandGuard.IsAuthorizedAsync(authority, command.ActorSubject, MaintenanceAction.RevokeRole, cancellationToken))
            return MaintenanceCommandResult.Denied();
        try
        {
            var changed = await repository.RevokeAsync(command.AssignmentId, command.ActorSubject, command.RevokedAtUtc,
                command.AuditId, command.EvidenceReference, cancellationToken);
            return changed ? MaintenanceCommandResult.Completed() : MaintenanceCommandResult.Denied("active-role-assignment-not-found");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch { return MaintenanceCommandResult.Denied("maintenance-write-not-completed"); }
    }
}
