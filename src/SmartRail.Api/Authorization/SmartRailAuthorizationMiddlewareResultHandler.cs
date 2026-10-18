using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Mvc;
using SmartRail.Api.Errors;

namespace SmartRail.Api.Authorization;

public sealed class SmartRailAuthorizationMiddlewareResultHandler(IProblemDetailsService problemDetailsService)
    : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _fallback = new();

    public async Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Succeeded)
        {
            await next(context);
            return;
        }

        if (authorizeResult.Challenged)
        {
            context.Response.Headers.WWWAuthenticate = "Bearer";
            await WriteProblemAsync(context, StatusCodes.Status401Unauthorized, "Authentication is required.", "authentication_required");
            return;
        }

        if (authorizeResult.Forbidden)
        {
            var unavailable = authorizeResult.AuthorizationFailure?.FailureReasons
                .Any(reason => reason.Message == "smartrail_authorization_unavailable") == true;
            await WriteProblemAsync(context,
                unavailable ? StatusCodes.Status503ServiceUnavailable : StatusCodes.Status403Forbidden,
                unavailable ? "Authorization is temporarily unavailable." : "Permission is denied.",
                unavailable ? "authorization_unavailable" : "permission_denied");
            return;
        }

        await _fallback.HandleAsync(next, context, policy, authorizeResult);
    }

    private async Task WriteProblemAsync(HttpContext context, int status, string title, string code)
    {
        context.Response.StatusCode = status;
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Type = $"https://smartrail.invalid/problems/{code}"
        };
        problem.Extensions["code"] = code;
        await problemDetailsService.WriteAsync(new ProblemDetailsContext { HttpContext = context, ProblemDetails = problem });
    }
}
