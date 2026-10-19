using Microsoft.EntityFrameworkCore;
using SmartRail.Application.Identity.Authorization;
using SmartRail.Application.JourneysStatus.Boundaries;
using SmartRail.Application.JourneysStatus.Publication;
using SmartRail.Domain.Identity;
using SmartRail.Domain.JourneysStatus;
using SmartRail.Domain.JourneysStatus.Entities;
using SmartRail.Infrastructure.JourneysStatus.Persistence;
using SmartRail.Infrastructure.RailwayNetwork;
using SmartRail.IntegrationTests.JourneysStatus.Fixtures;
using SmartRail.IntegrationTests.RailwayNetwork;
using SmartRail.Infrastructure.JourneysStatus.Initialization;

namespace SmartRail.IntegrationTests.JourneysStatus.Persistence;

[Collection(JourneyPostgresCollection.Name)]
public sealed class JourneyPublicationTests(PostgreSqlTestDatabaseFixture fixture)
{
    [Fact]
    public async Task Authorized_publication_is_atomic_audited_and_stale_or_denied_writes_change_nothing()
    {
        await using var db = CreateContext();
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
        await new RailwayNetworkFixtureLoader(db).LoadAsync("Testing", Path.Combine(AppContext.BaseDirectory, "Fixtures", "RailwayNetwork"));
        var seeded = await new JourneyInitializationRepository(db).InitializeAsync(JourneyFixtureBuilder.Record(), default);
        Assert.Equal(SmartRail.Application.JourneysStatus.Persistence.JourneyInitializationStoreOutcome.Created, seeded.Outcome);
        var journeyId = seeded.Journey!.JourneyInstanceId;

        var handler = CreateHandler(db, new AllowController(), new TestObservationVerifier(DriverObservationVerification.Verified()));
        var published = await handler.PublishStatusAsync(new(journeyId, 1, "verified-controller", JourneyStatus.Delayed, null, null, null));

        Assert.Equal(JourneyPublicationOutcome.Published, published.Outcome);
        var revision = await db.JourneyRevisions.SingleAsync(x => x.JourneyInstanceId == journeyId && x.Revision == 2);
        Assert.Equal("verified-controller", revision.PublishedBySubject);
        Assert.Equal(JourneySource.ControllerManual, revision.Source);
        Assert.NotEqual(Guid.Empty, revision.UpdateId);
        Assert.Equal(DateTimeOffset.UtcNow.Offset, revision.PublishedAt.Offset);
        Assert.Equal(2, await db.JourneyRevisionStops.CountAsync(x => x.JourneyInstanceId == journeyId && x.Revision == 2));

        var stale = await handler.PublishStatusAsync(new(journeyId, 1, "verified-controller", JourneyStatus.Interrupted, null, null, null));
        Assert.Equal(JourneyPublicationOutcome.Conflict, stale.Outcome);
        Assert.Equal(2, await db.JourneyRevisions.CountAsync(x => x.JourneyInstanceId == journeyId));

        var denied = await CreateHandler(db, new DenyController(), new TestObservationVerifier(DriverObservationVerification.Verified()))
            .PublishStatusAsync(new(journeyId, 2, "verified-passenger", JourneyStatus.Interrupted, null, null, null));
        Assert.Equal(JourneyPublicationOutcome.Unauthorized, denied.Outcome);
        Assert.Equal(2, await db.Journeys.Where(x => x.JourneyInstanceId == journeyId).Select(x => x.CurrentRevision).SingleAsync());
        Assert.Equal(2, await db.JourneyRevisions.CountAsync(x => x.JourneyInstanceId == journeyId));
    }

    [Fact]
    public async Task Stop_publication_snapshots_all_stops_and_owner_unavailability_rolls_back()
    {
        await using var db = CreateContext();
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
        await new RailwayNetworkFixtureLoader(db).LoadAsync("Testing", Path.Combine(AppContext.BaseDirectory, "Fixtures", "RailwayNetwork"));
        var seeded = await new JourneyInitializationRepository(db).InitializeAsync(JourneyFixtureBuilder.Record(), default);
        var journeyId = seeded.Journey!.JourneyInstanceId;
        var stopId = await db.JourneyRevisionStops.Where(x => x.JourneyInstanceId == journeyId && x.Revision == 1)
            .OrderBy(x => x.StopOrder).Select(x => x.JourneyStopId).FirstAsync();

        var unavailable = await CreateHandler(db, new AllowController(), new TestObservationVerifier(DriverObservationVerification.Unavailable()))
            .UpdateStopAsync(new(journeyId, stopId, 1, "verified-controller", "3", null, null, Guid.NewGuid(), null));
        Assert.Equal(JourneyPublicationOutcome.Unavailable, unavailable.Outcome);
        Assert.Equal(1, await db.Journeys.Where(x => x.JourneyInstanceId == journeyId).Select(x => x.CurrentRevision).SingleAsync());
        Assert.Equal(1, await db.JourneyRevisions.CountAsync(x => x.JourneyInstanceId == journeyId));

        var changed = await CreateHandler(db, new AllowController(), new TestObservationVerifier(DriverObservationVerification.Verified()))
            .UpdateStopAsync(new(journeyId, stopId, 1, "verified-controller", "3", null,
                new DateTimeOffset(2026, 10, 17, 18, 5, 0, TimeSpan.Zero), null, null));
        Assert.Equal(JourneyPublicationOutcome.Published, changed.Outcome);
        Assert.Equal(2, await db.JourneyRevisionStops.CountAsync(x => x.JourneyInstanceId == journeyId && x.Revision == 2));
        Assert.Equal("3", await db.JourneyRevisionStops.Where(x => x.JourneyInstanceId == journeyId && x.Revision == 2 && x.JourneyStopId == stopId)
            .Select(x => x.Platform).SingleAsync());
        Assert.Equal(2, await db.JourneyRevisions.CountAsync(x => x.JourneyInstanceId == journeyId));
    }

