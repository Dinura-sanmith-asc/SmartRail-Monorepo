using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using SmartRail.IntegrationTests.Identity;
using SmartRail.IntegrationTests.RailwayNetwork;

namespace SmartRail.IntegrationTests.Incidents;

[Collection(IncidentPostgresCollection.Name)]
public sealed class IncidentOpenApiContractTests(PostgreSqlTestDatabaseFixture database, LocalOidcFixture oidc)
{
    private static readonly IReadOnlyDictionary<string, string[]> Operations = new Dictionary<string, string[]>(StringComparer.Ordinal)
    {
        ["/api/incidents"] = ["get", "post"],
        ["/api/incidents/{id}"] = ["patch"]
    };

    [Fact]
    public async Task Generated_openapi_matches_approved_incident_contract_version_schemas_security_and_errors()
    {
        using var testHost = await IncidentApiTestHost.CreateAsync(database, oidc);
        using var factory = testHost.WithWebHostBuilder(builder => builder.UseEnvironment("Development"));
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        Assert.Equal("1.0.0", root.GetProperty("info").GetProperty("version").GetString());

        var contract = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "incident-management.openapi.yaml"));
        var normalizedContract = contract.Replace("\r\n", "\n", StringComparison.Ordinal);
        Assert.Contains("version: 1.0.0", normalizedContract, StringComparison.Ordinal);
        var contractPaths = Regex.Matches(normalizedContract, "(?m)^  (?<path>/api/incidents[^:]*):$")
            .Select(match => match.Groups["path"].Value).Order(StringComparer.Ordinal).ToArray();
        Assert.Equal(Operations.Keys.Order(StringComparer.Ordinal), contractPaths);
        Assert.Contains("operationId: listIncidents", normalizedContract, StringComparison.Ordinal);
        Assert.Contains("operationId: createIncident", normalizedContract, StringComparison.Ordinal);
        Assert.Contains("operationId: updateIncident", normalizedContract, StringComparison.Ordinal);

        var paths = root.GetProperty("paths");
        var incidentPaths = paths.EnumerateObject().Where(item => item.Name.StartsWith("/api/incidents", StringComparison.Ordinal))
            .ToDictionary(item => item.Name, item => item.Value, StringComparer.Ordinal);
        Assert.Equal(Operations.Keys.Order(StringComparer.Ordinal), incidentPaths.Keys.Order(StringComparer.Ordinal));
        foreach (var (path, methods) in Operations)
        {
            Assert.Equal(methods.Order(StringComparer.Ordinal), incidentPaths[path].EnumerateObject()
                .Select(item => item.Name).Order(StringComparer.Ordinal));
            Assert.Contains(path + ":", normalizedContract, StringComparison.Ordinal);
        }

        var get = incidentPaths["/api/incidents"].GetProperty("get");
        var post = incidentPaths["/api/incidents"].GetProperty("post");
        var patch = incidentPaths["/api/incidents/{id}"].GetProperty("patch");
        AssertOperation(get, ["200", "400", "401", "403", "503"]);
        AssertOperation(post, ["201", "400", "401", "403", "404", "409", "503"]);
        AssertOperation(patch, ["200", "400", "401", "403", "404", "409", "503"]);

        var schemes = root.GetProperty("components").GetProperty("securitySchemes");
        var bearer = schemes.GetProperty("CognitoAccessToken");
        Assert.Equal("http", bearer.GetProperty("type").GetString());
        Assert.Equal("bearer", bearer.GetProperty("scheme").GetString());
        foreach (var operation in new[] { get, post, patch })
            Assert.Contains(operation.GetProperty("security").EnumerateArray(), item => item.TryGetProperty("CognitoAccessToken", out _));

        var getParameters = get.GetProperty("parameters").EnumerateArray().ToDictionary(
            item => item.GetProperty("name").GetString()!, item => item, StringComparer.Ordinal);
        Assert.Equal(new[] { "incidentId", "journeyId", "stationId" }, getParameters.Keys.Order(StringComparer.Ordinal));
        foreach (var parameter in getParameters.Values)
        {
            Assert.Equal("query", parameter.GetProperty("in").GetString());
            Assert.Equal("uuid", parameter.GetProperty("schema").GetProperty("format").GetString());
            Assert.False(parameter.TryGetProperty("required", out var required) && required.GetBoolean());
        }

        var components = root.GetProperty("components");
        var schemas = components.GetProperty("schemas");
        var create = schemas.GetProperty("CreateIncidentRequest");
        Assert.Equal(new[] { "associations", "categoryCode", "description" },
            create.GetProperty("required").EnumerateArray().Select(item => item.GetString()).Order(StringComparer.Ordinal));
        Assert.False(create.GetProperty("additionalProperties").GetBoolean());
        AssertProperties(create, "categoryCode", "description", "occurredAt", "associations");

        var update = schemas.GetProperty("UpdateIncidentRequest");
        Assert.Equal(new[] { "expectedRevision" }, update.GetProperty("required").EnumerateArray().Select(item => item.GetString()));
        AssertProperties(update, "expectedRevision", "state", "addAssociations", "removeAssociationIds");
        var incident = schemas.GetProperty("IncidentResponse");
        AssertProperties(incident, "incidentId", "categoryCode", "description", "state", "revision", "source", "reportedAt", "occurredAt", "associations");
        Assert.DoesNotContain("reporterSubject", incident.GetProperty("properties").EnumerateObject().Select(item => item.Name));
        Assert.DoesNotContain("privateAuditEvidence", incident.GetProperty("properties").EnumerateObject().Select(item => item.Name));

        var getSchema = get.GetProperty("responses").GetProperty("200").GetProperty("content")
            .GetProperty("application/json").GetProperty("schema");
        if (getSchema.TryGetProperty("$ref", out var getSchemaReference))
            getSchema = ResolveSchema(root, getSchemaReference.GetString()!);
        Assert.Equal("object", getSchema.GetProperty("type").GetString());
        Assert.Contains("items", getSchema.GetProperty("required").EnumerateArray().Select(item => item.GetString()));
        Assert.Equal("array", getSchema.GetProperty("properties").GetProperty("items").GetProperty("type").GetString());
        Assert.Contains("IncidentResponse", getSchema.GetProperty("properties").GetProperty("items")
            .GetProperty("items").GetProperty("$ref").GetString(), StringComparison.Ordinal);
        Assert.Contains("Incident", post.GetProperty("responses").GetProperty("201").GetProperty("content")
            .GetProperty("application/json").GetProperty("schema").GetProperty("$ref").GetString(), StringComparison.Ordinal);
        Assert.Contains("Idempotency-Key", normalizedContract, StringComparison.Ordinal);
        foreach (var code in new[] { "400", "401", "403", "404", "409", "503" })
            Assert.Contains($"'{code}'", normalizedContract, StringComparison.Ordinal);
    }

    private static void AssertOperation(JsonElement operation, string[] expectedStatuses)
    {
        Assert.Equal(expectedStatuses.Order(StringComparer.Ordinal), operation.GetProperty("responses").EnumerateObject()
            .Select(item => item.Name).Order(StringComparer.Ordinal));
        Assert.Contains(operation.GetProperty("security").EnumerateArray(), item => item.TryGetProperty("CognitoAccessToken", out _));
    }

    private static void AssertProperties(JsonElement schema, params string[] expected) =>
        Assert.Subset(expected.ToHashSet(StringComparer.Ordinal),
            schema.GetProperty("properties").EnumerateObject().Select(item => item.Name).ToHashSet(StringComparer.Ordinal));

    private static JsonElement ResolveSchema(JsonElement document, string reference)
    {
        var result = document;
        foreach (var part in reference.TrimStart('#', '/').Split('/', StringSplitOptions.RemoveEmptyEntries))
            result = result.GetProperty(part);
        return result;
    }
}
