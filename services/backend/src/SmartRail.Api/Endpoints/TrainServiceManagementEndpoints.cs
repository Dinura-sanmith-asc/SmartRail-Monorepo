using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Npgsql;
using SmartRail.Api.Authorization;
using SmartRail.Api.Errors;
using SmartRail.Application.Identity.Authorization;
using SmartRail.Application.RailwayNetwork.Services;
using SmartRail.Domain.Identity;
using SmartRail.Infrastructure.RailwayNetwork.Persistence;

namespace SmartRail.Api.Endpoints;

public static class TrainServiceManagementEndpoints
{
    public static IEndpointRouteBuilder MapTrainServiceManagementEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/services").WithTags("Train Service Management").RequireAuthorization();
        Secure(group.MapGet("/management", ListAsync).WithName("ListManagedTrainServices"));
        Secure(endpoints.MapPost("/api/services", CreateAsync).WithName("CreateTrainService").WithTags("Train Service Management").RequireAuthorization());
        Secure(group.MapPatch("/{id}", UpdateAsync).WithName("UpdateTrainService"));
        Secure(group.MapDelete("/{id}", DeleteAsync).WithName("DeleteTrainService"));
        Secure(group.MapGet("/{id}/schedules", ListSchedulesAsync).WithName("ListTrainServiceSchedules"));
        Secure(group.MapPost("/{id}/schedules", CreateScheduleAsync).WithName("CreateTrainServiceSchedule"));
        Secure(group.MapPatch("/{id}/schedules/{scheduleId}", UpdateScheduleAsync).WithName("UpdateTrainServiceSchedule"));
        Secure(group.MapDelete("/{id}/schedules/{scheduleId}", DeleteScheduleAsync).WithName("DeleteTrainServiceSchedule"));
        Secure(group.MapPost("/{id}/schedules/{scheduleId}/publish", PublishScheduleAsync).WithName("PublishTrainServiceSchedule"));
        return endpoints;
    }

    private static async Task<IResult> ListSchedulesAsync(Guid id, ITrainServiceManagementRepository repository,
        IAuthorizationService authorization, HttpContext context, CancellationToken token)
    {
        var denied = await AuthorizeAsync(authorization, context);
        if (denied is not null) return denied;
        var schedules = await repository.GetSchedulesAsync(id, token);
        return schedules is null ? ProblemDetailsConfiguration.Create(404, "Train service not found.", "service_not_found") : Results.Ok(schedules);
    }

    private static async Task<IResult> CreateScheduleAsync(Guid id, ScheduleRevisionInput? request,
        ITrainServiceManagementRepository repository, IAuthorizationService authorization, HttpContext context, CancellationToken token)
    {
        var denied = await AuthorizeAsync(authorization, context);
        if (denied is not null) return denied;
        if (request?.Calendar is null) return Invalid("An operating calendar is required.");
        try
        {
            var created = await repository.CreateScheduleAsync(id, request, token);
            return Results.Created($"/api/services/{id:D}/schedules/{created.ScheduleId:D}", created);
        }
        catch (KeyNotFoundException ex) { return ProblemDetailsConfiguration.Create(404, ex.Message, "service_not_found"); }
        catch (ArgumentException ex) { return Invalid(ex.Message); }
        catch (DbUpdateException ex) { return MapWriteConflict(ex); }
    }

    private static async Task<IResult> UpdateScheduleAsync(Guid id, Guid scheduleId, ScheduleRevisionInput? request,
        ITrainServiceManagementRepository repository, IAuthorizationService authorization, HttpContext context, CancellationToken token)
    {
        var denied = await AuthorizeAsync(authorization, context);
        if (denied is not null) return denied;
        if (request?.Calendar is null) return Invalid("An operating calendar is required.");
        try
        {
            var updated = await repository.UpdateScheduleAsync(id, scheduleId, request, token);
            return updated is null ? ProblemDetailsConfiguration.Create(404, "Schedule not found.", "schedule_not_found") : Results.Ok(updated);
        }
        catch (ArgumentException ex) { return Invalid(ex.Message); }
        catch (InvalidOperationException ex) { return ProblemDetailsConfiguration.Create(409, ex.Message, "schedule_immutable"); }
        catch (DbUpdateException ex) { return MapWriteConflict(ex); }
    }

    private static async Task<IResult> DeleteScheduleAsync(Guid id, Guid scheduleId,
        ITrainServiceManagementRepository repository, IAuthorizationService authorization, HttpContext context, CancellationToken token)
    {
        var denied = await AuthorizeAsync(authorization, context);
        if (denied is not null) return denied;
        try { return await repository.DeleteScheduleAsync(id, scheduleId, token) ? Results.NoContent()
            : ProblemDetailsConfiguration.Create(404, "Schedule not found.", "schedule_not_found"); }
        catch (InvalidOperationException ex) { return ProblemDetailsConfiguration.Create(409, ex.Message, "schedule_in_use"); }
        catch (DbUpdateException ex)
        {
            if (ex.GetBaseException() is NpgsqlException { SqlState: PostgresErrorCodes.ForeignKeyViolation })
                return ProblemDetailsConfiguration.Create(409, "This schedule is referenced by operational history and cannot be deleted.", "schedule_in_use");
            return MapWriteConflict(ex);
        }
    }

    private static async Task<IResult> PublishScheduleAsync(Guid id, Guid scheduleId,
        ITrainServiceManagementRepository repository, IAuthorizationService authorization, HttpContext context, CancellationToken token)
    {
        var denied = await AuthorizeAsync(authorization, context);
        if (denied is not null) return denied;
        try
        {
            await repository.PublishScheduleAsync(id, scheduleId, DateTimeOffset.UtcNow, token);
            return Results.NoContent();
        }
        catch (KeyNotFoundException ex) { return ProblemDetailsConfiguration.Create(404, ex.Message, "schedule_not_found"); }
        catch (InvalidOperationException ex) { return ProblemDetailsConfiguration.Create(409, ex.Message, "schedule_publication_conflict"); }
        catch (DbUpdateException ex) { return MapWriteConflict(ex); }
    }

    private static async Task<IResult> ListAsync(ITrainServiceManagementRepository repository, IAuthorizationService authorization, HttpContext context, CancellationToken token)
    {
        var denied = await AuthorizeAsync(authorization, context);
        return denied ?? Results.Ok(await repository.ListAsync(token));
    }

    private static async Task<IResult> CreateAsync(CreateTrainServiceRequest? request, ITrainServiceManagementRepository repository,
        IAuthorizationService authorization, HttpContext context, CancellationToken token)
    {
        var denied = await AuthorizeAsync(authorization, context);
        if (denied is not null) return denied;
        var invalid = ValidateService(request?.ServiceCode, request?.RouteId, request?.DisplayName, request?.PublicServiceNumber);
        if (invalid is not null) return invalid;
        try
        {
            var created = await repository.CreateAsync(request!, token);
            return created is null ? ProblemDetailsConfiguration.Create(404, "Route not found.", "route_not_found")
                : Results.Created($"/api/services/{created.ServiceId:D}", created);
        }
        catch (DbUpdateException ex) { return MapWriteConflict(ex); }
        catch (ArgumentException ex) { return Invalid(ex.Message); }
    }

    private static async Task<IResult> UpdateAsync(string? id, UpdateTrainServiceRequest? request, ITrainServiceManagementRepository repository,
        IAuthorizationService authorization, HttpContext context, CancellationToken token)
    {
        var denied = await AuthorizeAsync(authorization, context);
        if (denied is not null) return denied;
        if (!TryId(id, out var serviceId)) return ProblemDetailsConfiguration.Create(400, "Invalid service identifier.", "invalid_service_id");
        var invalid = ValidateService(request?.ServiceCode, request?.RouteId, request?.DisplayName, request?.PublicServiceNumber);
        if (invalid is not null) return invalid;
        try
        {
            var updated = await repository.UpdateAsync(serviceId, request!, token);
            return updated is null ? ProblemDetailsConfiguration.Create(404, "Train service not found.", "service_not_found") : Results.Ok(updated);
        }
        catch (KeyNotFoundException ex) { return ProblemDetailsConfiguration.Create(404, ex.Message, "route_not_found"); }
        catch (InvalidOperationException ex) { return ProblemDetailsConfiguration.Create(409, ex.Message, "service_schedule_conflict"); }
        catch (DbUpdateException ex) { return MapWriteConflict(ex); }
        catch (ArgumentException ex) { return Invalid(ex.Message); }
    }

    private static async Task<IResult> DeleteAsync(string? id, ITrainServiceManagementRepository repository,
        IAuthorizationService authorization, HttpContext context, CancellationToken token)
    {
        var denied = await AuthorizeAsync(authorization, context);
        if (denied is not null) return denied;
        if (!TryId(id, out var serviceId)) return ProblemDetailsConfiguration.Create(400, "Invalid service identifier.", "invalid_service_id");
        try
        {
            return await repository.DeleteAsync(serviceId, token) ? Results.NoContent()
                : ProblemDetailsConfiguration.Create(404, "Train service not found.", "service_not_found");
        }
        catch (DbUpdateException ex) { return MapWriteConflict(ex); }
    }

    private static IResult? ValidateService(string? code, Guid? routeId, string? displayName, string? number)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(code) || code.Trim().Length > 50) errors["serviceCode"] = ["Service code is required and must be 50 characters or fewer."];
        if (routeId is null || routeId == Guid.Empty) errors["routeId"] = ["A valid route is required."];
        if (displayName?.Trim().Length > 200) errors["displayName"] = ["Display name must be 200 characters or fewer."];
        if (number?.Trim().Length > 50) errors["publicServiceNumber"] = ["Public service number must be 50 characters or fewer."];
        return errors.Count == 0 ? null : ProblemDetailsConfiguration.Create(400, "Train service data is invalid.", "invalid_service", errors);
    }

    private static IResult Invalid(string detail) => ProblemDetailsConfiguration.Create(400, "Train service data is invalid.", "invalid_service_management", new Dictionary<string, string[]> { ["service"] = [detail] });
    private static bool TryId(string? value, out Guid id) => Guid.TryParse(value, out id) && id != Guid.Empty;

    private static async Task<IResult?> AuthorizeAsync(IAuthorizationService authorization, HttpContext context)
    {
        // Uses the existing approved Controller permission grant for railway network management.
        var result = await authorization.AuthorizeAsync(context.User, null,
            new SmartRailAuthorizationRequirement(PermissionAction.ManageStations, ProtectedResource.None));
        if (result.Succeeded) return null;
        var unavailable = result.Failure?.FailureReasons.Any(reason => reason.Message == "smartrail_authorization_unavailable") == true;
        return ProblemDetailsConfiguration.Create(unavailable ? 503 : 403,
            unavailable ? "Authorization is temporarily unavailable." : "Permission is denied.",
            unavailable ? "authorization_unavailable" : "permission_denied");
    }

    private static IResult MapWriteConflict(DbUpdateException ex)
    {
        if (ex is DbUpdateConcurrencyException) return ProblemDetailsConfiguration.Create(409, "Train service data changed concurrently. Reload and retry.", "service_concurrency_conflict");
        if (ex.GetBaseException() is NpgsqlException postgres &&
            postgres.SqlState is PostgresErrorCodes.ForeignKeyViolation or "23001")
            return ProblemDetailsConfiguration.Create(409, "Train service is referenced by schedules, journeys, or operational records and cannot be deleted.", "service_in_use");
        if (ex.GetBaseException() is NpgsqlException { SqlState: PostgresErrorCodes.UniqueViolation } postgresUnique)
        {
            var constraint = (postgresUnique as PostgresException)?.ConstraintName;
            var number = constraint == "ux_train_service_public_service_number";
            return ProblemDetailsConfiguration.Create(409, number ? "Public service number is already in use." : "Service code is already in use.",
                number ? "duplicate_public_service_number" : "duplicate_service_code");
        }
        return ProblemDetailsConfiguration.Create(503, "Train service data is temporarily unavailable.", "service_data_unavailable");
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
