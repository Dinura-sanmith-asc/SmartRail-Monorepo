using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using SmartRail.Application.JourneysStatus.Persistence;
using SmartRail.Application.JourneysStatus.Initialization;
using SmartRail.Domain.JourneysStatus;
using SmartRail.Domain.JourneysStatus.Entities;
using SmartRail.Infrastructure.RailwayNetwork;

namespace SmartRail.Infrastructure.JourneysStatus.Persistence;

public sealed class JourneyInitializationRepository(SmartRailDbContext dbContext) : IJourneyInitializationRepository
{
    public async Task<JourneyInitializationStoreResult> InitializeAsync(JourneyInitializationRecord record,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var bySource = await dbContext.Journeys.SingleOrDefaultAsync(x =>
                x.SourceSystem == record.Journey.SourceSystem && x.SourceInstanceKey == record.Journey.SourceInstanceKey,
                cancellationToken);
            if (bySource is not null)
            {
                var replay = SameLinkage(bySource, record.Journey) &&
                    (!record.HasAuthoritativeJourneyId || bySource.JourneyInstanceId == record.Journey.JourneyInstanceId);
                await transaction.RollbackAsync(cancellationToken);
                await transaction.DisposeAsync();
                if (replay) return new(JourneyInitializationStoreOutcome.Replayed, bySource);
                await AppendConflictAuditAsync(CreateConflictAudit(record), cancellationToken);
                return new(JourneyInitializationStoreOutcome.Conflict, null);
            }

            var byId = await dbContext.Journeys.FindAsync([record.Journey.JourneyInstanceId], cancellationToken);
            if (byId is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
                await transaction.DisposeAsync();
                await AppendConflictAuditAsync(CreateConflictAudit(record), cancellationToken);
                return new(JourneyInitializationStoreOutcome.Conflict, null);
            }

            dbContext.Journeys.Add(record.Journey);
            dbContext.JourneyRevisions.Add(record.Baseline);
            dbContext.JourneyRevisionStops.AddRange(record.Stops);
            dbContext.JourneyInitializationAudits.Add(record.AcceptedAudit);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new(JourneyInitializationStoreOutcome.Created, record.Journey);
        }
        catch (DbUpdateException exception)
        {
            try { await transaction.RollbackAsync(cancellationToken); }
            catch (ObjectDisposedException) { }
            await transaction.DisposeAsync();
            dbContext.ChangeTracker.Clear();
            var postgresError = FindPostgresException(exception);
            if (postgresError?.SqlState != PostgresErrorCodes.UniqueViolation ||
                postgresError.ConstraintName is not ("ux_journey_source_key" or "pk_journey"))
                throw;
            var committed = await dbContext.Journeys.AsNoTracking().SingleOrDefaultAsync(x =>
                x.SourceSystem == record.Journey.SourceSystem && x.SourceInstanceKey == record.Journey.SourceInstanceKey,
                cancellationToken);
            var replay = committed is not null && SameLinkage(committed, record.Journey) &&
                (!record.HasAuthoritativeJourneyId || committed.JourneyInstanceId == record.Journey.JourneyInstanceId);
            if (replay) return new(JourneyInitializationStoreOutcome.Replayed, committed);
            await AppendConflictAuditAsync(CreateConflictAudit(record), cancellationToken);
            return new(JourneyInitializationStoreOutcome.Conflict, null);
        }
    }

    public async Task AppendConflictAuditAsync(JourneyInitializationAudit audit, CancellationToken cancellationToken)
    {
        dbContext.ChangeTracker.Clear();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        dbContext.JourneyInitializationAudits.Add(audit);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static bool SameLinkage(Journey existing, Journey candidate) =>
        existing.ServiceId == candidate.ServiceId && existing.ScheduleId == candidate.ScheduleId &&
        existing.ScheduleRevision == candidate.ScheduleRevision && existing.OperationDate == candidate.OperationDate &&
        existing.FirstDepartureOccurrence == candidate.FirstDepartureOccurrence;

    private static JourneyInitializationAudit CreateConflictAudit(JourneyInitializationRecord record) =>
        new(Guid.NewGuid(), record.Journey.SourceSystem, record.Journey.SourceInstanceKey,
            JourneyInitializationDecision.RejectedConflict, DateTimeOffset.UtcNow,
            record.Journey.InitializationProvenance, record.Journey.JourneyInstanceId,
            record.Journey.InitializedBySubject, record.Journey.ServiceId, record.Journey.ScheduleId,
            record.Journey.ScheduleRevision, record.Journey.OperationDate, record.Journey.FirstDepartureOccurrence);

    private static PostgresException? FindPostgresException(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException!)
            if (current is PostgresException postgres) return postgres;
        return null;
    }
}
