namespace SmartRail.Application.JourneysStatus.Projections;

public sealed record StationDepartureProjection(Guid JourneyInstanceId, Guid ServiceId, DateOnly OperationDate,
    DateOnly StationLocalDepartureDate, int Revision, string Status, Guid StationId, string StationName,
    int StopOrder, DateTimeOffset? ScheduledDepartureAt, DateTimeOffset? OperationalDepartureAt,
    string? Platform, string Source, DateTimeOffset UpdatedAt, string Ambiguity);
