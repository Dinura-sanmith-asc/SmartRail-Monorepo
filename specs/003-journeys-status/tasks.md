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

- [X] T001 [P] Create `src/SmartRail.Domain/JourneysStatus/`, `src/SmartRail.Application/JourneysStatus/`, `src/SmartRail.Infrastructure/JourneysStatus/`, `src/SmartRail.Api/Endpoints/JourneysStatus/`, `tests/SmartRail.UnitTests/JourneysStatus/`, and `tests/SmartRail.IntegrationTests/JourneysStatus/` following existing project conventions (FR-019).
- [X] T002 [P] Add deterministic journey, schedule-linkage, stop, revision, authorization, and explicitly authorized initialization fixture builders under `tests/SmartRail.UnitTests/JourneysStatus/Fixtures/` and `tests/SmartRail.IntegrationTests/JourneysStatus/Fixtures/`; confine authorized initialization authority to isolated test hosts/databases, without production credentials or Development/Production authentication bypasses (SC-001, SC-010, SC-011).
- [X] T003 [P] Add Feature 003 test collection/database isolation configuration in `tests/SmartRail.IntegrationTests/JourneysStatus/` reusing `PostgreSqlTestDatabaseFixture` on the isolated PostgreSQL test service, never development or production databases (SC-010).
- [X] T004 Document local PostgreSQL, migration, test, and no-AWS boundaries in `specs/003-journeys-status/quickstart.md` (FR-019, SC-010).

**Checkpoint**: Folder conventions, deterministic fixtures, and isolated test-database rules are established.

## Phase 2: Foundational Journey Identity and Domain Model

**Purpose**: Implement shared domain invariants, persistence shape, migration, and application ports required by all user stories.

- [X] T005 [P] Add journey lifecycle and safe public-source value types in `src/SmartRail.Domain/JourneysStatus/` matching the approved states and source labels in `specs/003-journeys-status/spec.md` (FR-001, FR-003, FR-021, SC-012).
- [X] T006 [P] Implement the `Journey` aggregate in `src/SmartRail.Domain/JourneysStatus/Entities/Journey.cs` with opaque `JourneyInstanceId`, service/schedule-revision/operation-date/first-departure linkage, current revision, and no uniqueness assumption on the linkage composite (SR-007, FR-001, SC-001, SC-011).
- [X] T007 [P] Implement immutable `JourneyRevision` and ordered `JourneyRevisionStop` snapshots in `src/SmartRail.Domain/JourneysStatus/Entities/` with one optional baseline `UpdateId`, immutable official-publication `UpdateId`, same-journey correction linkage, and stable stop IDs (FR-002, FR-004, FR-021, FR-022, SC-013, SC-016, SC-017).
- [X] T008 Implement the approved journey transition and terminal-correction rules in `src/SmartRail.Domain/JourneysStatus/Rules/JourneyLifecycle.cs`; reject ordinary reopening of Cancelled, Completed, or Terminated states (SR-019, SR-040, FR-003, FR-004, SC-012).
- [X] T009 Implement journey initialization identity/linkage and source-key idempotency invariants in `src/SmartRail.Domain/JourneysStatus/Rules/JourneyInitializationRules.cs`; distinct authoritative IDs may share the schedule composite, identical repeats are idempotent, and conflicting reuse is rejected (FR-001, SC-001, SC-011).
- [X] T010 Implement complete diversion stop-set validation in `src/SmartRail.Domain/JourneysStatus/Rules/JourneyDiversionRules.cs`: retained IDs belong to the current journey/revision and occur once; omitted IDs are new; removed IDs remain only in prior revisions (SR-022, FR-010, FR-022, SC-013, SC-017).
- [X] T011 [P] Add `JourneyInitializationAudit` persistence entity in `src/SmartRail.Domain/JourneysStatus/Entities/JourneyInitializationAudit.cs` recording verified actor when available, source/provenance, linkage, timestamp, and accepted/idempotent/rejected outcome without public projection (FR-001, SC-011).
- [X] T012 [P] Add framework-neutral journey persistence and transaction ports in `src/SmartRail.Application/JourneysStatus/Persistence/IJourneyRepository.cs` and `IJourneyUnitOfWork.cs` for current reads, append-only revisions, initialization idempotency, audit, and concurrency outcomes (FR-001, FR-004, SC-011, SC-012).
- [X] T013 [P] Add Feature 001 schedule/station and Feature 002 authorization boundary ports plus framework-neutral `IIncidentJourneyContextReader` and `IncidentJourneyContextResult` contracts in `src/SmartRail.Application/JourneysStatus/Boundaries/IIncidentJourneyContextReader.cs` and `IncidentJourneyContextResult.cs`; Incident Management owns authorized incident-context reads, and unavailable owner evidence remains distinct from verified no-context. Import no Infrastructure, EF Core, ASP.NET Core, or other modules' entities (SR-044, FR-014, FR-015, SC-008).
- [X] T014 [P] Define `IJourneyInitializationAuthority`, `IJourneyInstanceIdIssuer`, `IJourneyScheduleLinkVerifier`, and `IJourneyInitializationRepository` in `src/SmartRail.Application/JourneysStatus/Initialization/`; accept only verified actor/source evidence and never derive identity from service/date/occurrence (FR-001, SC-011).
- [X] T015 Implement a restricted non-HTTP `InitializeJourneyCommand` and handler in `src/SmartRail.Application/JourneysStatus/Initialization/`; require trusted authority, use `IJourneyScheduleLinkVerifier` to confirm the published service/revision, operation-date completeness, and first-call occurrence, validate manifest provenance, and create no public route or administration endpoint (FR-001, FR-018, SC-011).
- [X] T016 Implement trusted source/manifest validation and `JourneyInstanceIdIssuer` in `src/SmartRail.Infrastructure/JourneysStatus/Initialization/`; validate stable source-system/key and provenance, accept an authoritative ID or issue one stable ID per accepted source key, and reject conflicting reuse for audit (FR-001, SC-011).
- [X] T017 Implement `DenyAllJourneyInitializationAuthority` and register it as the Infrastructure production default in `src/SmartRail.Infrastructure/JourneysStatus/Initialization/` and `src/SmartRail.Infrastructure/DependencyInjection.cs`; do not enable authorized initialization through Development/Production configuration (FR-001, SC-011).
- [X] T018 [P] Configure `Journey`, `JourneyRevision`, `JourneyRevisionStop`, and `JourneyInitializationAudit` in `src/SmartRail.Infrastructure/JourneysStatus/Configurations/` with required fields, restrictive FKs, immutable history constraints, unique source key, unique UpdateId, and indexes from `data-model.md` (FR-001, FR-004, FR-021, FR-022, SC-011, SC-016, SC-017).
- [X] T019 Register Journey DbSets and configurations in `src/SmartRail.Infrastructure/RailwayNetwork/SmartRailDbContext.cs` while preserving the shared context and existing migrations (FR-001, FR-004).
- [X] T020 Implement PostgreSQL journey repository in `src/SmartRail.Infrastructure/JourneysStatus/Persistence/JourneyRepository.cs`; atomically create the journey, baseline revision, full initial stop snapshot, and accepted audit; write conflicting-identity audit only after rollback in a separate transaction; keep replays idempotent, enforce current-revision concurrency, and never update/delete prior revisions (FR-001, FR-004, FR-005, SC-002, SC-011, SC-012, SC-016).
- [X] T021 Generate additive EF Core migration named `JourneysStatus` under `src/SmartRail.Infrastructure/Migrations/` using EF-generated migration ID and the shared model snapshot; do not edit Feature 001/002 migrations (FR-001, FR-004).
- [X] T022 [P] Add domain/Application initialization tests in `tests/SmartRail.UnitTests/JourneysStatus/` for authority denial, authorized test-only invocation, source/manifest validation, ID issuance, same-key replay, conflicting ID/source-key linkage, no writes after denial, and default-deny DI under Development and Production; authorized test authority must exist only in isolated test fixtures (FR-001, SC-011).
- [X] T023 Add real PostgreSQL tests in `tests/SmartRail.IntegrationTests/JourneysStatus/Persistence/JourneyPersistenceTests.cs` for migration/constraints, atomic initial journey+baseline+ordered stops+audit creation, replay idempotency, distinct IDs sharing an occurrence, conflict rejection and separate conflict audit, rollback, append-only history, and concurrent revision writes; use only isolated test databases (SC-001, SC-011, SC-012, SC-016).

