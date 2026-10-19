using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi;
using SmartRail.Api.Contracts.Incidents;
using SmartRail.Api.Contracts.RailwayNetwork;
using SmartRail.Api.Errors;
using SmartRail.Application.Incidents.UseCases;

namespace SmartRail.Api.Endpoints.Incidents;

public static class CreateIncidentEndpoint
{
    public static async Task<IResult> HandleAsync(HttpContext context, CreateIncidentHandler handler, CancellationToken cancellationToken)
    {
        CreateIncidentRequest? request;
        try
        {
            request = await context.Request.ReadFromJsonAsync<CreateIncidentRequest>(cancellationToken);
        }
        catch (JsonException)
        {
            return ProblemDetailsConfiguration.Create(StatusCodes.Status400BadRequest,
                "The incident request body is malformed.", "invalid_incident_request");
        }
        catch (BadHttpRequestException)
        {
            return ProblemDetailsConfiguration.Create(StatusCodes.Status400BadRequest,
                "The incident request body is malformed.", "invalid_incident_request");
        }

        var subject = context.User.FindFirstValue("sub");
        var result = await handler.HandleAsync(new CreateIncidentCommand(subject,
            context.Request.Headers.TryGetValue("Idempotency-Key", out var key) ? key.ToString() : null,
            request?.ToInput()), cancellationToken);
        if (result.Incident is null) return IncidentProblemMapping.ForCreate(result);

        return Results.Json(IncidentResponse.From(result.Incident), statusCode: StatusCodes.Status201Created);
    }

    public static RouteHandlerBuilder MapPostEndpoint(RouteGroupBuilder group) => group
        .MapPost("", HandleAsync)
        .WithName("CreateIncident")
        .WithSummary("Report a railway incident")
        .WithDescription("Creates an incident after current role/scope, controlled category and owner references are verified. Test-only categories are isolated from production configuration.")
        .Accepts<CreateIncidentRequest>("application/json")
        .Produces<IncidentResponse>(StatusCodes.Status201Created)
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
                Name = "Idempotency-Key",
                In = ParameterLocation.Header,
                Required = true,
                Schema = new OpenApiSchema { Type = JsonSchemaType.String, MinLength = 1, MaxLength = 200 }
            });
            return Task.CompletedTask;
        });
}
