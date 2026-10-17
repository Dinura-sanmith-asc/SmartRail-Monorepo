namespace SmartRail.Application.RailwayNetwork.Stations;

public sealed record StationView(Guid StationId, string Name, IReadOnlyDictionary<string, string> VerifiedDetails);

public interface IStationRepository
{
    Task<IReadOnlyList<StationView>> ListPublishedAsync(CancellationToken cancellationToken = default);
    Task<bool> IsPublishedAsync(Guid stationId, CancellationToken cancellationToken = default);
}
