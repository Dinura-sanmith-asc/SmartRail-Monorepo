# Implementation Plan: Railway Network & Train Search

**Branch**: `001-railway-network-search` | **Date**: 2026-10-16 | **Spec**: [spec.md](spec.md)

**Input**: Clarified Feature 001 specification; SmartRail constitution, approved requirements and API/RBAC baseline, handoff, ERD, and existing solution.

## Summary

Implement Railway Network as a feature inside the existing ASP.NET Core .NET 10 modular monolith. Public Minimal API endpoints read published stations, ordered routes, scheduled services and date-eligible service choices. Application use cases own the search and station-import workflows; Domain owns stable identity and eligibility rules; Infrastructure uses EF Core and PostgreSQL. Station maintenance uses a controlled, authorized, repeatable import. No public station write endpoint or UI is added.

The minimum search model consists of a reusable service schedule, its ordered service calling points and exact published operating-date records. The service date is interpreted at the selected origin: the date a service leaves that station. A service that departs an earlier station before midnight may therefore match a later-origin search on the following date. This remains planned schedule data; Journeys & Status owns dated operational journeys and live state.

## Technical Context

**Language/Version**: C# / ASP.NET Core .NET 10 (`net10.0`; current API OpenAPI package is 10.0.12)

**Primary Dependencies**: ASP.NET Core Minimal APIs and built-in OpenAPI are present. Add a compatible EF Core 10 / Npgsql 10 set, EF design tooling, and ASP.NET Core integration-test hosting during implementation. None is currently referenced. Avoid adding a mediator, generic repository framework or extra backend project.

**Storage**: PostgreSQL 18 for local Docker and Amazon RDS for PostgreSQL 18 for demo, with exact engine patch selected for the target AWS region. Use EF Core migrations. RDS PostgreSQL 18 is listed in current AWS release information; verify the available patch and region at deployment.

**Testing**: Existing xUnit 2.9.3 and Microsoft.NET.Test.Sdk 17.14.1 projects. Add WebApplicationFactory support and a disposable real-PostgreSQL test fixture; keep unit and integration tests in the existing projects.

**Target Platform**: One ASP.NET Core .NET 10 application on AWS Lambda behind API Gateway HTTP API; anonymous read endpoints for this feature. Local development uses Docker PostgreSQL.

**Project Type**: REST web service in a modular monolith.

**Performance Goals**: No numerical latency, throughput or dataset-size targets are approved. Keep the search set-based and index its join keys; measure with the acceptance dataset before proposing a target.

**Constraints**: Preserve ADR-007 and inward dependencies. Use only the four approved Railway Network GET routes. No public station modification API, full SR-005 recurrence/calendar feature, live journey state, booking, or cross-module database writes. The repository currently contains only the starter weather route and no persistence/domain implementation.

**Scale/Scope**: One feature and the four existing backend projects; data and volume targets are not specified. Support a controlled deterministic acceptance dataset without claiming a production scale target.

## Constitution Check

### Pre-design gate

- **Approved scope and traceability**: Pass. The plan cites SR-002, SR-003, SR-004 and SR-013 and preserves their workbook wording and acceptance checks. Cross-cutting dependencies are identified separately.
- **Specification before implementation**: Pass. The clarified specification exists. This plan creates design artifacts only; tasks and implementation remain later workflow steps.
- **Modular monolith and inward dependencies**: Pass. No project or deployment boundary is added. Domain → Application → Infrastructure/API composition follows the existing references.
- **Sources of truth and integrity**: Pass with design decisions recorded below. Schedule eligibility is separate from operational journey state; PostgreSQL constraints enforce relationships.
- **Identity and safety**: Pass. The approved reads remain anonymous and return public projections. The import path is privileged and has no HTTP route. Its operator role must be approved before production use.
- **Documented contracts**: Pass. The four approved endpoints are specified in `contracts/railway-network.openapi.yaml`; no unrelated endpoint is proposed.
- **Evidence-based quality**: Pass. Unit, PostgreSQL, API and flow-1 verification are mapped to SR acceptance evidence.
- **Honest and observable operation**: Pass. Valid empty search, invalid input, missing eligibility publication and source outage have distinct outcomes. No freshness threshold is invented.
- **Approved stack and deployment**: Pass. .NET 10, EF Core, PostgreSQL, RDS and Lambda are retained. No code, dependency, project reference or migration is changed here.

