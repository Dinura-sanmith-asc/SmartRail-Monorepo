namespace SmartRail.Application.RailwayNetwork.Stations;

public sealed record StationManagementView(Guid StationId, string Name, IReadOnlyDictionary<string, string> VerifiedDetails, bool IsPublished);
public sealed record CreateStationRequest(string Name, IReadOnlyDictionary<string, string>? VerifiedDetails = null);
public sealed record UpdateStationRequest(string Name, IReadOnlyDictionary<string, string>? VerifiedDetails = null);

public interface IStationManagementRepository
{
    Task<IReadOnlyList<StationManagementView>> ListAsync(CancellationToken cancellationToken = default);
    Task<StationManagementView?> FindAsync(Guid stationId, CancellationToken cancellationToken = default);
    Task<StationManagementView> CreateAsync(CreateStationRequest request, CancellationToken cancellationToken = default);
    Task<StationManagementView?> UpdateAsync(Guid stationId, UpdateStationRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid stationId, CancellationToken cancellationToken = default);
}
