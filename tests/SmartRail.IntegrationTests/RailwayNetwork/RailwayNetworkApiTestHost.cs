using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Testing;
using SmartRail.Infrastructure.RailwayNetwork;

namespace SmartRail.IntegrationTests.RailwayNetwork;

internal static class RailwayNetworkApiTestHost
{
    public static async Task<WebApplicationFactory<Program>> CreateAsync(PostgreSqlTestDatabaseFixture fixture)
    {
        var factory = fixture.CreateWebApplicationFactory();
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartRailDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
        await new RailwayNetworkFixtureLoader(db).LoadAsync(
            "Testing", Path.Combine(AppContext.BaseDirectory, "Fixtures", "RailwayNetwork"));
        return factory;
    }
}
