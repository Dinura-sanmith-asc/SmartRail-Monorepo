using Microsoft.EntityFrameworkCore;
using SmartRail.Application.JourneysStatus.Persistence;
using SmartRail.Application.JourneysStatus.Publication;
using SmartRail.Domain.JourneysStatus.Entities;
using SmartRail.Infrastructure.RailwayNetwork;

namespace SmartRail.Infrastructure.JourneysStatus.Persistence;

public sealed class JourneyPublicationRepository(SmartRailDbContext db, IJourneyRepository appendRepository) : IJourneyPublicationRepository
{
    public async Task<JourneyPublicationState?> FindCurrentAsync(Guid journeyInstanceId, CancellationToken cancellationToken = default)
    {
        var journey = await db.Journeys.AsNoTracking().SingleOrDefaultAsync(x => x.JourneyInstanceId == journeyInstanceId, cancellationToken);
        if (journey is null) return null;
        var history = await db.JourneyRevisions.AsNoTracking().Where(x => x.JourneyInstanceId == journeyInstanceId)
            .OrderBy(x => x.Revision).ToArrayAsync(cancellationToken);
        var revision = history.SingleOrDefault(x => x.Revision == journey.CurrentRevision);
        if (revision is null) return null;
        var stops = await db.JourneyRevisionStops.AsNoTracking()
            .Where(x => x.JourneyInstanceId == journeyInstanceId && x.Revision == journey.CurrentRevision)
            .OrderBy(x => x.StopOrder).ToArrayAsync(cancellationToken);
        return new(journey, revision, stops, history);
    }

    public Task<bool> HasEarlierUpdateAsync(Guid journeyInstanceId, Guid updateId, int beforeRevision, CancellationToken cancellationToken = default) =>
        db.JourneyRevisions.AsNoTracking().AnyAsync(x => x.JourneyInstanceId == journeyInstanceId && x.Revision < beforeRevision && x.UpdateId == updateId, cancellationToken);

    public Task<JourneyRevisionAppendResult> AppendAsync(Guid journeyInstanceId, int expectedRevision,
        JourneyRevision revision, IReadOnlyCollection<JourneyRevisionStop> stops, CancellationToken cancellationToken = default) =>
        appendRepository.AppendRevisionAsync(journeyInstanceId, expectedRevision, revision, stops, cancellationToken);
}
