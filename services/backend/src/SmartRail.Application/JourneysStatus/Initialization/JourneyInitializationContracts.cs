namespace SmartRail.Application.JourneysStatus.Initialization;

public sealed record InitializeJourneyCommand(Guid? JourneyInstanceId, Guid ServiceId, Guid ScheduleId,
    int ScheduleRevision, DateOnly OperationDate, TimeOnly FirstDepartureOccurrence,
    string SourceSystem, string SourceInstanceKey, string Provenance);
public enum JourneyInitializationAuthorityStatus { Authorized, Denied, Unavailable }
public sealed record JourneyInitializationAuthorityResult(JourneyInitializationAuthorityStatus Status,
    string? VerifiedActorSubject, string? TrustedSourceSystem)
{
    public bool IsAuthorized => Status == JourneyInitializationAuthorityStatus.Authorized;
}
public interface IJourneyInitializationAuthority
{
    Task<JourneyInitializationAuthorityResult> AuthorizeAsync(InitializeJourneyCommand command, CancellationToken cancellationToken);
}
public interface IJourneyInstanceIdIssuer
{
    Guid Issue(string sourceSystem, string sourceInstanceKey, Guid? authoritativeId);
}
public interface IJourneyManifestValidator
{
    bool IsValid(string? sourceSystem, string? sourceInstanceKey, string? provenance);
}
public enum JourneyInitializationOutcome { Created, Replayed, Denied, Invalid, Conflict, Unavailable }
public sealed record JourneyInitializationResult(JourneyInitializationOutcome Outcome, Guid? JourneyInstanceId);
