using SmartRail.Domain.JourneysStatus.Entities;

namespace SmartRail.UnitTests.JourneysStatus.Fixtures;

public static class JourneyFixtureBuilder
{
    public static Journey Journey(Guid? journeyId = null, string sourceKey = "journey-fixture-1") =>
        new(journeyId ?? Guid.NewGuid(), Guid.Parse("30000000-0000-0000-0000-000000000001"),
            Guid.Parse("31000000-0000-0000-0000-000000000002"), 2, new DateOnly(2026, 10, 17),
            new TimeOnly(23, 20), "unit-test-feed", sourceKey, DateTimeOffset.Parse("2026-10-16T12:00:00Z"),
            "verified-test-actor", "manifest-rev-1");
}
