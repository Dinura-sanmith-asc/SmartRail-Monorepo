using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartRail.Api.Endpoints.JourneysStatus;
using SmartRail.Application.JourneysStatus.Persistence;
using SmartRail.Domain.JourneysStatus;
using SmartRail.Domain.JourneysStatus.Entities;
using SmartRail.Infrastructure.JourneysStatus;
using SmartRail.Infrastructure.JourneysStatus.Persistence;
using SmartRail.Infrastructure.RailwayNetwork;
using SmartRail.IntegrationTests.JourneysStatus.Fixtures;
using SmartRail.IntegrationTests.RailwayNetwork;

namespace SmartRail.IntegrationTests.JourneysStatus.Api;

[Collection(JourneyPostgresCollection.Name)]
public sealed class JourneyReadApiTests(PostgreSqlTestDatabaseFixture fixture)
{
    [Fact]
    public async Task Anonymous_reads_return_consistent_public_snapshot_complete_history_and_safe_errors()
    {
        await using (var seed = CreateContext())
        {
            await seed.Database.EnsureDeletedAsync();
            await seed.Database.MigrateAsync();
            await LoadReferenceFixturesAsync(seed);
            var journeyId = Guid.Parse("50000000-0000-0000-0000-000000000001");
            var record = JourneyFixtureBuilder.Record(journeyId, "journey-read-api-main");
            Assert.Equal(JourneyInitializationStoreOutcome.Created,
                (await new JourneyInitializationRepository(seed).InitializeAsync(record, default)).Outcome);
            await AppendStatusAsync(seed, record.Journey, record.Stops, 1, JourneyStatus.Running,
                Guid.Parse("60000000-0000-0000-0000-000000000001"), DateTimeOffset.Parse("2026-10-17T16:00:00Z"));
            var secondRevisionStops = record.Stops.Select(stop => Copy(stop, 3,
                platform: stop.StopOrder == 1 ? "4" : null)).ToArray();
            var platformUpdateId = Guid.Parse("60000000-0000-0000-0000-000000000002");
            var platform = new JourneyRevision(journeyId, 3, JourneyStatus.Running,
                DateTimeOffset.Parse("2026-10-17T16:05:00Z"), "ControllerManual", "stopChange",
                platformUpdateId, JourneyPublicationKind.StopChange, 2, "sensitive-controller-subject",
                privateAuditEvidence: "sensitive-private-evidence");
            Assert.Equal(JourneyRevisionAppendOutcome.Appended,
                (await new JourneyRepository(seed).AppendRevisionAsync(journeyId, 2, platform,
                    secondRevisionStops, default)).Outcome);

            var noUpdates = JourneyFixtureBuilder.Record(Guid.Parse("50000000-0000-0000-0000-000000000002"),
                "journey-read-api-empty");
            Assert.Equal(JourneyInitializationStoreOutcome.Created,
                (await new JourneyInitializationRepository(seed).InitializeAsync(noUpdates, default)).Outcome);
        }

        await using var app = await CreateAppAsync(fixture.ConnectionString);
        var client = app.GetTestClient();
        var journeyUri = "/api/journeys/50000000-0000-0000-0000-000000000001";
        var detailResponse = await client.GetAsync(journeyUri);
        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
        using var detail = JsonDocument.Parse(await detailResponse.Content.ReadAsStringAsync());
        var snapshot = detail.RootElement;
        Assert.Equal("50000000-0000-0000-0000-000000000001", snapshot.GetProperty("journeyInstanceId").GetString());
        Assert.Equal("30000000-0000-0000-0000-000000000001", snapshot.GetProperty("serviceId").GetString());
        Assert.Equal("31000000-0000-0000-0000-000000000002", snapshot.GetProperty("scheduleId").GetString());
        Assert.Equal(2, snapshot.GetProperty("scheduleRevision").GetInt32());
        Assert.Equal("2026-10-17", snapshot.GetProperty("operationDate").GetString());
        Assert.Equal(3, snapshot.GetProperty("revision").GetInt32());
        Assert.Equal("Running", snapshot.GetProperty("status").GetString());
        Assert.Equal("ControllerManual", snapshot.GetProperty("source").GetString());
        Assert.Equal(DateTimeOffset.Parse("2026-10-17T16:05:00Z"), snapshot.GetProperty("publishedAt").GetDateTimeOffset());
        Assert.True(snapshot.GetProperty("eta").ValueKind == JsonValueKind.Null);
        var snapshotStops = snapshot.GetProperty("stops").EnumerateArray().ToArray();
        Assert.Equal(new[] { 1, 2 }, snapshotStops.Select(item => item.GetProperty("stopOrder").GetInt32()));
        Assert.Equal("Colombo Fort", snapshotStops[0].GetProperty("stationName").GetString());
        Assert.Equal("4", snapshotStops[0].GetProperty("platform").GetString());
        Assert.True(snapshotStops[0].GetProperty("timingSource").ValueKind == JsonValueKind.Null);
        Assert.DoesNotContain("sensitive-controller-subject", detail.RootElement.GetRawText(), StringComparison.Ordinal);
        Assert.DoesNotContain("sensitive-private-evidence", detail.RootElement.GetRawText(), StringComparison.Ordinal);
        Assert.DoesNotContain("private-source-evidence", detail.RootElement.GetRawText(), StringComparison.Ordinal);
        Assert.DoesNotContain("driverObservationId", detail.RootElement.GetRawText(), StringComparison.Ordinal);

        var historyResponse = await client.GetAsync(journeyUri + "/updates");
        Assert.Equal(HttpStatusCode.OK, historyResponse.StatusCode);
        using var history = JsonDocument.Parse(await historyResponse.Content.ReadAsStringAsync());
        var updates = history.RootElement.EnumerateArray().ToArray();
        Assert.Equal(2, updates.Length);
        Assert.Equal(new[] { 2, 3 }, updates.Select(item => item.GetProperty("revision").GetInt32()));
        Assert.Equal("status", updates[0].GetProperty("kind").GetString());
        Assert.Equal("status", updates[0].GetProperty("publicChange").GetProperty("changeType").GetString());
        Assert.Equal("Running", updates[0].GetProperty("publicChange").GetProperty("status").GetString());
        Assert.Equal("stopChange", updates[1].GetProperty("kind").GetString());
        Assert.Equal("stopChange", updates[1].GetProperty("publicChange").GetProperty("changeType").GetString());
        Assert.Equal("4", updates[1].GetProperty("publicChange").GetProperty("platform").GetString());
        Assert.Equal("60000000-0000-0000-0000-000000000002", updates[1].GetProperty("updateId").GetString());
        Assert.DoesNotContain("privateAuditEvidence", history.RootElement.GetRawText(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sensitive-controller-subject", history.RootElement.GetRawText(), StringComparison.Ordinal);
        Assert.DoesNotContain("driverObservationId", history.RootElement.GetRawText(), StringComparison.Ordinal);

        var emptyHistory = await client.GetFromJsonAsync<JsonElement>("/api/journeys/50000000-0000-0000-0000-000000000002/updates");
        Assert.Equal(JsonValueKind.Array, emptyHistory.ValueKind);
        Assert.Empty(emptyHistory.EnumerateArray());
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/journeys/not-a-guid")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await client.GetAsync("/api/journeys/50000000-0000-0000-0000-000000000099")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await client.GetAsync("/api/journeys/50000000-0000-0000-0000-000000000099/updates")).StatusCode);

        var readEndpoints = app.Services.GetServices<EndpointDataSource>().SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>().Where(endpoint => endpoint.RoutePattern.RawText is "/api/journeys/{id}" or "/api/journeys/{id}/updates")
            .ToArray();
        Assert.Equal(2, readEndpoints.Length);
        foreach (var endpoint in readEndpoints)
        {
            var documentedStatuses = endpoint.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>()
                .Select(metadata => metadata.StatusCode).ToHashSet();
            Assert.Contains(StatusCodes.Status200OK, documentedStatuses);
            Assert.Contains(StatusCodes.Status400BadRequest, documentedStatuses);
            Assert.Contains(StatusCodes.Status404NotFound, documentedStatuses);
            Assert.Contains(StatusCodes.Status503ServiceUnavailable, documentedStatuses);
            var response = endpoint.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>()
                .Single(metadata => metadata.StatusCode == StatusCodes.Status200OK);
            if (endpoint.RoutePattern.RawText == "/api/journeys/{id}")
                Assert.Equal(typeof(JourneySnapshotResponse), response.Type);
            else
                Assert.Equal(typeof(IReadOnlyList<JourneyUpdateResponse>), response.Type);
        }

        await using (var unavailable = CreateContext())
            await unavailable.Database.ExecuteSqlRawAsync("DROP TABLE journey_revision_stop CASCADE");
        Assert.Equal(HttpStatusCode.ServiceUnavailable,
            (await client.GetAsync(journeyUri)).StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable,
            (await client.GetAsync(journeyUri + "/updates")).StatusCode);
    }

    private SmartRailDbContext CreateContext() => new(new DbContextOptionsBuilder<SmartRailDbContext>()
        .UseNpgsql(fixture.ConnectionString).Options);

    private async Task LoadReferenceFixturesAsync(SmartRailDbContext db) =>
        await new RailwayNetworkFixtureLoader(db).LoadAsync("Testing",
            Path.Combine(AppContext.BaseDirectory, "Fixtures", "RailwayNetwork"));

    private static async Task AppendStatusAsync(SmartRailDbContext db, Journey journey,
        IReadOnlyCollection<JourneyRevisionStop> baselineStops, int expectedRevision, JourneyStatus status,
        Guid updateId, DateTimeOffset publishedAt)
    {
        var revision = new JourneyRevision(journey.JourneyInstanceId, expectedRevision + 1, status, publishedAt,
            "ControllerManual", "status", updateId, JourneyPublicationKind.Status, expectedRevision,
            "sensitive-controller-subject", privateAuditEvidence: "sensitive-private-evidence");
        var stops = baselineStops.Select(stop => Copy(stop, expectedRevision + 1)).ToArray();
        Assert.Equal(JourneyRevisionAppendOutcome.Appended,
            (await new JourneyRepository(db).AppendRevisionAsync(journey.JourneyInstanceId, expectedRevision,
                revision, stops, default)).Outcome);
    }

    private static JourneyRevisionStop Copy(JourneyRevisionStop stop, int revision, string? platform = null) =>
        new(stop.JourneyInstanceId, revision, stop.JourneyStopId, stop.StopOrder, stop.StationId,
            stop.RouteStopId, stop.PlannedArrivalAt, stop.PlannedDepartureAt, stop.OperationalArrivalAt,
            stop.OperationalDepartureAt, platform ?? stop.Platform, platform is null ? stop.TimingSource : "private-source-evidence",
            platform is null ? stop.TimingUpdatedAt : DateTimeOffset.Parse("2026-10-17T16:05:00Z"), stop.AmbiguityState);

    private static async Task<WebApplication> CreateAppAsync(string connectionString)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
        builder.WebHost.UseTestServer();
        builder.Services.AddDbContext<SmartRailDbContext>(options => options.UseNpgsql(connectionString));
        builder.Services.AddJourneysStatusReadServices();
        var app = builder.Build();
        app.MapJourneysStatusReadEndpoints();
        await app.StartAsync();
        return app;
    }

}
