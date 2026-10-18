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

- [ ] T001 Verify the installed React/Vite/TypeScript baseline from `package.json`, `package-lock.json` and `tsconfig.app.json`; retain those versions, add Vitest 5, jsdom, React Testing Library, `@testing-library/dom`, user-event, jest-dom, Playwright Test and axe Playwright integration as development dependencies, add `test:unit` and `test:e2e` scripts in `package.json`, and update `package-lock.json`.
- [ ] T002 [P] Configure Vitest to reuse Vite plugins and use jsdom in `vite.config.ts` (after T001).
- [ ] T003 Create `src/test/setup.ts` to load jest-dom matchers and perform per-test DOM cleanup; reference it from `vite.config.ts` (after T002).
- [ ] T004 [P] Configure Playwright webServer, test directory and deterministic browser settings in `playwright.config.ts`; use root `e2e/` per the plan (after T001).

---

## Phase 2: Foundational — Shared frontend boundaries

**Purpose**: Provide the minimal shared pieces required by all six stories; no future API schema is assumed.

- [ ] T005 [P] Create `src/shared/api/http-client.ts` with a typed fetch wrapper for base URL, JSON parsing, network failure and the checked-in Problem response behavior; add `src/shared/api/http-client.test.ts` for success, malformed response and failure cases.
- [ ] T006 [P] Create `src/features/station-display/model/display-model.ts` with internal station context and normalized presentation concepts for fixture departures; keep future backend field names opaque, and keep state transitions in the separate `display-state.ts` module.
- [ ] T007 Create `src/features/station-display/api/fixtures/station-display.fixture.ts` with clearly labeled non-live station/departure fixtures for multiple stations, empty data and operational updates; ensure fixture state cannot be labeled live by application composition.

**Checkpoint**: Foundation allows story work with deterministic fixtures. Fixture output is never evidence of live backend integration.

---

## Phase 3: User Story 1 — Show departures for the configured station (Priority: P1)

**Goal**: Validate one deployment-provided station ID and show its published identity without fallback or passenger selection.

**Independent Test**: Provide valid, missing, malformed, unavailable and unpublished station IDs; verify the published name is shown only for an exact match and invalid cases show configuration/unavailable state.

### Tests for User Story 1

- [ ] T008 [P] [US1] Add configuration validation tests in `src/app/config.test.ts` for missing API base URL, missing station ID, malformed/non-UUID station ID and valid public configuration (SR-002; spec US1 scenarios 1 and 4).
- [ ] T009 [P] [US1] Add station reference client tests in `src/features/station-display/api/station-reference-client.test.ts` for published list match, no match, API failure and no arbitrary fallback (SR-002; spec US1 scenario 4).
- [ ] T010 [US1] Add station header rendering tests in `src/features/station-display/components/station-header.test.tsx` for authoritative station name and configuration/unavailable states (SR-054, SR-061; spec US1 scenario 1).

### Implementation for User Story 1

- [ ] T011 [US1] Implement public Vite configuration reading and UUID validation in `src/app/config.ts`; document build-time/public semantics and example variable names without storing secrets (spec FR-001; Q1).
- [ ] T012 [US1] Implement the published station lookup and exact stable-ID match in `src/features/station-display/api/station-reference-client.ts` using only the checked-in `GET /api/stations` contract (SR-002; spec US1 scenarios 1 and 4).
- [ ] T013 [US1] Implement the station identity header and invalid/unavailable station state in `src/features/station-display/components/station-header.tsx` and compose startup in the existing `src/App.tsx`; never show a selector or fallback station (SR-002, SR-061; spec FR-001/FR-008).

**Checkpoint**: US1 is independently testable with the public station contract or deterministic station fixtures.

---

## Phase 4: User Story 2 — Understand published operational changes (Priority: P1)

**Goal**: Present dated station departure fixtures with separate scheduled and operational meanings; preserve unknown values and visible disruption labels.

**Independent Test**: Given normalized, clearly labeled fixture departures for more than one station, render only the configured station's rows and verify scheduled time, service identity, published status/platform/delay/cancellation and missing-value handling.

### Tests for User Story 2

- [ ] T014 [P] [US2] Add departure row tests in `src/features/station-display/components/departure-row.test.tsx` for scheduled-time labeling, service identity, absent values and text/symbol status alongside color (SR-054, SR-035; spec US2 scenarios 1 and 5).
- [ ] T015 [P] [US2] Add board tests in `src/features/station-display/components/departure-board.test.tsx` for configured-station isolation, stable order from the source, empty state and no service-search substitution (SR-054, SR-003, SR-004; spec US1 scenarios 2/3).
- [ ] T016 [US2] Add operational update tests in `src/features/station-display/components/operational-change.test.tsx` for platform change, delay, cancellation prominence, source-provided update time and unknown values (SR-056, SR-010, SR-035; spec US2 scenarios 2–5).

