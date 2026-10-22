using Microsoft.AspNetCore.Authorization;
using System.Globalization;
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
        Secure(endpoints.MapGet("/api/routes/management", ListRoutesAsync).WithTags("Train Service Management").RequireAuthorization());
        Secure(endpoints.MapPost("/api/services", CreateAsync).WithName("CreateTrainService").WithTags("Train Service Management").RequireAuthorization());
        Secure(group.MapPatch("/{id}", UpdateAsync).WithName("UpdateTrainService"));
        Secure(group.MapDelete("/{id}", DeleteAsync).WithName("DeleteTrainService"));
        Secure(group.MapGet("/{id}/schedules", GetSchedulesAsync).WithName("GetServiceScheduleRevisions"));
        Secure(group.MapPost("/{id}/schedules", CreateScheduleAsync).WithName("CreateServiceScheduleRevision"));
        Secure(group.MapPatch("/{id}/schedules/{scheduleId}", UpdateScheduleAsync).WithName("UpdateServiceScheduleRevision"));
        Secure(group.MapDelete("/{id}/schedules/{scheduleId}", DeleteScheduleAsync).WithName("DeleteServiceScheduleRevision"));
        Secure(group.MapPost("/{id}/schedules/{scheduleId}/publish", PublishScheduleAsync).WithName("PublishServiceScheduleRevision"));
        Secure(group.MapPut("/{id}/schedules/{scheduleId}/operation-dates", SetOperationDatesAsync).WithName("SetServiceOperationDates"));
        Secure(endpoints.MapGet("/api/service-date-manifests/{date}", GetManifestAsync).WithTags("Train Service Management").RequireAuthorization());
        Secure(endpoints.MapPut("/api/service-date-manifests/{date}", SetManifestAsync).WithTags("Train Service Management").RequireAuthorization());
        return endpoints;
    }

    private static async Task<IResult> ListAsync(ITrainServiceManagementRepository repository, IAuthorizationService authorization, HttpContext context, CancellationToken token)
    {
        var denied = await AuthorizeAsync(authorization, context);
        return denied ?? Results.Ok(await repository.ListAsync(token));
    }

    private static async Task<IResult> ListRoutesAsync(ITrainServiceManagementRepository repository, IAuthorizationService authorization, HttpContext context, CancellationToken token)
    {
        var denied = await AuthorizeAsync(authorization, context);
        return denied ?? Results.Ok(await repository.ListRoutesAsync(token));
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

    private static async Task<IResult> GetSchedulesAsync(string? id, ITrainServiceManagementRepository repository,
        IAuthorizationService authorization, HttpContext context, CancellationToken token)
    {
        var denied = await AuthorizeAsync(authorization, context);
        if (denied is not null) return denied;
        if (!TryId(id, out var serviceId)) return ProblemDetailsConfiguration.Create(400, "Invalid service identifier.", "invalid_service_id");
        var result = await repository.GetSchedulesAsync(serviceId, token);
        return result is null ? ProblemDetailsConfiguration.Create(404, "Train service not found.", "service_not_found") : Results.Ok(result);
    }

    private static async Task<IResult> CreateScheduleAsync(string? id, ScheduleRevisionInput? request, ITrainServiceManagementRepository repository,
        IAuthorizationService authorization, HttpContext context, CancellationToken token)
    {
        var denied = await AuthorizeAsync(authorization, context);
        if (denied is not null) return denied;
        if (!TryId(id, out var serviceId)) return ProblemDetailsConfiguration.Create(400, "Invalid service identifier.", "invalid_service_id");
        var invalid = ValidateSchedule(request);
        if (invalid is not null) return invalid;
        try { return Results.Created($"/api/services/{serviceId:D}/schedules", await repository.CreateScheduleAsync(serviceId, request!, token)); }
        catch (KeyNotFoundException ex) { return ProblemDetailsConfiguration.Create(404, ex.Message, "service_not_found"); }
        catch (ArgumentException ex) { return Invalid(ex.Message); }
        catch (DbUpdateException ex) { return MapWriteConflict(ex); }
    }

    private static async Task<IResult> UpdateScheduleAsync(string? id, string? scheduleId, ScheduleRevisionInput? request,
        ITrainServiceManagementRepository repository, IAuthorizationService authorization, HttpContext context, CancellationToken token)
    {
        var denied = await AuthorizeAsync(authorization, context);
        if (denied is not null) return denied;
        if (!TryId(id, out var service) || !TryId(scheduleId, out var schedule)) return ProblemDetailsConfiguration.Create(400, "Invalid service or schedule identifier.", "invalid_schedule_id");
        var invalid = ValidateSchedule(request);
        if (invalid is not null) return invalid;
        try
        {
            var result = await repository.UpdateScheduleAsync(service, schedule, request!, token);
            return result is null ? ProblemDetailsConfiguration.Create(404, "Service or schedule revision not found.", "schedule_not_found") : Results.Ok(result);
        }
        catch (ArgumentException ex) { return Invalid(ex.Message); }
        catch (InvalidOperationException ex) { return ProblemDetailsConfiguration.Create(409, ex.Message, "schedule_revision_immutable"); }
        catch (DbUpdateException ex) { return MapWriteConflict(ex); }
    }

    private static async Task<IResult> DeleteScheduleAsync(string? id, string? scheduleId, ITrainServiceManagementRepository repository,
        IAuthorizationService authorization, HttpContext context, CancellationToken token)
    {
        var denied = await AuthorizeAsync(authorization, context);
        if (denied is not null) return denied;
        if (!TryId(id, out var service) || !TryId(scheduleId, out var schedule)) return ProblemDetailsConfiguration.Create(400, "Invalid service or schedule identifier.", "invalid_schedule_id");
        try
        {
            return await repository.DeleteScheduleAsync(service, schedule, token) ? Results.NoContent()
                : ProblemDetailsConfiguration.Create(404, "Schedule revision not found.", "schedule_not_found");
        }
        catch (InvalidOperationException ex) { return ProblemDetailsConfiguration.Create(409, ex.Message, "schedule_in_use"); }
        catch (DbUpdateException ex) { return MapWriteConflict(ex); }
    }

    private static async Task<IResult> PublishScheduleAsync(string? id, string? scheduleId, ITrainServiceManagementRepository repository,
        IAuthorizationService authorization, HttpContext context, CancellationToken token)
    {
        var denied = await AuthorizeAsync(authorization, context);
        if (denied is not null) return denied;
        if (!TryId(id, out var service) || !TryId(scheduleId, out var schedule)) return ProblemDetailsConfiguration.Create(400, "Invalid service or schedule identifier.", "invalid_schedule_id");
        try
        {
            await repository.PublishScheduleAsync(service, schedule, DateTimeOffset.UtcNow, token);
            return Results.NoContent();
        }
        catch (KeyNotFoundException ex) { return ProblemDetailsConfiguration.Create(404, ex.Message, "schedule_not_found"); }
        catch (SchedulePublicationRejectedException ex) { return ProblemDetailsConfiguration.Create(400, ex.Message, "schedule_publication_invalid", new Dictionary<string, string[]> { ["schedule"] = ex.Errors.ToArray() }); }
        catch (InvalidOperationException ex) { return ProblemDetailsConfiguration.Create(409, ex.Message, "schedule_publication_conflict"); }
        catch (DbUpdateException ex) { return MapWriteConflict(ex); }
    }

    private static async Task<IResult> SetOperationDatesAsync(string? id, string? scheduleId, OperationDatesRequest? request,
        ITrainServiceManagementRepository repository, IAuthorizationService authorization, HttpContext context, CancellationToken token)
    {
        var denied = await AuthorizeAsync(authorization, context);
        if (denied is not null) return denied;
        if (!TryId(id, out var service) || !TryId(scheduleId, out var schedule)) return ProblemDetailsConfiguration.Create(400, "Invalid service or schedule identifier.", "invalid_schedule_id");
        if (request?.Dates is null) return Invalid("Dates are required.");
        try
        {
            var result = await repository.SetOperationDatesAsync(service, schedule, request.Dates, token);
            return result is null ? ProblemDetailsConfiguration.Create(404, "Schedule revision not found.", "schedule_not_found") : Results.Ok(result);
        }
        catch (ArgumentException ex) { return Invalid(ex.Message); }
        catch (InvalidOperationException ex) { return ProblemDetailsConfiguration.Create(409, ex.Message, "operation_date_conflict"); }
        catch (DbUpdateException ex) { return MapWriteConflict(ex); }
    }

    private static async Task<IResult> GetManifestAsync(string? date, ITrainServiceManagementRepository repository,
        IAuthorizationService authorization, HttpContext context, CancellationToken token)
    {
        var denied = await AuthorizeAsync(authorization, context);
        if (denied is not null) return denied;
        if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var operationDate))
            return ProblemDetailsConfiguration.Create(400, "Operation date must use yyyy-MM-dd format.", "invalid_operation_date");
        var manifest = await repository.GetManifestAsync(operationDate, token);
        return manifest is null ? ProblemDetailsConfiguration.Create(404, "Service-date manifest not found.", "service_date_manifest_not_found") : Results.Ok(manifest);
    }

    private static async Task<IResult> SetManifestAsync(string? date, ServiceDateManifestRequest? request,
        ITrainServiceManagementRepository repository, IAuthorizationService authorization, HttpContext context, CancellationToken token)
    {
        var denied = await AuthorizeAsync(authorization, context);
        if (denied is not null) return denied;
        if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var operationDate))
            return ProblemDetailsConfiguration.Create(400, "Operation date must use yyyy-MM-dd format.", "invalid_operation_date");
        if (request is null || request.ScheduleIds is null || string.IsNullOrWhiteSpace(request.SourceRevision) ||
            string.IsNullOrWhiteSpace(request.FixtureVersion) || string.IsNullOrWhiteSpace(request.ContentHash))
            return Invalid("Manifest source revision, fixture version, content hash and schedule list are required.");
        var subject = context.User.FindFirst("sub")?.Value;
        if (request.IsComplete && string.IsNullOrWhiteSpace(subject))
            return ProblemDetailsConfiguration.Create(403, "A verified operator identity is required to publish a complete manifest.", "publisher_identity_required");
        var publishedAt = request.IsComplete ? DateTimeOffset.UtcNow : (DateTimeOffset?)null;
        var manifest = new SmartRail.Domain.RailwayNetwork.Entities.ServiceDateManifest(operationDate,
            request.SourceRevision, request.FixtureVersion, request.IsComplete, publishedAt,
            request.IsComplete ? subject : null, request.ContentHash);
        try { return Results.Ok(await repository.SetManifestAsync(manifest, request.ScheduleIds, token)); }
        catch (ArgumentException ex) { return Invalid(ex.Message); }
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

    private static IResult? ValidateSchedule(ScheduleRevisionInput? request)
    {
        if (request is null || request.CallingPoints is null || request.CallingPoints.Count < 2)
            return Invalid("A first departure time and at least two calling points are required.");
        if (request.CallingPoints.Any(item => item.RouteStopId == Guid.Empty)) return Invalid("Every calling point must include a valid route stop.");
        if (request.CallingPoints.Any(item => item.ArrivalOffsetMinutes < 0 || item.DepartureOffsetMinutes < 0))
            return Invalid("Arrival and departure offsets cannot be negative.");
        return null;
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

    public sealed record OperationDatesRequest(IReadOnlyList<DateOnly> Dates);
    public sealed record ServiceDateManifestRequest(string SourceRevision, string FixtureVersion, bool IsComplete,
        string ContentHash, IReadOnlyList<Guid> ScheduleIds);
}
