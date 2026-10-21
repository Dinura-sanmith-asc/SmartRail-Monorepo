using System.Data.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SmartRail.Api.Contracts.RailwayNetwork;
using SmartRail.Api.Errors;
using SmartRail.Application.RailwayNetwork.Routes;
using SmartRail.Application.RailwayNetwork.Services;
using SmartRail.Application.RailwayNetwork.Stations;
using SmartRail.Api.Authorization;
using SmartRail.Domain.Identity;
using SmartRail.Application.Identity.Authorization;

namespace SmartRail.Api.Endpoints;

public static class RailwayNetworkEndpoints
{
    public static IEndpointRouteBuilder MapRailwayNetworkEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/stations", ListStationsAsync)
            .WithName("ListStations").WithTags("Railway Network")
            .WithSummary("List published railway stations")
            .WithDescription("Returns station identifiers, names and only approved public station details.")
            .Produces<StationListResponse>(StatusCodes.Status200OK)
            .Produces<ProblemResponse>(StatusCodes.Status503ServiceUnavailable, contentType: "application/problem+json")
            .Produces<ProblemResponse>(StatusCodes.Status500InternalServerError, contentType: "application/problem+json")
            .AllowAnonymous();

        endpoints.MapPost("/api/stations", CreateStationAsync).WithName("CreateStation").WithTags("Railway Network")
            .RequireAuthorization().Produces<StationResponse>(StatusCodes.Status201Created)
            .Produces<ProblemResponse>(StatusCodes.Status400BadRequest, contentType: "application/problem+json");
        endpoints.MapPatch("/api/stations/{id}", UpdateStationAsync).WithName("UpdateStation").WithTags("Railway Network")
            .RequireAuthorization().Produces<StationResponse>(StatusCodes.Status200OK)
            .Produces<ProblemResponse>(StatusCodes.Status400BadRequest, contentType: "application/problem+json")
            .Produces<ProblemResponse>(StatusCodes.Status404NotFound, contentType: "application/problem+json");
        endpoints.MapDelete("/api/stations/{id}", DeleteStationAsync).WithName("DeleteStation").WithTags("Railway Network")
            .RequireAuthorization().Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemResponse>(StatusCodes.Status409Conflict, contentType: "application/problem+json");

        endpoints.MapGet("/api/routes/{id}/stops", GetRouteStopsAsync)
            .WithName("GetRouteStops").WithTags("Railway Network")
            .WithSummary("Get the ordered stops of a published route")
            .Produces<RouteStopsResponse>(StatusCodes.Status200OK)
            .Produces<ProblemResponse>(StatusCodes.Status400BadRequest, contentType: "application/problem+json")
            .Produces<ProblemResponse>(StatusCodes.Status404NotFound, contentType: "application/problem+json")
            .Produces<ProblemResponse>(StatusCodes.Status503ServiceUnavailable, contentType: "application/problem+json")
            .Produces<ProblemResponse>(StatusCodes.Status500InternalServerError, contentType: "application/problem+json")
            .AllowAnonymous();

        endpoints.MapGet("/api/services", SearchServicesAsync)
            .WithName("SearchServices").WithTags("Railway Network")
            .WithSummary("Search published services by origin, destination and travel date")
            .WithDescription("Returns planned service choices only. Travel date is the scheduled departure date from the selected origin; incomplete date evidence is unavailable, not an empty result.")
            .AddOpenApiOperationTransformer((operation, _, _) =>
            {
                operation.Parameters ??= [];
                var parameters = operation.Parameters.ToArray();
                operation.Parameters.Clear();
                foreach (var parameter in parameters)
                {
                    operation.Parameters.Add(new OpenApiParameter
                    {
                        Name = parameter.Name,
                        In = parameter.In,
                        Description = parameter.Description,
                        Required = true,
                        Schema = new OpenApiSchema
                        {
                            Type = JsonSchemaType.String,
                            Format = parameter.Name == "date" ? "date" : "uuid"
                        }
                    });
                }
                return Task.CompletedTask;
            })
            .Produces<ServiceSearchResponse>(StatusCodes.Status200OK)
            .Produces<ProblemResponse>(StatusCodes.Status400BadRequest, contentType: "application/problem+json")
            .Produces<ProblemResponse>(StatusCodes.Status503ServiceUnavailable, contentType: "application/problem+json")
            .Produces<ProblemResponse>(StatusCodes.Status500InternalServerError, contentType: "application/problem+json")
            .AllowAnonymous();

