using SmartRail.Application.Identity.Authorization;
using SmartRail.Application.Identity.Persistence;
using SmartRail.Domain.Identity.Entities;

namespace SmartRail.Application.Identity.Maintenance;

public sealed class GrantRoleAssignmentHandler(IRoleAssignmentRepository repository, IIdentityMaintenanceAuthority? authority = null)
{
    public async Task<MaintenanceCommandResult> HandleAsync(GrantRoleAssignmentCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (!await MaintenanceCommandGuard.IsAuthorizedAsync(authority, command.ActorSubject, MaintenanceAction.GrantRole, cancellationToken))
            return MaintenanceCommandResult.Denied();
        try
        {
            var assignment = new RoleAssignment(command.AssignmentId, command.TargetSubject, command.RoleCode, command.GrantedAtUtc, command.ActorSubject);
            await repository.GrantAsync(assignment, RoleAssignmentAudit.ForGrant(command.AuditId, assignment, command.EvidenceReference), cancellationToken);
            return MaintenanceCommandResult.Completed();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch { return MaintenanceCommandResult.Denied("maintenance-write-not-completed"); }
    }
}
