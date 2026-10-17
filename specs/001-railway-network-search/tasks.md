---
description: "Dependency-ordered implementation tasks for Railway Network & Train Search"
---

# Tasks: SmartRail Railway Network & Train Search

**Feature**: `001-railway-network-search`  
**Inputs**: [spec.md](spec.md), [plan.md](plan.md), [research.md](research.md), [data-model.md](data-model.md), [quickstart.md](quickstart.md), [OpenAPI contract](contracts/railway-network.openapi.yaml), [requirements inventory](../../docs/SmartRail-SDD-Handoff/requirements-inventory.md)

**Scope**: Implement only the Railway Network reference data and scheduled-service search in the existing six-project .NET solution. No live journey creation/status, disruption/incident workflows, driver operations, catering, public station-write API, or additional projects.

**Story mapping**: `[US1]` = P1 passenger train search (SR-013); `[US2]` = P2 stations and ordered routes (SR-002/SR-003); `[US3]` = P3 scheduled service details (SR-004).

**Planning decisions applied**: `Asia/Colombo` local schedule dates/times; versioned controlled timetable fixtures; exact operation-date completeness; one current published schedule revision per service with previous revisions preserved; complete-empty, incomplete, and absent dates remain distinct; ambiguous repeated-station publications are withheld until occurrence semantics are approved; only source-verified public station fields are exposed; station audit is limited to import/change evidence. Production station-import and timetable-publication authorities remain approval gates.

## Approved Requirement Traceability

The following wording and acceptance checks are transcribed from the approved workbook (`FINAL UNIFIED REQUIREMENTS`, rows 6, 7, 8 and 17). Task descriptions reference these IDs; acceptance work must preserve the approved checks.

| ID | Priority | Approved requirement | Workbook acceptance check |
|---|---|---|---|
| SR-002 | Must Have | The platform shall maintain identifiable railway stations and expose station names and essential location/service details to passenger and operational channels. | Station records can be created, retrieved, referenced by services, and displayed. |
| SR-003 | Must Have | The platform shall represent railway routes and their ordered station sequences. | Route stops are returned in journey order with unambiguous station IDs. |
| SR-004 | Must Have | The platform shall maintain train services associated with routes and identifiable service schedules. | Every published service references a valid route, schedule and service identifier. |
| SR-013 | Must Have | Passengers shall be able to search railway services using an origin, destination and travel date. | Valid search returns matching service choices; invalid pairs and no-result searches show useful feedback. |

**Migration sequencing note**: Phase 1 prepares EF Core, database configuration, and migration tooling. The actual initial schema migration is in Phase 3, after the Phase 2 entities and Phase 3 mappings exist; generating it in Phase 1 would create an incomplete schema and contradict the data model.

## Phase 1 — Foundation

**Exit criteria**: PostgreSQL starts locally with safe non-production configuration; the existing API and test projects can resolve EF Core and API test hosting; migration commands are documented and do not auto-run against production.

- [ ] T001 [P] Add mutually compatible EF Core 10, Npgsql EF provider 10.x, and EF design-time package references to `src/SmartRail.Infrastructure/SmartRail.Infrastructure.csproj`; preserve the `net10.0` target and verify package compatibility without introducing another data-access framework.
- [ ] T002 [P] Add `Microsoft.AspNetCore.Mvc.Testing` to `tests/SmartRail.IntegrationTests/SmartRail.IntegrationTests.csproj` for in-process API contract tests; keep the existing xUnit projects and project references.
- [ ] T003 [P] Add a PostgreSQL 18 local service, health check, persistent development volume, and non-production-only credentials to `docker-compose.yml`; do not include production credentials or apply migrations automatically.
- [ ] T004 [P] Add safe connection-string examples and environment binding in `src/SmartRail.Api/appsettings.Development.example.json` and `src/SmartRail.Api/Program.cs`; read real credentials from .NET User Secrets or environment variables and ensure local secret files are ignored by `.gitignore`.
- [ ] T005 Create `src/SmartRail.Infrastructure/RailwayNetwork/SmartRailDbContext.cs` and register it from `src/SmartRail.Infrastructure/DependencyInjection.cs` using Npgsql; keep DbContext and provider configuration out of Domain and Application.
- [ ] T006 Configure EF design-time creation and explicit migration commands in `src/SmartRail.Infrastructure/SmartRailDbContextFactory.cs` and `specs/001-railway-network-search/quickstart.md`; prohibit implicit production startup migration.
- [ ] T007 Document local/test database separation and connection-secret setup in `specs/001-railway-network-search/quickstart.md`; ensure integration tests fail clearly when PostgreSQL is unavailable rather than silently skipping persistence tests.
- [ ] T008 [P] Verify solution project references and package targets still follow the approved six-project architecture in `SmartRail.slnx`; record any required project-reference change as a task amendment before changing the architecture.

