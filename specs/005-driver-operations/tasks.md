# Tasks: Driver Operations

Source: approved Feature 005 spec/plan/contracts. Seven dependency-oriented phases; no new roles, routes or owner-module behavior. Checkboxes require passing verification. Paths are relative to repository root. [P] denotes independent work only after stated prerequisites.

## Phase 1 — Foundation
- [ ] T001 Verify baseline, constitution, workbook/PDF and existing owner interfaces; record decisions in `specs/005-driver-operations/spec.md` and `research.md` (FR-001–FR-011; technical prerequisite for all outcomes).
- [ ] T002 Define approved API and model in `specs/005-driver-operations/contracts/driver-operations.openapi.yaml` and `data-model.md`; analyze consistency (T001; SC-006).

## Phase 2 — Domain and shared ports
- [ ] T003 Implement assignment lifecycle in `src/SmartRail.Domain/DriverOperations/Entities/DriverJourneyAssignment.cs` (T002; FR-002, FR-011).
- [ ] T004 [P] Implement immutable audit and observation in `src/SmartRail.Domain/DriverOperations/Entities/DriverAssignmentAudit.cs` and `DriverObservation.cs` (T002; FR-005, FR-006, FR-011).
- [ ] T005 Define repository and default-deny maintenance authority in `src/SmartRail.Application/DriverOperations/IDriverOperationsRepository.cs` and `IDriverAssignmentMaintenanceAuthority.cs` (T003–T004; FR-010, FR-011).
- [ ] T006 Verify Domain invariants with `tests/SmartRail.UnitTests/DriverOperations/DriverDomainTests.cs` (T003–T005; SC-003, SC-004).

## Phase 3 — Persistence
- [ ] T007 Map assignment, audit and observation in `src/SmartRail.Infrastructure/DriverOperations/DriverConfigurations.cs`; restrictive owner/revision FKs, active-assignment and request uniqueness, lifecycle constraints (T003–T005; FR-002, FR-006, FR-011).
- [ ] T008 Register DbSets and append-only protection in `src/SmartRail.Infrastructure/RailwayNetwork/SmartRailDbContext.cs` (T007; FR-006, FR-011).
- [ ] T009 Implement atomic repository writes/current reads in `src/SmartRail.Infrastructure/DriverOperations/DriverOperationsRepository.cs` (T008; FR-002, FR-006, FR-010, FR-011).
- [ ] T010 Generate additive `DriverOperations` migration under `src/SmartRail.Infrastructure/Migrations/` using EF-generated ID and shared snapshot; prior migrations unchanged (T008; technical prerequisite FR-006/FR-011).

## Phase 4 — Application and owner integration
- [ ] T011 Implement current own/all assignment use case and genuine owner projection in `src/SmartRail.Application/DriverOperations/DriverOperationsHandler.cs` (T005,T009; FR-001–FR-004, FR-010; SC-001, SC-002).
- [ ] T012 Implement validated observation submit/replay and Controller review in `src/SmartRail.Application/DriverOperations/DriverOperationsHandler.cs` (T011; FR-005–FR-007, FR-009, FR-010; SC-003–SC-005).
- [ ] T013 [P] Implement controlled non-HTTP grant/revoke in `src/SmartRail.Application/DriverOperations/DriverAssignmentMaintenanceHandler.cs`; absent authority makes no writes (T005,T009; FR-011).
- [ ] T014 [P] Implement genuine current assignment evidence in `src/SmartRail.Infrastructure/DriverOperations/PostgreSqlDriverJourneyAssignmentEvidence.cs` (T005,T009; FR-001, FR-002, FR-005, FR-010).
- [ ] T015 Implement genuine stored observation verifier in `src/SmartRail.Infrastructure/DriverOperations/StoredDriverObservationVerifier.cs`; current review grant, same journey, original active assignment and actual captured revision (T012,T014; FR-008, FR-009, FR-010; SC-005).
- [ ] T016 Verify use-case and owner-adapter positive/negative cases in existing `tests/SmartRail.UnitTests/DriverOperations/` and `tests/SmartRail.IntegrationTests/DriverOperations/` (T011–T015; FR-001–FR-011).

## Phase 5 — Three protected HTTP operations
- [ ] T017 Implement transport DTOs and three route handlers in `src/SmartRail.Api/Contracts/DriverOperations/` and `src/SmartRail.Api/Endpoints/DriverOperations/`; reject unknown authority/status fields and sanitize errors (T011–T012; FR-001–FR-010; SC-006).
- [ ] T018 Register handlers/repository/genuine adapters and production default-deny maintenance in `src/SmartRail.Infrastructure/DependencyInjection.cs`; register endpoints once in `src/SmartRail.Api/Program.cs` (T013–T017; FR-008–FR-011).
- [ ] T019 Publish matching OpenAPI metadata without extra operations or grants (T017–T018; SC-006).

