namespace SmartRail.Application.Incidents.Persistence;

/// <summary>Transaction boundary without exposing a persistence framework to Application.</summary>
public interface IIncidentUnitOfWork
{
    Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default);
}
