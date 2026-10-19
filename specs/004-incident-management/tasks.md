# Tasks: SmartRail Feature 004 — Incident Management

**Input**: Design documents in `specs/004-incident-management/`
**Prerequisites**: `spec.md`, `plan.md`, `research.md`, `data-model.md`, `quickstart.md`, and `contracts/`
**Status**: Planning only. Every task is unchecked. No implementation has started through this task list.

Tasks are grouped into the seven approved dependency phases. User story labels follow `spec.md`: US1 Report, US2 View, US3 Lifecycle, US4 Impact Associations, US5 Operations Composition. Paths are repository-relative and reflect the existing six-project solution.

## Phase 1 — Setup and Isolated Test Prerequisites

**Purpose**: Establish deterministic local test fixtures using the existing signed OIDC fixture and disposable PostgreSQL databases on loopback port 5433. Never use development port 5432 or AWS.

- [ ] T001 [P] Add an Incident PostgreSQL xUnit collection/fixture in `tests/SmartRail.IntegrationTests/Incidents/IncidentPostgresCollection.cs`, reusing the existing isolated test-database fixture and enforcing port-5433-only connection validation; technical prerequisite for FR-001–FR-016 and SC-001–SC-009.
- [ ] T002 [P] Add named test-only category codes and deterministic incident builders in `tests/SmartRail.UnitTests/Incidents/IncidentTestData.cs` and `tests/SmartRail.IntegrationTests/Incidents/IncidentTestData.cs`; never seed or enable production categories; technical prerequisite for FR-001 and SC-001.
- [ ] T003 [P] Add the Incident API test host wiring in `tests/SmartRail.IntegrationTests/Incidents/IncidentApiTestHost.cs`, reusing existing real RSA-signed OIDC/JWKS test fixtures and overriding only test-scoped owner/category services; do not add an authentication bypass or production fake; technical prerequisite for FR-011–FR-014 and SC-005/SC-007/SC-008.

**Checkpoint**: The test host can authenticate signed test tokens and create a disposable PostgreSQL database; no application behavior is added yet.

## Phase 2 — Foundational Incident Domain and Persistence

**Purpose**: Build the aggregate, immutable revision model, ports, persistence mapping, and additive schema required before any user-story handler.

