---
description: "Dependency-ordered implementation tasks for the SmartRail Station Information & Departure Board"
---

# Tasks: SmartRail Station Information & Departure Board

**Input**: Design documents from `specs/001-station-information-board/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/](./contracts/), [quickstart.md](./quickstart.md)

**Test tasks**: Included because the approved feature prompt explicitly requires unit, component, integration, browser, accessibility, offline, reconnection and contract-backed tests.

**Execution note**: The repository has no existing `tests/e2e/` directory. The plan explicitly selects root `e2e/` for Playwright acceptance tests; implementation must keep that structure consistent.

## Phase 1: Setup — Test and project tooling

**Purpose**: Add the approved test toolchain without changing existing React/Vite/Tailwind versions.

- [X] T001 Verify the installed React/Vite/TypeScript baseline from `package.json`, `package-lock.json` and `tsconfig.app.json`; retain those versions, add Vitest 5, jsdom, React Testing Library, `@testing-library/dom`, user-event, jest-dom, Playwright Test and axe Playwright integration as development dependencies, add `test:unit`, fixture-only `test:e2e`, and opt-in backend-gated `test:e2e:live` scripts in `package.json`, and update `package-lock.json`; live specs must not run in the default fixture suite.
- [X] T002 [P] Configure Vitest to reuse Vite plugins and use jsdom in `vite.config.ts` (after T001).
- [X] T003 Create `src/test/setup.ts` to load jest-dom matchers and perform per-test DOM cleanup; reference it from `vite.config.ts` (after T002).
- [X] T004 [P] Configure Playwright webServer, test directory and deterministic browser settings in `playwright.config.ts`; use root `e2e/` per the plan (after T001).

---

## Phase 2: Foundational — Shared frontend boundaries

**Purpose**: Provide the minimal shared pieces required by all six stories; no future API schema is assumed.

- [X] T005 [P] Create `src/shared/api/http-client.ts` with a typed fetch wrapper for base URL, JSON parsing, network failure and the checked-in Problem response behavior; add `src/shared/api/http-client.test.ts` for success, malformed response and failure cases.
- [X] T006 [P] Create `src/features/station-display/model/display-model.ts` with internal station context and normalized presentation concepts for fixture departures; keep future backend field names opaque, and keep state transitions in the separate `display-state.ts` module.
- [X] T007 Define `src/features/station-display/api/station-departure-source.ts` as the normalized internal source interface for initial snapshots, revalidation and source-delivered changes (SR-058); both fixture and future approved live adapters MUST implement this same interface. This is an internal frontend boundary, not an approved backend JSON schema or API contract.
- [X] T008 Create `src/features/station-display/api/fixtures/station-display.fixture.ts` with clearly labeled non-live station/departure fixtures for multiple stations, empty data and operational updates; fixture state cannot be labeled live by application composition (after T006 and T007).
- [X] T009 Add `src/features/station-display/api/fixtures/fixture-departure-source.test.ts` proving deterministic update delivery through the T007 source interface and verifying the source identifies its data as fixture/non-live; do not assert a numeric refresh interval (after T008, before T010).
- [X] T010 Implement `src/features/station-display/api/fixtures/fixture-departure-source.ts` as a deterministic adapter of the T007 interface, using T008 fixtures and emitting updates without a production interval; preserve an explicit fixture/non-live origin marker (after T007 through T009).

**Checkpoint**: Foundation allows story work with deterministic fixtures. Fixture output is never evidence of live backend integration.

---

## Phase 3: User Story 1 — Show departures for the configured station (Priority: P1)

**Goal**: Validate one deployment-provided station ID and show its published identity without fallback or passenger selection.

**Independent Test**: Provide valid, missing, malformed, unavailable and unpublished station IDs; verify the published name is shown only for an exact match and invalid cases show configuration/unavailable state.

### Tests for User Story 1

- [X] T011 [P] [US1] Add configuration validation tests in `src/app/config.test.ts` for missing API base URL, missing station ID, malformed/non-UUID station ID and valid public configuration (SR-002; spec US1 scenarios 1 and 4).
- [X] T012 [P] [US1] Add station reference client tests in `src/features/station-display/api/station-reference-client.test.ts` for published list match, no match, API failure and no arbitrary fallback (SR-002; spec US1 scenario 4).
- [X] T013 [US1] Add station header rendering tests in `src/features/station-display/components/station-header.test.tsx` for authoritative station name and configuration/unavailable states (SR-054, SR-061; spec US1 scenario 1).

### Implementation for User Story 1

- [X] T014 [US1] Implement public Vite configuration reading and UUID validation in `src/app/config.ts`; document build-time/public semantics and example variable names without storing secrets (after T011; spec FR-001; Q1).
- [X] T015 [US1] Implement the published station lookup and exact stable-ID match in `src/features/station-display/api/station-reference-client.ts` using only the checked-in `GET /api/stations` contract (SR-002; spec US1 scenarios 1 and 4).
- [X] T016 [US1] Implement the station identity header and invalid/unavailable station state in `src/features/station-display/components/station-header.tsx` and compose startup in the existing `src/App.tsx`; never show a selector or fallback station (after T013 through T015; SR-002, SR-061; spec FR-001/FR-008).

**Checkpoint**: US1 is independently testable with the public station contract or deterministic station fixtures.

---

## Phase 4: User Story 2 — Understand published operational changes (Priority: P1)

**Goal**: Present dated station departure fixtures with separate scheduled and operational meanings; preserve unknown values and visible disruption labels.

**Independent Test**: Given normalized, clearly labeled fixture departures for more than one station, render only the configured station's rows and verify scheduled time, service identity, published status/platform/delay/cancellation and missing-value handling.

### Tests for User Story 2

- [ ] T017 [P] [US2] Add departure row tests in `src/features/station-display/components/departure-row.test.tsx` for scheduled-time labeling, service identity, absent values and text/symbol status alongside color (after T016; SR-054, SR-035; spec US2 scenarios 1 and 5).
- [ ] T018 [P] [US2] Add board and app composition tests in `src/features/station-display/components/departure-board.test.tsx` and `src/App.test.tsx` for configured-station isolation, source order, empty state, no service-search substitution, operational changes on the correct departure, and a persistent visible fixture/demo label whenever fixture data is rendered (after T016; SR-054, SR-056, SR-061, SR-003, SR-004; spec US1 scenarios 2/3).
- [ ] T019 [US2] Add operational update tests in `src/features/station-display/components/operational-change.test.tsx` for platform change, delay, cancellation prominence, planned scheduled-time meaning, source-provided update time and unknown values that are never inferred (after T016; SR-056, SR-010, SR-035; spec US2 scenarios 2 through 5).

### Implementation for User Story 2

- [ ] T020 [US2] Implement operational status/change presentation in `src/features/station-display/components/operational-change.tsx` for source-provided platform, delay and cancellation values; expose update time only when supplied with verified meaning and never derive values (after T019; SR-056, SR-010, SR-035; spec FR-003/FR-004).
- [ ] T021 [US2] Implement `src/features/station-display/components/departure-row.tsx` and compose `OperationalChange` into each affected row so scheduled values retain planned meaning and operational values/unknowns are clearly labeled (after T017 and T020; SR-054, SR-056, SR-035; spec FR-002 through FR-004).
- [ ] T022 [US2] Implement `src/features/station-display/components/departure-board.tsx` using only rows associated with the configured station and source-provided order; provide a no-departures state (after T018 and T021; SR-054; spec US1 scenarios 1 through 3).
- [ ] T023 [US2] Wire the T010 non-live fixture source into `src/App.tsx` for isolated UI development, after station identity (T016) and board composition (T022) are available; always show a persistent visible fixture/demo label whenever fixture data renders (T018 verifies the label; SR-054, SR-061).

### Backend contract gate for User Story 2 — BLOCKED

- [ ] T024 [US2] **BLOCKED — backend approval required**: Update `specs/001-station-information-board/contracts/README.md` to reference the approved contract for `GET /api/stations/{id}/departures` only after the backend owner supplies its response schema, public projection, station/date/time/timezone rules, “upcoming” ordering, operational and unknown-value meanings, timestamps/revisions, and error behavior.
- [ ] T025 [US2] **BLOCKED - T024 and T007 required; backend approval required**: Implement the live station departures adapter in `src/features/station-display/api/station-departure-adapter.ts` against the approved `GET /api/stations/{id}/departures` contract; implement the same T007 source interface as the fixture adapter. Do not add request/response fields or business rules absent from that approved contract.
- [ ] T026 [US2] **BLOCKED - T024 and T025 required**: Add live contract-backed departure acceptance coverage in `e2e/station-live-contract.spec.ts` for selected-station isolation, schedule/status meaning, empty/unknown values and API failures; run it only through opt-in `npm run test:e2e:live` after the backend gate, and never let fixtures pass this task.

**Checkpoint**: Fixture-backed US2 may be demonstrated as fixture data. It is not live SR-054/SR-056 acceptance until the blocked contract tasks pass.

---

## Phase 5: User Story 3 — See changes without manually reloading (Priority: P1)

**Goal**: Automatically present newly available authoritative information while connected, with no arbitrary polling interval or delivery protocol.

**Independent Test**: A deterministic source fixture emits a newer normalized snapshot; the rendered board updates without a passenger action and does not claim a production timing guarantee.

### Tests for User Story 3

- [ ] T027 [P] [US3] Add refresh coordinator tests in `src/features/station-display/hooks/use-departure-updates.test.ts` for initial snapshot, newly available snapshot, source failure and no passenger-triggered action (after T007 and T023; SR-058; spec US3 scenarios 1 through 3).

### Implementation for User Story 3

- [ ] T028 [US3] Implement `src/features/station-display/hooks/use-departure-updates.ts` to consume the T007 source interface and update displayed state without user action; keep cadence and transport in the source adapter rather than inventing polling/push behavior (after T027; SR-058; spec FR-005).

### Live automatic-update integration gate — BLOCKED

- [ ] T029 [US3] **BLOCKED - approved departure/change contract and T025 required**: Connect `src/features/station-display/api/station-departure-adapter.ts` to the backend-approved automatic update mechanism and interval only after those decisions are published; do not select polling versus push in the frontend plan.
- [ ] T030 [US3] **BLOCKED - T029 required**: Add live browser verification in `e2e/station-live-contract.spec.ts`, run only through opt-in `npm run test:e2e:live`, proving a published authoritative backend change reaches the board automatically without manual reload; assert no latency target unless separately approved.
- [ ] T031 [US3] **CONDITIONAL / BLOCKED - T024 required**: Add use of `GET /api/journeys/{id}/updates` in `src/features/station-display/api/station-update-adapter.ts` only if the approved departures contract cannot provide the required change detection; first record that backend decision in `contracts/README.md`.

**Checkpoint**: Fixture auto-update can be verified independently; live SR-058 remains blocked until the backend update contract is approved and tested.

---

## Phase 6: User Story 4 — Recognize disconnected or stale information (Priority: P1)

**Goal**: Preserve the last successful in-session information with an explicit stale indication during connection loss and clear that indication only after authoritative revalidation.

**Independent Test**: Verify disconnect-before-first-load, disconnect-after-success, and reconnect; prior data stays marked stale and only a successful authoritative response clears the warning.

### Tests for User Story 4

- [ ] T032 [P] [US4] Add display state tests in `src/features/station-display/model/display-state.test.ts` for unavailable-without-data, stale-with-last-success, revalidating and recovered transitions (after T023 and T028; SR-059; spec US4 scenarios 1–4).
- [ ] T033 [US4] Add reconnect behavior tests in `src/features/station-display/hooks/use-revalidation.test.ts` proving online notification triggers a source revalidation but does not clear stale state by itself (after T023 and T028; SR-059; spec FR-006/FR-007).
- [ ] T034 [US4] Add deterministic Playwright offline/recovery scenarios in `e2e/station-connectivity.spec.ts` using controlled request interception and/or the deterministic fixture source, not a future backend: simulate disconnect before first success and after successful data retrieval; verify cached/stale indication persists during outage and after failed revalidation; simulate reconnection and verify the stale indication is removed only after successful authoritative revalidation (after T023 and T028; SR-059; quickstart scenario 5). Do not assert a production refresh interval.

### Implementation for User Story 4

- [ ] T035 [US4] Implement the display information state transitions in `src/features/station-display/model/display-state.ts` so last successful data remains in active-session memory and is never represented as current during disconnection (after T032; SR-059; spec FR-006/FR-008).
- [ ] T036 [US4] Implement reconnection-triggered authoritative revalidation in `src/features/station-display/hooks/use-revalidation.ts`; retain the stale warning until the source confirms a successful current response (after T033 and T035; SR-059; spec FR-007).
- [ ] T037 [US4] Implement loading, empty, error, unavailable, disconnected/stale, revalidating and recovered state views in `src/features/station-display/components/display-state-view.tsx`; show last-update only when the approved source meaning is available (after T035 and T036; SR-059, SR-035; spec FR-006–FR-008).

**Checkpoint**: Active-session outage behavior is testable with fixtures. Durable browser storage and expiry/TTL are excluded pending approved policy.

---

## Phase 7: User Story 5 — Read the board at approved display profiles (Priority: P2)

**Goal**: Preserve legibility, essential fields and disruption visibility at 1920 × 1080 and 1366 × 768 landscape, meeting applicable WCAG 2.2 AA criteria.

**Independent Test**: Run the populated, delayed and cancelled board at both browser viewport sizes; verify all required information remains visible and status is understandable without color alone.

### Tests for User Story 5

- [ ] T038 [P] [US5] Add Playwright viewport scenarios in `e2e/station-display-profiles.spec.ts` at 1920 × 1080 and 1366 × 768; assert station, scheduled time, service identity, status and critical disruption are visible at both profiles (after T023 and T037; SR-054, SR-056, SR-061; spec SC-006).
- [ ] T039 [P] [US5] Add real-browser axe checks and text/symbol status assertions in `e2e/station-accessibility.spec.ts` for applicable WCAG 2.2 AA criteria; record any manual contrast checks in `specs/001-station-information-board/quickstart.md` (after T023 and T037; constitution IV/V).

### Implementation for User Story 5

- [ ] T040 [US5] Implement responsive board, row and disruption layout rules in `src/features/station-display/station-display.css` for both approved landscape viewports without adding viewing-distance or hardware claims (after T038 and T039; SR-054, SR-056, SR-061; spec FR-009).
- [ ] T041 [US5] Refine typography, hierarchy, focus/announcement semantics, contrast and text/symbol status treatment in `src/features/station-display/components/departure-row.tsx`, `src/features/station-display/components/operational-change.tsx`, `src/features/station-display/components/display-state-view.tsx` and `src/features/station-display/station-display.css` to satisfy WCAG 2.2 AA; ensure critical disruption is not hidden at 1366 × 768 (after T040; spec FR-009/FR-010/FR-012).
- [ ] T042 [US5] Capture and review browser evidence for both approved viewport profiles in `specs/001-station-information-board/quickstart.md`, including populated, delayed and cancelled states (after T041; SR-061; spec SC-006).

**Checkpoint**: US5 passes only with real-browser evidence at both approved viewports; jsdom layout assertions are insufficient.

---

## Phase 8: User Story 6 — Use a passive passenger-information display (Priority: P2)

**Goal**: Present public railway information without passenger selector, station management, controller, privileged write or kiosk workflows.

**Independent Test**: Inspect the rendered public board and all available interactions; verify no management or terminal actions are exposed.

### Tests for User Story 6

- [ ] T043 [P] [US6] Add Playwright assertions in `e2e/station-passive-display.spec.ts` that the public display exposes no station selector, editing, publishing, controller, authentication or kiosk actions (after T023 and T037; spec US6 scenarios 1–3; constitution frontend constraints).

### Implementation for User Story 6

- [ ] T044 [US6] Keep `src/App.tsx` and `src/features/station-display/components/` limited to passive public information and remove any starter/template interaction that is not part of the approved display scope (after T043; SR-061; spec FR-011).

**Checkpoint**: US6 is complete when the board remains passive across loading, error, stale and populated states.

---

## Phase 9: Polish and Cross-Cutting Verification

**Purpose**: Close requirement traceability, execute available quality gates, and preserve the explicit live-integration stop condition.

- [ ] T045 Record the verification mapping and fixture-versus-live status in `specs/001-station-information-board/quickstart.md` for SR-054, SR-056, SR-058, SR-059, SR-061 and applicable SR-001, SR-002, SR-003, SR-004, SR-010 and SR-035. For SR-001, record Station Display-specific evidence separately from cross-application evidence, identify the SmartRail cross-application integration/release verification workstream as owner (assignment remains with the release owner), and leave full acceptance pending until shared end-to-end verification succeeds; frontend tests alone do not accept SR-001.
- [ ] T046 Run `npm run lint`, `npm run build`, `npm run test:unit` and the fixture-only `npm run test:e2e`; record exact results and any environment limitation in `specs/001-station-information-board/quickstart.md` (after T045). Live tests use the separate opt-in command only after backend approval and do not block these frontend checks.
- [ ] T047 **BLOCKED - T024 through T026, T029 through T030, T034 through T037, T046 and verified backend environment required**: Complete live contract-backed acceptance in `specs/001-station-information-board/quickstart.md` for station departure integration, authoritative operational changes, automatic backend update propagation, stale information during disconnection, and successful authoritative revalidation after reconnection. Record source contract version, outcomes and evidence for SR-054, SR-056, SR-058 and SR-059; fixture-only tests never satisfy live acceptance.

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: T001 precedes T002-T004. T002 precedes T003. No application implementation begins before its test harness/configuration is available.
- **Foundational (Phase 2)**: T005 and T006 can proceed in parallel after setup. T007 follows T006 and defines the shared source interface. T008 follows T006 and T007; T009 tests the fixture source before T010 implements it. T010 implements the shared interface. Fixture-backed story work depends on this foundation, not on live backend approval.
- **User stories**: US1 establishes validated station identity. US2 uses it and T010 to render fixture-backed departures; T020-T023 are ordered after their tests and composed dependencies. US3 consumes the already-established T007 source boundary. US4 adds state/revalidation behavior. US5 verifies/refines the board; US6 verifies the passive surface.
- **Live integration**: T024 is blocked until the backend publishes/approves departure semantics. T025 requires T007 and T024; T026 requires T024 and T025. T029-T030 require the approved update mechanism/interval and T025. T031 is conditional on backend approval that `/api/journeys/{id}/updates` is needed. T047 requires T030 and the live departure, operational, stale/revalidation evidence plus a verified environment; fixture evidence cannot satisfy it.
- **Quickstart edits**: T042 precedes T045; T045 precedes T046; T046 precedes T047. These tasks all edit `quickstart.md` and must run sequentially.

### User Story Dependencies

- **US1 (P1)**: Starts after setup/foundation. Its station configuration and identity flow is independently testable against the existing public station contract.
- **US2 (P1)**: Uses US1's station context and the fixture source from T007-T010. Fixture-backed board behavior is independently testable; live departure integration is blocked by the future endpoint contract.
- **US3 (P1)**: Uses the T007 source boundary and board from US2. Fixture update arrival is independently testable; live update propagation is blocked by backend delivery/revision decisions.
- **US4 (P1)**: Uses US2/US3 data states. Deterministic fixture-driven offline and reconnect behavior is independently testable; live freshness validity remains contract-gated.
- **US5 (P2)**: Uses the populated board from US2 and state treatments from US4. Browser viewport and accessibility verification can proceed with labeled fixtures.
- **US6 (P2)**: Can verify the integrated passive surface after US1/US2 composition; it adds no management or kiosk functionality.

### Parallel Opportunities

- T002 and T004 can run in parallel after T001 because they configure separate files.
- T005 and T006 can run in parallel; T007 follows T006, then T008-T010 run in their stated dependency order.
- Within US1, T011 and T012 are independent test files; T013 tests the header, then T014-T016 follow their corresponding tests.
- Within US2, T017 and T018 are independent test files after T016; T019 covers operational rendering. T020-T023 must follow their relevant tests and component/source dependencies.
- US5 profile and accessibility test files T038 and T039 can be developed in parallel after the browser harness exists.
- Do not parallelize tasks that edit the same App composition, departure adapter, stylesheet or quickstart evidence file.

## Parallel Example: User Story 1

```text
After T005-T010 and test setup:
Task T011: Configuration validation tests in src/app/config.test.ts
Task T012: Station reference client tests in src/features/station-display/api/station-reference-client.test.ts
Then implement T014–T016 after the corresponding tests are in place.
```

## Implementation Strategy

### Suggested MVP slice

Complete Setup, Foundation and **US1 plus the fixture-backed core of US2** to demonstrate a single configured station and its clearly labeled departure board. This is a UI/fixture MVP only; it does not satisfy live departure acceptance or release verification while T024–T026 remain blocked.

### Incremental delivery

1. Complete test setup and shared API/model/fixture boundaries.
2. Deliver US1 station validation and identity.
3. Deliver US2 fixture-backed board and operational presentation.
4. Add US3 automatic fixture updates and US4 stale/revalidation states.
5. Verify US5 at both approved profiles and complete US6 passive-scope checks.
6. Unblock and complete live integration only after backend contracts are approved, then execute T047 after T030.

## Notes

- Every task begins with a checkbox and sequential ID; `[P]` is used only for independent work, and every story task includes a `[US#]` label.
- Every task names the exact source, test or planning artifact path it changes.
- “BLOCKED” tasks are not complete until their stated backend gate is met. Fixture evidence cannot unblock them.
- No React implementation, dependency installation, backend contract change, `plan.md` rewrite or task execution is performed by this task-generation artifact.