### Post-design gate

Pass subject to the approval gates in [research.md](research.md). The recommendations do not become approved business policy by appearing in this plan. In particular, the exact station-maintainer role, production publication approval, repeated-station route policy, timetable authority and service time-zone basis must be decided before the affected implementation is enabled. No constitution violation or additional project is proposed.

## Architecture and module design

Railway Network is a bounded feature inside the current four projects. Use matching `RailwayNetwork` folders in Domain, Application and Infrastructure; expose endpoint groups from the existing API host. Keep use cases and interfaces in Application, entities and eligibility invariants in Domain, EF Core mappings/import persistence in Infrastructure, and HTTP binding/Problem Details/OpenAPI metadata in Api.

| Layer/project | Feature responsibility | Dependency boundary |
|---|---|---|
| `src/SmartRail.Domain` | Station, route, ordered route stop, train service, schedule revision, published service date/calling point, and pure eligibility rules | No ASP.NET Core, EF Core, AWS or provider references |
| `src/SmartRail.Application` | List stations, get route stops, search services, get service details, and import stations; input/result types and read/import interfaces | References Domain; does not reference Infrastructure |
| `src/SmartRail.Infrastructure` | `SmartRailDbContext`, EF configurations and queries, transactional station import, publication completeness checks | Implements Application interfaces; may reference Application and Domain |
| `src/SmartRail.Api` | Four public GET endpoint groups, validation translation, safe Problem Details, OpenAPI, DI composition, explicit import command entry | References Application and Infrastructure; no business rules or direct table access |
| `tests/SmartRail.UnitTests` | Domain ordering/date rules and Application use cases | Existing project; add references only if implementation proves a missing boundary is needed |
| `tests/SmartRail.IntegrationTests` | HTTP contract and PostgreSQL persistence/constraint tests | Existing project; add test-host and PostgreSQL fixture packages during implementation |

Use small feature-specific query interfaces rather than one generic repository per entity. Search should be a projected EF query that joins published service-date evidence to the schedule's two requested calling points and route sequence. Keep the query behind an Application interface so the Domain test can cover eligibility independently and PostgreSQL tests can prove translation and constraints.

Register application and infrastructure services with focused extension methods (for example `AddSmartRailApplication` and `AddSmartRailInfrastructure`). Map the four read endpoints in a Railway Network endpoint group from `Program.cs`. The current OpenAPI endpoint is development-only; keep generated schemas available in local development and verify the deployment's documented contract without exposing development-only diagnostics.

### Station initialization and maintenance

Use a version-controlled station import file and an explicit import command in the existing application entry point, dispatching to an Application use case. The command is never mapped to HTTP. A Railway Reference Data Steward (proposed role) prepares the change; a separately authorized Railway Network owner approves the production import. The deployed import identity receives write access only for the import operation. This is a proposal: neither role appears in the approved RBAC matrix, so role mapping and human approval authority require project-owner approval before production access is configured.

The importer validates the entire file before writing, normalizes the station code for duplicate detection, rejects duplicate codes and missing required identity/name data, checks route/service references against the resulting station set, and applies the batch in one transaction. Upsert by canonical station code; preserve the stable `StationId`; update only explicitly supplied fields; never delete or withdraw a station on an omitted row. A rerun with the same source produces no additional changes. Record an import-run ID, source revision/hash, submitting and approving identities, time, counts and per-station change summary. Keep field-level history minimal; the broader SR-012 audit capability is Should Have and needs a scope decision if expanded beyond station import evidence.

The exact import file format and the public location/service detail fields must be agreed against the authoritative station source. The contract proposes nullable location description and a list of approved essential service-detail strings; this is a schema proposal, not wording from the workbook. No public station write endpoint is created. The import process must prove station creation, safe update, retrieval and route/service reference integrity (SR-002).

## Database design

Use the supplied ERD as a starting point, not as a migration specification. It supplies `STATION`, `ROUTE`, `ROUTE_STOP`, `TRAIN_SERVICE` and `SERVICE_STOP`; it omits an explicit schedule entity and published-date evidence. See [data-model.md](data-model.md) for proposed fields, keys, constraints, indexes and unresolved gates.

