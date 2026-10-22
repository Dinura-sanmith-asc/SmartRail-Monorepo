using System.Net;
using SmartRail.IntegrationTests.RailwayNetwork;

namespace SmartRail.IntegrationTests.Identity;

public sealed class CognitoJwtValidationTests(PostgreSqlTestDatabaseFixture database, LocalOidcFixture oidc) : IClassFixture<PostgreSqlTestDatabaseFixture>, IClassFixture<LocalOidcFixture>
{
    [Fact]
    public async Task Cognito_access_tokens_require_trusted_rsa_signature_issuer_lifetime_client_and_audience()
    {
        oidc.ExpectedAudience = "smartrail-api";
        using var factory = await IdentityApiTestHost.CreateAsync(database, oidc);
        using var client = factory.CreateClient();

        using (var valid = await Send(client, oidc.CreateToken(audience: "smartrail-api")))
            Assert.True(valid.StatusCode == HttpStatusCode.OK, $"Expected valid token to be accepted, got {(int)valid.StatusCode}: {await valid.Content.ReadAsStringAsync()}");
        Assert.Equal(HttpStatusCode.Unauthorized, (await Send(client, oidc.CreateToken(audience: "wrong-api"))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Send(client, oidc.CreateToken())).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Send(client, oidc.CreateToken(audience: "smartrail-api", issuer: "https://untrusted.invalid"))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Send(client, oidc.CreateToken(signingKey: oidc.CreateUntrustedKey(), audience: "smartrail-api"))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Send(client, oidc.CreateToken(expires: DateTime.UtcNow.AddMinutes(-2), notBefore: DateTime.UtcNow.AddMinutes(-3), audience: "smartrail-api"))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Send(client, oidc.CreateToken(notBefore: DateTime.UtcNow.AddMinutes(2), audience: "smartrail-api"))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Send(client, oidc.CreateToken(tokenUse: "id", audience: "smartrail-api"))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Send(client, oidc.CreateToken(clientId: "other-human-client", audience: "smartrail-api"))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Send(client, oidc.CreateToken(subject: null, audience: "smartrail-api"))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Send(client, oidc.CreateToken(subject: "", audience: "smartrail-api"))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Send(client, "not-a-jwt")).StatusCode);
    }

    [Fact]
    public async Task Unconfigured_audience_rejects_tokens_that_carry_aud_and_jwks_refresh_accepts_rotated_key()
    {
        oidc.ExpectedAudience = null;
        using var factory = await IdentityApiTestHost.CreateAsync(database, oidc);
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await Send(client, oidc.CreateToken())).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Send(client, oidc.CreateToken(audience: "unexpected-aud"))).StatusCode);

        oidc.RotateSigningKey();
        HttpStatusCode last = HttpStatusCode.Unauthorized;
        for (var attempt = 0; attempt < 12 && last != HttpStatusCode.OK; attempt++)
        {
            await Task.Delay(300);
            last = (await Send(client, oidc.CreateToken())).StatusCode;
        }
        Assert.Equal(HttpStatusCode.OK, last);
    }

    private static Task<HttpResponseMessage> Send(HttpClient client, string token) => client.SendAsync(IdentityTestData.Bearer(HttpMethod.Get, "/api/me", token));
}
