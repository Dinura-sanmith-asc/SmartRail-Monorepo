using Microsoft.EntityFrameworkCore;
using SmartRail.Application.RailwayNetwork.Routes;

namespace SmartRail.Infrastructure.RailwayNetwork.Persistence;

public sealed class RouteReadRepository(SmartRailDbContext dbContext) : IRouteReadRepository
{
    public async Task<RouteStopsView?> GetPublishedStopsAsync(Guid routeId, CancellationToken cancellationToken = default)
    {
        var route = await dbContext.Routes.AsNoTracking()
            .Where(item => item.RouteId == routeId && item.IsPublished)
            .Select(item => new { item.RouteId, item.RouteCode, item.Name })
            .SingleOrDefaultAsync(cancellationToken);
        if (route is null) return null;
        var stops = await (from stop in dbContext.RouteStops.AsNoTracking()
                           join station in dbContext.Stations.AsNoTracking() on stop.StationId equals station.StationId
                           where stop.RouteId == routeId && station.IsPublished
                           orderby stop.StopOrder
                           select new RouteStopView(stop.StopOrder, station.StationId, station.Name))
            .ToListAsync(cancellationToken);
        return new(route.RouteId, route.RouteCode, route.Name, stops);
    }
}
