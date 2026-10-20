using SmartRail.Domain.DriverOperations.Entities;

namespace SmartRail.Application.DriverOperations;

/// <summary>Driver Operations owns persistent assignments and immutable observations.</summary>
public interface IDriverOperationsRepository
{
    Task<IReadOnlyList<DriverJourneyAssignment>> GetActiveAssignmentsAsync(string? driverSubject, CancellationToken cancellationToken = default);
    Task<DriverJourneyAssignment?> FindActiveAssignmentAsync(string driverSubject, Guid journeyInstanceId, CancellationToken cancellationToken = default);
    Task<DriverJourneyAssignment?> FindAssignmentAsync(Guid assignmentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DriverObservation>> GetObservationsAsync(Guid journeyInstanceId, CancellationToken cancellationToken = default);
    Task<DriverObservation?> FindObservationAsync(Guid observationId, CancellationToken cancellationToken = default);
    Task<DriverObservation?> FindRequestAsync(string driverSubject, Guid requestId, CancellationToken cancellationToken = default);
    Task AddObservationAsync(DriverObservation observation, CancellationToken cancellationToken = default);
    Task GrantAssignmentAsync(DriverJourneyAssignment assignment, DriverAssignmentAudit audit, CancellationToken cancellationToken = default);
    Task<bool> RevokeAssignmentAsync(Guid assignmentId, string actorSubject, DateTimeOffset revokedAtUtc, Guid auditId,
        string evidenceReference, CancellationToken cancellationToken = default);
}
