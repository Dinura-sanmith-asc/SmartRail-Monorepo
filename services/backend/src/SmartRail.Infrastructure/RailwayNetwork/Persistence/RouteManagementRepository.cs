using Microsoft.EntityFrameworkCore;
using SmartRail.Application.RailwayNetwork.Routes;
using SmartRail.Domain.RailwayNetwork.Entities;

namespace SmartRail.Infrastructure.RailwayNetwork.Persistence;

public sealed class RouteManagementRepository(SmartRailDbContext db) : IRouteManagementRepository
{
    public async Task<IReadOnlyList<RouteManagementView>> ListAsync(CancellationToken cancellationToken = default)
    {
        var routes = await db.Routes.AsNoTracking().OrderBy(route => route.RouteCode).ToListAsync(cancellationToken);
        var stops = await (from stop in db.RouteStops.AsNoTracking()
                           join station in db.Stations.AsNoTracking() on stop.StationId equals station.StationId
                           orderby stop.RouteId, stop.StopOrder
                           select new { stop.RouteId, View = new RouteManagementStopView(stop.RouteStopId, stop.StationId, station.Name, stop.StopOrder) })
            .ToListAsync(cancellationToken);

        return routes.Select(route => new RouteManagementView(route.RouteId, route.RouteCode, route.Name, route.IsPublished,
            stops.Where(stop => stop.RouteId == route.RouteId).Select(stop => stop.View).ToArray())).ToArray();
    }

    public async Task<RouteManagementView?> CreateAsync(SaveRouteRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureStationsExistAsync(request.StationIds, cancellationToken);
        var route = new Route(Guid.NewGuid(), request.RouteCode, request.Name);
        db.Routes.Add(route);
        for (var index = 0; index < request.StationIds.Count; index++)
            db.RouteStops.Add(new RouteStop(Guid.NewGuid(), route.RouteId, request.StationIds[index], index + 1));
        await db.SaveChangesAsync(cancellationToken);
        return await FindViewAsync(route.RouteId, cancellationToken);
    }

    public async Task<RouteManagementView?> UpdateAsync(Guid routeId, SaveRouteRequest request, CancellationToken cancellationToken = default)
    {
        var route = await db.Routes.SingleOrDefaultAsync(item => item.RouteId == routeId, cancellationToken);
        if (route is null) return null;
        await EnsureStationsExistAsync(request.StationIds, cancellationToken);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var existingStops = await db.RouteStops.Where(stop => stop.RouteId == routeId)
            .OrderBy(stop => stop.StopOrder).ToListAsync(cancellationToken);
        var stopsByStation = existingStops.GroupBy(stop => stop.StationId)
            .ToDictionary(group => group.Key, group => new Queue<RouteStop>(group));
        var requestedStops = request.StationIds.Select(stationId =>
            stopsByStation.TryGetValue(stationId, out var queue) && queue.Count > 0 ? queue.Dequeue() : null).ToArray();
        var retainedStops = requestedStops.Where(stop => stop is not null).Cast<RouteStop>().ToArray();
        var removedStops = stopsByStation.Values.SelectMany(queue => queue).ToArray();

        route.Rename(request.RouteCode, request.Name);
        db.RouteStops.RemoveRange(removedStops);
        await db.SaveChangesAsync(cancellationToken);

        var temporaryStart = checked(Math.Max(existingStops.Select(stop => stop.StopOrder).DefaultIfEmpty(0).Max(), request.StationIds.Count) + request.StationIds.Count + 10);
        for (var index = 0; index < retainedStops.Length; index++)
            retainedStops[index].Reorder(temporaryStart + index);
        await db.SaveChangesAsync(cancellationToken);

        for (var index = 0; index < request.StationIds.Count; index++)
        {
            if (requestedStops[index] is null)
            {
                var created = new RouteStop(Guid.NewGuid(), routeId, request.StationIds[index], temporaryStart + retainedStops.Length + index);
                requestedStops[index] = created;
                db.RouteStops.Add(created);
            }
        }
        await db.SaveChangesAsync(cancellationToken);

        for (var index = 0; index < request.StationIds.Count; index++)
            requestedStops[index]!.Reorder(index + 1);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await FindViewAsync(routeId, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid routeId, CancellationToken cancellationToken = default)
    {
        var route = await db.Routes.SingleOrDefaultAsync(item => item.RouteId == routeId, cancellationToken);
        if (route is null) return false;
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var stops = await db.RouteStops.Where(stop => stop.RouteId == routeId).ToListAsync(cancellationToken);
        db.RouteStops.RemoveRange(stops);
        await db.SaveChangesAsync(cancellationToken);
        db.Routes.Remove(route);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    private async Task EnsureStationsExistAsync(IReadOnlyList<Guid> stationIds, CancellationToken cancellationToken)
    {
        if (stationIds.Any(id => id == Guid.Empty)) throw new ArgumentException("Every route stop must include a valid station.");
        var found = await db.Stations.CountAsync(station => stationIds.Contains(station.StationId), cancellationToken);
        if (found != stationIds.Count) throw new KeyNotFoundException("One or more selected stations do not exist.");
    }

    private async Task<RouteManagementView?> FindViewAsync(Guid routeId, CancellationToken cancellationToken)
    {
        var route = await db.Routes.AsNoTracking().SingleOrDefaultAsync(item => item.RouteId == routeId, cancellationToken);
        if (route is null) return null;
        var stops = await (from stop in db.RouteStops.AsNoTracking()
                           join station in db.Stations.AsNoTracking() on stop.StationId equals station.StationId
                           where stop.RouteId == routeId
                           orderby stop.StopOrder
                           select new RouteManagementStopView(stop.RouteStopId, station.StationId, station.Name, stop.StopOrder))
            .ToArrayAsync(cancellationToken);
        return new RouteManagementView(route.RouteId, route.RouteCode, route.Name, route.IsPublished, stops);
    }
}
