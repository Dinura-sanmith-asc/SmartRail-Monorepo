using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi;
using SmartRail.Api.Contracts.Incidents;
using SmartRail.Api.Contracts.RailwayNetwork;
using SmartRail.Api.Errors;
using SmartRail.Application.Incidents.UseCases;

namespace SmartRail.Api.Endpoints.Incidents;

public static class UpdateIncidentEndpoint
{
    public static async Task<IResult> HandleAsync(Guid id, HttpContext context, UpdateIncidentHandler handler,
        CancellationToken cancellationToken)
    {
        UpdateIncidentRequest? request;
        try { request = await context.Request.ReadFromJsonAsync<UpdateIncidentRequest>(cancellationToken); }
        catch (JsonException)
        {
            return ProblemDetailsConfiguration.Create(StatusCodes.Status400BadRequest,
                "The incident update body is malformed.", "invalid_incident_update");
        }
        catch (BadHttpRequestException)
        {
            return ProblemDetailsConfiguration.Create(StatusCodes.Status400BadRequest,
                "The incident update body is malformed.", "invalid_incident_update");
        }

        var subject = context.User.FindFirstValue("sub");
        var result = await handler.HandleAsync(new UpdateIncidentCommand(id, subject,
            context.Request.Headers.TryGetValue("Idempotency-Key", out var key) ? key.ToString() : null,
            request?.ToInput()), cancellationToken);
        if (result.Incident is null) return IncidentProblemMapping.ForUpdate(result);
        return Results.Ok(IncidentResponse.From(result.Incident));
    }

    public static RouteHandlerBuilder MapPatchEndpoint(RouteGroupBuilder group) => group
        .MapPatch("/{id:guid}", HandleAsync)
        .WithName("UpdateIncident")
        .WithSummary("Update incident lifecycle")
        .WithDescription("Applies a forward lifecycle transition against the expected incident revision; repeated requests are idempotent after current authorization is rechecked.")
        .Accepts<UpdateIncidentRequest>("application/json")
        .Produces<IncidentResponse>(StatusCodes.Status200OK)
        .Produces<ProblemResponse>(StatusCodes.Status400BadRequest, contentType: "application/problem+json")
        .Produces<ProblemResponse>(StatusCodes.Status401Unauthorized, contentType: "application/problem+json")
        .Produces<ProblemResponse>(StatusCodes.Status403Forbidden, contentType: "application/problem+json")
        .Produces<ProblemResponse>(StatusCodes.Status404NotFound, contentType: "application/problem+json")
        .Produces<ProblemResponse>(StatusCodes.Status409Conflict, contentType: "application/problem+json")
        .Produces<ProblemResponse>(StatusCodes.Status503ServiceUnavailable, contentType: "application/problem+json")
        .AddOpenApiOperationTransformer((operation, context, _) =>
        {
            operation.Security ??= [];
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("CognitoAccessToken", context.Document)] = []
            });
            operation.Parameters ??= [];
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "Idempotency-Key", In = ParameterLocation.Header, Required = true,
                Schema = new OpenApiSchema { Type = JsonSchemaType.String, MinLength = 1, MaxLength = 200 }
            });
            return Task.CompletedTask;
        });
}
