using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartRail.Domain.Identity;
using SmartRail.Domain.Identity.Entities;
using SmartRail.Domain.Incidents.Entities;
using SmartRail.Infrastructure.RailwayNetwork;
using SmartRail.IntegrationTests.Identity;

namespace SmartRail.IntegrationTests.Incidents;

internal static class IncidentOwnerTestSupport
{
    public static readonly Guid StationA = Guid.Parse("10000000-0000-0000-0000-000000000001");
    public static readonly Guid StationB = Guid.Parse("10000000-0000-0000-0000-000000000002");
    public static readonly Guid Service = Guid.Parse("30000000-0000-0000-0000-000000000001");

    public static async Task<Guid> GrantRoleAsync(IServiceProvider services, string subject, RoleCode role)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartRailDbContext>();
        var assignment = new RoleAssignment(Guid.NewGuid(), subject, role, DateTimeOffset.UtcNow.AddMinutes(-1), "integration-test-approver");
        db.RoleAssignments.Add(assignment);
        db.RoleAssignmentAudits.Add(RoleAssignmentAudit.ForGrant(Guid.NewGuid(), assignment));
        await db.SaveChangesAsync();
        return assignment.Id;
    }

    public static async Task<Guid> CreateIncidentAsync(HttpClient client, string token, string key,
        IEnumerable<object> associations, string description = "Verified owner-linked report")
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/incidents")
        {
            Content = JsonContent.Create(new
            {
                categoryCode = IncidentTestCategories.TrackObstruction,
                description,
                associations
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Idempotency-Key", key);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("incidentId").GetGuid();
    }

    public static object Link(string kind, Guid id, Guid? updateId = null, int? revision = null) =>
        new { kind, resourceId = id, updateId, revision };

    public static HttpRequestMessage PatchIncident(Guid id, string token, string key, int revision,
        string? state = null, object[]? add = null, Guid[]? remove = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/incidents/{id:D}")
        {
            Content = JsonContent.Create(new { expectedRevision = revision, state, addAssociations = add, removeAssociationIds = remove })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Idempotency-Key", key);
        return request;
    }

    public static async Task<Guid> PublishJourneyChangeAsync(HttpClient client, string token, Guid journeyId,
        int expectedRevision, string state)
    {
        using var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/operations/journeys/{journeyId:D}/status")
        {
            Content = JsonContent.Create(new { expectedJourneyRevision = expectedRevision, status = state })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var updatesResponse = await client.GetAsync($"/api/journeys/{journeyId:D}/updates");
        updatesResponse.EnsureSuccessStatusCode();
        using var updates = JsonDocument.Parse(await updatesResponse.Content.ReadAsStringAsync());
        return updates.RootElement.EnumerateArray().Single(item => item.GetProperty("revision").GetInt32() == expectedRevision + 1)
            .GetProperty("updateId").GetGuid();
    }

    public static async Task<Guid> AddPublishedStationAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartRailDbContext>();
        var id = Guid.NewGuid();
        db.Stations.Add(new SmartRail.Domain.RailwayNetwork.Entities.Station(id,
            "F004-" + Guid.NewGuid().ToString("N")[..10], "Additional fixture station", isPublished: true));
        await db.SaveChangesAsync();
        return id;
    }

    public static async Task<Guid> GrantStationScopeAsync(IServiceProvider services, string subject, Guid stationId)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartRailDbContext>();
        var assignment = new StationScopeAssignment(Guid.NewGuid(), subject, stationId, DateTimeOffset.UtcNow.AddMinutes(-1), "integration-test-approver");
        db.StationScopeAssignments.Add(assignment);
        db.StationScopeAssignmentAudits.Add(StationScopeAssignmentAudit.ForGrant(Guid.NewGuid(), assignment));
        await db.SaveChangesAsync();
        return assignment.Id;
    }
}
