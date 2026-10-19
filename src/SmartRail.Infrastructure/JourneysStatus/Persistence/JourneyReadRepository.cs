using System.Data;
using Microsoft.EntityFrameworkCore;
using SmartRail.Application.JourneysStatus.Queries;
using SmartRail.Domain.JourneysStatus.Entities;
using SmartRail.Infrastructure.RailwayNetwork;

namespace SmartRail.Infrastructure.JourneysStatus.Persistence;

/// <summary>Reads a journey revision and its stop snapshot from one repeatable-read database view.</summary>
public sealed class JourneyReadRepository(SmartRailDbContext dbContext) : IJourneyReadRepository
{
    public async Task<JourneySnapshotReadResult> ReadSnapshotAsync(Guid journeyInstanceId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
        var journey = await dbContext.Journeys.AsNoTracking()
            .SingleOrDefaultAsync(item => item.JourneyInstanceId == journeyInstanceId, cancellationToken);
        if (journey is null)
        {
            await transaction.CommitAsync(cancellationToken);
            return new(JourneyReadOutcome.NotFound);
        }

        var revision = await dbContext.JourneyRevisions.AsNoTracking()
            .SingleOrDefaultAsync(item => item.JourneyInstanceId == journeyInstanceId && item.Revision == journey.CurrentRevision, cancellationToken);
        if (revision is null) return new(JourneyReadOutcome.Unavailable);

        var stops = await dbContext.JourneyRevisionStops.AsNoTracking()
            .Where(item => item.JourneyInstanceId == journeyInstanceId && item.Revision == revision.Revision)
            .OrderBy(item => item.StopOrder).ToArrayAsync(cancellationToken);
        if (stops.Length == 0) return new(JourneyReadOutcome.Unavailable);
        var stationIds = stops.Select(item => item.StationId).Distinct().ToArray();
        var stationNames = await dbContext.Stations.AsNoTracking()
            .Where(item => stationIds.Contains(item.StationId) && item.IsPublished)
            .ToDictionaryAsync(item => item.StationId, item => item.Name, cancellationToken);
        if (stationNames.Count != stationIds.Length) return new(JourneyReadOutcome.Unavailable);

        await transaction.CommitAsync(cancellationToken);
        return new(JourneyReadOutcome.Found, journey, revision, stops, stationNames);
    }

    public async Task<JourneyHistoryReadResult> ReadHistoryAsync(Guid journeyInstanceId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
        var journey = await dbContext.Journeys.AsNoTracking()
            .SingleOrDefaultAsync(item => item.JourneyInstanceId == journeyInstanceId, cancellationToken);
        if (journey is null)
        {
            await transaction.CommitAsync(cancellationToken);
            return new(JourneyReadOutcome.NotFound);
        }

        var revisions = await dbContext.JourneyRevisions.AsNoTracking()
            .Where(item => item.JourneyInstanceId == journeyInstanceId)
            .OrderBy(item => item.Revision).ToArrayAsync(cancellationToken);
        if (!revisions.Any(item => item.Revision == journey.CurrentRevision))
            return new(JourneyReadOutcome.Unavailable);
        var stops = await dbContext.JourneyRevisionStops.AsNoTracking()
            .Where(item => item.JourneyInstanceId == journeyInstanceId)
            .OrderBy(item => item.Revision).ThenBy(item => item.StopOrder)
            .ToArrayAsync(cancellationToken);
        var stopsByRevision = stops.GroupBy(item => item.Revision)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<JourneyRevisionStop>)group.ToArray());
        if (!stopsByRevision.TryGetValue(journey.CurrentRevision, out var currentStops) || currentStops.Count == 0)
            return new(JourneyReadOutcome.Unavailable);
        if (revisions.Any(item => item.UpdateId.HasValue && !stopsByRevision.ContainsKey(item.Revision)))
            return new(JourneyReadOutcome.Unavailable);
        var referencedStationIds = stops.Select(item => item.StationId).Distinct().ToArray();
        var publishedStationCount = await dbContext.Stations.AsNoTracking()
            .CountAsync(item => referencedStationIds.Contains(item.StationId) && item.IsPublished, cancellationToken);
        if (publishedStationCount != referencedStationIds.Length) return new(JourneyReadOutcome.Unavailable);

        await transaction.CommitAsync(cancellationToken);
        return new(JourneyReadOutcome.Found, revisions, stopsByRevision);
    }
}