**Checkpoint**: Domain and schema invariants pass unit tests; additive migration and transactions pass isolated PostgreSQL tests. All user-story implementation depends on T023.

## Phase 3: User Story 1 â€” Passenger Views a Dated Journey (Priority: P1, MVP)

**Goal**: A passenger can retrieve a known dated journey and its complete safe operational update history, without confusing it with its reusable scheduled service.

**Independent Test**: Seed a controlled dated journey and revisions; anonymous journey/detail and updates reads identify the authoritative journey, preserve schedule linkage and stop order, return complete ascending public history, and show ETA/position as unavailable when unsupported.

### Tests for User Story 1

- [ ] T024 [P] [US1] Add Application tests for journey detail projection, schedule-linkage distinction, ordered stops, no-update versus unavailable outcomes, and ETA/position unknown behavior in `tests/SmartRail.UnitTests/JourneysStatus/GetJourneyHandlerTests.cs` (SR-007, SR-021, SR-035, FR-001, FR-009, FR-016, SC-001, SC-004).
- [ ] T025 [P] [US1] Add update-history tests for ascending complete official revisions, empty history, excluded initialization events, same-journey correction links, and closed safe public fields in `tests/SmartRail.UnitTests/JourneysStatus/GetJourneyUpdatesHandlerTests.cs` (SR-010, SR-031, FR-011, FR-021, SC-003, SC-016).

### Implementation for User Story 1