- [ ] T004 Define `IncidentState` and the only valid forward transitions (`Reported` → `In Progress` → `Resolved` → `Closed`) in `src/SmartRail.Domain/Incidents/IncidentState.cs`; `Closed` is terminal and no reopen/correction transition exists; FR-005, FR-006, SR-042, SC-002.
- [ ] T005 Implement the `Incident` aggregate and required-field/revision invariants in `src/SmartRail.Domain/Incidents/Entities/Incident.cs`; use opaque server-generated ID, required category code/description, verified reporter subject, server `ReportedAt`, optional separate `OccurredAt`, and positive current revision; FR-001–FR-003, FR-005, SR-041, SR-091, SC-001.
- [ ] T006 Implement one immutable `IncidentRevision` evidence entity in `src/SmartRail.Domain/Incidents/Entities/IncidentRevision.cs`, keyed by incident and revision and carrying verified actor, safe source classification, server timestamp, association delta/snapshot, and prior/new state only for lifecycle transitions; do not create a competing lifecycle-history entity; FR-005–FR-007, SR-042, SR-044, SC-002.
- [ ] T007 Implement stable, historically retained `IncidentAssociation` entities in `src/SmartRail.Domain/Incidents/Entities/IncidentAssociation.cs`; distinguish Station, Service, Journey, and Disruption links; removals close the active interval and re-addition creates a new association ID; FR-007–FR-009, SR-044, SC-004.
- [ ] T008 Implement `IncidentIdempotencyRecord` in `src/SmartRail.Domain/Incidents/Entities/IncidentIdempotencyRecord.cs`; scope keys to verified actor and operation, store canonical payload hash and original outcome, and never store credentials/tokens; do not invent expiry; FR-006, SR-042, SC-002.
- [ ] T009 Add domain tests in `tests/SmartRail.UnitTests/Incidents/IncidentAggregateTests.cs`, `IncidentStateTests.cs`, `IncidentRevisionTests.cs`, and `IncidentAssociationTests.cs` for required data, every approved transition, invalid/reverse/reopen transitions, stable revisions, immutable evidence, and association history; FR-001–FR-009, SC-001/SC-002/SC-004.
- [ ] T010 Define framework-neutral incident repository and transaction ports in `src/SmartRail.Application/Incidents/Persistence/IIncidentRepository.cs` and `IIncidentUnitOfWork.cs`; include query, atomic create/update, expected-revision, association, and idempotency operations without EF Core types; FR-001–FR-009.
- [ ] T011 Define `IIncidentCategoryCatalog` and Feature 001/003 owner-reference verification ports/result types in `src/SmartRail.Application/Incidents/Categories/IIncidentCategoryCatalog.cs` and `src/SmartRail.Application/Incidents/References/IncidentReferenceContracts.cs`; represent verified match, missing/inaccessible, stale/inactive, and unavailable distinctly; FR-001–FR-003, FR-007, FR-014, SC-001/SC-004.
- [ ] T012 Implement a framework-neutral incident access evaluator in `src/SmartRail.Application/Incidents/Authorization/IncidentAccessEvaluator.cs` using current Feature 002 `AuthorizeOperationHandler` and existing action grants; Controller scope is global only after current grant verification, while Station Staff reads/updates require authorization for every associated station and creation requires at least one reported station in current scope; no role/grant changes; FR-003, FR-011, SR-091, SC-005.
- [ ] T013 Add EF Core mappings in `src/SmartRail.Infrastructure/Incidents/Configurations/IncidentConfiguration.cs`, `IncidentRevisionConfiguration.cs`, `IncidentAssociationConfiguration.cs`, and `IncidentIdempotencyRecordConfiguration.cs`; configure required fields, restrictive relationships, append-only revision behavior, expected-revision concurrency, and unique keys; FR-001–FR-008, SC-001/SC-002/SC-004.
- [ ] T014 Add Incident DbSets and repository/category DI registrations to `src/SmartRail.Infrastructure/RailwayNetwork/SmartRailDbContext.cs` and `src/SmartRail.Infrastructure/DependencyInjection.cs`; preserve existing Feature 001–003 registrations and project references; FR-001–FR-016.
- [ ] T015 Configure relational constraints and indexes in `src/SmartRail.Infrastructure/Incidents/Configurations/IncidentConfiguration.cs`, `IncidentRevisionConfiguration.cs`, `IncidentAssociationConfiguration.cs`, and `IncidentIdempotencyRecordConfiguration.cs` for positive revisions, unique `(IncidentId, Revision)`, unique active associations, idempotency key uniqueness `(ActorSubject, Operation, IdempotencyKey)`, and restrictive deletion; FR-005–FR-008, SC-002/SC-004.
- [ ] T016 Generate the additive EF Core migration named `IncidentManagement` as `src/SmartRail.Infrastructure/Migrations/{EF-generated-id}_IncidentManagement.cs` and its designer file, updating the shared `src/SmartRail.Infrastructure/Migrations/SmartRailDbContextModelSnapshot.cs`; use EF-generated migration ID, do not edit prior migrations or apply to development/AWS; FR-001–FR-008.
- [ ] T017 Add real PostgreSQL schema/migration tests in `tests/SmartRail.IntegrationTests/Incidents/IncidentSchemaTests.cs` for migration application, constraints, indexes, restrictive references, append-only evidence, and isolated database teardown; run only through T001's port-5433 fixture; FR-005–FR-008, SC-002/SC-004.

**Checkpoint**: The additive schema applies to an isolated test database; aggregate invariants and persistence constraints are tested. No endpoints are required yet.

## Phase 3 — US1: Incident Creation and Idempotency (P1)

**Goal**: Report a valid incident with verified owner references, server identity/time, controlled category, and atomic idempotent persistence.

**Independent Test**: A signed, currently authorized user can create an incident with an approved test category and verified station/service reference; invalid, unauthorized, unavailable, or conflicting requests create no partial state. Production creation remains disabled until the category catalogue is approved/configured.

