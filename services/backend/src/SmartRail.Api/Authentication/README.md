# Local Cognito configuration

The API binds Cognito validation settings from `Authentication:Cognito`. Do not put real issuer values, client IDs, credentials, access tokens, or signing keys in committed files. `appsettings.Development.example.json` contains placeholders only; copy the required values into .NET User Secrets or provide them as environment variables.

From the repository root, set local Development values with .NET User Secrets:

```powershell
dotnet user-secrets set "Authentication:Cognito:Issuer" "https://<cognito-user-pool-issuer>" --project src/SmartRail.Api
dotnet user-secrets set "Authentication:Cognito:AllowedClientIds:0" "<human-app-client-id>" --project src/SmartRail.Api
```

If an API resource audience is configured, set it in User Secrets as well:

```powershell
dotnet user-secrets set "Authentication:Cognito:ExpectedAudience" "<api-resource-audience>" --project src/SmartRail.Api
```

The equivalent environment-variable names are `Authentication__Cognito__Issuer`, `Authentication__Cognito__AllowedClientIds__0`, and `Authentication__Cognito__ExpectedAudience`. .NET configuration precedence allows environment variables to override User Secrets. An omitted `ExpectedAudience` means the eventual access-token validator may accept only tokens without an `aud` claim; a configured audience must be present and match. The token-claim enforcement is implemented in a later phase.

The issuer must use HTTPS except for a loopback HTTP issuer in Development or Testing, which supports an isolated local OIDC/JWKS test fixture. Do not set a production issuer to HTTP. Startup validation fails clearly if the issuer or allowed client ID list is missing or invalid.
