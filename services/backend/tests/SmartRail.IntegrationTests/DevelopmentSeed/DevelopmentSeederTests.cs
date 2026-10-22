using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Configuration;
using SmartRail.Application.Identity.Authorization;
using SmartRail.Application.Incidents.Categories;
using SmartRail.Application.JourneysStatus.Initialization;
using SmartRail.Infrastructure;
using SmartRail.Infrastructure.Identity.Maintenance;
using SmartRail.Infrastructure.JourneysStatus.Initialization;
using SmartRail.Application.Identity.Maintenance;
using SmartRail.Application.Incidents.Persistence;
using SmartRail.Application.JourneysStatus.Queries.GetStationDepartures;
using SmartRail.Application.JourneysStatus.Persistence;
using SmartRail.Application.Incidents.UseCases;
using SmartRail.DevSeeder;
using SmartRail.Domain.Incidents;
using SmartRail.Domain.JourneysStatus;
using SmartRail.Domain.RailwayNetwork.Entities;
using SmartRail.Infrastructure.Incidents.Persistence;
using SmartRail.Infrastructure.RailwayNetwork;
using SmartRail.IntegrationTests.RailwayNetwork;

namespace SmartRail.IntegrationTests.DevelopmentSeed;

[CollectionDefinition("Development seed PostgreSQL")]
public sealed class DevelopmentSeedCollection : ICollectionFixture<PostgreSqlTestDatabaseFixture>;

[Collection("Development seed PostgreSQL")]
public sealed class DevelopmentSeederTests(PostgreSqlTestDatabaseFixture database)
{
    private async Task ResetAsync()
    {
        // The executable cannot target this DB. Tests exercise the internal engine only.
        var target = new Npgsql.NpgsqlConnectionStringBuilder(database.ConnectionString);
        Assert.Equal(5433, target.Port);
        Assert.StartsWith("smartrail_it_", target.Database);
        Assert.Contains(target.Host, new[] { "127.0.0.1", "localhost" });
        await using var db = Context();
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
    }

    private SmartRailDbContext Context() => new(new DbContextOptionsBuilder<SmartRailDbContext>()
        .UseNpgsql(database.ConnectionString).Options);