## Phase 2 — Railway Network Domain

**Exit criteria**: Domain entities express stable station identity, ordered route/call structure, immutable schedule revisions, completeness evidence, and pure search eligibility/date rules without infrastructure dependencies.

- [ ] T009 [P] [US2] Create `Station` in `src/SmartRail.Domain/RailwayNetwork/Entities/Station.cs` with stable `StationId`, required name, publication state, source-backed detail values, and an internal source identity that is not exposed by default; require verified source-field mapping before production data is loaded (SR-002).
- [ ] T010 [P] [US2] Create `Route` in `src/SmartRail.Domain/RailwayNetwork/Entities/Route.cs` with stable ID, route identity, name and publication state; keep it as reusable network reference data (SR-003).
- [ ] T011 [P] [US2] Create ordered `RouteStop` in `src/SmartRail.Domain/RailwayNetwork/Entities/RouteStop.cs`; enforce `stop_order > 0`, a stable station reference and no route-wide uniqueness rule on station ID (SR-003).
- [ ] T012 [P] [US3] Create `TrainService` in `src/SmartRail.Domain/RailwayNetwork/Entities/TrainService.cs` with stable service identity, route reference and publication state (SR-004).
- [ ] T013 [P] [US3] Create immutable `ServiceScheduleRevision` in `src/SmartRail.Domain/RailwayNetwork/Entities/ServiceScheduleRevision.cs`; enforce `revision > 0`, local first-departure time in `Asia/Colombo`, publication/supersession state, preserved history and exactly one current published revision for every published service (SR-004).
- [ ] T014 [P] [US1] Create `ServiceCallingPoint` in `src/SmartRail.Domain/RailwayNetwork/Entities/ServiceCallingPoint.cs` linked to a schedule revision and route stop; represent nullable arrival/departure offsets in minutes from first-call departure; enforce offsets `>= 0` and `arrival_offset_minutes <= departure_offset_minutes` when both values are present (SR-003/SR-013).
- [ ] T015 [P] [US1] Create `ServiceDateManifest` in `src/SmartRail.Domain/RailwayNetwork/Entities/ServiceDateManifest.cs` with operation date, source/fixture revision, explicit completeness and publication evidence; distinguish complete-empty from incomplete and absent dates (SR-013).
- [ ] T016 [P] [US1] Create `ServiceOperationDate` in `src/SmartRail.Domain/RailwayNetwork/Entities/ServiceOperationDate.cs` as exact schedule-revision operation-date evidence; do not add weekday recurrence, holiday calendars or generated future dates (SR-013; SR-005 remains deferred).
- [ ] T017 [P] [US1] Implement schedule publication invariants in `src/SmartRail.Domain/RailwayNetwork/Rules/SchedulePublicationValidator.cs`: validate referenced route stops, ordered calls, required endpoint times and revision state; withhold publication if a station ID repeats ambiguously until occurrence semantics are approved.
- [ ] T018 [P] [US1] Implement pure eligibility and origin-date calculation in `src/SmartRail.Domain/RailwayNetwork/Rules/ServiceSearchEligibility.cs`; use origin-before-destination call order and compute the base operation date from `Asia/Colombo` first departure plus origin offset, including offsets crossing one or more midnights (SR-013).

