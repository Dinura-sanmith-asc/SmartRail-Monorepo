using Microsoft.EntityFrameworkCore;
using SmartRail.Application.RailwayNetwork.Services;
using SmartRail.Domain.RailwayNetwork.Entities;
using SmartRail.Domain.RailwayNetwork.Rules;
using SmartRail.Infrastructure.RailwayNetwork;

namespace SmartRail.Infrastructure.RailwayNetwork.Persistence;

public sealed class SchedulePublicationRejectedException(IReadOnlyList<string> errors)
    : InvalidOperationException("Schedule publication was rejected: " + string.Join("; ", errors))
{
    public IReadOnlyList<string> Errors { get; } = errors;
}

public sealed class SchedulePublicationRepository(SmartRailDbContext dbContext) : ISchedulePublicationRepository
{
    public async Task PublishTimetableAsync(TimetablePublicationRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Dates.GroupBy(item => item.Manifest.OperationDate).Any(group => group.Count() > 1))
            throw new ArgumentException("Each operation date may appear only once.", nameof(request));

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var service = await dbContext.TrainServices.SingleOrDefaultAsync(item => item.ServiceId == request.ServiceId, cancellationToken)
            ?? throw new InvalidOperationException("The train service does not exist.");
        var route = await dbContext.Routes.SingleOrDefaultAsync(item => item.RouteId == service.RouteId, cancellationToken)
            ?? throw new InvalidOperationException("The service route does not exist.");
        var revisions = await dbContext.ServiceScheduleRevisions.Where(item => item.ServiceId == service.ServiceId).ToListAsync(cancellationToken);
        var candidate = revisions.SingleOrDefault(item => item.ScheduleId == request.ScheduleId)
            ?? throw new InvalidOperationException("The schedule revision does not belong to the train service.");
        if (candidate.IsCurrentPublished) throw new InvalidOperationException("The schedule revision is already current and published.");
        foreach (var current in revisions.Where(item => item.SlotId == candidate.SlotId && item.IsCurrentPublished)) current.Supersede(request.PublishedAt);
        // Clear the partial unique index before making the replacement current; both writes remain atomic.
        await dbContext.SaveChangesAsync(cancellationToken);
        candidate.Publish(request.PublishedAt);
        service.Publish();
        var routeStops = await dbContext.RouteStops.Where(stop => stop.RouteId == route.RouteId).ToListAsync(cancellationToken);
        var revisionIds = revisions.Select(item => item.ScheduleId).ToArray();
        var calls = await dbContext.ServiceCallingPoints
            .Where(item => revisionIds.Contains(item.ScheduleId))
            .ToListAsync(cancellationToken);
        var validation = SchedulePublicationValidator.Validate(service, route, routeStops, revisions, calls);
        if (!validation.IsValid) throw new SchedulePublicationRejectedException(validation.Errors);
        await dbContext.SaveChangesAsync(cancellationToken);

        var currentScheduleIds = await dbContext.ServiceScheduleRevisions
            .Where(item => item.IsPublished && item.SupersededAt == null).Select(item => item.ScheduleId).ToListAsync(cancellationToken);
        foreach (var date in request.Dates)
        {
            if (date.Operations.Any(item => item.OperationDate != date.Manifest.OperationDate))
                throw new ArgumentException("Operation rows must match their manifest date.", nameof(request));
            if (!date.Manifest.IsComplete && date.Operations.Count != 0)
                throw new ArgumentException("An incomplete date cannot assert operating services.", nameof(request));
            if (date.Operations.Any(item => !currentScheduleIds.Contains(item.ScheduleId)))
                throw new InvalidOperationException("Operation dates may reference only current published schedule revisions.");

            var oldOperations = await dbContext.ServiceOperationDates
                .Where(item => item.OperationDate == date.Manifest.OperationDate).ToListAsync(cancellationToken);
            dbContext.ServiceOperationDates.RemoveRange(oldOperations);
            var oldManifest = await dbContext.ServiceDateManifests.FindAsync([date.Manifest.OperationDate], cancellationToken);
            if (oldManifest is not null) dbContext.ServiceDateManifests.Remove(oldManifest);
            await dbContext.SaveChangesAsync(cancellationToken);
            dbContext.ServiceDateManifests.Add(date.Manifest);
            dbContext.ServiceOperationDates.AddRange(date.Operations);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task PublishAsync(Guid serviceId, Guid scheduleId, DateTimeOffset publishedAt, CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var service = await dbContext.TrainServices.SingleOrDefaultAsync(item => item.ServiceId == serviceId, cancellationToken)
            ?? throw new InvalidOperationException("The train service does not exist.");
        var route = await dbContext.Routes.SingleOrDefaultAsync(item => item.RouteId == service.RouteId, cancellationToken)
            ?? throw new InvalidOperationException("The service route does not exist.");
        var revisions = await dbContext.ServiceScheduleRevisions.Where(item => item.ServiceId == serviceId).ToListAsync(cancellationToken);
        var candidate = revisions.SingleOrDefault(item => item.ScheduleId == scheduleId)
            ?? throw new InvalidOperationException("The schedule revision does not belong to the train service.");
        if (candidate.IsCurrentPublished) throw new InvalidOperationException("The schedule revision is already current and published.");

        foreach (var current in revisions.Where(item => item.SlotId == candidate.SlotId && item.IsCurrentPublished)) current.Supersede(publishedAt);
        // Clear the partial unique index before making the replacement current; both writes remain atomic.
        await dbContext.SaveChangesAsync(cancellationToken);
        candidate.Publish(publishedAt);
        service.Publish();

        var routeStops = await dbContext.RouteStops.Where(stop => stop.RouteId == route.RouteId).ToListAsync(cancellationToken);
        var revisionIds = revisions.Select(item => item.ScheduleId).ToArray();
        var calls = await dbContext.ServiceCallingPoints
            .Where(item => revisionIds.Contains(item.ScheduleId))
            .ToListAsync(cancellationToken);
        var result = SchedulePublicationValidator.Validate(service, route, routeStops, revisions, calls);
        if (!result.IsValid) throw new SchedulePublicationRejectedException(result.Errors);

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
