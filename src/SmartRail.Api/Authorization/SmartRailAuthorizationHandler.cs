using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using SmartRail.Application.Identity.Authorization;
using SmartRail.Domain.Identity;

namespace SmartRail.Api.Authorization;

/// <summary>A policy requirement created by server-side feature code, never from caller role/scope input.</summary>
public sealed record SmartRailAuthorizationRequirement(PermissionAction Action, ProtectedResource Resource) : IAuthorizationRequirement;

public sealed class SmartRailAuthorizationHandler(AuthorizeOperationHandler applicationHandler)
    : AuthorizationHandler<SmartRailAuthorizationRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        SmartRailAuthorizationRequirement requirement)
    {
        var subject = context.User.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(subject))
        {
            context.Fail(new AuthorizationFailureReason(this, "smartrail_invalid_principal"));
            return;
        }

        var decision = await applicationHandler.HandleAsync(
            new(new VerifiedPrincipal(subject), requirement.Action, requirement.Resource));
        switch (decision.Outcome)
        {
            case AuthorizationOutcome.Allowed:
                context.Succeed(requirement);
                break;
            case AuthorizationOutcome.Unavailable:
                context.Fail(new AuthorizationFailureReason(this, "smartrail_authorization_unavailable"));
                break;
            default:
                context.Fail(new AuthorizationFailureReason(this, "smartrail_permission_denied"));
                break;
        }
    }
}
