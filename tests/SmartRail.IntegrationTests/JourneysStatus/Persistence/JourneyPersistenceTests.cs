using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Update;
using Npgsql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartRail.Application.JourneysStatus.Initialization;
using SmartRail.Application.JourneysStatus.Persistence;
using SmartRail.Domain.JourneysStatus;
using SmartRail.Domain.JourneysStatus.Entities;
using SmartRail.Infrastructure.JourneysStatus.Initialization;
using SmartRail.Infrastructure.JourneysStatus.Persistence;
using SmartRail.Infrastructure.RailwayNetwork;
using SmartRail.Infrastructure;
using SmartRail.IntegrationTests.JourneysStatus.Fixtures;
using SmartRail.IntegrationTests.RailwayNetwork;

namespace SmartRail.IntegrationTests.JourneysStatus.Persistence;

[Collection(JourneyPostgresCollection.Name)]
public sealed class JourneyPersistenceTests(PostgreSqlTestDatabaseFixture fixture)
{
    private SmartRailDbContext CreateContext() => new(new DbContextOptionsBuilder<SmartRailDbContext>()
        .UseNpgsql(fixture.ConnectionString).Options);

    [Fact]
    public async Task Additive_migration_creates_journey_tables_constraints_indexes_and_restrictive_fks()
    {
        await using var db = CreateContext();
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        var tables = await ReadSetAsync(connection,
            "SELECT table_name FROM information_schema.tables WHERE table_schema='public' AND table_type='BASE TABLE'");
        Assert.Contains("journey", tables);
        Assert.Contains("journey_revision", tables);
        Assert.Contains("journey_revision_stop", tables);
        Assert.Contains("journey_initialization_audit", tables);
        var migrationIds = await ReadSetAsync(connection, "SELECT \"MigrationId\" FROM \"__EFMigrationsHistory\"");
        Assert.Contains(migrationIds, item => item.EndsWith("_InitialRailwayNetwork", StringComparison.Ordinal));
        Assert.Contains(migrationIds, item => item.EndsWith("_IdentityPermissions", StringComparison.Ordinal));
        Assert.Contains(migrationIds, item => item.EndsWith("_JourneysStatus", StringComparison.Ordinal));
        var indexes = await ReadSetAsync(connection, "SELECT indexname FROM pg_indexes WHERE schemaname='public'");
        Assert.Contains("ux_journey_source_key", indexes);
        Assert.Contains("ux_journey_revision_update_id", indexes);
        Assert.Contains("ux_journey_stop_revision_order", indexes);
        var constraints = await ReadSetAsync(connection, "SELECT conname FROM pg_constraint WHERE connamespace='public'::regnamespace");
        Assert.Contains("fk_journey_service", constraints);
        Assert.Contains("fk_journey_schedule", constraints);
        Assert.Contains("fk_journey_revision_journey", constraints);
        Assert.Contains("fk_journey_stop_station", constraints);
        var restrictive = await ReadSetAsync(connection, "SELECT conname FROM pg_constraint WHERE contype='f' AND confdeltype='r'");
        Assert.Contains("fk_journey_service", restrictive);
        Assert.Contains("fk_journey_schedule", restrictive);
        Assert.Contains("fk_journey_stop_station", restrictive);
    }

