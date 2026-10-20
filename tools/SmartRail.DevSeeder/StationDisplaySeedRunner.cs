using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartRail.Application.JourneysStatus.Initialization;
using SmartRail.Application.RailwayNetwork.Services;
using SmartRail.Domain.RailwayNetwork.Entities;
using SmartRail.Infrastructure.RailwayNetwork;

namespace SmartRail.DevSeeder;

internal sealed record StationDisplaySeedResult(string Environment, string OperationDate, string DataKind,
    Guid ServiceId, Guid JourneyId, Guid ColomboFortStationId, Guid GalleStationId, Guid ColomboFortStopId, Guid GalleStopId,
    string ColomboFortDeparture, string GalleDeparture);

internal sealed class StationDisplaySeedRunner(IServiceProvider services)
{
    public async Task<StationDisplaySeedResult> RunAsync()
    {
        await using var scope = services.CreateAsyncScope();
        var provider = scope.ServiceProvider;
        var db = provider.GetRequiredService<SmartRailDbContext>();
        if ((await db.Database.GetPendingMigrationsAsync()).Any())
            throw new SeedRefusalException("Apply reviewed migrations separately before running the T030 seed.");

        var stations = await db.Stations.AsNoTracking()
            .Where(x => StationDisplaySeedManifest.StationIds.Contains(x.StationId))
            .ToDictionaryAsync(x => x.StationId);
        if (stations.Count != StationDisplaySeedManifest.StationIds.Length || stations.Values.Any(x => !x.IsPublished))
            throw new SeedRefusalException("Published Colombo Fort, Galle, and the existing development Matara sample are required.");
        if (stations[StationDisplaySeedManifest.ColomboFortStationId].Name != "Colombo Fort" ||
            stations[StationDisplaySeedManifest.GalleStationId].Name != "Galle" ||
            stations[StationDisplaySeedManifest.MataraStationId].Name != "Matara (development sample)")
            throw new SeedRefusalException("A reserved station identity differs from the approved T030 seed manifest.");

        await PreflightAsync(db);
        await EnsureRailwayGraphAsync(db);
        await EnsureSchedulePublicationAsync(provider, db);
        var initialized = await provider.GetRequiredService<InitializeJourneyHandler>().HandleAsync(new(
            StationDisplaySeedManifest.JourneyId,
            StationDisplaySeedManifest.ServiceId,
            StationDisplaySeedManifest.ScheduleId,
            1,
            StationDisplaySeedManifest.OperationDate,
            StationDisplaySeedManifest.ColomboDeparture,
            StationDisplaySeedManifest.SourceSystem,
            StationDisplaySeedManifest.SourceInstanceKey,
            StationDisplaySeedManifest.Provenance));
        if (initialized.Outcome is not (JourneyInitializationOutcome.Created or JourneyInitializationOutcome.Replayed) ||
            initialized.JourneyInstanceId != StationDisplaySeedManifest.JourneyId)
            throw new SeedRefusalException("Journey initialization did not complete or replay cleanly.");

        return new("Development", StationDisplaySeedManifest.OperationDate.ToString("yyyy-MM-dd"),
            "Synthetic local test timetable; not a published railway timetable", StationDisplaySeedManifest.ServiceId,
            StationDisplaySeedManifest.JourneyId, StationDisplaySeedManifest.ColomboFortStationId,
            StationDisplaySeedManifest.GalleStationId, StationDisplaySeedManifest.ColomboFortStopId,
            StationDisplaySeedManifest.GalleStopId, StationDisplaySeedManifest.ColomboDeparture.ToString("HH:mm"),
            StationDisplaySeedManifest.GalleDeparture.ToString("HH:mm"));
    }

