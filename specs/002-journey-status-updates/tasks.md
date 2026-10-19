---
description: "Feature 002 implementation task list"
---

# Tasks: Journey Status and Updates

**Input**: Approved design documents in `specs/002-journey-status-updates/`.

**Prerequisites**: [spec.md](spec.md), [plan.md](plan.md), [research.md](research.md), [data-model.md](data-model.md), [contracts/journey-status-client.md](contracts/journey-status-client.md), and [quickstart.md](quickstart.md).

**Testing**: Tests are required by the specification, constitution, and approved implementation brief. Follow test-first order: add focused tests, verify they fail for the missing behavior, then implement. Use MockClient/fakes for deterministic tests; real backend tests are separate.

**Organization**: Tasks are grouped by the four user stories in `spec.md`. Every task stays unchecked until completed with evidence. User Story phases all have P1 priority.

**Format**: `- [ ] T### [P?] [US#?] Action, exact paths, trace, verification, dependencies`

## Phase 1: Setup (Shared Fixtures and Integration Configuration)

**Purpose**: Provide explicit test inputs and fail-closed integration configuration. No production package or app behavior changes.

- [ ] T001 [P] Add valid and empty station-departure response fixtures at `test/features/journey_status/data/fixtures/station_departures.json` and `test/features/journey_status/data/fixtures/empty_station_departures.json`, including distinct journey/service IDs, station-local date, operation date, nullable calls, and an ambiguous candidate; verify fixtures are valid JSON matching the approved contract (FR-002–003; SR-007).
- [ ] T002 [P] Add valid baseline and operational snapshot fixtures at `test/features/journey_status/data/fixtures/journey_snapshot_baseline.json` and `test/features/journey_status/data/fixtures/journey_snapshot_operational.json`, covering baseline/operational source values, null/non-null ETA, and planned/operational stop values; the parameterized model test covers all seven statuses; verify only OpenAPI fields occur (FR-004–007; SR-019/021/102).
- [ ] T003 [P] Add empty and populated update-history fixtures at `test/features/journey_status/data/fixtures/journey_updates_empty.json` and `test/features/journey_status/data/fixtures/journey_updates_populated.json`, including status, stopChange, routeDiversion, and correction public changes in ascending revisions; verify no private/write-only fields are present (FR-008–010; SR-010/022/025/100).
- [ ] T004 Add fail-closed configuration tests in `test/journey_status_integration_config_test.dart` requiring an explicit API URL, origin station UUID, service UUID, and `YYYY-MM-DD` test date; assert missing/invalid defines fail with a clear setup error and never select a mock backend (SC-001/007; SR-089).
- [ ] T005 Implement explicit integration configuration in `integration_test/journey_status/journey_status_integration_config.dart`, reading only the required `SMART_RAIL_API_BASE_URL`, `SMART_RAIL_TEST_ORIGIN_STATION_ID`, `SMART_RAIL_TEST_SERVICE_ID`, and `SMART_RAIL_TEST_DATE` defines; validate UUID/date syntax without supplying defaults (FR-002, FR-017; SR-089; depends on T004).

---

## Phase 2: Foundational (Journey API and Domain Boundary)

**Purpose**: Complete shared typed contracts, API/repository boundary, and provider composition before any story UI/state work. These tasks support US1–US4 and block all story phases.

