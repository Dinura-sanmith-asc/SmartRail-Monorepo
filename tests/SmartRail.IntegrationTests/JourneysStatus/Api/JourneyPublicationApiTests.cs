using System.Net;
using Microsoft.AspNetCore.TestHost;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SmartRail.Application.JourneysStatus.Boundaries;
using SmartRail.Domain.Identity;
using SmartRail.Domain.JourneysStatus;
using SmartRail.Infrastructure.JourneysStatus.Initialization;
using SmartRail.Infrastructure.JourneysStatus.Persistence;
using SmartRail.Infrastructure.RailwayNetwork;
using SmartRail.IntegrationTests.Identity;
using SmartRail.IntegrationTests.JourneysStatus.Fixtures;
using SmartRail.IntegrationTests.RailwayNetwork;

namespace SmartRail.IntegrationTests.JourneysStatus.Api;

[Collection(JourneyPostgresCollection.Name)]
public sealed class JourneyPublicationApiTests(PostgreSqlTestDatabaseFixture fixture)
{
    [Fact]
    public async Task Controller_can_publish_status_and_passenger_cannot_change_official_state()
    {
        await ResetIsolatedDatabaseAsync();
        await using var oidc = new LocalOidcFixture();
        await oidc.InitializeAsync();
        using var factory = await IdentityApiTestHost.CreateAsync(fixture, oidc, installProbe: false, environment: "Testing", seedRailway: true);
        using var client = factory.CreateClient();
        var journeyId = await SeedJourneyAsync(factory.Services, "status-api-" + Guid.NewGuid().ToString("N"));
        const string controllerSubject = "verified-controller-api";
        const string passengerSubject = "verified-passenger-api";
        await IdentityTestData.GrantRoleAsync(factory.Services, controllerSubject, RoleCode.Controller);
        await IdentityTestData.GrantRoleAsync(factory.Services, passengerSubject, RoleCode.Passenger);

        using var request = IdentityTestData.Bearer(HttpMethod.Patch, $"/api/operations/journeys/{journeyId}/status",
            oidc.CreateToken(controllerSubject));
        request.Content = JsonContent.Create(new { expectedJourneyRevision = 1, status = "Delayed" });
        using var success = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, success.StatusCode);
        using var body = JsonDocument.Parse(await success.Content.ReadAsStringAsync());
        Assert.Equal(2, body.RootElement.GetProperty("revision").GetInt32());
        Assert.Equal("Delayed", body.RootElement.GetProperty("status").GetString());
        Assert.False(body.RootElement.TryGetProperty("publishedBySubject", out _));
        Assert.False(body.RootElement.TryGetProperty("driverObservationId", out _));

