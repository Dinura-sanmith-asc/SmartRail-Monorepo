# Quickstart: Identity & Permissions Local Verification

**Feature**: SmartRail 002 — Identity & Permissions
**Status**: Local Feature 002 verification completed on 2026-10-18. AWS setup and deployment remain manual user-operated work and were not performed.

**Approval and acceptance boundary**: The specification and existing API contract were approved for client integration by the SmartRail Project Owner on 2026-10-19. Backend implementation and local verification remain supported by the previously recorded evidence; tests were not rerun for this approval update. Approval does not establish production deployment readiness or platform-wide acceptance.

## Deployment boundary

Complete Feature 002 development and full SmartRail verification in the local environment before AWS deployment. Keep local settings and secrets separate from AWS environment settings. Use deterministic test issuer/JWKS keys and identity fixtures only in isolated tests; do not add a production authentication bypass. AWS resources, IAM, Cognito configuration, RDS, API Gateway, Lambda, SAM and deployment operations are performed manually by the user with step-by-step guidance after local verification. This feature implementation must not provision AWS resources or run AWS deployment commands. The approved AWS architecture remains the target.

## Local verification environment

- .NET 10 SDK and Docker Compose, matching the existing SmartRail development setup.
- Existing PostgreSQL 18 development and isolated test services from the repository Compose file.
- Deterministic loopback OIDC/JWKS and RSA signing keys are hosted only by the integration-test fixture. Tests use actual signature validation and isolated PostgreSQL databases.
- Feature 001 and its four anonymous Railway Network routes remain intact.
- Production role and station-scope provisioning approvals are required before production assignments are enabled. Unit and integration tests may use deterministic isolated fixtures.

Do not put Cognito secrets, tokens, passwords, or connection-string credentials in source control. Use .NET User Secrets for local API settings or environment variables supplied by the local environment. The Cognito signing keys are discovered through the trusted issuer's HTTPS metadata/JWKS and must not be copied into the repository.

The development connection was resolved from .NET User Secrets. Its effective target was `localhost:5432/smartrail_dev` using the local development user; the password was not displayed or written here. The isolated integration-test service was healthy at `127.0.0.1:5433` and created per-test-class databases. No AWS/RDS connection override was configured in the process, user or machine environment or repository `.env`; the EF design-time factory rejects non-loopback hosts.

## Completed local verification

From the SmartRail repository root, to reproduce local verification after implementation completion:

```powershell
docker compose up -d postgres postgres-test
dotnet restore SmartRail.slnx
dotnet build SmartRail.slnx --no-restore
dotnet test SmartRail.slnx --no-build
```

Use the actual Compose service names if they differ in the checked-out environment. PostgreSQL integration tests must use isolated test databases and must never target the development or production database.

On 2026-10-18, `dotnet restore SmartRail.slnx` and `dotnet build SmartRail.slnx` passed; the build completed with 0 warnings and 0 errors. `dotnet test SmartRail.slnx` passed with 72 unit tests and 34 integration tests, 0 failures and 0 skipped tests. Persistence integration tests used the isolated PostgreSQL service on port 5433.

The existing migration `20261018005918_IdentityPermissions` was applied only to the verified local `smartrail_dev` database on port 5432. EF migration history contains both `20261017145843_InitialRailwayNetwork` and `20261018005918_IdentityPermissions`. The Feature 001 initial migration was not modified. No remote database was targeted.

## HTTP and contract verification

`IdentityLocalSmokeTests` passed against a `WebApplicationFactory` host using the loopback OIDC/JWKS fixture and real RSA verification, plus the isolated PostgreSQL test database. It verified:

- A valid access token with a current internal assignment receives `200` from `GET /api/me` with only `subject` and active `roles`.
- Internal codes are `Passenger`, `Driver`, `StationStaff`, and `Controller`; `/api/me.roles` labels are `Passenger`, `Driver`, `Station Staff`, and `Controller`. `Public` is not returned.
- A valid subject with no active assignments receives `200` and `roles: []`.
- Missing, malformed, expired, wrong-issuer, wrong-client, wrong-use, or invalid-signature tokens receive `401` on protected operations.
- With an expected resource audience configured, tokens must include a matching `aud`; missing or mismatched `aud` receives `401`. With no expected audience configured, an access token without `aud` may pass audience validation, while a token containing an unconfigured `aud` receives `401`.
- An authenticated subject lacking the matrix grant or required resource scope receives `403`.
- An unavailable role or scope authority receives `503`; it must never be converted into a successful empty mapping or allow decision.
- The four Feature 001 public reads continue to respond anonymously; malformed or expired optional bearer tokens do not change their public behavior or response projection.
- No role or station-scope management HTTP route is exposed.

