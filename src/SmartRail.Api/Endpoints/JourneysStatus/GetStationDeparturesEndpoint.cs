using Microsoft.OpenApi;
using SmartRail.Api.Contracts.RailwayNetwork;
using SmartRail.Application.JourneysStatus.Queries.GetStationDepartures;

namespace SmartRail.Api.Endpoints.JourneysStatus;

public static class GetStationDeparturesEndpoint
{
    public static RouteHandlerBuilder MapGetStationDepartures(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/api/stations/{id}/departures", HandleAsync)
            .WithName("GetStationDepartures").WithTags("Station departures")
            .WithSummary("Get dated journey departures at a station")
            .WithDescription("Returns public departures filtered by station-local calendar date in Asia/Colombo. Missing times are never inferred.")
            .AddOpenApiOperationTransformer((operation, _, _) =>
            {
                if (operation.Parameters is not null)
                {
                    var parameters = operation.Parameters.ToArray();
                    operation.Parameters.Clear();
                    foreach (var parameter in parameters)
                    {
                        operation.Parameters.Add(parameter.Name == "date"
                            ? new OpenApiParameter
                            {
                                Name = parameter.Name,
                                In = parameter.In,
                                Description = parameter.Description,
                                Required = true,
                                Schema = new OpenApiSchema
                                {
                                    Type = JsonSchemaType.String,
                                    Format = "date"
                                }
                            }
                            : parameter);
                    }
                }
                return Task.CompletedTask;
            })
            .Produces<IReadOnlyList<StationDepartureResponse>>(StatusCodes.Status200OK)
            .Produces<ProblemResponse>(StatusCodes.Status400BadRequest, contentType: "application/problem+json")
            .Produces<ProblemResponse>(StatusCodes.Status404NotFound, contentType: "application/problem+json")
            .Produces<ProblemResponse>(StatusCodes.Status503ServiceUnavailable, contentType: "application/problem+json")
            .AllowAnonymous();

    private static async Task<IResult> HandleAsync(string? id, string? date,
        GetStationDeparturesHandler handler, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(id, out var stationId) || stationId == Guid.Empty) return JourneyReadProblemMapping.InvalidId("id");
        var result = await JourneyReadProblemMapping.Safe(async () =>
        {
            var response = await handler.HandleAsync(new GetStationDeparturesQuery(stationId, date), cancellationToken);
            return StationDepartureProblemMapping.Map(response);
        });
        return result;
    }
}