## Phase 3 — Persistence and Reference Data

**Exit criteria**: PostgreSQL constraints enforce the model; migrations are reviewed and repeatable; station imports are validated and atomic with minimal evidence; deterministic timetable fixtures reproduce every publication state.

- [ ] T019 [P] [US2] Add station EF mapping in `src/SmartRail.Infrastructure/RailwayNetwork/Configurations/StationConfiguration.cs`; enforce required name, stable ID and uniqueness only on the confirmed private source identity, while persisting only railway-owner-verified public detail fields (SR-002).
- [ ] T020 [P] [US2] Add route and ordered-stop mappings in `src/SmartRail.Infrastructure/RailwayNetwork/Configurations/RouteConfiguration.cs` and `RouteStopConfiguration.cs`; enforce route FK, station FK, positive stop order and unique `(route_id, stop_order)` (SR-003).
- [ ] T021 [P] [US3] Add service and schedule-revision mappings in `src/SmartRail.Infrastructure/RailwayNetwork/Configurations/TrainServiceConfiguration.cs` and `ServiceScheduleRevisionConfiguration.cs`; enforce route/revision FKs, unique `(service_id, revision)` and a partial unique constraint for one current published revision per service (SR-004).
- [ ] T022 [P] [US1] Add calling-point, operation-date and manifest mappings in `src/SmartRail.Infrastructure/RailwayNetwork/Configurations/ServiceCallingPointConfiguration.cs`, `ServiceOperationDateConfiguration.cs` and `ServiceDateManifestConfiguration.cs`; enforce references, offsets `>= 0`, `is_complete BOOLEAN NOT NULL`, one manifest per current operation date and required completeness evidence (SR-013).
- [ ] T023 [US1] Add search indexes and cross-row publication checks in `src/SmartRail.Infrastructure/RailwayNetwork/Configurations/RailwayNetworkIndexes.cs` and `src/SmartRail.Infrastructure/RailwayNetwork/Persistence/SchedulePublicationRepository.cs`; index origin/destination call lookups and reject incomplete or ambiguous publications before they become current (SR-003/SR-004/SR-013).
- [ ] T024 [US3] Implement atomic schedule publication in `src/SmartRail.Infrastructure/RailwayNetwork/Persistence/SchedulePublicationRepository.cs`; in one transaction supersede the prior current revision, publish the replacement, and retain the prior revision and source revision evidence (SR-004).
- [ ] T025 [P] [US1] Implement exact-date source completeness persistence in `src/SmartRail.Infrastructure/RailwayNetwork/Persistence/ServiceDateManifestRepository.cs`; store complete dates even when they contain zero service-operation rows and never treat an absent/incomplete manifest as a confirmed no-service date (SR-013).
- [ ] T026 Generate and review the initial Railway Network schema migration and model snapshot under `src/SmartRail.Infrastructure/Migrations/` after T009–T025; verify create/update against PostgreSQL 18 and confirm the migration preserves all keys, foreign keys, indexes, current-revision constraint and date completeness representation.
- [ ] T027 [US2] Implement whole-file station import validation in `src/SmartRail.Application/RailwayNetwork/Stations/ImportStationsValidator.cs` and persistence support in `src/SmartRail.Infrastructure/RailwayNetwork/Persistence/StationImportRepository.cs`; reject duplicate source identities, missing required identity/name, and unresolved route/service references before writing (SR-002).
- [ ] T028 [US2] Apply station import upserts and minimal import/change evidence in one transaction in `src/SmartRail.Infrastructure/RailwayNetwork/Persistence/StationImportRepository.cs`; preserve `StationId`, update only supplied verified fields, never delete on omission, record source revision/hash, submitter/approver, timestamp and counts `>= 0` with per-station action/changed-field names, and roll back all changes on failure (SR-002).
- [ ] T029 [P] [US1] Add versioned deterministic station and timetable publications in `tests/SmartRail.IntegrationTests/Fixtures/RailwayNetwork/stations-v1.json` and `tests/SmartRail.IntegrationTests/Fixtures/RailwayNetwork/timetable-v1.json`; include source revision, prior/current schedule revisions, ordered calls, a complete date with zero service rows, an incomplete date and an absent date, using only verified public station fields.
- [ ] T030 [US1] Add fixture loading through production validation/publication rules in `tests/SmartRail.IntegrationTests/RailwayNetwork/RailwayNetworkFixtureLoader.cs`; restrict fixture loading to local/test hosts and prevent fixture data from seeding production automatically.
- [ ] T031 Add migration and PostgreSQL model verification in `tests/SmartRail.IntegrationTests/RailwayNetwork/Persistence/RailwayNetworkMigrationTests.cs`; assert schema creation, unique/foreign-key constraints, partial current-revision constraint and transactional publication behavior against real PostgreSQL.

