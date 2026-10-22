# Feature 004 Local Development Quickstart

This guide is for Feature 004 implementation and verification after task approval. No database migration is applied by this planning artifact.

## Prerequisites

- .NET 10 SDK and repository package restore available.
- Docker Desktop running with the existing SmartRail PostgreSQL services.
- Existing local OIDC/JWKS fixture for signed JWTs; no AWS Cognito setup or authentication bypass.
- The category-code catalogue approval is required before production incident creation is enabled or production category acceptance is claimed. Isolated local signed-token HTTP, integration and unit tests may use explicitly named test-only category fixtures; these values must never load through normal Development/Production configuration.

## Local services and test database safety

Start only the dedicated test PostgreSQL service for persistence tests:

```powershell
docker compose up -d postgres-test
```

The established `PostgreSqlTestDatabaseFixture` must create a unique disposable database through the admin connection on loopback port `5433`, verify the host/port/database/user before use, and drop it after the test collection. Supply `SMART_RAIL_TEST_ADMIN_CONNECTION` through a local secret or shell environment; never commit it. Do not use the development service/database on port `5432` for destructive or integration tests. Tests must not target AWS/RDS.

## Build and tests

```powershell
dotnet restore SmartRail.slnx
dotnet build SmartRail.slnx --no-restore
dotnet test tests/SmartRail.UnitTests/SmartRail.UnitTests.csproj
dotnet test tests/SmartRail.IntegrationTests/SmartRail.IntegrationTests.csproj --filter FullyQualifiedName~Incidents
```

Run the full solution tests and Feature 001/002/003 regression tests before final local verification. Failed tests must not be silently skipped. Use the existing real cryptographic test issuer and WebApplicationFactory conventions.

## Migration workflow (implementation phase only)

After the planned model/configuration tasks are implemented, create a generated additive EF Core migration in `src/SmartRail.Infrastructure/Migrations` using the repository's established `SmartRailDbContext` startup/factory workflow. Review that it adds only Feature 004 schema and does not rewrite earlier migrations. Apply it only to the disposable integration database during automated tests. Do not apply it to `smartrail_dev` as part of Feature 004 local acceptance without a separately approved execution step; never target production/RDS.

## Local HTTP verification (implementation phase only)

Run the API with the existing local issuer and PostgreSQL development connection separated from integration-test configuration. The existing three Incident routes must require a real signed JWT and current Feature 002 role mapping. Verify `401`, `403`, `404`, `409` and `503` boundaries; check response bodies/logs for tokens, passwords, reporter subjects and private audit details. Feature 001 public reads and Feature 003 public JourneySnapshot behavior must remain unchanged.

No AWS resources, deployment commands, IAM changes, Cognito setup, RDS changes, API Gateway or Lambda operations are performed by Feature 004 planning or local development.

## Feature 004 final local verification evidence

Verification was run on the isolated `codex/f004-integration` worktree on 2026-10-20. PostgreSQL integration tests used the existing healthy loopback test service on `127.0.0.1:5433`; fixtures created and removed disposable `smartrail_it_*` databases. No migration was applied to the development database on port 5432 or to AWS/RDS.

- `dotnet restore SmartRail.slnx`: passed; projects were up to date. Restore reported the existing moderate `NU1902` advisory for AppHost dependency `MessagePack 2.5.302`.
- `dotnet build SmartRail.slnx --no-restore`: passed, 0 errors and 1 existing `NU1902` warning.
- `dotnet test SmartRail.slnx --no-build`: passed, 187 unit tests and 125 integration tests (312 total), 0 failed and 0 skipped. PostgreSQL persistence, authorization, API/OpenAPI, Feature 001–003 regression, owner-verification and signed-JWT HTTP smoke coverage ran in the suite. The local HTTP smoke test exercises safe GET/POST/PATCH success responses and invalid, denied, conflict and unavailable cases with test-only categories isolated to its test host.
- OpenAPI verification confirms only `GET/POST /api/incidents` and `PATCH /api/incidents/{id}` are added for Feature 004, with the approved request/response shapes, security and error responses. Regression tests preserve the existing Feature 001–003 route inventories and public projections.
- Scope review of the integrated diff confirms the migration `20261019203203_IncidentManagement` is additive; no prior migration was changed. Production DI contains the genuine Feature 001/003 reference verifiers and keeps unavailable-owner behavior fail-closed. No new role/grant, route, AWS configuration or production authentication bypass was added. A credential-pattern review found only deliberately inert test data in a regression test (a dummy loopback connection string and hostile HTML); no usable credential, token or signing key was included.
- The original `feature-04-incident-management` branch was not changed, merged into or pushed. Work remains isolated on `codex/f004-integration`.
- `git diff --check`: passed.

### Traceability audit and acceptance decision

The requirement-to-task table above maps every FR-001–FR-016 and SC-001–SC-009 to implementation and verification evidence. Automated backend evidence covers SR-041, SR-042 lifecycle/read behavior, and the Feature 004 incident data needed by SR-044; it does not close downstream acceptance that belongs to Operations or clients. In particular, T050 verifies that a closed incident appears on an authorized subsequent `GET /api/incidents`, not on `GET /api/operations/overview`.

**Local backend recommendation: GO for review and continued local integration. Overall MVP/production acceptance: NO-GO while the following approved gates remain open:**

- The business-approved production incident category-code catalogue has not been supplied; production creation remains fail-closed. Test-only category values do not satisfy this gate.
- Operations must integrate authorized incident context into its own `GET /api/operations/overview`. SR-038, the Operations portion of SR-042, FR-006/SC-003 full acceptance, and SR-044/SC-004 Operations composition remain open until the owning implementation and client demonstrate the required behavior.
- Passenger/operations client implementation and automated evidence for their required secure rendering, loading, no-data, denial, error, retry, no-network and freshness behavior remain open.
- Idempotency-key retention/expiry remains unspecified; no expiry or purge policy was invented.

Feature 004's genuine local owner verifiers are implemented and tested. Production owner data or authority that is missing or unavailable continues to deny verification and must not persist an unverified association.
