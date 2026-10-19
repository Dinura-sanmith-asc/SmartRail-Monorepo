using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartRail.Application.Identity.Maintenance;
using SmartRail.Application.Incidents.References;
using SmartRail.Application.Incidents.UseCases;
using SmartRail.Application.JourneysStatus.Initialization;
using SmartRail.Application.JourneysStatus.Publication;
using SmartRail.Application.RailwayNetwork.Services;
using SmartRail.Application.RailwayNetwork.Stations;
using SmartRail.Domain.Incidents;
using SmartRail.Domain.JourneysStatus;
using SmartRail.Domain.RailwayNetwork.Entities;
using SmartRail.Infrastructure.RailwayNetwork;

namespace SmartRail.DevSeeder;

internal sealed record SeedResult(string Manifest, string OperationDate, Guid[] Stations, Guid[] Services, Guid[] Journeys, Guid[] Incidents);

// Internal so the executable's environment/target checks cannot be bypassed through CLI switches.
// Integration tests invoke this engine only after validating a disposable port-5433 test database.
internal sealed class DevelopmentSeedRunner(IServiceProvider services)
{
    internal async Task<SeedResult> RunAsync()
    {
        await using var scope = services.CreateAsyncScope();
        var provider = scope.ServiceProvider;
        var db = provider.GetRequiredService<SmartRailDbContext>();
        if ((await db.Database.GetPendingMigrationsAsync()).Any())
            throw new SeedRefusalException("Apply reviewed migrations separately before running the seed.");
        await db.Database.OpenConnectionAsync();
        await using var seedLock = db.Database.GetDbConnection().CreateCommand();
        seedLock.CommandText = "SELECT pg_try_advisory_lock(4004001)";
        if (!Equals(await seedLock.ExecuteScalarAsync(), true))
            throw new SeedRefusalException("Another development seed invocation is running.");
        try
        {
            await PreflightAsync(db);
            await RailwayAsync(provider, db);
            db.ChangeTracker.Clear();
            await IdentityAsync(provider, db);
            db.ChangeTracker.Clear();
            await JourneysAsync(provider, db);
            db.ChangeTracker.Clear();
            var incidents = await IncidentsAsync(provider, db);
            return new(SeedManifest.Version, SeedManifest.Date.ToString("yyyy-MM-dd"),
                Enumerable.Range(0, 3).Select(i => SeedManifest.Id(1, i)).ToArray(),
                Enumerable.Range(0, 5).Select(i => SeedManifest.Id(3, i)).ToArray(),
                Enumerable.Range(0, 5).Select(i => SeedManifest.Id(5, i)).ToArray(), incidents);
        }
        finally
        {
            await using var unlock = db.Database.GetDbConnection().CreateCommand();
            unlock.CommandText = "SELECT pg_advisory_unlock(4004001)";
            await unlock.ExecuteScalarAsync();
        }
    }

    private static async Task PreflightAsync(SmartRailDbContext db)
    {
        // Fail before the first write if a shared date or reserved station identity is owned by other data.
        for (var day = 0; day < 2; day++)
        {
            var date = SeedManifest.Date.AddDays(day);
            var manifest = await db.ServiceDateManifests.AsNoTracking().SingleOrDefaultAsync(x => x.OperationDate == date);
            if (manifest is not null && (manifest.SourceRevision != SeedManifest.Version || !manifest.IsComplete ||
                manifest.ContentHash != SeedManifest.ContentHash || manifest.FixtureVersion != SeedManifest.Version ||
                manifest.PublishedBy != SeedManifest.Actor))
                throw new SeedRefusalException("Reserved date already has unrelated publication evidence.");
            if (manifest is not null)
            {
                var scheduleIds = await db.ServiceOperationDates.Where(x => x.OperationDate == date).Select(x => x.ScheduleId).ToArrayAsync();
                Require(scheduleIds.Order().SequenceEqual(Enumerable.Range(0, 5).Select(i => SeedManifest.Id(4, i)).Order()),
                    "Existing reserved operation-date evidence was edited; no overwrite is permitted.");
            }
        }
        for (var i = 0; i < 3; i++)
        {
            var id = SeedManifest.Id(1, i);
            var key = $"DEV-SEED-V1-STATION-{i}";
            var station = await db.Stations.Where(x => x.StationId == id || EF.Property<string>(x, "SourceStationKey") == key).Select(x => new { x.StationId, SourceStationKey = EF.Property<string>(x, "SourceStationKey"), x.Name, x.IsPublished }).SingleOrDefaultAsync();
            if (station is not null && (station.StationId != id || station.SourceStationKey != key ||
                station.Name != SeedManifest.Stations[i] || !station.IsPublished))
                throw new SeedRefusalException("Reserved station collision or edited seed record; no overwrite is permitted.");
        }
    }

