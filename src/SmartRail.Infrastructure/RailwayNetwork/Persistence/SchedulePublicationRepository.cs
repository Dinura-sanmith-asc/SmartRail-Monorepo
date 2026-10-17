using Microsoft.EntityFrameworkCore;
using SmartRail.Domain.RailwayNetwork.Entities;
using SmartRail.Domain.RailwayNetwork.Rules;
using SmartRail.Infrastructure.RailwayNetwork;

namespace SmartRail.Infrastructure.RailwayNetwork.Persistence;

public sealed class SchedulePublicationRejectedException(IReadOnlyList<string> errors)
    : InvalidOperationException("Schedule publication was rejected: " + string.Join("; ", errors))
{
    public IReadOnlyList<string> Errors { get; } = errors;
}

public sealed class SchedulePublicationRepository(SmartRailDbContext dbContext)
{
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

        foreach (var current in revisions.Where(item => item.IsCurrentPublished)) current.Supersede(publishedAt);
        candidate.Publish(publishedAt);
        service.Publish();

        var routeStops = await dbContext.RouteStops.Where(stop => stop.RouteId == route.RouteId).ToListAsync(cancellationToken);
        var calls = await dbContext.ServiceCallingPoints.ToListAsync(cancellationToken);
        var result = SchedulePublicationValidator.Validate(service, route, routeStops, revisions, calls);
        if (!result.IsValid) throw new SchedulePublicationRejectedException(result.Errors);

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}