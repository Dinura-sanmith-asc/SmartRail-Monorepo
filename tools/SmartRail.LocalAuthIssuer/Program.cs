using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

const string clientId = "smartrail-local-manual-client";
var identities = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
{
    ["Passenger"] = "smartrail-manual-passenger",
    ["Driver"] = "smartrail-manual-driver",
    ["StationStaff"] = "smartrail-manual-station-staff",
    ["Controller"] = "smartrail-manual-controller",
    ["Roleless"] = "smartrail-manual-roleless"
};

var builder = WebApplication.CreateBuilder(args);
if (!builder.Environment.IsDevelopment())
    throw new InvalidOperationException("SmartRail.LocalAuthIssuer may run only in Development.");

var issuer = builder.Configuration["LocalAuth:Issuer"];
if (!Uri.TryCreate(issuer, UriKind.Absolute, out var issuerUri) ||
    issuerUri.Scheme != Uri.UriSchemeHttp || !issuerUri.IsLoopback)
    throw new InvalidOperationException("LocalAuth:Issuer must be an HTTP loopback URL.");

using var rsa = RSA.Create(2048);
var publicKey = rsa.ExportParameters(false);
var keyId = "smartrail-local-" + Guid.NewGuid().ToString("N");
var app = builder.Build();

app.MapGet("/health", () => Results.Json(new { status = "ready", issuer }));
app.MapGet("/.well-known/openid-configuration", () => Results.Json(new
{
    issuer,
    jwks_uri = issuer + "/.well-known/jwks.json",
    authorization_endpoint = issuer + "/authorize",
    token_endpoint = issuer + "/token",
    id_token_signing_alg_values_supported = new[] { "RS256" }
}));
app.MapGet("/.well-known/jwks.json", () => Results.Json(new
{
    keys = new[]
    {
        new
        {
            kty = "RSA", use = "sig", alg = "RS256", kid = keyId,
            n = Base64Url(publicKey.Modulus!), e = Base64Url(publicKey.Exponent!)
        }
    }
}));
app.MapGet("/issue/{identity}", (string identity) => IssueToken(identity, expired: false));
app.MapGet("/issue-expired/{identity}", (string identity) => IssueToken(identity, expired: true));

IResult IssueToken(string identity, bool expired)
{
    if (!identities.TryGetValue(identity, out var subject))
        return Results.BadRequest(new { error = "unknown_test_identity", available = identities.Keys.Order().ToArray() });

    var now = DateTimeOffset.UtcNow;
    var header = JsonSerializer.SerializeToUtf8Bytes(new { alg = "RS256", typ = "JWT", kid = keyId });
    var payload = JsonSerializer.SerializeToUtf8Bytes(new
    {
        iss = issuer,
        sub = subject,
        token_use = "access",
        client_id = clientId,
        iat = now.ToUnixTimeSeconds(),
        exp = now.AddSeconds(expired ? -60 : 300).ToUnixTimeSeconds()
    });
    var signingInput = Base64Url(header) + "." + Base64Url(payload);
    var signature = rsa.SignData(Encoding.ASCII.GetBytes(signingInput), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    return Results.Json(new
    {
        token_type = "Bearer",
        expires_in = expired ? 0 : 300,
        identity,
        subject,
        access_token = signingInput + "." + Base64Url(signature)
    });
}

app.Lifetime.ApplicationStarted.Register(() =>
    app.Logger.LogInformation("Local manual OIDC issuer listening at {Issuer}; signing key is ephemeral and held in memory.", issuer));
app.Lifetime.ApplicationStopping.Register(() =>
    app.Logger.LogInformation("Local manual OIDC issuer stopping; ephemeral signing key will be discarded."));

await app.RunAsync();

static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
