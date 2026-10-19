using SmartRail.Application.JourneysStatus.Publication;
using SmartRail.Domain.JourneysStatus;
using SmartRail.Domain.JourneysStatus.Entities;
using SmartRail.Domain.JourneysStatus.Rules;

namespace SmartRail.Application.JourneysStatus.Validation;

public static class JourneyPublicationValidator
{
    public static bool IsValidStatusRequest(PublishJourneyStatusCommand command) =>
        command.JourneyInstanceId != Guid.Empty && command.ExpectedJourneyRevision > 0 &&
        !string.IsNullOrWhiteSpace(command.VerifiedActorSubject) &&
        (command.Status.HasValue || command.RouteDiversion is not null) &&
        (!command.Status.HasValue || Enum.IsDefined(command.Status.Value)) &&
        (!command.CorrectionOfUpdateId.HasValue || command.CorrectionOfUpdateId.Value != Guid.Empty) &&
        (!command.DriverObservationId.HasValue || command.DriverObservationId.Value != Guid.Empty) &&
        (command.RouteDiversion is null || IsValidDiversion(command.RouteDiversion));

    public static bool IsValidStopRequest(PublishJourneyStopCommand command) =>
        command.JourneyInstanceId != Guid.Empty && command.JourneyStopId != Guid.Empty && command.ExpectedJourneyRevision > 0 &&
        !string.IsNullOrWhiteSpace(command.VerifiedActorSubject) &&
        (command.Platform is null || !string.IsNullOrWhiteSpace(command.Platform) && command.Platform.Trim().Length <= 100) &&
        (command.PlatformSpecified || command.Platform is not null || command.OperationalArrivalSpecified || command.OperationalArrivalAt.HasValue || command.OperationalDepartureSpecified || command.OperationalDepartureAt.HasValue) &&
        (!command.CorrectionOfUpdateId.HasValue || command.CorrectionOfUpdateId.Value != Guid.Empty) &&
        (!command.DriverObservationId.HasValue || command.DriverObservationId.Value != Guid.Empty);

    public static bool IsValidTransition(JourneyStatus current, JourneyStatus next, Guid? correctionOfUpdateId)
    {
        try
        {
            JourneyLifecycle.EnsureTransition(current, next, correctionOfUpdateId.HasValue);
            return true;
        }
        catch (InvalidOperationException) { return false; }
    }

    public static bool IsValidDiversion(IReadOnlyCollection<DivertedStopInput> stops)
    {
        if (stops.Count == 0 || stops.Any(stop => stop.StationId == Guid.Empty || stop.RouteStopId == Guid.Empty ||
            stop.StopOrder <= 0 || stop.Platform is not null && (string.IsNullOrWhiteSpace(stop.Platform) || stop.Platform.Trim().Length > 100)))
            return false;
        return stops.Select(stop => stop.StopOrder).Order().SequenceEqual(Enumerable.Range(1, stops.Count));
    }

    public static bool IsValidRetainedStopSet(IReadOnlyCollection<DivertedStopInput> replacement,
        IReadOnlySet<Guid> currentJourneyStopIds)
    {
        try
        {
            JourneyDiversionRules.Validate(replacement.Select(x => new DiversionStopIdentity(x.JourneyStopId, x.StopOrder)).ToArray(), currentJourneyStopIds);
            return true;
        }
        catch (ArgumentException) { return false; }
    }
}
