using SmartRail.Application.JourneysStatus.Boundaries;

namespace SmartRail.Infrastructure.JourneysStatus.Authorization;

/// <summary>Fails closed until the Driver Operations owner module supplies its verifier.</summary>
public sealed class UnavailableDriverObservationVerifier : IDriverObservationVerifier
{
    public Task<DriverObservationVerification> VerifyAsync(string verifiedActorSubject, Guid observationId,
        Guid journeyInstanceId, CancellationToken cancellationToken = default) =>
        Task.FromResult(DriverObservationVerification.Unavailable());
}
