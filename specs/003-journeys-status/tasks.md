---
description: "Dependency-ordered implementation tasks for SmartRail Feature 003"
---

# Tasks: SmartRail Journeys & Status

**Input**: Approved design documents in `specs/003-journeys-status/`.

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `quickstart.md`, and the approved OpenAPI and supporting contracts.

**Scope**: Existing six-project .NET 10 modular monolith; PostgreSQL; exactly five approved HTTP operations. No new projects, RBAC grants, AWS resources, Feature 001/002 changes, driver-observation ownership, or incident lifecycle implementation.

**Task format**: Every task is unchecked and follows `- [ ] Txxx [P?] [US#?] action with target file path(s)`. `[P]` is used only where files and prerequisites allow independent work.

## Phase 1: Setup and Shared Prerequisites

**Purpose**: Establish Feature 003 folders and safe local test assumptions without changing existing feature behavior.

- [ ] T001 [P] Create `src/SmartRail.Domain/JourneysStatus/`, `src/SmartRail.Application/JourneysStatus/`, `src/SmartRail.Infrastructure/JourneysStatus/`, `src/SmartRail.Api/Endpoints/JourneysStatus/`, `tests/SmartRail.UnitTests/JourneysStatus/`, and `tests/SmartRail.IntegrationTests/JourneysStatus/` following existing project conventions (FR-019).
- [ ] T002 [P] Add deterministic journey, schedule-linkage, stop, revision, and authorization fixture builders under `tests/SmartRail.UnitTests/JourneysStatus/Fixtures/` and `tests/SmartRail.IntegrationTests/JourneysStatus/Fixtures/` without production credentials or test authentication bypasses (SC-001, SC-010).
- [ ] T003 [P] Add Feature 003 test collection/database isolation configuration in `tests/SmartRail.IntegrationTests/JourneysStatus/` reusing `PostgreSqlTestDatabaseFixture` on the isolated PostgreSQL test service, never development or production databases (SC-010).
- [ ] T004 Document local PostgreSQL, migration, test, and no-AWS boundaries in `specs/003-journeys-status/quickstart.md` (FR-019, SC-010).

**Checkpoint**: Folder conventions, deterministic fixtures, and isolated test-database rules are established.

## Phase 2: Foundational Journey Identity and Domain Model

**Purpose**: Implement shared domain invariants, persistence shape, migration, and application ports required by all user stories.

