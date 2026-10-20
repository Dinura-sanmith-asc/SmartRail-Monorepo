using System.Text.Json;
using SmartRail.IntegrationTests.RailwayNetwork;

namespace SmartRail.IntegrationTests.RailwayNetwork.Api;

public sealed class RailwayNetworkOpenApiTests(PostgreSqlTestDatabaseFixture fixture) : IClassFixture<PostgreSqlTestDatabaseFixture>
{
    [Fact]
    public async Task Generated_openapi_preserves_railway_contract_and_exposes_approved_identity_and_journey_paths()
    {
        using var factory = await RailwayNetworkApiTestHost.CreateAsync(fixture);
        using var client = factory.CreateClient();
        using var document = JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json"));
        var paths = document.RootElement.GetProperty("paths");
        var repository = new DirectoryInfo(AppContext.BaseDirectory);
        while (repository is not null && !Directory.Exists(Path.Combine(repository.FullName, "specs", "001-railway-network-search")))
            repository = repository.Parent;
        Assert.NotNull(repository);
        var approvedYaml = await File.ReadAllTextAsync(Path.Combine(repository!.FullName,
            "specs", "001-railway-network-search", "contracts", "railway-network.openapi.yaml"));

        var expectedRailwayPaths = new[] { "/api/stations", "/api/routes/{id}/stops", "/api/services", "/api/services/{id}" };
        var expectedFeaturePaths = expectedRailwayPaths.Concat(new[] { "/api/me", "/api/journeys/{id}",
            "/api/journeys/{id}/updates", "/api/stations/{id}/departures",
            "/api/operations/journeys/{id}/status", "/api/operations/journeys/{id}/stops/{stopId}",
            "/api/operations/overview",
            "/api/incidents", "/api/incidents/{id}", "/api/driver/assignments",
            "/api/driver/journeys/{id}/observations", "/api/operations/journeys/{id}/observations" }).ToArray();
        var generatedPaths = paths.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal).ToArray();
        Assert.Equal(expectedFeaturePaths.Order(StringComparer.Ordinal), generatedPaths);
        foreach (var path in expectedRailwayPaths)
        {
            Assert.Contains(path, approvedYaml, StringComparison.Ordinal);
            Assert.True(paths.GetProperty(path).TryGetProperty("get", out _));
        }