- [ ] T026 Add `GetJourneyQuery` and `GetJourneyHandler` in `src/SmartRail.Application/JourneysStatus/Queries/` to return one current committed revision and distinguish missing from unavailable source data (SR-007, SR-019, SR-021, SR-035, FR-001, FR-002, FR-009, FR-016).
- [ ] T027 Add `GetJourneyUpdatesQuery` and handler in `src/SmartRail.Application/JourneysStatus/Queries/` to select only official revisions with `UpdateId`, order ascending, include all public operational events without arbitrary caps, and exclude baseline/private initialization audits (SR-010, SR-031, FR-011, FR-021, SC-016).
- [ ] T028 Add explicit journey and update public response projections in `src/SmartRail.Application/JourneysStatus/Projections/`; expose approved source labels, publication time, revisions, immutable UpdateId, closed public change fields, and correction references while excluding actor subject, private evidence, and Driver observation IDs. Ensure cancellation and platform changes are detectable through the existing reads without adding a notification API (SR-010, SR-021, SR-025, SR-035, FR-009, FR-011, FR-013, FR-021, SC-007).
- [ ] T029 Implement the anonymous `GET /api/journeys/{id}` endpoint and DTO mapping in `src/SmartRail.Api/Endpoints/JourneysStatus/GetJourneyEndpoint.cs` according to `contracts/journeys-status.openapi.yaml` (SR-007, SR-019, SR-021, SR-035, SR-089, FR-001, FR-002, FR-009, FR-018).
- [ ] T030 Implement the anonymous `GET /api/journeys/{id}/updates` endpoint in `src/SmartRail.Api/Endpoints/JourneysStatus/GetJourneyUpdatesEndpoint.cs` with the approved complete ascending history schema and detectable cancellation/platform public change fields (SR-010, SR-025, SR-031, SR-089, FR-011, FR-013, FR-018, FR-021).
- [ ] T031 Map invalid IDs, missing journeys, valid empty history, unavailable publication source, and safe RFC 9457 errors for these reads in `src/SmartRail.Api/Endpoints/JourneysStatus/JourneyReadProblemMapping.cs` (FR-016, SC-010).
- [ ] T032 Register US1 handlers, endpoint metadata, and routes in `src/SmartRail.Api/Endpoints/JourneysStatus/JourneyReadEndpointRegistration.cs` and `src/SmartRail.Api/Program.cs`; preserve anonymous access and all existing Feature 001/002 routes (SR-089, FR-018).
- [ ] T033 Add API integration tests for both read endpoints, public projection redaction, unknown IDs, empty updates, unavailable data, and OpenAPI response shapes in `tests/SmartRail.IntegrationTests/JourneysStatus/Api/JourneyReadApiTests.cs` (SR-007, SR-010, SR-019, SR-021, SR-035, FR-009, FR-016, SC-001, SC-004, SC-016).

**Checkpoint**: User Story 1 is independently testable through anonymous reads with real PostgreSQL-backed journey data.

## Phase 4: User Story 2 â€” Controller Publishes an Official Operational Change (Priority: P1)

**Goal**: An authorized Controller publishes status, cancellation, timing, platform, or correction changes as immutable and auditable journey revisions.

**Independent Test**: With a controlled authorized Controller and current journey scope, publish valid changes; verify new revision and actor/time audit. Verify disallowed roles/scopes, stale revisions, invalid transitions, and failed observation verification produce no state change.

### Shared Application Contract for User Story 2

- [ ] T034 [US2] Define the verified Driver Operations observation lookup port and result mapping in `src/SmartRail.Application/JourneysStatus/Boundaries/IDriverObservationVerifier.cs` and `DriverObservationVerification.cs`; preserve owner data and map required verification failures to 400/404/409/503 without publishing (SR-069, FR-008, FR-015, FR-023, SC-014, SC-018).

### Tests for User Story 2

- [ ] T035 [P] [US2] Add lifecycle application tests for every approved transition, delayed/interrupted recovery, cancellation semantics, terminal rejection, linked correction, and stale revision conflict in `tests/SmartRail.UnitTests/JourneysStatus/PublishJourneyStatusHandlerTests.cs` (SR-040, SR-046, FR-003, FR-004, FR-005, FR-006, SC-002, SC-006, SC-012).
- [ ] T036 [P] [US2] Add authorization and side-effect tests for Controller grants, cancellation-specific authorization, revoked/missing/unavailable journey scope, and denied writes in `tests/SmartRail.UnitTests/JourneysStatus/JourneyPublicationAuthorizationTests.cs` (SR-040, SR-046, SR-091, FR-014, SC-006, SC-008).
- [ ] T037 [P] [US2] Add Driver observation boundary tests using a deterministic test-only verifier for malformed, missing/inaccessible, stale/revoked/incompatible, unavailable, and valid current-journey evidence in `tests/SmartRail.UnitTests/JourneysStatus/DriverObservationVerificationTests.cs`; do not depend on a production Driver Operations adapter (SR-069, FR-008, FR-023, SC-005, SC-014, SC-018).

### Implementation for User Story 2

