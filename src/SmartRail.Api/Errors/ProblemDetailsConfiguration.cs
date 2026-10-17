using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace SmartRail.Api.Errors;

public static class ProblemDetailsConfiguration
{
    public static IServiceCollection AddSmartRailProblemDetails(this IServiceCollection services)
    {
        services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
        {
            context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
            context.ProblemDetails.Extensions.TryAdd("code", context.ProblemDetails.Status switch
            {
                StatusCodes.Status400BadRequest => "invalid_request",
                StatusCodes.Status404NotFound => "not_found",
                StatusCodes.Status503ServiceUnavailable => "source_unavailable",
                _ => "internal_error"
            });
        });
        services.AddExceptionHandler<SafeApiExceptionHandler>();
        return services;
    }

    public static IApplicationBuilder UseSmartRailProblemDetails(this IApplicationBuilder app) => app.UseExceptionHandler();

    public static IResult Create(
        int status,
        string title,
        string code,
        IDictionary<string, string[]>? errors = null)
    {
        var extensions = new Dictionary<string, object?> { ["code"] = code };
        if (errors is not null && errors.Count > 0) extensions["errors"] = errors;
        return Results.Problem(
            statusCode: status,
            title: title,
            type: $"https://smartrail.invalid/problems/{code}",
            extensions: extensions);
    }
}

public sealed class SafeApiExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not BadHttpRequestException) return false;
        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Request parameters are missing or malformed.",
            Type = "https://smartrail.invalid/problems/invalid_request"
        };
        problem.Extensions["code"] = "invalid_request";
        problem.Extensions["errors"] = new Dictionary<string, string[]> { ["request"] = ["Check required path and query parameters."] };
        await problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem
        });
        return true;
    }
}
