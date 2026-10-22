using System.Net;
using Microsoft.Extensions.DependencyInjection;
using SmartRail.IntegrationTests.RailwayNetwork;

namespace SmartRail.IntegrationTests.Identity;

public sealed class AuthenticationPrivacyTests(PostgreSqlTestDatabaseFixture database, LocalOidcFixture oidc) : IClassFixture<PostgreSqlTestDatabaseFixture>, IClassFixture<LocalOidcFixture>
{
    [Fact]
    public async Task Errors_and_application_logs_do_not_expose_tokens_credentials_or_private_claims()
    {
        using var factory = await IdentityApiTestHost.CreateAsync(database, oidc);
        using var client = factory.CreateClient();
        var secretClaim = "private-claim-" + Guid.NewGuid().ToString("N");
        var token = oidc.CreateToken("privacy-subject", extraClaims: [new("email", secretClaim), new("password", "not-a-real-password")]);
        using (var response = await client.SendAsync(IdentityTestData.Bearer(HttpMethod.Get, "/api/me", token)))
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.DoesNotContain(token, body, StringComparison.Ordinal);
            Assert.DoesNotContain(secretClaim, body, StringComparison.Ordinal);
            Assert.DoesNotContain("password", body, StringComparison.OrdinalIgnoreCase);
        }
        var invalidToken = "Bearer " + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("raw-sensitive-error-token-" + Guid.NewGuid().ToString("N")));
        using (var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/api/me") { Headers = { { "Authorization", invalidToken } } }))
        {
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.DoesNotContain(invalidToken, body, StringComparison.Ordinal);
            Assert.DoesNotContain("connection", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Npgsql", body, StringComparison.OrdinalIgnoreCase);
        }
        var logs = string.Join("\n", factory.Services.GetRequiredService<IdentityTestLogCaptureProvider>().Messages);
        Assert.DoesNotContain(token, logs, StringComparison.Ordinal);
        Assert.DoesNotContain(invalidToken, logs, StringComparison.Ordinal);
        Assert.DoesNotContain(secretClaim, logs, StringComparison.Ordinal);
        Assert.DoesNotContain("not-a-real-password", logs, StringComparison.Ordinal);
        Assert.DoesNotContain("key-1", logs, StringComparison.Ordinal);
    }
}