- [ ] T018 [P] [US1] Add application/API creation scenario tests in `tests/SmartRail.UnitTests/Incidents/CreateIncidentHandlerTests.cs` and `tests/SmartRail.IntegrationTests/Incidents/CreateIncidentApiTests.cs` for valid report fields, category gate, owner verification, identity attribution, and 400/401/403/404/503 outcomes; FR-001–FR-003, FR-014, SR-041, SC-001.
- [ ] T019 [P] [US1] Implement validated category options and a production category catalogue adapter in `src/SmartRail.Infrastructure/Incidents/Categories/IncidentCategoryCatalog.cs` and `IncidentCategoryCatalogOptions.cs`; no category codes are hard-coded, arbitrary values are rejected, and missing/unconfigured production catalogue returns unavailable; FR-001, FR-014, SR-041, SC-001.
- [ ] T020 [US1] Implement creation request validation in `src/SmartRail.Application/Incidents/UseCases/CreateIncidentValidator.cs`; require description, category code and at least one verified Station or Service association; treat `OccurredAt` as optional and distinct from server `ReportedAt`; reject caller-supplied ID, reporter or actor; FR-001–FR-003, SR-041, SC-001.
- [ ] T021 [US1] Implement `CreateIncidentHandler` in `src/SmartRail.Application/Incidents/UseCases/CreateIncidentHandler.cs`; check current Feature 002 grant/scope, require an in-scope station for Station Staff, verify every supplied owner reference and category, and fail closed on unavailable evidence; FR-001–FR-003, FR-011, SR-041, SR-091, SC-001/SC-005.
- [ ] T022 [US1] Implement transactional PostgreSQL create and POST idempotency persistence in `src/SmartRail.Infrastructure/Incidents/Persistence/IncidentRepository.cs`; atomically insert incident revision 1, initial associations, and original result, with canonical request hashing and no partial state on failure; FR-001–FR-003, FR-006, SC-001/SC-002.
- [ ] T023 [US1] Implement `POST /api/incidents` request/response DTO mapping and route in `src/SmartRail.Api/Endpoints/Incidents/CreateIncidentEndpoint.cs`, `src/SmartRail.Api/Contracts/Incidents/CreateIncidentRequest.cs`, and `src/SmartRail.Api/Contracts/Incidents/IncidentResponse.cs`; require `Idempotency-Key`, authenticated verified subject and existing `CreateIncident` action; do not add routes/grants; FR-001–FR-003, FR-011, SC-001/SC-005.
- [ ] T024 [US1] Map creation outcomes to safe RFC 9457 responses in `src/SmartRail.Api/Endpoints/Incidents/IncidentProblemMapping.cs`; preserve 400/401/403/404/409/503 meanings without raw exceptions, tokens, reporter subjects, or database details; FR-012–FR-014, SR-100, SC-007.
- [ ] T025 [US1] Add PostgreSQL creation transaction tests in `tests/SmartRail.IntegrationTests/Incidents/CreateIncidentPersistenceTests.cs` for server ID/time, actor attribution, required links, category failure, unknown/unavailable references, rollback, and no writes after denied authorization; FR-001–FR-003, SC-001/SC-005.
- [ ] T026 [US1] Add concurrent POST idempotency tests in `tests/SmartRail.IntegrationTests/Incidents/CreateIncidentIdempotencyTests.cs`; identical key/payload creates once and returns original result, conflicting payload under same actor/operation/key returns 409; FR-006, SC-002.
- [ ] T027 [US1] Ensure POST replay rechecks current authentication, active role/grant and required scope before returning any stored protected response in `src/SmartRail.Application/Incidents/UseCases/CreateIncidentHandler.cs` and verify revocation behavior in `tests/SmartRail.IntegrationTests/Incidents/CreateIncidentIdempotencyTests.cs`; FR-003, FR-011, SC-005.
- [ ] T028 [US1] Add the independent creation checkpoint test in `tests/SmartRail.IntegrationTests/Incidents/IncidentCreationCheckpointTests.cs`; verify the successful POST projection and persisted create readback through the repository, invalid/no-category production behavior, and no state change after rejection; document that HTTP list retrieval is verified in US2 and successful production creation awaits category approval; FR-001–FR-003, SR-041, SC-001.

**Checkpoint**: US1 backend behavior is independently testable with a test-only controlled catalogue. Do not claim production category acceptance before approval.

## Phase 4 — US2: Authorized Incident Reads and Filtering (P1)

**Goal**: List incident context through the single approved GET operation with combined filters, current authorization, empty results, and privacy-safe projection.

**Independent Test**: Controller can read incidents under current global grant; Station Staff receive only incidents for which every associated station is in current scope. Combined filters work, no authorized match returns 200 with an empty list, and owner/authorization outages fail safely.