- [ ] T006 [P] Write DTO parsing tests in `test/features/journey_status/data/journey_api_dtos_test.dart` for station departure, snapshot, stop, estimate, update, each public-change discriminator/correction, Problem Details, nullable/empty values, missing required fields, malformed types/enums/timestamps, and unknown/private fields on closed schemas; ignore Problem Details extension fields safely without exposing them; require contract-compliant values to parse and invalid success bodies to fail (FR-002–009, FR-014; SR-007/010/019/021/100).
- [ ] T007 Implement strict immutable DTO parsing in `lib/features/journey_status/data/journey_api_dtos.dart`; enforce verbatim data-model constraints: `JourneyStatus` is exactly `Scheduled|Running|Delayed|Interrupted|Cancelled|Completed|Terminated`, `ambiguity` is `none|ambiguous`, snapshot `source` is `ScheduledBaseline|ControllerManual|ApprovedRailwayFeed`, update `kind` is `status|stopChange|routeDiversion|correction`, update `source` is `ControllerManual|ApprovedRailwayFeed`, and ETA `freshness` is `current|stale|lastKnown`; `scheduleRevision`, `revision`, and `stopOrder` are positive integers; `operationDate` and `stationLocalDepartureDate` are `YYYY-MM-DD`; `firstDepartureOccurrence` is a time; `date-time` values must be valid offset-bearing strings. Required fields are exact: StationDeparture = `journeyInstanceId, serviceId, operationDate, stationLocalDepartureDate, revision, status, stationId, stopOrder, scheduledDepartureAt, ambiguity`; Snapshot = `journeyInstanceId, serviceId, scheduleId, scheduleRevision, operationDate, firstDepartureOccurrence, revision, status, stops, source, publishedAt`; JourneyStop = `journeyStopId, stopOrder, stationId, ambiguity`; Update = `updateId, journeyInstanceId, revision, kind, status, source, publishedAt, publicChange`; `correctionOfUpdateId` is nullable and required for correction kind, where it identifies an earlier same-journey update. Keep nullable values/nullability as specified in data-model.md; stopChange requires at least one of platform, operationalArrivalAt, or operationalDepartureAt. Reject unknown fields on closed schemas, ignore Problem Details extensions without displaying them, preserve array order, and reject malformed success bodies (FR-002–009, FR-014; SR-007/010/019/021/100; depends on T001–T003, T006).
- [ ] T008 [P] Write domain validation tests in `test/features/journey_status/domain/journey_models_test.dart` for separate UUID identities, positive revisions/stop orders, seven distinct statuses, ambiguity values, immutable ordered collections, and offset-preserving timestamps; assert no date/time conversion or inferred fields (FR-002–007; SR-007/019/021).
- [ ] T009 Implement immutable domain types in `lib/features/journey_status/domain/journey_models.dart` and `lib/features/journey_status/domain/journey_timestamp.dart`; keep `journeyInstanceId` distinct from `serviceId`, `operationDate` distinct from `stationLocalDepartureDate`, preserve raw offsets, preserve ordered collections, require positive revisions/stop orders, retain all seven statuses distinctly, and show null ETA as unavailable without deriving one; satisfy the remaining per-field constraints in [data-model.md](data-model.md) (FR-002–007; SR-007/019/021; depends on T008).
- [ ] T010 Add journey-operation failure tests in `test/core/network/api_failure_mapping_test.dart` for departure/snapshot/history 400, 401/403, 404, 500/default, and 503; assert operation-specific classification and use simulated Problem Details/diagnostic values containing `SENTINEL_PRIVATE_TOKEN_123`, `SENTINEL_INTERNAL_ACTOR_456`, and `SENTINEL_TRACE_SECRET_789`, verifying passenger messages never contain them. Use synthetic values only, never credentials or personal data (FR-014; SR-089/100/114).
- [ ] T011 Extend `lib/core/network/api_failure.dart` with explicit station-departures, journey-snapshot, and journey-updates operation classifications and the operation-specific documented mappings, preserving all existing Feature 001 mappings; verify T010 and existing Feature 001 failure tests pass (FR-014; SR-089/100/114; depends on T010).
- [ ] T012 [P] Write exact HTTP contract tests in `test/features/journey_status/data/journey_status_api_test.dart` for the three GET paths, UUID path values, encoded `date=YYYY-MM-DD`, anonymous requests without Authorization, UTF-8 parsing, timeouts, network errors, and all documented response statuses; assert no PATCH/write request can be emitted (FR-002, FR-014, FR-017; SR-089/114; depends on T007, T011).
- [ ] T013 Implement `lib/features/journey_status/data/journey_status_api.dart` using existing `HttpApiClient` and the exact three operations in [journey-status-client.md](contracts/journey-status-client.md); do not add auth headers, endpoints, status semantics, or retries (FR-002, FR-014, FR-017; SR-089/114; depends on T007, T011–T012).
- [ ] T014 Define `JourneyStatusRepository` in `lib/features/journey_status/domain/journey_status_repository.dart` with only station-departure lookup, journey snapshot, and complete update-history reads; keep repository results in Feature 002 domain types (FR-002, FR-004, FR-008; SR-007/010/019; depends on T009).
- [ ] T015 [P] Write DTO-to-domain repository tests in `test/features/journey_status/data/journey_status_repository_impl_test.dart` for exact identity/date mapping, service/date association fields, update order preservation, empty history, nullable ETA, and parser failures; assert no schedule or status recalculation (FR-002–009, FR-014; SR-007/010/019/021; depends on T013–T014).
- [ ] T016 Implement `JourneyStatusRepositoryImpl` in `lib/features/journey_status/data/journey_status_repository_impl.dart`, mapping only strict DTO fields to domain models and preserving response order; verify T015 with fake API responses (FR-002–009; SR-007/010/019/021; depends on T009, T013–T015).
- [ ] T017 [P] Add provider-composition tests in `test/features/journey_status/application/journey_status_providers_test.dart` verifying journey repository receives the configured AppConfig base URI and reuses `sharedHttpClientProvider` (FR-014, FR-017; SR-089/113; depends on T016).
- [ ] T018 Implement journey API/repository providers in `lib/features/journey_status/application/journey_status_providers.dart`, reusing `appConfigProvider` and the app-owned shared HTTP client without creating/closing a second client; verify T017 (FR-014, FR-017; SR-089/113; depends on T016–T017).
- [ ] T019 Verify provider lifecycle in `test/app/journey_status_provider_lifecycle_test.dart`: resolving journey API and disposing its ProviderContainer closes the one shared client exactly once, while route/provider disposal does not close app-scoped transport; retain existing lifecycle tests (FR-014; SR-089; depends on T018).

**Checkpoint**: All three anonymous API operations parse and map through a typed repository; existing Feature 001 network semantics remain intact.

---

## Phase 3: User Story 1 — Open the Dated Journey for a Selected Service (Priority: P1)

**Goal**: Find the dated journey linked to the existing Feature 001 service/date, and preserve separate passenger choices for ambiguous candidates.

**Independent Test**: Feed a fake repository with zero, one unambiguous, multiple, and ambiguous candidates. Assert exact service/date matching, preserved response order and journey IDs, no synthetic instance, unique unambiguous behavior, and explicit taps for all ambiguous choices.

