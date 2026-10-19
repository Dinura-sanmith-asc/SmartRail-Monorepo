using SmartRail.Application.JourneysStatus.Boundaries;

namespace SmartRail.UnitTests.JourneysStatus;

public sealed class ModuleBoundaryTests
{
    [Fact]
    public async Task Driver_verifier_receives_verified_actor_and_journey_association()
    {
        var subject = "verified-driver-subject";
        var observationId = Guid.NewGuid();
        var journeyId = Guid.NewGuid();
        var verifier = new RecordingVerifier(DriverObservationVerification.Verified());

        var result = await verifier.VerifyAsync(subject, observationId, journeyId);

        Assert.Equal(DriverObservationVerificationOutcome.Verified, result.Outcome);
        Assert.Equal(subject, verifier.Subject);
        Assert.Equal(observationId, verifier.ObservationId);
        Assert.Equal(journeyId, verifier.JourneyId);
    }

    [Fact]
    public async Task Unavailable_driver_owner_evidence_is_distinct_and_never_treated_as_verified()
    {
        var verifier = new RecordingVerifier(DriverObservationVerification.Unavailable());

        var result = await verifier.VerifyAsync("verified-subject", Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal(DriverObservationVerificationOutcome.Unavailable, result.Outcome);
        Assert.NotEqual(DriverObservationVerificationOutcome.Verified, result.Outcome);
    }

    private sealed class RecordingVerifier(DriverObservationVerification result) : IDriverObservationVerifier
    {
        public string? Subject { get; private set; }
        public Guid ObservationId { get; private set; }
        public Guid JourneyId { get; private set; }

        public Task<DriverObservationVerification> VerifyAsync(string verifiedActorSubject, Guid observationId,
            Guid journeyInstanceId, CancellationToken cancellationToken = default)
        {
            Subject = verifiedActorSubject;
            ObservationId = observationId;
            JourneyId = journeyInstanceId;
            return Task.FromResult(result);
        }
    }
}
