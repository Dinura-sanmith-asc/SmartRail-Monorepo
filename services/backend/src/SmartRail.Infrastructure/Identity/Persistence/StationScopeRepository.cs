using Microsoft.EntityFrameworkCore;
using SmartRail.Application.Identity.Persistence;
using SmartRail.Domain.Identity.Entities;
using SmartRail.Infrastructure.RailwayNetwork;

namespace SmartRail.Infrastructure.Identity.Persistence;

public sealed class StationScopeRepository(SmartRailDbContext dbContext) : IStationScopeRepository
{
    public async Task<IReadOnlyList<Guid>> GetActiveStationIdsAsync(
        string verifiedCognitoSubject,
        CancellationToken cancellationToken = default) =>
        await dbContext.StationScopeAssignments.AsNoTracking()
            .Where(x => x.CognitoSubject == verifiedCognitoSubject && x.RevokedAtUtc == null && x.RevokedBySubject == null)
            .OrderBy(x => x.StationId)
            .Select(x => x.StationId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<StationScopeAssignment>> GetActiveAssignmentsAsync(
        string verifiedCognitoSubject,
        CancellationToken cancellationToken = default) =>
        await dbContext.StationScopeAssignments.AsNoTracking()
            .Where(x => x.CognitoSubject == verifiedCognitoSubject && x.RevokedAtUtc == null && x.RevokedBySubject == null)
            .OrderBy(x => x.StationId)
            .ToListAsync(cancellationToken);

    public async Task GrantAsync(
        StationScopeAssignment assignment,
        StationScopeAssignmentAudit audit,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(assignment);
        ArgumentNullException.ThrowIfNull(audit);
        if (audit.StationScopeAssignmentId != assignment.Id || audit.Action != AssignmentAuditAction.Granted ||
            !string.Equals(audit.ActorSubject, assignment.GrantedBySubject, StringComparison.Ordinal) ||
            audit.OccurredAtUtc != assignment.GrantedAtUtc)
        {
            throw new ArgumentException("Grant audit evidence must match its station-scope assignment.", nameof(audit));
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        dbContext.StationScopeAssignments.Add(assignment);
        dbContext.StationScopeAssignmentAudits.Add(audit);
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
        var assignment = await dbContext.StationScopeAssignments
            .SingleOrDefaultAsync(x => x.Id == assignmentId && x.RevokedAtUtc == null && x.RevokedBySubject == null, cancellationToken);
        if (assignment is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        assignment.Revoke(verifiedActorSubject, occurredAtUtc);
        dbContext.StationScopeAssignmentAudits.Add(StationScopeAssignmentAudit.ForRevocation(auditId, assignment, evidenceReference));
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }
}