## Phase 4 — Application Use Cases

**Exit criteria**: Feature-specific application handlers return stable read projections, validate search criteria, distinguish no-result from unavailable eligibility, and provide controlled import/publication entry points behind authorization gates.

- [ ] T032 [P] [US2] Define station read and import interfaces in `src/SmartRail.Application/RailwayNetwork/Stations/IStationRepository.cs` and `IStationImportRepository.cs`; expose use-case projections without leaking EF types (SR-002).
- [ ] T033 [P] [US2] Define route-stop and service query interfaces in `src/SmartRail.Application/RailwayNetwork/Routes/IRouteReadRepository.cs` and `Services/IServiceReadRepository.cs`; keep current-revision selection inside the use-case abstraction (SR-003/SR-004).
- [ ] T034 [P] [US2] Implement `ListStationsQuery` and handler in `src/SmartRail.Application/RailwayNetwork/Stations/ListStationsQuery.cs` and `ListStationsHandler.cs`; return only stable station ID, name and verified/approved location-service details (SR-002).
- [ ] T035 [P] [US2] Implement `GetRouteStopsQuery` and handler in `src/SmartRail.Application/RailwayNetwork/Routes/GetRouteStopsQuery.cs` and `GetRouteStopsHandler.cs`; project station IDs/names in ascending route order (SR-003).
- [ ] T036 [P] [US3] Implement `GetServiceQuery` and handler in `src/SmartRail.Application/RailwayNetwork/Services/GetServiceQuery.cs` and `GetServiceHandler.cs`; return service ID, route and current identifiable schedule revision only, without live journey fields (SR-004).
- [ ] T037 [US1] Define search query/result DTOs, `IServiceSearchRepository`, and input validation in `src/SmartRail.Application/RailwayNetwork/Services/SearchServicesQuery.cs`, `IServiceSearchRepository.cs` and `SearchServicesValidator.cs`; require origin, destination and valid calendar date, reject identical IDs and unknown/unpublished stations, and preserve useful field feedback (SR-013).
- [ ] T038 [US1] Implement set-based eligible service search in `src/SmartRail.Infrastructure/RailwayNetwork/Persistence/ServiceSearchRepository.cs`; match current published revision, exact base operation date derived from requested origin date, complete manifest, both service calls and origin-before-destination order; return complete empty results only when publication coverage is complete (SR-013).
- [ ] T039 [US1] Implement search handler in `src/SmartRail.Application/RailwayNetwork/Services/SearchServicesHandler.cs`; map local schedule values to `Asia/Colombo` scheduled timestamps with `+05:30`, distinguish invalid criteria, valid empty results and unavailable/incomplete evidence, and return no journey/live-state claims (SR-013).
- [ ] T040 [US2] Implement controlled station import use case and authorization boundary in `src/SmartRail.Application/RailwayNetwork/Stations/ImportStationsCommand.cs` and `ImportStationsHandler.cs`; require a verified authorized invocation, validate before commit and keep the command out of HTTP routing; production role/approval mapping must remain disabled until approved (SR-002).
- [ ] T041 [US3] Implement versioned timetable publication use case and `ISchedulePublicationRepository` in `src/SmartRail.Application/RailwayNetwork/Services/PublishTimetableCommand.cs`, `PublishTimetableHandler.cs` and `ISchedulePublicationRepository.cs`; validate completeness and revision rules before atomically replacing the current published revision, preserving prior history; require an approved production publisher before production writes (SR-004/SR-013).