- [ ] T029 [P] [US2] Add list handler and API scenario tests in `tests/SmartRail.UnitTests/Incidents/ListIncidentsHandlerTests.cs` and `tests/SmartRail.IntegrationTests/Incidents/ListIncidentsApiTests.cs` for combined `incidentId`, `stationId`, and `journeyId` filters, access filtering, empty results, and error behavior; FR-004, FR-011, FR-014, SR-038, SC-003/SC-005.
- [ ] T030 [P] [US2] Implement the application list query/handler in `src/SmartRail.Application/Incidents/UseCases/ListIncidentsHandler.cs`; derive caller only from verified principal, apply filters conjunctively, enforce current `ViewIncidents` grant and all-associated-stations scope, and return no unauthorized existence details; FR-004, FR-011, FR-014, SC-005.
- [ ] T031 [US2] Implement pageless authorized list projection and filtering in `src/SmartRail.Infrastructure/Incidents/Persistence/IncidentRepository.cs`; return only authorized candidate data needed by the handler and distinguish valid empty from unavailable persistence; do not invent pagination limits; FR-004, FR-012, FR-014, SR-038.
- [ ] T032 [US2] Implement only `GET /api/incidents` query binding/response mapping in `src/SmartRail.Api/Endpoints/Incidents/ListIncidentsEndpoint.cs` using `src/SmartRail.Api/Contracts/Incidents/IncidentResponse.cs`; support optional UUID filters, `200` empty results, authentication, and the existing `ViewIncidents` grant; no GET-by-ID or other route; FR-004, FR-011, FR-014, SR-038.
- [ ] T033 [US2] Add authorization/projection integration tests in `tests/SmartRail.IntegrationTests/Incidents/ListIncidentsAuthorizationTests.cs` for Controller global access, Station Staff current scope, multi-station all-required behavior, journey links not expanding scope, revoked roles, and no reporter/audit fields; FR-003, FR-004, FR-011, FR-012, SR-091, SC-005/SC-007.
- [ ] T034 [US2] Add filter and availability tests in `tests/SmartRail.IntegrationTests/Incidents/ListIncidentsFilterTests.cs`; verify filters combine, unknown/inaccessible filters produce no unauthorized disclosure, an authorized empty result is 200, and unavailable role/owner/database evidence is safe and fail-closed; FR-004, FR-014, SR-038, SC-003/SC-005.
- [ ] T035 [US2] Test safe response and log redaction for GET in `tests/SmartRail.IntegrationTests/Incidents/IncidentReadPrivacyTests.cs`; assert reporter subject, actor subject, raw claims, internal evidence, credentials, tokens and SQL details are absent; FR-012, SR-100, SC-007.
- [ ] T036 [US2] Add the independent read checkpoint in `tests/SmartRail.IntegrationTests/Incidents/IncidentReadCheckpointTests.cs`; verify list response is suitable for authorized Operations composition while private incident data remains absent from public Feature 003 responses; FR-004, FR-009, SR-038, SR-044, SC-003/SC-004.

**Checkpoint**: US2 incident reads and filters work independently. Operations overview/client composition remains downstream and open.

## Phase 5 — US3: Lifecycle Publication, Revision and Audit (P1)

**Goal**: Apply only the approved forward lifecycle, optimistic concurrency, immutable evidence, and idempotent PATCH semantics.

**Independent Test**: Valid transitions append one immutable revision record with approved actor/time/state evidence; invalid, reverse, terminal, stale, no-op, unauthorized and conflicting requests leave state unchanged.

