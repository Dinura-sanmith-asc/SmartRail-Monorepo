using Microsoft.EntityFrameworkCore;
using SmartRail.Application.JourneysStatus.Queries;
using SmartRail.Application.JourneysStatus.Queries.GetStationDepartures;
using SmartRail.Application.JourneysStatus.Persistence;
using SmartRail.Application.JourneysStatus.Publication;
using SmartRail.Domain.JourneysStatus;
using SmartRail.Domain.JourneysStatus.Entities;
using SmartRail.Infrastructure.JourneysStatus.Persistence;
using SmartRail.Infrastructure.RailwayNetwork;
using SmartRail.IntegrationTests.JourneysStatus.Fixtures;
using SmartRail.IntegrationTests.RailwayNetwork;

namespace SmartRail.IntegrationTests.JourneysStatus.Persistence;

[Collection(JourneyPostgresCollection.Name)]
public sealed class JourneyDepartureAndDiversionTests(PostgreSqlTestDatabaseFixture fixture)
{
    [Fact]
    public async Task Diversion_is_atomic_retains_history_and_station_read_uses_current_revision_and_verified_time()
    {
        await using var db = CreateContext();
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
        await new RailwayNetworkFixtureLoader(db).LoadAsync("Testing",
            Path.Combine(AppContext.BaseDirectory, "Fixtures", "RailwayNetwork"));

        var initial = JourneyFixtureBuilder.Record(sourceKey: "diversion-it-" + Guid.NewGuid().ToString("N"));
        var initialized = await new JourneyInitializationRepository(db).InitializeAsync(initial, default);
        Assert.Equal(JourneyInitializationStoreOutcome.Created, initialized.Outcome);
        var originalStops = initial.Stops.ToArray();
        var departureAt = new DateTimeOffset(2026, 10, 18, 0, 5, 0, TimeSpan.FromHours(5.5)).ToUniversalTime();
        var diverted = new[]
        {
            new JourneyRevisionStop(initial.Journey.JourneyInstanceId, 2, originalStops[0].JourneyStopId,
                1, originalStops[0].StationId, originalStops[0].RouteStopId,
                plannedDepartureAt: originalStops[0].PlannedDepartureAt),
            new JourneyRevisionStop(initial.Journey.JourneyInstanceId, 2, Guid.NewGuid(), 2,
                originalStops[1].StationId, routeStopId: null, operationalDepartureAt: departureAt,
                platform: "2", timingSource: "ControllerManual", timingUpdatedAt: DateTimeOffset.UtcNow)
        };
        var revision = new JourneyRevision(initial.Journey.JourneyInstanceId, 2, JourneyStatus.Scheduled,
            DateTimeOffset.UtcNow, "ControllerManual", "routeDiversion", Guid.NewGuid(),
            JourneyPublicationKind.RouteDiversion, 1, "verified-test-controller");
        var appended = await new JourneyPublicationRepository(db, new JourneyRepository(db)).AppendAsync(
            initial.Journey.JourneyInstanceId, 1, revision, diverted, default);
        Assert.Equal(JourneyRevisionAppendOutcome.Appended, appended.Outcome);

        var readRepo = new JourneyReadRepository(db);
        var result = await new GetStationDeparturesHandler(readRepo).HandleAsync(new(
            originalStops[1].StationId, "2026-10-18"));
        var departure = Assert.Single(result.Departures!);
        Assert.Equal(initial.Journey.JourneyInstanceId, departure.JourneyInstanceId);
        Assert.Equal(initial.Journey.OperationDate, departure.OperationDate);
        Assert.Equal(2, departure.Revision);
        Assert.Null(departure.ScheduledDepartureAt);
        Assert.Equal(departureAt, departure.OperationalDepartureAt);
        Assert.Equal("ControllerManual", departure.Source);
        Assert.Equal("2", departure.Platform);

        var snapshot = await readRepo.ReadSnapshotAsync(initial.Journey.JourneyInstanceId, default);
        var history = await readRepo.ReadHistoryAsync(initial.Journey.JourneyInstanceId, default);
        Assert.Equal(snapshot.Revision!.Revision, history.Revisions!.Max(item => item.Revision));
        Assert.Equal(snapshot.Revision.Revision,
            snapshot.Stops!.Select(item => item.Revision).Distinct().Single());
        Assert.Equal(new[] { 1, 2 }, history.StopsByRevision![1].Select(item => item.StopOrder));
        Assert.Contains(history.StopsByRevision[1], item => item.JourneyStopId == originalStops[1].JourneyStopId);
        Assert.DoesNotContain(snapshot.Stops!, item => item.JourneyStopId == originalStops[1].JourneyStopId);
        Assert.Contains(snapshot.Stops!, item => item.JourneyStopId == originalStops[0].JourneyStopId);
        Assert.Equal(new[] { 1, 2 }, snapshot.Stops!.Select(item => item.StopOrder));
    }

    private SmartRailDbContext CreateContext() => new(new DbContextOptionsBuilder<SmartRailDbContext>()
        .UseNpgsql(fixture.ConnectionString).Options);
}
