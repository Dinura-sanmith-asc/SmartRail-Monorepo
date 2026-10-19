using Microsoft.EntityFrameworkCore;
using SmartRail.Application.Incidents.Persistence;
using SmartRail.Infrastructure.RailwayNetwork;

namespace SmartRail.Infrastructure.Incidents.Persistence;

/// <summary>Runs incident mutations in one PostgreSQL transaction.</summary>
public sealed class IncidentUnitOfWork(SmartRailDbContext dbContext) : IIncidentUnitOfWork
{
    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var result = await operation(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            try { await transaction.RollbackAsync(CancellationToken.None); }
            catch { /* Preserve the original failure; no exception text is exposed by the API. */ }
            dbContext.ChangeTracker.Clear();
            throw;
        }
    }
}