- [ ] T005 [P] Add journey lifecycle and safe public-source value types in `src/SmartRail.Domain/JourneysStatus/` matching the approved states and source labels in `specs/003-journeys-status/spec.md` (FR-001, FR-003, FR-021, SC-012).
- [ ] T006 [P] Implement the `Journey` aggregate in `src/SmartRail.Domain/JourneysStatus/Entities/Journey.cs` with opaque `JourneyInstanceId`, service/schedule-revision/operation-date/first-departure linkage, current revision, and no uniqueness assumption on the linkage composite (SR-007, FR-001, SC-001, SC-011).
- [ ] T007 [P] Implement immutable `JourneyRevision` and ordered `JourneyRevisionStop` snapshots in `src/SmartRail.Domain/JourneysStatus/Entities/` with one optional baseline `UpdateId`, immutable official-publication `UpdateId`, same-journey correction linkage, and stable stop IDs (FR-002, FR-004, FR-021, FR-022, SC-013, SC-016, SC-017).
- [ ] T008 Implement the approved journey transition and terminal-correction rules in `src/SmartRail.Domain/JourneysStatus/Rules/JourneyLifecycle.cs`; reject ordinary reopening of Cancelled, Completed, or Terminated states (SR-019, SR-040, FR-003, FR-004, SC-012).
- [ ] T009 Implement journey initialization identity/linkage and source-key idempotency invariants in `src/SmartRail.Domain/JourneysStatus/Rules/JourneyInitializationRules.cs`; distinct authoritative IDs may share the schedule composite, identical repeats are idempotent, and conflicting reuse is rejected (FR-001, SC-001, SC-011).
- [ ] T010 Implement complete diversion stop-set validation in `src/SmartRail.Domain/JourneysStatus/Rules/JourneyDiversionRules.cs`: retained IDs belong to the current journey/revision and occur once; omitted IDs are new; removed IDs remain only in prior revisions (SR-022, FR-010, FR-022, SC-013, SC-017).
- [ ] T011 [P] Add `JourneyInitializationAudit` persistence entity in `src/SmartRail.Domain/JourneysStatus/Entities/JourneyInitializationAudit.cs` recording verified actor when available, source/provenance, linkage, timestamp, and accepted/idempotent/rejected outcome without public projection (FR-001, SC-011).
- [ ] T012 [P] Add framework-neutral journey persistence and transaction ports in `src/SmartRail.Application/JourneysStatus/Persistence/IJourneyRepository.cs` and `IJourneyUnitOfWork.cs` for current reads, append-only revisions, initialization idempotency, audit, and concurrency outcomes (FR-001, FR-004, SC-011, SC-012).
- [ ] T013 [P] Add Feature 001 schedule/station and Feature 002 authorization boundary ports in `src/SmartRail.Application/JourneysStatus/Boundaries/` without importing Infrastructure, EF Core, ASP.NET Core, or other modules' entities (FR-014, FR-015, SC-008).
- [ ] T014 [P] Configure `Journey`, `JourneyRevision`, `JourneyRevisionStop`, and `JourneyInitializationAudit` in `src/SmartRail.Infrastructure/JourneysStatus/Configurations/` with required fields, restrictive FKs, immutable history constraints, unique source key, unique UpdateId, and indexes from `data-model.md` (FR-001, FR-004, FR-021, FR-022, SC-011, SC-016, SC-017).
- [ ] T015 Register Journey DbSets and configurations in `src/SmartRail.Infrastructure/RailwayNetwork/SmartRailDbContext.cs` while preserving the shared context and existing migrations (FR-001, FR-004).
- [ ] T016 Implement PostgreSQL journey repository and atomic revision/audit transaction in `src/SmartRail.Infrastructure/JourneysStatus/Persistence/JourneyRepository.cs`; use current aggregate revision for concurrency and never update/delete prior revision snapshots (FR-004, FR-005, SC-002, SC-012, SC-016).
- [ ] T017 Generate additive EF Core migration named `JourneysStatus` under `src/SmartRail.Infrastructure/Migrations/` using EF-generated migration ID and the shared model snapshot; do not edit Feature 001/002 migrations (FR-001, FR-004).
- [ ] T018 [P] Add domain unit tests for journey identity, identical-source idempotency rules, conflicting identity reuse, separate IDs on equal composites, ordered snapshots, UpdateId/correction invariants, and stop identity rules in `tests/SmartRail.UnitTests/JourneysStatus/` (SC-001, SC-011, SC-016, SC-017).
- [ ] T019 Add real PostgreSQL migration, constraints, append-only revision, audit, rollback, duplicate-source, distinct-composite-identity, and concurrent revision tests in `tests/SmartRail.IntegrationTests/JourneysStatus/Persistence/JourneyPersistenceTests.cs` using only isolated test databases (SC-001, SC-011, SC-012, SC-016).

**Checkpoint**: Domain and schema invariants pass unit tests; additive migration and transactions pass isolated PostgreSQL tests. All user-story implementation depends on T019.

## Phase 3: User Story 1 — Passenger Views a Dated Journey (Priority: P1, MVP)

**Goal**: A passenger can retrieve a known dated journey and its complete safe operational update history, without confusing it with its reusable scheduled service.

**Independent Test**: Seed a controlled dated journey and revisions; anonymous journey/detail and updates reads identify the authoritative journey, preserve schedule linkage and stop order, return complete ascending public history, and show ETA/position as unavailable when unsupported.

### Tests for User Story 1

- [ ] T020 [P] [US1] Add Application tests for journey detail projection, schedule-linkage distinction, ordered stops, no-update versus unavailable outcomes, and ETA/position unknown behavior in `tests/SmartRail.UnitTests/JourneysStatus/GetJourneyHandlerTests.cs` (SR-007, SR-021, SR-035, FR-001, FR-009, FR-016, SC-001, SC-004).
- [ ] T021 [P] [US1] Add update-history tests for ascending complete official revisions, empty history, excluded initialization events, same-journey correction links, and closed safe public fields in `tests/SmartRail.UnitTests/JourneysStatus/GetJourneyUpdatesHandlerTests.cs` (SR-010, SR-031, FR-011, FR-021, SC-003, SC-016).

### Implementation for User Story 1

