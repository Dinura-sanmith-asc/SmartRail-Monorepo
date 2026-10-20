using System.Security.Claims;
using System.Text.Json;
using Microsoft.OpenApi;
using SmartRail.Api.Contracts.DriverOperations;
using SmartRail.Api.Contracts.RailwayNetwork;
using SmartRail.Api.Errors;
using SmartRail.Application.DriverOperations;

namespace SmartRail.Api.Endpoints.DriverOperations;

public static class DriverOperationsEndpoints
{
    public static IEndpointRouteBuilder MapDriverOperationsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        Secure(endpoints.MapGet("/api/driver/assignments", GetAssignmentsAsync)
            .WithName("GetDriverAssignments").WithSummary("Read current assigned duties")
            .Produces<DriverAssignmentListResponse>(200));
        Secure(endpoints.MapPost("/api/driver/journeys/{id}/observations", SubmitAsync)
            .WithName("SubmitDriverObservation").WithSummary("Report non-authoritative journey evidence")
            .Accepts<SubmitDriverObservationRequest>("application/json")
            .Produces<DriverObservationResponse>(201).Produces<DriverObservationResponse>(200)
            .Produces<ProblemResponse>(409, "application/problem+json"));
        Secure(endpoints.MapGet("/api/operations/journeys/{id}/observations", ReviewAsync)
            .WithName("ReviewDriverObservations").WithSummary("Control reads authored observation evidence")
            .Produces<DriverObservationListResponse>(200));
        return endpoints;
    }

    public static async Task<IResult> GetAssignmentsAsync(HttpContext context, DriverOperationsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.GetAssignmentsAsync(context.User.FindFirstValue("sub"), cancellationToken);
        return result.Value is not null && result.Outcome == DriverOutcome.Success
            ? Results.Ok(new DriverAssignmentListResponse(result.Value.Select(DriverAssignmentResponse.From).ToArray())) : Error(result.Outcome);
    }
    public static async Task<IResult> SubmitAsync(string id, HttpContext context, DriverOperationsHandler handler,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(id, out var journeyId) || journeyId == Guid.Empty) return Error(DriverOutcome.Invalid);
        SubmitDriverObservationRequest? input;
        try { input = await context.Request.ReadFromJsonAsync<SubmitDriverObservationRequest>(cancellationToken); }
        catch (JsonException) { return Error(DriverOutcome.Invalid); }
        catch (BadHttpRequestException) { return Error(DriverOutcome.Invalid); }
        if (input is null) return Error(DriverOutcome.Invalid);
        var result = await handler.SubmitObservationAsync(context.User.FindFirstValue("sub"), journeyId,
            input.RequestId, input.Text, cancellationToken);
        return result.Value is not null && result.Outcome is DriverOutcome.Created or DriverOutcome.Success
            ? Results.Json(DriverObservationResponse.From(result.Value), statusCode: result.Outcome == DriverOutcome.Created ? 201 : 200) : Error(result.Outcome);
    }
    public static async Task<IResult> ReviewAsync(string id, HttpContext context, DriverOperationsHandler handler,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(id, out var journeyId) || journeyId == Guid.Empty) return Error(DriverOutcome.Invalid);
        var result = await handler.GetObservationsAsync(context.User.FindFirstValue("sub"), journeyId, cancellationToken);
        return result.Value is not null && result.Outcome == DriverOutcome.Success
            ? Results.Ok(new DriverObservationListResponse(result.Value.Select(DriverObservationResponse.From).ToArray())) : Error(result.Outcome);
    }
    private static IResult Error(DriverOutcome outcome) => outcome switch
    {
        DriverOutcome.Invalid => ProblemDetailsConfiguration.Create(400, "The driver request is invalid.", "invalid_driver_request"),
        DriverOutcome.Unauthorized => ProblemDetailsConfiguration.Create(401, "Authentication is required.", "authentication_required"),
        DriverOutcome.Forbidden => ProblemDetailsConfiguration.Create(403, "The required current permission or assignment is denied.", "permission_denied"),
        DriverOutcome.NotFound => ProblemDetailsConfiguration.Create(404, "The requested journey was not found or is inaccessible.", "journey_not_found"),
        DriverOutcome.Conflict => ProblemDetailsConfiguration.Create(409, "The request identifier conflicts with an earlier submission.", "driver_request_conflict"),
        _ => ProblemDetailsConfiguration.Create(503, "Required driver or railway authority is temporarily unavailable.", "authority_unavailable")
    };
    private static RouteHandlerBuilder Secure(RouteHandlerBuilder builder) => builder.RequireAuthorization()
        .Produces<ProblemResponse>(400, "application/problem+json")
        .Produces<ProblemResponse>(401, "application/problem+json")
        .Produces<ProblemResponse>(403, "application/problem+json")
        .Produces<ProblemResponse>(404, "application/problem+json")
        .Produces<ProblemResponse>(503, "application/problem+json")
        .AddOpenApiOperationTransformer((operation, context, _) =>
        {
            if (operation.Parameters is not null)
                foreach (var parameter in operation.Parameters)
                    if (parameter is OpenApiParameter concrete && parameter.Name == "id" && parameter.In == ParameterLocation.Path)
                        concrete.Schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "uuid" };
            operation.Security ??= [];
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("CognitoAccessToken", context.Document)] = []
            });
            return Task.CompletedTask;
        });
}
