namespace SmartRail.Application.JourneysStatus.Boundaries;

/// <summary>Verifies an observation through its owning Driver Operations module.</summary>
public interface IDriverObservationVerifier
{
    Task<DriverObservationVerification> VerifyAsync(string verifiedActorSubject, Guid observationId,
        Guid journeyInstanceId, CancellationToken cancellationToken = default);
}

public enum DriverObservationVerificationOutcome { Verified, Malformed, MissingOrInaccessible, StaleOrIncompatible, Unavailable }
public sealed record DriverObservationVerification(DriverObservationVerificationOutcome Outcome)
{
    public static DriverObservationVerification Verified() => new(DriverObservationVerificationOutcome.Verified);
    public static DriverObservationVerification Malformed() => new(DriverObservationVerificationOutcome.Malformed);
    public static DriverObservationVerification MissingOrInaccessible() => new(DriverObservationVerificationOutcome.MissingOrInaccessible);
    public static DriverObservationVerification StaleOrIncompatible() => new(DriverObservationVerificationOutcome.StaleOrIncompatible);
    public static DriverObservationVerification Unavailable() => new(DriverObservationVerificationOutcome.Unavailable);
}