- [ ] T022 Add `GetJourneyQuery` and `GetJourneyHandler` in `src/SmartRail.Application/JourneysStatus/Queries/` to return one current committed revision and distinguish missing from unavailable source data (SR-007, SR-019, SR-021, SR-035, FR-001, FR-002, FR-009, FR-016).
- [ ] T023 Add `GetJourneyUpdatesQuery` and handler in `src/SmartRail.Application/JourneysStatus/Queries/` to select only official revisions with `UpdateId`, order ascending, include all public operational events without arbitrary caps, and exclude baseline/private initialization audits (SR-010, SR-031, FR-011, FR-021, SC-016).
- [ ] T024 Add explicit journey and update public response projections in `src/SmartRail.Application/JourneysStatus/Projections/`; expose approved source labels, publication time, revisions, closed public change fields, and correction references while excluding actor subject, private evidence, and Driver observation IDs (SR-010, SR-021, SR-035, FR-009, FR-011, FR-021).
- [ ] T025 Implement the anonymous `GET /api/journeys/{id}` endpoint and DTO mapping in `src/SmartRail.Api/Endpoints/JourneysStatus/GetJourneyEndpoint.cs` according to `contracts/journeys-status.openapi.yaml` (SR-007, SR-019, SR-021, SR-035, SR-089, FR-001, FR-002, FR-009, FR-018).
- [ ] T026 Implement the anonymous `GET /api/journeys/{id}/updates` endpoint in `src/SmartRail.Api/Endpoints/JourneysStatus/GetJourneyUpdatesEndpoint.cs` with the approved complete ascending history schema (SR-010, SR-031, SR-089, FR-011, FR-018, FR-021).
- [ ] T027 Map invalid IDs, missing journeys, valid empty history, unavailable publication source, and safe RFC 9457 errors for these reads in `src/SmartRail.Api/Endpoints/JourneysStatus/JourneyReadProblemMapping.cs` (FR-016, SC-010).
- [ ] T028 Register US1 handlers, endpoint metadata, and routes in `src/SmartRail.Api/Endpoints/JourneysStatus/JourneyReadEndpointRegistration.cs` and `src/SmartRail.Api/Program.cs`; preserve anonymous access and all existing Feature 001/002 routes (SR-089, FR-018).
- [ ] T029 Add API integration tests for both read endpoints, public projection redaction, unknown IDs, empty updates, unavailable data, and OpenAPI response shapes in `tests/SmartRail.IntegrationTests/JourneysStatus/Api/JourneyReadApiTests.cs` (SR-007, SR-010, SR-019, SR-021, SR-035, FR-009, FR-016, SC-001, SC-004, SC-016).

**Checkpoint**: User Story 1 is independently testable through anonymous reads with real PostgreSQL-backed journey data.

## Phase 4: User Story 2 — Controller Publishes an Official Operational Change (Priority: P1)

**Goal**: An authorized Controller publishes status, cancellation, timing, platform, or correction changes as immutable and auditable journey revisions.

**Independent Test**: With a controlled authorized Controller and current journey scope, publish valid changes; verify new revision and actor/time audit. Verify disallowed roles/scopes, stale revisions, invalid transitions, and failed observation verification produce no state change.

### Tests for User Story 2

- [ ] T030 [P] [US2] Add lifecycle application tests for every approved transition, delayed/interrupted recovery, cancellation semantics, terminal rejection, linked correction, and stale revision conflict in `tests/SmartRail.UnitTests/JourneysStatus/PublishJourneyStatusHandlerTests.cs` (SR-040, SR-046, FR-003, FR-004, FR-005, FR-006, SC-002, SC-006, SC-012).
- [ ] T031 [P] [US2] Add authorization and side-effect tests for Controller grants, cancellation-specific authorization, revoked/missing/unavailable journey scope, and denied writes in `tests/SmartRail.UnitTests/JourneysStatus/JourneyPublicationAuthorizationTests.cs` (SR-040, SR-046, SR-091, FR-014, SC-006, SC-008).
- [ ] T032 [P] [US2] Add Driver observation boundary tests for malformed, missing/inaccessible, stale/revoked/incompatible, unavailable, and valid current-journey evidence in `tests/SmartRail.UnitTests/JourneysStatus/DriverObservationVerificationTests.cs` (SR-069, FR-008, FR-023, SC-005, SC-014, SC-018).

### Implementation for User Story 2

