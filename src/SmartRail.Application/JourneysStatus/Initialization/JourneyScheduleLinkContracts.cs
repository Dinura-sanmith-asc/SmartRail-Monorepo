namespace SmartRail.Application.JourneysStatus.Initialization;

public sealed record JourneyScheduleLinkRequest(Guid ServiceId, Guid ScheduleId, int ScheduleRevision,
    DateOnly OperationDate, TimeOnly FirstDepartureOccurrence);
public sealed record JourneyScheduleStop(Guid JourneyStopId, Guid StationId, Guid RouteStopId,
    int StopOrder, DateTimeOffset? PlannedArrivalAt, DateTimeOffset? PlannedDepartureAt);
public sealed record JourneyScheduleLink(IReadOnlyList<JourneyScheduleStop> Stops, DateTimeOffset VerifiedAt);
public enum ScheduleLinkVerificationStatus { Verified, Invalid, Unavailable }
public sealed record JourneyScheduleLinkResult(ScheduleLinkVerificationStatus Status, JourneyScheduleLink? Link);

public interface IJourneyScheduleLinkVerifier
{
    Task<JourneyScheduleLinkResult> VerifyAsync(JourneyScheduleLinkRequest request, CancellationToken cancellationToken);
}