    [Fact]
    public async Task Initialization_is_atomic_idempotent_and_preserves_distinct_ids_on_same_occurrence()
    {
        await using var db = CreateContext();
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
        await LoadReferenceFixturesAsync(db);
        var link = await new JourneyScheduleLinkVerifier(db).VerifyAsync(new(JourneyFixtureBuilder.ServiceId,
            JourneyFixtureBuilder.ScheduleId, 2, JourneyFixtureBuilder.OperationDate, JourneyFixtureBuilder.FirstDeparture), default);
        Assert.Equal(ScheduleLinkVerificationStatus.Verified, link.Status);
        Assert.Equal(2, link.Link!.Stops.Count);
        var repository = new JourneyInitializationRepository(db);
        var firstId = Guid.NewGuid();
        var first = JourneyFixtureBuilder.Record(firstId);
        var created = await repository.InitializeAsync(first, default);
        Assert.Equal(JourneyInitializationStoreOutcome.Created, created.Outcome);
        Assert.Equal(1, await db.Journeys.CountAsync());
        Assert.Equal(1, await db.JourneyRevisions.CountAsync());
        Assert.Equal(2, await db.JourneyRevisionStops.CountAsync());
        Assert.Equal(1, await db.JourneyInitializationAudits.CountAsync(x => x.Decision == JourneyInitializationDecision.Accepted));

        var replay = await repository.InitializeAsync(JourneyFixtureBuilder.Record(Guid.NewGuid(), authoritativeId: false), default);
        Assert.Equal(JourneyInitializationStoreOutcome.Replayed, replay.Outcome);
        Assert.Equal(firstId, replay.Journey!.JourneyInstanceId);
        Assert.Equal(1, await db.Journeys.CountAsync());

        var second = await repository.InitializeAsync(JourneyFixtureBuilder.Record(Guid.NewGuid(), "journey-instance-fixture-2"), default);
        Assert.Equal(JourneyInitializationStoreOutcome.Created, second.Outcome);
        Assert.Equal(2, await db.Journeys.CountAsync());
        Assert.Single(await db.Journeys.Select(x => x.OperationDate).Distinct().ToListAsync());
        Assert.Single(await db.Journeys.Select(x => x.FirstDepartureOccurrence).Distinct().ToListAsync());
    }

    [Fact]
    public async Task Isolated_authorized_command_initializes_against_real_published_schedule()
    {
        await using var db = CreateContext();
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
        await LoadReferenceFixturesAsync(db);
        var handler = new InitializeJourneyHandler(new TestJourneyInitializationAuthority(),
            new JourneyInstanceIdIssuer(), new JourneyManifestValidator(), new JourneyScheduleLinkVerifier(db),
            new JourneyInitializationRepository(db), TimeProvider.System);
        var result = await handler.HandleAsync(new(null, JourneyFixtureBuilder.ServiceId, JourneyFixtureBuilder.ScheduleId,
            2, JourneyFixtureBuilder.OperationDate, JourneyFixtureBuilder.FirstDeparture,
            JourneyFixtureBuilder.Source, JourneyFixtureBuilder.SourceKey, "controlled-manifest-revision"));
        Assert.Equal(JourneyInitializationOutcome.Created, result.Outcome);
        var journey = await db.Journeys.SingleAsync();
        Assert.Equal(result.JourneyInstanceId, journey.JourneyInstanceId);
        Assert.Equal(2, await db.JourneyRevisionStops.CountAsync());
        Assert.All(await db.JourneyRevisionStops.OrderBy(x => x.StopOrder).ToListAsync(), stop => Assert.NotEqual(Guid.Empty, stop.JourneyStopId));
        Assert.Equal("verified-test-maintainer", journey.InitializedBySubject);
    }

    [Fact]
    public async Task Conflicting_source_reuse_is_audited_without_partial_journey_state()
    {
        await using var db = CreateContext();
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
        await LoadReferenceFixturesAsync(db);
        var repository = new JourneyInitializationRepository(db);
        await repository.InitializeAsync(JourneyFixtureBuilder.Record(), default);
        var conflictRecord = JourneyFixtureBuilder.Record(Guid.NewGuid(), operationDate: new DateOnly(2026, 10, 18));
        var conflict = await repository.InitializeAsync(conflictRecord, default);
        Assert.Equal(JourneyInitializationStoreOutcome.Conflict, conflict.Outcome);
        Assert.Equal(1, await db.Journeys.CountAsync());
        Assert.Equal(1, await db.JourneyRevisions.CountAsync());
        Assert.Equal(2, await db.JourneyRevisionStops.CountAsync());
        Assert.Contains(await db.JourneyInitializationAudits.ToListAsync(), audit => audit.Decision == JourneyInitializationDecision.RejectedConflict);
    }

