using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using SmartRail.Api.Authentication;
using SmartRail.Application.Identity.Authorization;
using SmartRail.Infrastructure.RailwayNetwork;
using SmartRail.Application.Identity.Persistence;
using SmartRail.IntegrationTests.RailwayNetwork;

namespace SmartRail.IntegrationTests.Identity;

public static class IdentityApiTestHost
{
    public static async Task<WebApplicationFactory<Program>> CreateAsync(PostgreSqlTestDatabaseFixture fixture, LocalOidcFixture oidc,
        bool installProbe = true, bool unavailableRoles = false, string environment = "Testing", bool seedRailway = false, bool unavailableStationScopes = false)
    {
        var logCapture = new IdentityTestLogCaptureProvider();
        var configuredIssuer = string.Equals(environment, "Testing", StringComparison.OrdinalIgnoreCase)
            ? oidc.Issuer
            : "https://127.0.0.1:1/cognito-test";
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(environment);
            builder.ConfigureLogging(logging => logging.AddProvider(logCapture).SetMinimumLevel(LogLevel.Trace));
            builder.UseSetting($"ConnectionStrings:{SmartRailDbContext.ConnectionStringName}", fixture.ConnectionString);
            builder.UseSetting($"{CognitoJwtOptions.SectionName}:Issuer", configuredIssuer);
            builder.UseSetting($"{CognitoJwtOptions.SectionName}:AllowedClientIds:0", oidc.ClientId);
            builder.UseSetting($"{CognitoJwtOptions.SectionName}:ExpectedAudience", oidc.ExpectedAudience);
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"ConnectionStrings:{SmartRailDbContext.ConnectionStringName}"] = fixture.ConnectionString,
                [$"{CognitoJwtOptions.SectionName}:Issuer"] = configuredIssuer,
                [$"{CognitoJwtOptions.SectionName}:AllowedClientIds:0"] = oidc.ClientId,
                [$"{CognitoJwtOptions.SectionName}:ExpectedAudience"] = oidc.ExpectedAudience
            }));
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<DbContextOptions<SmartRailDbContext>>();
                services.RemoveAll<SmartRailDbContext>();
                services.AddDbContext<SmartRailDbContext>(options => options.UseNpgsql(fixture.ConnectionString));
                services.RemoveAll<IDriverJourneyAssignmentEvidence>();
                services.RemoveAll<IIncidentResourceEvidence>();
                services.RemoveAll<IPassengerOrderOwnershipEvidence>();
                services.AddSingleton<IdentityTestEvidence>();
                services.AddSingleton<IDriverJourneyAssignmentEvidence>(sp => sp.GetRequiredService<IdentityTestEvidence>());
                services.AddSingleton<IIncidentResourceEvidence>(sp => sp.GetRequiredService<IdentityTestEvidence>());
                services.AddSingleton<IPassengerOrderOwnershipEvidence>(sp => sp.GetRequiredService<IdentityTestEvidence>());
                services.AddSingleton<ProbeWriteCounter>();
                services.AddSingleton(logCapture);
                if (unavailableRoles) services.AddScoped<IRoleAssignmentRepository, UnavailableRoleAssignmentRepository>();
                if (unavailableStationScopes) services.AddScoped<IStationScopeRepository, UnavailableStationScopeRepository>();
                if (installProbe) services.AddTransient<IStartupFilter, AuthorizationProbeStartupFilter>();
                services.PostConfigure<Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions>(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme,
                    options =>
                    {
                        if (options.ConfigurationManager is ConfigurationManager<OpenIdConnectConfiguration> manager) manager.RefreshInterval = TimeSpan.FromSeconds(2);
                    });
            });
        });
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartRailDbContext>();
        await db.Database.MigrateAsync();
        if (seedRailway)
            await new RailwayNetworkFixtureLoader(db).LoadAsync("Testing", Path.Combine(AppContext.BaseDirectory, "Fixtures", "RailwayNetwork"));
        return factory;
    }
}

internal sealed class UnavailableStationScopeRepository : IStationScopeRepository
{
    private static InvalidOperationException Down() => new("test station scope authority unavailable");
    public Task<IReadOnlyList<Guid>> GetActiveStationIdsAsync(string subject, CancellationToken cancellationToken = default) => Task.FromException<IReadOnlyList<Guid>>(Down());
    public Task<IReadOnlyList<SmartRail.Domain.Identity.Entities.StationScopeAssignment>> GetActiveAssignmentsAsync(string subject, CancellationToken cancellationToken = default) => Task.FromException<IReadOnlyList<SmartRail.Domain.Identity.Entities.StationScopeAssignment>>(Down());
    public Task GrantAsync(SmartRail.Domain.Identity.Entities.StationScopeAssignment assignment, SmartRail.Domain.Identity.Entities.StationScopeAssignmentAudit audit, CancellationToken cancellationToken = default) => Task.FromException(Down());
    public Task<bool> RevokeAsync(Guid assignmentId, string actor, DateTimeOffset occurredAtUtc, Guid auditId, string? evidenceReference = null, CancellationToken cancellationToken = default) => Task.FromException<bool>(Down());
}

internal sealed class UnavailableRoleAssignmentRepository : IRoleAssignmentRepository
{
    private static InvalidOperationException Down() => new("test role mapping unavailable");
    public Task<IReadOnlyList<SmartRail.Domain.Identity.RoleCode>> GetActiveRolesAsync(string subject, CancellationToken cancellationToken = default) => Task.FromException<IReadOnlyList<SmartRail.Domain.Identity.RoleCode>>(Down());
    public Task<IReadOnlyList<SmartRail.Domain.Identity.Entities.RoleAssignment>> GetActiveAssignmentsAsync(string subject, CancellationToken cancellationToken = default) => Task.FromException<IReadOnlyList<SmartRail.Domain.Identity.Entities.RoleAssignment>>(Down());
    public Task GrantAsync(SmartRail.Domain.Identity.Entities.RoleAssignment assignment, SmartRail.Domain.Identity.Entities.RoleAssignmentAudit audit, CancellationToken cancellationToken = default) => Task.FromException(Down());
    public Task<bool> RevokeAsync(Guid assignmentId, string actor, DateTimeOffset occurredAtUtc, Guid auditId, string? evidenceReference = null, CancellationToken cancellationToken = default) => Task.FromException<bool>(Down());
}
