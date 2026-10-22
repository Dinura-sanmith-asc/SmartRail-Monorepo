using Microsoft.EntityFrameworkCore;
using SmartRail.Application.Incidents.Persistence;
using SmartRail.Domain.Incidents.Entities;
using SmartRail.Infrastructure.RailwayNetwork;

namespace SmartRail.Infrastructure.Incidents.Persistence;

/// <summary>PostgreSQL persistence for incident aggregates and their transaction evidence.</summary>
public sealed class IncidentRepository(SmartRailDbContext dbContext) : IIncidentRepository
{
    public async Task<IncidentRecord?> FindAsync(Guid incidentId, CancellationToken cancellationToken = default)
    {
        var incident = await dbContext.Incidents.AsNoTracking()
            .SingleOrDefaultAsync(x => x.IncidentId == incidentId, cancellationToken);
        if (incident is null) return null;
        var associations = await dbContext.IncidentAssociations.AsNoTracking()
            .Where(x => x.IncidentId == incidentId && x.RemovedRevision == null)
            .OrderBy(x => x.AddedRevision).ThenBy(x => x.AssociationId)
            .ToListAsync(cancellationToken);
        return new IncidentRecord(incident, associations);
    }

    public async Task<IReadOnlyList<IncidentRecord>> ListAsync(IncidentFilters filters, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Incidents.AsNoTracking().AsQueryable();
        if (filters.IncidentId.HasValue)
            query = query.Where(x => x.IncidentId == filters.IncidentId.Value);
        if (filters.StationId.HasValue)
            query = query.Where(x => dbContext.IncidentAssociations.Any(a => a.IncidentId == x.IncidentId &&
                a.Kind == SmartRail.Domain.Incidents.IncidentAssociationKind.Station && a.ResourceId == filters.StationId.Value && a.RemovedRevision == null));
        if (filters.JourneyId.HasValue)
            query = query.Where(x => dbContext.IncidentAssociations.Any(a => a.IncidentId == x.IncidentId &&
                a.Kind == SmartRail.Domain.Incidents.IncidentAssociationKind.Journey && a.ResourceId == filters.JourneyId.Value && a.RemovedRevision == null));
        var incidents = await query.OrderByDescending(x => x.UpdatedAt).ThenBy(x => x.IncidentId).ToListAsync(cancellationToken);
        if (incidents.Count == 0) return [];
        var ids = incidents.Select(x => x.IncidentId).ToArray();
        var associations = await dbContext.IncidentAssociations.AsNoTracking()
            .Where(x => ids.Contains(x.IncidentId) && x.RemovedRevision == null)
            .OrderBy(x => x.AddedRevision).ThenBy(x => x.AssociationId)
            .ToListAsync(cancellationToken);
        var byIncident = associations.GroupBy(x => x.IncidentId).ToDictionary(x => x.Key, x => (IReadOnlyList<IncidentAssociation>)x.ToArray());
        return incidents.Select(incident => new IncidentRecord(incident,
            byIncident.GetValueOrDefault(incident.IncidentId, Array.Empty<IncidentAssociation>()))).ToArray();
    }

    public Task<IncidentIdempotencyRecord?> FindIdempotencyAsync(IdempotencyLookup lookup, CancellationToken cancellationToken = default) =>
        dbContext.IncidentIdempotencyRecords.AsNoTracking().SingleOrDefaultAsync(x =>
            x.ActorSubject == lookup.ActorSubject && x.Operation == lookup.Operation && x.IdempotencyKey == lookup.Key,
            cancellationToken);

    public async Task CreateAsync(Incident incident, IncidentRevision revision, IReadOnlyCollection<IncidentAssociation> associations,
        IncidentIdempotencyRecord idempotencyRecord, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(incident);
        ArgumentNullException.ThrowIfNull(revision);
        ArgumentNullException.ThrowIfNull(associations);
        ArgumentNullException.ThrowIfNull(idempotencyRecord);
        if (revision.IncidentId != incident.IncidentId || revision.Revision != 1 || incident.Revision != 1 ||
            associations.Any(x => x.IncidentId != incident.IncidentId || x.AddedRevision != 1) ||
            idempotencyRecord.IncidentId != incident.IncidentId)
            throw new ArgumentException("Initial incident state, revision, associations and retry record must share one incident identity.");

        dbContext.Incidents.Add(incident);
        dbContext.IncidentRevisions.Add(revision);
        dbContext.IncidentAssociations.AddRange(associations);
        dbContext.IncidentIdempotencyRecords.Add(idempotencyRecord);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> UpdateAsync(Incident incident, IncidentRevision revision, IReadOnlyCollection<IncidentAssociation> addAssociations,
        IReadOnlyCollection<IncidentAssociation> removeAssociations, IncidentIdempotencyRecord idempotencyRecord,
        int expectedRevision, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(incident);
        ArgumentNullException.ThrowIfNull(revision);
        ArgumentNullException.ThrowIfNull(addAssociations);
        ArgumentNullException.ThrowIfNull(removeAssociations);
        ArgumentNullException.ThrowIfNull(idempotencyRecord);
        if (expectedRevision <= 0 || incident.Revision != expectedRevision + 1 ||
            revision.IncidentId != incident.IncidentId || revision.Revision != incident.Revision ||
            revision.State != incident.State ||
            (revision.PreviousState.HasValue && !SmartRail.Domain.Incidents.IncidentLifecycle.CanTransition(revision.PreviousState.Value, revision.State)) ||
            idempotencyRecord.IncidentId != incident.IncidentId ||
            addAssociations.Any(x => x.IncidentId != incident.IncidentId || x.AddedRevision != incident.Revision) ||
            removeAssociations.Any(x => x.IncidentId != incident.IncidentId || x.RemovedRevision != incident.Revision))
            throw new ArgumentException("Incident mutation, revision evidence, associations and retry record must share one expected revision.");

        var incidentEntry = dbContext.Attach(incident);
        incidentEntry.State = EntityState.Modified;
        incidentEntry.Property(x => x.Revision).OriginalValue = expectedRevision;
        try
        {
            // Claim the expected revision before inserting evidence. This makes the
            // aggregate revision the concurrency gate even when two transactions race
            // to insert the same (IncidentId, Revision) evidence key.
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new IncidentConcurrencyException();
        }

        foreach (var association in addAssociations) dbContext.IncidentAssociations.Add(association);
        foreach (var association in removeAssociations)
        {
            if (association.IsActive || association.RemovedRevision != incident.Revision)
                throw new ArgumentException("Only active associations removed by this incident revision can be persisted.", nameof(removeAssociations));
            var associationEntry = dbContext.Attach(association);
            associationEntry.State = EntityState.Modified;
            associationEntry.Property(x => x.RemovedRevision).OriginalValue = null;
        }
        dbContext.IncidentRevisions.Add(revision);
        dbContext.IncidentIdempotencyRecords.Add(idempotencyRecord);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
