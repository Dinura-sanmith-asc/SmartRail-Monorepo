# Journeys & Status Local Development Quickstart

This is a future implementation verification guide. It does not create code, migrations, data, or AWS resources. Use only the local Docker PostgreSQL services and isolated test database.

## Prerequisites

- Windows PowerShell from the repository root.
- .NET SDK compatible with the repository's `net10.0` projects.
- Docker Desktop and Docker Compose.
- Local development connection configured in API Development User Secrets; do not place credentials in this document or Git.
- `SMART_RAIL_TEST_ADMIN_CONNECTION` set in the current test shell to the dedicated Compose test host on loopback port 5433, database `postgres`, user `smartrail_test_admin`. The integration fixture rejects other targets and creates a disposable database per fixture.
- Local auth tests use the existing real-signature OIDC/JWKS fixtures under `tests/SmartRail.IntegrationTests/Identity`; do not configure a bypass or production auth exception.

## Start local PostgreSQL

```powershell
docker compose up -d postgres postgres-test
docker compose ps
```

Wait for both health checks to report healthy. The development service binds to `127.0.0.1:5432` / `smartrail_dev`; the test service binds to `127.0.0.1:5433`. Journeys integration tests must use only the latter.

Set the integration connection string without printing it. Use the repository's existing local secret/password setup; do not copy a production or RDS connection string:

```powershell
$env:SMART_RAIL_TEST_ADMIN_CONNECTION = Read-Host 'Enter local port-5433 test admin connection string'
```

Close the shell after tests to clear the variable. Do not use `Write-Host`, transcript logging, or command history to reveal credentials.

## Restore, build and test

```powershell
dotnet restore SmartRail.slnx
dotnet build SmartRail.slnx --no-restore
dotnet test SmartRail.slnx --no-build
```

The PostgreSQL fixture creates and drops an isolated database. A missing test service or connection is a failure to resolve, not a passing/skipped verification. Never point the test admin variable at `smartrail_dev` or AWS/RDS.

## Feature test evidence expected

- Domain/Application tests: approved status transition graph; terminal correction; role/scope denial; initialization validation and idempotency decisions; ambiguity is never auto-selected.
- PostgreSQL integration tests: additive migration, schedule/station FK behavior, unique source idempotency, distinct IDs sharing the composite, conflict audit, append-only revisions with one `UpdateId` per official publication, same-journey correction references, atomic stop snapshots, rollback, and one-writer/one-409 concurrent updates.
- API tests: exactly five approved endpoints; `date` filtering by station-local scheduled departure date in Asia/Colombo while preserving operationDate; overnight and diverted-stop cases with no invented times; complete ascending public update history excluding initialization; stable stop IDs and rejection of duplicate/foreign IDs; Controller authorization and current scope; observation verification 400/404/409/503 plus existing 401/403; no private evidence leakage; same revision in consistent reads; F001 and F002 regression suite.
- Significant-change evidence tests: publish cancellation and platform changes separately, then verify existing journey detail/update/station-departure reads expose the correct change type, immutable `UpdateId`, revision, safe source, and publication timestamp for each change. No notification endpoint or delivery service is introduced.
- Test fixtures may invoke the internal initializer under a deterministic test authority. No public initialization route or Development/Production authentication bypass is allowed.

## OpenAPI and contract review

After implementation, run the API in a local Development environment using the existing launch profile and local User Secrets. The built-in document is exposed at `/openapi/v1.json` in Development. Compare the generated document to [contracts/journeys-status.openapi.yaml](contracts/journeys-status.openapi.yaml): exactly the five paths, public-read security behavior, bearer protection on two PATCH operations, expected revision, response schemas, and safe errors. Do not expose the local API or test issuer outside loopback.

## Migration procedure (implementation phase only)

The migration has not been generated or applied by this plan. When its task is authorized, add the EF migration named `JourneysStatus` with EF-generated ID, inspect the generated Up/Down operations and shared snapshot, then apply it first to an isolated port-5433 test database. Do not edit prior migration files. Apply to `smartrail_dev` only under a separately approved local verification step; never run EF commands against AWS/RDS as part of Feature 003.

## Approved contract details

The six contract decisions approved on 2026-10-19 are synchronized in the spec and OpenAPI: station-local date filtering, update-history content/order, stable diverted-stop identity, closed public metadata, Driver observation failure behavior, and one `UpdateId` stored with each official publication revision. No arbitrary pagination cap is added. Production initialization authority and audit governance remain approval gates; test initialization is limited to isolated fixtures.

## Downstream acceptance evidence (open)

- **FR-013 / SR-025 / SC-007:** Feature 003's implementation and integration tests prove cancellation/platform change evidence is available through existing reads with matching `UpdateId`, revision, source, and publication timestamp. Passenger-client specifications must own alert delivery/presentation tasks and tests; full SR-025/SC-007 acceptance stays open until an in-app alert and visible status are verified against the same revision.
- **SR-044 / FR-015:** Incident Management must provide the authorized affected-journey context read and preserve association ownership. The operations client must compose that context with official Feature 003 journey data. Full SR-044 acceptance stays open until both owner-module and client evidence exist. Public `JourneySnapshot` remains free of incident records.
- Documenting these obligations is not implementation evidence and does not close either acceptance item.

## AWS boundary

Feature work remains local-first. AWS resource creation, Cognito configuration, IAM, RDS, API Gateway, Lambda, SAM, and deployment are manual user operations after complete local verification. This quickstart contains no AWS commands or AWS configuration.
