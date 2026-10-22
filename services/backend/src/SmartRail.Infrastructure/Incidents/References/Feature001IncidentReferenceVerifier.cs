using Microsoft.EntityFrameworkCore;
using SmartRail.Application.Incidents.References;
using SmartRail.Domain.RailwayNetwork.Entities;
using SmartRail.Infrastructure.RailwayNetwork;

namespace SmartRail.Infrastructure.Incidents.References;

/// <summary>Verifies incident station and service links against published Feature 001 owner data.</summary>
public sealed class Feature001IncidentReferenceVerifier(SmartRailDbContext dbContext) : IFeature001IncidentReferenceVerifier
{
    public async Task<IncidentReferenceCheck> VerifyAsync(IncidentReference reference, CancellationToken cancellationToken = default)
    {
        if (reference.ResourceId == Guid.Empty || reference.Kind is not (IncidentReferenceKind.Station or IncidentReferenceKind.Service))
            return new(IncidentReferenceOutcome.MissingOrInaccessible);

        try
        {
            if (reference.Kind == IncidentReferenceKind.Station)
            {
                var station = await dbContext.Stations.AsNoTracking()
                    .Where(x => x.StationId == reference.ResourceId)
                    .Select(x => new { x.IsPublished })
                    .SingleOrDefaultAsync(cancellationToken);
                return station is null
                    ? new(IncidentReferenceOutcome.MissingOrInaccessible)
                    : station.IsPublished
                        ? new(IncidentReferenceOutcome.Verified)
                        : new(IncidentReferenceOutcome.StaleOrInactive);
            }

            var service = await dbContext.TrainServices.AsNoTracking()
                .Where(x => x.ServiceId == reference.ResourceId)
                .Select(x => new { x.IsPublished })
                .SingleOrDefaultAsync(cancellationToken);
            if (service is null)
                return new(IncidentReferenceOutcome.MissingOrInaccessible);
            if (!service.IsPublished)
                return new(IncidentReferenceOutcome.StaleOrInactive);
            var hasCurrentSchedule = await dbContext.ServiceScheduleRevisions.AsNoTracking()
                .AnyAsync(x => x.ServiceId == reference.ResourceId && x.IsPublished && x.SupersededAt == null, cancellationToken);
            return hasCurrentSchedule
                ? new(IncidentReferenceOutcome.Verified)
                : new(IncidentReferenceOutcome.StaleOrInactive);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch
        {
            return new(IncidentReferenceOutcome.Unavailable);
        }
    }
}