    [Fact]
    public async Task Seed_and_rerun_preserve_existing_data_counts_relationships_and_audit()
    {
        await ResetAsync();
        await LoadRailwayReferenceFixturesAsync();
        var unrelatedId = Guid.NewGuid();
        await using (var existing = Context())
        {
            existing.Stations.Add(new Station(unrelatedId, "unrelated-local", "Keep this existing station", true));
            await existing.SaveChangesAsync();
        }
        await using var services = SeedComposition.Create(database.ConnectionString);
        var first = await new DevelopmentSeedRunner(services).RunAsync();
        await using var db = Context();
        var counts = await CountsAsync(db);
        Assert.Equal(new[] { 6, 4, 9, 8, 9, 23, 6, 18, 2, 5, 4, 4, 2, 2, 8, 14, 40, 8, 4, 10, 15, 10 }, counts);
        var second = await new DevelopmentSeedRunner(services).RunAsync();
        Assert.Equal(first.Incidents, second.Incidents);
        Assert.Equal(first.Journeys, second.Journeys);
        Assert.Equal(counts, await CountsAsync(db));
        Assert.Equal("Keep this existing station", (await db.Stations.SingleAsync(x => x.StationId == unrelatedId)).Name);
        Assert.Equal(new[] { IncidentState.Reported, IncidentState.InProgress, IncidentState.Resolved, IncidentState.Closed },
            (await db.Incidents.OrderBy(x => x.Description).ToArrayAsync()).Select(x => x.State).OrderBy(x => x));
        foreach (var incident in await db.Incidents.ToArrayAsync())
        {
            var revisions = await db.IncidentRevisions.Where(x => x.IncidentId == incident.IncidentId).OrderBy(x => x.Revision).ToArrayAsync();
            Assert.Equal(Enumerable.Range(1, incident.Revision), revisions.Select(x => x.Revision));
        }
        await using (var seedScope = services.CreateAsyncScope())
        {
            var departures = seedScope.ServiceProvider.GetRequiredService<GetStationDeparturesHandler>();
            var colombo = await departures.HandleAsync(new(SeedManifest.ColomboFortReferenceId, SeedManifest.ReferenceStationDate.ToString("yyyy-MM-dd")));
            var galle = await departures.HandleAsync(new(SeedManifest.GalleReferenceId, SeedManifest.ReferenceStationDate.ToString("yyyy-MM-dd")));
            Assert.Equal(StationDeparturesOutcome.Success, colombo.Outcome);
            Assert.Equal(StationDeparturesOutcome.Success, galle.Outcome);
            Assert.Contains(colombo.Departures!, x => x.JourneyInstanceId == SeedManifest.Id(13, 0) && x.StationName == "Colombo Fort");
            Assert.Contains(galle.Departures!, x => x.JourneyInstanceId == SeedManifest.Id(13, 1) && x.StationName == "Galle");
        }
        var journeyStates = await db.JourneyRevisions.Where(x => x.Revision == 2).Select(x => x.Status).ToArrayAsync();
        Assert.Contains(JourneyStatus.Running, journeyStates);
        Assert.Contains(JourneyStatus.Delayed, journeyStates);
        Assert.Contains(JourneyStatus.Cancelled, journeyStates);
        Assert.Contains(JourneyStatus.Completed, await db.JourneyRevisions.Where(x => x.JourneyInstanceId == SeedManifest.Id(5, 5))
            .Select(x => x.Status).ToArrayAsync());
        var overnight = SeedManifest.Id(5, 4);
        var calls = await db.JourneyRevisionStops.Where(x => x.JourneyInstanceId == overnight).OrderBy(x => x.StopOrder).ToArrayAsync();
        Assert.Equal(SeedManifest.Date.AddDays(1), DateOnly.FromDateTime(calls[1].PlannedDepartureAt!.Value.ToOffset(TimeSpan.FromHours(5.5)).DateTime));
        Assert.All(await db.IncidentAssociations.ToArrayAsync(), association => Assert.Contains(association.IncidentId, first.Incidents));
        await using var scope = services.CreateAsyncScope();
        var list = scope.ServiceProvider.GetRequiredService<ListIncidentsHandler>();
        var staff = await list.HandleAsync(new(SeedManifest.Staff, new IncidentFilters()));
        Assert.Equal(ListIncidentsOutcome.Listed, staff.Outcome);
        Assert.Equal(3, staff.Incidents.Count);
        Assert.DoesNotContain(staff.Incidents, x => x.IncidentId == first.Incidents[3]);
        var driver = await list.HandleAsync(new("smartrail-manual-driver", new IncidentFilters()));
        Assert.Equal(ListIncidentsOutcome.Forbidden, driver.Outcome);
    }

    [Fact]
    public async Task Failure_after_incident_mutation_rolls_back_whole_command_and_rerun_resumes()
    {
        await ResetAsync();
        await LoadRailwayReferenceFixturesAsync();
        await using (var failing = SeedComposition.Create(database.ConnectionString, services =>
            services.Replace(ServiceDescriptor.Scoped<IIncidentUnitOfWork, FailingIncidentUnitOfWork>())))
            await Assert.ThrowsAnyAsync<InvalidOperationException>(() => new DevelopmentSeedRunner(failing).RunAsync());
        await using var db = Context();
        Assert.Equal(0, await db.Incidents.CountAsync());
        Assert.Equal(0, await db.IncidentRevisions.CountAsync());
        Assert.Equal(0, await db.IncidentAssociations.CountAsync());
        Assert.Equal(0, await db.IncidentIdempotencyRecords.CountAsync());
        Assert.Equal(8, await db.Journeys.CountAsync()); // Earlier atomic steps remain resumable.
        await using var recovered = SeedComposition.Create(database.ConnectionString);
        await new DevelopmentSeedRunner(recovered).RunAsync();
        Assert.Equal(4, await db.Incidents.CountAsync());
        Assert.Equal(8, await db.Journeys.CountAsync());
    }

