using System.Globalization;
using System.Security.Claims;
using Microsoft.OpenApi;
using SmartRail.Api.Contracts.Operations;
using SmartRail.Api.Contracts.RailwayNetwork;
using SmartRail.Api.Errors;
using SmartRail.Application.Operations;

namespace SmartRail.Api.Endpoints.Operations;

public static class OperationsOverviewEndpoints
{
    public static IEndpointRouteBuilder MapOperationsOverviewEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/operations/overview", HandleAsync)
            .WithName("GetOperationsOverview")
            .WithTags("Operations")
            .WithSummary("Read the authorized daily Controller operational overview")
            .WithDescription("Returns current PostgreSQL-backed journey state for the requested Asia/Colombo operational date. Total scheduled includes all dated journey instances planned for that date, regardless of current state. Running is a lifecycle state. Delayed is an official Delayed state or a positive delay from the latest verified ControllerManual/ApprovedRailwayFeed timing observation and may overlap Running. Open incidents are Reported or In Progress. Station Staff must supply a stationId within their current scope; Controllers may read globally or filter to one station.")
            .Produces<OperationsOverviewResponse>(StatusCodes.Status200OK)
            .Produces<ProblemResponse>(StatusCodes.Status400BadRequest, contentType: "application/problem+json")
            .Produces<ProblemResponse>(StatusCodes.Status401Unauthorized, contentType: "application/problem+json")
            .Produces<ProblemResponse>(StatusCodes.Status403Forbidden, contentType: "application/problem+json")
            .Produces<ProblemResponse>(StatusCodes.Status503ServiceUnavailable, contentType: "application/problem+json")
            .RequireAuthorization()
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
                    Name = "date", In = ParameterLocation.Query, Required = true,
                    Description = "Asia/Colombo journey operation date in yyyy-MM-dd format.",
                    Schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "date" }
                });
                operation.Parameters.Add(new OpenApiParameter
                {
                    Name = "stationId", In = ParameterLocation.Query, Required = false,
                    Description = "Optional station scope/filter. Required for Station Staff and checked against current station assignments.",
                    Schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "uuid" }
                });
                return Task.CompletedTask;
            });
        return endpoints;
    }

    private static async Task<IResult> HandleAsync(HttpContext context, OperationsOverviewHandler handler,
        CancellationToken cancellationToken)
    {
        if (!context.Request.Query.TryGetValue("date", out var dateValues) || dateValues.Count != 1 ||
            !DateOnly.TryParseExact(dateValues[0], "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var date))
            return ProblemDetailsConfiguration.Create(StatusCodes.Status400BadRequest,
                "A valid operational date is required in yyyy-MM-dd format.", "invalid_operational_date");

        Guid? stationId = null;
        if (context.Request.Query.TryGetValue("stationId", out var stationValues))
        {
            if (stationValues.Count != 1 || !Guid.TryParse(stationValues[0], out var parsed) || parsed == Guid.Empty)
                return ProblemDetailsConfiguration.Create(StatusCodes.Status400BadRequest,
                    "The station filter must be a valid UUID.", "invalid_station_filter");
            stationId = parsed;
        }

        var result = await handler.HandleAsync(new(context.User.FindFirstValue("sub"), date, stationId), cancellationToken);
        return result.Outcome switch
        {
            OperationsOverviewOutcome.Listed => Results.Ok(new OperationsOverviewResponse(result.OperationalDate,
                result.Metrics!, result.Journeys!.Select(OperationsJourneyResponse.From).ToArray())),
            OperationsOverviewOutcome.Unauthorized => ProblemDetailsConfiguration.Create(StatusCodes.Status401Unauthorized,
                "Authentication is required.", "authentication_required"),
            OperationsOverviewOutcome.Forbidden => ProblemDetailsConfiguration.Create(StatusCodes.Status403Forbidden,
                "The current role grant or station scope does not permit this overview.", "permission_denied"),
            _ => ProblemDetailsConfiguration.Create(StatusCodes.Status503ServiceUnavailable,
                "Current operational or authorization data is temporarily unavailable.", "authority_unavailable")
        };
    }
}