        using var deniedRequest = IdentityTestData.Bearer(HttpMethod.Patch, $"/api/operations/journeys/{journeyId}/status",
            oidc.CreateToken(passengerSubject));
        deniedRequest.Content = JsonContent.Create(new { expectedJourneyRevision = 2, status = "Interrupted" });
        using var denied = await client.SendAsync(deniedRequest);
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        using var dbScope = factory.Services.CreateScope();
        var db = dbScope.ServiceProvider.GetRequiredService<SmartRailDbContext>();
        Assert.Equal(2, await db.Journeys.Where(x => x.JourneyInstanceId == journeyId).Select(x => x.CurrentRevision).SingleAsync());
        Assert.Equal(2, await db.JourneyRevisions.CountAsync(x => x.JourneyInstanceId == journeyId));
    }

    [Fact]
    public async Task Stop_patch_validates_controller_auth_and_stale_revision_mapping()
    {
        await ResetIsolatedDatabaseAsync();
        await using var oidc = new LocalOidcFixture();
        await oidc.InitializeAsync();
        using var factory = await IdentityApiTestHost.CreateAsync(fixture, oidc, installProbe: false, environment: "Testing", seedRailway: true);
        using var client = factory.CreateClient();
        var journeyId = await SeedJourneyAsync(factory.Services, "stop-api-" + Guid.NewGuid().ToString("N"));
        const string controllerSubject = "verified-stop-controller";
        await IdentityTestData.GrantRoleAsync(factory.Services, controllerSubject, RoleCode.Controller);
        Guid stopId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SmartRailDbContext>();
            stopId = await db.JourneyRevisionStops.Where(x => x.JourneyInstanceId == journeyId && x.Revision == 1)
                .OrderBy(x => x.StopOrder).Select(x => x.JourneyStopId).FirstAsync();
        }

        using var request = IdentityTestData.Bearer(HttpMethod.Patch, $"/api/operations/journeys/{journeyId}/stops/{stopId}",
            oidc.CreateToken(controllerSubject));
        request.Content = JsonContent.Create(new { expectedJourneyRevision = 1, platform = "4" });
        using var success = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, success.StatusCode);

        using var staleRequest = IdentityTestData.Bearer(HttpMethod.Patch, $"/api/operations/journeys/{journeyId}/stops/{stopId}",
            oidc.CreateToken(controllerSubject));
        staleRequest.Content = JsonContent.Create(new { expectedJourneyRevision = 1, platform = "5" });
        using var stale = await client.SendAsync(staleRequest);
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);

        using var invalidRequest = IdentityTestData.Bearer(HttpMethod.Patch, $"/api/operations/journeys/{journeyId}/status",
            oidc.CreateToken(controllerSubject));
        invalidRequest.Content = JsonContent.Create(new { expectedJourneyRevision = 2, status = "NotAStatus" });
        using var invalid = await client.SendAsync(invalidRequest);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);

        using var missingRequest = IdentityTestData.Bearer(HttpMethod.Patch, $"/api/operations/journeys/{Guid.NewGuid()}/status",
            oidc.CreateToken(controllerSubject));
        missingRequest.Content = JsonContent.Create(new { expectedJourneyRevision = 1, status = "Delayed" });
        using var missing = await client.SendAsync(missingRequest);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task Authorized_controller_cancellation_uses_existing_additional_grant()
    {
        await ResetIsolatedDatabaseAsync();
        await using var oidc = new LocalOidcFixture();
        await oidc.InitializeAsync();
        using var factory = await IdentityApiTestHost.CreateAsync(fixture, oidc, installProbe: false, environment: "Testing", seedRailway: true);
        using var client = factory.CreateClient();
        var journeyId = await SeedJourneyAsync(factory.Services, "cancel-api-" + Guid.NewGuid().ToString("N"));
        const string subject = "verified-cancellation-controller";
        await IdentityTestData.GrantRoleAsync(factory.Services, subject, RoleCode.Controller);

        using var request = IdentityTestData.Bearer(HttpMethod.Patch, $"/api/operations/journeys/{journeyId}/status", oidc.CreateToken(subject));
        request.Content = JsonContent.Create(new { expectedJourneyRevision = 1, status = "Cancelled" });
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartRailDbContext>();
        var revision = await db.JourneyRevisions.SingleAsync(x => x.JourneyInstanceId == journeyId && x.Revision == 2);
        Assert.Equal(JourneyStatus.Cancelled, revision.Status);
        Assert.Equal("verified-cancellation-controller", revision.PublishedBySubject);
    }

    [Fact]
    public async Task Invalid_optional_driver_observation_is_unavailable_and_anonymous_write_is_unauthorized()
    {
        await ResetIsolatedDatabaseAsync();
        await using var oidc = new LocalOidcFixture();
        await oidc.InitializeAsync();
        using var baseFactory = await IdentityApiTestHost.CreateAsync(fixture, oidc, installProbe: false, environment: "Testing", seedRailway: true);
        using var factory = baseFactory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IDriverObservationVerifier>();
            services.AddSingleton<IDriverObservationVerifier>(new FixedObservationVerifier(DriverObservationVerification.Unavailable()));
        }));
        using var client = factory.CreateClient();
        var journeyId = await SeedJourneyAsync(factory.Services, "observation-api-" + Guid.NewGuid().ToString("N"));
        const string controllerSubject = "verified-observation-controller";
        await IdentityTestData.GrantRoleAsync(factory.Services, controllerSubject, RoleCode.Controller);

        using var unavailableRequest = IdentityTestData.Bearer(HttpMethod.Patch, $"/api/operations/journeys/{journeyId}/status",
            oidc.CreateToken(controllerSubject));
        unavailableRequest.Content = JsonContent.Create(new { expectedJourneyRevision = 1, status = "Delayed", driverObservationId = Guid.NewGuid() });
        using var unavailable = await client.SendAsync(unavailableRequest);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, unavailable.StatusCode);

        using var anonymousRequest = new HttpRequestMessage(HttpMethod.Patch, $"/api/operations/journeys/{journeyId}/status")
        {
            Content = JsonContent.Create(new { expectedJourneyRevision = 1, status = "Delayed" })
        };
        using var unauthorized = await client.SendAsync(anonymousRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartRailDbContext>();
        Assert.Equal(1, await db.Journeys.Where(x => x.JourneyInstanceId == journeyId).Select(x => x.CurrentRevision).SingleAsync());
    }

    private async Task ResetIsolatedDatabaseAsync()
    {
        await using var db = new SmartRailDbContext(new DbContextOptionsBuilder<SmartRailDbContext>().UseNpgsql(fixture.ConnectionString).Options);
        await db.Database.EnsureDeletedAsync();
    }

    private static async Task<Guid> SeedJourneyAsync(IServiceProvider services, string sourceKey)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartRailDbContext>();
        var result = await new JourneyInitializationRepository(db).InitializeAsync(JourneyFixtureBuilder.Record(sourceKey: sourceKey), default);
        Assert.Equal(SmartRail.Application.JourneysStatus.Persistence.JourneyInitializationStoreOutcome.Created, result.Outcome);
        return result.Journey!.JourneyInstanceId;
    }

    private sealed class FixedObservationVerifier(DriverObservationVerification result) : IDriverObservationVerifier
    {
        public Task<DriverObservationVerification> VerifyAsync(string verifiedActorSubject, Guid observationId,
            Guid journeyInstanceId, CancellationToken cancellationToken = default) => Task.FromResult(result);
    }
}
