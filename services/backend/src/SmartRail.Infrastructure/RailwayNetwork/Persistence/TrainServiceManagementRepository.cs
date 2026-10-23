using Microsoft.EntityFrameworkCore;
using SmartRail.Application.RailwayNetwork.Services;
using SmartRail.Domain.RailwayNetwork.Entities;
using SmartRail.Domain.RailwayNetwork.Rules;

namespace SmartRail.Infrastructure.RailwayNetwork.Persistence;

public sealed class TrainServiceManagementRepository(SmartRailDbContext db) : ITrainServiceManagementRepository
{
    public async Task<IReadOnlyList<TrainServiceManagementView>> ListAsync(CancellationToken cancellationToken = default)
    {
        var rows = await (from service in db.TrainServices.AsNoTracking()
               join route in db.Routes.AsNoTracking() on service.RouteId equals route.RouteId
               orderby service.ServiceCode
               select new { Service = service, route.RouteCode, route.Name }).ToListAsync(cancellationToken);
        var serviceIds = rows.Select(item => item.Service.ServiceId).ToArray();
        var selectedStops = await db.TrainServiceRouteStops.AsNoTracking().Where(item => serviceIds.Contains(item.ServiceId))
            .OrderBy(item => item.StopOrder).ToListAsync(cancellationToken);
        return rows.Select(item => ToView(item.Service, item.RouteCode, item.Name,
            selectedStops.Where(stop => stop.ServiceId == item.Service.ServiceId).Select(stop => stop.RouteStopId).ToArray())).ToArray();
    }

    public async Task<TrainServiceManagementView?> CreateAsync(CreateTrainServiceRequest request, CancellationToken cancellationToken = default)
    {
        if (!await db.Routes.AnyAsync(route => route.RouteId == request.RouteId, cancellationToken)) return null;
        var routeStops = await db.RouteStops.Where(item => item.RouteId == request.RouteId).OrderBy(item => item.StopOrder).ToListAsync(cancellationToken);
        var selectedStops = ResolveServiceStops(routeStops, request.RouteStopIds);
        var service = new TrainService(Guid.NewGuid(), request.ServiceCode, request.RouteId,
            displayName: request.DisplayName, publicServiceNumber: request.PublicServiceNumber);
        db.TrainServices.Add(service);
        db.TrainServiceRouteStops.AddRange(selectedStops.Select((stop, index) => new TrainServiceRouteStop(service.ServiceId, stop.RouteStopId, index + 1)));
        await db.SaveChangesAsync(cancellationToken);
        var route = await db.Routes.AsNoTracking().SingleAsync(item => item.RouteId == service.RouteId, cancellationToken);
        return ToView(service, route.RouteCode, route.Name, selectedStops.Select(item => item.RouteStopId).ToArray());
    }

    public async Task<TrainServiceManagementView?> UpdateAsync(Guid serviceId, UpdateTrainServiceRequest request, CancellationToken cancellationToken = default)
    {
        var service = await db.TrainServices.SingleOrDefaultAsync(item => item.ServiceId == serviceId, cancellationToken);
        if (service is null) return null;
        if (!await db.Routes.AnyAsync(route => route.RouteId == request.RouteId, cancellationToken)) throw new KeyNotFoundException("The selected route does not exist.");
        if (service.RouteId != request.RouteId && await db.ServiceScheduleRevisions.AnyAsync(item =>
            item.ServiceId == serviceId && item.IsPublished && item.SupersededAt == null, cancellationToken))
            throw new InvalidOperationException("A service with a published timetable cannot change routes.");
        var routeStops = await db.RouteStops.Where(item => item.RouteId == request.RouteId).OrderBy(item => item.StopOrder).ToListAsync(cancellationToken);
        var existingStops = await db.TrainServiceRouteStops.Where(item => item.ServiceId == serviceId).ToListAsync(cancellationToken);
        var requestedIds = request.RouteStopIds ?? (service.RouteId == request.RouteId && existingStops.Count > 0
            ? existingStops.OrderBy(item => item.StopOrder).Select(item => item.RouteStopId).ToArray()
            : null);
        var selectedStops = ResolveServiceStops(routeStops, requestedIds);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        service.UpdateDetails(request.ServiceCode, request.RouteId, request.DisplayName, request.PublicServiceNumber);
        db.TrainServiceRouteStops.RemoveRange(existingStops);
        await db.SaveChangesAsync(cancellationToken);
        db.TrainServiceRouteStops.AddRange(selectedStops.Select((stop, index) => new TrainServiceRouteStop(serviceId, stop.RouteStopId, index + 1)));
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        var route = await db.Routes.AsNoTracking().SingleAsync(item => item.RouteId == service.RouteId, cancellationToken);
        return ToView(service, route.RouteCode, route.Name, selectedStops.Select(item => item.RouteStopId).ToArray());
    }

