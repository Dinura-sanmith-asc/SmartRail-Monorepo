using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using SmartRail.IntegrationTests.Identity;
using SmartRail.IntegrationTests.RailwayNetwork;

namespace SmartRail.IntegrationTests.Incidents;

[Collection(IncidentPostgresCollection.Name)]
public sealed class IncidentAssociationOpenApiTests(PostgreSqlTestDatabaseFixture database, LocalOidcFixture oidc)
{
    [Fact]
    public async Task Openapi_describes_only_the_three_approved_incident_operations_and_conditional_association_inputs()
    {
        using var testHost = await IncidentApiTestHost.CreateAsync(database, oidc);
        using var factory = testHost.WithWebHostBuilder(builder => builder.UseEnvironment("Development"));
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        var paths = root.GetProperty("paths");
        var incidentPaths = paths.EnumerateObject()
            .Where(item => item.Name.StartsWith("/api/incidents", StringComparison.Ordinal))
            .ToDictionary(item => item.Name, item => item.Value, StringComparer.Ordinal);
        Assert.Equal(2, incidentPaths.Count);
        Assert.True(incidentPaths["/api/incidents"].TryGetProperty("get", out _));
        Assert.True(incidentPaths["/api/incidents"].TryGetProperty("post", out _));
        Assert.True(incidentPaths["/api/incidents/{id}"].TryGetProperty("patch", out var patch));
        Assert.False(patch.TryGetProperty("put", out _));

        var schemas = root.GetProperty("components").GetProperty("schemas");
        var update = schemas.GetProperty("UpdateIncidentRequest");
        Assert.Equal(new[] { "expectedRevision" }, update.GetProperty("required").EnumerateArray().Select(x => x.GetString()).ToArray());
        Assert.Equal(new[] { "Reported", "In Progress", "Resolved", "Closed" },
            update.GetProperty("properties").GetProperty("state").GetProperty("enum").EnumerateArray().Select(x => x.GetString()).ToArray());
        Assert.True(update.GetProperty("properties").GetProperty("removeAssociationIds").GetProperty("uniqueItems").GetBoolean());

        var association = schemas.GetProperty("IncidentAssociationInput");
        Assert.Equal(new[] { "Station", "Service", "Journey", "Disruption" },
            association.GetProperty("properties").GetProperty("kind").GetProperty("enum").EnumerateArray().Select(x => x.GetString()).ToArray());
        Assert.False(association.GetProperty("additionalProperties").GetBoolean());
    }
}
