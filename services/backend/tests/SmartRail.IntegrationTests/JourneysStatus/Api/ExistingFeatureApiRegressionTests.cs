using System.Net;
using SmartRail.IntegrationTests.Identity;
using SmartRail.IntegrationTests.RailwayNetwork;

namespace SmartRail.IntegrationTests.JourneysStatus.Api;

public sealed class ExistingFeatureApiRegressionTests(
    PostgreSqlTestDatabaseFixture database,
    LocalOidcFixture oidc) : IClassFixture<PostgreSqlTestDatabaseFixture>, IClassFixture<LocalOidcFixture>
{
    [Fact]
    public async Task Feature001_public_reads_ignore_optional_malformed_and_expired_tokens()
    {
        using var factory = await IdentityApiTestHost.CreateAsync(database, oidc, installProbe: false,
            environment: "Testing", seedRailway: true);
        using var client = factory.CreateClient();
        var expired = oidc.CreateToken("expired-public-read", notBefore: DateTime.UtcNow.AddMinutes(-15),
            expires: DateTime.UtcNow.AddMinutes(-10));
        var publicPaths = new[]
        {
            "/api/stations",
            "/api/routes/20000000-0000-0000-0000-000000000001/stops",
            "/api/services?origin=10000000-0000-0000-0000-000000000001&destination=10000000-0000-0000-0000-000000000002&date=2026-10-17",
            "/api/services/30000000-0000-0000-0000-000000000001"
        };

        foreach (var path in publicPaths)
        {
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(path)).StatusCode);
            using var malformed = IdentityTestData.Bearer(HttpMethod.Get, path, "not.a.valid.jwt");
            Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(malformed)).StatusCode);
            using var stale = IdentityTestData.Bearer(HttpMethod.Get, path, expired);
            Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(stale)).StatusCode);
        }

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/me")).StatusCode);
        using var invalidIdentity = IdentityTestData.Bearer(HttpMethod.Get, "/api/me", "not.a.valid.jwt");
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.SendAsync(invalidIdentity)).StatusCode);
        using var expiredIdentity = IdentityTestData.Bearer(HttpMethod.Get, "/api/me", expired);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.SendAsync(expiredIdentity)).StatusCode);
    }
}
