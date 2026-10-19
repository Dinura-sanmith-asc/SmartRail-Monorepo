using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartRail.Application.Identity.Persistence;
using SmartRail.Application.JourneysStatus.Persistence;
using SmartRail.Domain.Identity;
using SmartRail.Domain.JourneysStatus;
using SmartRail.Domain.JourneysStatus.Entities;
using SmartRail.Infrastructure.JourneysStatus.Persistence;
using SmartRail.Infrastructure.RailwayNetwork;
using SmartRail.IntegrationTests.Identity;
using SmartRail.IntegrationTests.JourneysStatus.Fixtures;
using SmartRail.IntegrationTests.RailwayNetwork;

namespace SmartRail.IntegrationTests.JourneysStatus.Api;

[Collection(JourneyPostgresCollection.Name)]
public sealed class JourneySelectionFlowTests(PostgreSqlTestDatabaseFixture fixture)
{
    private static readonly Guid Origin = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid Destination = Guid.Parse("10000000-0000-0000-0000-000000000002");

    [Fact]
    public async Task Feature001_search_to_departure_choices_to_selected_journey_keeps_instances_distinct_and_marks_ambiguity()
    {
        await ResetIsolatedDatabaseAsync();
        await using var oidc = new LocalOidcFixture();
        await oidc.InitializeAsync();
        using var factory = await IdentityApiTestHost.CreateAsync(fixture, oidc, installProbe: false,
            environment: "Testing", seedRailway: true);
        using var client = factory.CreateClient();

        var sameOccurrenceA = await SeedJourneyAsync(factory.Services,
            Guid.Parse("50000000-0000-0000-0000-000000000021"), "selection-a", Origin,
            DateTimeOffset.Parse("2026-10-17T17:50:00Z"));
        var sameOccurrenceB = await SeedJourneyAsync(factory.Services,
            Guid.Parse("50000000-0000-0000-0000-000000000022"), "selection-b", Origin,
            DateTimeOffset.Parse("2026-10-17T17:50:00Z"));
        var distinguishable = await SeedJourneyAsync(factory.Services,
            Guid.Parse("50000000-0000-0000-0000-000000000023"), "selection-c", Origin,
            DateTimeOffset.Parse("2026-10-17T17:50:00Z"), platform: "4");

        var serviceSearch = await client.GetAsync($"/api/services?origin={Origin}&destination={Destination}&date=2026-10-17");
        Assert.Equal(HttpStatusCode.OK, serviceSearch.StatusCode);
        using var search = JsonDocument.Parse(await serviceSearch.Content.ReadAsStringAsync());
        Assert.Contains("30000000-0000-0000-0000-000000000001", search.RootElement.GetRawText(), StringComparison.OrdinalIgnoreCase);

        var departuresResponse = await client.GetAsync($"/api/stations/{Origin}/departures?date=2026-10-17");
        Assert.Equal(HttpStatusCode.OK, departuresResponse.StatusCode);
        using var departures = JsonDocument.Parse(await departuresResponse.Content.ReadAsStringAsync());
        var choices = departures.RootElement.EnumerateArray().ToArray();
        Assert.Equal(3, choices.Length);
        Assert.Equal(new[] { sameOccurrenceA, sameOccurrenceB, distinguishable }.Order(),
            choices.Select(item => Guid.Parse(item.GetProperty("journeyInstanceId").GetString()!)).Order());
        Assert.All(choices.Where(item => item.GetProperty("journeyInstanceId").GetGuid() != distinguishable),
            item => Assert.Equal("ambiguous", item.GetProperty("ambiguity").GetString()));
        var selected = Assert.Single(choices, item => item.GetProperty("journeyInstanceId").GetGuid() == distinguishable);
        Assert.Equal("none", selected.GetProperty("ambiguity").GetString());
        Assert.Equal("4", selected.GetProperty("platform").GetString());
        Assert.Equal("2026-10-17", selected.GetProperty("operationDate").GetString());
        Assert.DoesNotContain("verified-integration-actor", departures.RootElement.GetRawText(), StringComparison.Ordinal);
        Assert.DoesNotContain("initializationProvenance", departures.RootElement.GetRawText(), StringComparison.OrdinalIgnoreCase);

        var detailResponse = await client.GetAsync($"/api/journeys/{distinguishable}");
        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
        using var detail = JsonDocument.Parse(await detailResponse.Content.ReadAsStringAsync());
        Assert.Equal(distinguishable, detail.RootElement.GetProperty("journeyInstanceId").GetGuid());
        Assert.Equal(selected.GetProperty("revision").GetInt32(), detail.RootElement.GetProperty("revision").GetInt32());

        using (var malformedDate = await client.GetAsync($"/api/stations/{Origin}/departures?date=2026-2-01"))
        {
            Assert.Equal(HttpStatusCode.BadRequest, malformedDate.StatusCode);
            Assert.Equal("application/problem+json", malformedDate.Content.Headers.ContentType?.MediaType);
            Assert.Contains("invalid_station_departure_date", await malformedDate.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        }
        using (var missingDate = await client.GetAsync($"/api/stations/{Origin}/departures"))
        {
            Assert.Equal(HttpStatusCode.BadRequest, missingDate.StatusCode);
            Assert.Equal("application/problem+json", missingDate.Content.Headers.ContentType?.MediaType);
        }
        Assert.Equal(HttpStatusCode.NotFound,
            (await client.GetAsync($"/api/stations/{Guid.NewGuid()}/departures?date=2026-10-17")).StatusCode);
        using (var overnight = JsonDocument.Parse(await client.GetStringAsync($"/api/stations/{Origin}/departures?date=2026-10-18")))
            Assert.Empty(overnight.RootElement.EnumerateArray());
        using (var empty = JsonDocument.Parse(await client.GetStringAsync($"/api/stations/{Origin}/departures?date=2026-10-30")))
            Assert.Empty(empty.RootElement.EnumerateArray());
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.GetAsync("/api/stations/not-a-uuid/departures?date=2026-10-17")).StatusCode);
        await using (var db = new SmartRailDbContext(new DbContextOptionsBuilder<SmartRailDbContext>()
            .UseNpgsql(fixture.ConnectionString).Options))
            await db.Database.ExecuteSqlRawAsync("DROP TABLE journey_revision_stop CASCADE");
        Assert.Equal(HttpStatusCode.ServiceUnavailable,
            (await client.GetAsync($"/api/stations/{Origin}/departures?date=2026-10-17")).StatusCode);
    }

    [Fact]
    public async Task Cancellation_and_platform_publications_are_visible_at_matching_revisions_in_public_reads()
    {
        await ResetIsolatedDatabaseAsync();
        await using var oidc = new LocalOidcFixture();
        await oidc.InitializeAsync();
        using var factory = await IdentityApiTestHost.CreateAsync(fixture, oidc, installProbe: false,
            environment: "Testing", seedRailway: true);
        using var client = factory.CreateClient();
        var cancellationJourney = await SeedJourneyAsync(factory.Services, Guid.NewGuid(), "cancel-" + Guid.NewGuid().ToString("N"), Origin,
            DateTimeOffset.Parse("2026-10-17T17:50:00Z"));
        var platformJourney = await SeedJourneyAsync(factory.Services, Guid.NewGuid(), "platform-" + Guid.NewGuid().ToString("N"), Origin,
            DateTimeOffset.Parse("2026-10-17T17:50:00Z"));
        const string controller = "journey-selection-controller";
        await IdentityTestData.GrantRoleAsync(factory.Services, controller, RoleCode.Controller);

        using (var cancel = IdentityTestData.Bearer(HttpMethod.Patch,
            $"/api/operations/journeys/{cancellationJourney}/status", oidc.CreateToken(controller)))
        {
            cancel.Content = JsonContent.Create(new { expectedJourneyRevision = 1, status = "Cancelled" });
            Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(cancel)).StatusCode);
        }
        using (var cancelHistory = JsonDocument.Parse(await client.GetStringAsync($"/api/journeys/{cancellationJourney}/updates")))
        using (var cancelledDetail = JsonDocument.Parse(await client.GetStringAsync($"/api/journeys/{cancellationJourney}")))
        using (var cancelDepartures = JsonDocument.Parse(await client.GetStringAsync($"/api/stations/{Origin}/departures?date=2026-10-17")))
        {
            var update = Assert.Single(cancelHistory.RootElement.EnumerateArray());
            var departure = Assert.Single(cancelDepartures.RootElement.EnumerateArray(),
                item => item.GetProperty("journeyInstanceId").GetGuid() == cancellationJourney);
            Assert.NotEqual(Guid.Empty, update.GetProperty("updateId").GetGuid());
            Assert.Equal("Cancelled", update.GetProperty("status").GetString());
            Assert.Equal("status", update.GetProperty("publicChange").GetProperty("changeType").GetString());
            Assert.Equal(update.GetProperty("revision").GetInt32(), cancelledDetail.RootElement.GetProperty("revision").GetInt32());
            Assert.Equal(update.GetProperty("revision").GetInt32(), departure.GetProperty("revision").GetInt32());
            Assert.Equal(update.GetProperty("source").GetString(), cancelledDetail.RootElement.GetProperty("source").GetString());
            Assert.Equal(update.GetProperty("source").GetString(), departure.GetProperty("source").GetString());
            Assert.Equal(update.GetProperty("publishedAt").GetDateTimeOffset(), cancelledDetail.RootElement.GetProperty("publishedAt").GetDateTimeOffset());
            Assert.Equal(update.GetProperty("publishedAt").GetDateTimeOffset(), departure.GetProperty("updatedAt").GetDateTimeOffset());
        }

        Guid stopId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SmartRailDbContext>();
            stopId = await db.JourneyRevisionStops.Where(item => item.JourneyInstanceId == platformJourney && item.Revision == 1)
                .Where(item => item.StopOrder == 1).Select(item => item.JourneyStopId).SingleAsync();
        }
        using (var platform = IdentityTestData.Bearer(HttpMethod.Patch,
            $"/api/operations/journeys/{platformJourney}/stops/{stopId}", oidc.CreateToken(controller)))
        {
            platform.Content = JsonContent.Create(new { expectedJourneyRevision = 1, platform = "6" });
            Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(platform)).StatusCode);
        }
        using var platformHistory = JsonDocument.Parse(await client.GetStringAsync($"/api/journeys/{platformJourney}/updates"));
        using var platformDetail = JsonDocument.Parse(await client.GetStringAsync($"/api/journeys/{platformJourney}"));
        using var platformDepartures = JsonDocument.Parse(await client.GetStringAsync($"/api/stations/{Origin}/departures?date=2026-10-17"));
        var platformUpdate = Assert.Single(platformHistory.RootElement.EnumerateArray());
        var platformDeparture = Assert.Single(platformDepartures.RootElement.EnumerateArray(),
            item => item.GetProperty("journeyInstanceId").GetGuid() == platformJourney);
        Assert.NotEqual(Guid.Empty, platformUpdate.GetProperty("updateId").GetGuid());
        Assert.Equal("stopChange", platformUpdate.GetProperty("publicChange").GetProperty("changeType").GetString());
        Assert.Equal("6", platformUpdate.GetProperty("publicChange").GetProperty("platform").GetString());
        Assert.Equal("6", platformDeparture.GetProperty("platform").GetString());
        Assert.Equal(platformUpdate.GetProperty("revision").GetInt32(), platformDetail.RootElement.GetProperty("revision").GetInt32());
        Assert.Equal(platformUpdate.GetProperty("revision").GetInt32(), platformDeparture.GetProperty("revision").GetInt32());
        Assert.Equal(platformUpdate.GetProperty("source").GetString(), platformDetail.RootElement.GetProperty("source").GetString());
        Assert.Equal(platformUpdate.GetProperty("source").GetString(), platformDeparture.GetProperty("source").GetString());
        Assert.Equal(platformUpdate.GetProperty("publishedAt").GetDateTimeOffset(), platformDetail.RootElement.GetProperty("publishedAt").GetDateTimeOffset());
        Assert.Equal(platformUpdate.GetProperty("publishedAt").GetDateTimeOffset(), platformDeparture.GetProperty("updatedAt").GetDateTimeOffset());
    }

    [Fact]
    public async Task OpenApi_exposes_station_departure_date_as_required()
    {
        await ResetIsolatedDatabaseAsync();
        await using var oidc = new LocalOidcFixture();
        await oidc.InitializeAsync();
        using var factory = await IdentityApiTestHost.CreateAsync(fixture, oidc, installProbe: false,
            environment: "Development", seedRailway: true);
        using var client = factory.CreateClient();
        using var document = JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json"));
        var operation = document.RootElement.GetProperty("paths")
            .GetProperty("/api/stations/{id}/departures").GetProperty("get");
        var date = Assert.Single(operation.GetProperty("parameters").EnumerateArray(),
            parameter => parameter.GetProperty("name").GetString() == "date");
        Assert.True(date.GetProperty("required").GetBoolean());
        Assert.Equal("query", date.GetProperty("in").GetString());
    }

    private async Task ResetIsolatedDatabaseAsync()
    {
        await using var db = new SmartRailDbContext(new DbContextOptionsBuilder<SmartRailDbContext>()
            .UseNpgsql(fixture.ConnectionString).Options);
        await db.Database.EnsureDeletedAsync();
    }

    private static async Task<Guid> SeedJourneyAsync(IServiceProvider services, Guid journeyId, string key,
        Guid origin, DateTimeOffset departureAt, string? platform = null)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartRailDbContext>();
        var baseRecord = JourneyFixtureBuilder.Record(journeyId, key);
        var first = new JourneyRevisionStop(journeyId, 1, Guid.NewGuid(), 1, origin,
            Guid.Parse("21000000-0000-0000-0000-000000000001"),
            plannedDepartureAt: departureAt, platform: platform);
        var second = new JourneyRevisionStop(journeyId, 1, Guid.NewGuid(), 2, Destination,
            Guid.Parse("21000000-0000-0000-0000-000000000002"),
            plannedArrivalAt: DateTimeOffset.Parse("2026-10-18T01:25:00+05:30").ToUniversalTime());
        var record = baseRecord with { Stops = [first, second] };
        var result = await new JourneyInitializationRepository(db).InitializeAsync(record, default);
        Assert.Equal(JourneyInitializationStoreOutcome.Created, result.Outcome);
        return journeyId;
    }
}