- [ ] T037 [P] [US3] Add state-transition application tests in `tests/SmartRail.UnitTests/Incidents/UpdateIncidentLifecycleHandlerTests.cs` for all three forward transitions and illegal/reverse/reopen transitions; FR-005–FR-006, SR-042, SC-002.
- [ ] T038 [P] [US3] Implement `UpdateIncidentLifecycleHandler` in `src/SmartRail.Application/Incidents/UseCases/UpdateIncidentHandler.cs`; require current `UpdateIncidentLifecycle` grant, current all-station scope for Station Staff, valid verified actor, expected revision, and one or more actual state/association changes; FR-003, FR-005–FR-006, FR-011, SC-002/SC-005.
- [ ] T039 [US3] Implement atomic expected-revision update and append-only `IncidentRevision` persistence in `src/SmartRail.Infrastructure/Incidents/Persistence/IncidentRepository.cs`; write state, revision, transition evidence and idempotency result in one transaction; FR-005–FR-006, SR-042, SC-002.
- [ ] T040 [US3] Implement `PATCH /api/incidents/{id}` transport DTO and endpoint in `src/SmartRail.Api/Endpoints/Incidents/UpdateIncidentEndpoint.cs` and `src/SmartRail.Api/Contracts/Incidents/UpdateIncidentRequest.cs`; require `Idempotency-Key` and `expectedRevision`; expose no separate lifecycle or correction route; FR-005–FR-006, FR-011, SC-002.
- [ ] T041 [US3] Reject no-op PATCH payloads, illegal transitions and Closed-state changes in `src/SmartRail.Application/Incidents/UseCases/UpdateIncidentHandler.cs`; revision-only, empty-delta or repeated-current-state requests must not increment revision; FR-005–FR-006, SR-042, SC-002.
- [ ] T042 [US3] Enforce append-only revision evidence in `src/SmartRail.Infrastructure/RailwayNetwork/SmartRailDbContext.cs` and `src/SmartRail.Infrastructure/Incidents/Configurations/IncidentRevisionConfiguration.cs`; reject update/delete and require actor, timestamp, revision, source and transition pair consistency; FR-005, FR-012, SC-002/SC-007.
- [ ] T043 [US3] Add PostgreSQL lifecycle and rollback tests in `tests/SmartRail.IntegrationTests/Incidents/IncidentLifecyclePersistenceTests.cs` for each valid transition, evidence persistence, append-only behavior, terminal closure and failed-write rollback; FR-005–FR-006, SC-002.
- [ ] T044 [US3] Add concurrent expected-revision tests in `tests/SmartRail.IntegrationTests/Incidents/IncidentConcurrencyTests.cs`; two updates from one revision permit at most one mutation and return 409 for stale writes without altering evidence; FR-006, SR-042, SC-002.
- [ ] T045 [US3] Add PATCH idempotency race/replay tests in `tests/SmartRail.IntegrationTests/Incidents/UpdateIncidentIdempotencyTests.cs`; identical retries apply once, conflicting key reuse returns 409, and response matches the original committed result; FR-006, SC-002.
- [ ] T046 [US3] Reauthorize PATCH replays before returning stored incident projections in `src/SmartRail.Application/Incidents/UseCases/UpdateIncidentHandler.cs` and verify revoked/missing/unavailable roles and scopes cannot retrieve stored protected results in `tests/SmartRail.IntegrationTests/Incidents/UpdateIncidentIdempotencyTests.cs`; FR-003, FR-011, FR-012, SC-005/SC-007.
- [ ] T047 [US3] Complete Feature 004 endpoint registration, authenticated-route metadata, error mapping and DI registration in `src/SmartRail.Api/Endpoints/Incidents/IncidentEndpointRegistration.cs`, `src/SmartRail.Api/Endpoints/Incidents/IncidentProblemMapping.cs`, and `src/SmartRail.Infrastructure/DependencyInjection.cs`; invoke existing Feature 002 authorization without modifying its files, and map denied/unavailable to 403/503; FR-011, FR-014, SC-005.
- [ ] T048 [US3] Add signed-token PATCH API tests in `tests/SmartRail.IntegrationTests/Incidents/UpdateIncidentApiTests.cs` for 200, 400, 401, 403, 404, 409, 503, no-op rejection, safe Problem Details and no denied state changes; FR-005–FR-006, FR-011–FR-014, SC-002/SC-005/SC-007.

**Checkpoint**: US3 lifecycle, revision, audit and retry behavior is independently testable. Operations dashboard display after closure is not considered complete here.

## Phase 6 — US4: Journey/Disruption Associations and Ownership Boundaries (P1)

**Goal**: Add and remove verified station/service/journey/disruption links atomically while preserving owner authority, history, privacy, and no automatic journey publication.

**Independent Test**: One incident can link distinct active journey instances and a known published disruption; invalid/stale/unavailable owner evidence never persists an unverified association, historical links remain after owner state changes, and incident mutations produce no Feature 003 publication.

