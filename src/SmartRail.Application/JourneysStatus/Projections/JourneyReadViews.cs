namespace SmartRail.Application.JourneysStatus.Projections;

public sealed record JourneySnapshotView(Guid JourneyInstanceId, Guid ServiceId, Guid ScheduleId,
    int ScheduleRevision, DateOnly OperationDate, TimeOnly FirstDepartureOccurrence, int Revision,
    string Status, string Source, DateTimeOffset? PublishedAt, DateTimeOffset? LastUpdatedAt,
    IReadOnlyList<JourneyStopView> Stops, JourneyEstimateView? Eta = null);

public sealed record JourneyStopView(Guid JourneyStopId, int StopOrder, Guid StationId, string? StationName,
    DateTimeOffset? PlannedArrivalAt, DateTimeOffset? PlannedDepartureAt,
    DateTimeOffset? OperationalArrivalAt, DateTimeOffset? OperationalDepartureAt,
    string? Platform, string? TimingSource, DateTimeOffset? TimingUpdatedAt, string Ambiguity);

public sealed record JourneyEstimateView(DateTimeOffset Value, string Source, DateTimeOffset UpdatedAt, string Freshness);

public sealed record JourneyUpdateView(Guid UpdateId, Guid JourneyInstanceId, int Revision, string Kind,
    string Status, string Source, DateTimeOffset PublishedAt, Guid? CorrectionOfUpdateId,
    JourneyPublicChangeView PublicChange);

public abstract record JourneyPublicChangeView;
public sealed record StatusPublicChangeView(string StatusValue) : JourneyPublicChangeView;
public sealed record StopPublicChangeView(Guid JourneyStopId, string? Platform,
    DateTimeOffset? OperationalArrivalAt, DateTimeOffset? OperationalDepartureAt) : JourneyPublicChangeView;
public sealed record DivertedStopView(Guid JourneyStopId, Guid StationId, int StopOrder,
    DateTimeOffset? ScheduledArrivalAt, DateTimeOffset? ScheduledDepartureAt,
    DateTimeOffset? OperationalArrivalAt, DateTimeOffset? OperationalDepartureAt, string? Platform);
public sealed record RouteDiversionPublicChangeView(IReadOnlyList<DivertedStopView> DivertedStops) : JourneyPublicChangeView;
public sealed record CorrectionPublicChangeView(JourneyPublicChangeView CorrectedChange) : JourneyPublicChangeView;

public sealed class JourneyReadProjectionException(string message) : Exception(message);