## Phase 5 — REST API

**Exit criteria**: Exactly the four approved public GET operations are mapped; schemas, validation, Problem Details and public projection match the checked-in OpenAPI contract; no station-management route is exposed.

- [ ] T042 Create Railway Network endpoint mapping in `src/SmartRail.Api/Endpoints/RailwayNetworkEndpoints.cs` and register it from `src/SmartRail.Api/Program.cs`; map only `GET /api/stations`, `GET /api/routes/{id}/stops`, `GET /api/services`, and `GET /api/services/{id}` (SR-002/SR-003/SR-004/SR-013).
- [ ] T043 [P] [US2] Create station and route response DTOs in `src/SmartRail.Api/Contracts/RailwayNetwork/StationResponse.cs` and `RouteStopsResponse.cs`; expose station ID/name and only source-verified approved location/service fields, and route stops with stable station IDs and ordered names (SR-002/SR-003).
- [ ] T044 [P] [US1] Create service details and search response DTOs in `src/SmartRail.Api/Contracts/RailwayNetwork/ServiceDetailResponse.cs` and `ServiceSearchResponse.cs`; include current schedule revision and selected origin/destination planned times but exclude journey ID, live status, ETA, platform, booking or ticket data (SR-004/SR-013).
- [ ] T045 [US1] Add UUID/date parsing, same-station rejection and station lookup validation in `src/SmartRail.Api/Endpoints/RailwayNetworkEndpoints.cs`; map malformed/missing/unknown criteria to contract-defined 400 responses (SR-013).
- [ ] T046 [US1] Map route/service not-found and source/database-unavailable outcomes in `src/SmartRail.Api/Endpoints/RailwayNetworkEndpoints.cs`; ensure incomplete/absent date-completeness evidence cannot be translated into a successful empty search (SR-003/SR-004/SR-013).
- [ ] T047 Configure safe RFC 9457 Problem Details and stable error codes in `src/SmartRail.Api/Errors/ProblemDetailsConfiguration.cs`; include field errors and trace IDs without exception detail, secrets or personal data (SR-114 support).
- [ ] T048 Publish generated OpenAPI metadata from `src/SmartRail.Api/Program.cs` for the four read operations and synchronize parameter/schema/status behavior with `specs/001-railway-network-search/contracts/railway-network.openapi.yaml`; do not add station write routes or unapproved operations.
- [ ] T049 Confirm all four Railway Network reads are anonymous and expose only approved public projections in `src/SmartRail.Api/Endpoints/RailwayNetworkEndpoints.cs`; do not weaken auth policies on other endpoints or add a public import command (RBAC/API baseline; SR-089 support).
- [ ] T050 Verify no HTTP station creation/update/delete or timetable publication operation exists in `src/SmartRail.Api/Endpoints/RailwayNetworkEndpoints.cs`; keep controlled import/publication commands outside REST and document their approval gates in `specs/001-railway-network-search/research.md` (SR-002).

## Phase 6 — Automated Testing

**Exit criteria**: Domain, application, PostgreSQL and API tests cover source acceptance checks, positive and negative behavior, incomplete data and scope boundaries using real PostgreSQL where persistence behavior is asserted.