    public async Task<bool> DeleteAsync(Guid serviceId, CancellationToken cancellationToken = default)
    {
        var service = await db.TrainServices.SingleOrDefaultAsync(item => item.ServiceId == serviceId, cancellationToken);
        if (service is null) return false;
        db.TrainServices.Remove(service);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<ScheduleRevisionView>?> GetSchedulesAsync(Guid serviceId, CancellationToken cancellationToken = default)
    {
        if (!await db.TrainServices.AnyAsync(item => item.ServiceId == serviceId, cancellationToken)) return null;
        var schedules = await db.ServiceScheduleRevisions.AsNoTracking().Where(item => item.ServiceId == serviceId)
            .OrderByDescending(item => item.Revision).ToListAsync(cancellationToken);
        var ids = schedules.Select(item => item.ScheduleId).ToArray();
        var calendars = await db.ServiceScheduleCalendars.AsNoTracking().Where(item => ids.Contains(item.ScheduleId))
            .ToDictionaryAsync(item => item.ScheduleId, cancellationToken);
        var exceptions = await db.ServiceScheduleDateExceptions.AsNoTracking().Where(item => ids.Contains(item.ScheduleId))
            .ToListAsync(cancellationToken);
        var calls = await (from call in db.ServiceCallingPoints.AsNoTracking()
                           join stop in db.RouteStops.AsNoTracking() on call.RouteStopId equals stop.RouteStopId
                           join station in db.Stations.AsNoTracking() on stop.StationId equals station.StationId
                           where ids.Contains(call.ScheduleId)
                           orderby call.StopOrder
                           select new { call.ScheduleId, View = new ScheduleCallingPointView(stop.RouteStopId, station.StationId,
                               station.Name, call.StopOrder, call.ArrivalOffsetMinutes, call.DepartureOffsetMinutes) })
            .ToListAsync(cancellationToken);
        var operations = await db.ServiceOperationDates.AsNoTracking().Where(item => ids.Contains(item.ScheduleId)).ToListAsync(cancellationToken);
        var dates = operations.Select(item => item.OperationDate).Distinct().ToArray();
        var manifests = await db.ServiceDateManifests.AsNoTracking().Where(item => dates.Contains(item.OperationDate)).ToDictionaryAsync(item => item.OperationDate, cancellationToken);
        return schedules.Select(schedule => new ScheduleRevisionView(schedule.ScheduleId, schedule.Revision,
            schedule.FirstDepartureLocalTime, schedule.IsPublished, schedule.PublishedAt, schedule.SupersededAt,
            calls.Where(item => item.ScheduleId == schedule.ScheduleId).Select(item => item.View).ToArray(),
            operations.Where(item => item.ScheduleId == schedule.ScheduleId).Select(item =>
            {
                manifests.TryGetValue(item.OperationDate, out var manifest);
                return new ScheduleOperationDateView(item.OperationDate, manifest?.IsComplete ?? false, manifest?.SourceRevision);
            }).OrderBy(item => item.OperationDate).ToArray(),
            calendars.TryGetValue(schedule.ScheduleId, out var calendar)
                ? new ScheduleCalendarInput(calendar.ValidFrom, calendar.ValidThrough, calendar.WeekdaysMask,
                    exceptions.Where(item => item.ScheduleId == schedule.ScheduleId).OrderBy(item => item.Date)
                        .Select(item => new ScheduleDateExceptionInput(item.Date, item.Runs)).ToArray())
                : null)).ToArray();
    }

    public async Task<ScheduleRevisionView> CreateScheduleAsync(Guid serviceId, ScheduleRevisionInput request, CancellationToken cancellationToken = default)
    {
        var service = await db.TrainServices.SingleOrDefaultAsync(item => item.ServiceId == serviceId, cancellationToken)
            ?? throw new KeyNotFoundException("Train service not found.");
        var revisions = await db.ServiceScheduleRevisions.Where(item => item.ServiceId == serviceId).ToListAsync(cancellationToken);
        var revision = revisions.Count == 0 ? 1 : checked(revisions.Max(item => item.Revision) + 1);
        var schedule = new ServiceScheduleRevision(Guid.NewGuid(), service.ServiceId, revision,
            request.FirstDepartureLocalTime, DateTimeOffset.UtcNow);
        var calls = await BuildCallingPoints(service.RouteId, serviceId, schedule.ScheduleId, request.CallingPoints,
            await db.RouteStops.Where(item => item.RouteId == service.RouteId).ToListAsync(cancellationToken),
            cancellationToken);
        db.ServiceScheduleRevisions.Add(schedule);
        db.ServiceCallingPoints.AddRange(calls);
        AddCalendar(schedule.ScheduleId, request.Calendar);
        await db.SaveChangesAsync(cancellationToken);
        return (await GetSchedulesAsync(serviceId, cancellationToken))!.Single(item => item.ScheduleId == schedule.ScheduleId);
    }

    public async Task<ScheduleRevisionView?> UpdateScheduleAsync(Guid serviceId, Guid scheduleId, ScheduleRevisionInput request, CancellationToken cancellationToken = default)
    {
        var service = await db.TrainServices.SingleOrDefaultAsync(item => item.ServiceId == serviceId, cancellationToken);
        if (service is null) return null;
        var schedule = await db.ServiceScheduleRevisions.SingleOrDefaultAsync(item => item.ServiceId == serviceId && item.ScheduleId == scheduleId, cancellationToken);
        if (schedule is null) return null;
        if (schedule.IsPublished) throw new InvalidOperationException("Published schedule revisions are immutable. Create a new revision to make changes.");
        var calls = await BuildCallingPoints(service.RouteId, serviceId, scheduleId, request.CallingPoints,
            await db.RouteStops.Where(item => item.RouteId == service.RouteId).ToListAsync(cancellationToken), cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        schedule.ChangeDraftDepartureTime(request.FirstDepartureLocalTime);
        var existing = await db.ServiceCallingPoints.Where(item => item.ScheduleId == scheduleId).ToListAsync(cancellationToken);
        db.ServiceCallingPoints.RemoveRange(existing);
        var existingCalendar = await db.ServiceScheduleCalendars.SingleOrDefaultAsync(item => item.ScheduleId == scheduleId, cancellationToken);
        if (existingCalendar is not null) db.ServiceScheduleCalendars.Remove(existingCalendar);
        db.ServiceScheduleDateExceptions.RemoveRange(db.ServiceScheduleDateExceptions.Where(item => item.ScheduleId == scheduleId));
        await db.SaveChangesAsync(cancellationToken);
        db.ServiceCallingPoints.AddRange(calls);
        AddCalendar(scheduleId, request.Calendar);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return (await GetSchedulesAsync(serviceId, cancellationToken))!.Single(item => item.ScheduleId == scheduleId);
    }

    public async Task<bool> DeleteScheduleAsync(Guid serviceId, Guid scheduleId, CancellationToken cancellationToken = default)
    {
        var schedule = await db.ServiceScheduleRevisions.SingleOrDefaultAsync(item => item.ServiceId == serviceId && item.ScheduleId == scheduleId, cancellationToken);
        if (schedule is null) return false;
        if (schedule.IsPublished) throw new InvalidOperationException("Published schedule revisions cannot be deleted.");
        if (await db.ServiceOperationDates.AnyAsync(item => item.ScheduleId == scheduleId, cancellationToken))
            throw new InvalidOperationException("A schedule with assigned operating dates cannot be deleted.");
        db.ServiceCallingPoints.RemoveRange(db.ServiceCallingPoints.Where(item => item.ScheduleId == scheduleId));
        db.ServiceScheduleCalendars.RemoveRange(db.ServiceScheduleCalendars.Where(item => item.ScheduleId == scheduleId));
        db.ServiceScheduleDateExceptions.RemoveRange(db.ServiceScheduleDateExceptions.Where(item => item.ScheduleId == scheduleId));
        db.ServiceScheduleRevisions.Remove(schedule);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task PublishScheduleAsync(Guid serviceId, Guid scheduleId, DateTimeOffset publishedAt, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var service = await db.TrainServices.SingleOrDefaultAsync(item => item.ServiceId == serviceId, cancellationToken)
            ?? throw new KeyNotFoundException("Train service not found.");
        var route = await db.Routes.SingleOrDefaultAsync(item => item.RouteId == service.RouteId, cancellationToken)
            ?? throw new KeyNotFoundException("The service route does not exist.");
        var revisions = await db.ServiceScheduleRevisions.Where(item => item.ServiceId == serviceId).ToListAsync(cancellationToken);
        var candidate = revisions.SingleOrDefault(item => item.ScheduleId == scheduleId)
            ?? throw new KeyNotFoundException("Schedule revision not found.");
        if (candidate.IsCurrentPublished) throw new InvalidOperationException("The schedule revision is already current and published.");
        var calendar = await db.ServiceScheduleCalendars.AsNoTracking().SingleOrDefaultAsync(item => item.ScheduleId == scheduleId, cancellationToken);
        if (calendar is null) throw new InvalidOperationException("An operating calendar is required before publication.");
        if (revisions.Any(item => item.IsCurrentPublished) && calendar.ValidFrom > DateOnly.FromDateTime(DateTime.UtcNow.AddHours(5.5)))
            throw new InvalidOperationException("A replacement timetable can be published when its validity period starts.");
        var selectedIds = await db.TrainServiceRouteStops.AsNoTracking().Where(item => item.ServiceId == serviceId)
            .OrderBy(item => item.StopOrder).Select(item => item.RouteStopId).ToArrayAsync(cancellationToken);
        var candidateCalls = await db.ServiceCallingPoints.AsNoTracking().Where(item => item.ScheduleId == scheduleId)
            .OrderBy(item => item.StopOrder).ToArrayAsync(cancellationToken);
        if (!candidateCalls.Select(item => item.RouteStopId).SequenceEqual(selectedIds))
            throw new InvalidOperationException("Timetable calls must match the service's selected stations in travel order.");
        if (candidateCalls.Length < 2 || candidateCalls[0].DepartureOffsetMinutes != 0 ||
            candidateCalls[^1].ArrivalOffsetMinutes is null ||
            candidateCalls.Skip(1).SkipLast(1).Any(item => item.ArrivalOffsetMinutes is null || item.DepartureOffsetMinutes is null))
            throw new InvalidOperationException("Every station needs its required arrival and departure times.");
        foreach (var current in revisions.Where(item => item.IsCurrentPublished)) current.Supersede(publishedAt);
        await db.SaveChangesAsync(cancellationToken);
        candidate.Publish(publishedAt);
        route.Publish();
        service.Publish();
        var stops = await db.RouteStops.Where(item => item.RouteId == route.RouteId).ToListAsync(cancellationToken);
        var calls = await db.ServiceCallingPoints.Where(item => revisions.Select(revision => revision.ScheduleId).Contains(item.ScheduleId)).ToListAsync(cancellationToken);
        var validation = SchedulePublicationValidator.Validate(service, route, stops, revisions, calls);
        if (!validation.IsValid) throw new SchedulePublicationRejectedException(validation.Errors);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ScheduleOperationDateView>?> SetOperationDatesAsync(Guid serviceId, Guid scheduleId,
        IReadOnlyList<DateOnly> dates, CancellationToken cancellationToken = default)
    {
        var schedule = await db.ServiceScheduleRevisions.SingleOrDefaultAsync(item => item.ServiceId == serviceId && item.ScheduleId == scheduleId, cancellationToken);
        if (schedule is null) return null;
        if (dates.Distinct().Count() != dates.Count) throw new ArgumentException("Operation dates must be unique.", nameof(dates));
        if (dates.Any(date => date < DateOnly.FromDateTime(schedule.CreatedAt.UtcDateTime)))
            throw new ArgumentException("Operation dates cannot precede schedule creation.", nameof(dates));
        var manifests = await db.ServiceDateManifests.Where(item => dates.Contains(item.OperationDate)).ToListAsync(cancellationToken);
        if (manifests.Count != dates.Count || manifests.Any(item => !item.IsComplete))
            throw new InvalidOperationException("Every operating date must have a complete service-date manifest before assignment.");
        var existing = await db.ServiceOperationDates.Where(item => item.ScheduleId == scheduleId).ToListAsync(cancellationToken);
        db.ServiceOperationDates.RemoveRange(existing);
        db.ServiceOperationDates.AddRange(dates.Select(date => new ServiceOperationDate(scheduleId, date)));
        await db.SaveChangesAsync(cancellationToken);
        return dates.Order().Select(date =>
        {
            var manifest = manifests.Single(item => item.OperationDate == date);
            return new ScheduleOperationDateView(date, manifest.IsComplete, manifest.SourceRevision);
        }).ToArray();
    }

    public async Task<ServiceDateManifestView?> GetManifestAsync(DateOnly operationDate, CancellationToken cancellationToken = default)
    {
        var manifest = await db.ServiceDateManifests.AsNoTracking().SingleOrDefaultAsync(item => item.OperationDate == operationDate, cancellationToken);
        if (manifest is null) return null;
        var scheduleIds = await db.ServiceOperationDates.AsNoTracking().Where(item => item.OperationDate == operationDate)
            .OrderBy(item => item.ScheduleId).Select(item => item.ScheduleId).ToListAsync(cancellationToken);
        return ToManifestView(manifest, scheduleIds);
    }

    public async Task<ServiceDateManifestView> SetManifestAsync(ServiceDateManifest manifest, IReadOnlyList<Guid> scheduleIds,
        CancellationToken cancellationToken = default)
    {
        if (scheduleIds.Distinct().Count() != scheduleIds.Count)
            throw new ArgumentException("Schedule IDs must be unique.", nameof(scheduleIds));
        if (!manifest.IsComplete && scheduleIds.Count != 0)
            throw new ArgumentException("An incomplete date manifest cannot assert operating services.", nameof(scheduleIds));
        var currentScheduleIds = await db.ServiceScheduleRevisions
            .Where(item => item.IsPublished && item.SupersededAt == null)
            .Select(item => item.ScheduleId).ToListAsync(cancellationToken);
        if (scheduleIds.Any(id => !currentScheduleIds.Contains(id)))
            throw new ArgumentException("Operating dates may reference only current published schedule revisions.", nameof(scheduleIds));

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var oldOperations = await db.ServiceOperationDates.Where(item => item.OperationDate == manifest.OperationDate).ToListAsync(cancellationToken);
        db.ServiceOperationDates.RemoveRange(oldOperations);
        var oldManifest = await db.ServiceDateManifests.FindAsync([manifest.OperationDate], cancellationToken);
        if (oldManifest is not null) db.ServiceDateManifests.Remove(oldManifest);
        await db.SaveChangesAsync(cancellationToken);
        db.ServiceDateManifests.Add(manifest);
        db.ServiceOperationDates.AddRange(scheduleIds.Select(id => new ServiceOperationDate(id, manifest.OperationDate)));
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ToManifestView(manifest, scheduleIds);
    }

    private async Task<List<ServiceCallingPoint>> BuildCallingPoints(Guid routeId, Guid serviceId, Guid scheduleId,
        IReadOnlyList<CallingPointInput> inputs, IReadOnlyCollection<RouteStop> routeStops, CancellationToken cancellationToken)
    {
        if (inputs is null || inputs.Count < 2) throw new ArgumentException("A schedule requires at least two calling points.", nameof(inputs));
        if (inputs.Select(item => item.RouteStopId).Distinct().Count() != inputs.Count)
            throw new ArgumentException("A route stop may appear only once in a schedule.", nameof(inputs));
        var byId = routeStops.ToDictionary(item => item.RouteStopId);
        var serviceStopIds = await db.TrainServiceRouteStops.AsNoTracking().Where(item => item.ServiceId == serviceId)
            .OrderBy(item => item.StopOrder).Select(item => item.RouteStopId).ToArrayAsync(cancellationToken);
        if (!inputs.Select(item => item.RouteStopId).SequenceEqual(serviceStopIds))
            throw new ArgumentException("Schedule calls must exactly match this train service's selected stations and travel order.", nameof(inputs));
        var calls = new List<ServiceCallingPoint>(inputs.Count);
        var previousOffset = -1;
        for (var index = 0; index < inputs.Count; index++)
        {
            var input = inputs[index];
            if (!byId.TryGetValue(input.RouteStopId, out var stop))
                throw new ArgumentException("Every calling point must reference a stop on the service route.", nameof(inputs));
            foreach (var offset in new[] { input.ArrivalOffsetMinutes, input.DepartureOffsetMinutes }.Where(value => value.HasValue).Select(value => value!.Value))
            {
                if (offset < previousOffset) throw new ArgumentException("Scheduled offsets must not move backwards along the route.", nameof(inputs));
                previousOffset = offset;
            }
            calls.Add(new ServiceCallingPoint(scheduleId, input.RouteStopId, input.ArrivalOffsetMinutes, input.DepartureOffsetMinutes, index + 1));
        }
        return calls;
    }

    private void AddCalendar(Guid scheduleId, ScheduleCalendarInput? input)
    {
        if (input is null) return;
        if (input.Exceptions is null || input.Exceptions.Select(item => item.Date).Distinct().Count() != input.Exceptions.Count)
            throw new ArgumentException("Calendar exception dates must be unique.", nameof(input));
        var calendar = new ServiceScheduleCalendar(scheduleId, input.ValidFrom, input.ValidThrough, input.WeekdaysMask);
        if (input.Exceptions.Any(item => item.Date < calendar.ValidFrom || item.Date > calendar.ValidThrough))
            throw new ArgumentException("Calendar exceptions must fall within the validity dates.", nameof(input));
        db.ServiceScheduleCalendars.Add(calendar);
        db.ServiceScheduleDateExceptions.AddRange(input.Exceptions.Select(item => new ServiceScheduleDateException(scheduleId, item.Date, item.Runs)));
    }

    private static IReadOnlyList<RouteStop> ResolveServiceStops(IReadOnlyList<RouteStop> routeStops, IReadOnlyList<Guid>? requestedIds)
    {
        if (routeStops.Count < 2) throw new ArgumentException("The selected route must contain at least two stations.");
        var ids = requestedIds ?? routeStops.Select(item => item.RouteStopId).ToArray();
        if (ids.Count < 2) throw new ArgumentException("A train service must include a start station and an end station.");
        if (ids.Distinct().Count() != ids.Count) throw new ArgumentException("A station may appear only once in a train service.");
        var byId = routeStops.Select((stop, index) => (stop, index)).ToDictionary(item => item.stop.RouteStopId);
        var result = new List<RouteStop>(ids.Count);
        var previousIndex = -1;
        var direction = 0;
        foreach (var id in ids)
        {
            if (!byId.TryGetValue(id, out var item)) throw new ArgumentException("Every service station must be on the selected route.");
            if (previousIndex >= 0)
            {
                var step = Math.Sign(item.index - previousIndex);
                if (step == 0 || direction != 0 && step != direction)
                    throw new ArgumentException("Service stations must follow the route in one travel direction.");
                direction = step;
            }
            result.Add(item.stop);
            previousIndex = item.index;
        }
        return result;
    }

    private static TrainServiceManagementView ToView(TrainService service, string routeCode, string routeName, IReadOnlyList<Guid> routeStopIds) =>
        new(service.ServiceId, service.ServiceCode, service.DisplayName, service.PublicServiceNumber,
            service.RouteId, routeCode, routeName, service.IsPublished, routeStopIds);

    private static ServiceDateManifestView ToManifestView(ServiceDateManifest manifest, IReadOnlyList<Guid> scheduleIds) =>
        new(manifest.OperationDate, manifest.SourceRevision, manifest.FixtureVersion, manifest.IsComplete,
            manifest.PublishedAt, manifest.PublishedBy, manifest.ContentHash, scheduleIds);
}
