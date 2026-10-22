using SmartRail.Domain.RailwayNetwork.Entities;
using SmartRail.Domain.RailwayNetwork.Rules;

namespace SmartRail.UnitTests.RailwayNetwork;

public sealed class RouteOrderingTests
{
    [Fact]
    public void Publication_validation_rejects_duplicate_stop_order_and_unresolved_call_stop()
    {
        var routeId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        var scheduleId = Guid.NewGuid();
        var now = DateTimeOffset.Parse("2026-10-17T08:00:00+05:30");
        var service = new TrainService(serviceId, "S-1", routeId, isPublished: true);
        var route = new Route(routeId, "R-1", "Test route", isPublished: true);
        var revision = new ServiceScheduleRevision(scheduleId, serviceId, 1, new TimeOnly(8, 0), now, true, now);
        var first = new RouteStop(Guid.NewGuid(), routeId, Guid.NewGuid(), 1);
        var second = new RouteStop(Guid.NewGuid(), routeId, Guid.NewGuid(), 1);
        var calls = new[]
        {
            new ServiceCallingPoint(scheduleId, first.RouteStopId, null, 0),
            new ServiceCallingPoint(scheduleId, Guid.NewGuid(), 60, null)
        };

        var result = SchedulePublicationValidator.Validate(service, route, [first, second], [revision], calls);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, message => message.Contains("order must be unique", StringComparison.Ordinal));
        Assert.Contains(result.Errors, message => message.Contains("route stop", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Publication_validation_keeps_distinct_station_ids_even_when_names_could_match()
    {
        var routeId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        var scheduleId = Guid.NewGuid();
        var now = DateTimeOffset.Parse("2026-10-17T08:00:00+05:30");
        var service = new TrainService(serviceId, "S-2", routeId, isPublished: true);
        var route = new Route(routeId, "R-2", "Similar names", isPublished: true);
        var revision = new ServiceScheduleRevision(scheduleId, serviceId, 1, new TimeOnly(8, 0), now, true, now);
        var first = new RouteStop(Guid.NewGuid(), routeId, Guid.NewGuid(), 1);
        var second = new RouteStop(Guid.NewGuid(), routeId, Guid.NewGuid(), 2);
        var calls = new[]
        {
            new ServiceCallingPoint(scheduleId, first.RouteStopId, null, 0),
            new ServiceCallingPoint(scheduleId, second.RouteStopId, 60, null)
        };

        var result = SchedulePublicationValidator.Validate(service, route, [first, second], [revision], calls);

        Assert.True(result.IsValid);
        Assert.NotEqual(first.StationId, second.StationId);
    }
}
