using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartRail.Domain.Identity;
using SmartRail.Domain.JourneysStatus;
using SmartRail.Infrastructure.JourneysStatus.Persistence;
using SmartRail.Infrastructure.RailwayNetwork;
using SmartRail.IntegrationTests.Identity;
using SmartRail.IntegrationTests.JourneysStatus.Fixtures;
using SmartRail.IntegrationTests.RailwayNetwork;

namespace SmartRail.IntegrationTests.JourneysStatus.Api;

public sealed class JourneyInformationRedactionTests(
    PostgreSqlTestDatabaseFixture database,
    LocalOidcFixture oidc) : IClassFixture<PostgreSqlTestDatabaseFixture>, IClassFixture<LocalOidcFixture>
{
    [Fact]
    public async Task Publication_errors_and_logs_do_not_reveal_token_identity_or_owner_evidence()
    {
        using var factory = await IdentityApiTestHost.CreateAsync(database, oidc, installProbe: false,
            environment: "Testing", seedRailway: true);
        using var client = factory.CreateClient();
        var journeyId = await SeedJourneyAsync(factory.Services);
        const string subject = "private-controller-subject";
        await IdentityTestData.GrantRoleAsync(factory.Services, subject, RoleCode.Controller);
        const string privateClaim = "private-personal-claim-value";
        const string privatePassword = "test-only-secret-password";
        var token = oidc.CreateToken(subject, extraClaims: [new("email", privateClaim), new("password", privatePassword)]);
        var observationId = Guid.NewGuid();

        using var request = IdentityTestData.Bearer(HttpMethod.Patch,
            $"/api/operations/journeys/{journeyId}/status", token);
        request.Content = JsonContent.Create(new
        {
            expectedJourneyRevision = 1,
            status = "Delayed",
            driverObservationId = observationId
        });
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        var logs = string.Join("\n", factory.Services.GetRequiredService<IdentityTestLogCaptureProvider>().Messages);

        foreach (var sensitive in new[] { token, subject, privateClaim, privatePassword, observationId.ToString(), "Npgsql", "connection string" })
        {
            Assert.DoesNotContain(sensitive, body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(sensitive, logs, StringComparison.OrdinalIgnoreCase);
        }

        await using var verifyScope = factory.Services.CreateAsyncScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<SmartRailDbContext>();
        Assert.Equal(1, await db.Journeys.Where(item => item.JourneyInstanceId == journeyId)
            .Select(item => item.CurrentRevision).SingleAsync());
        Assert.Equal(1, await db.JourneyRevisions.CountAsync(item => item.JourneyInstanceId == journeyId));
    }

    private static async Task<Guid> SeedJourneyAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartRailDbContext>();
        var result = await new JourneyInitializationRepository(db).InitializeAsync(
            JourneyFixtureBuilder.Record(sourceKey: "redaction-" + Guid.NewGuid().ToString("N")), default);
        Assert.Equal(SmartRail.Application.JourneysStatus.Persistence.JourneyInitializationStoreOutcome.Created, result.Outcome);
        return result.Journey!.JourneyInstanceId;
    }
}
