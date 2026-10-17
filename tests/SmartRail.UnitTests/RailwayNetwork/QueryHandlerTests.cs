using SmartRail.Application.RailwayNetwork.Routes;
using SmartRail.Application.RailwayNetwork.Services;
using SmartRail.Application.RailwayNetwork.Stations;

namespace SmartRail.UnitTests.RailwayNetwork;

public sealed class QueryHandlerTests
{
    [Fact]
    public async Task Station_list_and_service_detail_handlers_return_public_projections()
    {
        var station = new StationView(Guid.NewGuid(), "Central", new Dictionary<string, string> { ["district"] = "Colombo" });
        var stations = new StationRepository([station]);
        var listed = await new ListStationsHandler(stations).HandleAsync(new());
        Assert.Equal(station, Assert.Single(listed.Items));

        var service = new ServiceDetailView(Guid.NewGuid(), "S-1", Guid.NewGuid(), "R-1", "Route", 3, new TimeOnly(8, 15));
        var detail = await new GetServiceHandler(new ServiceRepository(service)).HandleAsync(new(service.ServiceId));
        Assert.Equal(service, detail.Service);
    }

    [Fact]
    public async Task Route_handler_returns_stops_in_repository_journey_order()
    {
        var routeId = Guid.NewGuid();
        var stops = new RouteStopsView(routeId, "R-1", "Route", [
            new RouteStopView(1, Guid.NewGuid(), "Origin"),
            new RouteStopView(2, Guid.NewGuid(), "Destination")
        ]);
        var result = await new GetRouteStopsHandler(new RouteRepository(stops)).HandleAsync(new(routeId));

        Assert.False(result.IsInvalid);
        Assert.Equal(new[] { 1, 2 }, result.Route!.Stops.Select(item => item.StopOrder));
        Assert.NotEqual(result.Route.Stops[0].StationId, result.Route.Stops[1].StationId);
    }

    private sealed class StationRepository(IReadOnlyList<StationView> stations) : IStationRepository
    {
        public Task<IReadOnlyList<StationView>> ListPublishedAsync(CancellationToken cancellationToken = default) => Task.FromResult(stations);
        public Task<bool> IsPublishedAsync(Guid stationId, CancellationToken cancellationToken = default) => Task.FromResult(stations.Any(item => item.StationId == stationId));
    }

    private sealed class RouteRepository(RouteStopsView route) : IRouteReadRepository
    {
        public Task<RouteStopsView?> GetPublishedStopsAsync(Guid routeId, CancellationToken cancellationToken = default) => Task.FromResult<RouteStopsView?>(route);
    }

    private sealed class ServiceRepository(ServiceDetailView service) : IServiceReadRepository
    {
        public Task<ServiceDetailView?> GetPublishedAsync(Guid serviceId, CancellationToken cancellationToken = default) => Task.FromResult<ServiceDetailView?>(service);
    }
}