### Implementation for User Story 2

- [ ] T017 [US2] Implement the departure row presentation in `src/features/station-display/components/departure-row.tsx` with distinct scheduled and operational labels, service identity and explicit unknown/unavailable output (SR-054, SR-035; spec FR-002–FR-004).
- [ ] T018 [US2] Implement the station departure board in `src/features/station-display/components/departure-board.tsx` using only rows associated with the configured station and source-provided order; provide a no-departures state (SR-054; spec US1 scenarios 1–3).
- [ ] T019 [US2] Implement operational status/change presentation in `src/features/station-display/components/operational-change.tsx` for source-provided platform, delay and cancellation values; expose update time only when supplied with verified meaning and never derive values (SR-056, SR-010, SR-035; spec FR-003/FR-004).
- [ ] T020 [US2] Wire the non-live fixture adapter into `src/App.tsx` for isolated UI development and show a persistent fixture/demo label whenever fixture data is rendered (SR-054, SR-061; plan fixture boundary).

### Backend contract gate for User Story 2 — BLOCKED

- [ ] T021 [US2] **BLOCKED — backend approval required**: Update `specs/001-station-information-board/contracts/README.md` to reference the approved contract for `GET /api/stations/{id}/departures` only after the backend owner supplies its response schema, public projection, station/date/time/timezone rules, “upcoming” ordering, operational and unknown-value meanings, timestamps/revisions, and error behavior.
- [ ] T022 [US2] **BLOCKED — T021 required**: Implement the live station departures adapter in `src/features/station-display/api/station-departure-adapter.ts` against the approved `GET /api/stations/{id}/departures` contract; do not add request/response fields or business rules absent from that contract.
- [ ] T023 [US2] **BLOCKED — T021 and T022 required**: Add live contract-backed departure acceptance coverage in `e2e/station-live-contract.spec.ts` for selected-station isolation, schedule/status meaning, empty/unknown values and API failures; fixtures alone must not pass this task.

**Checkpoint**: Fixture-backed US2 may be demonstrated as fixture data. It is not live SR-054/SR-056 acceptance until the blocked contract tasks pass.

---

## Phase 5: User Story 3 — See changes without manually reloading (Priority: P1)

**Goal**: Automatically present newly available authoritative information while connected, with no arbitrary polling interval or delivery protocol.

**Independent Test**: A deterministic source fixture emits a newer normalized snapshot; the rendered board updates without a passenger action and does not claim a production timing guarantee.

### Tests for User Story 3

- [ ] T024 [P] [US3] Add refresh coordinator tests in `src/features/station-display/hooks/use-departure-updates.test.ts` for initial snapshot, newly available snapshot, source failure and no passenger-triggered action (SR-058; spec US3 scenarios 1–3).
- [ ] T025 [US3] Add a fixture event-source test in `src/features/station-display/api/fixtures/fixture-departure-source.test.ts` proving deterministic update arrival without a configured numeric interval (SR-058; spec FR-005).

### Implementation for User Story 3

- [ ] T026 [US3] Define the normalized internal source boundary in `src/features/station-display/api/station-departure-source.ts` for initial data and source-delivered changes; document that it is not an approved backend JSON contract (SR-058; contract register).
- [ ] T027 [US3] Implement `src/features/station-display/hooks/use-departure-updates.ts` to consume source updates and refresh displayed state without a user action; keep cadence and transport in the source adapter rather than inventing polling/push behavior (SR-058; spec FR-005).
- [ ] T028 [US3] Implement deterministic fixture update delivery in `src/features/station-display/api/fixtures/fixture-departure-source.ts` so UI tests can emit changes with a persistent non-live label (SR-058; spec assumption on fixtures).

### Live automatic-update integration gate — BLOCKED

- [ ] T029 [US3] **BLOCKED — approved departure/change contract required**: Connect `src/features/station-display/api/station-departure-adapter.ts` to the backend-approved automatic update mechanism and interval only after those decisions are published; do not select polling versus push in the frontend plan.
- [ ] T030 [US3] **BLOCKED — T029 required**: Add live browser verification in `e2e/station-live-contract.spec.ts` proving a published backend change reaches the board without manual reload; assert no latency target unless separately approved.
- [ ] T031 [US3] **CONDITIONAL / BLOCKED**: Add use of `GET /api/journeys/{id}/updates` in `src/features/station-display/api/station-update-adapter.ts` only if the approved departures contract cannot provide the required change detection; first record that backend decision in `contracts/README.md`.

**Checkpoint**: Fixture auto-update can be verified independently; live SR-058 remains blocked until the backend update contract is approved and tested.

---

## Phase 6: User Story 4 — Recognize disconnected or stale information (Priority: P1)

