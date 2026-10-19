using SmartRail.Application.JourneysStatus.Queries;
using SmartRail.Application.JourneysStatus.Projections;
using SmartRail.Domain.JourneysStatus;
using SmartRail.Domain.JourneysStatus.Entities;
using SmartRail.UnitTests.JourneysStatus.Fixtures;

namespace SmartRail.UnitTests.JourneysStatus;

public sealed class GetJourneyUpdatesHandlerTests
{
    [Fact]
    public async Task Returns_complete_official_updates_in_ascending_revision_order_and_safe_correction_links()
    {
        var journey = JourneyFixtureBuilder.Journey();
        var at = DateTimeOffset.Parse("2026-10-16T12:00:00Z");
        var baseline = new JourneyRevision(journey.JourneyInstanceId, 1, JourneyStatus.Scheduled, at, "ScheduledBaseline");
        var updateId = Guid.NewGuid();
        var status = new JourneyRevision(journey.JourneyInstanceId, 2, JourneyStatus.Running, at.AddMinutes(1),
            "ControllerManual", "status", updateId, JourneyPublicationKind.Status, 1, "private-controller");
        var correctionId = Guid.NewGuid();
        var correction = new JourneyRevision(journey.JourneyInstanceId, 3, JourneyStatus.Delayed, at.AddMinutes(2),
            "ControllerManual", "correction", correctionId, JourneyPublicationKind.Correction, 2,
            "private-controller", correctionOfUpdateId: updateId, privateAuditEvidence: "secret-evidence");
        var stops = new[] { Stop(journey.JourneyInstanceId, 1), Stop(journey.JourneyInstanceId, 2), Stop(journey.JourneyInstanceId, 3) };
        var repository = new GetJourneyHandlerTests.FakeReadRepository
        {
            History = new(JourneyReadOutcome.Found, [correction, baseline, status],
                stops.ToDictionary(item => item.Revision, item => (IReadOnlyList<JourneyRevisionStop>)[item]))
        };

        var result = await new GetJourneyUpdatesHandler(repository).HandleAsync(new(journey.JourneyInstanceId));

        Assert.Equal(JourneyReadOutcome.Found, result.Outcome);
        Assert.NotNull(result.Value);
        Assert.Equal([2, 3], result.Value.Select(item => item.Revision));
        Assert.Equal([updateId, correctionId], result.Value.Select(item => item.UpdateId));
        Assert.Equal("status", result.Value[0].Kind);
        Assert.Equal(new StatusPublicChangeView("Running"), result.Value[0].PublicChange);
        Assert.Equal(correctionId, result.Value[1].UpdateId);
        Assert.Equal(updateId, result.Value[1].CorrectionOfUpdateId);
        Assert.Equal(new CorrectionPublicChangeView(new StatusPublicChangeView("Running")), result.Value[1].PublicChange);
        Assert.DoesNotContain(result.Value, item => item.PublicChange.ToString()!.Contains("secret", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain("private-controller", string.Join('|', result.Value.Select(item => item.ToString())));
    }

    [Fact]
    public async Task Valid_journey_with_no_official_update_returns_empty_history_but_unavailable_is_distinct()
    {
        var journey = JourneyFixtureBuilder.Journey();
        var baseline = new JourneyRevision(journey.JourneyInstanceId, 1, JourneyStatus.Scheduled,
            DateTimeOffset.Parse("2026-10-16T12:00:00Z"), "ScheduledBaseline");
        var repository = new GetJourneyHandlerTests.FakeReadRepository
        {
            History = new(JourneyReadOutcome.Found, [baseline],
                new Dictionary<int, IReadOnlyList<JourneyRevisionStop>> { [1] = [Stop(journey.JourneyInstanceId, 1)] })
        };
        var handler = new GetJourneyUpdatesHandler(repository);

        var empty = await handler.HandleAsync(new(journey.JourneyInstanceId));
        Assert.Equal(JourneyReadOutcome.Found, empty.Outcome);
        Assert.Empty(empty.Value!);

        repository = new GetJourneyHandlerTests.FakeReadRepository
        {
            History = new(JourneyReadOutcome.Unavailable)
        };
        var unavailable = await new GetJourneyUpdatesHandler(repository).HandleAsync(new(journey.JourneyInstanceId));
        Assert.Equal(JourneyReadOutcome.Unavailable, unavailable.Outcome);
    }

    private static JourneyRevisionStop Stop(Guid journeyId, int revision) =>
        new(journeyId, revision, Guid.Parse($"40000000-0000-0000-0000-{revision:000000000000}"), 1,
            Guid.Parse("10000000-0000-0000-0000-000000000001"),
            plannedDepartureAt: DateTimeOffset.Parse("2026-10-17T17:50:00Z"));
}
