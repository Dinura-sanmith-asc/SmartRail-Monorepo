using Microsoft.EntityFrameworkCore;
using SmartRail.Application.RailwayNetwork.Services;
using SmartRail.Domain.RailwayNetwork.Rules;

namespace SmartRail.Infrastructure.RailwayNetwork.Persistence;

public sealed class ServiceSearchRepository(SmartRailDbContext dbContext) : IServiceSearchRepository
{
    public async Task<ServiceSearchRepositoryResult> SearchAsync(ServiceSearchCriteria query, CancellationToken cancellationToken = default)
    {
        var candidates = await (
            from service in dbContext.TrainServices.AsNoTracking()
            join route in dbContext.Routes.AsNoTracking() on service.RouteId equals route.RouteId
            join schedule in dbContext.ServiceScheduleRevisions.AsNoTracking() on service.ServiceId equals schedule.ServiceId
            join originCall in dbContext.ServiceCallingPoints.AsNoTracking() on schedule.ScheduleId equals originCall.ScheduleId
            join originStop in dbContext.RouteStops.AsNoTracking() on originCall.RouteStopId equals originStop.RouteStopId
            join originStation in dbContext.Stations.AsNoTracking() on originStop.StationId equals originStation.StationId
            join destinationCall in dbContext.ServiceCallingPoints.AsNoTracking() on schedule.ScheduleId equals destinationCall.ScheduleId
            join destinationStop in dbContext.RouteStops.AsNoTracking() on destinationCall.RouteStopId equals destinationStop.RouteStopId
            join destinationStation in dbContext.Stations.AsNoTracking() on destinationStop.StationId equals destinationStation.StationId
            where service.IsPublished && route.IsPublished && schedule.IsPublished && schedule.SupersededAt == null
                && originStop.RouteId == route.RouteId && destinationStop.RouteId == route.RouteId
                && originStation.IsPublished && destinationStation.IsPublished
                && originStation.StationId == query.OriginStationId
                && destinationStation.StationId == query.DestinationStationId
                && originCall.StopOrder < destinationCall.StopOrder
            select new { Service = service, Route = route, Schedule = schedule, OriginCall = originCall,
                OriginStop = originStop, OriginStation = originStation, DestinationCall = destinationCall,
                DestinationStop = destinationStop, DestinationStation = destinationStation }
        ).ToListAsync(cancellationToken);

        var results = new List<ServiceChoiceView>();
        var operationDates = candidates.GroupBy(candidate => candidate.Schedule.ScheduleId).ToDictionary(group => group.Key, group =>
        {
            var candidate = group.First();
            return
            candidate.OriginCall.DepartureOffsetMinutes is { } offset
                ? ServiceSearchEligibility.CalculateOperationDate(query.TravelDate, candidate.Schedule.FirstDepartureLocalTime, offset)
                : (DateOnly?)null;
        });
        var requestedOperationDates = operationDates.Values.Where(item => item.HasValue).Select(item => item!.Value).Distinct().ToArray();
        var scheduleIds = operationDates.Keys.ToArray();
        var calendars = await dbContext.ServiceScheduleCalendars.AsNoTracking()
            .Where(item => scheduleIds.Contains(item.ScheduleId)).ToDictionaryAsync(item => item.ScheduleId, cancellationToken);
        var exceptions = await dbContext.ServiceScheduleDateExceptions.AsNoTracking()
            .Where(item => scheduleIds.Contains(item.ScheduleId) && requestedOperationDates.Contains(item.Date))
            .ToDictionaryAsync(item => (item.ScheduleId, item.Date), cancellationToken);
        var manifests = await dbContext.ServiceDateManifests.AsNoTracking()
            .Where(item => requestedOperationDates.Contains(item.OperationDate))
            .ToDictionaryAsync(item => item.OperationDate, cancellationToken);
        var operationRows = await dbContext.ServiceOperationDates.AsNoTracking()
            .Where(item => scheduleIds.Contains(item.ScheduleId) && requestedOperationDates.Contains(item.OperationDate))
            .ToListAsync(cancellationToken);
        var operations = operationRows.ToDictionary(item => (item.ScheduleId, item.OperationDate));
        foreach (var candidate in candidates)
        {
            if (candidate.OriginCall.DepartureOffsetMinutes is not int originOffset ||
                candidate.DestinationCall.ArrivalOffsetMinutes is not int destinationOffset ||
                !operationDates[candidate.Schedule.ScheduleId].HasValue)
                return new(false, []);

            var operationDate = operationDates[candidate.Schedule.ScheduleId]!.Value;
            if (calendars.TryGetValue(candidate.Schedule.ScheduleId, out var calendar))
            {
                var runs = exceptions.TryGetValue((candidate.Schedule.ScheduleId, operationDate), out var exception)
                    ? exception.Runs : calendar.RunsOn(operationDate);
                if (!runs) continue;
                results.Add(new(candidate.Service.ServiceId, candidate.Service.ServiceCode, candidate.Route.RouteId,
                    candidate.Route.RouteCode, candidate.Schedule.Revision,
                    ToScheduledCall(candidate.OriginStation.StationId, candidate.OriginStation.Name,
                        candidate.OriginCall.StopOrder, operationDate, candidate.Schedule.FirstDepartureLocalTime, originOffset),
                    ToScheduledCall(candidate.DestinationStation.StationId, candidate.DestinationStation.Name,
                        candidate.DestinationCall.StopOrder, operationDate, candidate.Schedule.FirstDepartureLocalTime, destinationOffset),
                    candidate.Service.DisplayName, candidate.Service.PublicServiceNumber));
                continue;
            }
            if (!manifests.TryGetValue(operationDate, out var dateManifest) || !dateManifest.IsComplete)
                return new(false, []);
            operations.TryGetValue((candidate.Schedule.ScheduleId, operationDate), out var operation);
            var eligibility = ServiceSearchEligibility.Evaluate(query.TravelDate, candidate.Schedule, dateManifest,
                operation, candidate.OriginCall, candidate.OriginStop, candidate.DestinationCall, candidate.DestinationStop);
            if (eligibility.Status == ServiceSearchEligibilityStatus.Unavailable) return new(false, []);
            if (!eligibility.IsEligible) continue;

            results.Add(new(candidate.Service.ServiceId, candidate.Service.ServiceCode, candidate.Route.RouteId,
                candidate.Route.RouteCode, candidate.Schedule.Revision,
                ToScheduledCall(candidate.OriginStation.StationId, candidate.OriginStation.Name,
                    candidate.OriginCall.StopOrder, operationDate, candidate.Schedule.FirstDepartureLocalTime,
                    originOffset),
                ToScheduledCall(candidate.DestinationStation.StationId, candidate.DestinationStation.Name,
                    candidate.DestinationCall.StopOrder, operationDate, candidate.Schedule.FirstDepartureLocalTime,
                    destinationOffset), candidate.Service.DisplayName, candidate.Service.PublicServiceNumber));
        }

        if (candidates.Count == 0)
        {
            if (await dbContext.ServiceScheduleCalendars.AsNoTracking().AnyAsync(item =>
                item.ValidFrom <= query.TravelDate && item.ValidThrough >= query.TravelDate, cancellationToken))
                return new(true, []);
            var requestedDateCovered = await dbContext.ServiceDateManifests.AsNoTracking()
                .AnyAsync(item => item.OperationDate == query.TravelDate && item.IsComplete, cancellationToken);
            if (!requestedDateCovered) return new(false, []);
        }
        return new(true, results.OrderBy(item => item.Origin.ScheduledAt).ThenBy(item => item.ServiceCode, StringComparer.Ordinal).ToArray());
    }

    private static ScheduledCallView ToScheduledCall(Guid stationId, string name, int order, DateOnly operationDate,
        TimeOnly firstDeparture, int offsetMinutes)
    {
        var local = operationDate.ToDateTime(firstDeparture).AddMinutes(offsetMinutes);
        return new(stationId, name, order, new DateTimeOffset(local, TimeSpan.FromHours(5.5)));
    }
}
