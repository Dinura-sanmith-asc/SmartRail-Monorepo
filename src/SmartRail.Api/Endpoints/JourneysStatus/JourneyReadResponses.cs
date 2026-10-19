using System.Text.Json.Serialization;

namespace SmartRail.Api.Endpoints.JourneysStatus;

public sealed record JourneySnapshotResponse(Guid JourneyInstanceId, Guid ServiceId, Guid ScheduleId,
    int ScheduleRevision, DateOnly OperationDate, TimeOnly FirstDepartureOccurrence, int Revision,
    string Status, IReadOnlyList<JourneyStopResponse> Stops, string Source, DateTimeOffset? PublishedAt,
    DateTimeOffset? LastUpdatedAt, JourneyEstimateResponse? Eta);

public sealed record JourneyEstimateResponse(DateTimeOffset Value, string Source,
    DateTimeOffset UpdatedAt, string Freshness);

public sealed record JourneyStopResponse(Guid JourneyStopId, int StopOrder, Guid StationId,
    string? StationName, DateTimeOffset? PlannedArrivalAt, DateTimeOffset? PlannedDepartureAt,
    DateTimeOffset? OperationalArrivalAt, DateTimeOffset? OperationalDepartureAt,
    string? Platform, string? TimingSource, DateTimeOffset? TimingUpdatedAt, string Ambiguity);

public sealed record StationDepartureResponse(Guid JourneyInstanceId, Guid ServiceId, DateOnly OperationDate,
    DateOnly StationLocalDepartureDate, int Revision, string Status, Guid StationId, string? StationName,
    int StopOrder, DateTimeOffset? ScheduledDepartureAt, DateTimeOffset? OperationalDepartureAt,
    string? Platform, string Source, DateTimeOffset UpdatedAt, string Ambiguity);

public sealed record JourneyUpdateResponse(Guid UpdateId, Guid JourneyInstanceId, int Revision,
    string Kind, string Status, string Source, DateTimeOffset PublishedAt,
    Guid? CorrectionOfUpdateId, JourneyPublicChangeResponse PublicChange);

[JsonPolymorphic(TypeDiscriminatorPropertyName = "changeType")]
[JsonDerivedType(typeof(StatusPublicChangeResponse), "status")]
[JsonDerivedType(typeof(StopPublicChangeResponse), "stopChange")]
[JsonDerivedType(typeof(RouteDiversionPublicChangeResponse), "routeDiversion")]
[JsonDerivedType(typeof(CorrectionPublicChangeResponse), "correction")]
public abstract record JourneyPublicChangeResponse;

public sealed record StatusPublicChangeResponse(string Status) : JourneyPublicChangeResponse;
public sealed record StopPublicChangeResponse(Guid JourneyStopId, string? Platform,
    DateTimeOffset? OperationalArrivalAt, DateTimeOffset? OperationalDepartureAt) : JourneyPublicChangeResponse;
public sealed record DivertedStopResponse(Guid JourneyStopId, Guid StationId, int StopOrder,
    DateTimeOffset? ScheduledArrivalAt, DateTimeOffset? ScheduledDepartureAt,
    DateTimeOffset? OperationalArrivalAt, DateTimeOffset? OperationalDepartureAt, string? Platform);
public sealed record RouteDiversionPublicChangeResponse(IReadOnlyList<DivertedStopResponse> DivertedStops)
    : JourneyPublicChangeResponse;
public sealed record CorrectionPublicChangeResponse(JourneyPublicChangeResponse CorrectedChange)
    : JourneyPublicChangeResponse;
