# Quickstart: Railway Network & Train Search

**Purpose**: Local development and acceptance guide for the implemented Feature 001 Railway Network reads. The initial PostgreSQL migration, persistence, controlled fixtures, Application use cases and four anonymous GET routes are in place. Production write authority remains gated as described below.

## Prerequisites

- .NET 10 SDK.
- Docker Desktop with Compose.
- PostgreSQL 18.6 Docker image pinned in `docker-compose.yml`.
- The pinned EF Core 10/Npgsql 10 packages, local `dotnet-ef` tool, and WebApplicationFactory test package.
- The checked-in, deterministic, versioned timetable fixtures under `tests/SmartRail.IntegrationTests/Fixtures/RailwayNetwork/`, containing stations, ordered routes/calls, schedule revisions, exact operation dates and explicit complete/incomplete manifests.

Do not put passwords or production credentials in committed configuration. Use .NET User Secrets for local API configuration and dedicated non-production database credentials in Compose. Production access and import authorization are separate and must be approved.

## Start local PostgreSQL

Start both the persistent development database and the separate test host:

```powershell
docker compose up -d postgres postgres-test
docker compose ps
```

Wait until both services report `healthy` before connecting. The development service is bound to `127.0.0.1:5432` and uses a persistent named volume. The isolated integration-test host is bound to `127.0.0.1:5433` and has separate non-production credentials. Neither service is for production.

## Configure local connection strings and migration tooling

Set the API connection string in User Secrets (the API project has a User Secrets ID) or through the `ConnectionStrings__SmartRailDbContext` environment variable. Do not put real credentials in committed configuration. `appsettings.Development.example.json` is a safe reference only; `appsettings.Development.json` is ignored by Git.

```powershell
dotnet user-secrets set --project src/SmartRail.Api "ConnectionStrings:SmartRailDbContext" "Host=127.0.0.1;Port=5432;Database=smartrail_dev;Username=smartrail_dev;Password=local-development-only"
$env:SMART_RAIL_TEST_ADMIN_CONNECTION = "Host=127.0.0.1;Port=5433;Database=postgres;Username=smartrail_test_admin;Password=local-integration-only"
dotnet tool restore
```

The test fixture requires `SMART_RAIL_TEST_ADMIN_CONNECTION`, accepts only a loopback host and the `postgres` maintenance database, creates a unique database for its test class, injects that database into `WebApplicationFactory`, and drops it at teardown. Test methods within a class run serially; distinct classes can run concurrently. It fails clearly if the dedicated host is unavailable. Never point local import, migration or test commands at RDS.

EF design-time creation uses `SmartRail.Infrastructure` for migrations and `SmartRail.Api` as the startup project. The factory reads Development User Secrets and then environment variables, with environment variables taking precedence. It rejects non-local database hosts and does not print credentials. The initial migration already exists; the following commands inspect/apply it to the local development database only.

```powershell
dotnet ef migrations list --project src/SmartRail.Infrastructure --startup-project src/SmartRail.Api --context SmartRailDbContext
dotnet ef database update --project src/SmartRail.Infrastructure --startup-project src/SmartRail.Api --context SmartRailDbContext
```

## Initialize controlled reference data

Use the deterministic station and timetable fixture documents under `tests/SmartRail.IntegrationTests/Fixtures/RailwayNetwork/`. `RailwayNetworkFixtureLoader` is an explicit Development/Test-only fixture helper and accepts only a fresh, migrated non-production database; it is not an API or a production import command. The development database used for the Phase 7 smoke checks already contained these fixtures. There is no station-write HTTP route. Production import requires the maintainer identity, separate approver authority and restricted invocation to be explicitly approved; these remain disabled/gated.

## Run API and tests

```powershell
dotnet run --project src/SmartRail.Api
# Ensure SMART_RAIL_TEST_ADMIN_CONNECTION targets the dedicated local test host on port 5433.
dotnet test SmartRail.slnx
```

