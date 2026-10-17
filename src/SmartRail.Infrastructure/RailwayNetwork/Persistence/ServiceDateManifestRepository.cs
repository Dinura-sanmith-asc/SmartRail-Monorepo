using Microsoft.EntityFrameworkCore;
using SmartRail.Domain.RailwayNetwork.Entities;
using SmartRail.Infrastructure.RailwayNetwork;

namespace SmartRail.Infrastructure.RailwayNetwork.Persistence;

public sealed class ServiceDateManifestRepository(SmartRailDbContext dbContext)
{
    /// <summary>Atomically replaces one operation-date publication, including a complete date with zero services.</summary>
    public async Task PublishAsync(
        ServiceDateManifest manifest,
        IReadOnlyCollection<ServiceOperationDate> operations,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(operations);
        if (operations.Any(item => item.OperationDate != manifest.OperationDate))
            throw new ArgumentException("Every operation row must match the manifest date.", nameof(operations));
        if (operations.Select(item => item.ScheduleId).Distinct().Count() != operations.Count)
            throw new ArgumentException("A schedule can appear only once in an operation-date publication.", nameof(operations));
        if (!manifest.IsComplete && operations.Count != 0)
            throw new ArgumentException("An incomplete publication cannot assert operating services.", nameof(operations));

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        if (manifest.IsComplete && operations.Count > 0)
        {
            var publishedScheduleIds = await dbContext.ServiceScheduleRevisions
                .Where(schedule => schedule.IsPublished && schedule.SupersededAt == null)
                .Select(schedule => schedule.ScheduleId)
                .ToListAsync(cancellationToken);
            if (operations.Any(item => !publishedScheduleIds.Contains(item.ScheduleId)))
                throw new InvalidOperationException("Operation dates may reference only current published schedule revisions.");
        }

        var oldOperations = await dbContext.ServiceOperationDates
            .Where(item => item.OperationDate == manifest.OperationDate).ToListAsync(cancellationToken);
        dbContext.ServiceOperationDates.RemoveRange(oldOperations);
        var previous = await dbContext.ServiceDateManifests.FindAsync([manifest.OperationDate], cancellationToken);
        if (previous is not null) dbContext.ServiceDateManifests.Remove(previous);
        await dbContext.SaveChangesAsync(cancellationToken);

        dbContext.ServiceDateManifests.Add(manifest);
        dbContext.ServiceOperationDates.AddRange(operations);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}