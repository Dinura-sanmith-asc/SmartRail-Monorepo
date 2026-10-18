using SmartRail.Application.Identity.Authorization;

namespace SmartRail.Infrastructure.Identity.Maintenance;

/// <summary>Production-safe default until a controlled maintenance authority is approved and configured.</summary>
public sealed class DenyAllIdentityMaintenanceAuthority : IIdentityMaintenanceAuthority
{
    public Task<MaintenanceAuthorityDecision> AuthorizeAsync(
        string verifiedActorSubject,
        MaintenanceAction action,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new MaintenanceAuthorityDecision(MaintenanceAuthorityOutcome.Denied));
}
