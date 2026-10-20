using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartRail.Domain.Identity;
using SmartRail.Application.Identity.Persistence;
using SmartRail.Domain.JourneysStatus;
using SmartRail.Domain.JourneysStatus.Entities;
using SmartRail.Domain.RailwayNetwork.Entities;
using SmartRail.Domain.Incidents;
using SmartRail.Domain.Incidents.Entities;
using SmartRail.Infrastructure.RailwayNetwork;
using SmartRail.IntegrationTests.Identity;
using SmartRail.IntegrationTests.RailwayNetwork;

namespace SmartRail.IntegrationTests.Operations;

public sealed class OperationsOverviewApiTests(PostgreSqlTestDatabaseFixture database, LocalOidcFixture oidc)
    : IClassFixture<PostgreSqlTestDatabaseFixture>, IClassFixture<LocalOidcFixture>
{
    private static readonly DateOnly Date = new(2026, 11, 3);

    [Fact]
    public async Task Controller_daily_overview_uses_current_database_state_and_computes_metrics()
    {
        using var factory = await IdentityApiTestHost.CreateAsync(database, oidc, installProbe: false);
        await using (var migrationScope = factory.Services.CreateAsyncScope())
        {
            var db = migrationScope.ServiceProvider.GetRequiredService<SmartRailDbContext>();
            Assert.Contains(await db.Database.GetAppliedMigrationsAsync(), migration =>
                migration.EndsWith("_ControllerOperationsData", StringComparison.Ordinal));
        }
        var subject = "ops-controller-" + Guid.NewGuid().ToString("N");
        await IdentityTestData.GrantRoleAsync(factory.Services, subject, RoleCode.Controller);
        var expected = await SeedOverviewData(factory.Services, subject);

        using var client = factory.CreateClient();
        var token = oidc.CreateToken(subject);
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/operations/overview?date={Date:yyyy-MM-dd}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var payload = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonDocument>();
        Assert.NotNull(payload);
        var root = payload.RootElement;
        var metrics = root.GetProperty("metrics");
        Assert.Equal(6, metrics.GetProperty("totalScheduled").GetInt32());
        Assert.Equal(2, metrics.GetProperty("running").GetInt32());
        Assert.Equal(2, metrics.GetProperty("delayed").GetInt32());
        Assert.Equal(2, metrics.GetProperty("openIncidents").GetInt32());
        Assert.Equal(1, metrics.GetProperty("cancelled").GetInt32());
        Assert.Equal(1, metrics.GetProperty("completed").GetInt32());

        var journeys = root.GetProperty("journeys").EnumerateArray().ToArray();
        Assert.Equal(6, journeys.Length);
        var named = Assert.Single(journeys, item => item.GetProperty("journeyId").GetGuid() == expected.DelayedRunning);
        Assert.Equal("Udarata Menike", named.GetProperty("serviceDisplayName").GetString());
        Assert.Equal("TEST-01", named.GetProperty("publicServiceNumber").GetString());
        Assert.Equal(expected.ServiceId, named.GetProperty("serviceId").GetGuid());
        Assert.NotEqual(expected.ServiceId, named.GetProperty("journeyId").GetGuid());
        Assert.Equal("Colombo Fort", named.GetProperty("origin").GetString());
        Assert.Equal("Galle", named.GetProperty("destination").GetString());
        Assert.Equal("Running", named.GetProperty("status").GetString());
        Assert.Equal(5m, named.GetProperty("recordedDelayMinutes").GetDecimal());
        Assert.Equal("Colombo Fort", named.GetProperty("lastReportedStation").GetString());
        Assert.True(named.GetProperty("lastUpdateAt").GetDateTimeOffset() > DateTimeOffset.MinValue);
        Assert.Equal(JsonValueKind.Null, named.GetProperty("assignedDriver").ValueKind);

        using var anotherDate = Request("/api/operations/overview?date=2026-11-04", token);
        using var emptyResponse = await client.SendAsync(anotherDate);
        Assert.Equal(HttpStatusCode.OK, emptyResponse.StatusCode);
        using var emptyPayload = await emptyResponse.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.NotNull(emptyPayload);
        Assert.Equal(0, emptyPayload.RootElement.GetProperty("metrics").GetProperty("totalScheduled").GetInt32());
        Assert.Empty(emptyPayload.RootElement.GetProperty("journeys").EnumerateArray());
    }

    [Fact]
    public async Task Overview_enforces_current_role_and_station_scope_and_validates_date()
    {
        using var factory = await IdentityApiTestHost.CreateAsync(database, oidc, installProbe: false);
        using var client = factory.CreateClient();
        var noRoleSubject = "ops-no-role-" + Guid.NewGuid().ToString("N");
        using (var noRole = Request("/api/operations/overview?date=2026-11-03", oidc.CreateToken(noRoleSubject)))
        using (var denied = await client.SendAsync(noRole)) Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);

        var revokedSubject = "ops-revoked-" + Guid.NewGuid().ToString("N");
        var roleId = await IdentityTestData.GrantRoleAsync(factory.Services, revokedSubject, RoleCode.Controller);
        await using (var revokeScope = factory.Services.CreateAsyncScope())
        {
            var roles = revokeScope.ServiceProvider.GetRequiredService<IRoleAssignmentRepository>();
            Assert.True(await roles.RevokeAsync(roleId, "test-approver", DateTimeOffset.UtcNow, Guid.NewGuid()));
        }
        using (var revoked = Request("/api/operations/overview?date=2026-11-03", oidc.CreateToken(revokedSubject)))
        using (var denied = await client.SendAsync(revoked)) Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);

        var driverSubject = "ops-driver-" + Guid.NewGuid().ToString("N");
        await IdentityTestData.GrantRoleAsync(factory.Services, driverSubject, RoleCode.Driver);
        using (var driver = Request("/api/operations/overview?date=2026-11-03", oidc.CreateToken(driverSubject)))
        using (var denied = await client.SendAsync(driver)) Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);

        var staffSubject = "ops-staff-" + Guid.NewGuid().ToString("N");
        await IdentityTestData.GrantRoleAsync(factory.Services, staffSubject, RoleCode.StationStaff);
        var stationId = await IdentityTestData.AddStationAndGrantScopeAsync(factory.Services, staffSubject);
        using (var unscoped = Request("/api/operations/overview?date=2026-11-03", oidc.CreateToken(staffSubject)))
        using (var denied = await client.SendAsync(unscoped)) Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        using (var scoped = Request($"/api/operations/overview?date=2026-11-03&stationId={stationId}", oidc.CreateToken(staffSubject)))
        using (var response = await client.SendAsync(scoped))
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
            Assert.NotNull(body);
            Assert.Empty(body.RootElement.GetProperty("journeys").EnumerateArray());
            Assert.Equal(0, body.RootElement.GetProperty("metrics").GetProperty("totalScheduled").GetInt32());
        }
        using (var wrongScope = Request($"/api/operations/overview?date=2026-11-03&stationId={Guid.NewGuid()}", oidc.CreateToken(staffSubject)))
        using (var denied = await client.SendAsync(wrongScope)) Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        using (var invalidDate = await client.GetAsync("/api/operations/overview?date=03-11-2026"))
            Assert.Equal(HttpStatusCode.Unauthorized, invalidDate.StatusCode);
        using (var authenticatedInvalidDate = Request("/api/operations/overview?date=03-11-2026", oidc.CreateToken(staffSubject)))
        using (var invalid = await client.SendAsync(authenticatedInvalidDate)) Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
    }

    [Fact]
    public async Task OpenApi_describes_only_the_authorized_overview_route_contract()
    {
        using var factory = await IdentityApiTestHost.CreateAsync(database, oidc, installProbe: false, environment: "Development");
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = await response.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.NotNull(document);
        var paths = document.RootElement.GetProperty("paths");
        var overview = paths.GetProperty("/api/operations/overview").GetProperty("get");
        var parameters = overview.GetProperty("parameters").EnumerateArray().ToArray();
        var namedParameters = parameters.Where(item => item.TryGetProperty("name", out _)).ToArray();
        Assert.Contains(namedParameters, item => item.GetProperty("name").GetString() == "date" && item.GetProperty("required").GetBoolean());
        Assert.Contains(namedParameters, item => item.GetProperty("name").GetString() == "stationId" &&
            (!item.TryGetProperty("required", out var required) || !required.GetBoolean()));
        Assert.True(overview.GetProperty("security").EnumerateArray().Any());

        var responses = overview.GetProperty("responses");
        foreach (var status in new[] { "200", "400", "401", "403", "503" }) Assert.True(responses.TryGetProperty(status, out _));
        var overviewSchema = ResolveSchema(document.RootElement, responses.GetProperty("200")
            .GetProperty("content").GetProperty("application/json").GetProperty("schema"));
        foreach (var field in new[] { "operationalDate", "metrics", "journeys" })
            Assert.True(overviewSchema.GetProperty("properties").TryGetProperty(field, out _), $"Missing OpenAPI field: {field}");
        var metricsSchema = ResolveSchema(document.RootElement, overviewSchema.GetProperty("properties").GetProperty("metrics"));
        Assert.Equal(new[] { "totalScheduled", "running", "delayed", "openIncidents", "cancelled", "completed" }.Order(),
            metricsSchema.GetProperty("properties").EnumerateObject().Select(property => property.Name).Order());
        var journeySchema = ResolveSchema(document.RootElement, overviewSchema.GetProperty("properties")
            .GetProperty("journeys").GetProperty("items"));
        foreach (var field in new[] { "journeyId", "serviceId", "serviceDisplayName", "publicServiceNumber", "origin",
                     "destination", "scheduledDeparture", "scheduledArrival", "status", "recordedDelayMinutes",
                     "lastReportedStation", "lastUpdateAt", "assignedDriver" })
            Assert.True(journeySchema.GetProperty("properties").TryGetProperty(field, out _), $"Missing OpenAPI field: {field}");
    }

    private static JsonElement ResolveSchema(JsonElement document, JsonElement schema)
    {
        if (!schema.TryGetProperty("$ref", out var reference)) return schema;
        var name = reference.GetString()!.Split('/').Last();
        return document.GetProperty("components").GetProperty("schemas").GetProperty(name);
    }

    private static HttpRequestMessage Request(string url, string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    private static async Task<(Guid ServiceId, Guid DelayedRunning)> SeedOverviewData(IServiceProvider services, string actor)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartRailDbContext>();
        var colombo = Guid.NewGuid();
        var galle = Guid.NewGuid();
        var routeId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        var scheduleId = Guid.NewGuid();
        var originRouteStop = Guid.NewGuid();
        var destinationRouteStop = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        db.Stations.AddRange(new Station(colombo, "OPS-CMB-" + Guid.NewGuid().ToString("N"), "Colombo Fort", isPublished: true),
            new Station(galle, "OPS-GAL-" + Guid.NewGuid().ToString("N"), "Galle", isPublished: true));
        db.Routes.Add(new Route(routeId, "OPS-ROUTE-" + Guid.NewGuid().ToString("N")[..8], "Coastal Line", isPublished: true));
        db.RouteStops.AddRange(new RouteStop(originRouteStop, routeId, colombo, 1), new RouteStop(destinationRouteStop, routeId, galle, 2));
        db.TrainServices.Add(new TrainService(serviceId, "OPS-SERVICE-" + Guid.NewGuid().ToString("N")[..8], routeId,
            isPublished: true, displayName: "Udarata Menike", publicServiceNumber: "TEST-01"));
        db.ServiceScheduleRevisions.Add(new ServiceScheduleRevision(scheduleId, serviceId, 1, new TimeOnly(8, 0), now,
            isPublished: true, publishedAt: now));
        db.ServiceCallingPoints.AddRange(new ServiceCallingPoint(scheduleId, originRouteStop, null, 0),
            new ServiceCallingPoint(scheduleId, destinationRouteStop, 125, null));
        await db.SaveChangesAsync();

        var journeyIds = Enumerable.Range(0, 6).Select(_ => Guid.NewGuid()).ToArray();
        var statuses = new[] { JourneyStatus.Running, JourneyStatus.Running, JourneyStatus.Delayed, JourneyStatus.Cancelled, JourneyStatus.Completed, JourneyStatus.Scheduled };
        for (var i = 0; i < journeyIds.Length; i++)
        {
            var id = journeyIds[i];
            var key = "ops-fixture-" + Guid.NewGuid().ToString("N");
            var journey = new Journey(id, serviceId, scheduleId, 1, Date, new TimeOnly(8, 0), "integration-test", key,
                now.AddMinutes(-60), actor, "isolated Operations API integration fixture");
            var baseline = new JourneyRevision(id, 1, JourneyStatus.Scheduled, now.AddMinutes(-60), "ScheduledBaseline");
            var stop1 = new JourneyRevisionStop(id, 1, Guid.NewGuid(), 1, colombo, originRouteStop,
                plannedDepartureAt: LocalInstant(Date, new TimeOnly(8, 0)));
            var stop2 = new JourneyRevisionStop(id, 1, Guid.NewGuid(), 2, galle, destinationRouteStop,
                plannedArrivalAt: LocalInstant(Date, new TimeOnly(10, 5)));
            db.Journeys.Add(journey);
            db.JourneyRevisions.Add(baseline);
            db.JourneyRevisionStops.AddRange(stop1, stop2);
            db.JourneyInitializationAudits.Add(new JourneyInitializationAudit(Guid.NewGuid(), "integration-test", key,
                JourneyInitializationDecision.Accepted, now.AddMinutes(-60), "isolated Operations API integration fixture",
                id, actor, serviceId, scheduleId, 1, Date, new TimeOnly(8, 0)));
            if (statuses[i] != JourneyStatus.Scheduled)
            {
                journey.AdvanceRevision(1);
                var updateId = Guid.NewGuid();
                db.JourneyRevisions.Add(new JourneyRevision(id, 2, statuses[i], now, "ControllerManual",
                    statuses[i].ToString(), updateId, JourneyPublicationKind.Status, previousRevision: 1,
                    publishedBySubject: actor, privateAuditEvidence: "integration fixture"));
                var delayedTiming = i == 0;
                var actualDeparture = delayedTiming ? stop1.PlannedDepartureAt!.Value.AddMinutes(5) : (DateTimeOffset?)null;
                db.JourneyRevisionStops.AddRange(
                    new JourneyRevisionStop(id, 2, stop1.JourneyStopId, 1, colombo, originRouteStop,
                        plannedDepartureAt: stop1.PlannedDepartureAt, operationalDepartureAt: actualDeparture,
                        timingSource: delayedTiming ? "ControllerManual" : null,
                        timingUpdatedAt: delayedTiming ? now : null),
                    new JourneyRevisionStop(id, 2, stop2.JourneyStopId, 2, galle, destinationRouteStop,
                        plannedArrivalAt: stop2.PlannedArrivalAt));
            }
        }
        await db.SaveChangesAsync();

        foreach (var state in new[] { IncidentState.Reported, IncidentState.InProgress, IncidentState.Resolved, IncidentState.Closed })
        {
            var incidentId = Guid.NewGuid();
            var incident = new Incident(incidentId, "TEST_ONLY", "Isolated dashboard test incident", actor, now.AddMinutes(-20));
            var revision = new IncidentRevision(incidentId, 1, IncidentState.Reported, actor, now.AddMinutes(-20), "[]");
            var association = new IncidentAssociation(Guid.NewGuid(), incidentId, IncidentAssociationKind.Station,
                colombo, now.AddMinutes(-20), 1);
            db.Incidents.Add(incident);
            db.IncidentRevisions.Add(revision);
            db.IncidentAssociations.Add(association);
            var expected = IncidentState.Reported;
            foreach (var next in state switch
            {
                IncidentState.Reported => Array.Empty<IncidentState>(),
                IncidentState.InProgress => new[] { IncidentState.InProgress },
                IncidentState.Resolved => new[] { IncidentState.InProgress, IncidentState.Resolved },
                _ => new[] { IncidentState.InProgress, IncidentState.Resolved, IncidentState.Closed }
            })
            {
                var previous = expected;
                incident.TransitionTo(next, incident.Revision, now.AddMinutes(-10));
                db.IncidentRevisions.Add(new IncidentRevision(incidentId, incident.Revision, next, actor,
                    now.AddMinutes(-10), "[]", previous));
                expected = next;
            }
        }
        await db.SaveChangesAsync();
        return (serviceId, journeyIds[0]);
    }

    private static DateTimeOffset LocalInstant(DateOnly date, TimeOnly time) => new DateTimeOffset(
        date.Year, date.Month, date.Day, time.Hour, time.Minute, time.Second, TimeSpan.FromHours(5.5)).ToUniversalTime();
}
