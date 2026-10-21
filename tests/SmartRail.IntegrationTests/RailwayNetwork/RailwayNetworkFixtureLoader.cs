using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SmartRail.Application.RailwayNetwork.Stations;
using SmartRail.Domain.RailwayNetwork.Entities;
using SmartRail.Infrastructure.RailwayNetwork;
using SmartRail.Infrastructure.RailwayNetwork.Persistence;

namespace SmartRail.IntegrationTests.RailwayNetwork;

/// <summary>Explicit test/development-only loader; never registered as an application startup seed.</summary>
public sealed class RailwayNetworkFixtureLoader(SmartRailDbContext dbContext)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task LoadAsync(string environmentName, string fixtureDirectory, CancellationToken cancellationToken = default)
    {
        if (!IsNonProduction(environmentName))
            throw new InvalidOperationException("Railway fixtures may only be loaded in Development, Test, or Testing environments.");
        ArgumentException.ThrowIfNullOrWhiteSpace(fixtureDirectory);
        if (await dbContext.Stations.AnyAsync(cancellationToken) || await dbContext.Routes.AnyAsync(cancellationToken))
            throw new InvalidOperationException("Fixtures load only into a fresh, migrated database.");

        var stationDocument = await ReadAsync<StationFixtureDocument>(Path.Combine(fixtureDirectory, "stations-v1.json"), cancellationToken);
        var timetableDocument = await ReadAsync<TimetableFixtureDocument>(Path.Combine(fixtureDirectory, "timetable-v1.json"), cancellationToken);
        var importRepository = new StationImportRepository(dbContext);
        await importRepository.ApplyAsync(new StationImportRequest(
            stationDocument.SourceRevision, stationDocument.ContentHash,
            "fixture-submitter", "fixture-approver", DateTimeOffset.UtcNow,
            stationDocument.Stations), cancellationToken);

        var routes = timetableDocument.Routes.Select(item => new Route(item.RouteId, item.RouteCode, item.Name, isPublished: true)).ToArray();
        var stops = timetableDocument.Routes.SelectMany(item => item.Stops.Select(stop =>
            new RouteStop(stop.RouteStopId, item.RouteId, stop.StationId, stop.StopOrder))).ToArray();
        var services = timetableDocument.Services.Select(item => new TrainService(item.ServiceId, item.ServiceCode, item.RouteId)).ToArray();
        var revisions = timetableDocument.Services.SelectMany(service => service.Revisions.Select(revision =>
            new ServiceScheduleRevision(revision.ScheduleId, service.ServiceId, revision.Revision,
                TimeOnly.Parse(revision.FirstDepartureLocalTime), revision.CreatedAt.ToUniversalTime()))).ToArray();
        var stopOrders = stops.ToDictionary(stop => stop.RouteStopId, stop => stop.StopOrder);
        var calls = timetableDocument.Services.SelectMany(service => service.Revisions.SelectMany(revision => revision.Calls.Select(call =>
            new ServiceCallingPoint(revision.ScheduleId, call.RouteStopId, call.ArrivalOffsetMinutes, call.DepartureOffsetMinutes,
                stopOrders[call.RouteStopId])))).ToArray();

        dbContext.Routes.AddRange(routes);
        dbContext.RouteStops.AddRange(stops);
        dbContext.TrainServices.AddRange(services);
        dbContext.ServiceScheduleRevisions.AddRange(revisions);
        dbContext.ServiceCallingPoints.AddRange(calls);
        await dbContext.SaveChangesAsync(cancellationToken);

        var schedulePublisher = new SchedulePublicationRepository(dbContext);
        foreach (var service in timetableDocument.Services)
        foreach (var revision in service.Revisions.OrderBy(item => item.Revision))
            await schedulePublisher.PublishAsync(service.ServiceId, revision.ScheduleId, DateTimeOffset.UtcNow, cancellationToken);

        var datePublisher = new ServiceDateManifestRepository(dbContext);
        foreach (var item in timetableDocument.DateManifests)
        {
            var manifest = new ServiceDateManifest(item.OperationDate, item.SourceRevision, item.FixtureVersion,
                item.IsComplete, item.PublishedAt?.ToUniversalTime(), item.PublishedBy, item.ContentHash);
            var operations = item.ScheduleIds.Select(scheduleId => new ServiceOperationDate(scheduleId, item.OperationDate)).ToArray();
            await datePublisher.PublishAsync(manifest, operations, cancellationToken);
        }
    }

    private static bool IsNonProduction(string environmentName) =>
        string.Equals(environmentName, "Development", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(environmentName, "Test", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(environmentName, "Testing", StringComparison.OrdinalIgnoreCase);

    private static async Task<T> ReadAsync<T>(string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken)
            ?? throw new InvalidDataException($"Fixture '{Path.GetFileName(path)}' has no document content.");
    }
}

public sealed record StationFixtureDocument(string SourceRevision, string ContentHash, IReadOnlyList<StationImportRecord> Stations);
public sealed record TimetableFixtureDocument(IReadOnlyList<RouteFixture> Routes, IReadOnlyList<ServiceFixture> Services, IReadOnlyList<DateManifestFixture> DateManifests);
public sealed record RouteFixture(Guid RouteId, string RouteCode, string Name, IReadOnlyList<RouteStopFixture> Stops);
public sealed record RouteStopFixture(Guid RouteStopId, Guid StationId, int StopOrder);
public sealed record ServiceFixture(Guid ServiceId, string ServiceCode, Guid RouteId, IReadOnlyList<ScheduleFixture> Revisions);
public sealed record ScheduleFixture(Guid ScheduleId, int Revision, DateTimeOffset CreatedAt, string FirstDepartureLocalTime, IReadOnlyList<CallingPointFixture> Calls);
public sealed record CallingPointFixture(Guid RouteStopId, int? ArrivalOffsetMinutes, int? DepartureOffsetMinutes);
public sealed record DateManifestFixture(DateOnly OperationDate, string SourceRevision, string FixtureVersion, bool IsComplete, DateTimeOffset? PublishedAt, string? PublishedBy, string ContentHash, IReadOnlyList<Guid> ScheduleIds);
