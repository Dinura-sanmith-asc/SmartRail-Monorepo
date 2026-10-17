# Quickstart: Railway Network & Train Search

**Purpose**: Local implementation and acceptance guide for Feature 001. Phase 1 provides the PostgreSQL Compose services, EF Core context and migration tooling. The initial migration, Railway Network data, import command and feature routes remain for later phases.

## Prerequisites

- .NET 10 SDK.
- Docker Desktop with Compose.
- PostgreSQL 18.6 Docker image pinned in `docker-compose.yml`.
- The pinned EF Core 10/Npgsql 10 packages, local `dotnet-ef` tool, and WebApplicationFactory test package added in Phase 1.
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

EF design-time creation uses the Infrastructure project as both the migration and startup project; the `IDesignTimeDbContextFactory` reads `ConnectionStrings__SmartRailDbContext`. The commands below are documented for the later Phase 3 migration task only. **Do not run them during Phase 1**; the initial schema migration must wait until all domain entities and EF mappings exist.

```powershell
$env:ConnectionStrings__SmartRailDbContext = "Host=127.0.0.1;Port=5432;Database=smartrail_dev;Username=smartrail_dev;Password=local-development-only"
dotnet ef migrations add InitialRailwayNetwork --project src/SmartRail.Infrastructure --startup-project src/SmartRail.Infrastructure
dotnet ef database update --project src/SmartRail.Infrastructure --startup-project src/SmartRail.Infrastructure
```

## Initialize controlled reference data

Use the approved station source file and deterministic timetable fixture version. The future import command validates the full station file, checks duplicate source identities and references, and commits the batch with minimal import/change evidence in one transaction or rolls it back entirely. Fixture publication includes complete dates with zero services and incomplete dates, so no-service and unavailable outcomes can be exercised separately.

```powershell
dotnet run --project src/SmartRail.Api -- --import-stations path\to\stations.csv
```

The command above is the planned invocation, not an existing executable option. No HTTP endpoint accepts station changes. A production import requires an approved maintainer and approval authority plus restricted database access; those roles remain an approval gate.

## Run API and tests

```powershell
dotnet run --project src/SmartRail.Api
dotnet test tests/SmartRail.UnitTests
dotnet test tests/SmartRail.IntegrationTests
```

The unit suite checks ordering, exact-date eligibility, `Asia/Colombo` date/offset calculations and input rules. Integration tests use a dedicated real PostgreSQL test host and a unique database per test class, apply the migration, load a versioned fixture, exercise the API through `WebApplicationFactory`, and drop each database at fixture teardown. Test methods within a class are serialized; classes may run concurrently because they have separate databases. They must prove that a complete date with no service rows returns no results, while an incomplete or absent manifest returns unavailable; that a later-origin overnight service is found using its operation date from before the requested origin date; that audit rows persist and roll back with station changes; and that unauthorized import attempts leave database state unchanged. Tests must not silently skip when the configured database is unavailable.

## Smoke-test the approved read contracts

Replace IDs and base URL with fixture values. For PowerShell, use `Invoke-RestMethod` with the full query URL.

```text
GET /api/stations
GET /api/routes/{routeId}/stops
GET /api/services?origin={originStationId}&destination={destinationStationId}&date=2026-10-16
GET /api/services/{serviceId}
```

Expected checks:

1. Station response contains stable ID, code, name and only source-verified public details.
2. Route stops use stable station IDs and ascending route order.
3. A matching service appears only when a complete published operation-date source establishes both calls in origin-before-destination order.
4. For an overnight service, use the date the service departs the selected origin, even when its schedule begins on the previous date.
5. A valid query with no eligible services returns `200` and `services: []`; missing/malformed values, identical stations and unknown station IDs return `400` Problem Details; incomplete eligibility source returns `503` Problem Details.
6. Service details show planned identity/route/schedule only. No result implies a live journey, ticket, platform or ETA.
7. All four reads work anonymously. There is no HTTP station-management route.

## Requirement verification

Run the requirement-focused tests from `plan.md` and compare their results with the exact SR-002, SR-003, SR-004 and SR-013 acceptance checks in `spec.md`. E2E flow 1 continues into Journeys & Status only after that module provides its own approved contract and data; Feature 001 itself stops at scheduled service choices.