- [ ] T038 Add official status/cancellation publication command and handler in `src/SmartRail.Application/JourneysStatus/Commands/PublishJourneyStatus/`; derive actor from verified Feature 002 identity, require current Controller action and journey scope, and use `expectedJourneyRevision` (SR-031, SR-034, SR-040, SR-046, SR-091, FR-003, FR-004, FR-005, FR-014, FR-017).
- [ ] T039 Add stop platform/timing publication command and handler in `src/SmartRail.Application/JourneysStatus/Commands/UpdateJourneyStop/`; accept only approved changes for the identified current journey stop and require expected revision and authorization (SR-040, SR-056, FR-005, FR-006, FR-014, FR-018).
- [ ] T040 Add status transition, cancellation, terminal correction, and source/change-field validation in `src/SmartRail.Application/JourneysStatus/Validation/JourneyPublicationValidator.cs`; only an authorized linked correction may correct a terminal state (SR-040, SR-046, FR-003, FR-004, FR-007, FR-021, SC-012).
- [ ] T041 Add current Feature 002 journey authorization adapter in `src/SmartRail.Infrastructure/JourneysStatus/Authorization/Feature002JourneyAuthorization.cs`; use current mapped roles and resource scope for each write, fail closed, and do not add/change grants (SR-046, SR-091, FR-014, SC-006, SC-008).
- [ ] T042 Implement PostgreSQL append transaction for official revision, complete stop snapshot, UpdateId, verified actor/publication-time audit, observation reference, and correction linkage in `src/SmartRail.Infrastructure/JourneysStatus/Persistence/JourneyPublicationRepository.cs`; stale conditional update must roll back and map to 409 (SR-010, SR-040, FR-004, FR-005, FR-007, FR-021, SC-002, SC-012, SC-016).
- [ ] T043 Implement `PATCH /api/operations/journeys/{id}/status` in `src/SmartRail.Api/Endpoints/JourneysStatus/PublishJourneyStatusEndpoint.cs` using the approved status/diversion-capable request contract and no new route (SR-034, SR-040, SR-046, FR-003, FR-004, FR-018).
- [ ] T044 Implement `PATCH /api/operations/journeys/{id}/stops/{stopId}` in `src/SmartRail.Api/Endpoints/JourneysStatus/UpdateJourneyStopEndpoint.cs` for only platform and operational timing changes (SR-040, SR-056, FR-006, FR-018).
- [ ] T045 Map 400/401/403/404/409/503 outcomes to safe Problem Details for publication endpoints in `src/SmartRail.Api/Endpoints/JourneysStatus/JourneyPublicationProblemMapping.cs`, preserving Feature 002 authentication and authorization behavior (FR-014, FR-016, FR-023, SC-008, SC-018).
- [ ] T046 Register exactly the two approved protected PATCH operations and existing authorization policies in `src/SmartRail.Api/Endpoints/JourneysStatus/JourneyPublicationEndpointRegistration.cs` and `src/SmartRail.Api/Program.cs`; add no admin or observation-review endpoint (FR-018, FR-019).
- [ ] T047 Add PostgreSQL integration tests for atomic publication/audit, verified actor and timestamp provenance, rollback, lifecycle, correction, observation evidence, denial no-write, and optimistic concurrency yielding one success plus 409 in `tests/SmartRail.IntegrationTests/JourneysStatus/Persistence/JourneyPublicationTests.cs`; supply any owner evidence through deterministic test-only doubles, not US4 production adapters (SR-040, SR-046, SR-069, FR-004, FR-005, FR-007, FR-023, SC-002, SC-005, SC-006, SC-012, SC-018).
- [ ] T048 Add API integration tests for authorized/unauthorized status, cancellation, stop updates, all approved error mappings, and no sensitive fields in `tests/SmartRail.IntegrationTests/JourneysStatus/Api/JourneyPublicationApiTests.cs`; keep the host's owner verifier explicitly test-only and independent of US4 adapters (SR-040, SR-046, SR-091, FR-014, FR-016, FR-023, SC-006, SC-008, SC-018).

**Checkpoint**: User Story 2 publishes only through existing Controller-authorized operations, and every denied/conflicted/failed-evidence write leaves official state unchanged.

## Phase 5: User Story 3 â€” Passenger and Station Consumers See a Disruption Consistently (Priority: P1)

**Goal**: Journey and station departure reads reflect the same committed status/diversion revision and station-local departure date.

**Independent Test**: Publish a disruption and diversion, then read journey detail, complete history, and station departures. Verify revision/source/time consistency, overnight local date behavior, stable stop identity, and no invented times.

### Tests for User Story 3

- [ ] T049 [P] [US3] Add station-departure projection unit tests for Asia/Colombo local date, overnight stops with preserved operationDate, diversion-only operational time, missing times, and deterministic ordering in `tests/SmartRail.UnitTests/JourneysStatus/GetStationDeparturesHandlerTests.cs` (SR-054, SR-056, FR-012, FR-020, SC-003, SC-015).
- [ ] T050 [P] [US3] Add diversion application tests for retained/new/removed stop IDs, order, invalid/duplicate/foreign references, and all-or-nothing rejection in `tests/SmartRail.UnitTests/JourneysStatus/PublishJourneyDiversionTests.cs` (SR-022, FR-010, FR-022, SC-013, SC-017).
- [ ] T051 [P] [US3] Add revision consistency tests proving passenger/station projections use one committed journey revision and expose safe source/update time in `tests/SmartRail.UnitTests/JourneysStatus/JourneyProjectionConsistencyTests.cs` (SR-010, SR-022, SR-048, FR-011, FR-012, SC-003).

### Implementation for User Story 3

