using SmartRail.Domain.Identity;

namespace SmartRail.DevSeeder;

internal static class SeedManifest
{
    internal const string Version = "smartrail-dev-v1";
    internal const string Actor = "local-development-seed-maintainer";
    internal const string Controller = "smartrail-manual-controller";
    internal const string Staff = "smartrail-manual-station-staff";
    internal const string Category = "DEV-SAMPLE-OPERATIONAL";
    // Fingerprint the fixed source dataset, not runtime-generated audit IDs/timestamps.
    internal static string ContentHash => "sha256:" + Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
        System.Text.Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(new
        {
            Version, Date, Stations, Services, Departures = Enumerable.Range(0, 5).Select(Departure).ToArray(),
            ArrivalOffsets = new int?[] { null, 120, 180 }, DepartureOffsets = new int?[] { 0, 125, null }
        }))));
    internal static readonly DateOnly Date = new(2026, 11, 1);
    internal static readonly DateOnly ReferenceStationDate = new(2026, 11, 3);
    internal static readonly Guid ColomboFortReferenceId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    internal static readonly Guid GalleReferenceId = Guid.Parse("10000000-0000-0000-0000-000000000002");
    internal static readonly string[] ReferenceStationServices = ["Colombo Fort to Galle", "Galle to Colombo Fort"];
    internal static TimeOnly ReferenceDeparture(int index) => index == 0 ? new(8, 30) : new(15, 10);
    internal static string ReferenceContentHash => "sha256:" + Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
        System.Text.Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(new
        {
            Version, ReferenceStationDate, ColomboFortReferenceId, GalleReferenceId, ReferenceStationServices,
            Departures = Enumerable.Range(0, 2).Select(ReferenceDeparture).ToArray(), ArrivalOffsetMinutes = 130
        }))));
    internal static readonly DateTimeOffset EvidenceTime = new(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);
    internal static Guid Id(int family, int number) => Guid.Parse($"d0040000-0000-0000-{family:0000}-{number:000000000000}");
    internal static readonly string[] Stations = ["Colombo Fort (development sample)", "Galle (development sample)", "Matara (development sample)"];
    internal static readonly string[] Services = ["Scheduled", "Running", "Delayed", "Cancelled", "Overnight"];
    internal static TimeOnly Departure(int index) => index == 4 ? new(23, 20) : new(8 + index * 2, 0);
    internal static readonly (RoleCode Role, string Subject)[] Identities =
    [
        (RoleCode.Controller, Controller), (RoleCode.StationStaff, Staff),
        (RoleCode.Driver, "smartrail-manual-driver"), (RoleCode.Passenger, "smartrail-manual-passenger")
    ];

    internal static object Export() => new
    {
        version = Version, developmentOnly = true, syntheticTimetable = true, contentHash = ContentHash,
        sourceReference = "Existing Feature 001 coastal fixture station names/topology; isolated reserved IDs and synthetic extensions/times, not an authoritative railway timetable",
        dates = new[] { Date.ToString("yyyy-MM-dd"), Date.AddDays(1).ToString("yyyy-MM-dd") },
        referenceStationDepartures = new
        {
            operationDate = ReferenceStationDate.ToString("yyyy-MM-dd"),
            contentHash = ReferenceContentHash,
            stations = new[]
            {
                new { id = ColomboFortReferenceId, name = "Colombo Fort", scheduledDepartureCount = 1 },
                new { id = GalleReferenceId, name = "Galle", scheduledDepartureCount = 1 }
            },
            services = Enumerable.Range(0, 2).Select(i => new
            {
                scenario = ReferenceStationServices[i], routeId = Id(10, i), serviceId = Id(11, i),
                scheduleId = Id(12, i), journeyInstanceId = Id(13, i),
                departureAsiaColombo = ReferenceDeparture(i).ToString("HH:mm"), arrivalOffsetMinutes = 130
            })
        },
        stations = Stations.Select((name, i) => new { id = Id(1, i), name }),
        route = new { id = Id(2, 0), code = "DEV-SEED-COAST", orderedStopIds = Enumerable.Range(1, 3).Select(i => Id(2, i)) },
        services = Services.Select((scenario, i) => new { scenario, serviceId = Id(3, i), code = $"DEV-SEED-{i + 1:000}",
            scheduleId = Id(4, i), scheduleRevision = 1, departureAsiaColombo = Departure(i).ToString("HH:mm"),
            journeyInstanceId = Id(5, i), operationDate = Date.ToString("yyyy-MM-dd") }),
        additionalJourney = new { scenario = "Completed", journeyInstanceId = Id(5, 5), serviceId = Id(3, 0),
            scheduleId = Id(4, 0), operationDate = Date.AddDays(1).ToString("yyyy-MM-dd") },
        identities = Identities.Select(x => new { role = x.Role.ToString(), subject = x.Subject }),
        stationStaffScope = Enumerable.Range(0, 2).Select(i => Id(1, i)),
        sampleCategoryCode = Category,
        incidentIdentity = "Generated once by the approved create command; stable actor/operation/idempotency keys return the same IDs on reruns",
        expectedNewRecords = new { stations = 3, routes = 3, routeStops = 7, services = 7, schedules = 7, callingPoints = 19,
            dateManifests = 3, operationDates = 17, stationImportRuns = 1, stationChanges = 3,
            roles = 4, roleAudits = 4, stationScopes = 2, scopeAudits = 2,
            journeys = 8, journeyRevisions = 14, journeyRevisionStops = 40, initializationAudits = 8,
            incidents = 4, incidentRevisions = 10, incidentAssociations = 15, incidentIdempotencyRecords = 10 }
    };
}
