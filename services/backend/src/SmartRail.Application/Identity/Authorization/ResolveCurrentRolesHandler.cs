using SmartRail.Application.Identity.Persistence;
using SmartRail.Domain.Identity;

namespace SmartRail.Application.Identity.Authorization;

public enum RoleResolutionOutcome { Verified, Unavailable }
public sealed record CurrentRolesResult(RoleResolutionOutcome Outcome, IReadOnlyList<RoleCode> Roles)
{
    public static CurrentRolesResult Verified(IReadOnlyList<RoleCode> roles) => new(RoleResolutionOutcome.Verified, roles);
    public static CurrentRolesResult Unavailable() => new(RoleResolutionOutcome.Unavailable, []);
}
public sealed record ResolveCurrentRolesQuery(VerifiedPrincipal Principal);

/// <summary>Reads authoritative active role state on each call; no role claims or caching are consulted.</summary>
public sealed class ResolveCurrentRolesHandler(IRoleAssignmentRepository repository)
{
    public async Task<CurrentRolesResult> HandleAsync(ResolveCurrentRolesQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.Principal is null || string.IsNullOrWhiteSpace(query.Principal.CognitoSubject)) return CurrentRolesResult.Unavailable();
        try
        {
            var roles = await repository.GetActiveRolesAsync(query.Principal.CognitoSubject, cancellationToken);
            return CurrentRolesResult.Verified(roles.Distinct().Order().ToArray());
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch { return CurrentRolesResult.Unavailable(); }
    }
}
