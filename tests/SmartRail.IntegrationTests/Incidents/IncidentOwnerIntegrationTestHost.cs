using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using SmartRail.Application.JourneysStatus.Initialization;
using SmartRail.Infrastructure.Incidents.Categories;
using SmartRail.Infrastructure.RailwayNetwork;
using SmartRail.IntegrationTests.Identity;
using SmartRail.IntegrationTests.JourneysStatus.Fixtures;
using SmartRail.IntegrationTests.RailwayNetwork;

namespace SmartRail.IntegrationTests.Incidents;

/// <summary>Test-only composition for genuine Feature 001/003 owner verifiers against one isolated PostgreSQL database.</summary>
internal static class IncidentOwnerIntegrationTestHost
{
    public static async Task<WebApplicationFactory<Program>> CreateAsync(PostgreSqlTestDatabaseFixture database, LocalOidcFixture oidc,
        Action<IServiceCollection>? additionalTestServices = null)
    {
        EnsureDedicatedTestDatabase(database.ConnectionString);
        await using (var cleanup = new SmartRailDbContext(new DbContextOptionsBuilder<SmartRailDbContext>()
            .UseNpgsql(database.ConnectionString).Options))
            await cleanup.Database.EnsureDeletedAsync();

        var factory = await IncidentApiTestHost.CreateAsync(database, oidc, services =>
        {
            services.Configure<IncidentCategoryCatalogOptions>(options => options.Codes = IncidentTestCategories.All.ToArray());
            services.RemoveAll<IJourneyInitializationAuthority>();
            services.AddScoped<IJourneyInitializationAuthority, TestJourneyInitializationAuthority>();
            additionalTestServices?.Invoke(services);
        });

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartRailDbContext>();
        await new RailwayNetworkFixtureLoader(db).LoadAsync("Testing",
            Path.Combine(AppContext.BaseDirectory, "Fixtures", "RailwayNetwork"));
        var service = await db.TrainServices.SingleAsync(item => item.ServiceId == JourneyFixtureBuilder.ServiceId);
        service.Publish();
        await db.SaveChangesAsync();
        return factory;
    }

    private static void EnsureDedicatedTestDatabase(string connectionString)
    {
        var builder = new Npgsql.NpgsqlConnectionStringBuilder(connectionString);
        if (builder.Port != 5433 || !(builder.Host?.Equals("localhost", StringComparison.OrdinalIgnoreCase) == true ||
            System.Net.IPAddress.TryParse(builder.Host, out var address) && System.Net.IPAddress.IsLoopback(address)) ||
            !(builder.Database?.StartsWith("smartrail_it_", StringComparison.Ordinal) ?? false))
            throw new InvalidOperationException("Owner integration tests require the isolated loopback PostgreSQL test database on port 5433.");
    }

    public static async Task<Guid> InitializeJourneyAsync(IServiceProvider services, string sourceKey)
    {
        await using var scope = services.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<InitializeJourneyHandler>();
        var result = await handler.HandleAsync(new(null, JourneyFixtureBuilder.ServiceId, JourneyFixtureBuilder.ScheduleId,
            2, JourneyFixtureBuilder.OperationDate, JourneyFixtureBuilder.FirstDeparture,
            "f004-integration-test-feed", sourceKey, "test-manifest-v1"));
        Assert.Equal(JourneyInitializationOutcome.Created, result.Outcome);
        return Assert.IsType<Guid>(result.JourneyInstanceId);
    }

    private sealed class TestJourneyInitializationAuthority : IJourneyInitializationAuthority
    {
        public Task<JourneyInitializationAuthorityResult> AuthorizeAsync(InitializeJourneyCommand command, CancellationToken cancellationToken) =>
            Task.FromResult(command.SourceSystem == "f004-integration-test-feed" && command.Provenance == "test-manifest-v1"
                ? new JourneyInitializationAuthorityResult(JourneyInitializationAuthorityStatus.Authorized,
                    "verified-test-journey-maintainer", command.SourceSystem)
                : new JourneyInitializationAuthorityResult(JourneyInitializationAuthorityStatus.Denied, null, null));
    }
}
