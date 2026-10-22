namespace SmartRail.Domain.JourneysStatus.Entities;

public sealed class JourneyRevisionStop
{
    public Guid JourneyInstanceId { get; private set; }
    public int Revision { get; private set; }
    public Guid JourneyStopId { get; private set; }
    public int StopOrder { get; private set; }
    public Guid StationId { get; private set; }
    public Guid? RouteStopId { get; private set; }
    public DateTimeOffset? PlannedArrivalAt { get; private set; }
    public DateTimeOffset? PlannedDepartureAt { get; private set; }
    public DateTimeOffset? OperationalArrivalAt { get; private set; }
    public DateTimeOffset? OperationalDepartureAt { get; private set; }
    public string? Platform { get; private set; }
    public string? TimingSource { get; private set; }
    public DateTimeOffset? TimingUpdatedAt { get; private set; }
    public string AmbiguityState { get; private set; } = "none";

    private JourneyRevisionStop() { }

    public JourneyRevisionStop(Guid journeyInstanceId, int revision, Guid journeyStopId, int stopOrder,
        Guid stationId, Guid? routeStopId = null, DateTimeOffset? plannedArrivalAt = null,
        DateTimeOffset? plannedDepartureAt = null, DateTimeOffset? operationalArrivalAt = null,
        DateTimeOffset? operationalDepartureAt = null, string? platform = null,
        string? timingSource = null, DateTimeOffset? timingUpdatedAt = null, string ambiguityState = "none")
    {
        if (journeyInstanceId == Guid.Empty || journeyStopId == Guid.Empty || stationId == Guid.Empty)
            throw new ArgumentException("Journey, stop, and station identifiers must be non-empty.");
        if (revision <= 0) throw new ArgumentOutOfRangeException(nameof(revision));
        if (stopOrder <= 0) throw new ArgumentOutOfRangeException(nameof(stopOrder));
        if (routeStopId == Guid.Empty) throw new ArgumentException("Route stop ID cannot be empty when present.", nameof(routeStopId));
        if (string.IsNullOrWhiteSpace(ambiguityState) || ambiguityState is not ("none" or "ambiguous"))
            throw new ArgumentException("Ambiguity state must be none or ambiguous.", nameof(ambiguityState));
        JourneyInstanceId = journeyInstanceId;
        Revision = revision;
        JourneyStopId = journeyStopId;
        StopOrder = stopOrder;
        StationId = stationId;
        RouteStopId = routeStopId;
        PlannedArrivalAt = plannedArrivalAt;
        PlannedDepartureAt = plannedDepartureAt;
        OperationalArrivalAt = operationalArrivalAt;
        OperationalDepartureAt = operationalDepartureAt;
        Platform = platform?.Trim();
        TimingSource = timingSource?.Trim();
        TimingUpdatedAt = timingUpdatedAt;
        AmbiguityState = ambiguityState;
    }
}
