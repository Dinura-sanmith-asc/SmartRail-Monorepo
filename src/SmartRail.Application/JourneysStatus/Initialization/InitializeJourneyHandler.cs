using SmartRail.Application.JourneysStatus.Persistence;
using SmartRail.Domain.JourneysStatus;
using SmartRail.Domain.JourneysStatus.Entities;
using SmartRail.Domain.JourneysStatus.Rules;

namespace SmartRail.Application.JourneysStatus.Initialization;

public sealed class InitializeJourneyHandler(IJourneyInitializationAuthority authority,
    IJourneyInstanceIdIssuer idIssuer, IJourneyManifestValidator manifestValidator, IJourneyScheduleLinkVerifier scheduleVerifier,
    IJourneyInitializationRepository repository, TimeProvider timeProvider)
{
    public async Task<JourneyInitializationResult> HandleAsync(InitializeJourneyCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var authorization = await authority.AuthorizeAsync(command, cancellationToken);
        if (authorization.Status == JourneyInitializationAuthorityStatus.Unavailable)
            return new(JourneyInitializationOutcome.Unavailable, null);
        if (!authorization.IsAuthorized || string.IsNullOrWhiteSpace(authorization.VerifiedActorSubject) ||
            !string.Equals(authorization.TrustedSourceSystem, command.SourceSystem?.Trim(), StringComparison.Ordinal))
            return new(JourneyInitializationOutcome.Denied, null);

        try
        {
            JourneyInitializationRules.ValidateSourceIdentity(command.SourceSystem, command.SourceInstanceKey, command.JourneyInstanceId);
            if (command.ServiceId == Guid.Empty || command.ScheduleId == Guid.Empty || command.ScheduleRevision <= 0 ||
                command.OperationDate == default || !manifestValidator.IsValid(command.SourceSystem, command.SourceInstanceKey, command.Provenance))
                return new(JourneyInitializationOutcome.Invalid, null);
        }
        catch (ArgumentException) { return new(JourneyInitializationOutcome.Invalid, null); }

        var linkRequest = new JourneyScheduleLinkRequest(command.ServiceId, command.ScheduleId, command.ScheduleRevision,
            command.OperationDate, command.FirstDepartureOccurrence);
        var linkResult = await scheduleVerifier.VerifyAsync(linkRequest, cancellationToken);
        if (linkResult.Status == ScheduleLinkVerificationStatus.Unavailable)
            return new(JourneyInitializationOutcome.Unavailable, null);
        if (linkResult.Status != ScheduleLinkVerificationStatus.Verified || linkResult.Link is null || linkResult.Link.Stops.Count == 0)
            return new(JourneyInitializationOutcome.Invalid, null);

        var journeyId = idIssuer.Issue(command.SourceSystem!, command.SourceInstanceKey!, command.JourneyInstanceId);
        var now = timeProvider.GetUtcNow();
        var journey = new Journey(journeyId, command.ServiceId, command.ScheduleId, command.ScheduleRevision,
            command.OperationDate, command.FirstDepartureOccurrence, command.SourceSystem!, command.SourceInstanceKey!,
            now, authorization.VerifiedActorSubject!, command.Provenance!);
        var baseline = new JourneyRevision(journeyId, 1, JourneyStatus.Scheduled, now,
            JourneySource.ScheduledBaseline.ToString());
        var stops = linkResult.Link.Stops.OrderBy(stop => stop.StopOrder)
            .Select(stop => new JourneyRevisionStop(journeyId, 1, stop.JourneyStopId, stop.StopOrder,
                stop.StationId, stop.RouteStopId, stop.PlannedArrivalAt, stop.PlannedDepartureAt)).ToArray();
        var audit = new JourneyInitializationAudit(Guid.NewGuid(), command.SourceSystem!, command.SourceInstanceKey!,
            JourneyInitializationDecision.Accepted, now, command.Provenance!, journeyId, authorization.VerifiedActorSubject,
            command.ServiceId, command.ScheduleId, command.ScheduleRevision, command.OperationDate, command.FirstDepartureOccurrence);
        var stored = await repository.InitializeAsync(new(journey, baseline, stops, audit,
            command.JourneyInstanceId.HasValue), cancellationToken);
        return stored.Outcome switch
        {
            JourneyInitializationStoreOutcome.Created => new(JourneyInitializationOutcome.Created, stored.Journey?.JourneyInstanceId),
            JourneyInitializationStoreOutcome.Replayed => new(JourneyInitializationOutcome.Replayed, stored.Journey?.JourneyInstanceId),
            _ => new(JourneyInitializationOutcome.Conflict, null)
        };
    }
}