- [ ] T052 Extend the approved status publication command to apply a complete ordered journey-only diversion snapshot in `src/SmartRail.Application/JourneysStatus/Commands/PublishJourneyStatus/`; leave Feature 001 service, route, and schedule records unchanged (SR-022, FR-010, FR-018, SC-013).
- [ ] T053 Add station departures query/handler in `src/SmartRail.Application/JourneysStatus/Queries/GetStationDepartures/` requiring `date=YYYY-MM-DD`, filtering the station-local scheduled departure date in Asia/Colombo, and retaining operationDate independently (SR-054, SR-056, FR-012, FR-020, SC-015).
- [ ] T054 Implement diversion-only station date fallback using verified operational departure time and exclude calls with neither scheduled nor operational departure time in `src/SmartRail.Application/JourneysStatus/Queries/GetStationDepartures/`; never invent a time or date (SR-021, SR-035, SR-054, FR-009, FR-020, SC-004, SC-015).
- [ ] T055 Add station departure read projection with journeyInstanceId, ambiguity indicator/context, stop identity/order, operationDate, revision, safe source/time, and known scheduled/operational time in `src/SmartRail.Application/JourneysStatus/Projections/StationDepartureProjection.cs` (SR-007, SR-010, SR-054, SR-056, FR-001, FR-011, FR-012, SC-001, SC-003).
- [ ] T056 Implement anonymous `GET /api/stations/{id}/departures?date=YYYY-MM-DD` in `src/SmartRail.Api/Endpoints/JourneysStatus/GetStationDeparturesEndpoint.cs` with the exact approved OpenAPI query contract (SR-054, SR-056, SR-089, FR-018, FR-020).
- [ ] T057 Map invalid date, unknown station, unavailable schedule/operational source, and valid empty departure results in `src/SmartRail.Api/Endpoints/JourneysStatus/StationDepartureProblemMapping.cs` (FR-016, FR-020, SC-015).
- [ ] T058 Extend `JourneyPublicationRepository` in `src/SmartRail.Infrastructure/JourneysStatus/Persistence/JourneyPublicationRepository.cs` to persist complete diversion snapshots transactionally and preserve prior revisions and stop IDs (FR-004, FR-010, FR-022, SC-013, SC-017).
- [ ] T059 Extend journey repository read projections in `src/SmartRail.Infrastructure/JourneysStatus/Persistence/JourneyRepository.cs` to return station departures and current/history reads from consistent committed revisions; add only justified indexes in `src/SmartRail.Infrastructure/JourneysStatus/Configurations/` (SR-010, SR-048, FR-011, FR-012, SC-003).
- [ ] T060 Add real PostgreSQL tests for station-local date filtering, overnight operationDate, diversion timing fallback, unknown times, diversion snapshot/history, stop-ID integrity, and same-revision reads in `tests/SmartRail.IntegrationTests/JourneysStatus/Persistence/JourneyDepartureAndDiversionTests.cs` (SR-007, SR-010, SR-022, SR-054, SR-056, FR-010, FR-020, FR-022, SC-001, SC-003, SC-013, SC-015, SC-017).
- [ ] T061 Add API integration coverage in `tests/SmartRail.IntegrationTests/JourneysStatus/Api/JourneySelectionFlowTests.cs` for the unchanged Feature 001 service-search endpoint → Feature 003 station departure choices → selected `journeyInstanceId` → journey detail flow; cover multiple journeys for one service/date, distinct IDs sharing one planned occurrence, passenger-safe descriptors, ambiguous choices with no automatic selection, required/invalid date, overnight departure-date behavior, empty/missing/unavailable results, and public metadata redaction without modifying Feature 001 contracts (SR-007, SR-013, SR-054, SR-056, FR-001, FR-020, SC-001, SC-011, SC-015).
- [ ] T062 Add end-to-end API integration coverage for separate cancellation and platform publications, reading journey detail, complete updates, and station departures after each change; assert change type, UpdateId, committed revision, safe source, and publication timestamp in update-history responses. Compare journey-detail and station-departure responses using only the status, revision, source, and timestamp fields exposed by each schema; do not require UpdateId or change type on projections that do not define them. These backend tests do not claim passenger alert delivery or close client acceptance (SR-010, SR-022, SR-025, SR-048, FR-011, FR-012, FR-013, SC-003, SC-007, SC-013).

**Checkpoint**: User Story 3 proves that journey and station views share one committed revision and the approved local-date/overnight rules.

## Phase 6: User Story 4 â€” Operational Teams Use Journey Status Without Crossing Module Authority (Priority: P2)

**Goal**: Feature 003 consumes owned evidence through explicit boundaries while Driver Operations, Incident Management, and clients retain their responsibilities.

**Independent Test**: With test boundary providers, verify authorized observation references can be recorded as private publication evidence, observations cannot publish state, incident associations remain external/read-only, and unavailable owners fail closed.

### Tests for User Story 4

- [ ] T063 [P] [US4] Add boundary tests for Driver Operations observation ownership, journey association, and unavailable verification in `tests/SmartRail.UnitTests/JourneysStatus/ModuleBoundaryTests.cs` (SR-044, SR-069, FR-015, FR-023, SC-005, SC-014, SC-018).
- [ ] T064 [P] [US4] Add test cases proving the Application incident-context port preserves Incident Management ownership, distinguishes unavailable from verified no-context, exposes no incident fields in public journey projections, and cannot mutate incident lifecycle or infer official journey status in `tests/SmartRail.UnitTests/JourneysStatus/IncidentContextBoundaryTests.cs` (SR-044, FR-015, FR-019).

### Implementation for User Story 4

