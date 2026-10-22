using SmartRail.Domain.Identity;
using SmartRail.Domain.Identity.Entities;

namespace SmartRail.Application.Identity.Persistence;

/// <summary>Persistence boundary for current role state and controlled role lifecycle changes.</summary>
public interface IRoleAssignmentRepository
{
    Task<IReadOnlyList<RoleCode>> GetActiveRolesAsync(string verifiedCognitoSubject, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RoleAssignment>> GetActiveAssignmentsAsync(string verifiedCognitoSubject, CancellationToken cancellationToken = default);
    Task GrantAsync(RoleAssignment assignment, RoleAssignmentAudit audit, CancellationToken cancellationToken = default);
    Task<bool> RevokeAsync(Guid assignmentId, string verifiedActorSubject, DateTimeOffset occurredAtUtc,
        Guid auditId, string? evidenceReference = null, CancellationToken cancellationToken = default);
}

/// <summary>Persistence boundary for current station scope and controlled scope lifecycle changes.</summary>
public interface IStationScopeRepository
{
    Task<IReadOnlyList<Guid>> GetActiveStationIdsAsync(string verifiedCognitoSubject, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StationScopeAssignment>> GetActiveAssignmentsAsync(string verifiedCognitoSubject, CancellationToken cancellationToken = default);
    Task GrantAsync(StationScopeAssignment assignment, StationScopeAssignmentAudit audit, CancellationToken cancellationToken = default);
    Task<bool> RevokeAsync(Guid assignmentId, string verifiedActorSubject, DateTimeOffset occurredAtUtc,
        Guid auditId, string? evidenceReference = null, CancellationToken cancellationToken = default);
}
