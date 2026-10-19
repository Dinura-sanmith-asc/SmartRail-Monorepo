# Feature 004 Local Development Quickstart

This guide is for Feature 004 implementation and verification after task approval. No database migration is applied by this planning artifact.

## Prerequisites

- .NET 10 SDK and repository package restore available.
- Docker Desktop running with the existing SmartRail PostgreSQL services.
- Existing local OIDC/JWKS fixture for signed JWTs; no AWS Cognito setup or authentication bypass.
- The category-code catalogue approval is required before tests may expect successful incident creation with production category values. Unit and contract tests can use explicitly named test-only category fixtures.

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