    [Fact]
    public async Task Conflicting_authoritative_journey_id_is_rejected_and_audited_without_rebinding()
    {
        await using var db = CreateContext();
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
        await LoadReferenceFixturesAsync(db);
        var fixedId = Guid.NewGuid();
        var repository = new JourneyInitializationRepository(db);
        await repository.InitializeAsync(JourneyFixtureBuilder.Record(fixedId), default);
        var conflict = await repository.InitializeAsync(JourneyFixtureBuilder.Record(fixedId, "another-source-key"), default);
        Assert.Equal(JourneyInitializationStoreOutcome.Conflict, conflict.Outcome);
        var journey = await db.Journeys.SingleAsync();
        Assert.Equal(fixedId, journey.JourneyInstanceId);
        Assert.Equal(JourneyFixtureBuilder.SourceKey, journey.SourceInstanceKey);
        Assert.Contains(await db.JourneyInitializationAudits.ToListAsync(), audit => audit.Decision == JourneyInitializationDecision.RejectedConflict);
    }

    [Fact]
    public async Task Failed_initial_snapshot_write_rolls_back_journey_and_persists_only_conflict_audit()
    {
        await using var db = CreateContext();
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
        await LoadReferenceFixturesAsync(db);
        var journeyId = Guid.NewGuid();
        var record = JourneyFixtureBuilder.Record(stops: [
            new JourneyRevisionStop(journeyId, 1, Guid.NewGuid(), 1, Guid.Parse("10000000-0000-0000-0000-000000000001")),
            new JourneyRevisionStop(journeyId, 1, Guid.NewGuid(), 1, Guid.Parse("10000000-0000-0000-0000-000000000002"))], journeyId: journeyId);
        await Assert.ThrowsAsync<DbUpdateException>(() => new JourneyInitializationRepository(db).InitializeAsync(record, default));
        Assert.Empty(await db.Journeys.AsNoTracking().ToListAsync());
        Assert.Empty(await db.JourneyRevisions.AsNoTracking().ToListAsync());
        Assert.Empty(await db.JourneyRevisionStops.AsNoTracking().ToListAsync());
        Assert.Empty(await db.JourneyInitializationAudits.AsNoTracking().ToListAsync());
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    public async Task Initialization_authority_is_deny_all_by_default_for_every_host_environment(string environment)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:SmartRailDbContext"] = fixture.ConnectionString,
            ["DOTNET_ENVIRONMENT"] = environment
        }).Build();
        var services = new ServiceCollection();
        services.AddSmartRailInfrastructure(configuration);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var authority = scope.ServiceProvider.GetRequiredService<IJourneyInitializationAuthority>();
        var result = await authority.AuthorizeAsync(new(null, Guid.NewGuid(), Guid.NewGuid(), 1,
            new DateOnly(2026, 10, 17), new TimeOnly(23, 20), "source", "key", "manifest"), default);
        Assert.Equal(JourneyInitializationAuthorityStatus.Denied, result.Status);
        Assert.Null(result.VerifiedActorSubject);
    }

    [Fact]
    public void Manifest_validation_rejects_missing_unbounded_or_secret_like_evidence()
    {
        var validator = new JourneyManifestValidator();
        Assert.True(validator.IsValid("feed", "instance", "revision-1"));
        Assert.False(validator.IsValid("feed", " ", "revision-1"));
        Assert.False(validator.IsValid("feed", "instance", new string('x', 1001)));
        Assert.False(validator.IsValid("feed", "instance", "access-token=not-allowed"));
    }

    [Fact]
    public async Task Concurrent_revision_appends_commit_one_snapshot_and_report_one_stale_writer()
    {
        await using (var seedDb = CreateContext())
        {
            await seedDb.Database.EnsureDeletedAsync();
            await seedDb.Database.MigrateAsync();
            await LoadReferenceFixturesAsync(seedDb);
            await new JourneyInitializationRepository(seedDb).InitializeAsync(JourneyFixtureBuilder.Record(), default);
        }
        var journeyId = await GetJourneyIdAsync();
        var firstTask = AppendRevisionAsync(journeyId, JourneyStatus.Delayed);
        var secondTask = AppendRevisionAsync(journeyId, JourneyStatus.Interrupted);
        var outcomes = await Task.WhenAll(firstTask, secondTask);
        Assert.Single(outcomes, result => result.Outcome == SmartRail.Application.JourneysStatus.Persistence.JourneyRevisionAppendOutcome.Appended);
        Assert.Single(outcomes, result => result.Outcome == SmartRail.Application.JourneysStatus.Persistence.JourneyRevisionAppendOutcome.Stale);
        await using var verify = CreateContext();
        Assert.Equal(2, await verify.JourneyRevisions.CountAsync(x => x.JourneyInstanceId == journeyId));
        Assert.Equal(4, await verify.JourneyRevisionStops.CountAsync(x => x.JourneyInstanceId == journeyId));
        Assert.Equal(2, await verify.Journeys.Where(x => x.JourneyInstanceId == journeyId).Select(x => x.CurrentRevision).SingleAsync());
    }

    [Fact]
    public async Task Revision_and_initialization_audit_rows_are_append_only()
    {
        await using var db = CreateContext();
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
        await LoadReferenceFixturesAsync(db);
        await new JourneyInitializationRepository(db).InitializeAsync(JourneyFixtureBuilder.Record(), default);
        var audit = await db.JourneyInitializationAudits.SingleAsync();
        db.Entry(audit).Property(nameof(JourneyInitializationAudit.Provenance)).CurrentValue = "changed";
        db.Entry(audit).Property(nameof(JourneyInitializationAudit.Provenance)).IsModified = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();
        var revision = await db.JourneyRevisions.SingleAsync();
        db.Entry(revision).Property(nameof(JourneyRevision.PrivateAuditEvidence)).CurrentValue = "changed";
        db.Entry(revision).Property(nameof(JourneyRevision.PrivateAuditEvidence)).IsModified = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
    }

    private async Task<Guid> GetJourneyIdAsync()
    {
        await using var db = CreateContext();
        return await db.Journeys.Select(x => x.JourneyInstanceId).SingleAsync();
    }

    private async Task<SmartRail.Application.JourneysStatus.Persistence.JourneyRevisionAppendResult> AppendRevisionAsync(Guid journeyId, JourneyStatus status)
    {
        await using var db = CreateContext();
        var oldStops = await db.JourneyRevisionStops.AsNoTracking().Where(x => x.JourneyInstanceId == journeyId && x.Revision == 1)
            .OrderBy(x => x.StopOrder).ToListAsync();
        var revision = new JourneyRevision(journeyId, 2, status, DateTimeOffset.UtcNow,
            "ControllerManual", "status", Guid.NewGuid(), JourneyPublicationKind.Status, 1, "verified-controller");
        var stopSnapshots = oldStops.Select(stop => new JourneyRevisionStop(journeyId, 2, stop.JourneyStopId,
            stop.StopOrder, stop.StationId, stop.RouteStopId, stop.PlannedArrivalAt, stop.PlannedDepartureAt,
            stop.OperationalArrivalAt, stop.OperationalDepartureAt, stop.Platform, stop.TimingSource,
            stop.TimingUpdatedAt, stop.AmbiguityState)).ToArray();
        return await new JourneyRepository(db).AppendRevisionAsync(journeyId, 1, revision, stopSnapshots, default);
    }

    private async Task LoadReferenceFixturesAsync(SmartRailDbContext db)
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "Fixtures", "RailwayNetwork");
        await new RailwayNetworkFixtureLoader(db).LoadAsync("Testing", dir);
    }

    private static async Task<HashSet<string>> ReadSetAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        var result = new HashSet<string>(StringComparer.Ordinal);
        while (await reader.ReadAsync()) result.Add(reader.GetString(0));
        return result;
    }

    private sealed class TestJourneyInitializationAuthority : IJourneyInitializationAuthority
    {
        public Task<JourneyInitializationAuthorityResult> AuthorizeAsync(InitializeJourneyCommand command,
            CancellationToken cancellationToken) => Task.FromResult(new JourneyInitializationAuthorityResult(
                JourneyInitializationAuthorityStatus.Authorized, "verified-test-maintainer", command.SourceSystem));
    }
}
