using System.Net;
using System.Text.Json;
using SmartRail.Domain.Identity;
using SmartRail.IntegrationTests.Identity;
using SmartRail.IntegrationTests.RailwayNetwork;

namespace SmartRail.IntegrationTests.Incidents;

[Collection(IncidentPostgresCollection.Name)]
public sealed class IncidentReadCheckpointTests(PostgreSqlTestDatabaseFixture database, LocalOidcFixture oidc)
{
    [Fact]
    public async Task Authorized_incident_projection_is_composable_and_public_journey_read_contains_no_incident_data()
    {
        await using var factory = await IncidentApiTestHost.CreateAsync(database, oidc,
            IncidentCreationTestSupport.ConfigureValidTestAuthorities);
        using var client = factory.CreateClient();
        const string subject = "f004-read-checkpoint-" + nameof(Authorized_incident_projection_is_composable_and_public_journey_read_contains_no_incident_data);
        await IncidentCreationTestSupport.GrantRoleAsync(factory.Services, subject, RoleCode.Controller);
        var stationId = await IncidentCreationTestSupport.AddPublishedStationAsync(factory.Services);
        var incidentId = await IncidentReadTestSupport.CreateIncidentAsync(client, oidc, subject, stationId,
            "read-checkpoint-" + Guid.NewGuid());

        using var read = await client.SendAsync(IncidentReadTestSupport.Get(oidc.CreateToken(subject), $"?incidentId={incidentId}"));
        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        using var document = JsonDocument.Parse(await read.Content.ReadAsStringAsync());
        var response = Assert.Single(document.RootElement.EnumerateArray());
        Assert.Equal(incidentId, response.GetProperty("incidentId").GetGuid());
        Assert.Equal("Reported", response.GetProperty("state").GetString());
        Assert.Equal(1, response.GetProperty("revision").GetInt32());
        Assert.Equal("IncidentManagement", response.GetProperty("source").GetString());
        Assert.True(response.GetProperty("reportedAt").GetDateTimeOffset() <= DateTimeOffset.UtcNow);
        Assert.Single(response.GetProperty("associations").EnumerateArray());
        Assert.DoesNotContain("reporterSubject", response.GetRawText(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("auditEvidence", response.GetRawText(), StringComparison.OrdinalIgnoreCase);

        // Feature 003's public read remains anonymous and independent. A missing journey returns only its own safe
        // not-found response; no private incident projection is joined into that public contract.
        using var publicJourney = await client.GetAsync($"/api/journeys/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, publicJourney.StatusCode);
        var publicBody = await publicJourney.Content.ReadAsStringAsync();
        Assert.DoesNotContain("IncidentManagement", publicBody, StringComparison.Ordinal);
        Assert.DoesNotContain("reporterSubject", publicBody, StringComparison.OrdinalIgnoreCase);
    }
}
