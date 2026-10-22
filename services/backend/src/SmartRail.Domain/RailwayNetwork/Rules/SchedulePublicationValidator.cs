using SmartRail.Domain.RailwayNetwork.Entities;

namespace SmartRail.Domain.RailwayNetwork.Rules;

public sealed record SchedulePublicationValidationResult(IReadOnlyList<string> Errors)
{
    public bool IsValid => Errors.Count == 0;
}

public static class SchedulePublicationValidator
{
    public static SchedulePublicationValidationResult Validate(
        TrainService service,
        Route route,
        IReadOnlyCollection<RouteStop> routeStops,
        IReadOnlyCollection<ServiceScheduleRevision> revisions,
        IReadOnlyCollection<ServiceCallingPoint> callingPoints)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(route);
        ArgumentNullException.ThrowIfNull(routeStops);
        ArgumentNullException.ThrowIfNull(revisions);
        ArgumentNullException.ThrowIfNull(callingPoints);

        var errors = new List<string>();
        if (service.RouteId != route.RouteId) errors.Add("Service route reference does not match the supplied route.");
        if (service.IsPublished && !route.IsPublished) errors.Add("A published service requires a published route.");
        if (routeStops.Any(stop => stop.RouteId != route.RouteId)) errors.Add("Every route stop must belong to the supplied route.");
        if (routeStops.GroupBy(stop => stop.StopOrder).Any(group => group.Count() > 1)) errors.Add("Route stop order must be unique.");
        if (routeStops.GroupBy(stop => stop.RouteStopId).Any(group => group.Count() > 1)) errors.Add("Route stop IDs must be unique.");

        var current = revisions.Where(revision => revision.ServiceId == service.ServiceId && revision.IsCurrentPublished).ToArray();
        if (service.IsPublished && current.Length != 1) errors.Add("A published service must have exactly one current published schedule revision.");
        if (!service.IsPublished && current.Length > 0) errors.Add("An unpublished service cannot have a current published schedule revision.");
        if (revisions.Any(revision => revision.ServiceId != service.ServiceId)) errors.Add("Every schedule revision must belong to the supplied service.");
        var revisionIds = revisions.Select(revision => revision.ScheduleId).ToHashSet();
        if (callingPoints.Any(call => !revisionIds.Contains(call.ScheduleId))) errors.Add("A calling point must reference a known schedule revision.");

        if (current.Length == 1)
        {
            var revision = current[0];
            var revisionCalls = callingPoints.Where(call => call.ScheduleId == revision.ScheduleId).ToArray();
            var stopsById = routeStops.GroupBy(stop => stop.RouteStopId).ToDictionary(group => group.Key, group => group.First());
            var linked = new List<(ServiceCallingPoint Call, RouteStop Stop)>();
            foreach (var call in revisionCalls)
            {
                if (!stopsById.TryGetValue(call.RouteStopId, out var stop))
                {
                    errors.Add("Every calling point must reference a route stop on the service route.");
                    continue;
                }
                linked.Add((call, stop));
            }

            var ordered = linked.OrderBy(item => item.Call.StopOrder > 0 ? item.Call.StopOrder : item.Stop.StopOrder).ToArray();
            if (ordered.Any(item => item.Call.StopOrder < 0) || ordered.Select(item => item.Call.StopOrder > 0 ? item.Call.StopOrder : item.Stop.StopOrder).Distinct().Count() != ordered.Length)
                errors.Add("Calling point order must be positive and unique.");
            if (ordered.Length != revisionCalls.Length) errors.Add("Every calling point must resolve to a route stop.");
            if (ordered.Select(item => item.Stop.StationId).Distinct().Count() != ordered.Length)
                errors.Add("A schedule with repeated station calls cannot be published until occurrence semantics are approved.");
            if (ordered.Length > 0 && !ordered[0].Call.DepartureOffsetMinutes.HasValue)
                errors.Add("The first calling point must have a departure time.");
            if (ordered.Length > 0 && !ordered[^1].Call.ArrivalOffsetMinutes.HasValue)
                errors.Add("The final calling point must have an arrival time.");
            if (ordered.Any(item => item.Call.ArrivalOffsetMinutes.HasValue && item.Call.DepartureOffsetMinutes.HasValue &&
                                    item.Call.ArrivalOffsetMinutes > item.Call.DepartureOffsetMinutes))
                errors.Add("Arrival must not be after departure at a calling point.");
        }

        return new SchedulePublicationValidationResult(errors.AsReadOnly());
    }
}