- [ ] T033 Add official status/cancellation publication command and handler in `src/SmartRail.Application/JourneysStatus/Commands/PublishJourneyStatus/`; derive actor from verified Feature 002 identity, require current Controller action and journey scope, and use `expectedJourneyRevision` (SR-031, SR-034, SR-040, SR-046, SR-091, FR-003, FR-004, FR-005, FR-014, FR-017).
- [ ] T034 Add stop platform/timing publication command and handler in `src/SmartRail.Application/JourneysStatus/Commands/UpdateJourneyStop/`; accept only approved changes for the identified current journey stop and require expected revision and authorization (SR-040, SR-056, FR-005, FR-006, FR-014, FR-018).
- [ ] T035 Add status transition, cancellation, terminal correction, and source/change-field validation in `src/SmartRail.Application/JourneysStatus/Validation/JourneyPublicationValidator.cs`; only an authorized linked correction may correct a terminal state (SR-040, SR-046, FR-003, FR-004, FR-007, FR-021, SC-012).
- [ ] T036 Add verified Driver Operations observation lookup port and result mapping in `src/SmartRail.Application/JourneysStatus/Boundaries/IDriverObservationVerifier.cs` and `DriverObservationVerification.cs`; preserve owner data and map required verification failures to 400/404/409/503 without publishing (SR-069, FR-008, FR-015, FR-023, SC-014, SC-018).
- [ ] T037 Add current Feature 002 journey authorization adapter in `src/SmartRail.Infrastructure/JourneysStatus/Authorization/Feature002JourneyAuthorization.cs`; use current mapped roles and resource scope for each write, fail closed, and do not add/change grants (SR-046, SR-091, FR-014, SC-006, SC-008).
- [ ] T038 Implement PostgreSQL append transaction for official revision, complete stop snapshot, UpdateId, actor/time audit, observation reference, and correction linkage in `src/SmartRail.Infrastructure/JourneysStatus/Persistence/JourneyPublicationRepository.cs`; stale conditional update must roll back and map to 409 (SR-010, SR-040, FR-004, FR-005, FR-021, SC-002, SC-012, SC-016).
- [ ] T039 Implement `PATCH /api/operations/journeys/{id}/status` in `src/SmartRail.Api/Endpoints/JourneysStatus/PublishJourneyStatusEndpoint.cs` using the approved status/diversion-capable request contract and no new route (SR-034, SR-040, SR-046, FR-003, FR-004, FR-018).
- [ ] T040 Implement `PATCH /api/operations/journeys/{id}/stops/{stopId}` in `src/SmartRail.Api/Endpoints/JourneysStatus/UpdateJourneyStopEndpoint.cs` for only platform and operational timing changes (SR-040, SR-056, FR-006, FR-018).
- [ ] T041 Map 400/401/403/404/409/503 outcomes to safe Problem Details for publication endpoints in `src/SmartRail.Api/Endpoints/JourneysStatus/JourneyPublicationProblemMapping.cs`, preserving Feature 002 authentication and authorization behavior (FR-014, FR-016, FR-023, SC-008, SC-018).
- [ ] T042 Register exactly the two approved protected PATCH operations and existing authorization policies in `src/SmartRail.Api/Endpoints/JourneysStatus/JourneyPublicationEndpointRegistration.cs` and `src/SmartRail.Api/Program.cs`; add no admin or observation-review endpoint (FR-018, FR-019).
- [ ] T043 Add PostgreSQL integration tests for atomic publication/audit, actor and timestamp provenance, rollback, lifecycle, correction, observation evidence, denial no-write, and optimistic concurrency yielding one success plus 409 in `tests/SmartRail.IntegrationTests/JourneysStatus/Persistence/JourneyPublicationTests.cs` (SR-040, SR-046, SR-069, FR-004, FR-005, FR-023, SC-002, SC-005, SC-006, SC-012, SC-018).
- [ ] T044 Add API integration tests for authorized/unauthorized status, cancellation, stop updates, all approved error mappings, and no sensitive fields in `tests/SmartRail.IntegrationTests/JourneysStatus/Api/JourneyPublicationApiTests.cs` (SR-040, SR-046, SR-091, FR-014, FR-016, FR-023, SC-006, SC-008, SC-018).

**Checkpoint**: User Story 2 publishes only through existing Controller-authorized operations, and every denied/conflicted/failed-evidence write leaves official state unchanged.

## Phase 5: User Story 3 — Passenger and Station Consumers See a Disruption Consistently (Priority: P1)

**Goal**: Journey and station departure reads reflect the same committed status/diversion revision and station-local departure date.

**Independent Test**: Publish a disruption and diversion, then read journey detail, complete history, and station departures. Verify revision/source/time consistency, overnight local date behavior, stable stop identity, and no invented times.

### Tests for User Story 3

- [ ] T045 [P] [US3] Add station-departure projection unit tests for Asia/Colombo local date, overnight stops with preserved operationDate, diversion-only operational time, missing times, and deterministic ordering in `tests/SmartRail.UnitTests/JourneysStatus/GetStationDeparturesHandlerTests.cs` (SR-054, SR-056, FR-012, FR-020, SC-003, SC-015).
- [ ] T046 [P] [US3] Add diversion application tests for retained/new/removed stop IDs, order, invalid/duplicate/foreign references, and all-or-nothing rejection in `tests/SmartRail.UnitTests/JourneysStatus/PublishJourneyDiversionTests.cs` (SR-022, FR-010, FR-022, SC-013, SC-017).
- [ ] T047 [P] [US3] Add revision consistency tests proving passenger/station projections use one committed journey revision and expose safe source/update time in `tests/SmartRail.UnitTests/JourneysStatus/JourneyProjectionConsistencyTests.cs` (SR-010, SR-022, SR-048, FR-011, FR-012, SC-003).

### Implementation for User Story 3