The proposal adds a versioned reusable schedule, ordered calling points tied to route stops, exact service operating dates and a completeness publication record. The date row represents an authoritative published schedule operation, not an operational Journey. Search derives the service's operation date from the requested origin date and that calling point's scheduled departure offset. It requires a complete published date source and both calls in route order. Missing/incomplete date evidence returns an unavailable state, not a fabricated match or a false empty result. No weekly recurrence, holiday rules or generated future calendar is introduced.

All references are foreign keys; code and sequence uniqueness are explicit; route stop and calling-point order are enforced; times/offsets are checked and cross-row order is validated before publication. Keep deleted/withdrawn source records out of public reads; the exact publication/retirement lifecycle is an approval gate. EF Core model configuration owns column types, constraints, indexes and relationships. Create reviewed migrations during implementation, then seed only deterministic local/reference data through the same validated import path. Do not apply migrations or modify the ERD image in this planning phase.

## Train search design

1. Require origin UUID, destination UUID and ISO `YYYY-MM-DD` date. Reject missing/malformed values, identical station IDs and unknown/unpublished station IDs with field-specific validation.
2. Resolve the current published service schedule and its calling points for both station IDs. Route membership alone never counts as a call. The route stop sequence must place origin before destination.
3. Read the origin calling point's departure offset from the schedule's first-call departure. Calculate the schedule operation date whose departure at the selected origin falls on the requested travel date. For an overnight service, this may be the previous calendar date.
4. Require a complete published source manifest for that operation date and an eligible service-operation row. Require a valid departure time for origin and arrival time for destination. If source completeness or timing is missing, fail closed with `503 schedule_data_unavailable`; do not return a partial list as complete.
5. Project service ID/code, route ID/code, requested travel date and the selected origin departure/destination arrival times. Do not include live status, ETA, journey ID, platform, booking or the full service stopping pattern in this feature's search result.
6. Return `200` with an empty `services` array only when the relevant publication is complete and no eligible service matches. Return `503` when required eligibility coverage is missing or unavailable. Do not apply a freshness age threshold until an owner approves one.

For repeated station visits or duplicate calling points, the recommendation is to reject the affected route/schedule from publication until railway owners decide which occurrence a passenger search means. Do not choose a boarding occurrence silently. This policy and the associated uniqueness rule require approval. The proposed time basis is the railway's `Asia/Colombo` local calendar with offset-bearing API timestamps; confirm that basis before implementation. No past-date cutoff, recurrence calendar, or minimum search date range is assumed.

## API contract design

The approved API/RBAC PDF lists exactly four Railway Network routes, all read for Public/Passenger, Driver, Station Staff and Controller. The OpenAPI contract documents complete request/response DTOs and failure shapes for those methods. Anonymous access is intentional; only public published station/service fields are returned. Valid no-result search is `200` with an empty array; invalid query is `400`; missing route/service is `404`; unavailable database or incomplete eligibility publication is `503`. Errors use `application/problem+json` with stable machine code and field errors where applicable. No additional route is added.

Station maintenance uses the controlled import command and its separate authorization/audit boundary. Any future HTTP management surface would require explicit approval and a changed RBAC/API inventory; it is outside these four contracts. See [contracts/railway-network.openapi.yaml](contracts/railway-network.openapi.yaml).

## Testing strategy and SR traceability

Use deterministic station, route, schedule and date-manifest fixtures with a documented source revision. xUnit is already referenced; no tests currently exist. The integration test project currently lacks `Microsoft.AspNetCore.Mvc.Testing`, EF Core/Npgsql, or a PostgreSQL isolation fixture. Add compatible references in implementation only; use `WebApplicationFactory<Program>` and a disposable PostgreSQL database/container so provider queries, migrations, constraints and HTTP serialization are real. Do not substitute an in-memory provider for persistence acceptance.

