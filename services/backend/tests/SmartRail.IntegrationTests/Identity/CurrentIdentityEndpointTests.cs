using System.Net;
using System.Text.Json;
using SmartRail.Domain.Identity;
using SmartRail.IntegrationTests.RailwayNetwork;

namespace SmartRail.IntegrationTests.Identity;

public sealed class CurrentIdentityEndpointTests(PostgreSqlTestDatabaseFixture database, LocalOidcFixture oidc) : IClassFixture<PostgreSqlTestDatabaseFixture>, IClassFixture<LocalOidcFixture>
{
    [Fact]
    public async Task Returns_only_verified_subject_and_current_active_external_role_labels()
    {
        using var factory = await IdentityApiTestHost.CreateAsync(database, oidc);
        var subject = "current-" + Guid.NewGuid().ToString("N");
        await IdentityTestData.GrantRoleAsync(factory.Services, subject, RoleCode.StationStaff);
        await IdentityTestData.GrantRoleAsync(factory.Services, subject, RoleCode.Passenger);
        using var client = factory.CreateClient();
        using var response = await client.SendAsync(IdentityTestData.Bearer(HttpMethod.Get, "/api/me",
            oidc.CreateToken(subject, extraClaims: [new("groups", "Controller"), new("email", "private@example.invalid")])));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(subject, json.RootElement.GetProperty("subject").GetString());
        Assert.Equal(new[] { "Passenger", "Station Staff" }, json.RootElement.GetProperty("roles").EnumerateArray().Select(v => v.GetString()).ToArray());
        Assert.Equal(new[] { "roles", "subject" }, json.RootElement.EnumerateObject().Select(p => p.Name).Order().ToArray());
        Assert.DoesNotContain("private@example.invalid", json.RootElement.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("groups", json.RootElement.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Verified_identity_without_mapping_returns_empty_roles_and_invalid_or_unavailable_auth_returns_safe_errors()
    {
        using var factory = await IdentityApiTestHost.CreateAsync(database, oidc);
        using var client = factory.CreateClient();
        using (var empty = await client.SendAsync(IdentityTestData.Bearer(HttpMethod.Get, "/api/me", oidc.CreateToken("unassigned-" + Guid.NewGuid().ToString("N")))))
        {
            Assert.Equal(HttpStatusCode.OK, empty.StatusCode);
            using var body = JsonDocument.Parse(await empty.Content.ReadAsStringAsync());
            Assert.Empty(body.RootElement.GetProperty("roles").EnumerateArray());
        }
        using (var missing = await client.GetAsync("/api/me")) Assert.Equal(HttpStatusCode.Unauthorized, missing.StatusCode);
        using (var invalid = await client.SendAsync(IdentityTestData.Bearer(HttpMethod.Get, "/api/me", "bad"))) Assert.Equal(HttpStatusCode.Unauthorized, invalid.StatusCode);

        using var unavailableFactory = await IdentityApiTestHost.CreateAsync(database, oidc, unavailableRoles: true);
        using var unavailableClient = unavailableFactory.CreateClient();
        using var unavailable = await unavailableClient.SendAsync(IdentityTestData.Bearer(HttpMethod.Get, "/api/me", oidc.CreateToken("authority-down")));
        Assert.Equal(HttpStatusCode.ServiceUnavailable, unavailable.StatusCode);
        Assert.Equal("application/problem+json", unavailable.Content.Headers.ContentType?.MediaType);
    }
}
