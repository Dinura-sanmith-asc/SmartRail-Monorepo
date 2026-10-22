using System.Net;
using SmartRail.IntegrationTests.RailwayNetwork;

namespace SmartRail.IntegrationTests.Identity;

public sealed class PublicRailwayReadRegressionTests(PostgreSqlTestDatabaseFixture database, LocalOidcFixture oidc) : IClassFixture<PostgreSqlTestDatabaseFixture>, IClassFixture<LocalOidcFixture>
{
    private static readonly string[] PublicPaths =
    [
        "/api/stations",
        "/api/routes/20000000-0000-0000-0000-000000000001/stops",
        "/api/services?origin=10000000-0000-0000-0000-000000000001&destination=10000000-0000-0000-0000-000000000002&date=2026-10-17",
        "/api/services/30000000-0000-0000-0000-000000000001"
    ];

    [Fact]
    public async Task All_four_public_railway_routes_ignore_missing_malformed_and_expired_optional_tokens()
    {
        using var factory = await IdentityApiTestHost.CreateAsync(database, oidc, seedRailway: true);
        using var client = factory.CreateClient();
        var cases = new string?[] { null, "not-a-jwt", oidc.CreateToken(expires: DateTime.UtcNow.AddMinutes(-1), notBefore: DateTime.UtcNow.AddMinutes(-2)) };
        foreach (var path in PublicPaths)
        foreach (var token in cases)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, path);
            if (token is not null) request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + token);
            using var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.StartsWith("application/json", response.Content.Headers.ContentType?.MediaType ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            var text = await response.Content.ReadAsStringAsync();
            if (token is not null) Assert.DoesNotContain(token, text, StringComparison.Ordinal);
            if (path == PublicPaths[0]) Assert.Contains("Colombo Fort", text, StringComparison.Ordinal);
            if (path == PublicPaths[1]) Assert.Contains("CMB-GAL", text, StringComparison.Ordinal);
            if (path.Contains("/api/services", StringComparison.Ordinal)) Assert.Contains("FTR-001", text, StringComparison.Ordinal);
        }
    }
}
