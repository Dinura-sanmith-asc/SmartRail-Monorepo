using System.Security.Cryptography;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace SmartRail.IntegrationTests.Identity;

/// <summary>Loopback-only OIDC metadata/JWKS server backed by real RSA keys.</summary>
public sealed class LocalOidcFixture : IAsyncLifetime
{
    private WebApplication? _application;
    private readonly object _gate = new();
    private RsaSecurityKey _key = NewKey("key-1");
    private readonly Dictionary<string, RsaSecurityKey> _knownKeys = new(StringComparer.Ordinal);

    public LocalOidcFixture() => _knownKeys.Add(_key.KeyId!, _key);
    public string Issuer { get; private set; } = string.Empty;
    public string ClientId { get; } = "smartrail-test-human-client";
    public string? ExpectedAudience { get; set; }

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        _application = builder.Build();
        _application.MapGet("/.well-known/openid-configuration", () => Results.Json(new
        {
            issuer = Issuer,
            jwks_uri = Issuer + "/.well-known/jwks.json",
            authorization_endpoint = Issuer + "/authorize",
            token_endpoint = Issuer + "/token",
            id_token_signing_alg_values_supported = new[] { "RS256" }
        }));
        _application.MapGet("/.well-known/jwks.json", () => Results.Json(GetJwks()));
        await _application.StartAsync();
        var server = _application.Services.GetRequiredService<Microsoft.AspNetCore.Hosting.Server.IServer>();
        Issuer = server.Features.Get<IServerAddressesFeature>()!.Addresses.Single();
    }

    public string CreateToken(string? subject = "test-subject", string tokenUse = "access", string? clientId = null,
        string? audience = null, DateTime? notBefore = null, DateTime? expires = null, SecurityKey? signingKey = null,
        string? issuer = null, IEnumerable<System.Security.Claims.Claim>? extraClaims = null)
    {
        lock (_gate)
        {
            var key = signingKey ?? _key;
            var claims = new List<System.Security.Claims.Claim>();
            if (subject is not null) claims.Add(new("sub", subject));
            claims.Add(new("token_use", tokenUse));
            claims.Add(new("client_id", clientId ?? ClientId));
            if (extraClaims is not null) claims.AddRange(extraClaims);
            var token = new JwtSecurityToken(issuer ?? Issuer, audience, claims,
                notBefore ?? DateTime.UtcNow.AddSeconds(-5), expires ?? DateTime.UtcNow.AddMinutes(5),
                new SigningCredentials(key, SecurityAlgorithms.RsaSha256));
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    public SecurityKey CreateUntrustedKey() => NewKey("untrusted-" + Guid.NewGuid().ToString("N"));

    public void RotateSigningKey()
    {
        lock (_gate)
        {
            _key = NewKey("key-" + Guid.NewGuid().ToString("N"));
            _knownKeys.Add(_key.KeyId!, _key);
        }
    }

    private object GetJwks()
    {
        lock (_gate)
        {
            return new { keys = _knownKeys.Values.Select(k => new
            {
                kty = "RSA", use = "sig", alg = "RS256", kid = k.KeyId,
                n = Base64UrlEncoder.Encode(k.Rsa!.ExportParameters(false).Modulus!),
                e = Base64UrlEncoder.Encode(k.Rsa.ExportParameters(false).Exponent!)
            }).ToArray() };
        }
    }

    private static RsaSecurityKey NewKey(string id)
    {
        var rsa = RSA.Create(2048);
        return new RsaSecurityKey(rsa) { KeyId = id };
    }

    public async Task DisposeAsync()
    {
        if (_application is not null)
        {
            await _application.StopAsync();
            await _application.DisposeAsync();
        }
        lock (_gate)
        {
            foreach (var key in _knownKeys.Values.Distinct()) key.Rsa?.Dispose();
            _knownKeys.Clear();
        }
    }
}
