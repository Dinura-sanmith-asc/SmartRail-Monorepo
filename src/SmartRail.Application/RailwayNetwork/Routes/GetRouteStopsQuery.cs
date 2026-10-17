namespace SmartRail.Application.RailwayNetwork.Routes;

public sealed record GetRouteStopsQuery(Guid RouteId);
public sealed record GetRouteStopsResult(RouteStopsView? Route, bool IsInvalid, string? Error);

public sealed class GetRouteStopsHandler(IRouteReadRepository repository)
{
    public async Task<GetRouteStopsResult> HandleAsync(GetRouteStopsQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.RouteId == Guid.Empty) return new(null, true, "Route ID is required.");
        return new(await repository.GetPublishedStopsAsync(query.RouteId, cancellationToken), false, null);
    }
}