The unit suite checks ordering, exact-date eligibility, `Asia/Colombo` date/offset calculations and input rules. The integration suite uses the healthy real PostgreSQL test host on port 5433 and a unique database per test class; it applies the migration, loads a versioned fixture, exercises the API through `WebApplicationFactory`, then drops the isolated database. No persistence test targets the development database. Test methods within a class are serialized; classes may run concurrently because they have separate databases. The suite verifies complete-empty versus incomplete/absent publication, overnight origin-date search, audit persistence/rollback, and deny-by-default import authorization. `StationImportTests.Authorized_import_is_idempotent_and_preserves_station_identity_and_minimal_audit` also reloads the station from PostgreSQL after an in-place verified-detail update to verify the structural value comparer persists changes. Database failures are not skipped.

## Smoke-test the approved read contracts

Start the Development API at `http://localhost:5236` and use the checked-in fixture IDs below. These are anonymous reads; no bearer token is sent.

```text
GET /api/stations
GET /api/routes/20000000-0000-0000-0000-000000000001/stops
GET /api/services?origin=10000000-0000-0000-0000-000000000001&destination=10000000-0000-0000-0000-000000000002&date=2026-10-17
GET /api/services/30000000-0000-0000-0000-000000000001
```

Expected checks:

1. Station response contains the stable station ID and name; no source identity or unverified station details are exposed.
2. Route stops use stable station IDs and ascending route order.
3. A matching service appears only when a complete published operation-date source establishes both calls in origin-before-destination order.
4. For an overnight service, use the date the service departs the selected origin, even when its schedule begins on the previous date.
5. A valid query with no eligible services returns `200` and `services: []`; missing/malformed values, identical stations and unknown station IDs return `400` Problem Details; incomplete or absent eligibility evidence returns `503` Problem Details.
6. Service details show planned identity/route/schedule only. No result implies a live journey, ticket, platform or ETA.
7. All four reads work anonymously. There is no HTTP station-management route.

## Requirement verification

Run the requirement-focused tests from `plan.md` and compare their results with the exact SR-002, SR-003, SR-004 and SR-013 acceptance checks in the approved workbook. E2E flow 1 continues into Journeys & Status only after that module provides its own approved contract and data; Feature 001 itself stops at scheduled service choices.

## Phase 7 final verification evidence

Verified 2026-10-17 against the local Development database (`smartrail_dev`, loopback port 5432) using Development User Secrets; no connection-string environment override was set. `dotnet ef migrations list --project src/SmartRail.Infrastructure --startup-project src/SmartRail.Api --context SmartRailDbContext` reported `20261017145843_InitialRailwayNetwork` applied. Both Compose PostgreSQL services (`postgres` and `postgres-test`) were healthy.

`dotnet build SmartRail.slnx` succeeded for all six projects with 0 warnings and 0 errors. `dotnet test SmartRail.slnx` passed 36 unit tests and 14 real-PostgreSQL/API integration tests (50 total; 0 failed, 0 skipped). The test host was `127.0.0.1:5433`, using unique isolated per-class databases.

TCP HTTP smoke checks against the Development API used the fixture IDs above without authentication. All passed: `GET /api/stations` 200 (two fixture stations); `GET /api/routes/{id}/stops` 200 (stops 1 then 2); valid service search 200 (service `30000000-0000-0000-0000-000000000001`, origin departure `2026-10-17T23:20:00+05:30`, destination arrival `2026-10-18T01:25:00+05:30`); service detail 200; malformed criteria and unknown station 400; complete-empty date `2026-10-18` 200 with no services; incomplete date `2026-10-19` and absent date `2026-10-20` 503; unknown route and service 404. The Development OpenAPI document was generated at `/openapi/v1.json`; automated contract verification is recorded in `plan.md`.

No production station-import/timetable authority was enabled. This evidence is local acceptance only and does not constitute a production deployment approval.

