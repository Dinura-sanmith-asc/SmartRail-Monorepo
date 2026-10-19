using SmartRail.Application.Incidents.References;

namespace SmartRail.Infrastructure.Incidents.References;

/// <summary>Production-safe default until genuine owner adapters are registered.</summary>
public sealed class UnavailableIncidentReferenceVerifier : IFeature001IncidentReferenceVerifier, IFeature003IncidentReferenceVerifier
{
    public Task<IncidentReferenceCheck> VerifyAsync(IncidentReference reference, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new IncidentReferenceCheck(IncidentReferenceOutcome.Unavailable));
    }
}