- [ ] T049 [P] [US4] Add association validation tests in `tests/SmartRail.UnitTests/Incidents/IncidentAssociationHandlerTests.cs` for duplicate, malformed, foreign, inactive and disruption-evidence combinations; FR-007–FR-009, SC-004/SC-006.
- [ ] T050 [P] [US4] Implement genuine Feature 001 station/service verification adapters in `src/SmartRail.Infrastructure/Incidents/References/Feature001IncidentReferenceVerifier.cs`; use authoritative owner data and distinguish missing from unavailable; no test fake in production registration; FR-001–FR-003, FR-007, FR-014, SC-001/SC-004.
- [ ] T051 [P] [US4] Implement genuine Feature 003 journey/disruption verification adapters in `src/SmartRail.Infrastructure/Incidents/References/Feature003IncidentReferenceVerifier.cs`; verify stable JourneyInstanceId, active state for new journey links, and exact published UpdateId/revision for disruption links; absent owner provider fails closed; FR-007–FR-009, FR-014, SC-004/SC-006.
- [ ] T052 [US4] Extend application PATCH orchestration for atomic association add/remove in `src/SmartRail.Application/Incidents/UseCases/UpdateIncidentHandler.cs`; validate conditional input rules, prohibit disruption-only fields on other kinds, require approved update/revision evidence for disruptions, and require non-empty mutation; FR-007–FR-009, SC-004/SC-006.
- [ ] T053 [US4] Implement association delta persistence in `src/SmartRail.Infrastructure/Incidents/Persistence/IncidentRepository.cs`; atomically increment the incident revision, append association delta evidence, soft-close removed links and create new stable IDs without deleting historical rows; FR-006–FR-009, SC-002/SC-004/SC-006.
- [ ] T054 [US4] Validate conditional `AssociationInput` request schemas and errors in `src/SmartRail.Api/Contracts/Incidents/AssociationInput.cs` and `src/SmartRail.Api/Endpoints/Incidents/IncidentProblemMapping.cs`; require disruption identity/revision together, forbid those fields for non-disruptions, reject duplicate or foreign stop/resource IDs safely; FR-007, FR-014, SC-004.
- [ ] T055 [US4] Add real PostgreSQL association tests in `tests/SmartRail.IntegrationTests/Incidents/IncidentAssociationPersistenceTests.cs` for multiple distinct journeys, known disruption references, duplicate-active uniqueness, atomic add/remove, rollback, and stable historical association IDs; FR-007, SR-044, SC-004.
- [ ] T056 [US4] Add owner evidence failure tests in `tests/SmartRail.IntegrationTests/Incidents/IncidentReferenceVerificationTests.cs` for unknown/inaccessible, stale/inactive, malformed, conflicting, and unavailable station/service/journey/disruption references; assert 404/409/503 mapping and zero unverified writes; FR-001–FR-003, FR-007, FR-014, SC-001/SC-004.
- [ ] T057 [US4] Verify multi-station authorization after creation in `tests/SmartRail.IntegrationTests/Incidents/IncidentMultiStationAuthorizationTests.cs`; Station Staff creation needs at least one currently in-scope reported station, but later read/update requires scope over every active associated station; Controller must have a current active grant; journey links do not expand scope; FR-003, FR-011, SR-091, SC-005.
- [ ] T058 [US4] Test owner-state changes and historical association retention in `tests/SmartRail.IntegrationTests/Incidents/IncidentAssociationHistoryTests.cs`; later journey cancellation/completion or disruption supersession preserves prior links while explicit PATCH removal changes the active set; FR-007–FR-009, SC-004/SC-006.
- [ ] T059 [US4] Prove incident creation, association and lifecycle changes issue no Feature 003 publication in `tests/SmartRail.IntegrationTests/Incidents/NoAutomaticJourneyPublicationTests.cs`; verify cancellation remains a separate authorized Feature 003 operation; FR-008–FR-010, SC-006.
- [ ] T060 [US4] Add independent-boundary tests in `tests/SmartRail.IntegrationTests/Incidents/IncidentBoundaryRegressionTests.cs` for public Feature 003 responses containing no private incident fields, untrusted incident text remaining inert data in API responses, production DI containing no fake owner providers, and simulated Catering API outage not affecting incident create/read/lifecycle; FR-009, FR-013, FR-015, FR-016, SC-004/SC-007/SC-009. Downstream client rendering/security acceptance remains open.

**Checkpoint**: US4 backend association behavior is independently testable. US5 Operations composition remains a downstream integration/client acceptance and is not completed by these tests.

## Phase 7 — Final Verification and Regression

**Purpose**: Prove local Feature 004 implementation, preserve scope boundaries, and record accurate acceptance evidence without claiming downstream acceptance.

