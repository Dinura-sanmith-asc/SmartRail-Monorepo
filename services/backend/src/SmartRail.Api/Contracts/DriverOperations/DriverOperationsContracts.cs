using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SmartRail.Application.DriverOperations;
using SmartRail.Application.JourneysStatus.Projections;
using SmartRail.Api.Endpoints.JourneysStatus;

namespace SmartRail.Api.Contracts.DriverOperations;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SubmitDriverObservationRequest([property: Required] Guid RequestId,
    [property: Required, MinLength(1), MaxLength(4000)] string? Text);
public sealed record DriverAssignmentListResponse([property: Required] IReadOnlyList<DriverAssignmentResponse> Items);
public sealed record DriverObservationListResponse([property: Required] IReadOnlyList<DriverObservationResponse> Items);
public sealed record DriverAssignmentResponse([property: Required] Guid AssignmentId,
    [property: Required] string DriverSubject, [property: Required] DriverJourneyContextResponse Journey)
{
    public static DriverAssignmentResponse From(DriverAssignmentView view) => new(view.AssignmentId, view.DriverSubject,
        new(GetJourneyEndpoint.ToResponse(view.Journey.Snapshot), view.Journey.Updates.Select(ToResponse).ToArray()));
    private static JourneyUpdateResponse ToResponse(JourneyUpdateView value) => new(value.UpdateId, value.JourneyInstanceId,
        value.Revision, value.Kind, value.Status, value.Source, value.PublishedAt, value.CorrectionOfUpdateId,
        ToResponse(value.PublicChange));
    private static JourneyPublicChangeResponse ToResponse(JourneyPublicChangeView value) => value switch
    {
        StatusPublicChangeView status => new StatusPublicChangeResponse(status.StatusValue),
        StopPublicChangeView stop => new StopPublicChangeResponse(stop.JourneyStopId, stop.Platform,
            stop.OperationalArrivalAt, stop.OperationalDepartureAt),
        RouteDiversionPublicChangeView diversion => new RouteDiversionPublicChangeResponse(diversion.DivertedStops.Select(
            stop => new DivertedStopResponse(stop.JourneyStopId, stop.StationId, stop.StopOrder, stop.ScheduledArrivalAt,
                stop.ScheduledDepartureAt, stop.OperationalArrivalAt, stop.OperationalDepartureAt, stop.Platform)).ToArray()),
        CorrectionPublicChangeView correction => new CorrectionPublicChangeResponse(ToResponse(correction.CorrectedChange)),
        _ => throw new InvalidOperationException("Unsupported safe published change.")
    };
}
public sealed record DriverJourneyContextResponse([property: Required] JourneySnapshotResponse Snapshot,
    [property: Required] IReadOnlyList<JourneyUpdateResponse> Updates);

public sealed record DriverObservationResponse([property: Required] Guid ObservationId,
    [property: Required] Guid JourneyInstanceId, [property: Required] Guid AssignmentId,
    [property: Required] string DriverSubject, [property: Required, MinLength(1), MaxLength(4000)] string Text,
    [property: Required] DateTimeOffset ReportedAtUtc, [property: Required, Range(1, int.MaxValue)] int JourneyRevision)
{
    public static DriverObservationResponse From(DriverObservationView view) => new(view.ObservationId,
        view.JourneyInstanceId, view.AssignmentId, view.DriverSubject, view.Text, view.ReportedAtUtc, view.JourneyRevision);
}