- [ ] T051 [P] [US2] Add Station identity/detail/import invariant tests in `tests/SmartRail.UnitTests/RailwayNetwork/StationTests.cs`; cover create, verified-field update, stable ID, duplicate source identity and missing name cases (SR-002).
- [ ] T052 [P] [US2] Add ordered route and calling-point rule tests in `tests/SmartRail.UnitTests/RailwayNetwork/RouteOrderingTests.cs`; cover ascending order, duplicate order rejection, station-ID disambiguation and ambiguous repeated-station publication withholding (SR-003/SR-013).
- [ ] T053 [P] [US3] Add schedule revision and publication validator tests in `tests/SmartRail.UnitTests/RailwayNetwork/SchedulePublicationTests.cs`; prove one current published revision, previous revision retention, invalid route/schedule rejection and no ambiguous-call publication (SR-004/SR-013).
- [ ] T054 [P] [US1] Add date-manifest eligibility tests in `tests/SmartRail.UnitTests/RailwayNetwork/ServiceDateManifestTests.cs`; prove complete-empty is authoritative no-service while incomplete and absent manifests are unavailable (SR-013).
- [ ] T055 [P] [US1] Add `Asia/Colombo` origin-date arithmetic tests in `tests/SmartRail.UnitTests/RailwayNetwork/OvernightSearchDateTests.cs`; cover same-day calls, midnight crossing, origin after midnight with prior operation date, and offsets spanning multiple days (SR-013).
- [ ] T056 [P] Add application handler tests in `tests/SmartRail.UnitTests/RailwayNetwork/QueryHandlerTests.cs`; cover station list projections, ordered route stops, current service details, invalid search criteria and empty search results (SR-002/SR-003/SR-004/SR-013).
- [ ] T057 [US2] Add station import validation and authorization tests in `tests/SmartRail.UnitTests/RailwayNetwork/StationImportHandlerTests.cs`; verify unauthorized calls do not write, the whole batch validates before commit, omitted rows are not deleted and audit scope remains minimal (SR-002).
- [ ] T058 Add real-PostgreSQL migration and constraints tests in `tests/SmartRail.IntegrationTests/RailwayNetwork/Persistence/RailwayNetworkMigrationTests.cs`; exercise FKs, unique keys, order checks, one-current-revision constraint and schedule/route consistency (SR-003/SR-004).
- [ ] T059 [US2] Add transaction and import-evidence tests in `tests/SmartRail.IntegrationTests/RailwayNetwork/Persistence/StationImportTests.cs`; prove a valid import creates/updates/retrieves station records, failed validation rolls back the entire batch, repeated import is idempotent, service references remain valid, and only import/change evidence is written (SR-002).
- [ ] T060 [US3] Add revision publication persistence tests in `tests/SmartRail.IntegrationTests/RailwayNetwork/Persistence/ScheduleRevisionPublicationTests.cs`; publish a replacement transactionally, preserve the prior revision, enforce one current revision and prove failed publication leaves the prior revision current (SR-004).
- [ ] T061 [US1] Add search repository PostgreSQL tests in `tests/SmartRail.IntegrationTests/RailwayNetwork/Persistence/ServiceSearchRepositoryTests.cs`; prove exact-date coverage, skipped calling points, origin-before-destination, complete-empty no result, incomplete/absent manifest unavailable and prior schedule revisions excluded (SR-013).
- [ ] T062 [P] Add HTTP contract tests through `WebApplicationFactory` in `tests/SmartRail.IntegrationTests/RailwayNetwork/Api/RailwayNetworkEndpointTests.cs`; exercise all four approved GET routes anonymously and verify there is no station-management HTTP route (SR-002/SR-003/SR-004/SR-013).
- [ ] T063 [P] [US1] Add positive search and overnight API tests in `tests/SmartRail.IntegrationTests/RailwayNetwork/Api/ServiceSearchEndpointTests.cs`; prove a valid service appears only for correct ordered calls and origin departure date, with `Asia/Colombo` `+05:30` scheduled timestamps and planned-only response fields (SR-013).
- [ ] T064 [P] Add negative/error API tests in `tests/SmartRail.IntegrationTests/RailwayNetwork/Api/RailwayNetworkFailureTests.cs`; cover missing/malformed query, invalid calendar date, same/unknown/unpublished stations, reversed calls, 200 empty result, 404 route/service and 503 unavailable evidence with Problem Details (SR-013; SR-114 support).
- [ ] T065 [P] Add OpenAPI contract comparison in `tests/SmartRail.IntegrationTests/RailwayNetwork/Api/RailwayNetworkOpenApiTests.cs`; compare generated API methods, paths, parameters, DTO fields and statuses against `specs/001-railway-network-search/contracts/railway-network.openapi.yaml`, including absence of unverified station fields (SR-002/SR-003/SR-004/SR-013).

