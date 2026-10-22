using Microsoft.EntityFrameworkCore;
using SmartRail.Application.DriverOperations;
using SmartRail.Domain.DriverOperations;
using SmartRail.Domain.DriverOperations.Entities;
using SmartRail.Infrastructure.RailwayNetwork;
using Npgsql;

namespace SmartRail.Infrastructure.DriverOperations;

public sealed class DriverOperationsRepository(SmartRailDbContext db) : IDriverOperationsRepository
{
    public async Task<IReadOnlyList<DriverJourneyAssignment>> GetActiveAssignmentsAsync(string? subject, CancellationToken cancellationToken = default) =>
        await db.DriverJourneyAssignments.AsNoTracking().Where(x => x.RevokedAtUtc == null && (subject == null || x.DriverSubject == subject))
            .OrderBy(x => x.AssignedAtUtc).ThenBy(x => x.AssignmentId).ToListAsync(cancellationToken);
    public Task<DriverJourneyAssignment?> FindActiveAssignmentAsync(string subject, Guid journeyId, CancellationToken cancellationToken = default) =>
        db.DriverJourneyAssignments.AsNoTracking().SingleOrDefaultAsync(x => x.DriverSubject == subject && x.JourneyInstanceId == journeyId && x.RevokedAtUtc == null, cancellationToken);
    public Task<DriverJourneyAssignment?> FindAssignmentAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.DriverJourneyAssignments.AsNoTracking().SingleOrDefaultAsync(x => x.AssignmentId == id, cancellationToken);
    public async Task<IReadOnlyList<DriverObservation>> GetObservationsAsync(Guid journeyId, CancellationToken cancellationToken = default) =>
        await db.DriverObservations.AsNoTracking().Where(x => x.JourneyInstanceId == journeyId).OrderBy(x => x.ReportedAtUtc).ThenBy(x => x.ObservationId).ToListAsync(cancellationToken);
    public Task<DriverObservation?> FindObservationAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.DriverObservations.AsNoTracking().SingleOrDefaultAsync(x => x.ObservationId == id, cancellationToken);
    public Task<DriverObservation?> FindRequestAsync(string subject, Guid requestId, CancellationToken cancellationToken = default) =>
        db.DriverObservations.AsNoTracking().SingleOrDefaultAsync(x => x.DriverSubject == subject && x.RequestId == requestId, cancellationToken);
    public async Task AddObservationAsync(DriverObservation observation, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // Serialize submission with revocation of this exact assignment lifecycle.
            var assignment = await db.DriverJourneyAssignments.FromSqlInterpolated(
                $"SELECT * FROM driver_journey_assignment WHERE assignment_id = {observation.AssignmentId} FOR UPDATE")
                .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
            if (assignment is null || !assignment.IsActive || assignment.DriverSubject != observation.DriverSubject || assignment.JourneyInstanceId != observation.JourneyInstanceId)
                throw new DriverAssignmentWriteConflictException();
            db.DriverObservations.Add(observation);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException error) when (error.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            await transaction.RollbackAsync(cancellationToken);
            db.Entry(observation).State = EntityState.Detached;
            throw new DriverObservationWriteConflictException();
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            db.Entry(observation).State = EntityState.Detached;
            throw;
        }
    }
    public async Task GrantAssignmentAsync(DriverJourneyAssignment assignment, DriverAssignmentAudit audit, CancellationToken cancellationToken = default)
    {
        if (audit.AssignmentId != assignment.AssignmentId || audit.Action != DriverAssignmentAuditAction.Granted || audit.ActorSubject != assignment.AssignedBySubject || audit.OccurredAtUtc != assignment.AssignedAtUtc)
            throw new ArgumentException("Assignment and grant audit evidence must agree.");
        db.DriverJourneyAssignments.Add(assignment);
        db.DriverAssignmentAudits.Add(audit);
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException error) when (error.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            db.Entry(assignment).State = EntityState.Detached;
            db.Entry(audit).State = EntityState.Detached;
            throw new DriverAssignmentWriteConflictException();
        }
    }
    public async Task<bool> RevokeAssignmentAsync(Guid id, string actor, DateTimeOffset at, Guid auditId, string evidence, CancellationToken cancellationToken = default)
    {
        var assignment = await db.DriverJourneyAssignments.SingleOrDefaultAsync(x => x.AssignmentId == id, cancellationToken);
        if (assignment is null || !assignment.IsActive) return false;
        assignment.Revoke(actor, at);
        db.DriverAssignmentAudits.Add(new DriverAssignmentAudit(auditId, id, DriverAssignmentAuditAction.Revoked, actor, at, evidence));
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException)
        {
            db.Entry(assignment).State = EntityState.Detached;
            foreach (var entry in db.ChangeTracker.Entries<DriverAssignmentAudit>().Where(x => x.State == EntityState.Added).ToArray()) entry.State = EntityState.Detached;
            throw new DriverAssignmentWriteConflictException();
        }
        return true;
    }
}