- [ ] T061 Validate `specs/004-incident-management/contracts/incident-management.openapi.yaml` against generated API metadata in `tests/SmartRail.IntegrationTests/Incidents/IncidentOpenApiContractTests.cs`; verify OpenAPI 1.0.0, exactly the three approved paths, request/response schemas, security, and 400/401/403/404/409/503 responses; FR-001–FR-014.
- [ ] T062 Run Feature 001, Feature 002 and Feature 003 API/authentication regression tests and add any Feature 004 regression assertions in `tests/SmartRail.IntegrationTests/Incidents/IncidentFeatureRegressionTests.cs`; confirm existing routes, grants and public projections are unchanged; FR-009–FR-011, SC-005–SC-007.
- [ ] T063 Run `dotnet build SmartRail.slnx` and `dotnet test SmartRail.slnx` with real isolated PostgreSQL integration tests and no silently skipped tests; record exact counts and failures in `specs/004-incident-management/quickstart.md`; FR-016, SC-009.
- [ ] T064 Audit each Feature 004 FR and SC against implementation and automated evidence in `specs/004-incident-management/tasks.md`; map FR-001–FR-016 and SC-001–SC-009 accurately, and mark SR-041/042/044 or SC-003/004/008 downstream acceptance open wherever Operations/owner/client evidence is absent.
- [ ] T065 Perform local signed-JWT HTTP smoke tests for `GET /api/incidents`, `POST /api/incidents`, and `PATCH /api/incidents/{id}` using the established API test/manual fixture and isolated data in `tests/SmartRail.IntegrationTests/Incidents/IncidentLocalSmokeTests.cs`; verify safe 200/201/200 paths only after the approved category catalogue is available, plus invalid/denied/conflict/unavailable paths; FR-001–FR-016, SC-001–SC-009.
- [ ] T066 Review the final Feature 004 diff and registered routes in `src/SmartRail.Api/Endpoints/Incidents/IncidentEndpointRegistration.cs` and record the scope review in `specs/004-incident-management/quickstart.md`; confirm no added roles/grants/routes, no Feature 001–003 behavior changes, no AWS configuration, no secrets/log leakage, and additive migration only; FR-009–FR-016, SC-005–SC-009.
- [ ] T067 Record a Feature 004 local GO/NO-GO recommendation and remaining category, owner-provider, Operations/client, and idempotency-retention gates in `specs/004-incident-management/quickstart.md`; do not claim SR-038/SR-044/client acceptance from backend evidence alone; FR-004, FR-009, FR-014, FR-016, SC-003/SC-004/SC-008/SC-009.

## Dependency Order and Parallel Execution

### Phase dependencies

- Phase 1 (T001–T003) has no application dependencies and prepares isolated tests.
- Phase 2 (T004–T017) depends on test setup where tests are run; it blocks all user-story work. T016 follows domain, mappings, DbContext and indexes; T017 follows T016.
- Phase 3 (T018–T028, US1) follows Phase 2; create/idempotency is the first backend increment.
- Phase 4 (T029–T036, US2) follows the common model and depends on US1's persisted incident/read projection.
- Phase 5 (T037–T048, US3) follows the read model and persistence and introduces the shared PATCH operation and lifecycle revision logic.
- Phase 6 (T049–T060, US4) follows US1 and US3 because associations are created on POST and modified through PATCH. Feature 001/003 production adapters remain unavailable/fail closed until genuine providers are registered.
- Phase 7 (T061–T067) follows all applicable backend tasks. Client/Operations acceptance gates may remain open and must be reported as such.

### Critical prerequisites

```text
T001–T003 → T004–T017 → T018–T028 (US1)
                         ├→ T029–T036 (US2)
                         └→ T037–T048 (US3)
T018–T028 + T037–T048 → T049–T060 (US4)
T018–T060 → T061–T067 (final verification)
```

Within each phase, task IDs execute in ascending order; each task depends on earlier tasks in that phase unless it is explicitly marked `[P]`. Tasks marked `[P]` depend on the preceding phase/prerequisite work but not on their same-phase `[P]` peers; the next unmarked task waits for the parallel group to finish. Never begin schema tests before T016; never begin creation persistence before the migration model is complete; never run owner-dependent acceptance with production fakes.

### Parallel opportunities

- T001, T002 and T003 can be prepared in parallel because they touch separate fixture files and do not alter application source.
- T050 and T051 can be implemented in parallel after T011 and T017 because they are independent owner adapters in separate files; both remain fail-closed if the genuine owner provider is unavailable.
- T018/T019, T029/T030, and T037/T038 can be authored in parallel after their prior phase contracts are fixed because each pair uses separate test/application or test/infrastructure files; downstream integration tasks wait for both members of each pair.
- Most persistence/API tasks are sequential because they touch shared repository, DbContext, route registration, or test-host files. Do not parallelize conflicting edits to those files.

## Requirement-to-Task Traceability