## Phase 7 — Final Verification

**Exit criteria**: The complete solution builds, all relevant tests pass, all four approved endpoints and acceptance checks are evidenced, and feature boundaries remain intact.

- [ ] T066 Build all six existing projects with `dotnet build SmartRail.slnx`; fix compile/analyzer errors only within the approved solution and record the result in `specs/001-railway-network-search/quickstart.md`.
- [ ] T067 Run `dotnet test SmartRail.slnx` including the real PostgreSQL integration suite; record the command, database prerequisite and passing result in `specs/001-railway-network-search/quickstart.md`.
- [ ] T068 Run the four endpoint smoke checks in `specs/001-railway-network-search/quickstart.md`; verify station list, ordered stops, valid search and service detail with fixture IDs and confirm anonymous access.
- [ ] T069 Verify the exact SR-002, SR-003, SR-004 and SR-013 workbook acceptance checks against named tests and results in `specs/001-railway-network-search/plan.md`; record any unmet check as open rather than marking the feature complete.
- [ ] T070 Verify public response data and error handling against the approved RBAC/API PDF and `specs/001-railway-network-search/contracts/railway-network.openapi.yaml`; confirm no station write route, no unverified station detail and safe Problem Details (SR-089/SR-114 support).
- [ ] T071 Verify incomplete/absent date evidence never produces a false empty result, complete-empty dates do, and overnight operation dates can precede requested origin dates in `tests/SmartRail.IntegrationTests/RailwayNetwork/Api/ServiceSearchEndpointTests.cs` and `RailwayNetworkFailureTests.cs` (SR-013).
- [ ] T072 Verify the implementation does not create dated journeys, publish live status, manage disruptions/incidents, operate drivers or implement catering in `specs/001-railway-network-search/plan.md` and the final API/OpenAPI route inventory; Journeys & Status remains the owner of operational records.
- [ ] T073 Update the feature verification matrix with final test names, evidence and remaining approval gates in `specs/001-railway-network-search/plan.md`; preserve traceability from each SR acceptance check to its automated test.
- [ ] T074 Re-run `git diff --check` and inspect `git status --short`; confirm no workbook, constitution, application code or package changes were made by the planning/task-generation work before reporting review readiness.

## Dependency Order

1. Complete Phase 1 before domain work: T001–T007 establish dependencies, local PostgreSQL, safe configuration, DbContext/DI and migration tooling. T008 confirms existing project boundaries.
2. Complete Phase 2 domain model/rules: T009–T018. The independent entity files can be implemented concurrently after T008; publication and date rules follow their entity contracts.
3. Complete Phase 3 persistence: T019–T025 and T027–T030 can proceed in parallel by distinct files after Phase 2. T026 depends on the model and mappings in T019–T025. T031 depends on T026 and the database setup T003–T006.
4. Complete Phase 4 use cases: T032–T037 can proceed by separate use-case files after Phase 2; T038 depends on T019–T025 and T018; T039 depends on T037–T038; T040 depends on T027–T028; T041 depends on T017 and T024–T025.
5. Complete Phase 5 API: T042–T050 depend on the relevant Phase 4 handlers. T043 and T044 can proceed in parallel before endpoint binding; T045–T050 integrate through `RailwayNetworkEndpoints.cs` and must be coordinated to avoid same-file conflicts.
6. Complete Phase 6 verification: T051–T057 can proceed in parallel after their domain/application rules exist; T058–T061 require PostgreSQL/migrations and Phase 3 persistence; T062–T065 require Phase 5 endpoints and T029–T030 fixtures. Do not treat mocked persistence as evidence for database constraints.
7. Complete Phase 7 after the implementation and tests: T066–T074. Build/test/smoke/traceability results must be recorded before review.

