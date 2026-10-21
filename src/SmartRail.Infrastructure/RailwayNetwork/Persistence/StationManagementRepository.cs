using Microsoft.EntityFrameworkCore;
using SmartRail.Application.RailwayNetwork.Stations;
using SmartRail.Domain.RailwayNetwork.Entities;

namespace SmartRail.Infrastructure.RailwayNetwork.Persistence;

public sealed class StationManagementRepository(SmartRailDbContext dbContext) : IStationManagementRepository
{
    public async Task<IReadOnlyList<StationManagementView>> ListAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Stations.AsNoTracking().OrderBy(station => station.Name)
            .Select(station => new StationManagementView(station.StationId, station.Name, station.VerifiedDetails, station.IsPublished))
            .ToListAsync(cancellationToken);

    public async Task<StationManagementView?> FindAsync(Guid stationId, CancellationToken cancellationToken = default) =>
        await dbContext.Stations.AsNoTracking().Where(station => station.StationId == stationId)
            .Select(station => new StationManagementView(station.StationId, station.Name, station.VerifiedDetails, station.IsPublished))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<StationManagementView> CreateAsync(CreateStationRequest request, CancellationToken cancellationToken = default)
    {
        var stationId = Guid.NewGuid();
        var station = new Station(stationId, $"manual:{stationId:D}", request.Name, true, request.VerifiedDetails);
        dbContext.Stations.Add(station);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new(station.StationId, station.Name, station.VerifiedDetails, station.IsPublished);
    }

    public async Task<StationManagementView?> UpdateAsync(Guid stationId, UpdateStationRequest request, CancellationToken cancellationToken = default)
    {
        var station = await dbContext.Stations.SingleOrDefaultAsync(item => item.StationId == stationId, cancellationToken);
        if (station is null) return null;
        station.UpdateVerifiedDetails(request.Name, request.VerifiedDetails ?? new Dictionary<string, string>());
        await dbContext.SaveChangesAsync(cancellationToken);
        return new(station.StationId, station.Name, station.VerifiedDetails, station.IsPublished);
    }

    public async Task<bool> DeleteAsync(Guid stationId, CancellationToken cancellationToken = default)
    {
        var station = await dbContext.Stations.SingleOrDefaultAsync(item => item.StationId == stationId, cancellationToken);
        if (station is null) return false;
        dbContext.Stations.Remove(station);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
