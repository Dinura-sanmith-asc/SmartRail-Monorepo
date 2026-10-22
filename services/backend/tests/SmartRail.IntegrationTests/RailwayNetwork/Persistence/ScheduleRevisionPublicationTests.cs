using Microsoft.EntityFrameworkCore;
using SmartRail.Application.RailwayNetwork.Services;
using SmartRail.Domain.RailwayNetwork.Entities;
using SmartRail.Infrastructure.RailwayNetwork;
using SmartRail.Infrastructure.RailwayNetwork.Persistence;

namespace SmartRail.IntegrationTests.RailwayNetwork.Persistence;

public sealed class ScheduleRevisionPublicationTests(PostgreSqlTestDatabaseFixture fixture) : IClassFixture<PostgreSqlTestDatabaseFixture>
{
    [Fact]
    public async Task Replacement_publication_preserves_history_and_failed_publication_rolls_back_current_revision()
    {
        await using var db = CreateContext();
        await db.Database.MigrateAsync();
        await new RailwayNetworkFixtureLoader(db).LoadAsync("Testing", FixtureDirectory);
        var serviceId = Guid.Parse("30000000-0000-0000-0000-000000000001");
        var service = await db.TrainServices.SingleAsync(item => item.ServiceId == serviceId);
        var routeStops = await db.RouteStops.Where(item => item.RouteId == service.RouteId).OrderBy(item => item.StopOrder).ToArrayAsync();
        var now = DateTimeOffset.UtcNow;
        var replacement = new ServiceScheduleRevision(Guid.NewGuid(), serviceId, 3, new TimeOnly(23, 20), now.AddMinutes(-2));
        db.ServiceScheduleRevisions.Add(replacement);
        db.ServiceCallingPoints.AddRange(
            new ServiceCallingPoint(replacement.ScheduleId, routeStops[0].RouteStopId, null, 0, 1),
            new ServiceCallingPoint(replacement.ScheduleId, routeStops[^1].RouteStopId, 125, null, 2));
        await db.SaveChangesAsync();

        var repository = new SchedulePublicationRepository(db);
        var date = new DateOnly(2026, 10, 17);
        await repository.PublishTimetableAsync(new TimetablePublicationRequest(serviceId, replacement.ScheduleId, now,
            "fixture-test-publisher", [new TimetableDatePublication(
                new ServiceDateManifest(date, "replacement-source", "replacement-v1", true, now,
                    "fixture-test-publisher", "sha256:replacement"),
                [new ServiceOperationDate(replacement.ScheduleId, date)])]));
        db.ChangeTracker.Clear();
        Assert.True(await db.ServiceScheduleRevisions.AnyAsync(item => item.Revision == 1 && item.SupersededAt != null));
        Assert.True(await db.ServiceScheduleRevisions.AnyAsync(item => item.Revision == 2 && item.SupersededAt != null));
        Assert.True(await db.ServiceScheduleRevisions.AnyAsync(item => item.Revision == 3 && item.IsPublished && item.SupersededAt == null));
        Assert.True(await db.ServiceOperationDates.AnyAsync(item => item.ScheduleId == replacement.ScheduleId && item.OperationDate == date));

        var invalid = new ServiceScheduleRevision(Guid.NewGuid(), serviceId, 4, new TimeOnly(23, 20), now.AddMinutes(-1));
        db.ServiceScheduleRevisions.Add(invalid);
        db.ServiceCallingPoints.Add(new ServiceCallingPoint(invalid.ScheduleId, routeStops[0].RouteStopId, null, 0, 1));
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<SchedulePublicationRejectedException>(() => repository.PublishTimetableAsync(
            new TimetablePublicationRequest(serviceId, invalid.ScheduleId, now.AddSeconds(1), "fixture-test-publisher",
                [new TimetableDatePublication(
                    new ServiceDateManifest(date, "failed-source", "failed-v1", true, now.AddSeconds(1),
                        "fixture-test-publisher", "sha256:failed"),
                    [new ServiceOperationDate(invalid.ScheduleId, date)])])));
        db.ChangeTracker.Clear();
        Assert.True(await db.ServiceScheduleRevisions.AnyAsync(item => item.Revision == 3 && item.IsPublished && item.SupersededAt == null));
        Assert.False(await db.ServiceScheduleRevisions.AnyAsync(item => item.Revision == 4 && item.IsPublished));
        Assert.True(await db.ServiceOperationDates.AnyAsync(item => item.ScheduleId == replacement.ScheduleId && item.OperationDate == date));
    }

    private SmartRailDbContext CreateContext() => new(new DbContextOptionsBuilder<SmartRailDbContext>()
        .UseNpgsql(fixture.ConnectionString).Options);

    private static string FixtureDirectory => Path.Combine(AppContext.BaseDirectory, "Fixtures", "RailwayNetwork");
}
