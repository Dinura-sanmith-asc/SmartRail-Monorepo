using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using SmartRail.Application.JourneysStatus.Persistence;
using SmartRail.Domain.JourneysStatus.Entities;
using SmartRail.Infrastructure.RailwayNetwork;

namespace SmartRail.Infrastructure.JourneysStatus.Persistence;

public sealed class JourneyRepository(SmartRailDbContext dbContext) : IJourneyRepository
{
    public Task<Journey?> FindByIdAsync(Guid journeyInstanceId, CancellationToken cancellationToken) =>
        dbContext.Journeys.AsNoTracking().SingleOrDefaultAsync(x => x.JourneyInstanceId == journeyInstanceId, cancellationToken);

    public Task<int?> GetCurrentRevisionAsync(Guid journeyInstanceId, CancellationToken cancellationToken) =>
        dbContext.Journeys.AsNoTracking().Where(x => x.JourneyInstanceId == journeyInstanceId)
            .Select(x => (int?)x.CurrentRevision).SingleOrDefaultAsync(cancellationToken);

    public async Task<JourneyRevisionAppendResult> AppendRevisionAsync(Guid journeyInstanceId, int expectedRevision,
        JourneyRevision revision, IReadOnlyCollection<JourneyRevisionStop> stops, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var journey = await dbContext.Journeys.SingleOrDefaultAsync(x => x.JourneyInstanceId == journeyInstanceId, cancellationToken);
        if (journey is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return new(JourneyRevisionAppendOutcome.NotFound, null);
        }
        if (journey.CurrentRevision != expectedRevision || revision.JourneyInstanceId != journeyInstanceId ||
            revision.Revision != expectedRevision + 1 || stops.Any(stop => stop.JourneyInstanceId != journeyInstanceId || stop.Revision != revision.Revision))
        {
            await transaction.RollbackAsync(cancellationToken);
            return new(JourneyRevisionAppendOutcome.Stale, journey.CurrentRevision);
        }

        journey.AdvanceRevision(expectedRevision);
        dbContext.JourneyRevisions.Add(revision);
        dbContext.JourneyRevisionStops.AddRange(stops);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new(JourneyRevisionAppendOutcome.Appended, journey.CurrentRevision);
        }
        catch (DbUpdateConcurrencyException)
        {
            try { await transaction.RollbackAsync(cancellationToken); } catch (ObjectDisposedException) { }
            dbContext.ChangeTracker.Clear();
            var current = await GetCurrentRevisionAsync(journeyInstanceId, cancellationToken);
            return new(JourneyRevisionAppendOutcome.Stale, current);
        }
        catch (DbUpdateException exception) when (FindPostgresException(exception)?.ConstraintName == "pk_journey_revision")
        {
            try { await transaction.RollbackAsync(cancellationToken); } catch (ObjectDisposedException) { }
            dbContext.ChangeTracker.Clear();
            var current = await GetCurrentRevisionAsync(journeyInstanceId, cancellationToken);
            if (current > expectedRevision) return new(JourneyRevisionAppendOutcome.Stale, current);
            throw;
        }
    }

    private static PostgresException? FindPostgresException(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
            if (current is PostgresException postgres) return postgres;
        return null;
    }
}

public sealed class JourneyUnitOfWork(SmartRailDbContext dbContext) : IJourneyUnitOfWork
{
    public async Task<IJourneyTransaction> BeginTransactionAsync(CancellationToken cancellationToken) =>
        new JourneyTransaction(await dbContext.Database.BeginTransactionAsync(cancellationToken));
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);

    private sealed class JourneyTransaction(IDbContextTransaction transaction) : IJourneyTransaction
    {
        public Task CommitAsync(CancellationToken cancellationToken) => transaction.CommitAsync(cancellationToken);
        public Task RollbackAsync(CancellationToken cancellationToken) => transaction.RollbackAsync(cancellationToken);
        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }
}
