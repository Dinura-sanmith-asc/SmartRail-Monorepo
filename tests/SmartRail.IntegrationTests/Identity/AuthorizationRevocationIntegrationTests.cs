using System.Net;
using Microsoft.Extensions.DependencyInjection;
using SmartRail.Application.Identity.Authorization;
using SmartRail.Application.Identity.Persistence;
using SmartRail.Domain.Identity;
using SmartRail.IntegrationTests.RailwayNetwork;

namespace SmartRail.IntegrationTests.Identity;

public sealed class AuthorizationRevocationIntegrationTests(PostgreSqlTestDatabaseFixture database, LocalOidcFixture oidc) : IClassFixture<PostgreSqlTestDatabaseFixture>, IClassFixture<LocalOidcFixture>
{
    [Fact]
    public async Task Role_and_station_scope_revocation_take_effect_on_the_next_api_check()
    {
        using var factory = await IdentityApiTestHost.CreateAsync(database, oidc);
        using var client = factory.CreateClient();
        var subject = "revoke-" + Guid.NewGuid().ToString("N");
        var roleId = await IdentityTestData.GrantRoleAsync(factory.Services, subject, RoleCode.StationStaff);
        var stationId = await IdentityTestData.AddStationAndGrantScopeAsync(factory.Services, subject);
        var token = oidc.CreateToken(subject);
        var url = IdentityTestData.ProbeUrl(PermissionAction.ViewOperationsDashboard, ProtectedResourceKind.Station, stationId);
        using (var allowed = await client.GetAsync(url, token)) Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IRoleAssignmentRepository>();
            Assert.True(await repository.RevokeAsync(roleId, "test-revoker", DateTimeOffset.UtcNow.AddMinutes(1), Guid.NewGuid()));
        }
        using (var roleRevoked = await client.GetAsync(url, token)) Assert.Equal(HttpStatusCode.Forbidden, roleRevoked.StatusCode);

        await IdentityTestData.GrantRoleAsync(factory.Services, subject, RoleCode.StationStaff);
        using (var allowedAgain = await client.GetAsync(url, token)) Assert.Equal(HttpStatusCode.OK, allowedAgain.StatusCode);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IStationScopeRepository>();
            var assignment = Assert.Single(await repository.GetActiveAssignmentsAsync(subject));
            Assert.True(await repository.RevokeAsync(assignment.Id, "test-revoker", DateTimeOffset.UtcNow.AddMinutes(2), Guid.NewGuid()));
        }
        using (var scopeRevoked = await client.GetAsync(url, token)) Assert.Equal(HttpStatusCode.Forbidden, scopeRevoked.StatusCode);
    }
}
