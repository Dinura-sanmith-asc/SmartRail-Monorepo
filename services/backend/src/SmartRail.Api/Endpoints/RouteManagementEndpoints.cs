using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Npgsql;
using SmartRail.Api.Authorization;
using SmartRail.Api.Errors;
using SmartRail.Application.Identity.Authorization;
using SmartRail.Application.RailwayNetwork.Routes;
using SmartRail.Domain.Identity;

namespace SmartRail.Api.Endpoints;

public static class RouteManagementEndpoints
{
    public static IEndpointRouteBuilder MapRouteManagementEndpoints(this IEndpointRouteBuilder endpoints)
    {
        Secure(endpoints.MapGet("/api/routes/management", ListAsync).WithName("ListManagedRoutes").WithTags("Route Management").RequireAuthorization());
        Secure(endpoints.MapPost("/api/routes/management", CreateAsync).WithName("CreateRoute").WithTags("Route Management").RequireAuthorization());
        Secure(endpoints.MapPatch("/api/routes/management/{id}", UpdateAsync).WithName("UpdateRoute").WithTags("Route Management").RequireAuthorization());
        Secure(endpoints.MapDelete("/api/routes/management/{id}", DeleteAsync).WithName("DeleteRoute").WithTags("Route Management").RequireAuthorization());
        return endpoints;
    }

    private static async Task<IResult> ListAsync(IRouteManagementRepository repository, IAuthorizationService authorization,
        HttpContext context, CancellationToken token)
    {
        var denied = await AuthorizeAsync(authorization, context);
        return denied ?? Results.Ok(await repository.ListAsync(token));
    }

    private static async Task<IResult> CreateAsync(SaveRouteRequest? request, IRouteManagementRepository repository,
        IAuthorizationService authorization, HttpContext context, CancellationToken token)
    {
        var denied = await AuthorizeAsync(authorization, context);
        if (denied is not null) return denied;
        var invalid = Validate(request);
        if (invalid is not null) return invalid;
        try
        {
            var created = await repository.CreateAsync(request!, token);
            return Results.Created($"/api/routes/management/{created!.RouteId:D}", created);
        }
        catch (KeyNotFoundException ex) { return ProblemDetailsConfiguration.Create(404, ex.Message, "station_not_found"); }
        catch (ArgumentException ex) { return Invalid(ex.Message); }
        catch (DbUpdateException ex) { return MapWriteConflict(ex); }
    }

    private static async Task<IResult> UpdateAsync(string? id, SaveRouteRequest? request, IRouteManagementRepository repository,
        IAuthorizationService authorization, HttpContext context, CancellationToken token)
    {
        var denied = await AuthorizeAsync(authorization, context);
        if (denied is not null) return denied;
        if (!Guid.TryParse(id, out var routeId) || routeId == Guid.Empty)
            return ProblemDetailsConfiguration.Create(400, "Route identifier is invalid.", "invalid_route_id");
        var invalid = Validate(request);
        if (invalid is not null) return invalid;
        try
        {
            var updated = await repository.UpdateAsync(routeId, request!, token);
            return updated is null ? ProblemDetailsConfiguration.Create(404, "Route not found.", "route_not_found") : Results.Ok(updated);
        }
        catch (KeyNotFoundException ex) { return ProblemDetailsConfiguration.Create(404, ex.Message, "station_not_found"); }
        catch (ArgumentException ex) { return Invalid(ex.Message); }
        catch (DbUpdateConcurrencyException) { return ProblemDetailsConfiguration.Create(409, "Route data changed concurrently. Reload and retry.", "route_concurrency_conflict"); }
        catch (DbUpdateException ex) { return MapWriteConflict(ex); }
        catch (OverflowException) { return Invalid("The route has too many stops to order."); }
    }

    private static async Task<IResult> DeleteAsync(string? id, IRouteManagementRepository repository,
        IAuthorizationService authorization, HttpContext context, CancellationToken token)
    {
        var denied = await AuthorizeAsync(authorization, context);
        if (denied is not null) return denied;
        if (!Guid.TryParse(id, out var routeId) || routeId == Guid.Empty)
            return ProblemDetailsConfiguration.Create(400, "Route identifier is invalid.", "invalid_route_id");
        try
        {
            return await repository.DeleteAsync(routeId, token) ? Results.NoContent()
                : ProblemDetailsConfiguration.Create(404, "Route not found.", "route_not_found");
        }
        catch (DbUpdateConcurrencyException) { return ProblemDetailsConfiguration.Create(409, "Route data changed concurrently. Reload and retry.", "route_concurrency_conflict"); }
        catch (DbUpdateException ex) { return MapWriteConflict(ex); }
    }

    private static IResult? Validate(SaveRouteRequest? request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.RouteCode) || request.RouteCode.Trim().Length > 50 ||
            string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 200 || request.StationIds is null)
            return Invalid("Route code and name are required, and route stops must be a list.");
        if (request.StationIds.Count > 500) return Invalid("A route cannot contain more than 500 stops.");
        if (request.StationIds.Any(id => id == Guid.Empty)) return Invalid("Every route stop must include a valid station.");
        return null;
    }

    private static async Task<IResult?> AuthorizeAsync(IAuthorizationService authorization, HttpContext context)
    {
        var result = await authorization.AuthorizeAsync(context.User, null,
            new SmartRailAuthorizationRequirement(PermissionAction.ManageStations, ProtectedResource.None));
        if (result.Succeeded) return null;
        var unavailable = result.Failure?.FailureReasons.Any(reason => reason.Message == "smartrail_authorization_unavailable") == true;
        return ProblemDetailsConfiguration.Create(unavailable ? 503 : 403,
            unavailable ? "Authorization is temporarily unavailable." : "Permission is denied.",
            unavailable ? "authorization_unavailable" : "permission_denied");
    }

    private static IResult Invalid(string detail) => ProblemDetailsConfiguration.Create(400,
        "Route data is invalid.", "invalid_route", new Dictionary<string, string[]> { ["route"] = [detail] });

    private static IResult MapWriteConflict(DbUpdateException ex)
    {
        if (ex.GetBaseException() is NpgsqlException { SqlState: PostgresErrorCodes.UniqueViolation } postgresUnique &&
            (postgresUnique as PostgresException)?.ConstraintName == "ux_route_route_code")
            return ProblemDetailsConfiguration.Create(409, "Route code is already in use.", "duplicate_route_code");
        if (ex.GetBaseException() is NpgsqlException postgres &&
            postgres.SqlState is PostgresErrorCodes.ForeignKeyViolation or "23001")
            return ProblemDetailsConfiguration.Create(409, "Route is referenced by train services or schedule calling points and cannot be changed or deleted.", "route_in_use");
        return ProblemDetailsConfiguration.Create(503, "Route data is temporarily unavailable.", "route_data_unavailable");
    }

    private static RouteHandlerBuilder Secure(RouteHandlerBuilder builder) => builder.AddOpenApiOperationTransformer((operation, context, _) =>
    {
        operation.Security ??= [];
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("CognitoAccessToken", context.Document)] = []
        });
        return Task.CompletedTask;
    });
}