| Requirement / acceptance evidence | Domain/Application | PostgreSQL/API or flow evidence |
|---|---|---|
| SR-002 — records can be created, retrieved, referenced by services and displayed | Import validation, stable ID on repeat import, update preservation and authorized command checks | Real-PG create/update/re-import, duplicate code rejection, route/service FK checks, public `GET /api/stations`; authorized import succeeds and unauthorized invocation is denied |
| SR-003 — route stops returned in order with unambiguous IDs | Stop ordering and ID projection | FK/order constraints; `GET /api/routes/{id}/stops` preserves sequence and distinguishes 404/503 |
| SR-004 — every published service references valid route, schedule and service identifier | Publication rejects missing references | Real-PG FK and publication tests; `GET /api/services/{id}` public DTO contains valid references only |
| SR-013 — valid search results; invalid pairs and no-results provide useful feedback | Direction, skipped route station, exact date manifest, overnight departure at selected origin, same station, absent call, duplicate/ambiguous call, no-match vs unavailable | `GET /api/services` query/response and OpenAPI tests; anonymous read succeeds; malformed/missing/unknown inputs are 400, complete empty result is 200, incomplete data is 503 |
| SR-089 / SR-114 support — permitted public reads and distinct error/empty states | No operational/private properties in read projections | Anonymous requests to all four routes; problem shape, no-result, 404 and 503 cases; test no station-management HTTP route exists |

Additional PostgreSQL tests cover transaction rollback on invalid import, unique constraints, route-stop ordering, schedule-to-route consistency, date-manifest completeness, and an overnight date where origin departure is the following day after service start. API contract tests compare generated OpenAPI schemas/parameters/status responses to the checked-in YAML. E2E flow 1 verifies anonymous origin/destination/date search to planned route/service detail and the adjacent Journeys & Status projection; this feature alone does not implement live status. No UI code or full five-flow release test is part of Feature 001.

## Local development and verification

Add a pinned PostgreSQL 18 Docker Compose service with a named local volume, health check, non-production credentials and a separate test database/container. Add an ignored local secrets file or .NET User Secrets for connection strings; commit only safe example configuration. Set a single railway time-zone configuration after approval. Keep production RDS credentials outside source control and restrict RDS to the Lambda/application and approved import identity.

Planned developer sequence after implementation artifacts exist:

```powershell
docker compose up -d postgres
dotnet ef database update --project src/SmartRail.Infrastructure --startup-project src/SmartRail.Api
dotnet run --project src/SmartRail.Api
dotnet test tests/SmartRail.UnitTests
dotnet test tests/SmartRail.IntegrationTests
```

The first two commands depend on the future Compose service and EF tooling; they cannot run against the current starter repository. Run the controlled import against a local fixture after migrations. Use the API OpenAPI document and the smoke checks in [quickstart.md](quickstart.md) to verify station list, ordered route stops, valid search, overnight origin date, valid empty result and incomplete-source error. Configuration names and executable command syntax must be finalized when implementation adds the EF context and importer.

## Project Structure

### Documentation (this feature)

```text
specs/001-railway-network-search/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
└── contracts/
    └── railway-network.openapi.yaml
```

### Source Code (repository root)

```text
src/
├── SmartRail.Api/
│   ├── Program.cs
│   └── Features/RailwayNetwork/       # four GET routes, HTTP validation/DTO mapping
├── SmartRail.Application/
│   └── Features/RailwayNetwork/       # query/import use cases and interfaces
├── SmartRail.Domain/
│   └── RailwayNetwork/                # entities and eligibility rules
└── SmartRail.Infrastructure/
    └── RailwayNetwork/                # EF mapping, query store and importer
tests/
├── SmartRail.UnitTests/
│   └── RailwayNetwork/
└── SmartRail.IntegrationTests/
    └── RailwayNetwork/
```

**Structure Decision**: Use the existing `SmartRail.Api`, `SmartRail.Application`, `SmartRail.Domain`, `SmartRail.Infrastructure`, `SmartRail.UnitTests` and `SmartRail.IntegrationTests` projects. The listed feature folders are planned locations, not existing files. Do not create separate Railway Network projects or a second backend deployment.

## Complexity Tracking

No constitution deviation or additional project is proposed. The date publication and calling-point structures exist only to meet SR-013's date and ordered-call eligibility without creating operational journey state. Approval gates and deferred policy are listed in [research.md](research.md).
