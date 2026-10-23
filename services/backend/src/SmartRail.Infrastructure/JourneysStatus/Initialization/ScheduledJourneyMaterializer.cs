using Microsoft.EntityFrameworkCore;
using SmartRail.Application.JourneysStatus.Initialization;
using SmartRail.Application.JourneysStatus.Persistence;
using SmartRail.Domain.JourneysStatus;
using SmartRail.Domain.JourneysStatus.Entities;
using SmartRail.Domain.Incidents;
using SmartRail.Infrastructure.RailwayNetwork;

namespace SmartRail.Infrastructure.JourneysStatus.Initialization;

/// <summary>Creates one dated journey for each published timetable slot that runs on the requested day.</summary>
public sealed class ScheduledJourneyMaterializer(
    SmartRailDbContext db,
    IJourneyScheduleLinkVerifier verifier,
    IJourneyInitializationRepository repository,
    TimeProvider clock)
{
    private const string SourceSystem = "SmartRailCalendar";
    private const string SystemActor = "system:published-timetable";

    public async Task<JourneyPreparationResult> PrepareAsync(DateOnly date, Guid? stationId = null,
        CancellationToken cancellationToken = default)
    {
        var candidates = await (
            from schedule in db.ServiceScheduleRevisions.AsNoTracking()
            join calendar in db.ServiceScheduleCalendars.AsNoTracking() on schedule.ScheduleId equals calendar.ScheduleId
            join service in db.TrainServices.AsNoTracking() on schedule.ServiceId equals service.ServiceId
            join route in db.Routes.AsNoTracking() on service.RouteId equals route.RouteId
            where schedule.IsPublished && schedule.SupersededAt == null && service.IsPublished && route.IsPublished &&
                  calendar.ValidFrom <= date && calendar.ValidThrough >= date
            select new { Schedule = schedule, Calendar = calendar }
        ).ToArrayAsync(cancellationToken);
        if (stationId.HasValue)
        {
            var stationSchedules = await (from call in db.ServiceCallingPoints.AsNoTracking()
                                          join stop in db.RouteStops.AsNoTracking() on call.RouteStopId equals stop.RouteStopId
                                          where stop.StationId == stationId.Value
                                          select call.ScheduleId).Distinct().ToArrayAsync(cancellationToken);
            var allowed = stationSchedules.ToHashSet();
            candidates = candidates.Where(item => allowed.Contains(item.Schedule.ScheduleId)).ToArray();
        }
        var ids = candidates.Select(item => item.Schedule.ScheduleId).ToArray();
        var exceptions = await db.ServiceScheduleDateExceptions.AsNoTracking()
            .Where(item => ids.Contains(item.ScheduleId) && item.Date == date)
            .ToDictionaryAsync(item => item.ScheduleId, cancellationToken);

        var created = 0;
        var replanned = 0;
        foreach (var candidate in candidates)
        {
            var schedule = candidate.Schedule;
            var runs = exceptions.TryGetValue(schedule.ScheduleId, out var exception)
                ? exception.Runs : candidate.Calendar.RunsOn(date);
            if (!runs) continue;

            // Slot + date identifies the trip even when the timetable gets a new revision.
            var sourceKey = $"{schedule.SlotId:N}:{date:yyyy-MM-dd}";
            var existing = await db.Journeys.AsNoTracking().SingleOrDefaultAsync(item =>
                item.SourceSystem == SourceSystem && item.SourceInstanceKey == sourceKey, cancellationToken);
            if (existing?.ScheduleId == schedule.ScheduleId)
                continue;
            // Respect dated journeys already supplied by an earlier source for this slot.
            if (existing is null && await (from existingJourney in db.Journeys.AsNoTracking()
                       join linked in db.ServiceScheduleRevisions.AsNoTracking() on existingJourney.ScheduleId equals linked.ScheduleId
                       where existingJourney.OperationDate == date && existingJourney.ServiceId == schedule.ServiceId && linked.SlotId == schedule.SlotId
                       select existingJourney.JourneyInstanceId).AnyAsync(cancellationToken))
                continue;

            if (existing is not null && !await CanReplanAsync(existing, date, schedule.FirstDepartureLocalTime, cancellationToken))
                continue;

            var link = await verifier.VerifyAsync(new(schedule.ServiceId, schedule.ScheduleId, schedule.Revision,
                date, schedule.FirstDepartureLocalTime), cancellationToken);
            if (link.Status == ScheduleLinkVerificationStatus.Unavailable)
                throw new InvalidOperationException("Published schedule data is temporarily unavailable.");
            if (link.Status != ScheduleLinkVerificationStatus.Verified || link.Link is null)
                throw new InvalidOperationException("A published schedule cannot be used to create its dated journey.");

            if (existing is not null)
            {
                await ReplanAsync(existing.JourneyInstanceId, schedule.ScheduleId, schedule.Revision,
                    schedule.FirstDepartureLocalTime, link.Link.Stops, cancellationToken);
                replanned++;
                continue;
            }

            var journeyId = Guid.NewGuid();
            var now = clock.GetUtcNow();
            var provenance = "Automatically created from the published timetable calendar.";
            var journey = new Journey(journeyId, schedule.ServiceId, schedule.ScheduleId, schedule.Revision,
                date, schedule.FirstDepartureLocalTime, SourceSystem, sourceKey, now, SystemActor, provenance);
            var baseline = new JourneyRevision(journeyId, 1, JourneyStatus.Scheduled, now,
                JourneySource.ScheduledBaseline.ToString());
            var stops = link.Link.Stops.OrderBy(item => item.StopOrder)
                .Select(item => new JourneyRevisionStop(journeyId, 1, item.JourneyStopId, item.StopOrder,
                    item.StationId, item.RouteStopId, item.PlannedArrivalAt, item.PlannedDepartureAt)).ToArray();
            var audit = new JourneyInitializationAudit(Guid.NewGuid(), SourceSystem, sourceKey,
                JourneyInitializationDecision.Accepted, now, provenance, journeyId, SystemActor,
                schedule.ServiceId, schedule.ScheduleId, schedule.Revision, date, schedule.FirstDepartureLocalTime);
            var result = await repository.InitializeAsync(new(journey, baseline, stops, audit, false), cancellationToken);
            if (result.Outcome == JourneyInitializationStoreOutcome.Created) created++;
            else if (result.Outcome != JourneyInitializationStoreOutcome.Replayed)
                throw new InvalidOperationException("The dated journey conflicts with an existing journey identity.");
        }
        var currentScheduleIds = candidates.Where(item =>
            exceptions.TryGetValue(item.Schedule.ScheduleId, out var exception)
                ? exception.Runs : item.Calendar.RunsOn(date))
            .Select(item => item.Schedule.ScheduleId).ToHashSet();
        var automaticQuery = db.Journeys.AsNoTracking()
            .Where(item => item.SourceSystem == SourceSystem && item.OperationDate == date);
        if (stationId.HasValue)
            automaticQuery = automaticQuery.Where(journey => db.JourneyRevisionStops.Any(stop =>
                stop.JourneyInstanceId == journey.JourneyInstanceId && stop.Revision == journey.CurrentRevision &&
                stop.StationId == stationId.Value));
        var automaticJourneys = await automaticQuery
            .Select(item => new { item.ScheduleId, item.CurrentRevision }).ToArrayAsync(cancellationToken);
        var needsReview = automaticJourneys.Count(item => !currentScheduleIds.Contains(item.ScheduleId));
        return new(created, replanned, needsReview);
    }

    private async Task<bool> CanReplanAsync(Journey journey, DateOnly date, TimeOnly departure,
        CancellationToken cancellationToken)
    {
        if (journey.CurrentRevision != 1 ||
            new DateTimeOffset(date.ToDateTime(journey.FirstDepartureOccurrence), TimeSpan.FromHours(5.5)) <= clock.GetUtcNow() ||
            new DateTimeOffset(date.ToDateTime(departure), TimeSpan.FromHours(5.5)) <= clock.GetUtcNow())
            return false;
        if (await db.DriverJourneyAssignments.AsNoTracking().AnyAsync(item =>
                item.JourneyInstanceId == journey.JourneyInstanceId, cancellationToken)) return false;
        return !await db.IncidentAssociations.AsNoTracking().AnyAsync(item =>
            item.Kind == IncidentAssociationKind.Journey && item.ResourceId == journey.JourneyInstanceId,
            cancellationToken);
    }

    private async Task ReplanAsync(Guid journeyId, Guid scheduleId, int revision, TimeOnly departure,
        IReadOnlyList<JourneyScheduleStop> calls, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var journey = await db.Journeys.SingleAsync(item => item.JourneyInstanceId == journeyId, cancellationToken);
        if (journey.CurrentRevision != 1) throw new InvalidOperationException("The journey changed during timetable preparation.");
        var oldStops = await db.JourneyRevisionStops.Where(item =>
            item.JourneyInstanceId == journeyId && item.Revision == 1).ToArrayAsync(cancellationToken);
        var oldIds = oldStops.Where(item => item.RouteStopId.HasValue)
            .ToDictionary(item => item.RouteStopId!.Value, item => item.JourneyStopId);
        db.JourneyRevisionStops.RemoveRange(oldStops);
        await db.SaveChangesAsync(cancellationToken);
        journey.ReplaceUntouchedPlan(scheduleId, revision, departure);
        db.JourneyRevisionStops.AddRange(calls.Select(item => new JourneyRevisionStop(journeyId, 1,
            oldIds.GetValueOrDefault(item.RouteStopId, item.JourneyStopId), item.StopOrder, item.StationId, item.RouteStopId,
            item.PlannedArrivalAt, item.PlannedDepartureAt)));
        db.JourneyInitializationAudits.Add(new JourneyInitializationAudit(Guid.NewGuid(), SourceSystem,
            journey.SourceInstanceKey, JourneyInitializationDecision.Accepted, clock.GetUtcNow(),
            "Untouched future journey replanned from a newer published timetable revision.",
            journeyId, SystemActor, journey.ServiceId, scheduleId, revision, journey.OperationDate, departure));
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}

public sealed record JourneyPreparationResult(int Created, int Replanned, int NeedsReview);
