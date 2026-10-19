using System.Text.Json;
using SmartRail.Application.JourneysStatus.Boundaries;
using SmartRail.Application.JourneysStatus.Persistence;
using SmartRail.Domain.Identity;
using SmartRail.Domain.JourneysStatus;
using SmartRail.Domain.JourneysStatus.Entities;
using SmartRail.Domain.JourneysStatus.Rules;
using SmartRail.Application.JourneysStatus.Validation;

namespace SmartRail.Application.JourneysStatus.Publication;

public sealed class JourneyPublicationHandler(
    IJourneyPublicationRepository repository,
    IJourneyPublicationAuthorization authorization,
    IDriverObservationVerifier observationVerifier,
    TimeProvider timeProvider)
{
    public Task<JourneyPublicationResult> PublishStatusAsync(PublishJourneyStatusCommand command, CancellationToken cancellationToken = default) =>
        PublishAsync(command, cancellationToken);

    public async Task<JourneyPublicationResult> UpdateStopAsync(PublishJourneyStopCommand command, CancellationToken cancellationToken = default)
    {
        if (!JourneyPublicationValidator.IsValidStopRequest(command))
            return JourneyPublicationResult.Of(JourneyPublicationOutcome.Invalid);

        var decision = await AuthorizeAsync(command.VerifiedActorSubject!, command.JourneyInstanceId,
            PermissionAction.ChangePlatformOrOperationalTiming, cancellationToken);
        if (decision.Outcome == PublicationAuthorizationOutcome.Unavailable) return JourneyPublicationResult.Of(JourneyPublicationOutcome.Unavailable);
        if (decision.Outcome != PublicationAuthorizationOutcome.Allowed) return JourneyPublicationResult.Of(JourneyPublicationOutcome.Unauthorized);

        var state = await FindCurrentAsync(command.JourneyInstanceId, cancellationToken);
        if (state.Unavailable) return JourneyPublicationResult.Of(JourneyPublicationOutcome.Unavailable);
        var currentState = state.Value;
        if (currentState is null) return JourneyPublicationResult.Of(JourneyPublicationOutcome.NotFound);
        if (currentState.Journey.CurrentRevision != command.ExpectedJourneyRevision) return JourneyPublicationResult.Of(JourneyPublicationOutcome.Conflict);
        var currentStop = currentState.Stops.SingleOrDefault(x => x.JourneyStopId == command.JourneyStopId);
        if (currentStop is null) return JourneyPublicationResult.Of(JourneyPublicationOutcome.NotFound);

        var observation = await VerifyObservationAsync(command.VerifiedActorSubject!, command.DriverObservationId, command.JourneyInstanceId, cancellationToken);
        if (observation is not null) return JourneyPublicationResult.Of(observation.Value);
        var correction = await ValidateCorrectionAsync(command.JourneyInstanceId, command.ExpectedJourneyRevision + 1, command.CorrectionOfUpdateId, cancellationToken);
        if (correction is not null) return JourneyPublicationResult.Of(correction.Value);

        var now = timeProvider.GetUtcNow();
        var next = command.ExpectedJourneyRevision + 1;
        var updateId = Guid.NewGuid();
        var kind = command.CorrectionOfUpdateId.HasValue ? JourneyPublicationKind.Correction : JourneyPublicationKind.StopChange;
        var publicType = command.CorrectionOfUpdateId.HasValue ? "correction" : "stopChange";
        var platformSpecified = command.PlatformSpecified || command.Platform is not null;
        var arrivalSpecified = command.OperationalArrivalSpecified || command.OperationalArrivalAt.HasValue;
        var departureSpecified = command.OperationalDepartureSpecified || command.OperationalDepartureAt.HasValue;
        var evidence = JsonSerializer.Serialize(new { changeType = "stopChange", journeyStopId = command.JourneyStopId,
            platform = command.Platform, operationalArrivalAt = command.OperationalArrivalAt,
            operationalDepartureAt = command.OperationalDepartureAt,
            platformSpecified, arrivalSpecified, departureSpecified });
        var revision = new JourneyRevision(command.JourneyInstanceId, next, currentState.Revision.Status, now,
            JourneySource.ControllerManual.ToString(), publicType, updateId, kind, command.ExpectedJourneyRevision,
            command.VerifiedActorSubject, command.DriverObservationId, command.CorrectionOfUpdateId, evidence);
        var stops = currentState.Stops.Select(stop => Copy(stop, next,
            stop.JourneyStopId == command.JourneyStopId && platformSpecified ? command.Platform : stop.Platform,
            stop.JourneyStopId == command.JourneyStopId && arrivalSpecified ? command.OperationalArrivalAt : stop.OperationalArrivalAt,
            stop.JourneyStopId == command.JourneyStopId && departureSpecified ? command.OperationalDepartureAt : stop.OperationalDepartureAt,
            stop.JourneyStopId == command.JourneyStopId && (arrivalSpecified || departureSpecified) ? now : stop.TimingUpdatedAt,
            stop.JourneyStopId == command.JourneyStopId && (arrivalSpecified || departureSpecified) ? JourneySource.ControllerManual.ToString() : stop.TimingSource)).ToArray();
        return await AppendAsync(command.JourneyInstanceId, command.ExpectedJourneyRevision, revision, stops, cancellationToken);
    }

    private async Task<JourneyPublicationResult> PublishAsync(PublishJourneyStatusCommand command, CancellationToken cancellationToken)
    {
        if (!JourneyPublicationValidator.IsValidStatusRequest(command))
            return JourneyPublicationResult.Of(JourneyPublicationOutcome.Invalid);

        var actionDecision = await AuthorizeAsync(command.VerifiedActorSubject!, command.JourneyInstanceId,
            PermissionAction.PublishOfficialJourneyStatus, cancellationToken);
        if (actionDecision.Outcome == PublicationAuthorizationOutcome.Unavailable) return JourneyPublicationResult.Of(JourneyPublicationOutcome.Unavailable);
        if (actionDecision.Outcome != PublicationAuthorizationOutcome.Allowed) return JourneyPublicationResult.Of(JourneyPublicationOutcome.Unauthorized);
        if (command.Status == JourneyStatus.Cancelled)
        {
            var cancellation = await AuthorizeAsync(command.VerifiedActorSubject!, command.JourneyInstanceId,
                PermissionAction.ConfirmJourneyCancellation, cancellationToken);
            if (cancellation.Outcome == PublicationAuthorizationOutcome.Unavailable) return JourneyPublicationResult.Of(JourneyPublicationOutcome.Unavailable);
            if (cancellation.Outcome != PublicationAuthorizationOutcome.Allowed) return JourneyPublicationResult.Of(JourneyPublicationOutcome.Unauthorized);
        }

        var lookup = await FindCurrentAsync(command.JourneyInstanceId, cancellationToken);
        if (lookup.Unavailable) return JourneyPublicationResult.Of(JourneyPublicationOutcome.Unavailable);
        var state = lookup.Value;
        if (state is null) return JourneyPublicationResult.Of(JourneyPublicationOutcome.NotFound);
        if (state.Journey.CurrentRevision != command.ExpectedJourneyRevision) return JourneyPublicationResult.Of(JourneyPublicationOutcome.Conflict);

        var observation = await VerifyObservationAsync(command.VerifiedActorSubject!, command.DriverObservationId, command.JourneyInstanceId, cancellationToken);
        if (observation is not null) return JourneyPublicationResult.Of(observation.Value);
        var correction = await ValidateCorrectionAsync(command.JourneyInstanceId, command.ExpectedJourneyRevision + 1, command.CorrectionOfUpdateId, cancellationToken);
        if (correction is not null) return JourneyPublicationResult.Of(correction.Value);

        var newStatus = command.Status ?? state.Revision.Status;
        if (command.Status.HasValue)
        {
            if (!JourneyPublicationValidator.IsValidTransition(state.Revision.Status, newStatus, command.CorrectionOfUpdateId))
                return JourneyPublicationResult.Of(JourneyPublicationOutcome.Invalid);
        }

        var now = timeProvider.GetUtcNow();
        var next = command.ExpectedJourneyRevision + 1;
        IReadOnlyList<JourneyRevisionStop> stops;
        string publicType;
        JourneyPublicationKind kind;
        if (command.RouteDiversion is not null)
        {
                if (!JourneyPublicationValidator.IsValidRetainedStopSet(command.RouteDiversion, state.Stops.Select(x => x.JourneyStopId).ToHashSet()))
                return JourneyPublicationResult.Of(JourneyPublicationOutcome.Invalid);
            var existing = state.Stops.ToDictionary(x => x.JourneyStopId);
            if (command.RouteDiversion.Any(input => input.JourneyStopId is { } retainedId &&
                existing[retainedId].StationId != input.StationId))
                return JourneyPublicationResult.Of(JourneyPublicationOutcome.Invalid);
            stops = command.RouteDiversion.Select(input =>
            {
                var id = input.JourneyStopId ?? Guid.NewGuid();
                existing.TryGetValue(id, out var prior);
                return new JourneyRevisionStop(command.JourneyInstanceId, next, id, input.StopOrder, input.StationId,
                    input.RouteStopId, input.PlannedArrivalAt, input.PlannedDepartureAt,
                    input.OperationalArrivalAt, input.OperationalDepartureAt, input.Platform,
                    input.OperationalArrivalAt.HasValue || input.OperationalDepartureAt.HasValue ? JourneySource.ControllerManual.ToString() : prior?.TimingSource,
                    input.OperationalArrivalAt.HasValue || input.OperationalDepartureAt.HasValue ? now : prior?.TimingUpdatedAt,
                    prior?.AmbiguityState ?? "none");
            }).ToArray();
            publicType = command.CorrectionOfUpdateId.HasValue ? "correction" : "routeDiversion";
            kind = command.CorrectionOfUpdateId.HasValue ? JourneyPublicationKind.Correction : JourneyPublicationKind.RouteDiversion;
        }
        else
        {
            stops = state.Stops.Select(x => Copy(x, next, x.Platform, x.OperationalArrivalAt, x.OperationalDepartureAt, x.TimingUpdatedAt, x.TimingSource)).ToArray();
            publicType = command.CorrectionOfUpdateId.HasValue ? "correction" : "status";
            kind = command.CorrectionOfUpdateId.HasValue ? JourneyPublicationKind.Correction : JourneyPublicationKind.Status;
        }
        var publicEvidence = JsonSerializer.Serialize(new { status = newStatus.ToString(), changeType = command.RouteDiversion is not null ? "routeDiversion" : "status" });
        var revision = new JourneyRevision(command.JourneyInstanceId, next, newStatus, now,
            JourneySource.ControllerManual.ToString(), publicType, Guid.NewGuid(), kind, command.ExpectedJourneyRevision,
            command.VerifiedActorSubject, command.DriverObservationId, command.CorrectionOfUpdateId, publicEvidence);
        return await AppendAsync(command.JourneyInstanceId, command.ExpectedJourneyRevision, revision, stops, cancellationToken);
    }

    private async Task<JourneyPublicationOutcome?> VerifyObservationAsync(string actor, Guid? observationId, Guid journeyId, CancellationToken cancellationToken)
    {
        if (!observationId.HasValue) return null;
        if (observationId == Guid.Empty) return JourneyPublicationOutcome.Invalid;
        DriverObservationVerification verification;
        try { verification = await observationVerifier.VerifyAsync(actor, observationId.Value, journeyId, cancellationToken); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch { return JourneyPublicationOutcome.Unavailable; }
        return verification.Outcome switch
        {
            DriverObservationVerificationOutcome.Verified => null,
            DriverObservationVerificationOutcome.Malformed => JourneyPublicationOutcome.Invalid,
            DriverObservationVerificationOutcome.MissingOrInaccessible => JourneyPublicationOutcome.NotFound,
            DriverObservationVerificationOutcome.StaleOrIncompatible => JourneyPublicationOutcome.Conflict,
            DriverObservationVerificationOutcome.Unavailable => JourneyPublicationOutcome.Unavailable,
            _ => JourneyPublicationOutcome.Unavailable
        };
    }

    private async Task<JourneyPublicationOutcome?> ValidateCorrectionAsync(Guid journeyId, int beforeRevision, Guid? correctionId, CancellationToken cancellationToken)
    {
        if (!correctionId.HasValue) return null;
        if (correctionId == Guid.Empty) return JourneyPublicationOutcome.Invalid;
        try { return await repository.HasEarlierUpdateAsync(journeyId, correctionId.Value, beforeRevision, cancellationToken)
                ? null : JourneyPublicationOutcome.Invalid; }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch { return JourneyPublicationOutcome.Unavailable; }
    }

    private async Task<JourneyPublicationResult> AppendAsync(Guid journeyId, int expectedRevision,
        JourneyRevision revision, IReadOnlyCollection<JourneyRevisionStop> stops, CancellationToken cancellationToken)
    {
        JourneyRevisionAppendResult result;
        try { result = await repository.AppendAsync(journeyId, expectedRevision, revision, stops, cancellationToken); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch { return JourneyPublicationResult.Of(JourneyPublicationOutcome.Unavailable); }
        if (result.Outcome == JourneyRevisionAppendOutcome.NotFound) return JourneyPublicationResult.Of(JourneyPublicationOutcome.NotFound);
        if (result.Outcome == JourneyRevisionAppendOutcome.Stale) return JourneyPublicationResult.Of(JourneyPublicationOutcome.Conflict);
        var current = await FindCurrentAsync(journeyId, cancellationToken);
        return current.Unavailable || current.Value is null
            ? JourneyPublicationResult.Of(JourneyPublicationOutcome.Unavailable)
            : new(JourneyPublicationOutcome.Published, current.Value);
    }

    private async Task<PublicationAuthorizationDecision> AuthorizeAsync(string subject, Guid journeyId,
        PermissionAction action, CancellationToken cancellationToken)
    {
        try { return await authorization.AuthorizeAsync(subject, journeyId, action, cancellationToken); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch { return new(PublicationAuthorizationOutcome.Unavailable); }
    }

    private async Task<(JourneyPublicationState? Value, bool Unavailable)> FindCurrentAsync(Guid journeyId, CancellationToken cancellationToken)
    {
        try { return (await repository.FindCurrentAsync(journeyId, cancellationToken), false); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch { return (null, true); }
    }

    private static JourneyRevisionStop Copy(JourneyRevisionStop stop, int revision, string? platform,
        DateTimeOffset? arrival, DateTimeOffset? departure, DateTimeOffset? timingUpdatedAt, string? timingSource) =>
        new(stop.JourneyInstanceId, revision, stop.JourneyStopId, stop.StopOrder, stop.StationId, stop.RouteStopId,
            stop.PlannedArrivalAt, stop.PlannedDepartureAt, arrival, departure, platform, timingSource, timingUpdatedAt, stop.AmbiguityState);
}