**Goal**: Preserve the last successful in-session information with an explicit stale indication during connection loss and clear that indication only after authoritative revalidation.

**Independent Test**: Verify disconnect-before-first-load, disconnect-after-success, and reconnect; prior data stays marked stale and only a successful authoritative response clears the warning.

### Tests for User Story 4

- [ ] T032 [P] [US4] Add display state tests in `src/features/station-display/model/display-state.test.ts` for unavailable-without-data, stale-with-last-success, revalidating and recovered transitions (SR-059; spec US4 scenarios 1–4).
- [ ] T033 [US4] Add reconnect behavior tests in `src/features/station-display/hooks/use-revalidation.test.ts` proving online notification triggers a source revalidation but does not clear stale state by itself (SR-059; spec FR-006/FR-007).
- [ ] T034 [US4] Add browser offline/recovery scenarios in `e2e/station-connectivity.spec.ts` for disconnect before first response, disconnect after success, cached label, failed revalidation and recovery after success (SR-059; quickstart scenario 5).

### Implementation for User Story 4

- [ ] T035 [US4] Implement the display information state transitions in `src/features/station-display/model/display-state.ts` so last successful data remains in active-session memory and is never represented as current during disconnection (SR-059; spec FR-006/FR-008).
- [ ] T036 [US4] Implement reconnection-triggered authoritative revalidation in `src/features/station-display/hooks/use-revalidation.ts`; retain the stale warning until the source confirms a successful current response (SR-059; spec FR-007).
- [ ] T037 [US4] Implement loading, empty, error, unavailable, disconnected/stale, revalidating and recovered state views in `src/features/station-display/components/display-state-view.tsx`; show last-update only when the approved source meaning is available (SR-059, SR-035; spec FR-006–FR-008).

**Checkpoint**: Active-session outage behavior is testable with fixtures. Durable browser storage and expiry/TTL are excluded pending approved policy.

---

## Phase 7: User Story 5 — Read the board at approved display profiles (Priority: P2)

**Goal**: Preserve legibility, essential fields and disruption visibility at 1920 × 1080 and 1366 × 768 landscape, meeting applicable WCAG 2.2 AA criteria.

**Independent Test**: Run the populated, delayed and cancelled board at both browser viewport sizes; verify all required information remains visible and status is understandable without color alone.

### Tests for User Story 5

- [ ] T038 [P] [US5] Add Playwright viewport scenarios in `e2e/station-display-profiles.spec.ts` at 1920 × 1080 and 1366 × 768; assert station, scheduled time, service identity, status and critical disruption are visible at both profiles (SR-054, SR-056, SR-061; spec SC-006).
- [ ] T039 [P] [US5] Add real-browser axe checks and text/symbol status assertions in `e2e/station-accessibility.spec.ts` for applicable WCAG 2.2 AA criteria; record any manual contrast checks in `specs/001-station-information-board/quickstart.md` (constitution IV/V).

### Implementation for User Story 5

- [ ] T040 [US5] Implement responsive board, row and disruption layout rules in `src/features/station-display/station-display.css` for both approved landscape viewports without adding viewing-distance or hardware claims (SR-054, SR-056, SR-061; spec FR-009).
- [ ] T041 [US5] Refine typography, hierarchy, focus/announcement semantics, contrast and text/symbol status treatment in `src/features/station-display/components/departure-row.tsx`, `src/features/station-display/components/operational-change.tsx`, `src/features/station-display/components/display-state-view.tsx` and `src/features/station-display/station-display.css` to satisfy WCAG 2.2 AA; ensure critical disruption is not hidden at 1366 × 768 (spec FR-009/FR-010/FR-012).
- [ ] T042 [US5] Capture and review browser evidence for both approved viewport profiles in `specs/001-station-information-board/quickstart.md`, including populated, delayed and cancelled states (SR-061; spec SC-006).

**Checkpoint**: US5 passes only with real-browser evidence at both approved viewports; jsdom layout assertions are insufficient.

---

## Phase 8: User Story 6 — Use a passive passenger-information display (Priority: P2)

**Goal**: Present public railway information without passenger selector, station management, controller, privileged write or kiosk workflows.

**Independent Test**: Inspect the rendered public board and all available interactions; verify no management or terminal actions are exposed.

### Tests for User Story 6

- [ ] T043 [P] [US6] Add Playwright assertions in `e2e/station-passive-display.spec.ts` that the public display exposes no station selector, editing, publishing, controller, authentication or kiosk actions (spec US6 scenarios 1–3; constitution frontend constraints).

### Implementation for User Story 6

- [ ] T044 [US6] Keep `src/App.tsx` and `src/features/station-display/components/` limited to passive public information and remove any starter/template interaction that is not part of the approved display scope (SR-061; spec FR-011).

**Checkpoint**: US6 is complete when the board remains passive across loading, error, stale and populated states.

