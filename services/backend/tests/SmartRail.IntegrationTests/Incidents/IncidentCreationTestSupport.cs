using System.Net.Http.Headers;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SmartRail.Application.Incidents.Categories;
using SmartRail.Application.Incidents.References;
using SmartRail.Domain.Identity;
using SmartRail.Domain.Identity.Entities;
using SmartRail.Domain.RailwayNetwork.Entities;
using SmartRail.Infrastructure.Incidents.Categories;
using SmartRail.Infrastructure.RailwayNetwork;
using SmartRail.IntegrationTests.Identity;

namespace SmartRail.IntegrationTests.Incidents;

internal static class IncidentCreationTestSupport
{
    public static void ConfigureValidTestAuthorities(IServiceCollection services)
    {
        services.Configure<IncidentCategoryCatalogOptions>(options => options.Codes = IncidentTestCategories.All.ToArray());
        ConfigureTestOwnerVerifier(services);
    }

    public static void ConfigureTestOwnerVerifier(IServiceCollection services)
    {
        services.RemoveAll<IFeature001IncidentReferenceVerifier>();
        services.AddScoped<IFeature001IncidentReferenceVerifier, TestFeature001ReferenceVerifier>();
    }

    public static async Task<Guid> AddPublishedStationAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartRailDbContext>();
        var id = Guid.NewGuid();
        db.Stations.Add(new Station(id, "F004-" + Guid.NewGuid().ToString("N")[..12], "Incident creation test station", isPublished: true));
        await db.SaveChangesAsync();
        return id;
    }

    public static async Task<Guid> AddPublishedServiceAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartRailDbContext>();
        var routeId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        db.Routes.Add(new Route(routeId, "F004-" + Guid.NewGuid().ToString("N")[..10], "Incident test route", isPublished: true));
        db.TrainServices.Add(new TrainService(serviceId, "F004-" + Guid.NewGuid().ToString("N")[..10], routeId, isPublished: true));
        await db.SaveChangesAsync();
        return serviceId;
    }

    public static async Task<Guid> GrantRoleAsync(IServiceProvider services, string subject, RoleCode role)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartRailDbContext>();
        var assignment = new RoleAssignment(Guid.NewGuid(), subject, role, DateTimeOffset.UtcNow.AddSeconds(-1), "test-approver");
        db.RoleAssignments.Add(assignment);
        db.RoleAssignmentAudits.Add(RoleAssignmentAudit.ForGrant(Guid.NewGuid(), assignment));
        await db.SaveChangesAsync();
        return assignment.Id;
    }

    public static HttpRequestMessage Post(string stationId, string category = IncidentTestCategories.TrackObstruction,
        string description = "Track obstruction reported", string? token = null, string key = "incident-create-test-key")
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/incidents")
        {
            Content = new StringContent($"{{\"categoryCode\":\"{category}\",\"description\":\"{description}\",\"associations\":[{{\"kind\":\"Station\",\"resourceId\":\"{stationId}\"}}]}}", Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Idempotency-Key", key);
        if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    public static HttpRequestMessage PostService(string serviceId, string category = IncidentTestCategories.TrackObstruction,
        string description = "Service incident reported", string? token = null, string key = "incident-create-service-key")
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/incidents")
        {
            Content = new StringContent($"{{\"categoryCode\":\"{category}\",\"description\":\"{description}\",\"associations\":[{{\"kind\":\"Service\",\"resourceId\":\"{serviceId}\"}}]}}", Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Idempotency-Key", key);
        if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    private sealed class TestFeature001ReferenceVerifier(SmartRailDbContext db) : IFeature001IncidentReferenceVerifier
    {
        public async Task<IncidentReferenceCheck> VerifyAsync(IncidentReference reference, CancellationToken cancellationToken = default)
        {
            var exists = reference.Kind switch
            {
                IncidentReferenceKind.Station => await db.Stations.AsNoTracking().AnyAsync(x => x.StationId == reference.ResourceId && x.IsPublished, cancellationToken),
                IncidentReferenceKind.Service => await db.TrainServices.AsNoTracking().AnyAsync(x => x.ServiceId == reference.ResourceId && x.IsPublished, cancellationToken),
                _ => false
            };
            return new IncidentReferenceCheck(exists ? IncidentReferenceOutcome.Verified : IncidentReferenceOutcome.MissingOrInaccessible);
        }
    }
}
