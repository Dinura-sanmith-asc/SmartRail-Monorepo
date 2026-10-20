using SmartRail.Application.DriverOperations;
using SmartRail.Application.Identity.Authorization;

namespace SmartRail.Infrastructure.DriverOperations;

/// <summary>Reads genuine current Driver Operations ownership evidence; no role claims or cached allows.</summary>
public sealed class PostgreSqlDriverJourneyAssignmentEvidence(IDriverOperationsRepository repository) : IDriverJourneyAssignmentEvidence
{
    public async Task<EvidenceCheck> CheckAssignmentAsync(string verifiedDriverSubject, Guid journeyId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(verifiedDriverSubject) || journeyId == Guid.Empty)
            return new(EvidenceOutcome.Missing);
        try
        {
            var assignment = await repository.FindActiveAssignmentAsync(verifiedDriverSubject, journeyId, cancellationToken);
            return assignment is not null && assignment.IsActive && assignment.DriverSubject == verifiedDriverSubject && assignment.JourneyInstanceId == journeyId
                ? new(EvidenceOutcome.VerifiedMatch) : new(EvidenceOutcome.VerifiedNoMatch);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch { return new(EvidenceOutcome.Unavailable); }
    }
}