- [ ] T065 Implement the Driver Operations adapter only when a genuine owner provider exists in `src/SmartRail.Infrastructure/JourneysStatus/Boundaries/DriverOperationsObservationVerifier.cs`; otherwise register an unavailable verifier that returns the approved fail-closed result. Never fabricate production observations, own/copy owner data, or add a review endpoint (SR-069, FR-008, FR-015, FR-023).
- [ ] T066 Integrate `IIncidentJourneyContextReader` with Incident Management only when a genuine owner provider exists in `src/SmartRail.Infrastructure/JourneysStatus/Boundaries/IncidentJourneyContextReader.cs`; until then register/retain the unavailable result. Do not fabricate a provider, assume unfinished owner-module readiness, create incidents, implement incident lifecycle, or expose incident records through the public JourneySnapshot. The operations client composes authorized incident context with official journey data (SR-044, FR-015, FR-019).
- [ ] T067 Register genuine owner providers when available and unavailable fail-closed defaults in `src/SmartRail.Infrastructure/JourneysStatus/DependencyInjection.cs`; keep deterministic authorized/denied/unavailable doubles exclusively in test projects and do not change Feature 001/002 registrations or APIs (FR-014, FR-015, SC-008).
- [ ] T068 Add integration tests proving real owner-provider absence returns unavailable, test-only doubles cannot activate outside test hosts, Driver observations remain private/non-authoritative, and incident ownership/read boundaries are preserved in `tests/SmartRail.IntegrationTests/JourneysStatus/ModuleBoundaryIntegrationTests.cs`; leave SR-044 operations-client composition acceptance open until Incident Management and the client provide evidence (SR-044, SR-069, FR-015, FR-023, SC-005, SC-008, SC-014, SC-018).

**Checkpoint**: Feature 003 consumes only approved owner interfaces; no driver/incident module functionality is implemented here.

## Phase 7: Final Testing, Regression, and Documentation

**Purpose**: Verify all direct Feature 003 requirements, preserve baseline APIs and security, and record downstream acceptance obligations honestly.

- [ ] T069 Add contract validation tests comparing generated OpenAPI to `specs/003-journeys-status/contracts/journeys-status.openapi.yaml` in `tests/SmartRail.IntegrationTests/JourneysStatus/Api/JourneysStatusOpenApiTests.cs`; assert exactly the five approved operations, schemas, anonymous reads, protected writes, and no extra routes (FR-018, SC-010).
- [ ] T070 Add security regression tests for Feature 001 anonymous railway GET routes and Feature 002 authentication/authorization behavior in `tests/SmartRail.IntegrationTests/JourneysStatus/Api/ExistingFeatureApiRegressionTests.cs`; confirm no contract or access change (SR-089, SR-090, SR-091, FR-014, FR-018, SC-006, SC-008).
- [ ] T071 Add structured-log and error-response redaction tests for tokens, actor subjects, Driver observation IDs, private audit data, SQL/provider exceptions, and connection details in `tests/SmartRail.IntegrationTests/JourneysStatus/Api/JourneyInformationRedactionTests.cs` (FR-016, FR-021, SC-010, SC-018).
- [ ] T072 Audit source acceptance checks against implementation and automated tests and record a requirement-to-test evidence matrix in `specs/003-journeys-status/quickstart.md` for SR-007, SR-010, SR-019, SR-021, SR-022, SR-025, SR-031, SR-034, SR-035, SR-040, SR-044, SR-046, SR-048, SR-054, and SR-056 and FR-001–FR-023/SC-001–SC-018. Record FR-013/SR-025/SC-007 backend change-evidence checks separately from passenger-client alert delivery, and SR-044 backend boundary evidence separately from owner/client composition; neither downstream obligation is closed by this audit (SC-010).
- [ ] T073 Record downstream client and module obligations in `specs/003-journeys-status/quickstart.md`: passenger-client tasks/tests must prove configured in-app cancellation/platform alerts and visible status use the same UpdateId/revision (FR-013, SR-025, SC-007); Incident Management and operations-client tasks/tests must prove authorized incident context is composed with official journey data (SR-044). Keep both acceptance items open until evidence passes; keep SR-058 station-display refresh open until its owning client is implemented and tested (SR-001, SR-025, SR-044, SR-054, SR-056, SR-058, SR-059, SR-066, SR-102, SR-109, SR-114, SR-113, SC-007).
- [ ] T074 Record implementation and local acceptance evidence for cross-module obligations in `specs/003-journeys-status/quickstart.md`, including Feature 001 references (SR-002/003/004/013), Feature 002 authorization (SR-089/090/091), Driver Operations (SR-064/065/069), Incident Management (SR-038/041/042/044), platform observability (SR-115), and explicit train-control exclusion (SR-072). Record SR-044 as open until the owning module and operations client demonstrate authorized association visibility; leave owner evidence open where not delivered here (SC-009, SC-010).
- [ ] T075 Run `dotnet restore SmartRail.slnx` and `dotnet build SmartRail.slnx`; record exact results in `specs/003-journeys-status/quickstart.md` (SC-010).
- [ ] T076 Run the complete solution unit and isolated PostgreSQL/API integration suite; record pass/fail/skip counts and test-database target in `specs/003-journeys-status/quickstart.md`; do not mark failed or skipped verification as success (SC-010).
- [ ] T077 Run `git diff --check` and review the complete Feature 003 change set for unintended Feature 001/002 modifications, unapproved routes/grants, secrets, or control/signaling functionality; record outcome in `specs/003-journeys-status/quickstart.md` (FR-018, FR-019, SC-009, SC-010).
- [ ] T078 Record production initialization authority, operator/source-key governance, and audit-retention approval gates in `specs/003-journeys-status/quickstart.md`; verify production initialization remains deny-by-default until those approvals, with local deterministic fixtures isolated to tests (FR-001, SC-011).

**Final checkpoint**: Local Feature 003 implementation is eligible for acceptance only when direct requirement evidence is complete. Downstream client and other-module requirements remain open until the owning work provides evidence; production governance remains an explicit gate. No AWS resource creation or deployment is part of this feature.