- [ ] T020 [P] [US1] Write lookup notifier tests in `test/features/journey_status/application/journey_lookup_notifier_test.dart` for loading, valid empty, exact `serviceId` and `stationLocalDepartureDate` filtering, server-order preservation, backend failure/retry, and zero synthetic journeys (FR-001–003; SR-007/013/114; depends on T016, T018).
- [ ] T021 [US1] Implement `JourneyLookupState` and `JourneyLookupNotifier` in `lib/features/journey_status/application/journey_lookup_state.dart` and `lib/features/journey_status/application/journey_lookup_notifier.dart`; call only the repository departures read and discard completions after disposal (FR-001–003, FR-015; SR-007/013/114; depends on T020).
- [ ] T022 [P] [US1] Write candidate-selection tests in `test/features/journey_status/application/journey_candidate_selection_test.dart` proving one `ambiguity=none` match can be opened, every ambiguous/multiple candidate requires an explicit tap, identical labels keep distinct journey IDs, and no first-row auto-selection occurs (FR-002–003; SR-007/114; depends on T016, T018).
- [ ] T023 [US1] Implement explicit candidate selection in `lib/features/journey_status/application/journey_lookup_notifier.dart`; navigate/select only the tapped candidate's exact `journeyInstanceId`, and retain service/origin/date context (FR-001–003, FR-015; SR-007/013; depends on T021–T022).
- [ ] T024 [P] [US1] Write lookup and candidate widget tests in `test/features/journey_status/presentation/journey_lookup_screen_test.dart` and `test/features/journey_status/presentation/journey_candidates_screen_test.dart` for loading, no-match, one unambiguous result, ambiguous list, identical public labels, explicit tap, accessible ordinal control labels, Back action, and safe failure/retry (FR-001–003, FR-014–016; SR-007/013/102/103/114; depends on T021–T023).
- [ ] T025 [US1] Implement `JourneyCandidatesScreen` in `lib/features/journey_status/presentation/journey_candidates_screen.dart`, showing distinct candidate rows with documented status/date/time/platform values only, an ambiguity cue, accessible option ordinal where needed, and no implicit selection (FR-002–003, FR-016; SR-007/102/103/104; depends on T024).
- [ ] T026 [US1] Implement `JourneyLookupScreen` in `lib/features/journey_status/presentation/journey_lookup_screen.dart` with loading, valid empty, candidate/error feedback and Back behavior; preserve the Feature 001 service context passed into the flow (FR-001–003, FR-015–016; SR-007/013/114; depends on T024–T025).

**Checkpoint**: A passenger can reach a real returned dated journey ID through Feature 001 context, with no service/journey ID conflation or ambiguity auto-selection.

---

## Phase 4: User Story 2 — Inspect Official Journey Status and Updates (Priority: P1)

**Goal**: Display the selected journey's backend-published status, source, planned/operational stops, ETA availability, and ordered public update history.

**Independent Test**: Use snapshot/history fixtures and a fake repository to verify all seven status labels, planned versus operational values, source/revision/publication time, null and populated ETA, empty history, ordered revisions, and omission of private fields.

- [ ] T027 [P] [US2] Write initial snapshot state tests in `test/features/journey_status/application/journey_status_notifier_test.dart` for initial loading, all seven exact statuses, baseline `publishedAt=null`, source/revision retention, ETA absent/null/present, ordered stops, and no inferred status/time/ETA (FR-004–007, FR-011; SR-010/019/021/102; depends on T016, T018).
- [ ] T028 [US2] Implement initial journey snapshot/history loading state in `lib/features/journey_status/application/journey_status_state.dart` and `lib/features/journey_status/application/journey_status_notifier.dart`; keep sections independently representable and accept only the currently selected `journeyInstanceId` response (FR-004–009, FR-012, FR-015; SR-007/010/019/022/109; depends on T027).
- [ ] T029 [P] [US2] Write update-history presentation tests in `test/features/journey_status/presentation/journey_updates_list_test.dart` for empty history versus populated history, ascending returned revisions, each documented public-change type, correction reference, source/time, and no actor/driver/private content (FR-008–009, FR-016; SR-010/022/098/100/102).
- [ ] T030 [US2] Implement `JourneyUpdatesList` in `lib/features/journey_status/presentation/journey_updates_list.dart`, rendering returned update order and documented public fields only; show an explicit no-published-updates state for `[]` (FR-008–009, FR-016–017; SR-010/022/098/100/102/114; depends on T029).
- [ ] T031 [P] [US2] Write status detail widget tests in `test/features/journey_status/presentation/journey_status_screen_test.dart` for seven distinct text status labels, source/revision/time context, baseline without false update time, planned/operational field labels, unknown ETA, supplied ETA freshness, and ordered stops (FR-004–007, FR-016; SR-019/021/102/103/104).
- [ ] T032 [US2] Implement `JourneyStatusScreen` in `lib/features/journey_status/presentation/journey_status_screen.dart`, composing the status snapshot, planned/operational stop values, ETA/unavailable state, update history, and loading/empty feedback using Feature 001 theme/shared components (FR-004–009, FR-016–017; SR-001/010/019/021/022/089/098/100/102/113/114; depends on T028, T030–T031).
- [ ] T033 [US2] Add integration assertions to `test/features/journey_status/application/journey_status_notifier_test.dart` that empty update history remains distinct from missing journey or failed history read and that snapshot response values are displayed without merging in a different response's values (FR-008, FR-012; SR-010/114; depends on T028).

