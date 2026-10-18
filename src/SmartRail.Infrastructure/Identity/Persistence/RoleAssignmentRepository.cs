using Microsoft.EntityFrameworkCore;
using SmartRail.Application.Identity.Persistence;
using SmartRail.Domain.Identity;
using SmartRail.Domain.Identity.Entities;
using SmartRail.Infrastructure.RailwayNetwork;

namespace SmartRail.Infrastructure.Identity.Persistence;

public sealed class RoleAssignmentRepository(SmartRailDbContext dbContext) : IRoleAssignmentRepository
{
    public async Task<IReadOnlyList<RoleCode>> GetActiveRolesAsync(
        string verifiedCognitoSubject,
        CancellationToken cancellationToken = default) =>
        await dbContext.RoleAssignments.AsNoTracking()
            .Where(x => x.CognitoSubject == verifiedCognitoSubject && x.RevokedAtUtc == null && x.RevokedBySubject == null)
            .OrderBy(x => x.RoleCode)
            .Select(x => x.RoleCode)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<RoleAssignment>> GetActiveAssignmentsAsync(
        string verifiedCognitoSubject,
        CancellationToken cancellationToken = default) =>
        await dbContext.RoleAssignments.AsNoTracking()
            .Where(x => x.CognitoSubject == verifiedCognitoSubject && x.RevokedAtUtc == null && x.RevokedBySubject == null)
            .OrderBy(x => x.RoleCode)
            .ToListAsync(cancellationToken);

    public async Task GrantAsync(
        RoleAssignment assignment,
        RoleAssignmentAudit audit,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(assignment);
        ArgumentNullException.ThrowIfNull(audit);
        if (audit.RoleAssignmentId != assignment.Id || audit.Action != AssignmentAuditAction.Granted ||
            !string.Equals(audit.ActorSubject, assignment.GrantedBySubject, StringComparison.Ordinal) ||
            audit.OccurredAtUtc != assignment.GrantedAtUtc)
        {
            throw new ArgumentException("Grant audit evidence must match its role assignment.", nameof(audit));
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        dbContext.RoleAssignments.Add(assignment);
        dbContext.RoleAssignmentAudits.Add(audit);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<bool> RevokeAsync(
        Guid assignmentId,
        string verifiedActorSubject,
        DateTimeOffset occurredAtUtc,
        Guid auditId,
        string? evidenceReference = null,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var assignment = await dbContext.RoleAssignments
            .SingleOrDefaultAsync(x => x.Id == assignmentId && x.RevokedAtUtc == null && x.RevokedBySubject == null, cancellationToken);
        if (assignment is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        assignment.Revoke(verifiedActorSubject, occurredAtUtc);
        dbContext.RoleAssignmentAudits.Add(RoleAssignmentAudit.ForRevocation(auditId, assignment, evidenceReference));
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }
}