    [Fact]
    public async Task Concurrent_publication_writers_commit_one_revision_and_report_one_conflict()
    {
        await using (var seed = CreateContext())
        {
            await seed.Database.EnsureDeletedAsync();
            await seed.Database.MigrateAsync();
            await new RailwayNetworkFixtureLoader(seed).LoadAsync("Testing", Path.Combine(AppContext.BaseDirectory, "Fixtures", "RailwayNetwork"));
            await new JourneyInitializationRepository(seed).InitializeAsync(JourneyFixtureBuilder.Record(), default);
        }
        await using var read = CreateContext();
        var journeyId = await read.Journeys.Select(x => x.JourneyInstanceId).SingleAsync();
        var first = PublishConcurrentAsync(journeyId, JourneyStatus.Delayed);
        var second = PublishConcurrentAsync(journeyId, JourneyStatus.Interrupted);
        var outcomes = await Task.WhenAll(first, second);
        Assert.Single(outcomes, x => x.Outcome == JourneyPublicationOutcome.Published);
        Assert.Single(outcomes, x => x.Outcome == JourneyPublicationOutcome.Conflict);
        Assert.Equal(2, await read.Journeys.Where(x => x.JourneyInstanceId == journeyId).Select(x => x.CurrentRevision).SingleAsync());
        Assert.Equal(2, await read.JourneyRevisions.CountAsync(x => x.JourneyInstanceId == journeyId));
        Assert.Equal(4, await read.JourneyRevisionStops.CountAsync(x => x.JourneyInstanceId == journeyId));
    }

    [Fact]
    public async Task Failed_revision_stop_constraint_rolls_back_current_revision_and_audit_event()
    {
        await using var db = CreateContext();
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
        await new RailwayNetworkFixtureLoader(db).LoadAsync("Testing", Path.Combine(AppContext.BaseDirectory, "Fixtures", "RailwayNetwork"));
        var initialized = await new JourneyInitializationRepository(db).InitializeAsync(JourneyFixtureBuilder.Record(), default);
        var journeyId = initialized.Journey!.JourneyInstanceId;
        var now = DateTimeOffset.UtcNow;
        var invalidRevision = new JourneyRevision(journeyId, 2, JourneyStatus.Delayed, now,
            "ControllerManual", "status", Guid.NewGuid(), JourneyPublicationKind.Status, 1, "verified-controller");
        var invalidStop = new JourneyRevisionStop(journeyId, 2, Guid.NewGuid(), 1, Guid.NewGuid());
        await Assert.ThrowsAsync<DbUpdateException>(() => new JourneyRepository(db).AppendRevisionAsync(journeyId, 1,
            invalidRevision, [invalidStop], default));
        db.ChangeTracker.Clear();
        Assert.Equal(1, await db.Journeys.Where(x => x.JourneyInstanceId == journeyId).Select(x => x.CurrentRevision).SingleAsync());
        Assert.Equal(1, await db.JourneyRevisions.CountAsync(x => x.JourneyInstanceId == journeyId));
        Assert.Empty(await db.JourneyRevisions.Where(x => x.JourneyInstanceId == journeyId && x.Revision == 2).ToListAsync());
    }

    private async Task<JourneyPublicationResult> PublishConcurrentAsync(Guid journeyId, JourneyStatus status)
    {
        await using var db = CreateContext();
        return await CreateHandler(db, new AllowController(), new TestObservationVerifier(DriverObservationVerification.Verified()))
            .PublishStatusAsync(new(journeyId, 1, "verified-controller", status, null, null, null));
    }

    private SmartRailDbContext CreateContext() => new(new DbContextOptionsBuilder<SmartRailDbContext>().UseNpgsql(fixture.ConnectionString).Options);

    private static JourneyPublicationHandler CreateHandler(SmartRailDbContext db, IJourneyPublicationAuthorization auth, IDriverObservationVerifier verifier)
    {
        var repository = new JourneyPublicationRepository(db, new JourneyRepository(db));
        return new(repository, auth, verifier, TimeProvider.System);
    }

    private sealed class AllowController : IJourneyPublicationAuthorization
    {
        public Task<PublicationAuthorizationDecision> AuthorizeAsync(string verifiedSubject, Guid journeyInstanceId,
            PermissionAction action, CancellationToken cancellationToken = default) =>
            Task.FromResult(new PublicationAuthorizationDecision(PublicationAuthorizationOutcome.Allowed));
    }

    private sealed class DenyController : IJourneyPublicationAuthorization
    {
        public Task<PublicationAuthorizationDecision> AuthorizeAsync(string verifiedSubject, Guid journeyInstanceId,
            PermissionAction action, CancellationToken cancellationToken = default) =>
            Task.FromResult(new PublicationAuthorizationDecision(PublicationAuthorizationOutcome.Denied));
    }

    private sealed class TestObservationVerifier(DriverObservationVerification verification) : IDriverObservationVerifier
    {
        public Task<DriverObservationVerification> VerifyAsync(string verifiedActorSubject, Guid observationId,
            Guid journeyInstanceId, CancellationToken cancellationToken = default) => Task.FromResult(verification);
    }
}