**Checkpoint**: The user can inspect confirmed public snapshot/history data, and empty, baseline, unknown ETA, and private-data boundaries are accurately represented.

---

## Phase 5: User Story 3 — Refresh Information and Understand Its Freshness (Priority: P1)

**Goal**: Manually refresh snapshot and history, retain confirmed sections on failure, and distinguish client retrieval time from backend publication time.

**Independent Test**: Load both sections, refresh to a newer response, fail each section independently, and verify targeted retry, retained-not-refreshed labels, and client/source timestamps remain distinct.

- [ ] T034 [P] [US3] Write refresh and partial-read tests in `test/features/journey_status/application/journey_status_refresh_test.dart` for simultaneous refresh, independently pending reads, duplicate refresh suppression, partial success, differing revisions, and late completion after notifier disposal (FR-011–013, FR-015; SR-109/114; depends on T028).
- [ ] T035 [US3] Implement `refresh()`, `retrySnapshot()`, and `retryUpdates()` in `lib/features/journey_status/application/journey_status_notifier.dart`; full refresh requests both reads, each section retry requests only its failed operation, and request generations/disposal prevent duplicate or late state writes (FR-011–013, FR-015; SR-109/114; depends on T034).
- [ ] T036 [P] [US3] Write retrieval metadata tests in `test/features/journey_status/domain/journey_read_metadata_test.dart` for successful client retrieval time, failed-refresh stale marker retaining last successful time, later successful recovery, and separation from backend `publishedAt`/`lastUpdatedAt` (FR-011, FR-013; SR-010/109).
- [ ] T037 [US3] Implement per-section `JourneyReadMetadata` in `lib/features/journey_status/domain/journey_read_metadata.dart` and wire it through `lib/features/journey_status/application/journey_status_state.dart`; never label client retrieval time as railway publication time (FR-011–013; SR-010/109; depends on T036).
- [ ] T038 [P] [US3] Write refresh interaction widget tests in `test/features/journey_status/presentation/journey_status_refresh_test.dart` for accessible manual refresh, loading feedback, last-retrieved display, retained section after failure, section-specific retry, and visible revision mismatch/incomplete state (FR-011–013, FR-016; SR-109/114; depends on T035, T037).
- [ ] T039 [US3] Implement refresh actions, progress, retry controls, stale labels, and revision-mismatch explanation in `lib/features/journey_status/presentation/journey_status_screen.dart`; keep loaded data visible only with `Not refreshed` when a subsequent read fails (FR-011–013, FR-016; SR-109/114; depends on T038).
- [ ] T040 [US3] Add notifier tests in `test/features/journey_status/application/journey_status_refresh_test.dart` verifying a failed history refresh does not erase successful snapshot data, a failed snapshot refresh does not erase successful history, and retries do not touch the healthy sibling operation (FR-012–013; SR-109/114; depends on T035, T037).
- [ ] T041 [US3] Add widget/provider disposal tests in `test/features/journey_status/application/journey_status_refresh_test.dart` verifying Back/disposal during requests keeps navigation responsive and late results neither replace departed UI nor reopen a journey route (FR-015; SR-114; depends on T035).

**Checkpoint**: Manual refresh and recovery preserve confirmed data honestly and cannot let delayed responses mutate a departed view.

---

## Phase 6: User Story 4 — Notice Significant Changes and Recover (Priority: P1)

**Goal**: Alert only for newly detected supported public changes on view entry/manual refresh, associate notices with history identity/revision, and suppress duplicates for the active session.

**Independent Test**: Seed a session baseline, return a new supported change, fail then retry history, revisit the view, and assert only a successfully detected new update alerts once with its exact `updateId` and revision; no polling occurs.

- [ ] T042 [P] [US4] Write session-tracker tests in `test/features/journey_status/application/seen_journey_update_tracker_test.dart` for first-read baseline with no alert, new update IDs, repeated IDs, separate journey IDs, failed-read non-advancement, and active-session reset (FR-010; SR-022/025).
- [ ] T043 [P] [US4] Implement app-session `SeenJourneyUpdateTracker` in `lib/features/journey_status/application/seen_journey_update_tracker.dart`, keyed by `journeyInstanceId` and storing only successfully observed `updateId`s in memory; first valid history is baseline and no persistence/timer is added (FR-010, FR-017; SR-025; depends on T042).
- [ ] T044 [P] [US4] Write supported-change and correction detection tests in `test/features/journey_status/application/journey_update_alerts_test.dart` for delayed/cancelled status, platform/operational-time stop changes, route diversion, documented correction changes, and unsupported/non-significant changes; assert alert fields equal the source history update ID/revision (FR-009–010; SR-010/022/025/100).
- [ ] T045 [US4] Implement significant public-change classification in `lib/features/journey_status/application/journey_update_alerts.dart` using only `JourneyUpdate.publicChange` and documented correction data; never derive a reason or alert from driver/private fields (FR-009–010, FR-017; SR-010/022/025/098/100; depends on T009, T044).
- [ ] T046 [US4] Integrate baseline comparison and successful-read advancement in `lib/features/journey_status/application/journey_status_notifier.dart`; only view entry/manual refresh can detect changes, unsuccessful history reads do not advance seen IDs, and repeated IDs do not re-alert (FR-010–013, FR-017; SR-025/109/114; depends on T035, T043–T045).
- [ ] T047 [P] [US4] Write alert widget tests in `test/features/journey_status/presentation/journey_update_alert_test.dart` asserting the notice displays the exact same `updateId`, journey revision, status/change details and source as the history entry, and announces once without relying on color (FR-009–010, FR-016; SR-010/022/025/102/103).
- [ ] T048 [US4] Implement in-app update notice presentation in `lib/features/journey_status/presentation/journey_update_alert.dart` and compose it in `lib/features/journey_status/presentation/journey_status_screen.dart`; do not add push, notification subscription, background delivery, polling, or periodic timers (FR-010, FR-016–017; SR-025/102/103; depends on T046–T047).
- [ ] T049 [US4] Add no-polling and retry-baseline assertions in `test/features/journey_status/application/journey_update_alerts_test.dart`: no network request occurs absent view entry/manual refresh, and after failed history the next successful read can still detect and alert the new update exactly once (FR-010–013; SR-025/109/114; depends on T046).

