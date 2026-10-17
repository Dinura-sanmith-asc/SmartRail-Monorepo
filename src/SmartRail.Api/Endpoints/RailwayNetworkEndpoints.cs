using System.Data.Common;
using Microsoft.AspNetCore.Mvc;
using SmartRail.Api.Contracts.RailwayNetwork;
using SmartRail.Api.Errors;
using SmartRail.Application.RailwayNetwork.Routes;
using SmartRail.Application.RailwayNetwork.Services;
using SmartRail.Application.RailwayNetwork.Stations;

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

    private static Task<IResult> GetRouteStopsAsync(Guid routeId, GetRouteStopsHandler handler, CancellationToken cancellationToken) =>
        Safe(async () =>
        {
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
        [FromQuery(Name = "origin")] Guid originId,
        [FromQuery(Name = "destination")] Guid destinationId,
        DateOnly date,
        SearchServicesHandler handler,
        CancellationToken cancellationToken) =>
        Safe(async () =>
        {
            var result = await handler.HandleAsync(new SearchServicesQuery(originId, destinationId,
                date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)), cancellationToken);
            if (result.Outcome == ServiceSearchOutcome.Invalid)
                return ProblemDetailsConfiguration.Create(StatusCodes.Status400BadRequest, "Search criteria are invalid.", "invalid_search_criteria",
                    result.Errors.ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal));
            if (result.Outcome == ServiceSearchOutcome.Unavailable)
                return ProblemDetailsConfiguration.Create(StatusCodes.Status503ServiceUnavailable,
                    "Railway service eligibility information is unavailable.", "service_eligibility_unavailable");

            var response = new ServiceSearchResponse(originId, destinationId,
                date,
                result.Services.Select(service => new ServiceChoiceResponse(service.ServiceId, service.ServiceCode,
                    service.RouteId, service.RouteCode, service.ScheduleRevision,
                    ToResponse(service.Origin), ToResponse(service.Destination))).ToArray());
            return Results.Ok(response);
        });

    private static Task<IResult> GetServiceAsync(Guid serviceId, GetServiceHandler handler, CancellationToken cancellationToken) =>
        Safe(async () =>
        {
            var result = await handler.HandleAsync(new GetServiceQuery(serviceId), cancellationToken);
            if (result.IsInvalid)
                return ProblemDetailsConfiguration.Create(StatusCodes.Status400BadRequest, "Invalid service identifier.", "invalid_service_id",
                    new Dictionary<string, string[]> { ["id"] = [result.Error ?? "Service ID is invalid."] });
            if (result.Service is null)
                return ProblemDetailsConfiguration.Create(StatusCodes.Status404NotFound, "Service not found.", "service_not_found");
            var service = result.Service;
            return Results.Ok(new ServiceDetailResponse(service.ServiceId, service.ServiceCode,
                new RouteReferenceResponse(service.RouteId, service.RouteCode, service.RouteName),
                new ScheduleReferenceResponse(service.ScheduleRevision, service.FirstDepartureTimeLocal)));
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
