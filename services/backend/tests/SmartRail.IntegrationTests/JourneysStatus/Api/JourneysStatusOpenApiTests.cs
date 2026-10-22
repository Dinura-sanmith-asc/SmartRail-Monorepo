using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using SmartRail.IntegrationTests.Identity;
using SmartRail.IntegrationTests.RailwayNetwork;

namespace SmartRail.IntegrationTests.JourneysStatus.Api;

public sealed class JourneysStatusOpenApiTests(
    PostgreSqlTestDatabaseFixture database,
    LocalOidcFixture oidc) : IClassFixture<PostgreSqlTestDatabaseFixture>, IClassFixture<LocalOidcFixture>
{
    private static readonly IReadOnlyDictionary<string, string> ApprovedOperations = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["/api/journeys/{id}"] = "get",
        ["/api/journeys/{id}/updates"] = "get",
        ["/api/stations/{id}/departures"] = "get",
        ["/api/operations/journeys/{id}/status"] = "patch",
        ["/api/operations/journeys/{id}/stops/{stopId}"] = "patch"
    };

    [Fact]
    public async Task Generated_openapi_matches_the_five_approved_journeys_status_operations()
    {
        using var factory = await IdentityApiTestHost.CreateAsync(database, oidc, installProbe: false,
            environment: "Development", seedRailway: true);
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        Assert.Equal("1.0.0", root.GetProperty("info").GetProperty("version").GetString());
        var paths = root.GetProperty("paths");
        var actualFeaturePaths = paths.EnumerateObject().Select(property => property.Name)
            .Where(path => path.StartsWith("/api/journeys/", StringComparison.Ordinal) ||
                           (path.StartsWith("/api/operations/journeys/", StringComparison.Ordinal) && !path.EndsWith("/observations", StringComparison.Ordinal)) ||
                           path == "/api/stations/{id}/departures")
            .Order(StringComparer.Ordinal).ToArray();
        Assert.Equal(ApprovedOperations.Keys.Order(StringComparer.Ordinal), actualFeaturePaths);

        var contract = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "journeys-status.openapi.yaml"));
        var normalizedContract = contract.Replace("\r\n", "\n", StringComparison.Ordinal);
        var contractPaths = Regex.Matches(normalizedContract, "(?m)^  (?<path>/api/[^:]+):$")
            .Select(match => match.Groups["path"].Value).Order(StringComparer.Ordinal).ToArray();
        Assert.Equal(ApprovedOperations.Keys.Order(StringComparer.Ordinal), contractPaths);

        foreach (var (path, method) in ApprovedOperations)
        {
            Assert.Contains($"  {path}:\n    {method}:", normalizedContract, StringComparison.Ordinal);
            var item = paths.GetProperty(path);
            Assert.True(item.TryGetProperty(method, out var operation));
            Assert.DoesNotContain(item.EnumerateObject(), property => property.Name is "post" or "put" or "delete");

            if (method == "get")
                Assert.False(operation.TryGetProperty("security", out var security) && security.GetArrayLength() > 0);
            else
                Assert.Contains(operation.GetProperty("security").EnumerateArray(), entry =>
                    entry.TryGetProperty("CognitoAccessToken", out _));
        }

        Assert.Contains("global journey scope", paths.GetProperty("/api/operations/journeys/{id}/status")
            .GetProperty("patch").GetProperty("description").GetString(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ConfirmJourneyCancellation", paths.GetProperty("/api/operations/journeys/{id}/status")
            .GetProperty("patch").GetProperty("description").GetString(), StringComparison.Ordinal);

        var departures = paths.GetProperty("/api/stations/{id}/departures").GetProperty("get");
        var date = departures.GetProperty("parameters").EnumerateArray()
            .Single(parameter => parameter.GetProperty("name").GetString() == "date");
        Assert.True(date.GetProperty("required").GetBoolean());
        Assert.Equal("date", date.GetProperty("schema").GetProperty("format").GetString());

        AssertResponseStatuses(paths.GetProperty("/api/journeys/{id}").GetProperty("get"), "200", "400", "404", "503");
        AssertResponseStatuses(paths.GetProperty("/api/journeys/{id}/updates").GetProperty("get"), "200", "400", "404", "503");
        AssertResponseStatuses(departures, "200", "400", "404", "503");
        AssertResponseStatuses(paths.GetProperty("/api/operations/journeys/{id}/status").GetProperty("patch"),
            "200", "400", "401", "403", "404", "409", "503");
        AssertResponseStatuses(paths.GetProperty("/api/operations/journeys/{id}/stops/{stopId}").GetProperty("patch"),
            "200", "400", "401", "403", "404", "409", "503");

        var schemas = root.GetProperty("components").GetProperty("schemas");
        var snapshotSchema = schemas.GetProperty("JourneySnapshotResponse");
        Assert.Contains("journeyInstanceId", snapshotSchema.GetProperty("properties").EnumerateObject().Select(property => property.Name));
        Assert.Contains("revision", snapshotSchema.GetProperty("properties").EnumerateObject().Select(property => property.Name));
        Assert.Contains("stops", snapshotSchema.GetProperty("properties").EnumerateObject().Select(property => property.Name));
        var stopProperties = schemas.GetProperty("JourneyStopResponse").GetProperty("properties");
        Assert.Contains("stationName", stopProperties.EnumerateObject().Select(property => property.Name));
        Assert.Equal("string", stopProperties.GetProperty("stationName").GetProperty("type").GetString());
        var departureProperties = schemas.GetProperty("StationDepartureResponse").GetProperty("properties");
        Assert.Equal("string", departureProperties.GetProperty("stationName").GetProperty("type").GetString());
        Assert.Contains("JourneySnapshot", normalizedContract, StringComparison.Ordinal);
        Assert.Contains("JourneyUpdate", normalizedContract, StringComparison.Ordinal);
        Assert.Contains("StationDeparture", normalizedContract, StringComparison.Ordinal);
        Assert.Contains("UpdateId", normalizedContract, StringComparison.Ordinal);
    }

    private static void AssertResponseStatuses(JsonElement operation, params string[] expected)
    {
        var actual = operation.GetProperty("responses").EnumerateObject().Select(response => response.Name)
            .Order(StringComparer.Ordinal).ToArray();
        Assert.Equal(expected.Order(StringComparer.Ordinal), actual);
    }
}