    private static async Task PreflightAsync(SmartRailDbContext db)
    {
        var manifest = await db.ServiceDateManifests.AsNoTracking()
            .SingleOrDefaultAsync(x => x.OperationDate == StationDisplaySeedManifest.OperationDate);
        var operations = await db.ServiceOperationDates.AsNoTracking()
            .Where(x => x.OperationDate == StationDisplaySeedManifest.OperationDate)
            .Select(x => x.ScheduleId).ToArrayAsync();
        if (manifest is not null && (manifest.SourceRevision != StationDisplaySeedManifest.SourceSystem ||
            manifest.FixtureVersion != StationDisplaySeedManifest.SourceSystem || !manifest.IsComplete ||
            manifest.ContentHash != StationDisplaySeedManifest.ContentHash || manifest.PublishedBy != SeedManifest.Actor ||
            !operations.SequenceEqual([StationDisplaySeedManifest.ScheduleId])))
            throw new SeedRefusalException("The current date already has unrelated timetable evidence; no overwrite is permitted.");

        var route = await db.Routes.AsNoTracking().SingleOrDefaultAsync(x =>
            x.RouteId == StationDisplaySeedManifest.RouteId || x.RouteCode == StationDisplaySeedManifest.RouteCode);
        if (route is not null && (route.RouteId != StationDisplaySeedManifest.RouteId ||
            route.RouteCode != StationDisplaySeedManifest.RouteCode || route.Name != StationDisplaySeedManifest.RouteName || !route.IsPublished))
            throw new SeedRefusalException("The reserved T030 route identity is occupied by different data.");

        var service = await db.TrainServices.AsNoTracking().SingleOrDefaultAsync(x =>
            x.ServiceId == StationDisplaySeedManifest.ServiceId || x.ServiceCode == StationDisplaySeedManifest.ServiceCode);
        if (service is not null && (service.ServiceId != StationDisplaySeedManifest.ServiceId ||
            service.ServiceCode != StationDisplaySeedManifest.ServiceCode || service.RouteId != StationDisplaySeedManifest.RouteId))
            throw new SeedRefusalException("The reserved T030 service identity is occupied by different data.");

        var schedule = await db.ServiceScheduleRevisions.AsNoTracking()
            .SingleOrDefaultAsync(x => x.ScheduleId == StationDisplaySeedManifest.ScheduleId);
        if (schedule is not null && (schedule.ServiceId != StationDisplaySeedManifest.ServiceId || schedule.Revision != 1 ||
            schedule.FirstDepartureLocalTime != StationDisplaySeedManifest.ColomboDeparture))
            throw new SeedRefusalException("The reserved T030 schedule identity is occupied by different data.");

        var journey = await db.Journeys.AsNoTracking()
            .SingleOrDefaultAsync(x => x.JourneyInstanceId == StationDisplaySeedManifest.JourneyId);
        if (journey is not null && (journey.ServiceId != StationDisplaySeedManifest.ServiceId ||
            journey.ScheduleId != StationDisplaySeedManifest.ScheduleId || journey.OperationDate != StationDisplaySeedManifest.OperationDate ||
            journey.SourceSystem != StationDisplaySeedManifest.SourceSystem || journey.SourceInstanceKey != StationDisplaySeedManifest.SourceInstanceKey))
            throw new SeedRefusalException("The reserved T030 journey identity is occupied by different data.");
    }

