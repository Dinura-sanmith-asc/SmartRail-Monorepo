using System.Net;
using System.Text.Json;
using SmartRail.IntegrationTests.RailwayNetwork;

namespace SmartRail.IntegrationTests.Identity;

public sealed class IdentityOpenApiTests(PostgreSqlTestDatabaseFixture database, LocalOidcFixture oidc) : IClassFixture<PostgreSqlTestDatabaseFixture>, IClassFixture<LocalOidcFixture>
{
    [Fact]
    public async Task Openapi_exposes_approved_feature_routes_and_exact_identity_response_surface()
    {
        using var factory = await IdentityApiTestHost.CreateAsync(database, oidc, installProbe: false, environment: "Development");
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        var paths = root.GetProperty("paths");
        var expectedPaths = new[] { "/api/me", "/api/routes/{id}/stops", "/api/services", "/api/services/{id}", "/api/services/management",
            "/api/services/{id}/schedules", "/api/services/{id}/schedules/{scheduleId}",
            "/api/services/{id}/schedules/{scheduleId}/publish",
            "/api/routes/management", "/api/routes/management/{id}", "/api/stations", "/api/stations/{id}",
            "/api/journeys/{id}", "/api/journeys/{id}/updates", "/api/stations/{id}/departures",
            "/api/operations/journeys/{id}/status", "/api/operations/journeys/{id}/stops/{stopId}",
            "/api/operations/overview",
            "/api/incidents", "/api/incidents/{id}", "/api/driver/assignments",
            "/api/driver/journeys/{id}/observations", "/api/operations/journeys/{id}/observations",
            "/api/catering/options", "/api/catering/orders", "/api/catering/orders/{id}" };
        Assert.Equal(expectedPaths.Order(StringComparer.Ordinal), paths.EnumerateObject().Select(p => p.Name).Order(StringComparer.Ordinal).ToArray());
        Assert.Equal(new[] { "get", "post" }, paths.GetProperty("/api/incidents").EnumerateObject()
            .Select(operation => operation.Name).Order(StringComparer.Ordinal).ToArray());
        Assert.Equal(new[] { "patch" }, paths.GetProperty("/api/incidents/{id}").EnumerateObject()
            .Select(operation => operation.Name).ToArray());
        var me = paths.GetProperty("/api/me").GetProperty("get");
        Assert.Contains(me.GetProperty("security").EnumerateArray(), item => item.TryGetProperty("CognitoAccessToken", out _));
        Assert.True(me.GetProperty("responses").TryGetProperty("200", out var success));
        Assert.True(me.GetProperty("responses").TryGetProperty("401", out _));
        Assert.True(me.GetProperty("responses").TryGetProperty("503", out _));
        var schema = root.GetProperty("components").GetProperty("schemas").GetProperty("CurrentIdentityResponse");
        var roles = schema.GetProperty("properties").GetProperty("roles").GetProperty("items").GetProperty("enum").EnumerateArray().Select(v => v.GetString()).ToArray();
        Assert.Equal(new[] { "Passenger", "Driver", "Station Staff", "Controller" }, roles);
        Assert.True(root.GetProperty("components").GetProperty("securitySchemes").GetProperty("CognitoAccessToken").GetProperty("scheme").GetString() == "bearer");
        Assert.DoesNotContain(paths.EnumerateObject(), p => p.Name.Contains("role", StringComparison.OrdinalIgnoreCase) || p.Name.Contains("scope", StringComparison.OrdinalIgnoreCase));
        Assert.True(success.GetProperty("content").GetProperty("application/json").GetProperty("schema").GetProperty("$ref").GetString()?.EndsWith("CurrentIdentityResponse", StringComparison.Ordinal));
    }
}