- [ ] T048 Extend the approved status publication command to apply a complete ordered journey-only diversion snapshot in `src/SmartRail.Application/JourneysStatus/Commands/PublishJourneyStatus/`; leave Feature 001 service, route, and schedule records unchanged (SR-022, FR-010, FR-018, SC-013).
- [ ] T049 Add station departures query/handler in `src/SmartRail.Application/JourneysStatus/Queries/GetStationDepartures/` requiring `date=YYYY-MM-DD`, filtering the station-local scheduled departure date in Asia/Colombo, and retaining operationDate independently (SR-054, SR-056, FR-012, FR-020, SC-015).
- [ ] T050 Implement diversion-only station date fallback using verified operational departure time and exclude calls with neither scheduled nor operational departure time in `src/SmartRail.Application/JourneysStatus/Queries/GetStationDepartures/`; never invent a time or date (SR-021, SR-035, SR-054, FR-009, FR-020, SC-004, SC-015).
- [ ] T051 Add station departure read projection with journeyInstanceId, ambiguity indicator/context, stop identity/order, operationDate, revision, safe source/time, and known scheduled/operational time in `src/SmartRail.Application/JourneysStatus/Projections/StationDepartureProjection.cs` (SR-007, SR-010, SR-054, SR-056, FR-001, FR-011, FR-012, SC-001, SC-003).
- [ ] T052 Implement anonymous `GET /api/stations/{id}/departures?date=YYYY-MM-DD` in `src/SmartRail.Api/Endpoints/JourneysStatus/GetStationDeparturesEndpoint.cs` with the exact approved OpenAPI query contract (SR-054, SR-056, SR-089, FR-018, FR-020).
- [ ] T053 Map invalid date, unknown station, unavailable schedule/operational source, and valid empty departure results in `src/SmartRail.Api/Endpoints/JourneysStatus/StationDepartureProblemMapping.cs` (FR-016, FR-020, SC-015).
- [ ] T054 Extend `JourneyPublicationRepository` in `src/SmartRail.Infrastructure/JourneysStatus/Persistence/JourneyPublicationRepository.cs` to persist complete diversion snapshots transactionally and preserve prior revisions and stop IDs (FR-004, FR-010, FR-022, SC-013, SC-017).
- [ ] T055 Extend journey repository read projections in `src/SmartRail.Infrastructure/JourneysStatus/Persistence/JourneyRepository.cs` to return station departures and current/history reads from consistent committed revisions; add only justified indexes in `src/SmartRail.Infrastructure/JourneysStatus/Configurations/` (SR-010, SR-048, FR-011, FR-012, SC-003).
- [ ] T056 Add real PostgreSQL tests for station-local date filtering, overnight operationDate, diversion timing fallback, unknown times, diversion snapshot/history, stop-ID integrity, and same-revision reads in `tests/SmartRail.IntegrationTests/JourneysStatus/Persistence/JourneyDepartureAndDiversionTests.cs` (SR-007, SR-010, SR-022, SR-054, SR-056, FR-010, FR-020, FR-022, SC-001, SC-003, SC-013, SC-015, SC-017).
- [ ] T057 Add station-departure API tests for required/invalid date, unknown station, empty result, overnight cases, ambiguity, unavailable source, and public metadata redaction in `tests/SmartRail.IntegrationTests/JourneysStatus/Api/StationDeparturesApiTests.cs` (SR-054, SR-056, FR-012, FR-016, FR-020, SC-001, SC-015).
- [ ] T058 Add end-to-end API integration coverage that reads journey detail, updates, and station departures after one publication and asserts identical committed revision/source/time in `tests/SmartRail.IntegrationTests/JourneysStatus/Api/JourneyRevisionConsistencyApiTests.cs` (SR-010, SR-022, SR-048, FR-011, FR-012, SC-003, SC-013).

**Checkpoint**: User Story 3 proves that journey and station views share one committed revision and the approved local-date/overnight rules.

## Phase 6: User Story 4 — Operational Teams Use Journey Status Without Crossing Module Authority (Priority: P2)

**Goal**: Feature 003 consumes owned evidence through explicit boundaries while Driver Operations, Incident Management, and clients retain their responsibilities.

**Independent Test**: With test boundary providers, verify authorized observation references can be recorded as private publication evidence, observations cannot publish state, incident associations remain external/read-only, and unavailable owners fail closed.

### Tests for User Story 4

- [ ] T059 [P] [US4] Add boundary tests for Driver Operations observation ownership, journey association, and unavailable verification in `tests/SmartRail.UnitTests/JourneysStatus/ModuleBoundaryTests.cs` (SR-044, SR-069, FR-015, FR-023, SC-005, SC-014, SC-018).
- [ ] T060 [P] [US4] Add test cases proving incident context cannot mutate incident lifecycle or infer official journey status in `tests/SmartRail.UnitTests/JourneysStatus/IncidentContextBoundaryTests.cs` (SR-044, FR-015, FR-019).

