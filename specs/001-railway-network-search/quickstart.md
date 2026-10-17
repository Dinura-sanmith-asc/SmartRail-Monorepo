# Quickstart: Railway Network & Train Search

**Purpose**: Local implementation and acceptance guide for Feature 001. These steps describe the planned environment; the current repository has no Compose file, EF context, migrations, import command or Railway Network routes, so they become runnable only after implementation.

## Prerequisites

- .NET 10 SDK.
- Docker Desktop with Compose.
- PostgreSQL 18 Docker image (pinned in the future Compose file).
- The EF Core 10/Npgsql 10 packages, EF tooling, and WebApplicationFactory test package added in the implementation phase.
- The checked-in, deterministic, versioned timetable fixtures under `tests/SmartRail.IntegrationTests/Fixtures/RailwayNetwork/`, containing stations, ordered routes/calls, schedule revisions, exact operation dates and explicit complete/incomplete manifests.

Do not put passwords or production credentials in committed configuration. Use .NET User Secrets for local API configuration and dedicated non-production database credentials in Compose. Production access and import authorization are separate and must be approved.

## Start local PostgreSQL and initialize schema

After implementation creates the Compose service and migrations:

```powershell
docker compose up -d postgres
dotnet ef database update --project src/SmartRail.Infrastructure --startup-project src/SmartRail.Api
```

The database must be healthy before applying migrations. Use a dedicated test-only PostgreSQL host separate from the developer database and RDS. The integration-test fixture creates one uniquely named database per test class, configures that class's DbContext and `WebApplicationFactory` to use it, and drops it at teardown. Test methods within a class run serially; distinct classes may run concurrently because they never share a database. Fail clearly if the test host is unavailable. Never point local import, migration or test commands at RDS.

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

