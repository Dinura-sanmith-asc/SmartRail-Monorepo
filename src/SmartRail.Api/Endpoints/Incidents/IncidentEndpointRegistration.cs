namespace SmartRail.Api.Endpoints.Incidents;

/// <summary>Feature 004's incremental registration point; later phases append only approved operations.</summary>
public static class IncidentEndpointRegistration
{
    public static IEndpointRouteBuilder MapIncidentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/incidents")
            .WithTags("Incidents")
            .RequireAuthorization();
        CreateIncidentEndpoint.MapPostEndpoint(group);
        return endpoints;
    }
}
