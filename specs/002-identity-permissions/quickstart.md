# Quickstart: Identity & Permissions Design Verification

**Feature**: SmartRail 002 — Identity & Permissions
**Status**: Planning reference only. No application code or migration is included.

## Deployment boundary

Complete Feature 002 development and full SmartRail verification in the local environment before AWS deployment. Keep local settings and secrets separate from AWS environment settings. Use deterministic test issuer/JWKS keys and identity fixtures only in isolated tests; do not add a production authentication bypass. AWS resources, IAM, Cognito configuration, RDS, API Gateway, Lambda, SAM and deployment operations are performed manually by the user with step-by-step guidance after local verification. This feature implementation must not provision AWS resources or run AWS deployment commands. The approved AWS architecture remains the target.

## Prerequisites for a future implementation

- .NET 10 SDK and Docker Compose, matching the existing SmartRail development setup.
- Existing PostgreSQL 18 development and isolated test services from the repository Compose file.
- A configured Cognito User Pool issuer URL and allowlisted human application client IDs for the target environment. These are environment-specific values, not committed settings.
- Feature 001 and its four anonymous Railway Network routes remain intact.
- Production role and station-scope provisioning approvals are required before production assignments are enabled. Unit and integration tests may use deterministic isolated fixtures.

Do not put Cognito secrets, tokens, passwords, or connection-string credentials in source control. Use .NET User Secrets for local API settings or environment variables supplied by the local environment. The Cognito signing keys are discovered through the trusted issuer's HTTPS metadata/JWKS and must not be copied into the repository.

## Planned local verification sequence

From the SmartRail repository root, after the implementation tasks are approved and completed:

```powershell
docker compose up -d postgres postgres-test
dotnet restore SmartRail.slnx
dotnet build SmartRail.slnx --no-restore
dotnet test SmartRail.slnx --no-build
```

Use the actual Compose service names if they differ in the checked-out environment. PostgreSQL integration tests must use isolated test databases and must never target the development or production database.

After an approved Identity migration is created, apply it only to the intended local development database through the existing EF tooling and verify `__EFMigrationsHistory`. Do not generate or apply a migration as part of this planning step.

## Planned API checks

With the API running against local PostgreSQL and a test Cognito-compatible issuer/JWKS fixture:

- A valid access token with a current internal assignment receives `200` from `GET /api/me` with only `subject` and active `roles`.
- Internal codes are `Passenger`, `Driver`, `StationStaff`, and `Controller`; `/api/me.roles` labels are `Passenger`, `Driver`, `Station Staff`, and `Controller`. `Public` is not returned.
- A valid subject with no active assignments receives `200` and `roles: []`.
- Missing, malformed, expired, wrong-issuer, wrong-client, wrong-use, or invalid-signature tokens receive `401` on protected operations.
- With an expected resource audience configured, tokens must include a matching `aud`; missing or mismatched `aud` receives `401`. With no expected audience configured, an access token without `aud` may pass audience validation, while a token containing an unconfigured `aud` receives `401`.
- An authenticated subject lacking the matrix grant or required resource scope receives `403`.
- An unavailable role or scope authority receives `503`; it must never be converted into a successful empty mapping or allow decision.
- The four Feature 001 public reads continue to respond anonymously; malformed or expired optional bearer tokens do not change their public behavior or response projection.
- No role or station-scope management HTTP route is exposed.

Do not use real production tokens in local logs, shell history, test fixtures, or screenshots. Test signing keys and fixtures must be isolated from normal Development/Production configuration and must not add a bypass to production authentication.

## Planned automated test groups

- Domain/Application unit tests for active/revoked roles, explicit-grant union, unknown/empty roles, scope ownership outcomes, and unavailable evidence.
- Real PostgreSQL integration tests for migration, constraints, current-state reads, role and station-scope revoke/regrant, append-only audit history, actor/action/time evidence, audit/state atomicity, station foreign key, and rollback.
- API integration tests using `WebApplicationFactory` for `GET /api/me`, status mapping, safe Problem Details, logging redaction, and the four public-read regressions.
- JWT handler tests for issuer, signature/JWKS rotation, `token_use`, `client_id`, expiry, configured/missing/mismatched audience cases, and rejection of unconfigured present `aud`.

Client secure-token storage and SR-114 screen states remain obligations of the applicable Flutter/React feature specifications; this backend plan does not claim their completion.
