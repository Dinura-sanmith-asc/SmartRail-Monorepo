using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using SmartRail.Api.Authentication;
using SmartRail.Infrastructure.RailwayNetwork;
using SmartRail.IntegrationTests.Identity;
using SmartRail.IntegrationTests.RailwayNetwork;

namespace SmartRail.IntegrationTests.Incidents;

/// <summary>Signed-token API host backed by one isolated port-5433 database.</summary>
public static class IncidentApiTestHost
{
    public static async Task<WebApplicationFactory<Program>> CreateAsync(PostgreSqlTestDatabaseFixture database,
        LocalOidcFixture oidc, Action<IServiceCollection>? configureTestServices = null)
    {
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting($"ConnectionStrings:{SmartRailDbContext.ConnectionStringName}", database.ConnectionString);
            builder.UseSetting($"{CognitoJwtOptions.SectionName}:Issuer", oidc.Issuer);
            builder.UseSetting($"{CognitoJwtOptions.SectionName}:AllowedClientIds:0", oidc.ClientId);
            builder.UseSetting($"{CognitoJwtOptions.SectionName}:ExpectedAudience", oidc.ExpectedAudience);
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"ConnectionStrings:{SmartRailDbContext.ConnectionStringName}"] = database.ConnectionString,
                [$"{CognitoJwtOptions.SectionName}:Issuer"] = oidc.Issuer,
                [$"{CognitoJwtOptions.SectionName}:AllowedClientIds:0"] = oidc.ClientId,
                [$"{CognitoJwtOptions.SectionName}:ExpectedAudience"] = oidc.ExpectedAudience
            }));
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<DbContextOptions<SmartRailDbContext>>();
                services.RemoveAll<SmartRailDbContext>();
                services.AddDbContext<SmartRailDbContext>(options => options.UseNpgsql(database.ConnectionString));
                configureTestServices?.Invoke(services);
            });
        });

        await using var scope = factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<SmartRailDbContext>().Database.MigrateAsync();
        return factory;
    }
}