**Checkpoint**: Update alerts follow approved Q2, use update ID/revision identity, and do not imply any delivery channel the API lacks.

---

## Phase 7: Cross-Story Recovery, Navigation, and Accessibility

**Purpose**: Integrate the journey flow into the existing app, complete operation-specific recovery and accessibility, and prove Feature 001 remains unchanged.

- [ ] T050 [P] Write journey recovery widget tests in `test/features/journey_status/presentation/journey_recovery_test.dart` for lookup/snapshot/history 400, unexpected 401/403, 404, 500, 503, timeout, network and malformed-success cases; place synthetic `SENTINEL_PRIVATE_TOKEN_123`, `SENTINEL_INTERNAL_ACTOR_456`, and `SENTINEL_TRACE_SECRET_789` values in simulated Problem Details/diagnostic metadata and assert they are absent from passenger-visible text and accessibility semantics. Also assert correct section, Back availability, and Retry only for recoverable unchanged inputs; use no real credentials or personal data (FR-014–016; SR-089/100/109/114).
- [ ] T051 Implement operation-specific passenger recovery mapping in `lib/features/journey_status/application/journey_recovery_mapper.dart`: departure 400/404, detail/history 400/404, unexpected access denial and recoverable server/network/protocol outcomes must follow [journey-status-client.md](contracts/journey-status-client.md), with no login prompt or raw diagnostics; preserve T050 sentinel-redaction assertions (FR-014, FR-016; SR-089/100/114; depends on T050).
- [ ] T052 Implement retry/back/error section composition in `lib/features/journey_status/presentation/journey_recovery_section.dart` and use it in journey lookup/status screens; make 403/400/404 Back-only and 500/503/network/protocol Retry recover only the failed read, and keep diagnostic sentinels out of rendered text and semantics (FR-012, FR-014–016; SR-089/100/109/114; depends on T051).
- [ ] T053 [P] Write route/navigation tests in `test/features/journey_status/presentation/journey_status_navigation_test.dart` for Feature 001 service-details → lookup → candidate selection → status/history → Back, preserving active search/results/service details and ignoring late responses after Back or changed selection. Use a test-owned GoRouter harness with completed screen interfaces so this test compiles before production route wiring; production route/entry behavior is verified by T061–T062 (FR-001, FR-015; SR-007/013/089/114; depends on T025–026, T032, T035).
- [ ] T054 Integrate journey lookup/status routes into `lib/app/router.dart` with typed route context carrying the selected service, origin, travel date, and selected opaque journey ID; keep GoRouter push/pop stack and do not alter Feature 001 route behavior (FR-001–003, FR-015; SR-007/013/089; depends on T053).
- [ ] T055 [P] Write Feature 001 entry-action tests in `test/features/railway_search/presentation/journey_status_entry_navigation_test.dart` verifying the journey-status action receives the existing service ID, origin station UUID, and search travel date without changing service-detail data or search state (FR-001–002, FR-015; SR-007/013/089).
- [ ] T056 Add the journey-status entry action to `lib/features/railway_search/presentation/service_detail_screen.dart`, passing only existing Feature 001 selection context into the registered journey route and retaining Back to the same detail screen (FR-001–003, FR-015; SR-007/013/089; depends on T054–T055).
- [ ] T057 [P] Write accessibility tests in `test/features/journey_status/presentation/journey_status_accessibility_test.dart` for accessible names/selected states on candidate choices, meaningful live announcements for status/error/alert changes, text scaling, status meaning independent of color, and absence of the three synthetic sensitive-data sentinels from error semantics; label SR-103/SR-104 assertions Should Have (FR-014, FR-016; SR-100/102/103/104/114; depends on T026, T032, T048, T052).
- [ ] T058 Serial integration task: apply the accessibility semantics, focus order, non-color status meaning and text-scale-safe layouts in `lib/features/journey_status/presentation/journey_lookup_screen.dart`, `journey_candidates_screen.dart`, `journey_status_screen.dart`, `journey_updates_list.dart`, and `journey_update_alert.dart`, after T025–T026, T032, T039 and T048 are complete. This task is owned by the integration orchestrator; Agent D authors tests only and does not edit these screens. Preserve SR-103/SR-104 Should Have priority (FR-016; SR-102/103/104; depends on T025–T026, T032, T039, T048, T057).
- [ ] T059 Add Feature 001 regression coverage in `test/features/railway_search/presentation/journey_status_entry_navigation_test.dart` and run `flutter test test/features/railway_search`; assert search, results, service details, existing APIs, and navigation tests remain passing after the entry action (FR-001, FR-015, FR-017; SR-013/089/113; depends on T056).
- [ ] T060 Review, validate and update the existing `specs/002-journey-status-updates/verification-matrix.md`; preserve all existing FR, SC and SR mappings, status/evidence columns and any recorded evidence, adding only required traceability corrections (including SR-100 sentinel-redaction evidence). Never recreate, overwrite or reset the matrix or initialize existing evidence back to NOT RUN (SC-001–007; SR-001/007/010/013/019/021/022/025/089/098/100/102/103/104/109/113/114; depends on T059).

