using Microsoft.EntityFrameworkCore;
using Npgsql;
using SmartRail.Application.RailwayNetwork.Services;
using SmartRail.Domain.RailwayNetwork.Entities;
using SmartRail.Infrastructure.RailwayNetwork;
using SmartRail.Infrastructure.RailwayNetwork.Persistence;

namespace SmartRail.IntegrationTests.RailwayNetwork.Persistence;

public sealed class TrainServiceManagementRepositoryTests(PostgreSqlTestDatabaseFixture fixture) : IClassFixture<PostgreSqlTestDatabaseFixture>
{
    [Fact]
    public async Task Service_crud_enforces_unique_numbers_and_generates_identifiers()
    {
        await using var db = CreateContext();
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
        await new RailwayNetworkFixtureLoader(db).LoadAsync("Testing", FixtureDirectory);
        var repository = new TrainServiceManagementRepository(db);
        var routeId = Guid.Parse("20000000-0000-0000-0000-000000000001");
        var created = await repository.CreateAsync(new CreateTrainServiceRequest("MANUAL-01", routeId, "Manual Express", "9001"));
        Assert.NotNull(created);
        Assert.NotEqual(Guid.Empty, created.ServiceId);
        Assert.Equal("9001", created.PublicServiceNumber);

        await Assert.ThrowsAsync<DbUpdateException>(() => repository.CreateAsync(
            new CreateTrainServiceRequest("MANUAL-02", routeId, "Duplicate", "9001")));
        db.ChangeTracker.Clear();
        var updated = await repository.UpdateAsync(created.ServiceId,
            new UpdateTrainServiceRequest("MANUAL-01A", routeId, "Manual Express Updated", "9002"));
        Assert.Equal("9002", updated!.PublicServiceNumber);
        Assert.True(await repository.DeleteAsync(created.ServiceId));
    }

    [Fact]
    public async Task Schedule_revision_publication_and_operation_dates_preserve_published_history()
    {
        await using var db = CreateContext();
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
        await new RailwayNetworkFixtureLoader(db).LoadAsync("Testing", FixtureDirectory);
        var repository = new TrainServiceManagementRepository(db);
        var serviceId = Guid.Parse("30000000-0000-0000-0000-000000000001");
        var routeId = Guid.Parse("20000000-0000-0000-0000-000000000001");
        var stops = await db.RouteStops.Where(item => item.RouteId == routeId).OrderBy(item => item.StopOrder).ToArrayAsync();
        var created = await repository.CreateScheduleAsync(serviceId, new ScheduleRevisionInput(new TimeOnly(8, 15),
        [
            new CallingPointInput(stops[0].RouteStopId, null, 0),
            new CallingPointInput(stops[^1].RouteStopId, 125, null)
        ]));
        Assert.Equal(3, created.Revision);
        var edited = await repository.UpdateScheduleAsync(serviceId, created.ScheduleId, new ScheduleRevisionInput(new TimeOnly(8, 20),
        [
            new CallingPointInput(stops[0].RouteStopId, null, 0),
            new CallingPointInput(stops[^1].RouteStopId, 130, null)
        ]));
        Assert.Equal(new TimeOnly(8, 20), edited!.FirstDepartureLocalTime);
        Assert.Equal(130, edited.CallingPoints[^1].ArrivalOffsetMinutes);

        await repository.PublishScheduleAsync(serviceId, created.ScheduleId, DateTimeOffset.UtcNow);
        var operationDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
        await repository.SetManifestAsync(new ServiceDateManifest(operationDate, "test-source", "test-v1", true,
            DateTimeOffset.UtcNow, "test-operator", "sha256:test"), [created.ScheduleId]);
        var dates = await repository.SetOperationDatesAsync(serviceId, created.ScheduleId, [operationDate]);
        Assert.True(dates!.Single().IsManifestComplete);
        db.ChangeTracker.Clear();
        var revisions = await db.ServiceScheduleRevisions.Where(item => item.ServiceId == serviceId).OrderBy(item => item.Revision).ToArrayAsync();
        Assert.Equal(3, revisions.Length);
        Assert.All(revisions.Take(2), item => Assert.NotNull(item.SupersededAt));
        Assert.True(revisions[2].IsCurrentPublished);
        Assert.Contains(await db.ServiceOperationDates.ToListAsync(), item => item.ScheduleId == created.ScheduleId && item.OperationDate == operationDate);
    }

    private SmartRailDbContext CreateContext() => new(new DbContextOptionsBuilder<SmartRailDbContext>().UseNpgsql(fixture.ConnectionString).Options);
    private static string FixtureDirectory => Path.Combine(AppContext.BaseDirectory, "Fixtures", "RailwayNetwork");
}
