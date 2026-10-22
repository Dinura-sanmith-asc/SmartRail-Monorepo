using Microsoft.EntityFrameworkCore;
using SmartRail.Application.RailwayNetwork.Stations;

namespace SmartRail.Infrastructure.RailwayNetwork.Persistence;

public sealed class StationReadRepository(SmartRailDbContext dbContext) : IStationRepository
{
    public async Task<IReadOnlyList<StationView>> ListPublishedAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Stations.AsNoTracking().Where(item => item.IsPublished)
            .OrderBy(item => item.Name).ThenBy(item => item.StationId)
            .Select(item => new StationView(item.StationId, item.Name, item.VerifiedDetails))
            .ToListAsync(cancellationToken);

    public Task<bool> IsPublishedAsync(Guid stationId, CancellationToken cancellationToken = default) =>
        dbContext.Stations.AsNoTracking().AnyAsync(item => item.StationId == stationId && item.IsPublished, cancellationToken);
}
