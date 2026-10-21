using Microsoft.EntityFrameworkCore;
using Npgsql;
using SmartRail.Application.JourneysStatus.Initialization;
using SmartRail.Domain.RailwayNetwork.Entities;
using SmartRail.Infrastructure.RailwayNetwork;

namespace SmartRail.Infrastructure.JourneysStatus.Initialization;

public sealed class JourneyScheduleLinkVerifier(SmartRailDbContext dbContext) : IJourneyScheduleLinkVerifier
{
    public async Task<JourneyScheduleLinkResult> VerifyAsync(JourneyScheduleLinkRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
        if (request.ServiceId == Guid.Empty || request.ScheduleId == Guid.Empty || request.ScheduleRevision <= 0)
            return new(ScheduleLinkVerificationStatus.Invalid, null);

        var service = await dbContext.TrainServices.AsNoTracking()
            .SingleOrDefaultAsync(x => x.ServiceId == request.ServiceId, cancellationToken);
        var schedule = await dbContext.ServiceScheduleRevisions.AsNoTracking()
            .SingleOrDefaultAsync(x => x.ScheduleId == request.ScheduleId, cancellationToken);
        var hasDate = await dbContext.ServiceOperationDates.AsNoTracking()
            .AnyAsync(x => x.ScheduleId == request.ScheduleId && x.OperationDate == request.OperationDate, cancellationToken);
        var completeness = await dbContext.ServiceDateManifests.AsNoTracking()
            .SingleOrDefaultAsync(x => x.OperationDate == request.OperationDate, cancellationToken);
        if (service is null || schedule is null || !service.IsPublished || !schedule.IsCurrentPublished ||
            schedule.ServiceId != service.ServiceId || schedule.Revision != request.ScheduleRevision ||
            !hasDate || completeness is null || !completeness.IsComplete ||
            schedule.FirstDepartureLocalTime != request.FirstDepartureOccurrence)
            return new(ScheduleLinkVerificationStatus.Invalid, null);

        var stops = await (from call in dbContext.ServiceCallingPoints.AsNoTracking()
                           join routeStop in dbContext.RouteStops.AsNoTracking() on call.RouteStopId equals routeStop.RouteStopId
                           where call.ScheduleId == request.ScheduleId && routeStop.RouteId == service.RouteId
                           orderby call.StopOrder
                           select new { Call = call, RouteStop = routeStop })
            .ToListAsync(cancellationToken);
        if (stops.Count == 0 || stops[0].Call.DepartureOffsetMinutes != 0)
            return new(ScheduleLinkVerificationStatus.Invalid, null);

        var localStart = request.OperationDate.ToDateTime(schedule.FirstDepartureLocalTime);
        var utcOffset = TimeSpan.FromHours(5.5);
        var planned = stops.Select(row => new JourneyScheduleStop(Guid.NewGuid(), row.RouteStop.StationId,
            row.RouteStop.RouteStopId, row.Call.StopOrder,
            row.Call.ArrivalOffsetMinutes.HasValue ? new DateTimeOffset(localStart.AddMinutes(row.Call.ArrivalOffsetMinutes.Value), utcOffset).ToUniversalTime() : null,
            row.Call.DepartureOffsetMinutes.HasValue ? new DateTimeOffset(localStart.AddMinutes(row.Call.DepartureOffsetMinutes.Value), utcOffset).ToUniversalTime() : null)).ToArray();
            return new(ScheduleLinkVerificationStatus.Verified, new JourneyScheduleLink(planned, DateTimeOffset.UtcNow));
        }
        catch (NpgsqlException)
        {
            return new(ScheduleLinkVerificationStatus.Unavailable, null);
        }
        catch (TimeoutException)
        {
            return new(ScheduleLinkVerificationStatus.Unavailable, null);
        }
    }
}