    [Fact]
    public async Task Unrelated_date_publication_is_rejected_before_any_seed_write()
    {
        await ResetAsync();
        await using var db = Context();
        db.ServiceDateManifests.Add(new(SeedManifest.Date, "unrelated", "unrelated", false, null, null, "unrelated"));
        await db.SaveChangesAsync();
        await using var services = SeedComposition.Create(database.ConnectionString);
        await Assert.ThrowsAnyAsync<InvalidOperationException>(() => new DevelopmentSeedRunner(services).RunAsync());
        Assert.Equal(0, await db.Stations.CountAsync());
        Assert.Equal("unrelated", (await db.ServiceDateManifests.SingleAsync()).SourceRevision);
    }

    [Fact]
    public async Task Rerun_does_not_restore_a_revoked_seed_role()
    {
        await ResetAsync();
        await LoadRailwayReferenceFixturesAsync();
        await using var services = SeedComposition.Create(database.ConnectionString);
        await new DevelopmentSeedRunner(services).RunAsync();
        await using (var scope = services.CreateAsyncScope())
        {
            var result = await scope.ServiceProvider.GetRequiredService<RevokeRoleAssignmentHandler>().HandleAsync(new(
                SeedManifest.Actor, SeedManifest.Id(6, 0), DateTimeOffset.UtcNow, Guid.NewGuid(), "manual-revocation-check"));
            Assert.Equal(MaintenanceCommandOutcome.Completed, result.Outcome);
        }
        await Assert.ThrowsAnyAsync<InvalidOperationException>(() => new DevelopmentSeedRunner(services).RunAsync());
        await using var db = Context();
        Assert.Equal(4, await db.RoleAssignments.CountAsync());
        Assert.NotNull((await db.RoleAssignments.SingleAsync(x => x.Id == SeedManifest.Id(6, 0))).RevokedAtUtc);
    }

