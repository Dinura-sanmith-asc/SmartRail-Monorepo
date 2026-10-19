using Microsoft.EntityFrameworkCore;
using SmartRail.Application.Identity.Authorization;
using SmartRail.Infrastructure.RailwayNetwork;

namespace SmartRail.Infrastructure.Incidents.References;

/// <summary>Resolves only incident existence plus one legacy boundary station/journey hint.</summary>
public sealed class IncidentResourceEvidenceProvider(SmartRailDbContext dbContext) : IIncidentResourceEvidence
{
    public async Task<IncidentResourceEvidence> ResolveIncidentAsync(Guid incidentId, CancellationToken cancellationToken = default)
    {
        if (incidentId == Guid.Empty) return new(EvidenceOutcome.Missing);
        try
        {
            var exists = await dbContext.Incidents.AsNoTracking().AnyAsync(x => x.IncidentId == incidentId, cancellationToken);
            if (!exists) return new(EvidenceOutcome.Missing);
            var activeLinks = await dbContext.IncidentAssociations.AsNoTracking()
                .Where(x => x.IncidentId == incidentId && x.RemovedRevision == null)
                .OrderBy(x => x.AssociationId)
                .Select(x => new { x.Kind, x.ResourceId })
                .ToArrayAsync(cancellationToken);
            var stationId = activeLinks.FirstOrDefault(x => x.Kind == SmartRail.Domain.Incidents.IncidentAssociationKind.Station)?.ResourceId;
            var journeyId = activeLinks.FirstOrDefault(x => x.Kind == SmartRail.Domain.Incidents.IncidentAssociationKind.Journey)?.ResourceId;
            return new(EvidenceOutcome.VerifiedMatch, stationId, journeyId);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch { return new(EvidenceOutcome.Unavailable); }
    }
}
