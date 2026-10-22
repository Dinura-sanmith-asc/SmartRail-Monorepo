namespace SmartRail.Application.JourneysStatus.Persistence;

public interface IJourneyUnitOfWork
{
    Task<IJourneyTransaction> BeginTransactionAsync(CancellationToken cancellationToken);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}

public interface IJourneyTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken);
    Task RollbackAsync(CancellationToken cancellationToken);
}