    private static async Task RailwayAsync(IServiceProvider provider, SmartRailDbContext db)
    {
        var stationIds = Enumerable.Range(0, 3).Select(i => SeedManifest.Id(1, i)).ToArray();
        var count = await db.Stations.CountAsync(x => stationIds.Contains(x.StationId));
        if (count is not (0 or 3)) throw new SeedRefusalException("Partial reserved station set.");
        if (count == 0)
        {
            var import = await provider.GetRequiredService<ImportStationsHandler>().HandleAsync(new(SeedManifest.Actor,
                new(SeedManifest.Version, SeedManifest.ContentHash, SeedManifest.Actor, SeedManifest.Actor,
                    DateTimeOffset.UtcNow, Enumerable.Range(0, 3).Select(i => new StationImportRecord(
                        stationIds[i], $"DEV-SEED-V1-STATION-{i}", SeedManifest.Stations[i], true,
                        new Dictionary<string, string>())).ToArray())));
            Require(import.ImportRun is not null && !import.IsUnauthorized, "Station import failed.");
        }

        var routeId = SeedManifest.Id(2, 0);
        var route = await db.Routes.SingleOrDefaultAsync(x => x.RouteId == routeId || x.RouteCode == "DEV-SEED-COAST");
        if (route is null)
        {
            await using var transaction = await db.Database.BeginTransactionAsync();
            db.Routes.Add(new Route(routeId, "DEV-SEED-COAST", "Development sample: Colombo Fort – Galle – Matara", true));
            for (var i = 0; i < 3; i++) db.RouteStops.Add(new(SeedManifest.Id(2, i + 1), routeId, stationIds[i], i + 1));
            for (var i = 0; i < 5; i++)
            {
                db.TrainServices.Add(new(SeedManifest.Id(3, i), $"DEV-SEED-{i + 1:000}", routeId));
                db.ServiceScheduleRevisions.Add(new(SeedManifest.Id(4, i), SeedManifest.Id(3, i), 1,
                    SeedManifest.Departure(i), SeedManifest.EvidenceTime));
                db.ServiceCallingPoints.AddRange(
                    new ServiceCallingPoint(SeedManifest.Id(4, i), SeedManifest.Id(2, 1), null, 0),
                    new ServiceCallingPoint(SeedManifest.Id(4, i), SeedManifest.Id(2, 2), 120, 125),
                    new ServiceCallingPoint(SeedManifest.Id(4, i), SeedManifest.Id(2, 3), 180, null));
            }
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        else Require(route.RouteId == routeId && route.RouteCode == "DEV-SEED-COAST" && route.IsPublished,
            "Reserved route collision or edited record.");

        // Never repair an existing reference graph silently.
        var stops = await db.RouteStops.Where(x => x.RouteId == routeId).OrderBy(x => x.StopOrder).ToArrayAsync();
        Require(stops.Length == 3 && stops.Select(x => x.StationId).SequenceEqual(stationIds) &&
            stops.Select(x => x.RouteStopId).SequenceEqual(Enumerable.Range(1, 3).Select(i => SeedManifest.Id(2, i))), "Reserved stop graph differs.");
        for (var i = 0; i < 5; i++)
        {
            var serviceId = SeedManifest.Id(3, i);
            var scheduleId = SeedManifest.Id(4, i);
            var service = await db.TrainServices.SingleOrDefaultAsync(x => x.ServiceId == serviceId);
            var schedule = await db.ServiceScheduleRevisions.SingleOrDefaultAsync(x => x.ScheduleId == scheduleId);
            Require(await db.ServiceScheduleRevisions.CountAsync(x => x.ServiceId == serviceId) == 1,
                "Reserved service has additional schedule revisions; manual changes must be reconciled explicitly.");
            Require(service is not null && service.RouteId == routeId && service.ServiceCode == $"DEV-SEED-{i + 1:000}" &&
                schedule is not null && schedule.ServiceId == serviceId && schedule.Revision == 1 &&
                schedule.FirstDepartureLocalTime == SeedManifest.Departure(i) && schedule.SupersededAt is null, "Reserved service/schedule differs.");
            var calls = await db.ServiceCallingPoints.Where(x => x.ScheduleId == scheduleId).ToArrayAsync();
            Require(calls.Length == 3 && calls.Any(x => x.RouteStopId == SeedManifest.Id(2, 1) && x.ArrivalOffsetMinutes == null && x.DepartureOffsetMinutes == 0) &&
                calls.Any(x => x.RouteStopId == SeedManifest.Id(2, 2) && x.ArrivalOffsetMinutes == 120 && x.DepartureOffsetMinutes == 125) &&
                calls.Any(x => x.RouteStopId == SeedManifest.Id(2, 3) && x.ArrivalOffsetMinutes == 180 && x.DepartureOffsetMinutes == null), "Calling point graph differs.");
            if (!schedule!.IsCurrentPublished)
            {
                if (i == 4)
                {
                    var firstDate = SeedManifest.Date;
                    var secondDate = firstDate.AddDays(1);
                    Require(!await db.ServiceDateManifests.AnyAsync(x => x.OperationDate == firstDate || x.OperationDate == secondDate),
                        "Existing date publications will not be replaced, even when their source marker matches.");
                }
                var dates = i == 4 ? Enumerable.Range(0, 2).Select(day => new TimetableDatePublication(
                    new ServiceDateManifest(SeedManifest.Date.AddDays(day), SeedManifest.Version, SeedManifest.Version,
                        true, SeedManifest.EvidenceTime, SeedManifest.Actor, SeedManifest.ContentHash),
                    Enumerable.Range(0, 5).Select(j => new ServiceOperationDate(SeedManifest.Id(4, j), SeedManifest.Date.AddDays(day))).ToArray())).ToArray() : [];
                var result = await provider.GetRequiredService<PublishTimetableHandler>().HandleAsync(new(
                    new(serviceId, scheduleId, SeedManifest.EvidenceTime, SeedManifest.Actor, dates)));
                Require(result.IsPublished, "Timetable publication failed.");
            }
        }
        for (var day = 0; day < 2; day++)
        {
            var date = SeedManifest.Date.AddDays(day);
            var dateSchedules = await db.ServiceOperationDates.Where(x => x.OperationDate == date).Select(x => x.ScheduleId).ToArrayAsync();
            Require(dateSchedules.Order().SequenceEqual(Enumerable.Range(0, 5).Select(i => SeedManifest.Id(4, i)).Order()), "Date operation evidence differs.");
        }
    }

    private static async Task IdentityAsync(IServiceProvider provider, SmartRailDbContext db)
    {
        for (var i = 0; i < SeedManifest.Identities.Length; i++)
        {
            var (role, subject) = SeedManifest.Identities[i];
            if (await db.RoleAssignments.AnyAsync(x => x.CognitoSubject == subject && x.RoleCode == role && x.RevokedAtUtc == null)) continue;
            Require(!await db.RoleAssignments.AnyAsync(x => x.Id == SeedManifest.Id(6, i)), "Seed role was revoked; do not undo manual revocation.");
            var result = await provider.GetRequiredService<GrantRoleAssignmentHandler>().HandleAsync(new(
                SeedManifest.Actor, subject, role, DateTimeOffset.UtcNow, SeedManifest.Id(6, i), SeedManifest.Id(7, i), SeedManifest.Version));
            Require(result.Outcome == MaintenanceCommandOutcome.Completed, "Local role grant failed.");
        }
        for (var i = 0; i < 2; i++)
        {
            var stationId = SeedManifest.Id(1, i);
            if (await db.StationScopeAssignments.AnyAsync(x => x.CognitoSubject == SeedManifest.Staff && x.StationId == stationId && x.RevokedAtUtc == null)) continue;
            Require(!await db.StationScopeAssignments.AnyAsync(x => x.Id == SeedManifest.Id(8, i)), "Seed scope was revoked; do not undo manual revocation.");
            var result = await provider.GetRequiredService<GrantStationScopeHandler>().HandleAsync(new(SeedManifest.Actor,
                SeedManifest.Staff, stationId, DateTimeOffset.UtcNow, SeedManifest.Id(8, i), SeedManifest.Id(9, i), SeedManifest.Version));
            Require(result.Outcome == MaintenanceCommandOutcome.Completed, "Local station scope grant failed.");
        }
    }

    private static async Task JourneysAsync(IServiceProvider provider, SmartRailDbContext db)
    {
        for (var i = 0; i < 5; i++)
        {
            var journeyId = SeedManifest.Id(5, i);
            var initialized = await provider.GetRequiredService<InitializeJourneyHandler>().HandleAsync(new(journeyId,
                SeedManifest.Id(3, i), SeedManifest.Id(4, i), 1, SeedManifest.Date, SeedManifest.Departure(i),
                SeedManifest.Version, $"journey-{i}", SeedManifest.Version));
            Require(initialized.Outcome is JourneyInitializationOutcome.Created or JourneyInitializationOutcome.Replayed, "Journey initialization failed.");
            if (i is 0 or 4) continue;
            var state = i switch { 1 => JourneyStatus.Running, 2 => JourneyStatus.Delayed, _ => JourneyStatus.Cancelled };
            var current = await db.Journeys.AsNoTracking().SingleAsync(x => x.JourneyInstanceId == journeyId);
            if (current.CurrentRevision == 1)
            {
                var result = await provider.GetRequiredService<JourneyPublicationHandler>().PublishStatusAsync(new(journeyId,
                    1, SeedManifest.Controller, state, null, null, null));
                Require(result.Outcome == JourneyPublicationOutcome.Published, "Controller publication failed.");
            }
            var revision = await db.JourneyRevisions.AsNoTracking().SingleAsync(x => x.JourneyInstanceId == journeyId && x.Revision == 2);
            Require(revision.Status == state && revision.PublishedBySubject == SeedManifest.Controller, "Existing journey publication differs.");
            if (i == 2)
            {
                current = await db.Journeys.AsNoTracking().SingleAsync(x => x.JourneyInstanceId == journeyId);
                if (current.CurrentRevision == 2)
                {
                    var stop = await db.JourneyRevisionStops.AsNoTracking().SingleAsync(x => x.JourneyInstanceId == journeyId && x.Revision == 2 && x.StopOrder == 1);
                    var result = await provider.GetRequiredService<JourneyPublicationHandler>().UpdateStopAsync(new(journeyId,
                        stop.JourneyStopId, 2, SeedManifest.Controller, "DEV-2", null,
                        stop.PlannedDepartureAt!.Value.AddMinutes(15), null, null));
                    Require(result.Outcome == JourneyPublicationOutcome.Published, "Platform/timing publication failed.");
                }
            }
        }
    }

    private static async Task<Guid[]> IncidentsAsync(IServiceProvider provider, SmartRailDbContext db)
    {
        var disruption = await db.JourneyRevisions.AsNoTracking().SingleAsync(x => x.JourneyInstanceId == SeedManifest.Id(5, 2) && x.Revision == 2);
        var ids = new List<Guid>();
        for (var i = 0; i < 4; i++)
        {
            var links = new List<IncidentReference>
            {
                new(IncidentReferenceKind.Station, SeedManifest.Id(1, i == 3 ? 2 : i == 2 ? 1 : 0)),
                new(IncidentReferenceKind.Service, SeedManifest.Id(3, i == 3 ? 0 : i)),
                new(IncidentReferenceKind.Journey, SeedManifest.Id(5, i == 3 ? 0 : i))
            };
            if (i == 1) links.AddRange([
                new(IncidentReferenceKind.Station, SeedManifest.Id(1, 1)),
                new(IncidentReferenceKind.Journey, SeedManifest.Id(5, 2)),
                new(IncidentReferenceKind.Disruption, SeedManifest.Id(5, 2), disruption.UpdateId, 2)]);
            var created = await provider.GetRequiredService<CreateIncidentHandler>().HandleAsync(new(SeedManifest.Controller,
                $"{SeedManifest.Version}-incident-{i}", new(SeedManifest.Category,
                    $"DEVELOPMENT SAMPLE ONLY — {new[] { "Reported", "In Progress", "Resolved", "Closed" }[i]} incident",
                    SeedManifest.EvidenceTime, links)));
            Require(created.Outcome is CreateIncidentOutcome.Created or CreateIncidentOutcome.Replayed, "Incident creation failed.");
            var id = created.Incident!.IncidentId;
            ids.Add(id);
            db.ChangeTracker.Clear(); // Each command models a separate request/unit of work.
            for (var step = 0; step < i; step++)
            {
                var result = await provider.GetRequiredService<UpdateIncidentHandler>().HandleAsync(new(id, SeedManifest.Controller,
                    $"{SeedManifest.Version}-incident-{i}-transition-{step}", new(step + 1, (IncidentState)(step + 2))));
                Require(result.Outcome is UpdateIncidentOutcome.Updated or UpdateIncidentOutcome.Replayed, "Incident lifecycle update failed.");
                db.ChangeTracker.Clear();
            }
        }
        return ids.ToArray();
    }

    private static void Require(bool condition, string safeReason)
    {
        if (!condition) throw new SeedRefusalException(safeReason);
    }
}
