using SmartRail.Application.DriverOperations;
using SmartRail.Application.Identity.Authorization;
using SmartRail.Application.JourneysStatus.Boundaries;
using SmartRail.Application.JourneysStatus.Queries;
using SmartRail.Domain.Identity;

namespace SmartRail.Infrastructure.DriverOperations;

/// <summary>Verifies genuine authored evidence for a new official publication, without publishing anything itself.</summary>
public sealed class StoredDriverObservationVerifier(IDriverOperationsRepository repository,
    ResolveCurrentRolesHandler currentRoles, IJourneyReadRepository journeys) : IDriverObservationVerifier
{
    public async Task<DriverObservationVerification> VerifyAsync(string verifiedActorSubject, Guid observationId,
        Guid journeyInstanceId, CancellationToken cancellationToken = default)
    {
        if (observationId == Guid.Empty || journeyInstanceId == Guid.Empty)
            return DriverObservationVerification.Malformed();
        if (string.IsNullOrWhiteSpace(verifiedActorSubject)) return DriverObservationVerification.MissingOrInaccessible();
        try
        {
            var roles = await currentRoles.HandleAsync(new(new(verifiedActorSubject)), cancellationToken);
            if (roles.Outcome == RoleResolutionOutcome.Unavailable) return DriverObservationVerification.Unavailable();
            if (!RolePermissionCatalog.IsGranted(roles.Roles, PermissionAction.ReviewDriverObservations))
                return DriverObservationVerification.MissingOrInaccessible();
            var observation = await repository.FindObservationAsync(observationId, cancellationToken);
            if (observation is null) return DriverObservationVerification.MissingOrInaccessible();
            if (observation.JourneyInstanceId != journeyInstanceId) return DriverObservationVerification.StaleOrIncompatible();
            var assignment = await repository.FindAssignmentAsync(observation.AssignmentId, cancellationToken);
            if (assignment is null || !assignment.IsActive || assignment.DriverSubject != observation.DriverSubject || assignment.JourneyInstanceId != journeyInstanceId)
                return DriverObservationVerification.StaleOrIncompatible();
            var history = await journeys.ReadHistoryAsync(journeyInstanceId, cancellationToken);
            if (history.Outcome == JourneyReadOutcome.Unavailable) return DriverObservationVerification.Unavailable();
            if (history.Outcome != JourneyReadOutcome.Found || history.Revisions is null ||
                !history.Revisions.Any(x => x.JourneyInstanceId == journeyInstanceId && x.Revision == observation.JourneyRevision))
                return DriverObservationVerification.StaleOrIncompatible();
            return DriverObservationVerification.Verified();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch { return DriverObservationVerification.Unavailable(); }
    }
}