---

## Phase 9: Polish and Cross-Cutting Verification

**Purpose**: Close requirement traceability, execute available quality gates, and preserve the explicit live-integration stop condition.

- [ ] T045 [P] Add the verification mapping and fixture-versus-live acceptance status to `specs/001-station-information-board/quickstart.md` for SR-054/056/058/059/061 and applicable SR-001/002/003/004/010/035.
- [ ] T046 Run `npm run lint`, `npm run build`, `npm run test:unit` and `npm run test:e2e`; record exact results and any environment limitation in `specs/001-station-information-board/quickstart.md`.
- [ ] T047 **BLOCKED — T021–T023, T029 and verified backend environment required**: Complete live contract-backed acceptance and record source contract version, outcomes and evidence for SR-054/056/058/059 in `specs/001-station-information-board/quickstart.md`; do not mark this complete with fixtures alone.

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: T001 precedes T002–T004. T002 precedes T003. No application implementation begins before its test harness/configuration is available.
- **Foundational (Phase 2)**: T005 and T006 can proceed in parallel after setup. T007 depends on T006. Story work depends on the required foundation and test setup.
- **User stories**: US1 establishes validated station identity. US2 uses it to render fixture-backed departures. US3 and US4 extend the source/state boundary after the board exists. US5 verifies/refines the populated board; US6 verifies passive scope. Fixture-based story work does not depend on future backend contracts.
- **Live integration**: T021 is blocked until the backend publishes/approves all required departure semantics. T022–T023 depend on T021. T029–T030 depend on an approved update mechanism/interval and T022. T031 is conditional on backend approval that `/api/journeys/{id}/updates` is needed. T047 remains blocked until all live acceptance dependencies and a verified environment exist.
- **Polish (Phase 9)**: T045–T046 follow completed independent stories. T047 remains blocked if the backend gate is unresolved; it does not prevent fixture-based UI verification.

### User Story Dependencies

- **US1 (P1)**: Starts after setup/foundation. Its station configuration and identity flow is independently testable against the existing public station contract.
- **US2 (P1)**: Uses US1's station context. Fixture-backed board behavior is independently testable; live departure integration is blocked by the future endpoint contract.
- **US3 (P1)**: Uses the board/source boundary from US2. Fixture update arrival is independently testable; live update propagation is blocked by backend delivery/revision decisions.
- **US4 (P1)**: Uses US2/US3 data states. Fixture-driven offline and reconnect behavior is independently testable; live freshness validity remains contract-gated.
- **US5 (P2)**: Uses the populated board from US2 and state treatments from US4. Browser viewport and accessibility verification can proceed with labeled fixtures.
- **US6 (P2)**: Can verify the integrated passive surface after US1/US2 composition; it adds no management or kiosk functionality.

### Parallel Opportunities

- T002 and T004 can run in parallel after T001 because they configure separate files.
- T005 and T006 can run in parallel; T007 follows T006.
- Within US1, T008 and T009 are independent test files; T010 is independent after the component boundary exists.
- Within US2, T014 and T015 can be prepared in separate test files; T016 covers operational rendering separately. Component implementation tasks must wait for their respective tests and shared view model.
- US5 profile and accessibility test files T038 and T039 can be developed in parallel after the browser harness exists.
- Do not parallelize tasks that edit the same App composition, departure adapter, stylesheet or quickstart evidence file.

## Parallel Example: User Story 1

```text
After T005–T007 and test setup:
Task T008: Configuration validation tests in src/app/config.test.ts
Task T009: Station reference client tests in src/features/station-display/api/station-reference-client.test.ts
Then implement T011–T013 after the corresponding tests are in place.
```

## Implementation Strategy

### Suggested MVP slice

Complete Setup, Foundation and **US1 plus the fixture-backed core of US2** to demonstrate a single configured station and its clearly labeled departure board. This is a UI/fixture MVP only; it does not satisfy live departure acceptance or release verification while T021–T023 remain blocked.

### Incremental delivery

1. Complete test setup and shared API/model/fixture boundaries.
2. Deliver US1 station validation and identity.
3. Deliver US2 fixture-backed board and operational presentation.
4. Add US3 automatic fixture updates and US4 stale/revalidation states.
5. Verify US5 at both approved profiles and complete US6 passive-scope checks.
6. Unblock and complete live integration only after backend contracts are approved, then execute T047.

## Notes

- Every task begins with a checkbox and sequential ID; `[P]` is used only for independent work, and every story task includes a `[US#]` label.
- Every task names the exact source, test or planning artifact path it changes.
- “BLOCKED” tasks are not complete until their stated backend gate is met. Fixture evidence cannot unblock them.
- No React implementation, dependency installation, backend contract change, `plan.md` rewrite or task execution is performed by this task-generation artifact.
