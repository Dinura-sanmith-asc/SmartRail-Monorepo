using System.Net;
using System.Text.Json;
using SmartRail.IntegrationTests.Identity;
using SmartRail.IntegrationTests.RailwayNetwork;

namespace SmartRail.IntegrationTests.DriverOperations;

public sealed class DriverOperationsOpenApiTests(PostgreSqlTestDatabaseFixture database, LocalOidcFixture oidc)
    : IClassFixture<PostgreSqlTestDatabaseFixture>, IClassFixture<LocalOidcFixture>
{
    [Fact]
    public async Task Generated_contract_has_only_three_approved_operations_with_bearer_security_and_safe_schema()
    {
        using var host = await IdentityApiTestHost.CreateAsync(database, oidc, installProbe: false, environment: "Development");
        using var client = host.CreateClient();
        using var response = await client.GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        var paths = root.GetProperty("paths");
        var expected = new Dictionary<string, string>
        {
            ["/api/driver/assignments"] = "get",
            ["/api/driver/journeys/{id}/observations"] = "post",
            ["/api/operations/journeys/{id}/observations"] = "get"
        };
        var actual = paths.EnumerateObject().Where(x => x.Name.StartsWith("/api/driver/", StringComparison.Ordinal) ||
            x.Name.EndsWith("/observations", StringComparison.Ordinal)).Select(x => x.Name).Order(StringComparer.Ordinal);
        Assert.Equal(expected.Keys.Order(StringComparer.Ordinal), actual);
        foreach (var (path, method) in expected)
        {
            var operation = paths.GetProperty(path).GetProperty(method);
            Assert.Contains(operation.GetProperty("security").EnumerateArray(), x => x.TryGetProperty("CognitoAccessToken", out _));
            foreach (var code in new[] { "401", "403", "503" })
                Assert.True(operation.GetProperty("responses").TryGetProperty(code, out _));
        }
        var submit = paths.GetProperty("/api/driver/journeys/{id}/observations").GetProperty("post");
        foreach (var code in new[] { "200", "201", "400", "404", "409" })
            Assert.True(submit.GetProperty("responses").TryGetProperty(code, out _));
        var schemaRef = submit.GetProperty("requestBody").GetProperty("content").GetProperty("application/json").GetProperty("schema").GetProperty("$ref").GetString()!;
        var schema = root.GetProperty("components").GetProperty("schemas").GetProperty(schemaRef.Split('/').Last());
        Assert.Equal(new[] { "requestId", "text" }, schema.GetProperty("properties").EnumerateObject().Select(x => x.Name).Order(StringComparer.Ordinal));
        Assert.Equal(new[] { "requestId", "text" }, schema.GetProperty("required").EnumerateArray().Select(x => x.GetString()).Order(StringComparer.Ordinal));
        Assert.False(schema.GetProperty("additionalProperties").GetBoolean());
        Assert.Equal("uuid", schema.GetProperty("properties").GetProperty("requestId").GetProperty("format").GetString());
        Assert.Equal(1, schema.GetProperty("properties").GetProperty("text").GetProperty("minLength").GetInt32());
        Assert.Equal(4000, schema.GetProperty("properties").GetProperty("text").GetProperty("maxLength").GetInt32());
        foreach (var path in expected.Keys.Where(x => x.Contains("{id}", StringComparison.Ordinal)))
        {
            var operation = paths.GetProperty(path).GetProperty(expected[path]);
            var id = Assert.Single(operation.GetProperty("parameters").EnumerateArray(), x => x.GetProperty("name").GetString() == "id");
            Assert.Equal("uuid", id.GetProperty("schema").GetProperty("format").GetString());
        }
        var schemas = root.GetProperty("components").GetProperty("schemas");
        var observation = schemas.GetProperty("DriverObservationResponse");
        Assert.Equal(new[] { "assignmentId", "driverSubject", "journeyInstanceId", "journeyRevision", "observationId", "reportedAtUtc", "text" },
            observation.GetProperty("required").EnumerateArray().Select(x => x.GetString()).Order(StringComparer.Ordinal));
        Assert.Equal(1, observation.GetProperty("properties").GetProperty("journeyRevision").GetProperty("minimum").GetInt32());
        var context = schemas.GetProperty("DriverJourneyContextResponse").GetProperty("properties");
        Assert.EndsWith("JourneySnapshotResponse", context.GetProperty("snapshot").GetProperty("$ref").GetString());
        Assert.EndsWith("JourneyUpdateResponse", context.GetProperty("updates").GetProperty("items").GetProperty("$ref").GetString());
        Assert.DoesNotContain(paths.EnumerateObject(), x => x.Name.Contains("assignment", StringComparison.OrdinalIgnoreCase) &&
            x.Value.TryGetProperty("post", out _));
    }
}
