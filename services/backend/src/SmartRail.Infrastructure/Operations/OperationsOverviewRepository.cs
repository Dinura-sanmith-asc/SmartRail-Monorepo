using System.Data;
using Microsoft.EntityFrameworkCore;
using SmartRail.Application.Operations;
using SmartRail.Infrastructure.RailwayNetwork;

namespace SmartRail.Infrastructure.Operations;

/// <summary>Reads each daily board from one repeatable-read PostgreSQL snapshot.</summary>
public sealed class OperationsOverviewRepository(SmartRailDbContext db) : IOperationsOverviewRepository
{
    public async Task<IReadOnlyList<OperationsJourneyRecord>> ReadForDateAsync(DateOnly operationalDate,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
        var journeys = await db.Journeys.AsNoTracking()
            .Where(item => item.OperationDate == operationalDate)
            .OrderBy(item => item.JourneyInstanceId)
            .ToArrayAsync(cancellationToken);
        var results = new List<OperationsJourneyRecord>(journeys.Length);

        foreach (var journey in journeys)
        {
            var revision = await db.JourneyRevisions.AsNoTracking().SingleOrDefaultAsync(item =>
                item.JourneyInstanceId == journey.JourneyInstanceId && item.Revision == journey.CurrentRevision,
                cancellationToken);
            var service = await db.TrainServices.AsNoTracking().SingleOrDefaultAsync(item =>
                item.ServiceId == journey.ServiceId && item.IsPublished, cancellationToken);
            if (revision is null || service is null)
                throw new InvalidOperationException("Published operational data is incomplete.");
            var route = await db.Routes.AsNoTracking().SingleOrDefaultAsync(item =>
                item.RouteId == service.RouteId && item.IsPublished, cancellationToken);
            if (route is null) throw new InvalidOperationException("Published route data is incomplete.");

            var stops = await db.JourneyRevisionStops.AsNoTracking()
                .Where(item => item.JourneyInstanceId == journey.JourneyInstanceId && item.Revision == revision.Revision)
                .OrderBy(item => item.StopOrder).ToArrayAsync(cancellationToken);
            if (stops.Length < 2) throw new InvalidOperationException("Published journey calling points are incomplete.");
            var stationIds = stops.Select(item => item.StationId).Distinct().ToArray();
            var stationNames = await db.Stations.AsNoTracking()
                .Where(item => stationIds.Contains(item.StationId) && item.IsPublished)
                .ToDictionaryAsync(item => item.StationId, item => item.Name, cancellationToken);
            if (stationNames.Count != stationIds.Length)
                throw new InvalidOperationException("Published station reference data is incomplete.");

            results.Add(new OperationsJourneyRecord(journey, revision, service, route,
                stops.Select(item => new OperationsJourneyStop(item, stationNames[item.StationId])).ToArray()));
        }

        await transaction.CommitAsync(cancellationToken);
        return results;
    }
}