    private static async Task EnsureRailwayGraphAsync(SmartRailDbContext db)
    {
        var existingRoute = await db.Routes.SingleOrDefaultAsync(x => x.RouteId == StationDisplaySeedManifest.RouteId);
        await using var transaction = await db.Database.BeginTransactionAsync();
        if (existingRoute is null)
            db.Routes.Add(new Route(StationDisplaySeedManifest.RouteId, StationDisplaySeedManifest.RouteCode,
                StationDisplaySeedManifest.RouteName, true));

        foreach (var stop in StationDisplaySeedManifest.RouteStops)
        {
            var existing = await db.RouteStops.SingleOrDefaultAsync(x => x.RouteStopId == stop.RouteStopId);
            if (existing is null) db.RouteStops.Add(new RouteStop(stop.RouteStopId, StationDisplaySeedManifest.RouteId,
                stop.StationId, stop.StopOrder));
            else if (existing.RouteId != StationDisplaySeedManifest.RouteId || existing.StationId != stop.StationId ||
                existing.StopOrder != stop.StopOrder)
                throw new SeedRefusalException("A reserved T030 route stop differs from the seed manifest.");
        }

        var service = await db.TrainServices.SingleOrDefaultAsync(x => x.ServiceId == StationDisplaySeedManifest.ServiceId);
        if (service is null)
            db.TrainServices.Add(new TrainService(StationDisplaySeedManifest.ServiceId, StationDisplaySeedManifest.ServiceCode,
                StationDisplaySeedManifest.RouteId));

        var schedule = await db.ServiceScheduleRevisions.SingleOrDefaultAsync(x => x.ScheduleId == StationDisplaySeedManifest.ScheduleId);
        if (schedule is null)
        {
            db.ServiceScheduleRevisions.Add(new ServiceScheduleRevision(StationDisplaySeedManifest.ScheduleId,
                StationDisplaySeedManifest.ServiceId, 1, StationDisplaySeedManifest.ColomboDeparture, SeedManifest.EvidenceTime));
            db.ServiceCallingPoints.AddRange(StationDisplaySeedManifest.CallingPoints.Select(x =>
                new ServiceCallingPoint(StationDisplaySeedManifest.ScheduleId, x.RouteStopId,
                    x.ArrivalOffsetMinutes, x.DepartureOffsetMinutes)));
        }
        else
        {
            var calls = await db.ServiceCallingPoints.AsNoTracking()
                .Where(x => x.ScheduleId == StationDisplaySeedManifest.ScheduleId).ToArrayAsync();
            if (calls.Length != StationDisplaySeedManifest.CallingPoints.Length ||
                StationDisplaySeedManifest.CallingPoints.Any(expected => !calls.Any(actual => actual.RouteStopId == expected.RouteStopId &&
                    actual.ArrivalOffsetMinutes == expected.ArrivalOffsetMinutes && actual.DepartureOffsetMinutes == expected.DepartureOffsetMinutes)))
                throw new SeedRefusalException("The reserved T030 calling points differ from the seed manifest.");
        }
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    private static async Task EnsureSchedulePublicationAsync(IServiceProvider provider, SmartRailDbContext db)
    {
        var schedule = await db.ServiceScheduleRevisions.AsNoTracking()
            .SingleAsync(x => x.ScheduleId == StationDisplaySeedManifest.ScheduleId);
        var manifest = await db.ServiceDateManifests.AsNoTracking()
            .SingleOrDefaultAsync(x => x.OperationDate == StationDisplaySeedManifest.OperationDate);
        if (schedule.IsCurrentPublished)
        {
            if (manifest is null) throw new SeedRefusalException("The reserved T030 schedule is published without its date evidence; no repair is permitted.");
            return;
        }
        if (manifest is not null)
            throw new SeedRefusalException("The T030 date evidence exists before schedule publication; no overwrite is permitted.");

        var date = new ServiceDateManifest(StationDisplaySeedManifest.OperationDate,
            StationDisplaySeedManifest.SourceSystem, StationDisplaySeedManifest.SourceSystem, true,
            DateTimeOffset.UtcNow, SeedManifest.Actor, StationDisplaySeedManifest.ContentHash);
        var publication = new TimetablePublicationRequest(StationDisplaySeedManifest.ServiceId,
            StationDisplaySeedManifest.ScheduleId, DateTimeOffset.UtcNow, SeedManifest.Actor,
            [new TimetableDatePublication(date, [new ServiceOperationDate(StationDisplaySeedManifest.ScheduleId,
                StationDisplaySeedManifest.OperationDate)])]);
        var result = await provider.GetRequiredService<PublishTimetableHandler>().HandleAsync(new(publication));
        if (!result.IsPublished) throw new SeedRefusalException("T030 timetable publication was refused by the backend validation workflow.");
    }
}

internal static class StationDisplaySeedManifest
{
    internal static readonly DateOnly OperationDate = new(2026, 10, 20);
    internal const string SourceSystem = "smartrail-station-display-t030-dev-v1";
    internal const string SourceInstanceKey = "station-display-t030-colombo-fort-galle-2026-10-20";
    internal const string Provenance = "Synthetic, local Development-only timetable for Station Display integration verification.";
    internal const string RouteCode = "DEV-T030-COAST-20261020";
    internal const string RouteName = "Development-only Station Display verification: Colombo Fort to Galle to Matara";
    internal const string ServiceCode = "DEV-T030-20261020";
    internal const int FamilyRoute = 2;
    internal const int FamilyService = 3;
    internal const int FamilySchedule = 4;
    internal const int FamilyJourney = 5;
    internal static Guid Id(int family, int number) => Guid.Parse($"d0040000-0000-0000-{family:0000}-{number:000000000000}");
    internal static readonly Guid RouteId = Id(FamilyRoute, 10);
    internal static readonly Guid ServiceId = Id(FamilyService, 10);
    internal static readonly Guid ScheduleId = Id(FamilySchedule, 10);
    internal static readonly Guid JourneyId = Id(FamilyJourney, 10);
    internal static readonly Guid ColomboFortStationId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    internal static readonly Guid GalleStationId = Guid.Parse("10000000-0000-0000-0000-000000000002");
    internal static readonly Guid MataraStationId = SeedManifest.Id(1, 2);
    internal static readonly Guid ColomboFortStopId = Id(FamilyRoute, 11);
    internal static readonly Guid GalleStopId = Id(FamilyRoute, 12);
    internal static readonly TimeOnly ColomboDeparture = new(15, 0);
    internal static readonly TimeOnly GalleDeparture = new(17, 5);
    internal static readonly Guid[] StationIds = [ColomboFortStationId, GalleStationId, MataraStationId];
    internal static readonly (Guid RouteStopId, Guid StationId, int StopOrder)[] RouteStops =
    [
        (ColomboFortStopId, ColomboFortStationId, 1),
        (GalleStopId, GalleStationId, 2),
        (Id(FamilyRoute, 13), MataraStationId, 3)
    ];
    internal static readonly (Guid RouteStopId, int? ArrivalOffsetMinutes, int? DepartureOffsetMinutes)[] CallingPoints =
    [
        (RouteStops[0].RouteStopId, null, 0),
        (RouteStops[1].RouteStopId, 120, 125),
        (RouteStops[2].RouteStopId, 180, null)
    ];
    internal static string ContentHash => "sha256:" + Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
        System.Text.Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(new
        {
            SourceSystem, OperationDate, RouteId, RouteCode, RouteStops, ServiceId, ServiceCode, ScheduleId,
            ScheduleRevision = 1, FirstDeparture = ColomboDeparture, CallingPoints
        }))));
}
