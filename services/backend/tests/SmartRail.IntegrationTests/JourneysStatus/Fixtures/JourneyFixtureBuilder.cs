using SmartRail.Domain.JourneysStatus;
using SmartRail.Domain.JourneysStatus.Entities;
using SmartRail.Application.JourneysStatus.Persistence;

namespace SmartRail.IntegrationTests.JourneysStatus.Fixtures;

public static class JourneyFixtureBuilder
{
    public const string Source = "integration-test-feed";
    public const string SourceKey = "journey-instance-fixture-1";
    public static readonly Guid ServiceId = Guid.Parse("30000000-0000-0000-0000-000000000001");
    public static readonly Guid ScheduleId = Guid.Parse("31000000-0000-0000-0000-000000000002");
    public static readonly DateOnly OperationDate = new(2026, 10, 17);
    public static readonly TimeOnly FirstDeparture = new(23, 20);

    public static JourneyInitializationRecord Record(Guid? journeyId = null, string? sourceKey = null,
        IReadOnlyCollection<JourneyRevisionStop>? stops = null, bool authoritativeId = true,
        DateOnly? operationDate = null)
    {
        var id = journeyId ?? Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var date = operationDate ?? OperationDate;
        var journey = new Journey(id, ServiceId, ScheduleId, 2, date, FirstDeparture, Source,
            sourceKey ?? SourceKey, now, "verified-integration-actor", "fixture-manifest-v1");
        var baseline = new JourneyRevision(id, 1, JourneyStatus.Scheduled, now, "ScheduledBaseline");
        var initialStops = stops ?? [
            new JourneyRevisionStop(id, 1, Guid.NewGuid(), 1, Guid.Parse("10000000-0000-0000-0000-000000000001"),
                Guid.Parse("21000000-0000-0000-0000-000000000001"), plannedDepartureAt: new DateTimeOffset(2026,10,17,23,20,0,TimeSpan.FromHours(5.5)).ToUniversalTime()),
            new JourneyRevisionStop(id, 1, Guid.NewGuid(), 2, Guid.Parse("10000000-0000-0000-0000-000000000002"),
                Guid.Parse("21000000-0000-0000-0000-000000000002"), plannedArrivalAt: new DateTimeOffset(2026,10,18,1,25,0,TimeSpan.FromHours(5.5)).ToUniversalTime())
        ];
        var audit = new JourneyInitializationAudit(Guid.NewGuid(), Source, sourceKey ?? SourceKey,
            JourneyInitializationDecision.Accepted, now, "fixture-manifest-v1", id,
            "verified-integration-actor", ServiceId, ScheduleId, 2, date, FirstDeparture);
        return new(journey, baseline, initialStops, audit, authoritativeId);
    }
}
