using Microsoft.AspNetCore.Http;
using SmartRail.Api.Errors;
using SmartRail.Application.Incidents.UseCases;

namespace SmartRail.Api.Endpoints.Incidents;

public static class IncidentProblemMapping
{
    public static IResult ForList(SmartRail.Application.Incidents.UseCases.ListIncidentsResult result) => result.Outcome switch
    {
        SmartRail.Application.Incidents.UseCases.ListIncidentsOutcome.Unauthorized => ProblemDetailsConfiguration.Create(
            StatusCodes.Status401Unauthorized, "Authentication is required.", "unauthorized"),
        SmartRail.Application.Incidents.UseCases.ListIncidentsOutcome.Forbidden => ProblemDetailsConfiguration.Create(
            StatusCodes.Status403Forbidden, "The caller is not authorized to view incidents.", "forbidden"),
        SmartRail.Application.Incidents.UseCases.ListIncidentsOutcome.Unavailable => ProblemDetailsConfiguration.Create(
            StatusCodes.Status503ServiceUnavailable, "Incident or authorization information is temporarily unavailable.", "authority_unavailable"),
        _ => throw new ArgumentOutOfRangeException(nameof(result))
    };

    public static IResult ForCreate(CreateIncidentResult result) => result.Outcome switch
    {
        CreateIncidentOutcome.Invalid => ProblemDetailsConfiguration.Create(StatusCodes.Status400BadRequest,
            "The incident request is invalid.", "invalid_incident_request", result.Errors?.ToDictionary(x => x.Key, x => x.Value)),
        CreateIncidentOutcome.Unauthorized => ProblemDetailsConfiguration.Create(StatusCodes.Status401Unauthorized,
            "Authentication is required.", "authentication_required"),
        CreateIncidentOutcome.Forbidden => ProblemDetailsConfiguration.Create(StatusCodes.Status403Forbidden,
            "Permission or required station scope is denied.", "permission_denied"),
        CreateIncidentOutcome.NotFound => ProblemDetailsConfiguration.Create(StatusCodes.Status404NotFound,
            "A referenced resource was not found or is inaccessible.", "reference_not_found"),
        CreateIncidentOutcome.Conflict => ProblemDetailsConfiguration.Create(StatusCodes.Status409Conflict,
            "The request conflicts with current incident or reference state.", "incident_conflict"),
        _ => ProblemDetailsConfiguration.Create(StatusCodes.Status503ServiceUnavailable,
            "A required authorization, category or owner authority is unavailable.", "incident_authority_unavailable")
    };
}
