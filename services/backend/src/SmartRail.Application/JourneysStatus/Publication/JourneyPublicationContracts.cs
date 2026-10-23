using SmartRail.Domain.Identity;
using SmartRail.Domain.JourneysStatus;
using SmartRail.Domain.JourneysStatus.Entities;
using SmartRail.Application.JourneysStatus.Persistence;

namespace SmartRail.Application.JourneysStatus.Publication;

public enum PublicationAuthorizationOutcome { Allowed, Denied, Unavailable }
public sealed record PublicationAuthorizationDecision(PublicationAuthorizationOutcome Outcome, JourneySource? Source = null);

/// <summary>Evaluates current Feature 002 grants and resource scope for this protected write.</summary>
public interface IJourneyPublicationAuthorization
{
    Task<PublicationAuthorizationDecision> AuthorizeAsync(string verifiedSubject, Guid journeyInstanceId,
        PermissionAction action, CancellationToken cancellationToken = default);

    Task<PublicationAuthorizationDecision> AuthorizeStopAsync(string verifiedSubject, Guid journeyInstanceId,
        Guid stationId, CancellationToken cancellationToken = default) =>
        AuthorizeAsync(verifiedSubject, journeyInstanceId, PermissionAction.ChangePlatformOrOperationalTiming, cancellationToken);
}

public sealed record JourneyPublicationState(Journey Journey, JourneyRevision Revision,
    IReadOnlyList<JourneyRevisionStop> Stops, IReadOnlyList<JourneyRevision> History);

public interface IJourneyPublicationRepository
{
    Task<JourneyPublicationState?> FindCurrentAsync(Guid journeyInstanceId, CancellationToken cancellationToken = default);
    Task<bool> HasEarlierUpdateAsync(Guid journeyInstanceId, Guid updateId, int beforeRevision, CancellationToken cancellationToken = default);
    Task<JourneyRevisionAppendResult> AppendAsync(Guid journeyInstanceId, int expectedRevision,
        JourneyRevision revision, IReadOnlyCollection<JourneyRevisionStop> stops, CancellationToken cancellationToken = default);
}

public sealed record DivertedStopInput(Guid? JourneyStopId, Guid StationId, int StopOrder, Guid? RouteStopId,
    DateTimeOffset? PlannedArrivalAt, DateTimeOffset? PlannedDepartureAt, DateTimeOffset? OperationalArrivalAt,
    DateTimeOffset? OperationalDepartureAt, string? Platform);

public sealed record PublishJourneyStatusCommand(Guid JourneyInstanceId, int ExpectedJourneyRevision,
    string? VerifiedActorSubject, JourneyStatus? Status, IReadOnlyList<DivertedStopInput>? RouteDiversion,
    Guid? DriverObservationId, Guid? CorrectionOfUpdateId);

public sealed record PublishJourneyStopCommand(Guid JourneyInstanceId, Guid JourneyStopId,
    int ExpectedJourneyRevision, string? VerifiedActorSubject, string? Platform,
    DateTimeOffset? OperationalArrivalAt, DateTimeOffset? OperationalDepartureAt,
    Guid? DriverObservationId, Guid? CorrectionOfUpdateId,
    bool PlatformSpecified = false, bool OperationalArrivalSpecified = false, bool OperationalDepartureSpecified = false);

public enum JourneyPublicationOutcome { Published, Invalid, Unauthorized, Unavailable, NotFound, Conflict }
public sealed record JourneyPublicationResult(JourneyPublicationOutcome Outcome, JourneyPublicationState? State = null)
{
    public static JourneyPublicationResult Of(JourneyPublicationOutcome outcome) => new(outcome);
}