## Phase 6 — Automated verification
- [ ] T020 Verify migration/constraints, duplicate active assignment, owner FKs and atomic rollback using isolated PostgreSQL port5433 in `tests/SmartRail.IntegrationTests/DriverOperations/` (T009–T010; FR-006, FR-011).
- [ ] T021 Verify revoke/regrant history, append-only audit/observations and current assignment reads in the same test directory (T020; FR-002, FR-006, FR-011).
- [ ] T022 Verify actual RSA-signed JWT own/all assignment HTTP, cross-driver/role denial and current central revisions/overnight details (T018,T020; FR-001–FR-004, FR-010; SC-001, SC-002).
- [ ] T023 Verify submit/review, invalid input, identical/conflicting/concurrent retry and no official status mutation over HTTP (T022; FR-005–FR-007, FR-009; SC-003–SC-005).
- [ ] T024 Verify current role/assignment revocation, unauthorized no-writes and unavailable authority fail-closed (T023; FR-001, FR-005, FR-010).
- [ ] T025 Verify genuine Feature 003 publication references and missing/foreign/revoked/unavailable evidence failures without public evidence disclosure (T015,T023; FR-008–FR-010; SC-005).
- [ ] T026 Verify exact approved OpenAPI paths/schemas and unchanged Feature001–004 public/private boundaries; update additive route inventory expectations only (T019,T022; FR-009, FR-010; SC-006).

## Phase 7 — Final local verification
- [ ] T027 Restore/build `SmartRail.slnx`; run all unit and isolated real-PostgreSQL integration/regression tests and record actual counts in `specs/005-driver-operations/quickstart.md` (T006,T016,T020–T026; all FR/SC).
- [ ] T028 Audit exact source acceptance-to-test evidence and downstream obligations in `specs/005-driver-operations/quickstart.md` (T027; SR-064/065/066/069/072; SC-001–SC-006).
- [ ] T029 Review diff, architecture, secrets, default-deny authorities, unchanged prior migrations/RBAC and `git diff --check`; commit integrated branch only (T028; FR-009–FR-011).

## Traceability and checkpoints
| Requirement | Implementation | Verification |
|---|---|---|
| FR-001 | T011,T014,T017,T018 | T022,T024 |
| FR-002 | T003,T007,T009,T011,T014 | T021,T022 |
| FR-003 | T011 | T022 |
| FR-004 | T011 | T022 |
| FR-005 | T004,T012,T014,T017 | T006,T023,T024 |
| FR-006 | T004,T007–T010,T012 | T006,T020,T021,T023 |
| FR-007 | T012,T017 | T023,T024 |
| FR-008 | T015,T018 | T025 |
| FR-009 | T012,T015,T017 | T023,T025,T026,T029 |
| FR-010 | T005,T009,T011–T015,T017,T018 | T016,T024–T026 |
| FR-011 | T003–T005,T007–T010,T013,T018 | T006,T016,T020,T021,T029 |
| SC-001 / SR-064,SR-065 | T011,T014,T017 | T022,T024 |
| SC-002 / SR-066 backend | T011 | T022; Flutter refresh OPEN |
| SC-003 / SR-069 | T012,T017 | T023,T024 |
| SC-004 / SR-069 | T009,T012 | T023 |
| SC-005 / SR-069 | T012,T015 | T023,T025 |
| SC-006 / SR-072 | T017–T019 | T026,T029 |

Phase order is 1→2→3→4→5→6→7; marked parallel work never bypasses prerequisites. US1/2/3 HTTP checkpoints require T018 and real isolated fixture setup; no fictitious early production provider. T010 generates only; test fixtures migrate disposable test databases, never development5432. Test-only authorized assignment initialization and JWT issuer live inside integration tests, not production registration.

## Open downstream / operational acceptance
Flutter Driver owns duty refresh after central change (full SR-066), secure credential storage/session expiry (SR-093), loading/no-data/denial/error/retry/no-network/freshness UI (SR-109/SR-114), and complete client E2E evidence. These remain OPEN and need implementation/test tasks when its SDD feature starts. Existing SR-025/SR-044 and Operations/client gates from Features003/004 remain OPEN. Production assignment-maintenance operator/procedure and retention remain unapproved; normal maintenance is DenyAll. Backend implementation does not accept those gates. No AWS provisioning/deployment, development DB migration, public maintenance routes or new grants.