### Implementation for User Story 4

- [ ] T061 Implement Infrastructure adapter for the existing authorized Driver Operations observation read in `src/SmartRail.Infrastructure/JourneysStatus/Boundaries/DriverOperationsObservationVerifier.cs`; do not copy/own observations or add a review endpoint (SR-069, FR-008, FR-015, FR-023).
- [ ] T062 Implement read-only Incident Management affected-journey context adapter in `src/SmartRail.Infrastructure/JourneysStatus/Boundaries/IncidentJourneyContextReader.cs`; do not create incidents or implement their lifecycle (SR-044, FR-015, FR-019).
- [ ] T063 Register owner boundary adapters and safe unavailable outcomes through `src/SmartRail.Infrastructure/JourneysStatus/DependencyInjection.cs` and `src/SmartRail.Api/Program.cs` without circular project references or changes to Features 001/002 (FR-014, FR-015, SC-008).
- [ ] T064 Add integration tests proving Driver observation references remain private and non-authoritative, Incident Management ownership is preserved, and unavailable owner evidence fails closed in `tests/SmartRail.IntegrationTests/JourneysStatus/ModuleBoundaryIntegrationTests.cs` (SR-044, SR-069, FR-015, FR-023, SC-005, SC-008, SC-014, SC-018).

**Checkpoint**: Feature 003 consumes only approved owner interfaces; no driver/incident module functionality is implemented here.

## Phase 7: Final Testing, Regression, and Documentation

**Purpose**: Verify all direct Feature 003 requirements, preserve baseline APIs and security, and record downstream acceptance obligations honestly.

- [ ] T065 Add contract validation tests comparing generated OpenAPI to `specs/003-journeys-status/contracts/journeys-status.openapi.yaml` in `tests/SmartRail.IntegrationTests/JourneysStatus/Api/JourneysStatusOpenApiTests.cs`; assert exactly the five approved operations, schemas, anonymous reads, protected writes, and no extra routes (FR-018, SC-010).
- [ ] T066 Add security regression tests for Feature 001 anonymous railway GET routes and Feature 002 authentication/authorization behavior in `tests/SmartRail.IntegrationTests/JourneysStatus/Api/ExistingFeatureApiRegressionTests.cs`; confirm no contract or access change (SR-089, SR-090, SR-091, FR-014, FR-018, SC-006, SC-008).
- [ ] T067 Add structured-log and error-response redaction tests for tokens, actor subjects, Driver observation IDs, private audit data, SQL/provider exceptions, and connection details in `tests/SmartRail.IntegrationTests/JourneysStatus/Api/JourneyInformationRedactionTests.cs` (FR-016, FR-021, SC-010, SC-018).
- [ ] T068 Add cross-story automated traceability tests/evidence index mapping each direct requirement SR-007, SR-010, SR-019, SR-021, SR-022, SR-025, SR-031, SR-034, SR-035, SR-040, SR-046, and SR-048 and each FR-001–FR-023/SC-001–SC-018 to passing verification in `tests/SmartRail.IntegrationTests/JourneysStatus/RequirementEvidenceTests.cs` (SC-010).
- [ ] T069 Record downstream client and module obligations in `specs/003-journeys-status/quickstart.md`: passenger/station/driver/operations status, freshness, alerts, loading/error/retry/offline/accessibility, and owning-module acceptance remain open until their own specs and tests provide evidence; explicitly keep SR-025/SC-007 alert-delivery acceptance open until the configured in-app channel demonstrates the same UpdateId/revision (SR-001, SR-025, SR-054, SR-056, SR-058, SR-059, SR-066, SR-102, SR-109, SR-114, SR-113, SC-007).
- [ ] T070 Record implementation and local acceptance evidence for cross-module obligations in `specs/003-journeys-status/quickstart.md`, including Feature 001 references (SR-002/003/004/013), Feature 002 authorization (SR-089/090/091), Driver Operations (SR-064/065/069), Incident Management (SR-038/041/042/044), platform observability (SR-115), and explicit train-control exclusion (SR-072); leave owner evidence open where not delivered here (SC-009, SC-010).
- [ ] T071 Run `dotnet restore SmartRail.slnx` and `dotnet build SmartRail.slnx`; record exact results in `specs/003-journeys-status/quickstart.md` (SC-010).
- [ ] T072 Run the complete solution unit and isolated PostgreSQL/API integration suite; record pass/fail/skip counts and test-database target in `specs/003-journeys-status/quickstart.md`; do not mark failed or skipped verification as success (SC-010).
- [ ] T073 Run `git diff --check` and review the complete Feature 003 change set for unintended Feature 001/002 modifications, unapproved routes/grants, secrets, or control/signaling functionality; record outcome in `specs/003-journeys-status/quickstart.md` (FR-018, FR-019, SC-009, SC-010).
- [ ] T074 Record production initialization authority, operator/source-key governance, and audit-retention approval gates in `specs/003-journeys-status/quickstart.md`; verify production initialization remains deny-by-default until those approvals, with local deterministic fixtures isolated to tests (FR-001, SC-011).