## Dependencies and Execution Order

### Phase Dependencies

- Phase 1 setup tasks may run independently except T004 follows T001.
- Phase 2 blocks every user story. T014-T017 define initialization ports, restricted command, validation/ID issuance, and production deny-all authority. T018-T019 configure/register persistence; T020 implements atomic initialization and revision transactions; T021 is the additive migration; T022-T023 verify behavior using unit tests and isolated PostgreSQL.
- US1 (T024-T033) starts after T023. Its handlers precede endpoints and API integration tests.
- US2 (T035-T048) starts after T023 and Feature 002 contracts. It is independent of US4 adapters: observation checks use the US2 Application port and test-only doubles; an absent production owner provider remains unavailable/fail-closed.
- US3 (T049-T062) depends on US1 projections and US2 publication/revision behavior. It consumes the existing Feature 001 service-search API unchanged.
- US4 (T063-T068) verifies owner boundaries. Genuine Driver Operations/Incident Management providers are optional production integration prerequisites; until available, Infrastructure returns unavailable and dependent writes fail closed.
- Phase 7 follows the feature implementation and its applicable tests.

### User Story Dependencies

- **US1 (P1, MVP)**: After Phase 2; independently delivers passenger journey detail and complete public update history.
- **US2 (P1)**: After Phase 2 and Feature 002 authorization contracts; does not depend on unfinished US4 adapters.
- **US3 (P1)**: Requires US1 projections and US2 publication/revision behavior for consistent journey/station reads and passenger selection.
- **US4 (P2)**: Requires Application boundary ports; consumes owner evidence only through genuine providers or fails closed.

### Critical Task Edges

- T014 follows T012; T015-T017 follow T014. T018 follows T006-T011; T019 follows T018; T020 follows T012, T015-T016, and T019; T021 follows T018-T019; T022 follows T002, T009, and T014-T017; T023 follows T003 and T020-T022.
- T024-T025 can be authored in parallel after T023; T026-T028 follow T012, T020, and T023; T029-T030 follow their handlers/projections; T031 follows T029-T030; T032 follows T029-T031; T033 follows T032.
- T034 defines the Driver observation verifier and result contract before dependent tests or handlers. T035-T037 follow T023 and T034 and can be authored in parallel; T038-T040 follow those tests and T034; T041 follows Feature 002 contracts; T042 follows T020, T034, and T038-T041; T043-T044 follow T038-T042; T045 follows T043-T044; T046 follows T045; T047 follows T042-T046; T048 follows T043-T047. US2 tests use test-only providers and none depends on US4 tasks T065-T068.
- T049-T051 can be authored in parallel after T023 and relevant US1/US2 contracts; T052-T055 follow the corresponding application contracts; T056 follows T052-T055; T057 follows T056; T058 follows T032-T033 and T052-T057; T059 follows T058 and the journey revision/read contracts; T060 follows T058-T059; T061 follows T029, T033, and T053-T059; T062 follows T030, T043-T044, and T056-T059 so both approved publication operations and all affected reads are available for its cancellation/platform revision assertions.
- T063-T064 use test-only doubles and follow T013; T065-T066 follow their owner contracts and retain unavailable defaults if no genuine provider exists; T067 follows T065-T066; T068 follows T067.
- T069-T078 follow implementation evidence. T073-T074 must record client/module obligations as open until their owners provide evidence.

### Task-Level Prerequisites

- T001-T003 have no prerequisites; T004 follows T001.
- T005-T007 and T011-T014 follow T001; T008-T010 follow T006-T007; T015-T017 follow T014.
- T018 follows T006-T011; T019 follows T018; T020 follows T012, T015-T016, and T019; T021 follows T018-T019; T022 follows T002, T009, and T014-T017; T023 follows T003 and T020-T022.
- T024-T025 follow T023; T026-T028 follow T012, T020, and T023; T029-T030 follow T026-T028; T031 follows T029-T030; T032 follows T029-T031; T033 follows T032.
- T034 follows T013 and the approved Driver Operations read boundary contract; T035-T037 follow T023 and T034; T038-T040 follow T012-T013, T034, and T035-T037; T041 follows T013 and Feature 002 authorization; T042 follows T020, T034, and T038-T041; T043-T044 follow T038-T042; T045 follows T043-T044; T046 follows T045; T047 follows T042-T046; T048 follows T043-T047. T065-T068 are not prerequisites for US2.
- T049-T051 follow T023 and the applicable US1/US2 contracts; T052-T055 follow the corresponding query/projection requirements and T038/T042; T056 follows T052-T055; T057 follows T056; T058 follows T032-T033 and T052-T057; T059 follows T058; T060 follows T058-T059; T061 follows T029, T033, and T053-T059; T062 follows T030, T043-T044, and T056-T059.
- T063-T064 follow T013; T065-T066 follow owner contracts and must keep unavailable behavior when providers do not exist; T067 follows T065-T066; T068 follows T067.
- T069-T070 follow endpoint registration and story tests; T071 follows public/private projection and error mapping; T072 follows all relevant story tests; T073-T074 follow evidence collection; T075-T078 follow their applicable preceding verification tasks.

### Parallel Opportunities

