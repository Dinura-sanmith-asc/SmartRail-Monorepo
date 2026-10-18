using System.Net;
using System.Text.Json;
using SmartRail.Application.Identity.Authorization;
using SmartRail.Domain.Identity;
using SmartRail.IntegrationTests.RailwayNetwork;

namespace SmartRail.IntegrationTests.Identity;

/// <summary>Phase 7 local HTTP smoke coverage using real RSA/JWKS validation and the isolated test database.</summary>
public sealed class IdentityLocalSmokeTests(PostgreSqlTestDatabaseFixture database, LocalOidcFixture oidc) : IClassFixture<PostgreSqlTestDatabaseFixture>, IClassFixture<LocalOidcFixture>
{
    [Fact]
    public async Task Local_identity_and_public_railway_smoke_flows_preserve_statuses_and_public_data()
    {
        using var factory = await IdentityApiTestHost.CreateAsync(database, oidc, seedRailway: true, unavailableStationScopes: true);
        using var client = factory.CreateClient();

        var mappedSubject = "smoke-mapped-" + Guid.NewGuid().ToString("N");
        await IdentityTestData.GrantRoleAsync(factory.Services, mappedSubject, RoleCode.StationStaff);
        using (var mapped = await client.SendAsync(IdentityTestData.Bearer(HttpMethod.Get, "/api/me", oidc.CreateToken(mappedSubject))))
        {
            Assert.Equal(HttpStatusCode.OK, mapped.StatusCode);
            using var body = JsonDocument.Parse(await mapped.Content.ReadAsStringAsync());
            Assert.Equal(mappedSubject, body.RootElement.GetProperty("subject").GetString());
            Assert.Equal(new[] { "Station Staff" }, body.RootElement.GetProperty("roles").EnumerateArray().Select(role => role.GetString()).ToArray());
        }
        using (var empty = await client.SendAsync(IdentityTestData.Bearer(HttpMethod.Get, "/api/me", oidc.CreateToken("smoke-empty-" + Guid.NewGuid().ToString("N")))))
        {
            Assert.Equal(HttpStatusCode.OK, empty.StatusCode);
            using var body = JsonDocument.Parse(await empty.Content.ReadAsStringAsync());
            Assert.Empty(body.RootElement.GetProperty("roles").EnumerateArray());
        }

        using (var invalid = await client.SendAsync(IdentityTestData.Bearer(HttpMethod.Get, "/api/me", "malformed-token")))
            Assert.Equal(HttpStatusCode.Unauthorized, invalid.StatusCode);
        using (var expired = await client.SendAsync(IdentityTestData.Bearer(HttpMethod.Get, "/api/me", oidc.CreateToken(expires: DateTime.UtcNow.AddMinutes(-2), notBefore: DateTime.UtcNow.AddMinutes(-3)))))
            Assert.Equal(HttpStatusCode.Unauthorized, expired.StatusCode);

        var deniedSubject = "smoke-denied-" + Guid.NewGuid().ToString("N");
        await IdentityTestData.GrantRoleAsync(factory.Services, deniedSubject, RoleCode.Passenger);
        using (var forbidden = await client.SendAsync(IdentityTestData.Bearer(HttpMethod.Post,
            IdentityTestData.ProbeUrl(PermissionAction.PublishOfficialJourneyStatus, ProtectedResourceKind.None), oidc.CreateToken(deniedSubject))))
            Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);

        var scopedSubject = "smoke-scope-down-" + Guid.NewGuid().ToString("N");
        await IdentityTestData.GrantRoleAsync(factory.Services, scopedSubject, RoleCode.StationStaff);
        using (var unavailable = await client.GetAsync(IdentityTestData.ProbeUrl(PermissionAction.ViewOperationsDashboard,
            ProtectedResourceKind.Station, Guid.NewGuid()), oidc.CreateToken(scopedSubject)))
            Assert.Equal(HttpStatusCode.ServiceUnavailable, unavailable.StatusCode);

        string[] publicPaths =
        [
            "/api/stations",
            "/api/routes/20000000-0000-0000-0000-000000000001/stops",
            "/api/services?origin=10000000-0000-0000-0000-000000000001&destination=10000000-0000-0000-0000-000000000002&date=2026-10-17",
            "/api/services/30000000-0000-0000-0000-000000000001"
        ];
        var expiredOptional = oidc.CreateToken(expires: DateTime.UtcNow.AddMinutes(-2), notBefore: DateTime.UtcNow.AddMinutes(-3));
        foreach (var path in publicPaths)
        foreach (var token in new string?[] { null, "malformed-token", expiredOptional })
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, path);
            if (token is not null) request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + token);
            using var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseBody = await response.Content.ReadAsStringAsync();
            Assert.DoesNotContain(token ?? "__never-present__", responseBody, StringComparison.Ordinal);
            if (path == publicPaths[0]) Assert.Contains("Colombo Fort", responseBody, StringComparison.Ordinal);
        }
    }
}
