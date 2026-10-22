using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi;
using SmartRail.Api.Contracts.Incidents;
using SmartRail.Api.Contracts.RailwayNetwork;
using SmartRail.Api.Errors;
using SmartRail.Application.Incidents.Persistence;
using SmartRail.Application.Incidents.UseCases;

namespace SmartRail.Api.Endpoints.Incidents;

public static class ListIncidentsEndpoint
{
    public static async Task<IResult> HandleAsync(HttpContext context, ListIncidentsHandler handler,
        CancellationToken cancellationToken)
    {
        if (!TryReadOptionalGuid(context, "incidentId", out var incidentId) ||
            !TryReadOptionalGuid(context, "stationId", out var stationId) ||
            !TryReadOptionalGuid(context, "journeyId", out var journeyId))
            return ProblemDetailsConfiguration.Create(StatusCodes.Status400BadRequest,
                "Incident filters must be valid UUIDs.", "invalid_incident_filter");

        var result = await handler.HandleAsync(new ListIncidentsQuery(
            context.User.FindFirstValue("sub"), new IncidentFilters(incidentId, stationId, journeyId)), cancellationToken);
        if (result.Outcome != ListIncidentsOutcome.Listed) return IncidentProblemMapping.ForList(result);
        return Results.Ok(new IncidentListResponse(result.Incidents.Select(IncidentResponse.From).ToArray()));
    }

    public static RouteHandlerBuilder MapGetEndpoint(RouteGroupBuilder group) => group
        .MapGet("", HandleAsync)
        .WithName("ListIncidents")
        .WithSummary("List authorized incident context")
        .WithDescription("Returns only incident projections permitted by current role and station scope. Optional filters are combined conjunctively; an authorized empty result is returned with an empty items array.")
        .Produces<IncidentListResponse>(StatusCodes.Status200OK)
        .Produces<ProblemResponse>(StatusCodes.Status400BadRequest, contentType: "application/problem+json")
        .Produces<ProblemResponse>(StatusCodes.Status401Unauthorized, contentType: "application/problem+json")
        .Produces<ProblemResponse>(StatusCodes.Status403Forbidden, contentType: "application/problem+json")
        .Produces<ProblemResponse>(StatusCodes.Status503ServiceUnavailable, contentType: "application/problem+json")
        .AddOpenApiOperationTransformer((operation, context, _) =>
        {
            operation.Security ??= [];
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("CognitoAccessToken", context.Document)] = []
            });
            operation.Parameters ??= [];
            foreach (var name in new[] { "incidentId", "stationId", "journeyId" })
            {
                operation.Parameters.Add(new OpenApiParameter
                {
                    Name = name,
                    In = ParameterLocation.Query,
                    Required = false,
                    Schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "uuid" }
                });
            }
            return Task.CompletedTask;
        });

    private static bool TryReadOptionalGuid(HttpContext context, string name, out Guid? value)
    {
        value = null;
        if (!context.Request.Query.TryGetValue(name, out var supplied)) return true;
        if (supplied.Count != 1 || !Guid.TryParse(supplied[0], out var parsed) || parsed == Guid.Empty) return false;
        value = parsed;
        return true;
    }
}
