using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SmartRail.Api.Authorization;
using SmartRail.Application.Identity.Authorization;
using SmartRail.Domain.Identity;

namespace SmartRail.IntegrationTests.Identity;

/// <summary>HTTP probe installed only by IdentityApiTestHost, never by application composition.</summary>
public sealed class AuthorizationProbeStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.Use(async (context, downstream) =>
        {
            if (!context.Request.Path.StartsWithSegments("/__test/authorization"))
            {
                await downstream(context);
                return;
            }

            await downstream(context);
            if (context.Response.StatusCode != (int)HttpStatusCode.NotFound) return;
            context.Response.Clear();
            context.Response.ContentType = "application/problem+json";

            var actionText = context.Request.Query["action"].ToString();
            var kindText = context.Request.Query["kind"].ToString();
            var resourceText = context.Request.Query["id"].ToString();
            if (!Enum.TryParse<PermissionAction>(actionText, out var action) || !Enum.TryParse<ProtectedResourceKind>(kindText, out var kind))
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                await JsonSerializer.SerializeAsync(context.Response.Body, new { status = 400, title = "Invalid test probe input." });
                return;
            }

            var resourceId = Guid.TryParse(resourceText, out var parsed) ? parsed : Guid.Empty;
            var resource = kind switch
            {
                ProtectedResourceKind.None => ProtectedResource.None,
                ProtectedResourceKind.Station => ProtectedResource.Station(resourceId),
                ProtectedResourceKind.Journey => ProtectedResource.Journey(resourceId),
                ProtectedResourceKind.Incident => ProtectedResource.Incident(resourceId),
                ProtectedResourceKind.CateringOrder => ProtectedResource.CateringOrder(resourceId),
                _ => ProtectedResource.None
            };
            var requirement = new SmartRailAuthorizationRequirement(action, resource);
            var authorization = context.RequestServices.GetRequiredService<IAuthorizationService>();
            var result = await authorization.AuthorizeAsync(context.User, requirement.Resource, [requirement]);
            var unavailable = result.Failure?.FailureReasons.Any(reason => reason.Message == "smartrail_authorization_unavailable") == true;
            context.Response.StatusCode = result.Succeeded ? (int)HttpStatusCode.OK : unavailable
                ? (int)HttpStatusCode.ServiceUnavailable : (int)HttpStatusCode.Forbidden;
            if (result.Succeeded && context.Request.Method == "POST")
            {
                var counter = context.RequestServices.GetRequiredService<ProbeWriteCounter>();
                counter.Increment();
            }
            await JsonSerializer.SerializeAsync(context.Response.Body, new { status = context.Response.StatusCode });
        });
        next(app);
    };
}

public sealed class ProbeWriteCounter
{
    private int _count;
    public int Count => Volatile.Read(ref _count);
    public void Increment() => Interlocked.Increment(ref _count);
}

public sealed class IdentityTestEvidence : IDriverJourneyAssignmentEvidence, IIncidentResourceEvidence, IPassengerOrderOwnershipEvidence
{
    public EvidenceOutcome DriverOutcome { get; set; } = EvidenceOutcome.VerifiedNoMatch;
    public EvidenceOutcome IncidentOutcome { get; set; } = EvidenceOutcome.VerifiedNoMatch;
    public EvidenceOutcome OrderOutcome { get; set; } = EvidenceOutcome.VerifiedNoMatch;
    public Guid? IncidentStationId { get; set; }
    public Task<EvidenceCheck> CheckAssignmentAsync(string subject, Guid journeyId, CancellationToken cancellationToken = default) => Task.FromResult(new EvidenceCheck(DriverOutcome));
    public Task<IncidentResourceEvidence> ResolveIncidentAsync(Guid incidentId, CancellationToken cancellationToken = default) => Task.FromResult(new IncidentResourceEvidence(IncidentOutcome, IncidentStationId));
    public Task<EvidenceCheck> CheckOwnershipAsync(string subject, Guid orderId, CancellationToken cancellationToken = default) => Task.FromResult(new EvidenceCheck(OrderOutcome));
}
