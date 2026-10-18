using System.Net;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SmartRail.Api.Authentication;
using SmartRail.IntegrationTests.RailwayNetwork;

namespace SmartRail.IntegrationTests.Identity;

public sealed class TestAuthenticationIsolationTests(PostgreSqlTestDatabaseFixture database, LocalOidcFixture oidc) : IClassFixture<PostgreSqlTestDatabaseFixture>, IClassFixture<LocalOidcFixture>
{
    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    public async Task Test_probe_is_absent_from_normal_application_composition(string environment)
    {
        using var factory = await IdentityApiTestHost.CreateAsync(database, oidc, installProbe: false, environment: environment);
        using var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var probe = await client.GetAsync("/__test/authorization");
        Assert.Contains(probe.StatusCode, new[] { HttpStatusCode.NotFound, HttpStatusCode.Unauthorized });
        using var protectedResponse = await client.GetAsync("/api/me");
        Assert.Equal(HttpStatusCode.Unauthorized, protectedResponse.StatusCode);
        using var fixtureToken = await client.SendAsync(IdentityTestData.Bearer(HttpMethod.Get, "/api/me", oidc.CreateToken()));
        Assert.Equal(HttpStatusCode.Unauthorized, fixtureToken.StatusCode);
    }

    [Fact]
    public void Production_rejects_loopback_http_issuer_while_testing_allows_it_only_for_local_fixture()
    {
        var options = new CognitoJwtOptions { Issuer = oidc.Issuer, AllowedClientIds = [oidc.ClientId] };
        var development = new CognitoJwtOptionsValidator(new TestHostEnvironment(Environments.Development));
        var production = new CognitoJwtOptionsValidator(new TestHostEnvironment(Environments.Production));
        Assert.True(development.Validate(null, options).Succeeded);
        Assert.False(production.Validate(null, options).Succeeded);
    }

    private sealed class TestHostEnvironment(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "SmartRail.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
