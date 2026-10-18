using SmartRail.Application.Identity.Authorization;
using SmartRail.Domain.Identity;

namespace SmartRail.Application.Identity.Queries;

public sealed record GetCurrentIdentityQuery(VerifiedPrincipal Principal);
public enum CurrentIdentityOutcome { Verified, Unavailable }
/// <summary>Contains only the validated subject and current recognized internal role codes.</summary>
public sealed record CurrentIdentityResult(CurrentIdentityOutcome Outcome, string? Subject, IReadOnlyList<RoleCode> Roles)
{
    public static CurrentIdentityResult Verified(string subject, IReadOnlyList<RoleCode> roles) => new(CurrentIdentityOutcome.Verified, subject, roles);
    public static CurrentIdentityResult Unavailable() => new(CurrentIdentityOutcome.Unavailable, null, []);
}

public sealed class GetCurrentIdentityHandler(ResolveCurrentRolesHandler roleResolver)
{
    public async Task<CurrentIdentityResult> HandleAsync(GetCurrentIdentityQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.Principal is null || string.IsNullOrWhiteSpace(query.Principal.CognitoSubject)) return CurrentIdentityResult.Unavailable();
        var roles = await roleResolver.HandleAsync(new(query.Principal), cancellationToken);
        return roles.Outcome == RoleResolutionOutcome.Verified
            ? CurrentIdentityResult.Verified(query.Principal.CognitoSubject, roles.Roles)
            : CurrentIdentityResult.Unavailable();
    }
}