The smoke test verified `/api/me` returns 200 with mapped `Station Staff`, returns 200 with an empty role list for an unmapped subject, rejects malformed and expired protected credentials with 401, returns 403 for an insufficient permission and 503 when station-scope evidence is unavailable. It also verified all four Feature 001 reads return their public data without a token and with malformed or expired optional tokens. Responses exposed no token or private profile claim.

Generated OpenAPI verification passed in `IdentityOpenApiTests` and the Feature 001 OpenAPI regression test. `/api/me` is the only Feature 002 route; the four approved Railway Network routes remain present, with no role/scope management endpoint.

Do not use real production tokens in local logs, shell history, test fixtures, or screenshots. Test signing keys and fixtures must be isolated from normal Development/Production configuration and must not add a bypass to production authentication.

## Requirement evidence and remaining client work

- Domain/Application unit tests for active/revoked roles, explicit-grant union, unknown/empty roles, scope ownership outcomes, and unavailable evidence.
- Real PostgreSQL integration tests for migration, constraints, current-state reads, role and station-scope revoke/regrant, append-only audit history, actor/action/time evidence, audit/state atomicity, station foreign key, and rollback.
- API integration tests using `WebApplicationFactory` for `GET /api/me`, status mapping, safe Problem Details, logging redaction, and the four public-read regressions.
- JWT handler tests for issuer, signature/JWKS rotation, `token_use`, `client_id`, expiry, configured/missing/mismatched audience cases, and rejection of unconfigured present `aud`.

| Requirement | Feature 002 local evidence | Overall MVP acceptance |
|---|---|---|
| SR-089 | Passed: the four public reads remain anonymous and return the fixture's public data with absent, malformed and expired optional tokens (`PublicRailwayReadRegressionTests`, `IdentityLocalSmokeTests`). | Backend evidence recorded; full release acceptance still includes the end-to-end flows owned by other features. |
| SR-090 | Passed: Cognito-compatible RSA/JWKS signature, issuer, lifetime, `token_use`, client and audience validation plus `/api/me` 401/200/503 behavior (`CognitoJwtValidationTests`, `CurrentIdentityEndpointTests`, `IdentityLocalSmokeTests`). | Feature 002 backend evidence recorded. |
| SR-091 | Passed for the approved Identity boundary: complete role/action matrix, current station/journey/order evidence, denied-write behavior and next-check role/scope revocation (`AuthorizationEndpointTests`, `AuthorizationMatrixTests`, `AuthorizationRevocationIntegrationTests`). Cross-module evidence providers remain owned by their respective modules. | Overall cross-module acceptance remains dependent on owner-module implementations and the release E2E workflows. |
| SR-100 | Passed: Problem Details and captured logs do not expose bearer tokens, password-like values, private claims or signing-key identifiers (`AuthenticationPrivacyTests`). | Feature 002 backend evidence recorded. |
| SR-093 | Backend evidence: protected requests reject expired credentials; API does not persist access tokens. | **Open** until Flutter Driver and applicable operational clients provide secure-storage and controlled-session-expiry implementation and automated-test evidence. |
| SR-114 | Backend evidence: API returns safe 401/403/503 outcomes and preserves authorization on retries by requiring server checks. | **Open** until applicable Flutter passenger/driver and React operational workflows provide loading, no-data, 403, 500, retry and no-network implementation and automated-test evidence. |

No owning Flutter/React feature specifications or client implementation/test evidence were available in the repository at this verification point. The downstream tracking section in `tasks.md` remains open and must be linked to those specifications when they are created. Feature 002 backend evidence does not constitute acceptance of SR-093 or the client portion of SR-114, nor does it establish acceptance of the overall MVP.

## Scope and readiness review

The six-project solution and Modular Monolith boundaries remain unchanged. Local configuration is separate from AWS settings. Production role and station-scope maintenance remains deny-by-default pending the governance approvals listed in `plan.md` (authorized operators, bootstrap/separation-of-duties, audit retention/access, environment-specific Cognito values, key rotation/revocation monitoring, and owner-module scope-provider readiness). AWS IAM, Cognito, RDS, API Gateway, Lambda, SAM and deployment resources were not configured or provisioned. These are manual user-operated steps after complete local system verification.

Final local backend verification: **PASS for Feature 002 with controlled fixtures and owner-provider limits**. This is an implementation and backend verification result only. The specification is approved for client integration; production activation and overall MVP acceptance remain gated as stated above. `git diff --check` passed on 2026-10-18; final scope review found no changes to Feature 001 endpoint declarations/contracts, no new projects, no production authentication bypass and no AWS deployment operations.