**Final checkpoint**: Local Feature 003 implementation is eligible for acceptance only when direct requirement evidence is complete. Downstream client and other-module requirements remain open until the owning work provides evidence; production governance remains an explicit gate. No AWS resource creation or deployment is part of this feature.

## Dependencies and Execution Order

### Phase Dependencies

- Phase 1 setup tasks may run independently except T004 follows the local conventions established by T001.
- Phase 2 blocks all user stories. In particular, T015 follows entity definitions/configuration; T016 follows persistence ports and mappings; T017 follows all model mappings; T019 validates T017/T016 against isolated PostgreSQL.
- US1 starts after T019. Its query, endpoint, and API verification tasks follow required repository ports and handlers.
- US2 starts after T019 and the relevant Feature 002 identity/RBAC contracts. It may proceed independently of US1 implementation, but shares the journey repository; T038 must integrate with T016 and preserve immutable revisions.
- US3 depends on the revision/publication behavior from US2 and the public read projections from US1. T048/T054 require T033/T038; T051/T055 require the foundational model and US1 projections.
- US4 depends on the Application boundary ports from T013 and publication evidence from US2. Owner adapters must not implement the owning modules.
- Phase 7 follows all user stories and complete automated verification.

### User Story Dependencies

- **US1 (P1, MVP)**: After Phase 2; independently delivers passenger journey detail and update history.
- **US2 (P1)**: After Phase 2 and Feature 002 authorization contracts; independently delivers protected official Controller publication.
- **US3 (P1)**: Requires US1 projections and US2 publication/revision behavior to prove consistent station/journey reads.
- **US4 (P2)**: Requires shared boundary ports and publication evidence; consumes but does not implement Driver Operations or Incident Management.

### Critical Task Edges

- T006–T011 precede T014; T012–T013 precede implementing Application handlers.
- T014–T015 precede T016–T017; T017 precedes T019.
- T020–T021 are test specifications for US1 and may be authored in parallel after T019; handlers precede endpoint tasks T025–T026, which precede T029.
- T030–T032 may be authored in parallel after T019; handlers/authorization precede T038–T044.
- T045–T047 may be authored in parallel after US1/US2 prerequisites; diversion persistence/read updates precede T056–T058.
- T059–T060 may be authored in parallel after the required boundary contracts exist; T061–T064 follow those contracts and implementations.
- T065–T074 follow the implementation and earlier acceptance tasks they verify.

### Task-Level Prerequisites

- T001–T003 have no prerequisites; T004 follows T001.
- T005–T007 and T011–T013 follow T001; T008–T010 follow T006–T007.
- T014 follows T006–T011; T015 follows T014; T016 follows T012, T014, and T015; T017 follows T014–T015; T018 follows T005–T010; T019 follows T016–T018.
- T020–T021 follow T019; T022–T024 follow T012, T016, and T019; T025–T026 follow their respective handlers/projections T022–T024; T027 follows T025–T026; T028 follows T025–T027; T029 follows T028.
- T030–T032 follow T019; T033–T036 follow T012–T013, T030–T032; T037 follows T013 and Feature 002 contracts; T038 follows T016, T033–T037; T039–T040 follow T033–T038; T041 follows T039–T040; T042 follows T039–T041; T043 follows T038–T042; T044 follows T042–T043.
- T045–T047 follow T019 and the US1/US2 model and projection contracts; T048 follows T033–T038; T049–T051 follow T012–T013, T022, and T024; T052 follows T049–T051; T053 follows T052; T054 follows T038 and T048; T055 follows T023–T024 and T049–T051; T056 follows T054–T055; T057 follows T052–T056; T058 follows T029, T044, and T057.
- T059–T060 follow T013 and relevant US2 boundary contracts; T061–T062 follow T013 and the respective owner contract; T063 follows T061–T062; T064 follows T063.
- T065–T066 follow T028, T042, T052, and T063; T067 follows T024 and T041; T068 follows all story-level tests T029, T043–T044, T056–T060, and T064–T067; T069–T070 follow implementation evidence collection; T071–T074 follow all applicable preceding verification tasks.

## Parallel Opportunities