**Checkpoint**: Journey flow is reachable from existing Feature 001 details, Back preserves context, accessible states and operation-specific failures are testable, and Feature 001 regression tests pass.

---

## Phase 8: Real Backend Integration (Android Emulator)

**Purpose**: Verify real anonymous service-to-journey selection and displayed values against the seeded SmartRail backend. Mocks are not accepted as integration evidence.

- [ ] T061 [P] Add UI-driven real station-departure integration test in `integration_test/journey_status/journey_lookup_integration_test.dart` using the fail-closed config from T005; launch the app through its registered GoRouter path from the Feature 001 service-detail entry, make an anonymous request, filter the configured real service/date, and assert selected journey ID came from the response and ambiguous choices require a tap. Author/compile only after providers, lookup/candidate screens, route and entry action exist (FR-001–003, FR-017; SR-007/013/089; depends on T005, T018, T025–T026, T054, T056, T060).
- [ ] T062 [P] Add UI-driven real snapshot/history integration test in `integration_test/journey_status/journey_status_integration_test.dart` using the selected returned `journeyInstanceId`; launch through the same app route and compare displayed status, revision, source/times/stops, update IDs/revisions/order and ETA availability with actual backend responses. Author/compile only after providers, status/history/alert screens, GoRouter route and Feature 001 entry action exist (FR-004–010, FR-017; SR-001/010/019/021/022/025/089/098/100/102; depends on T018, T032, T048, T054, T056, T060).
- [ ] T063 Run the Feature 002 integration suite on an Android emulator against the explicitly configured reachable seeded SmartRail backend using `flutter test integration_test/journey_status -d <android-emulator-id> --dart-define=SMART_RAIL_API_BASE_URL=... --dart-define=SMART_RAIL_TEST_ORIGIN_STATION_ID=... --dart-define=SMART_RAIL_TEST_SERVICE_ID=... --dart-define=SMART_RAIL_TEST_DATE=...`; capture actual origin/service/date/journey IDs and displayed-versus-response evidence, and mark PASS only after execution/assertions succeed (SC-001, SC-002, SC-003, SC-004, SC-007; SR-007/010/019/021/022/089; depends on T061–T062).
- [ ] T064 Record integration status and prerequisites in the existing `specs/002-journey-status-updates/verification-matrix.md` and actual configured instructions in `specs/002-journey-status-updates/quickstart.md`; record PASS/FAIL only from executed T063 evidence, BLOCKED for a named unavailable prerequisite, or NOT RUN when not attempted. This reporting task may proceed while T063 remains unchecked and must never turn mocked evidence into a real-integration PASS (SC-001–007; SR-089/109/114; depends on T060–T062; coordinate status recording after any T063 attempt).

**Gate**: T063 is mandatory acceptance when its prerequisites are available. If blocked, leave it unchecked and record BLOCKED evidence; do not count mocked tests as a pass. Physical-device smoke is supplementary unless approved sources change the requirement.

---

## Phase 9: Final Verification and Traceability

**Purpose**: Verify the whole app, confirm approved scope and record remaining gates honestly.

- [ ] T065 Run `flutter pub get` from the repository root and record the command/result in `specs/002-journey-status-updates/verification-matrix.md`; do not change dependency versions or add packages for this feature. This local check does not depend on real-backend integration T063 or status-recording T064 (FR-017; all applicable SR; depends on T001–T062).
- [ ] T066 Run `flutter analyze` from the repository root and record PASS/FAIL plus output summary in `specs/002-journey-status-updates/verification-matrix.md` (SC-005; SR-100/102/103/104/114; depends on T065).
- [ ] T067 Run `flutter test` from the repository root and record total pass/fail counts in `specs/002-journey-status-updates/verification-matrix.md`; include API, DTO, repository, state, widget, accessibility, navigation, and Feature 001 regression suites, including synthetic sentinel redaction assertions (SC-001–SC-006; SR-007/010/019/021/022/025/089/100/102/103/104/109/114; depends on T066).
- [ ] T068 Re-run `flutter test test/features/railway_search` from the repository root and record explicit Feature 001 regression result in `specs/002-journey-status-updates/verification-matrix.md`; any failure blocks acceptance and must not be hidden by the Feature 002 test result (FR-001, FR-015, FR-017; SR-013/089/113; depends on T067).
- [ ] T069 Audit every `FR-001`–`FR-017`, `SC-001`–`SC-007`, and applicable Must/Should SR row against task IDs and evidence in `specs/002-journey-status-updates/verification-matrix.md`; preserve SR-103/SR-104 as Should Have and record incomplete evidence honestly. Explicitly report T063 as BLOCKED or NOT RUN when applicable and state that mandatory real-backend acceptance remains incomplete; audit can proceed without a T063 PASS (all FR/SC; applicable SR; depends on T060–T062, T064–T068).
- [ ] T070 Review `specs/002-journey-status-updates/contracts/journey-status-client.md`, `data-model.md`, and `tasks.md` against the canonical SmartRail OpenAPI and app source; confirm exactly three public GETs, no Authorization/write operations, correct ID separation, no fabricated ETA/polling, no exposed private fields, and `git diff --check` clean; record remaining gates in `verification-matrix.md`. State explicitly when mandatory T063 acceptance is BLOCKED/NOT RUN; never infer success from mocks or local checks (FR-001–FR-017, SC-001–SC-007; all applicable SR; depends on T069).

