using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SmartRail.Application.DriverOperations;
using SmartRail.Domain.Identity;
using SmartRail.Infrastructure.RailwayNetwork;
using SmartRail.IntegrationTests.Identity;
using SmartRail.IntegrationTests.Incidents;
using SmartRail.IntegrationTests.RailwayNetwork;

namespace SmartRail.IntegrationTests.DriverOperations;

public sealed class DriverCurrentJourneyTests(PostgreSqlTestDatabaseFixture database, LocalOidcFixture oidc)
    : IClassFixture<PostgreSqlTestDatabaseFixture>, IClassFixture<LocalOidcFixture>
{
    [Fact]
    public async Task ControllerAndMultiroleReadAllWhileUnassignedAndRevokedDriverReadEmpty()
    {
        using var host = await IncidentOwnerIntegrationTestHost.CreateAsync(database, oidc);
        using var client = host.CreateClient();
        var first = await IncidentOwnerIntegrationTestHost.InitializeJourneyAsync(host.Services, "current-first");
        var second = await IncidentOwnerIntegrationTestHost.InitializeJourneyAsync(host.Services, "current-second");
        await IdentityTestData.GrantRoleAsync(host.Services, "driver-a", RoleCode.Driver);
        await IdentityTestData.GrantRoleAsync(host.Services, "driver-b", RoleCode.Driver);
        await IdentityTestData.GrantRoleAsync(host.Services, "empty-driver", RoleCode.Driver);
        await IdentityTestData.GrantRoleAsync(host.Services, "controller", RoleCode.Controller);
        await IdentityTestData.GrantRoleAsync(host.Services, "combined", RoleCode.Driver);
        await IdentityTestData.GrantRoleAsync(host.Services, "combined", RoleCode.Controller);
        var assignment = await DriverOperationsApiTests.GrantAssignmentAsync(host.Services, "driver-a", first);
        await DriverOperationsApiTests.GrantAssignmentAsync(host.Services, "driver-b", second);
        Assert.Equal(2, (await Assignments(client, "controller")).Length);
        Assert.Equal(2, (await Assignments(client, "combined")).Length);
        Assert.Empty(await Assignments(client, "empty-driver"));
        Assert.Single(await Assignments(client, "driver-a"));
        await using (var scope = host.Services.CreateAsyncScope())
            Assert.True(await scope.ServiceProvider.GetRequiredService<IDriverOperationsRepository>().RevokeAssignmentAsync(assignment, "operator", DateTimeOffset.UtcNow, Guid.NewGuid(), "revocation"));
        Assert.Empty(await Assignments(client, "driver-a"));
        Assert.Single(await Assignments(client, "controller"));
    }

    [Fact]
    public async Task NextDriverReadReflectsGenuineCentralStatusPlatformChangesAndOvernightInstants()
    {
        using var host = await IncidentOwnerIntegrationTestHost.CreateAsync(database, oidc);
        using var client = host.CreateClient();
        var journey = await IncidentOwnerIntegrationTestHost.InitializeJourneyAsync(host.Services, "central-current");
        await IdentityTestData.GrantRoleAsync(host.Services, "driver", RoleCode.Driver);
        await IdentityTestData.GrantRoleAsync(host.Services, "controller", RoleCode.Controller);
        await DriverOperationsApiTests.GrantAssignmentAsync(host.Services, "driver", journey);
        var initial = Assert.Single(await Assignments(client, "driver")).GetProperty("journey").GetProperty("snapshot");
        var stops = initial.GetProperty("stops").EnumerateArray().ToArray();
        Assert.True(stops.Length >= 2);
        Assert.Equal(Enumerable.Range(1, stops.Length), stops.Select(x => x.GetProperty("stopOrder").GetInt32()));
        var departure = stops.First().GetProperty("plannedDepartureAt").GetDateTimeOffset().ToOffset(TimeSpan.FromHours(5.5));
        var arrival = stops.Last().GetProperty("plannedArrivalAt").GetDateTimeOffset().ToOffset(TimeSpan.FromHours(5.5));
        Assert.Equal(new DateOnly(2026,10,17), DateOnly.FromDateTime(departure.DateTime));
        Assert.Equal(new DateOnly(2026,10,18), DateOnly.FromDateTime(arrival.DateTime));
        Assert.Equal("2026-10-17", initial.GetProperty("operationDate").GetString());
        await Publish(client, $"/api/operations/journeys/{journey}/status", new { expectedJourneyRevision = 1, status = "Delayed" });
        var stopId = stops.First().GetProperty("journeyStopId").GetGuid();
        await Publish(client, $"/api/operations/journeys/{journey}/stops/{stopId}", new { expectedJourneyRevision = 2, platform = "7" });
        var context = Assert.Single(await Assignments(client, "driver")).GetProperty("journey");
        Assert.Equal(3, context.GetProperty("snapshot").GetProperty("revision").GetInt32());
        Assert.Equal("Delayed", context.GetProperty("snapshot").GetProperty("status").GetString());
        Assert.Equal("7", context.GetProperty("snapshot").GetProperty("stops")[0].GetProperty("platform").GetString());
        var updates = context.GetProperty("updates").EnumerateArray().ToArray();
        Assert.Equal(new[] { 2, 3 }, updates.Select(x => x.GetProperty("revision").GetInt32()));
        Assert.All(updates, item => { Assert.NotEqual(Guid.Empty, item.GetProperty("updateId").GetGuid()); Assert.False(string.IsNullOrWhiteSpace(item.GetProperty("source").GetString())); Assert.NotEqual(default, item.GetProperty("publishedAt").GetDateTimeOffset()); });
        Assert.Contains("Delayed", updates[0].GetRawText());
        Assert.Contains("7", updates[1].GetProperty("publicChange").GetRawText());
    }

    [Fact]
    public async Task ProductionDefaultMaintenanceDeniesAndTestScopedAuthorityCreatesAuditedIdempotentLifecycle()
    {
        using var host = await IncidentOwnerIntegrationTestHost.CreateAsync(database, oidc);
        var journey = await IncidentOwnerIntegrationTestHost.InitializeJourneyAsync(host.Services, "maintenance-current");
        var id = Guid.NewGuid();
        var grant = new GrantDriverAssignmentCommand("approved-test-operator", id, "driver", journey, "test-manifest");
        await using (var scope = host.Services.CreateAsyncScope())
        {
            Assert.IsType<DenyAllDriverAssignmentMaintenanceAuthority>(scope.ServiceProvider.GetRequiredService<IDriverAssignmentMaintenanceAuthority>());
            Assert.Equal(DriverOutcome.Forbidden, (await scope.ServiceProvider.GetRequiredService<DriverAssignmentMaintenanceHandler>().GrantAsync(grant)).Outcome);
            Assert.Empty(await scope.ServiceProvider.GetRequiredService<SmartRailDbContext>().DriverJourneyAssignments.ToListAsync());
        }
        using var authorizedHost = host.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IDriverAssignmentMaintenanceAuthority>();
            services.AddSingleton<IDriverAssignmentMaintenanceAuthority, TestOnlyAuthority>();
        }));
        await using (var scope = authorizedHost.Services.CreateAsyncScope())
        {
            var handler = scope.ServiceProvider.GetRequiredService<DriverAssignmentMaintenanceHandler>();
            Assert.Equal(DriverOutcome.Created, (await handler.GrantAsync(grant)).Outcome);
            Assert.Equal(DriverOutcome.Success, (await handler.GrantAsync(grant)).Outcome);
            Assert.Equal(DriverOutcome.Success, (await handler.RevokeAsync(new("approved-test-operator", id, "test-revoke"))).Outcome);
            Assert.Equal(DriverOutcome.Success, (await handler.RevokeAsync(new("approved-test-operator", id, "test-revoke"))).Outcome);
            var db = scope.ServiceProvider.GetRequiredService<SmartRailDbContext>();
            Assert.Single(await db.DriverJourneyAssignments.ToListAsync());
            Assert.False((await db.DriverJourneyAssignments.SingleAsync()).IsActive);
            Assert.Equal(2, await db.DriverAssignmentAudits.CountAsync());
        }
    }

    private async Task<JsonElement[]> Assignments(HttpClient client, string subject)
    {
        using var response = await client.SendAsync(IdentityTestData.Bearer(HttpMethod.Get, "/api/driver/assignments", oidc.CreateToken(subject)));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("items").EnumerateArray().Select(x => x.Clone()).ToArray();
    }
    private async Task Publish(HttpClient client, string url, object payload)
    {
        using var request = IdentityTestData.Bearer(HttpMethod.Patch, url, oidc.CreateToken("controller"));
        request.Content = JsonContent.Create(payload);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
    private sealed class TestOnlyAuthority : IDriverAssignmentMaintenanceAuthority
    {
        public Task<DriverAssignmentMaintenanceOutcome> AuthorizeAsync(string subject, DriverAssignmentMaintenanceAction action, CancellationToken cancellationToken = default) =>
            Task.FromResult(subject == "approved-test-operator" ? DriverAssignmentMaintenanceOutcome.Allowed : DriverAssignmentMaintenanceOutcome.Denied);
    }
}