    [Fact]
    public async Task Normal_application_composition_keeps_categories_unavailable_and_maintenance_denied()
    {
        var collection = new ServiceCollection();
        collection.AddLogging();
        collection.AddSmartRailInfrastructure(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:SmartRailDbContext"] = database.ConnectionString
        }).Build());
        await using var provider = collection.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        Assert.IsType<DenyAllJourneyInitializationAuthority>(scope.ServiceProvider.GetRequiredService<IJourneyInitializationAuthority>());
        Assert.IsType<DenyAllIdentityMaintenanceAuthority>(scope.ServiceProvider.GetRequiredService<IIdentityMaintenanceAuthority>());
        Assert.Equal(CategoryCheckOutcome.Unavailable, (await scope.ServiceProvider.GetRequiredService<IIncidentCategoryCatalog>()
            .CheckAsync(SeedManifest.Category)).Outcome);
    }

    [Fact]
    public async Task Production_process_refuses_seeding_even_with_all_confirmation_flags()
    {
        var start = new System.Diagnostics.ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true
        };
        foreach (var arg in new[] { "exec", "--runtimeconfig", Path.Combine(AppContext.BaseDirectory, "SmartRail.IntegrationTests.runtimeconfig.json"),
            "--depsfile", Path.Combine(AppContext.BaseDirectory, "SmartRail.IntegrationTests.deps.json"), typeof(SeedSafety).Assembly.Location,
            "--confirm-local-development", "--enable-development-sample-categories" }) start.ArgumentList.Add(arg);
        start.Environment["DOTNET_ENVIRONMENT"] = "Production";
        start.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";
        start.Environment["ConnectionStrings__SmartRailDbContext"] = "Host=database.rds.amazonaws.com;Password=do-not-print-this-test-marker";
        using var process = System.Diagnostics.Process.Start(start)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        try { await process.WaitForExitAsync(timeout.Token); }
        finally { if (!process.HasExited) process.Kill(entireProcessTree: true); }
        var output = await stdout + await stderr;
        Assert.Equal(1, process.ExitCode);
        Assert.Contains("Development seed stopped safely", output);
        Assert.DoesNotContain("do-not-print-this-test-marker", output);
        Assert.DoesNotContain("database.rds.amazonaws.com", output);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("Production", null)]
    [InlineData("Testing", null)]
    [InlineData("Development", "Production")]
    public void Executable_rejects_non_development_environments(string? dotnet, string? aspnet) =>
        Assert.ThrowsAny<InvalidOperationException>(() => SeedSafety.ValidateEnvironment(dotnet, aspnet));

    [Fact]
    public void Exact_opted_in_development_target_is_accepted_without_connecting()
    {
        SeedSafety.ValidateEnvironment("Development", "Development");
        SeedSafety.ValidateTarget("Host=127.0.0.1;Port=5432;Database=smartrail_dev;Username=smartrail_dev");
        Assert.ThrowsAny<InvalidOperationException>(() => SeedSafety.ValidateTarget(
            "Host=127.0.0.1;Port=5432;Database=smartrail_dev;Username=smartrail_dev;Search Path=other"));
    }

    [Theory]
    [InlineData("127.0.0.1", 5433, "smartrail_dev", "smartrail_dev")]
    [InlineData("localhost", 5432, "smartrail_dev", "smartrail_dev")]
    [InlineData("database.rds.amazonaws.com", 5432, "smartrail_dev", "smartrail_dev")]
    [InlineData("127.0.0.1", 5432, "production", "smartrail_dev")]
    [InlineData("127.0.0.1", 5432, "smartrail_dev", "postgres")]
    public void Executable_rejects_every_unapproved_target(string host, int port, string name, string user) =>
        Assert.ThrowsAny<InvalidOperationException>(() => SeedSafety.ValidateTarget($"Host={host};Port={port};Database={name};Username={user}"));

    private async Task LoadRailwayReferenceFixturesAsync()
    {
        await using var db = Context();
        var loader = new RailwayNetworkFixtureLoader(db);
        await loader.LoadAsync("Testing", Path.Combine(AppContext.BaseDirectory, "Fixtures", "RailwayNetwork"));
    }
    private static async Task<int[]> CountsAsync(SmartRailDbContext db) =>
    [
        await db.Stations.CountAsync(), await db.Routes.CountAsync(), await db.RouteStops.CountAsync(),
        await db.TrainServices.CountAsync(), await db.ServiceScheduleRevisions.CountAsync(), await db.ServiceCallingPoints.CountAsync(),
        await db.ServiceDateManifests.CountAsync(), await db.ServiceOperationDates.CountAsync(),
        await db.StationImportRuns.CountAsync(), await db.StationChanges.CountAsync(),
        await db.RoleAssignments.CountAsync(), await db.RoleAssignmentAudits.CountAsync(),
        await db.StationScopeAssignments.CountAsync(), await db.StationScopeAssignmentAudits.CountAsync(),
        await db.Journeys.CountAsync(), await db.JourneyRevisions.CountAsync(), await db.JourneyRevisionStops.CountAsync(), await db.JourneyInitializationAudits.CountAsync(),
        await db.Incidents.CountAsync(), await db.IncidentRevisions.CountAsync(), await db.IncidentAssociations.CountAsync(), await db.IncidentIdempotencyRecords.CountAsync()
    ];

    private sealed class FailingIncidentUnitOfWork(SmartRailDbContext db) : IIncidentUnitOfWork
    {
        public Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default) =>
            new IncidentUnitOfWork(db).ExecuteAsync<T>(async ct =>
            {
                await operation(ct);
                throw new InvalidOperationException("Injected isolated test failure after write.");
            }, cancellationToken);
    }
}
