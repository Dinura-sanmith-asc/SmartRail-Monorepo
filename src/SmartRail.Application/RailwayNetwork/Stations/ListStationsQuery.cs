namespace SmartRail.Application.RailwayNetwork.Stations;

public sealed record ListStationsQuery;
public sealed record ListStationsResult(IReadOnlyList<StationView> Items);

public sealed class ListStationsHandler(IStationRepository repository)
{
    public async Task<ListStationsResult> HandleAsync(ListStationsQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        return new(await repository.ListPublishedAsync(cancellationToken));
    }
}
