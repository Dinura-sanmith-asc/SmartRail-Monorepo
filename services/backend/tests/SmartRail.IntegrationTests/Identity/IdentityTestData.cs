using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartRail.Application.Identity.Authorization;
using SmartRail.Domain.Identity;
using SmartRail.Domain.Identity.Entities;
using SmartRail.Domain.RailwayNetwork.Entities;
using SmartRail.Infrastructure.RailwayNetwork;

namespace SmartRail.IntegrationTests.Identity;

internal static class IdentityTestData
{
    public static async Task<Guid> GrantRoleAsync(IServiceProvider services, string subject, RoleCode role)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartRailDbContext>();
        var assignment = new RoleAssignment(Guid.NewGuid(), subject, role, DateTimeOffset.UtcNow, "test-approver");
        db.RoleAssignments.Add(assignment);
        db.RoleAssignmentAudits.Add(RoleAssignmentAudit.ForGrant(Guid.NewGuid(), assignment));
        await db.SaveChangesAsync();
        return assignment.Id;
    }

    public static async Task<Guid> AddStationAndGrantScopeAsync(IServiceProvider services, string subject)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartRailDbContext>();
        var stationId = Guid.NewGuid();
        db.Stations.Add(new Station(stationId, "TEST-" + Guid.NewGuid().ToString("N")[..8], "Identity test station"));
        var assignment = new StationScopeAssignment(Guid.NewGuid(), subject, stationId, DateTimeOffset.UtcNow, "test-approver");
        db.StationScopeAssignments.Add(assignment);
        db.StationScopeAssignmentAudits.Add(StationScopeAssignmentAudit.ForGrant(Guid.NewGuid(), assignment));
        await db.SaveChangesAsync();
        return stationId;
    }

    public static HttpRequestMessage Bearer(HttpMethod method, string url, string token)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    public static string ProbeUrl(PermissionAction action, ProtectedResourceKind kind, Guid? id = null) =>
        $"/__test/authorization?action={action}&kind={kind}&id={id ?? Guid.Empty}";
}
