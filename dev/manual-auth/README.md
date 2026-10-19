# SmartRail manual local authentication testing

This tooling lets you call the normal Development API with real RS256-signed access JWTs. The standalone issuer binds only to `127.0.0.1:5107`, creates a fresh RSA key in memory each time it starts, publishes that key through local OIDC/JWKS metadata, and discards the private key on exit. The API validates the JWT through its normal Cognito bearer handler. There is no authentication bypass and no new SmartRail API endpoint.

The issuer creates five fixed subjects: Passenger (`smartrail-manual-passenger`), Driver (`smartrail-manual-driver`), StationStaff (`smartrail-manual-station-staff`), Controller (`smartrail-manual-controller`) and Roleless (`smartrail-manual-roleless`). Tokens contain only Cognito-compatible `iss`, `sub`, `token_use=access`, `client_id`, `iat` and five-minute `exp` claims. They contain no role or group claim; `/api/me` reads active roles from PostgreSQL on every request.

## One-time local setup

Run PowerShell from the SmartRail repository root. Ensure the local development PostgreSQL Compose service is running and its existing development connection string is configured in the API's Development User Secrets. This role helper uses only the local Docker Compose `postgres` service and refuses non-local Docker contexts or a database/user other than `smartrail_dev`.

```powershell
.\dev\manual-auth\Configure-ManualAuth.ps1
```

This sets the API's Development User Secrets issuer and human client ID to the loopback test issuer, and removes the resource audience for these no-audience test JWTs. It does not alter the database connection secret. Environment variables override User Secrets; the script stops if current-session `Authentication__Cognito__*` overrides exist. When finished, restore your prior local issuer/client/audience values with `dotnet user-secrets set` as appropriate. Never use these values in Production or AWS.

## Start both services with Aspire

From the repository root, run:

```powershell
aspire run --apphost SmartRail.AppHost/SmartRail.AppHost.csproj
```

Aspire starts the issuer first and waits for its HTTP `/health` check before starting the API. The AppHost uses its `aspire` launch profiles on loopback ports 5108 (issuer) and 5237 (API), so existing standalone services on ports 5107 and 5236 are left untouched. It passes the same issuer endpoint to the issuer metadata/JWT generation and `Authentication:Cognito:Issuer` for API validation. PostgreSQL continues to come from the API's existing Development User Secrets; Aspire does not create or migrate a database. The API retains its configured allowed client IDs and audience policy.
## Start the issuer and API

Open separate PowerShell windows at the repository root.

Issuer window:

```powershell
dotnet run --project tools/SmartRail.LocalAuthIssuer/SmartRail.LocalAuthIssuer.csproj
```

Wait for the message that it is listening at `http://127.0.0.1:5107`. The issuer logs lifecycle and request paths only; it does not log request bodies, JWTs or authorization headers.

API window:

```powershell
dotnet run --project src/SmartRail.Api/SmartRail.Api.csproj --launch-profile http
```

The API is available at `http://localhost:5236`. It must be running with `ASPNETCORE_ENVIRONMENT=Development` so it loads the configured User Secrets and permits the loopback HTTP issuer.

## Provision or revoke local test roles

Grant and revoke role mappings with an audited, repeatable local-only script. Repeating a grant does not create a duplicate active mapping. Revocation appends audit evidence and takes effect on the next API request. These commands write only to the local Compose `smartrail_dev` database; the API still exposes no role administration route.

```powershell
.\dev\manual-auth\Set-LocalRole.ps1 -Operation Grant -Role Passenger
.\dev\manual-auth\Set-LocalRole.ps1 -Operation Grant -Role Driver
.\dev\manual-auth\Set-LocalRole.ps1 -Operation Grant -Role StationStaff
.\dev\manual-auth\Set-LocalRole.ps1 -Operation Grant -Role Controller
```

The `Roleless` issuer identity intentionally has no mapping. To revoke any provisioned mapping:

```powershell
.\dev\manual-auth\Set-LocalRole.ps1 -Operation Revoke -Role StationStaff
```

The script uses the fixed subject associated with the selected role. Its local audit actor is `local-manual-test-maintainer`; this is test evidence, not a production identity or approval workflow.

## Request a test token

PowerShell:

```powershell
$tokenResponse = Invoke-RestMethod 'http://127.0.0.1:5107/issue/StationStaff'
$token = $tokenResponse.access_token
Invoke-RestMethod 'http://localhost:5236/api/me' -Headers @{ Authorization = "Bearer $token" }
```

For another identity, replace `StationStaff` with `Passenger`, `Driver`, `Controller` or `Roleless`. The endpoint returns a new token valid for five minutes. Do not print tokens, paste them into source files, check them into Postman collections, or share screenshots containing them. In Postman, use `GET http://127.0.0.1:5107/issue/StationStaff`, copy `access_token` directly to a temporary Bearer Token authorization field, then call `GET http://localhost:5236/api/me`. Clear the response and token field when finished.

To get an already expired, correctly signed token for the 401 check, use `GET http://127.0.0.1:5107/issue-expired/StationStaff` and submit its `access_token` to `/api/me`.

## Manual checks

1. Grant `StationStaff`, request its token, and call `/api/me`: expect `200` with the subject and `roles: ["Station Staff"]`.
2. Request `/issue/Roleless` and call `/api/me`: expect `200` and `roles: []`.
3. Change a token character or use `/issue-expired/StationStaff`: expect `401`.
4. Revoke `StationStaff`, then call `/api/me` again with the same still-valid token: expect `200` and `roles: []`. The identity is still valid; only the current mapping changed.
5. Call the four public Feature 001 reads without an Authorization header: `GET /api/stations`, `GET /api/routes/{id}/stops`, `GET /api/services?origin={stationId}&destination={stationId}&date=YYYY-MM-DD`, and `GET /api/services/{id}`. They remain anonymous. Their data may be empty until local railway reference data and a schedule are loaded.

This issuer is an isolated development test tool. It is not Cognito, is not production-grade identity infrastructure, and must never be exposed beyond loopback. No AWS or RDS configuration is used.

## Run API and local issuer together with Aspire

From the repository root, run:

```powershell
aspire run --apphost .\SmartRail.AppHost\SmartRail.AppHost.csproj
```

Aspire starts the development-only issuer and then SmartRail API after the issuer health check succeeds. The issuer advertises the endpoint URL assigned by Aspire, and the API validates that exact same issuer. The Aspire launch profiles use ports 5108 (issuer) and 5237 (API), leaving the standalone profiles above available on 5107 and 5236. PostgreSQL continues to use the API's existing Development User Secrets connection string; Aspire does not create another database or apply migrations. Client ID and configured audience validation remain enabled as configured for the API.

Use the Aspire issuer and API URLs shown in the dashboard for manual requests. Request tokens from `/issue/Passenger`, `/issue/Driver`, `/issue/StationStaff`, `/issue/Controller` or `/issue/Roleless`, then call `/api/me`. The issuer is loopback-only and refuses to start outside Development. Do not expose its URLs publicly or store tokens in source control or logs.
