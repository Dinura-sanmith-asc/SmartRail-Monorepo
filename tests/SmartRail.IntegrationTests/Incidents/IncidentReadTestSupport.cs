using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartRail.Domain.Identity;
using SmartRail.Domain.Identity.Entities;
using SmartRail.Application.Identity.Persistence;
using SmartRail.Infrastructure.RailwayNetwork;
using SmartRail.IntegrationTests.Identity;

namespace SmartRail.IntegrationTests.Incidents;

internal static class IncidentReadTestSupport
{
    public static async Task RevokeStationScopeAsync(IServiceProvider services, Guid assignmentId)
    {
        await using var scope = services.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<SmartRail.Application.Identity.Persistence.IStationScopeRepository>();
        Assert.True(await repository.RevokeAsync(assignmentId, "test-scope-revoker", DateTimeOffset.UtcNow.AddSeconds(1), Guid.NewGuid()));
    }

    public static async Task<Guid> GrantStationScopeAsync(IServiceProvider services, string subject, Guid stationId)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartRailDbContext>();
        var assignment = new StationScopeAssignment(Guid.NewGuid(), subject, stationId,
            DateTimeOffset.UtcNow.AddSeconds(-1), "test-approver");
        db.StationScopeAssignments.Add(assignment);
        db.StationScopeAssignmentAudits.Add(StationScopeAssignmentAudit.ForGrant(Guid.NewGuid(), assignment));
        await db.SaveChangesAsync();
        return assignment.Id;
    }

    public static async Task RevokeRoleAsync(IServiceProvider services, Guid assignmentId)
    {
        await using var scope = services.CreateAsyncScope();
        var roles = scope.ServiceProvider.GetRequiredService<IRoleAssignmentRepository>();
        Assert.True(await roles.RevokeAsync(assignmentId, "test-revoker", DateTimeOffset.UtcNow.AddSeconds(1), Guid.NewGuid()));
    }

    public static async Task<Guid> CreateIncidentAsync(HttpClient client, LocalOidcFixture oidc, string subject,
        Guid stationId, string key)
    {
        using var response = await client.SendAsync(IncidentCreationTestSupport.Post(stationId.ToString(),
            token: oidc.CreateToken(subject), key: key));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("incidentId").GetGuid();
    }

    public static HttpRequestMessage Get(string token, string query = "")
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/incidents" + query);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    public static HttpRequestMessage Patch(Guid incidentId, int expectedRevision, string state,
        string token, string key, string? extraJson = null)
    {
        var body = extraJson ?? System.Text.Json.JsonSerializer.Serialize(new { expectedRevision, state });
        var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/incidents/{incidentId:D}")
        {
            Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Idempotency-Key", key);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    public static async Task<int> IncidentCountAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<SmartRailDbContext>().Incidents.CountAsync();
    }
}
