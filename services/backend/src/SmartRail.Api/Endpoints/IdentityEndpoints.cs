using System.Security.Claims;
using Microsoft.OpenApi;
using SmartRail.Api.Contracts.Identity;
using SmartRail.Api.Contracts.RailwayNetwork;
using SmartRail.Api.Errors;
using SmartRail.Application.Identity.Authorization;
using SmartRail.Application.Identity.Queries;

namespace SmartRail.Api.Endpoints;

public static class IdentityEndpoints
{
    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/me", GetCurrentIdentityAsync)
            .WithName("GetCurrentIdentity")
            .WithTags("Identity")
            .WithSummary("Get the authenticated caller's SmartRail identity")
            .WithDescription("Returns only the validated Cognito subject and currently active recognized SmartRail role labels.")
            .Produces<CurrentIdentityResponse>(StatusCodes.Status200OK)
            .Produces<ProblemResponse>(StatusCodes.Status401Unauthorized, contentType: "application/problem+json")
            .Produces<ProblemResponse>(StatusCodes.Status503ServiceUnavailable, contentType: "application/problem+json")
            .RequireAuthorization()
            .AddOpenApiOperationTransformer((operation, context, _) =>
            {
                if (context.Description.HttpMethod == "GET" && context.Description.RelativePath?.Trim('/') == "api/me")
                {
                    operation.Security ??= [];
                    var requirement = new OpenApiSecurityRequirement
                    {
                        [new OpenApiSecuritySchemeReference("CognitoAccessToken", context.Document)] = []
                    };
                    operation.Security.Add(requirement);
                    if (operation.Responses?.TryGetValue("200", out var success) == true)
                        success.Description = "Verified caller subject and active recognized role labels.";
                    if (operation.Responses?.TryGetValue("401", out var unauthorized) == true)
                        unauthorized.Description = "Missing or invalid access token.";
                    if (operation.Responses?.TryGetValue("503", out var unavailable) == true)
                        unavailable.Description = "The current role mapping could not be verified.";
                }
                return Task.CompletedTask;
            });
        return endpoints;
    }

    private static async Task<IResult> GetCurrentIdentityAsync(HttpContext context, GetCurrentIdentityHandler handler, CancellationToken cancellationToken)
    {
        var subject = context.User.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(subject))
            return ProblemDetailsConfiguration.Create(StatusCodes.Status401Unauthorized, "Authentication is required.", "authentication_required");

        var result = await handler.HandleAsync(new(new VerifiedPrincipal(subject)), cancellationToken);
        if (result.Outcome == CurrentIdentityOutcome.Unavailable)
            return ProblemDetailsConfiguration.Create(StatusCodes.Status503ServiceUnavailable,
                "Identity information is temporarily unavailable.", "authorization_unavailable");

        var response = new CurrentIdentityResponse(result.Subject!, result.Roles.Select(CurrentIdentityResponse.ToExternalRoleLabel).ToArray());
        return Results.Ok(response);
    }
}