        endpoints.MapGet("/api/services/{id}", GetServiceAsync)
            .WithName("GetService").WithTags("Railway Network")
            .WithSummary("Get a published train service")
            .WithDescription("Returns planned service, route and current schedule revision information without live journey state.")
            .Produces<ServiceDetailResponse>(StatusCodes.Status200OK)
            .Produces<ProblemResponse>(StatusCodes.Status400BadRequest, contentType: "application/problem+json")
            .Produces<ProblemResponse>(StatusCodes.Status404NotFound, contentType: "application/problem+json")
            .Produces<ProblemResponse>(StatusCodes.Status503ServiceUnavailable, contentType: "application/problem+json")
            .Produces<ProblemResponse>(StatusCodes.Status500InternalServerError, contentType: "application/problem+json")
            .AllowAnonymous();

        return endpoints;
    }

    private static Task<IResult> ListStationsAsync(ListStationsHandler handler, CancellationToken cancellationToken) =>
        Safe(async () =>
        {
            var result = await handler.HandleAsync(new ListStationsQuery(), cancellationToken);
            var response = new StationListResponse(result.Items.Select(station =>
                new StationResponse(station.StationId, station.Name)).ToArray());
            return Results.Ok(response);
        });

    private static async Task<IResult> CreateStationAsync(CreateStationRequest? request, IStationManagementRepository repository,
        IAuthorizationService authorization, HttpContext context, CancellationToken cancellationToken)
    {
        var denied = await AuthorizeStationManagement(authorization, context);
        if (denied is not null) return denied;
        var validation = ValidateStation(request?.Name, request?.VerifiedDetails);
        if (validation is not null) return validation;
        try
        {
            var stations = await repository.ListAsync(cancellationToken);
            if (stations.Any(item => string.Equals(item.Name, request!.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
                return ProblemDetailsConfiguration.Create(409, "A station with this name already exists.", "duplicate_station");
            var created = await repository.CreateAsync(request!, cancellationToken);
            return Results.Created($"/api/stations/{created.StationId:D}", new StationResponse(created.StationId, created.Name));
        }
        catch (DbUpdateException ex) { return MapStationWriteConflict(ex); }
        catch (ArgumentException ex) { return ProblemDetailsConfiguration.Create(400, "Station data is invalid.", "invalid_station", new Dictionary<string, string[]> { ["station"] = [ex.Message] }); }
    }

    private static async Task<IResult> UpdateStationAsync(string? id, UpdateStationRequest? request, IStationManagementRepository repository,
        IAuthorizationService authorization, HttpContext context, CancellationToken cancellationToken)
    {
        var denied = await AuthorizeStationManagement(authorization, context);
        if (denied is not null) return denied;
        if (!Guid.TryParse(id, out var stationId) || stationId == Guid.Empty)
            return ProblemDetailsConfiguration.Create(400, "Station identifier is invalid.", "invalid_station_id");
        var validation = ValidateStation(request?.Name, request?.VerifiedDetails);
        if (validation is not null) return validation;
        try
        {
            var stations = await repository.ListAsync(cancellationToken);
            if (stations.Any(item => item.StationId != stationId && string.Equals(item.Name, request!.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
                return ProblemDetailsConfiguration.Create(409, "A station with this name already exists.", "duplicate_station");
            var updated = await repository.UpdateAsync(stationId, request!, cancellationToken);
            return updated is null ? ProblemDetailsConfiguration.Create(404, "Station not found.", "station_not_found")
                : Results.Ok(new StationResponse(updated.StationId, updated.Name));
        }
        catch (DbUpdateException ex) { return MapStationWriteConflict(ex); }
        catch (ArgumentException ex) { return ProblemDetailsConfiguration.Create(400, "Station data is invalid.", "invalid_station", new Dictionary<string, string[]> { ["station"] = [ex.Message] }); }
    }

    private static async Task<IResult> DeleteStationAsync(string? id, IStationManagementRepository repository,
        IAuthorizationService authorization, HttpContext context, CancellationToken cancellationToken)
    {
        var denied = await AuthorizeStationManagement(authorization, context);
        if (denied is not null) return denied;
        if (!Guid.TryParse(id, out var stationId) || stationId == Guid.Empty)
            return ProblemDetailsConfiguration.Create(400, "Station identifier is invalid.", "invalid_station_id");
        try
        {
            return await repository.DeleteAsync(stationId, cancellationToken) ? Results.NoContent()
                : ProblemDetailsConfiguration.Create(404, "Station not found.", "station_not_found");
        }
        catch (DbUpdateException ex) { return MapStationWriteConflict(ex); }
    }

    private static async Task<IResult?> AuthorizeStationManagement(IAuthorizationService authorization, HttpContext context)
    {
        var result = await authorization.AuthorizeAsync(context.User, null,
            new SmartRailAuthorizationRequirement(PermissionAction.ManageStations, ProtectedResource.None));
        if (result.Succeeded) return null;
        var unavailable = result.Failure?.FailureReasons.Any(reason => reason.Message == "smartrail_authorization_unavailable") == true;
        return ProblemDetailsConfiguration.Create(unavailable ? 503 : 403,
            unavailable ? "Authorization is temporarily unavailable." : "Permission is denied.",
            unavailable ? "authorization_unavailable" : "permission_denied");
    }

    private static IResult? ValidateStation(string? name, IReadOnlyDictionary<string, string>? details)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(name)) errors["name"] = ["Station name is required."];
        else if (name.Trim().Length > 200) errors["name"] = ["Station name must be 200 characters or fewer."];
        if (details is not null && details.Any(item => string.IsNullOrWhiteSpace(item.Key) || string.IsNullOrWhiteSpace(item.Value) || item.Key.Length > 100 || item.Value.Length > 500))
            errors["verifiedDetails"] = ["Detail keys and values are required and must not exceed 100 and 500 characters respectively."];
        return errors.Count == 0 ? null : ProblemDetailsConfiguration.Create(400, "Station data is invalid.", "invalid_station", errors);
    }

    private static IResult MapStationWriteConflict(DbUpdateException exception)
    {
        if (exception is DbUpdateConcurrencyException)
            return ProblemDetailsConfiguration.Create(409, "Station was changed by another request. Reload and retry.", "station_concurrency_conflict");
        if (exception.InnerException is PostgresException postgres && postgres.SqlState == PostgresErrorCodes.ForeignKeyViolation)
            return ProblemDetailsConfiguration.Create(409, "Station is referenced by railway or operational data and cannot be deleted.", "station_in_use");
        if (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            return ProblemDetailsConfiguration.Create(409, "A station with this identity already exists.", "duplicate_station");
        return ProblemDetailsConfiguration.Create(503, "Station data is temporarily unavailable.", "station_data_unavailable");
    }

    private static Task<IResult> GetRouteStopsAsync(string? id, GetRouteStopsHandler handler, CancellationToken cancellationToken) =>
        Safe(async () =>
        {
            if (!Guid.TryParse(id, out var routeId))
                return ProblemDetailsConfiguration.Create(StatusCodes.Status400BadRequest, "Invalid route identifier.", "invalid_route_id",
                    new Dictionary<string, string[]> { ["id"] = ["Route ID must be a valid UUID."] });
            var result = await handler.HandleAsync(new GetRouteStopsQuery(routeId), cancellationToken);
            if (result.IsInvalid)
                return ProblemDetailsConfiguration.Create(StatusCodes.Status400BadRequest, "Invalid route identifier.", "invalid_route_id",
                    new Dictionary<string, string[]> { ["id"] = [result.Error ?? "Route ID is invalid."] });
            if (result.Route is null)
                return ProblemDetailsConfiguration.Create(StatusCodes.Status404NotFound, "Route not found.", "route_not_found");
            return Results.Ok(new RouteStopsResponse(result.Route.RouteId, result.Route.RouteCode, result.Route.Name,
                result.Route.Stops.Select(stop => new RouteStopResponse(stop.StopOrder, stop.StationId, stop.StationName)).ToArray()));
        });

    private static Task<IResult> SearchServicesAsync(
        [FromQuery(Name = "origin")] string? origin,
        [FromQuery(Name = "destination")] string? destination,
        [FromQuery(Name = "date")] string? date,
        SearchServicesHandler handler,
        CancellationToken cancellationToken) =>
        Safe(async () =>
        {
            var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
            if (!Guid.TryParse(origin, out var originId)) errors["origin"] = ["Origin must be a valid station UUID."];
            if (!Guid.TryParse(destination, out var destinationId)) errors["destination"] = ["Destination must be a valid station UUID."];
            if (string.IsNullOrWhiteSpace(date)) errors["date"] = ["Travel date is required in yyyy-MM-dd format."];
            if (errors.Count > 0)
                return ProblemDetailsConfiguration.Create(StatusCodes.Status400BadRequest, "Search criteria are invalid.", "invalid_search_criteria", errors);

            var result = await handler.HandleAsync(new SearchServicesQuery(originId, destinationId,
                date), cancellationToken);
            if (result.Outcome == ServiceSearchOutcome.Invalid)
                return ProblemDetailsConfiguration.Create(StatusCodes.Status400BadRequest, "Search criteria are invalid.", "invalid_search_criteria",
                    result.Errors.ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal));
            if (result.Outcome == ServiceSearchOutcome.Unavailable)
                return ProblemDetailsConfiguration.Create(StatusCodes.Status503ServiceUnavailable,
                    "Railway service eligibility information is unavailable.", "service_eligibility_unavailable");

            var response = new ServiceSearchResponse(originId, destinationId,
                DateOnly.ParseExact(date!, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                result.Services.Select(service => new ServiceChoiceResponse(service.ServiceId, service.ServiceCode,
                    service.RouteId, service.RouteCode, service.ScheduleRevision,
                    ToResponse(service.Origin), ToResponse(service.Destination), service.DisplayName, service.PublicServiceNumber)).ToArray());
            return Results.Ok(response);
        });

    private static Task<IResult> GetServiceAsync(string? id, GetServiceHandler handler, CancellationToken cancellationToken) =>
        Safe(async () =>
        {
            if (!Guid.TryParse(id, out var serviceId))
                return ProblemDetailsConfiguration.Create(StatusCodes.Status400BadRequest, "Invalid service identifier.", "invalid_service_id",
                    new Dictionary<string, string[]> { ["id"] = ["Service ID must be a valid UUID."] });
            var result = await handler.HandleAsync(new GetServiceQuery(serviceId), cancellationToken);
            if (result.IsInvalid)
                return ProblemDetailsConfiguration.Create(StatusCodes.Status400BadRequest, "Invalid service identifier.", "invalid_service_id",
                    new Dictionary<string, string[]> { ["id"] = [result.Error ?? "Service ID is invalid."] });
            if (result.Service is null)
                return ProblemDetailsConfiguration.Create(StatusCodes.Status404NotFound, "Service not found.", "service_not_found");
            var service = result.Service;
            return Results.Ok(new ServiceDetailResponse(service.ServiceId, service.ServiceCode,
                new RouteReferenceResponse(service.RouteId, service.RouteCode, service.RouteName),
                new ScheduleReferenceResponse(service.ScheduleRevision, service.FirstDepartureTimeLocal),
                service.DisplayName, service.PublicServiceNumber));
        });

    private static ScheduledCallResponse ToResponse(ScheduledCallView call) =>
        new(call.StationId, call.StationName, call.StopOrder, call.ScheduledAt);

    private static async Task<IResult> Safe(Func<Task<IResult>> action)
    {
        try
        {
            return await action();
        }
        catch (DbException)
        {
            return ProblemDetailsConfiguration.Create(StatusCodes.Status503ServiceUnavailable,
                "Railway data is temporarily unavailable.", "railway_data_unavailable");
        }
        catch (TimeoutException)
        {
            return ProblemDetailsConfiguration.Create(StatusCodes.Status503ServiceUnavailable,
                "Railway data is temporarily unavailable.", "railway_data_unavailable");
        }
    }
}