- Setup: T001–T003 can run in parallel; T004 follows folder conventions.
- Foundation: T005–T007, T011, and T013 can be developed in separate files; aggregate rules depend on the entity definitions. Mapping and repository work remain sequential where they touch the shared context/snapshot.
- US1: T020 and T021 can run in parallel; the two read query/handler implementations can proceed in separate files after foundation. Endpoint tests follow API registration.
- US2: T030–T032 are independent test files. The two endpoint implementations T039–T040 can proceed in parallel after shared application/persistence contracts, while route registration follows both.
- US3: T045–T047 can run in parallel; application date filtering, diversion snapshot persistence, and API test files are separate after shared revision contracts are stable.
- US4: T059 and T060 can run in parallel; owner adapters T061–T062 can run in parallel after T013; DI integration follows both.
- Do not parallelize writes to `SmartRailDbContext`, its shared model snapshot/migration, `Program.cs`, or a shared repository file without coordinating the edit.

## Independent Test Criteria

- **US1**: A seeded journey is anonymously retrievable by opaque ID with schedule linkage, current status, ordered stops, safe source/time, and unknown ETA where unsupported; full updates are ascending and initialization is excluded.
- **US2**: An authorized Controller publishes a valid update with one atomic revision/audit; unauthorized, stale, terminal-invalid, or unverifiable-observation requests fail with correct status and no official state change.
- **US3**: One approved publication appears at the same revision in journey detail/history and station departures; local-date/overnight/diversion cases follow Asia/Colombo rules without invented times.
- **US4**: Driver observations and incident context remain owned by their modules; approved evidence can be consumed through ports, and unavailable evidence fails closed.

## Implementation Strategy

1. Complete Setup and Foundation, including the additive migration and real PostgreSQL checks.
2. Deliver US1 as the first independently reviewable increment.
3. Deliver US2 protected Controller publication and its authorization/transaction evidence.
4. Deliver US3 station departures, diversions, and consistent revision projections.
5. Deliver US4 owner-boundary integration without taking over other modules.
6. Complete final regression and evidence tasks. Backend completion does not close client- or other-module-owned acceptance criteria.

## Requirement Traceability

| Approved requirement(s) | Implementation tasks | Verification tasks |
|---|---|---|
| SR-007; FR-001–002 | T006–T016, T022–T025, T049–T051 | T018–T021, T029, T056–T057, T068 |
| SR-010, SR-031; FR-011, FR-021 | T007, T023–T024, T038, T051, T055 | T018–T019, T021, T029, T043, T047, T056, T058, T068 |
| SR-019, SR-021, SR-035; FR-009 | T005, T022, T024 | T020, T029, T067–T068 |
| SR-022; FR-010, FR-022 | T010, T048, T054 | T018, T046–T047, T056, T058, T068 |
| SR-025, SR-048; FR-011, FR-013 | T024, T038, T051, T055, T069 | T021, T043, T047, T058, T068–T070 |
| SR-034, SR-040; FR-003–008, FR-017 | T008, T033–T042 | T030–T044, T067–T068 |
| SR-046; SR-091; FR-014 | T013, T031, T033–T042 | T031, T043–T044, T066, T068 |
| SR-054, SR-056; FR-012, FR-020 | T049–T055 | T045, T047, T056–T058, T068 |
| SR-069; FR-008, FR-015, FR-023 | T013, T032, T036, T061 | T032, T043–T044, T059, T064, T068 |
| FR-016, FR-018–019 | T025–T028, T039–T042, T052–T053 | T029, T044, T057, T065–T067, T073 |
| FR-001/004/005/021/022; SC-001/011/012/016/017 | T006–T019, T033–T038, T048, T054 | T018–T019, T030, T043, T046, T056, T068 |
| SC-002–010, SC-013–018 | T020–T070 across the associated stories | T029–T032, T043–T047, T056–T070 |
| SR-001/002/003/004/013/038/041/042/044/054/056/058/059/064/065/066/069/072/089/090/091/102/109/113/114/115 | Existing Feature 001/002 and downstream client/module boundaries; T013, T024, T036–T037, T051–T063, T069–T070 | T059–T070; owning client/module evidence remains open where assigned outside this feature |

## Open Approval Gates and Downstream Obligations

- Production journey-initialization authority, authorized operator identity, source-key governance, and audit-retention/approval remain explicit operational approval gates. Keep production initialization deny-by-default; local test initialization uses deterministic isolated fixtures only.
- Passenger, station-display, driver, and operations client tasks for presentation, status labels, accessible meaning, loading/no-data/error/retry/offline/freshness, refresh/alerts, and session behavior belong to their owning client specifications. Track applicable SR-019, SR-021, SR-025, SR-035, SR-054, SR-056, SR-058, SR-059, SR-066, SR-102, SR-109, and SR-114 as open until those owners provide implementation and automated-test evidence.
- Driver Operations owns assignments and observations; Incident Management owns incident records/lifecycle and affected-journey associations; their requirements remain open until their owning features provide evidence. This feature may only consume approved interfaces.
- No AWS setup/deployment is included. User performs approved AWS work manually after local system verification.