| Spec reference | Implementation tasks | Verification tasks |
|---|---|---|
| FR-001 / SR-041 | T005, T010–T011, T019–T023 | T018, T025–T028, T061–T065 |
| FR-002 / SR-041 | T005–T007, T020–T022 | T018, T025, T028 |
| FR-003 / SR-041/SR-091 | T012, T021, T030, T038 | T025, T027, T033–T035, T046, T056–T057 |
| FR-004 / SR-038/SR-044 | T030–T032 | T029, T033–T036; Operations client remains open |
| FR-005 / SR-042 | T004–T006, T038–T042 | T009, T037, T043–T048 |
| FR-006 / SR-042 | T008, T022, T038–T040, T053 | T026–T027, T041, T043–T048, T055 |
| FR-007 / SR-044 | T007, T011, T051–T054 | T049, T055–T058 |
| FR-008 / SR-044/SR-010/SR-048 | T052–T053 | T059–T060, T062 |
| FR-009 / SR-044 | T030–T032, T051–T054 | T036, T058–T062; owner/client composition remains open |
| FR-010 / SR-046 | Preserve existing operation boundary in T023/T032/T040 | T059, T061–T062 |
| FR-011 / SR-089/SR-090/SR-091 | T003, T012, T021, T030, T038, T047 | T018, T025–T028, T033–T035, T046, T048, T057, T062 |
| FR-012 / SR-100/SR-115 | T024, T031–T032, T040 | T033–T035, T048, T060–T062 |
| FR-013 / SR-095 | T020, T023, T032, T040 | T035, T048, T060–T062; client rendering remains open |
| FR-014 / SR-109/SR-114 | T024, T032, T040, T047 | T018, T025, T029, T034, T048, T056, T061–T065; client states remain open |
| FR-015 / SR-113 | No Catering dependency in application/infrastructure composition | T060, T063, T065 |
| FR-016 / SR-001/SR-117 | T001–T003, T014, T016 | T017, T063, T065–T067 |
| SC-001 | T005, T020–T023 | T018, T025–T028, T065 |
| SC-002 | T004, T006, T008, T038–T042 | T009, T037, T043–T048, T055 |
| SC-003 | T030–T032 | T034–T036; Operations dashboard test remains downstream/open |
| SC-004 | T007, T051–T054 | T049, T055–T060; full owner/client acceptance remains open |
| SC-005 | T012, T021, T030, T038, T047 | T018, T025, T033–T035, T046, T048, T056–T057, T062 |
| SC-006 | T052–T053 | T058–T060, T062 |
| SC-007 | T024, T031–T032, T040, T042 | T033–T035, T048, T060–T062; client text rendering remains open |
| SC-008 | T024, T032, T040 | T048, T061–T065; owning client specifications/tests remain open |
| SC-009 | T001–T003, T019 | T060, T063, T065–T067 |

## Downstream Artifacts and Acceptance Gates

These are follow-up obligations for their owning SDD work; they are not Feature 004 implementation tasks and are not satisfied by this backend task list:

- **SR-038 / SC-003 — Operations overview**: the Operations feature must specify and implement the approved `GET /api/operations/overview` using current authorized incident context and service/journey states; acceptance evidence must include authorized and denied reads and closure visible on the next verified overview read.
- **SR-044 / SC-004 — Operations composition**: the Operations feature/client specification must define composition of protected Incident Management context with official Feature 003 journey status. Evidence must show multiple distinct journey impacts and current owner data together, with private incident data absent from public Feature 003 responses.
- **US5 — Compose incident context with official journey status (P1)**: independently verify through the owning Operations implementation that an authorized Operations user sees current incident context alongside the matching official Feature 003 journey state, that closure appears on the next verified overview read, and unauthorized users cannot view the summary. This remains downstream; no Feature 004 task implements `GET /api/operations/overview` or claims US5 acceptance.
- **Client-owned SR-095/SR-109/SR-114 and SC-007/SC-008**: owning Flutter/React feature specifications must include safe text rendering plus loading, no-data, 403 denial, error, retry, no-network and freshness states, with implementation tasks and automated tests.
- **Category gate**: business owner supplies the controlled category-code catalogue before successful production incident creation is enabled; no task may invent values or provide a permissive fallback.
- **Owner integration gate**: genuine Feature 001 station/service and Feature 003 journey/disruption providers must be registered before production association verification; unavailable owners return 503 and persist no unverified link.
- **Retention gate**: no idempotency-key retention or expiry period is specified; preserve retry behavior and obtain policy before adding automated purging.

## Implementation Strategy

Complete Phase 1 and Phase 2 first. Then deliver US1 report/create, US2 authorized reads, US3 lifecycle, and US4 associations in order, with each checkpoint passing its focused unit and real PostgreSQL/API tests. Final verification runs the complete local solution and existing Feature 001–003 regressions. US5 Operations composition and client acceptance remain open until their owning SDD work supplies evidence. Production successful creation remains blocked until the category catalogue is approved; do not mark that gate complete from test-only categories.