- Setup: T001-T003 can run in parallel; T004 follows T001.
- Foundation: T005-T007, T011, T013, and T014 can be prepared in separate files; command, validation, authority, mapping, shared context, repository, and migration tasks follow their listed dependencies.
- US1: T024 and T025 can run in parallel; handlers/projections can be developed in separate files after foundation.
- US2: T035-T037 are separate tests. Status and stop endpoint implementations may proceed in separate files after shared application/persistence contracts; registration follows both. US2 does not wait for US4 production adapters.
- US3: T049-T051 can run in parallel; station search-flow integration tests follow the relevant endpoints.
- US4: T063 and T064 can run in parallel. T065-T066 may proceed separately only as genuine providers/contracts permit; otherwise their unavailable defaults remain.
- Do not parallelize edits to `SmartRailDbContext`, the shared migration snapshot, `Program.cs`, or a shared repository file without coordinating the edit.

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

| Approved requirement(s) | Implementation tasks | Verification tasks / ownership |
|---|---|---|
| SR-007; FR-001-FR-002 | T006, T009, T014-T020, T026, T029, T055 | T022-T024, T033, T056, T061, T072 |
| SR-010, SR-031; FR-011, FR-021 | T007, T027-T028, T042, T055, T059 | T023, T025, T033, T037, T047, T051, T060, T062, T072 |
| SR-019, SR-021, SR-035; FR-003, FR-009 | T005, T008, T026, T028 | T024, T035, T071-T072 |
| SR-022; FR-010, FR-022 | T010, T052, T058 | T022-T023, T050-T051, T060, T062, T072 |
| SR-025, SR-048; FR-011, FR-013; SC-007 | T028, T030, T042, T055, T059 | T047 verifies persistence of publication and audit evidence. T062 verifies API read consistency: update-history responses carry change type, UpdateId, revision, source, and publication time; journey-detail and station-departure responses are compared using only their schema-exposed status, revision, source, and timestamp fields. T073 tracks passenger-client delivery/presentation; full SR-025/SC-007 acceptance remains open until that client test passes. |
| SR-034, SR-040; FR-003-FR-008, FR-017 | T008, T038-T046 | T035-T048, T071-T072 |
| SR-046, SR-091; FR-014 | T013, T036, T038-T046 | T036, T043-T048, T070, T072 |
| SR-054, SR-056; FR-012, FR-020 | T049-T059 | T049, T051, T056-T062, T072; station-departure data/projection is backend-owned here |
| SR-058 | Backend revision consistency support T055, T059; station display refresh behavior is client-owned | T062 verifies consistent backend reads; client refresh acceptance remains open until the station-display client is implemented and tested (T073) |
| SR-069; FR-008, FR-015, FR-023 | T013, T037, T034, T065 | T037, T047-T048, T063, T068, T072 |
| SR-044; FR-015 | T013 defines the Application owner port; Incident Management owns authorized context; T066 integrates only a genuine provider; operations client composes context with official journey data | T064/T068 verify boundary and unavailable behavior. Full SR-044 acceptance stays open until owner-module and operations-client evidence is recorded by T073-T074. |
| SR-040; FR-007 | T040 validates correction linkage; T042 persists verified actor, publication timestamp, affected journey, revision/evidence, and correction linkage | T047 verifies actor/time audit persistence and atomicity; T072 records source acceptance evidence |
| FR-016, FR-018-FR-019 | T029-T033, T043-T046, T056-T057 | T033, T048, T061, T069-T071, T077 |
| FR-001/FR-004/FR-005/FR-021/FR-022; SC-001/SC-011/SC-012/SC-016/SC-017 | T006-T023, T038-T042, T052, T058 | T018-T023, T035, T047, T050, T060, T072 |
| SC-002-SC-010, SC-013-SC-018 | Story-specific implementation tasks above; client/module evidence is separately owned | T024-T025, T033-T037, T047-T051, T056-T064, T068-T074 |
| Related SR-001/002/003/004/013/038/041/042/044/059/064/065/066/069/072/089/090/091/102/109/113/114/115 | Feature 001/002, owner modules, and clients retain their existing ownership; Feature 003 boundary/support tasks T013, T028, T037-T041, T051-T068, T073-T074 | Existing feature regression T070 and evidence tracking T073-T074; SR-044 and client-owned SR-025/SR-058 acceptance remain open until their evidence exists |

## Open Approval Gates and Downstream Obligations

- Production journey-initialization authority, authorized operator identity, source-key governance, and audit-retention/approval remain explicit operational approval gates. Keep production initialization deny-by-default; local test initialization uses deterministic isolated fixtures only.
- Passenger, station-display, driver, and operations client tasks for presentation, status labels, accessible meaning, loading/no-data/error/retry/offline/freshness, refresh/alerts, and session behavior belong to their owning client specifications. Track applicable SR-019, SR-021, SR-025, SR-035, SR-054, SR-056, SR-058, SR-059, SR-066, SR-102, SR-109, and SR-114 as open until those owners provide implementation and automated-test evidence. Backend support for FR-013 is implemented/tested through T028/T030/T062; it does not close passenger alert acceptance.
- Driver Operations owns assignments and observations. Incident Management owns incident records/lifecycle, affected-journey associations, and authorized context reads; the operations client composes this context with official journey data. SR-044 remains open until owner and client evidence exists. This feature may only consume approved interfaces, and public journey snapshots contain no incident fields.
- No AWS setup/deployment is included. User performs approved AWS work manually after local system verification.
