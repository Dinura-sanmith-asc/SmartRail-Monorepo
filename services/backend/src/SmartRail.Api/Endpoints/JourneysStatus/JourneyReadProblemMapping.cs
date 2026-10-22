using System.Data.Common;
using SmartRail.Api.Errors;
using SmartRail.Application.JourneysStatus.Queries;
using SmartRail.Application.JourneysStatus.Projections;

namespace SmartRail.Api.Endpoints.JourneysStatus;

internal static class JourneyReadProblemMapping
{
    public static IResult InvalidId(string parameter) => ProblemDetailsConfiguration.Create(
        StatusCodes.Status400BadRequest, "Journey identifier is invalid.", "invalid_journey_id",
        new Dictionary<string, string[]> { [parameter] = ["Journey ID must be a valid UUID."] });

    public static IResult NotFound() => ProblemDetailsConfiguration.Create(
        StatusCodes.Status404NotFound, "Journey not found.", "journey_not_found");

    public static IResult Unavailable() => ProblemDetailsConfiguration.Create(
        StatusCodes.Status503ServiceUnavailable, "Journey publication data is temporarily unavailable.",
        "journey_data_unavailable");

    public static async Task<IResult> Safe(Func<Task<IResult>> operation)
    {
        try { return await operation(); }
        catch (DbException) { return Unavailable(); }
        catch (TimeoutException) { return Unavailable(); }
        catch (JourneyReadProjectionException) { return Unavailable(); }
    }
}
