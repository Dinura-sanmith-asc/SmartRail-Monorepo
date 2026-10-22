using SmartRail.Domain.Incidents.Entities;

namespace SmartRail.Application.Incidents.Persistence;

public sealed record IncidentFilters(Guid? IncidentId = null, Guid? StationId = null, Guid? JourneyId = null);
public sealed record IncidentRecord(Incident Incident, IReadOnlyList<IncidentAssociation> Associations);
public sealed record IdempotencyLookup(string ActorSubject, string Operation, string Key);

/// <summary>Signals an expected-revision race after the surrounding transaction has been rolled back.</summary>
public sealed class IncidentConcurrencyException : Exception { }

/// <summary>Framework-neutral persistence port for incident state, history, links, and retries.</summary>
public interface IIncidentRepository
{
    Task<IncidentRecord?> FindAsync(Guid incidentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IncidentRecord>> ListAsync(IncidentFilters filters, CancellationToken cancellationToken = default);
    Task<IncidentIdempotencyRecord?> FindIdempotencyAsync(IdempotencyLookup lookup, CancellationToken cancellationToken = default);
    Task CreateAsync(Incident incident, IncidentRevision revision, IReadOnlyCollection<IncidentAssociation> associations,
        IncidentIdempotencyRecord idempotencyRecord, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(Incident incident, IncidentRevision revision, IReadOnlyCollection<IncidentAssociation> addAssociations,
        IReadOnlyCollection<IncidentAssociation> removeAssociations, IncidentIdempotencyRecord idempotencyRecord,
        int expectedRevision, CancellationToken cancellationToken = default);
}