## Parallel Execution Opportunities

- **Foundation**: T001, T002, T003 and T004 touch distinct project/configuration files; T005 follows T001; T006 follows T005.
- **Domain**: T009–T016 are separate entity files and can run concurrently. T017–T018 follow the relevant entities.
- **Persistence**: T019–T022 are distinct EF configuration files and can run concurrently. T027 station-import validation/adapter and T029 fixture authoring can proceed alongside mapping work after the domain model exists. T026 waits for all model mappings.
- **Application**: T032–T036 are separate interfaces/handlers. Search implementation T037–T039 depends on validation and repository contracts; import/publication T040–T041 are separate workflows.
- **Verification**: T051–T057 are separate unit-test files; T058–T061 are separate PostgreSQL test classes after schema availability; T062–T065 are separable API test classes after endpoints are stable.

Tasks touching the same file (notably `Program.cs`, `RailwayNetworkEndpoints.cs`, `ServiceSearchRepository.cs`, `quickstart.md`, and `plan.md`) are intentionally serialized or require a single integrator.

### Task-Level Prerequisites

- T001–T004 and T008 have no feature-code prerequisites. T005 depends on T001; T006 depends on T005; T007 depends on T003–T006.
- T009–T016 depend on Phase 1 completion and can proceed in parallel. T017 depends on T009–T016; T018 depends on T013–T016.
- T019 depends on T009; T020 on T010–T011; T021 on T012–T013; T022 on T014–T016. T023 depends on T019–T022; T024 on T017/T021/T023; T025 on T022. T026 depends on T019–T025. T027 depends on T009/T019; T028 on T026–T027; T029 on T009–T016 and the source-field gate; T030 on T024–T025/T027–T029; T031 on T003–T006/T026.
- T032–T033 depend on their corresponding domain entities; T034 on T032/T019; T035 on T033/T020; T036 on T033/T021; T037 on T018/T033; T038 on T018/T022–T025/T037; T039 on T037–T038; T040 on T027–T028/T032; T041 on T017/T021–T025 and the production-publication gate.
- T042 depends on Phase 1 and the read handlers T034–T039; T043–T044 can proceed in parallel after their read handlers; T045–T050 depend on T042–T044 and must be integrated in the shared endpoint/Program files.
- T051–T057 depend on corresponding domain/application tasks; T058–T061 depend on T026–T031; T062–T065 depend on T042–T050 and T029–T030. T066–T074 depend on the implementation and relevant test tasks T051–T065.

## MVP Delivery Strategy

Deliver the P1 search increment first: finish foundation and domain/persistence prerequisites, then the date-eligible search path, public `GET /api/services`, and its positive/negative PostgreSQL/API tests. The P2 station/route read and controlled-import acceptance tasks (SR-002/SR-003) and P3 service detail (SR-004) are required Feature 001 scope and must be complete before the feature is declared done. The downstream passenger-status portion of cross-boundary E2E Flow 1 is owned by Journeys & Status; Feature 001 must expose the planned service choice and preserve that module boundary without manufacturing a live journey.

## Outstanding Approval Gates

- Name the authoritative production timetable source and authorized publisher; confirm its exact-date completeness guarantee before interpreting missing rows as no operation.
- Name the production station-import maintainer and independent approval authority; do not grant production write access based on proposed role names.
- Confirm station source identity mapping and exact location/service details approved for public display; fixtures and responses contain only verified fields.
- Keep publication of routes/schedules with repeated station IDs withheld until deterministic call-occurrence semantics are approved.
- Confirm withdrawal/supersession authority and any timetable freshness/refresh threshold before implementing age-based expiry. No age threshold is assumed.

