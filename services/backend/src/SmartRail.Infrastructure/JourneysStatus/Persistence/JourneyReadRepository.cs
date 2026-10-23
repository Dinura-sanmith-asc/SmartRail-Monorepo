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

    public async Task<JourneyStationDeparturesReadResult> ReadStationDeparturesAsync(Guid stationId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
        var station = await dbContext.Stations.AsNoTracking()
            .Where(item => item.StationId == stationId && item.IsPublished)
            .Select(item => new { item.StationId, item.Name }).SingleOrDefaultAsync(cancellationToken);
        if (station is null)
        {
            await transaction.CommitAsync(cancellationToken);
            return new(JourneyReadOutcome.NotFound);
        }

        var incompleteCurrentState = await dbContext.Journeys.AsNoTracking().AnyAsync(journey =>
            !dbContext.JourneyRevisions.Any(revision => revision.JourneyInstanceId == journey.JourneyInstanceId &&
                revision.Revision == journey.CurrentRevision) ||
            !dbContext.JourneyRevisionStops.Any(stop => stop.JourneyInstanceId == journey.JourneyInstanceId &&
                stop.Revision == journey.CurrentRevision), cancellationToken);
        if (incompleteCurrentState) return new(JourneyReadOutcome.Unavailable);

        var candidates = await (from journey in dbContext.Journeys.AsNoTracking()
                                join revision in dbContext.JourneyRevisions.AsNoTracking()
                                    on new { journey.JourneyInstanceId, Revision = journey.CurrentRevision }
                                    equals new { revision.JourneyInstanceId, revision.Revision }
                                join stop in dbContext.JourneyRevisionStops.AsNoTracking()
                                    on new { journey.JourneyInstanceId, Revision = journey.CurrentRevision }
                                    equals new { stop.JourneyInstanceId, stop.Revision }
                                where stop.StationId == stationId
                                select new JourneyStationDepartureCandidate(journey, revision, stop, station.Name))
            .ToArrayAsync(cancellationToken);
        var candidateJourneyIds = candidates.Select(item => item.Journey.JourneyInstanceId).Distinct().ToArray();
        if (candidateJourneyIds.Length > 0)
        {
            var referencedStationIds = await (from journey in dbContext.Journeys.AsNoTracking()
                                              join stop in dbContext.JourneyRevisionStops.AsNoTracking()
                                                  on new { journey.JourneyInstanceId, Revision = journey.CurrentRevision }
                                                  equals new { stop.JourneyInstanceId, stop.Revision }
                                              where candidateJourneyIds.Contains(journey.JourneyInstanceId)
                                              select stop.StationId).Distinct().ToArrayAsync(cancellationToken);
            var publishedStationCount = await dbContext.Stations.AsNoTracking()
                .CountAsync(item => referencedStationIds.Contains(item.StationId) && item.IsPublished, cancellationToken);
            if (publishedStationCount != referencedStationIds.Length) return new(JourneyReadOutcome.Unavailable);
        }
        var serviceIds = candidates.Select(item => item.Journey.ServiceId).Distinct().ToArray();
        var services = await dbContext.TrainServices.AsNoTracking()
            .Where(item => serviceIds.Contains(item.ServiceId))
            .ToDictionaryAsync(item => item.ServiceId, cancellationToken);
        var boundaries = await (from journey in dbContext.Journeys.AsNoTracking()
                                join stop in dbContext.JourneyRevisionStops.AsNoTracking()
                                    on new { journey.JourneyInstanceId, Revision = journey.CurrentRevision }
                                    equals new { stop.JourneyInstanceId, stop.Revision }
                                where candidateJourneyIds.Contains(journey.JourneyInstanceId)
                                select new { journey.JourneyInstanceId, stop.StopOrder, stop.StationId })
            .ToArrayAsync(cancellationToken);
        var boundaryStationIds = boundaries.Select(stop => stop.StationId).Distinct().ToArray();
        var boundaryNames = await dbContext.Stations.AsNoTracking()
            .Where(item => boundaryStationIds.Contains(item.StationId))
            .ToDictionaryAsync(item => item.StationId, item => item.Name, cancellationToken);
        var directions = boundaries.GroupBy(item => item.JourneyInstanceId).ToDictionary(group => group.Key, group =>
        {
            var ordered = group.OrderBy(item => item.StopOrder).ToArray();
            return (Origin: boundaryNames[ordered[0].StationId], Destination: boundaryNames[ordered[^1].StationId]);
        });
        var enriched = candidates.Select(item =>
        {
            services.TryGetValue(item.Journey.ServiceId, out var service);
            directions.TryGetValue(item.Journey.JourneyInstanceId, out var direction);
            return item with { ServiceCode = service?.ServiceCode, DisplayName = service?.DisplayName,
                PublicServiceNumber = service?.PublicServiceNumber, OriginName = direction.Origin,
                DestinationName = direction.Destination };
        }).ToArray();
        await transaction.CommitAsync(cancellationToken);
        return new(JourneyReadOutcome.Found, enriched);
    }
}