---

## Requested Implementation Phase Mapping

Task phases remain grouped by the four P1 user stories for independent implementation and acceptance. The requested implementation work packages map to them as follows:

| Requested package | Task coverage |
|---|---|
| 1 — Contract models and API foundation | Setup T001–T005 plus foundational T006–T019 |
| 2 — Journey lookup and selection | User Story 1, T020–T026 |
| 3 — Official journey status | User Story 2 snapshot/status work, T027–T028 and T031–T033 |
| 4 — Update history and alerts | User Story 2 history, T029–T030; User Story 4 alert detection/presentation, T042–T049 |
| 5 — Recovery and navigation | User Story 3 refresh/freshness, T034–T041; cross-story recovery/navigation, T050–T056 |
| 6 — Accessibility and UI integration | Story UI tasks T024–T026, T029–T032, T038–T039, T047–T048; accessibility/integration T053–T059 |
| 7 — Real backend integration | T061–T064 |
| 8 — Final verification | T065–T070 |

---
## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: Independent test fixtures and fail-closed integration configuration; T001–T003 can run in parallel, T004 precedes T005.
- **Foundational (Phase 2)**: T006–T019 depend on setup fixtures and complete before any user story. DTO/domain/client/repository/provider interfaces must be stable before parallel story implementation.
- **US1 (Phase 3)**: Depends on the repository/provider foundation. Candidate lookup and selection can be completed/tested without routing integration; T054/T056 later connect the entry point.
- **US2 (Phase 4)**: Depends on foundation, not on candidate screen implementation if tests inject a selected journey ID. Its status/history view is independently testable with fixtures.
- **US3 (Phase 5)**: Depends on US2 state ownership and section models (T028); can proceed alongside US1 once foundational APIs are complete.
- **US4 (Phase 6)**: Depends on update/domain models (foundation), then uses US2 history state and US3 refresh/session entry (T035); cannot complete before those interfaces.
- **Cross-story integration (Phase 7)**: Depends on journey screens and providers. Shared app/router/Feature 001 entry files are serial single-owner changes.
- **Real backend (Phase 8)**: Depends on routes, entry, screens, fixtures/config, and matrix. T063 cannot pass without a reachable backend, seeded linked journey, and Android emulator.
- **Final verification (Phase 9)**: Local checks depend on implementation and test preparation, not on a successful backend/emulator run. T063 remains a separate mandatory real-integration acceptance task. T064 records its evidence status, including BLOCKED/NOT RUN, and T069–T070 must explicitly report incomplete mandatory acceptance when T063 did not pass.

### User Story Dependencies

- **US1 (P1)**: Can be implemented after foundational repository/provider tasks. Its real in-app path requires later shared routing/Feature 001 entry integration (T054/T056).
- **US2 (P1)**: Can be implemented after foundational repository/provider tasks; uses a fake selected journey identity in isolated tests.
- **US3 (P1)**: Depends on US2's section state and repository reads.
- **US4 (P1)**: Depends on US2 history models/view and US3's explicit refresh/view-entry events; it has no polling dependency.

### Genuine Parallel Opportunities

- Setup fixtures T001–T003 are independent files. Config test T004 can be written independently; config implementation T005 follows it.
- In Foundation, DTO parser tests T006, domain tests T008, and API request tests T012 target separate files and can begin in parallel after fixture format is agreed. T010/T011 are exclusively owned by the integration orchestrator because they change shared Feature 001 failure mapping. Their corresponding implementations follow their tests.
- US1 lookup tests T020 and candidate-selection tests T022 are independent. US2 snapshot state tests T027 and history widget tests T029/status widget tests T031 can be prepared independently after domain interfaces exist.
- US3 metadata tests T036 and refresh interaction tests T038 are separate files after state interfaces are agreed. US4 tracker tests T042, classification tests T044, and alert widget tests T047 can be parallelized before shared notifier/screen integration.
- Real UI integration test files T061 and T062 can be authored separately only after their app routes, providers, screens and Feature 001 entry action compile; the emulator run T063 is serial after both. T064 may record status independently of a T063 PASS. Local verification T065–T068 does not depend on T063/T064.
- **Exclusive ownership**: T010–T011 and shared app composition/navigation/Feature 001 entry tasks T017–T019, T054–T056, T059–T060, T063–T070 belong to the integration orchestrator. Agent A does not touch `lib/core/network/api_failure.dart` or `test/core/network/api_failure_mapping_test.dart`. Agent C exclusively owns its shared notifier/status-screen edits and serializes T028/T035/T039/T046/T048 within that stream. Agent D writes accessibility/integration tests only; T058 screen edits are a later serial integration-orchestrator task. Never run concurrent agents against `tasks.md`, `verification-matrix.md`, or the same source/test file.