        Assert.False(paths.GetProperty("/api/stations").TryGetProperty("post", out _));
        var search = paths.GetProperty("/api/services").GetProperty("get");
        var parameters = search.GetProperty("parameters").EnumerateArray()
            .Select(item => item.GetProperty("name").GetString()!) .ToHashSet(StringComparer.Ordinal);
        Assert.Equal(new[] { "origin", "destination", "date" }.ToHashSet(StringComparer.Ordinal), parameters);
        foreach (var parameter in search.GetProperty("parameters").EnumerateArray())
        {
            Assert.True(parameter.GetProperty("required").GetBoolean());
            var expectedFormat = parameter.GetProperty("name").GetString() == "date" ? "date" : "uuid";
            Assert.Equal(expectedFormat, parameter.GetProperty("schema").GetProperty("format").GetString());
        }
        Assert.Contains("400", search.GetProperty("responses").EnumerateObject().Select(item => item.Name));
        Assert.Contains("503", search.GetProperty("responses").EnumerateObject().Select(item => item.Name));
        var stationSchema = ResolveSchema(document.RootElement, paths.GetProperty("/api/stations").GetProperty("get")
            .GetProperty("responses").GetProperty("200").GetProperty("content").GetProperty("application/json").GetProperty("schema"));
        Assert.Contains("items", stationSchema.GetProperty("properties").EnumerateObject().Select(item => item.Name));
        var station = ResolveSchema(document.RootElement,
            stationSchema.GetProperty("properties").GetProperty("items").GetProperty("items"));
        AssertContainsProperties(station, "stationId", "name", "locationDetails", "serviceDetails");
        var routeSchema = ResolveSchema(document.RootElement, paths.GetProperty("/api/routes/{id}/stops").GetProperty("get")
            .GetProperty("responses").GetProperty("200").GetProperty("content").GetProperty("application/json").GetProperty("schema"));
        var routeOperation = paths.GetProperty("/api/routes/{id}/stops").GetProperty("get");
        Assert.Contains("400", routeOperation.GetProperty("responses").EnumerateObject().Select(item => item.Name));
        Assert.Contains("404", routeOperation.GetProperty("responses").EnumerateObject().Select(item => item.Name));
        Assert.Contains("503", routeOperation.GetProperty("responses").EnumerateObject().Select(item => item.Name));
        Assert.Contains("stops", routeSchema.GetProperty("properties").EnumerateObject().Select(item => item.Name));
        var routeStop = ResolveSchema(document.RootElement, routeSchema.GetProperty("properties").GetProperty("stops").GetProperty("items"));
        AssertContainsProperties(routeStop, "stopOrder", "stationId", "stationName");
        var searchSchema = ResolveSchema(document.RootElement, search.GetProperty("responses").GetProperty("200")
            .GetProperty("content").GetProperty("application/json").GetProperty("schema"));
        Assert.Contains("services", searchSchema.GetProperty("properties").EnumerateObject().Select(item => item.Name));
        var choice = ResolveSchema(document.RootElement, searchSchema.GetProperty("properties").GetProperty("services").GetProperty("items"));
        AssertContainsProperties(choice, "serviceId", "serviceCode", "routeId", "routeCode", "scheduleRevision", "origin", "destination");
        var scheduledCall = ResolveSchema(document.RootElement, choice.GetProperty("properties").GetProperty("origin"));
        AssertContainsProperties(scheduledCall, "stationId", "stationName", "stopOrder", "scheduledAt");
        var serviceOperation = paths.GetProperty("/api/services/{id}").GetProperty("get");
        Assert.Contains("400", serviceOperation.GetProperty("responses").EnumerateObject().Select(item => item.Name));
        Assert.Contains("404", serviceOperation.GetProperty("responses").EnumerateObject().Select(item => item.Name));
        Assert.Contains("503", serviceOperation.GetProperty("responses").EnumerateObject().Select(item => item.Name));
        var serviceSchema = ResolveSchema(document.RootElement, serviceOperation
            .GetProperty("responses").GetProperty("200").GetProperty("content").GetProperty("application/json").GetProperty("schema"));
        AssertContainsProperties(serviceSchema, "serviceId", "serviceCode", "route", "schedule");
        foreach (var field in new[] { "stationId", "stationName", "stopOrder", "scheduledAt", "serviceId", "serviceCode", "scheduleRevision" })
            Assert.Contains(field, approvedYaml, StringComparison.Ordinal);
        Assert.Contains("format: uuid", approvedYaml, StringComparison.Ordinal);
        Assert.Contains("format: date", approvedYaml, StringComparison.Ordinal);
        Assert.DoesNotContain("sourceStationKey", approvedYaml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sourceStationKey", document.RootElement.GetRawText(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("liveStatus", document.RootElement.GetRawText(), StringComparison.OrdinalIgnoreCase);
    }

    private static JsonElement ResolveSchema(JsonElement document, JsonElement schema)
    {
        if (!schema.TryGetProperty("$ref", out var reference)) return schema;
        var parts = reference.GetString()!.TrimStart('#', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        var result = document;
        foreach (var part in parts) result = result.GetProperty(part);
        return result;
    }

    private static void AssertContainsProperties(JsonElement schema, params string[] expected) =>
        Assert.Subset(expected.ToHashSet(StringComparer.Ordinal),
            schema.GetProperty("properties").EnumerateObject().Select(item => item.Name).ToHashSet(StringComparer.Ordinal));
}
