using Microsoft.EntityFrameworkCore;
using SmartRail.Application.Identity.Authorization;

namespace SmartRail.Api.Catering;

public sealed class CateringOwnershipEvidence(CateringDbContext db) : IPassengerOrderOwnershipEvidence
{
    public async Task<EvidenceCheck> CheckOwnershipAsync(string verifiedPassengerSubject, Guid orderId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return new(await db.Correlations.AsNoTracking().AnyAsync(x => x.Id == orderId &&
                x.PassengerSubject == verifiedPassengerSubject, cancellationToken)
                ? EvidenceOutcome.VerifiedMatch : EvidenceOutcome.VerifiedNoMatch);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new(EvidenceOutcome.Unavailable);
        }
    }
}