## Parallel Example: Independent Workstreams

Use these workstreams with the stated prerequisites and exclusive file ownership. Shared-tree concurrent changes to the same file are prohibited. T001–T003 fixture files and T004–T005 configuration are owned by Agent D and must finish before dependent model/API work. Shared interfaces are frozen after T006–T019 before story streams begin.

```text
Agent A — Journey DTO/domain/API/repository implementation and isolated tests:
T006–T009 and T012–T016; own only journey-specific DTO/domain/API/repository source and tests. Do not own T010–T011 or modify `lib/core/network/api_failure.dart` / `test/core/network/api_failure_mapping_test.dart`.

Agent B — Journey lookup and candidate selection:
T020–T026; own lookup/selection state and candidate/lookup presentation files and corresponding tests only.

Agent C — Snapshot/history refresh and alert detection:
T027–T049; own snapshot/history/refresh/session state and status/history/alert presentation files and corresponding tests. Serialize tasks touching `journey_status_notifier.dart` and `journey_status_screen.dart` within this workstream.

Agent D — Fixtures, accessibility test authoring and integration-test preparation:
T001–T005, T057, T061–T062; own fixture/config, accessibility test and integration test files only. T057 follows T026/T032/T048/T052 so test references compile. T061/T062 follow the listed routes, providers, screens and entry action. Do not edit presentation source files.

Integration orchestrator — Shared failures, composition, accessibility integration and evidence:
T010–T011, T017–T019, T050–T056, T058–T060, T063–T070; exclusively own shared failure mapping, app providers/router, Feature 001 entry, serial accessibility screen edits, shared merges, checkboxes and verification matrix. T058 follows completed screen implementations; T063 is a real-backend emulator run, and BLOCKED/NOT RUN does not prevent T065–T070.
```

### File Ownership and Merge Order

| Files / work | Exclusive owner | Merge or prerequisite rule |
|---|---|---|
| `lib/core/network/api_failure.dart`, `test/core/network/api_failure_mapping_test.dart` (T010–T011) | Integration orchestrator | Preserve Feature 001 mappings; finish before T013 and shared failure tests. |
| Journey DTO/domain/API/repository files and isolated tests (T006–T009, T012–T016) | Agent A | Depends on fixtures and frozen DTO/repository interfaces; no shared failure files. |
| Lookup/candidate state, screens and tests (T020–T026) | Agent B | Merge after foundation; presentation imports must compile before router integration. |
| Snapshot/history/refresh/alert state, screens and tests (T027–T049) | Agent C | One owner serializes shared notifier/status-screen edits; merge after foundation. |
| Fixtures/config/accessibility and UI integration test files (T001–T005, T057, T061–T062) | Agent D | Test files are authored only after their referenced interfaces/routes/screens exist; no presentation source edits. |
| App providers/router, Feature 001 detail entry, recovery integration and T058 (T017–T019, T050–T056, T058–T060) | Integration orchestrator | Merge story outputs first; T058 is serial after T025–T026, T032, T039 and T048. |
| Emulator run and verification evidence (T063–T070) | Integration orchestrator | T063 is independent mandatory acceptance; blocked/not-run status does not gate local T065–T068. |

## Implementation Strategy

### MVP First

Deliver US1 first: complete T001–T026, then the minimum entry route and Feature 001 details integration in T053–T056. Validate anonymous lookup, no-match, unambiguous open, ambiguous explicit selection, and Back before moving on. Do not claim Feature 002 complete until mandatory status/history, freshness, alert, error/accessibility requirements and real-backend emulator acceptance are covered.

### Incremental Delivery

1. Complete setup and foundation (T001–T019).
2. Deliver and independently test US1 lookup/selection (T020–T026), then connect it to Feature 001 (T053–T056).
3. Deliver US2 official status/history (T027–T033).
4. Deliver US3 refresh/freshness/recovery (T034–T041).
5. Deliver US4 session-scoped alerts (T042–T049).
6. Complete accessibility, integration, regression, and final evidence (T050–T070).

## Verification Status Rules

- **PASS**: Executed and all assertions passed with command/device and evidence recorded.
- **FAIL**: Executed and one or more assertions failed; retain failure evidence.
- **BLOCKED**: Not executable because a named external prerequisite is unavailable; identify it explicitly.
- **NOT RUN**: Not attempted or supplementary and intentionally not run.

BLOCKED and NOT RUN are never acceptance passes. T063 must remain unchecked unless the actual Android emulator integration passes against a real reachable seeded backend. Do not mark tasks complete without evidence.

## Notes

- Every task ID T001–T070 is sequential and starts unchecked. SR-098 remains Should Have/stretch and does not gate the MVP or Must Have acceptance.
- Every task names exact source/test/doc paths, carries a user-story label in story phases, and includes relevant FR/SR or SC traceability plus verification intent.
- No task adds a backend route, modifies the approved contract, installs packages, implements authentication, exposes protected data, or changes the Feature 001 search behavior.
- No physical-device acceptance task is mandatory; emulator plus real backend is primary and physical-device smoke remains supplementary.






