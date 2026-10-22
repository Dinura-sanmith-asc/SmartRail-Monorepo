using SmartRail.Application.Identity.Authorization;

namespace SmartRail.Infrastructure.Identity;

/// <summary>Fail-closed placeholders until the owning modules implement their evidence ports.</summary>
public sealed class UnavailableOwnerEvidence : IDriverJourneyAssignmentEvidence, IIncidentResourceEvidence, IPassengerOrderOwnershipEvidence
{
    public Task<EvidenceCheck> CheckAssignmentAsync(string verifiedDriverSubject, Guid journeyId, CancellationToken cancellationToken = default) =>
        Task.FromResult(new EvidenceCheck(EvidenceOutcome.Unavailable));

    public Task<IncidentResourceEvidence> ResolveIncidentAsync(Guid incidentId, CancellationToken cancellationToken = default) =>
        Task.FromResult(new IncidentResourceEvidence(EvidenceOutcome.Unavailable));

    public Task<EvidenceCheck> CheckOwnershipAsync(string verifiedPassengerSubject, Guid orderId, CancellationToken cancellationToken = default) =>
        Task.FromResult(new EvidenceCheck(EvidenceOutcome.Unavailable));
}
