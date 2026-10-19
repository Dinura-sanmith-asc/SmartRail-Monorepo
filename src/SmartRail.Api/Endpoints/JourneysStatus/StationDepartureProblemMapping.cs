using SmartRail.Api.Errors;
using SmartRail.Application.JourneysStatus.Queries.GetStationDepartures;

namespace SmartRail.Api.Endpoints.JourneysStatus;

internal static class StationDepartureProblemMapping
{
    public static IResult InvalidDate() => ProblemDetailsConfiguration.Create(
        StatusCodes.Status400BadRequest, "Station departure date is invalid.", "invalid_station_departure_date",
        new Dictionary<string, string[]> { ["date"] = ["Date must be a real calendar date in YYYY-MM-DD format."] });

    public static IResult StationNotFound() => ProblemDetailsConfiguration.Create(
        StatusCodes.Status404NotFound, "Published station was not found.", "station_not_found");

    public static IResult Unavailable() => ProblemDetailsConfiguration.Create(
        StatusCodes.Status503ServiceUnavailable, "Published departure information is temporarily unavailable.",
        "departure_data_unavailable");

    public static IResult Map(GetStationDeparturesResult result) => result.Outcome switch
    {
        StationDeparturesOutcome.InvalidDate => InvalidDate(),
        StationDeparturesOutcome.NotFound => StationNotFound(),
        StationDeparturesOutcome.Unavailable => Unavailable(),
        _ => Results.Ok((result.Departures ?? Array.Empty<SmartRail.Application.JourneysStatus.Projections.StationDepartureProjection>())
            .Select(item => new StationDepartureResponse(item.JourneyInstanceId, item.ServiceId,
                item.OperationDate, item.StationLocalDepartureDate, item.Revision, item.Status,
                item.StationId, item.StationName, item.StopOrder, item.ScheduledDepartureAt,
                item.OperationalDepartureAt, item.Platform, item.Source, item.UpdatedAt, item.Ambiguity)).ToArray())
    };
}
