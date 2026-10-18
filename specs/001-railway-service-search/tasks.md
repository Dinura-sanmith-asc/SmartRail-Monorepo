---
description: "Dependency-ordered implementation tasks for Railway Search and Service Details"
---

# Tasks: Railway Search and Service Details

**Branch**: `001-railway-service-search`  
**Input**: Design documents in `specs/001-railway-service-search/`

**Prerequisites**: Approved `spec.md` and `plan.md`; `research.md`, `data-model.md`,
`contracts/railway-network-client.md`, and `quickstart.md` provide implementation detail.

**Organization**: The requested eight implementation phases are retained. Tasks in
story-specific phases use `[US1]`, `[US2]`, or `[US3]`; shared foundation/final tasks map
their story and requirement coverage in each task description.

**Requirement shorthand**: US1 = Search scheduled services (P1); US2 = Inspect a scheduled
service (P1); US3 = Recover from unavailable information (P2). SR-103 and SR-104 remain
Should Have stretch requirements.

## Phase 1 — Flutter Foundation

**Purpose**: Add the approved client dependencies and establish the app shell, environment
configuration, feature structure, and Android network access.

- [X] T001 Add `flutter_riverpod ^3.4.3`, `go_router ^18.1.0`, `http ^1.6.0`, and `intl ^0.20.3` to `pubspec.yaml`; do not add code-generation or mock packages. Trace: US1/US2/US3; FR-001, FR-020; SR-089, SR-114.
- [X] T002 Resolve the approved package set with `flutter pub get` and commit the compatible resolution in `pubspec.lock`. Trace: US1/US2/US3; FR-001, FR-020; SR-089, SR-114.
- [X] T003 [P] Write environment configuration tests in `test/app/app_config_test.dart` for a valid configured API base URL, missing value, malformed URL, and rejection of a production localhost default. Trace: US1/US2/US3; FR-001, FR-020; SR-089.
- [X] T004 Implement validated compile-time `SMART_RAIL_API_BASE_URL` configuration in `lib/app/app_config.dart`; keep environment values out of source constants and secrets out of this public-read config. Depends on T003. Trace: US1/US2/US3; FR-001, FR-020; SR-089.
- [X] T005 [P] Establish shared app theme and reusable loading, empty, validation, unavailable, and access-denied presentation primitives in `lib/app/theme/app_theme.dart` and `lib/core/widgets/async_feedback.dart`. Trace: US1/US2/US3; FR-010–019; SR-103, SR-104, SR-109, SR-114.
- [X] T006 Configure declarative Search, Results, and Details routes and the app composition shell in `lib/app/router.dart` and `lib/app/app.dart`; keep a push/pop stack and add no authentication redirect. Depends on T001, T004, T005, T008. Trace: US1/US2/US3; FR-001, FR-017, FR-022; SR-089, SR-114.
- [X] T007 Replace the generated counter startup with `ProviderScope` and `MaterialApp.router` in `lib/main.dart`; preserve the project entry point and initialize only configured dependencies. Depends on T006. Trace: US1/US2/US3; FR-001, FR-020; SR-089.
- [X] T008 Establish the feature-first source boundary and compile-safe Search, Results, and Details route placeholder screens in `lib/features/railway_search/presentation/railway_search_screen.dart`, `lib/features/railway_search/presentation/search_results_screen.dart`, and `lib/features/railway_search/presentation/service_detail_screen.dart`; each placeholder must be constructible by the router. Follow the `lib/features/railway_search/{presentation,application,domain,data}` structure in `plan.md`. Depends on T001. Trace: US1/US2; FR-001, FR-008, FR-017; SR-003, SR-013.
- [X] T009 [P] Add Android Internet permission to `android/app/src/main/AndroidManifest.xml` and verify release configuration does not enable broad cleartext traffic; any required local HTTP exception must be debug-only. Trace: US1/US2/US3; FR-001, FR-020; SR-089, SR-114.

**Checkpoint**: App shell, route placeholders, validated URL configuration, resolved approved
packages, and Android release network permission are ready for API work.

---

## Phase 2 — API Client and Models

**Purpose**: Create strict typed mappings for the existing four public reads and deterministic
contract/error tests. Use only `docs/contracts/railway-network.openapi.yaml`.

- [X] T010 [P] Write fixture-based DTO tests in `test/features/railway_search/data/railway_api_dtos_test.dart` for required fields, optional station detail arrays, empty collections, UUID strings, ISO full dates, and malformed types. Trace: US1/US2; FR-002, FR-004, FR-006, FR-008, FR-020; SR-002, SR-003, SR-004, SR-013.
- [X] T011 Implement strict DTO parsing for `StationList`, `Station`, `RouteStops`, `RouteStop`, `ServiceSearchResponse`, `ServiceChoice`, `ScheduledCall`, and `ServiceDetail` in `lib/features/railway_search/data/railway_api_dtos.dart`, matching required/optional OpenAPI fields exactly. Depends on T010. Trace: US1/US2; FR-002, FR-004, FR-006, FR-008, FR-020; SR-002, SR-003, SR-004, SR-013.
- [X] T012 [P] Write RFC 9457 parsing and error-classification tests in `test/core/network/problem_details_parser_test.dart` for required `type/title/status`, optional `detail/instance/code/errors/traceId`, malformed bodies, and safe fallbacks. Trace: US1/US2/US3; FR-011–016, FR-020; SR-114.
- [X] T013 Implement typed API failures and safe Problem Details parsing in `lib/core/network/api_failure.dart` and `lib/core/network/problem_details_parser.dart`; never expose stack traces, internal details, or trace IDs to passengers. Depends on T012. Trace: US1/US2/US3; FR-011–016, FR-020; SR-114.
- [X] T014 [P] Write MockClient request/response tests in `test/features/railway_search/data/railway_network_api_test.dart` for exact URI paths, query names, full-date formatting, anonymous headers, status codes, and response content types; also write shared-client lifecycle tests in `test/core/network/http_api_client_lifecycle_test.dart` proving app-scope ownership and exactly-once close on provider-container disposal. Trace: US1/US2/US3; FR-001, FR-004, FR-005, FR-008, FR-020, FR-022; SR-013, SR-089, SR-109, SR-114.
- [X] T015 Implement one injectable bounded-timeout HTTP transport in `lib/core/network/http_api_client.dart` and an app-scoped `railwayApiClientProvider` in `lib/app/providers.dart`; the provider owns the shared `http.Client`, registers `Client.close()` with `ref.onDispose`, and is the only owner that closes it. Keep request-generation/disposal checks as the late-response correctness guard. Depends on T002, T013, T014. Trace: US1/US2/US3; FR-010, FR-013–016, FR-022; SR-109, SR-114.
- [X] T016 Implement only `GET /api/stations`, `GET /api/routes/{id}/stops`, `GET /api/services?origin=&destination=&date=`, and `GET /api/services/{id}` in `lib/features/railway_search/data/railway_network_api.dart`; do not add headers requiring login or any other route. Depends on T011, T014, T015. Trace: US1/US2/US3; FR-001, FR-002, FR-005, FR-008, FR-020; SR-002, SR-003, SR-004, SR-013, SR-089.
- [X] T017 Define immutable domain models and validation constraints in `lib/features/railway_search/domain/railway_models.dart`: `Station.name` has `minLength: 1`; `TravelDate` is calendar-only `YYYY-MM-DD`; `SearchCriteria` is submittable only when origin, destination, and date exist and station IDs differ; `RouteStop.stopOrder`, `ServiceChoice.scheduleRevision`, and `ServiceDetail.schedule.revision` are integers with `minimum: 1`. Depends on T011. Trace: US1/US2; FR-002–009; SR-002, SR-003, SR-004, SR-013.
- [X] T018 [P] Write literal scheduled-time parsing/display tests in `test/features/railway_search/domain/scheduled_timestamp_test.dart` proving the raw offset-bearing timestamp, local date, and clock are preserved without device-timezone conversion. Trace: US1/US2; FR-006, FR-009; SR-004, SR-013.
- [X] T019 Implement `ScheduledTimestamp` in `lib/features/railway_search/domain/scheduled_timestamp.dart`; preserve raw wire text and derive display components without normalizing to UTC/device time. Depends on T018. Trace: US1/US2; FR-006, FR-009; SR-004, SR-013.
- [X] T020 Define the `RailwaySearchRepository` interface in `lib/features/railway_search/domain/railway_search_repository.dart` for station listing, service search, service detail, and route stops, using domain models and typed failures. Depends on T017, T019. Trace: US1/US2/US3; FR-001–017, FR-020; SR-002, SR-003, SR-004, SR-013, SR-089, SR-109, SR-114.
- [X] T021 Implement `RailwaySearchRepositoryImpl` in `lib/features/railway_search/data/railway_search_repository_impl.dart`, mapping DTOs to domain values and keeping service detail and route-stop calls independently recoverable. Depends on T013, T016, T020. Trace: US1/US2/US3; FR-001–017, FR-020; SR-002, SR-003, SR-004, SR-013, SR-089, SR-109, SR-114.

**Checkpoint**: The four reads and typed boundary are mock-testable without UI or live data.

---

## Phase 3 — Search and Station Selection (US1, Priority: P1)

**Goal**: Let an anonymous passenger load stations and choose distinct origin/destination
stations and a travel date.

**Independent Test**: With a fake repository, station choices load; empty/failure states are
distinct; missing or identical criteria block submission; valid selected date remains the
same calendar date.

- [ ] T022 [US1] Write Riverpod search-state, initial Search screen, and route tests in `test/features/railway_search/application/search_controller_test.dart`, `test/features/railway_search/presentation/railway_search_screen_test.dart`, and `test/features/railway_search/presentation/railway_search_navigation_test.dart` for station loading/success/empty/failure/retry, selection retention, missing fields, identical station IDs, date serialization, anonymous initial/loading/error UX, and no-login navigation. Use a fake/spy repository with a `search` invocation counter and assert exactly zero `repository.search` calls separately for missing origin, missing destination, missing travel date, and identical origin/destination. Trace: US1; FR-001–005, FR-010–011, FR-017–018; SR-002, SR-013, SR-089, SR-114; SC-002.
- [ ] T023 [US1] Implement station loading, origin/destination/date selection, local validation, and retry transitions in `lib/features/railway_search/application/search_controller.dart`; do not infer date eligibility. Depends on T020, T022. Trace: US1; FR-002–005, FR-010–011; SR-002, SR-013, SR-114.
- [ ] T024 [US1] Implement accessible searchable station selection controls in `lib/features/railway_search/presentation/station_picker.dart`, exposing station names and only returned approved location/service details. Depends on T017, T023. Trace: US1; FR-002–003, FR-018; SR-002, SR-013, SR-103.
- [ ] T025 [US1] Implement the Search screen in `lib/features/railway_search/presentation/railway_search_screen.dart` with unset initial criteria, date picker, loading/empty/unavailable station states, retry, and understandable field/pair validation. Depends on T005, T023, T024. Trace: US1; FR-001–005, FR-010–011, FR-018–019; SR-002, SR-013, SR-103, SR-104, SR-114.
- [ ] T026 [US1] Run the prewritten Search screen suite in `test/features/railway_search/presentation/railway_search_screen_test.dart` and fix failures in `lib/features/railway_search/presentation/railway_search_screen.dart`; verify two distinct station choices, date choice, retry, and validation. Confirm the fake/spy repository search counter is exactly zero in each invalid-input case: missing origin, missing destination, missing travel date, and identical origin/destination. Depends on T025. Trace: US1; FR-001–005, FR-010–011; SR-002, SR-013, SR-089, SR-114; SC-002.
- [ ] T027 [US1] Run the prewritten Search route/provider tests in `test/features/railway_search/presentation/railway_search_navigation_test.dart`; station-load retry must not require login and must not leave a stale loading state. Depends on T006, T025. Trace: US1; FR-001–003, FR-010, FR-017–018; SR-002, SR-013, SR-089, SR-114.

**Checkpoint**: US1 station/date entry is independently reviewable using fake data.

---

## Phase 4 — Service Search Results (US1, Priority: P1)

**Goal**: Submit valid criteria once and present only the scheduled choices returned by the
backend, or a distinct successful empty state.

**Independent Test**: A fake repository returns matching choices or an empty collection;
displayed values/order match the response; edits clear results and late/duplicate responses
cannot display stale results.

- [ ] T028 [US1] Write result/search transition and Results screen/card tests in `test/features/railway_search/application/search_results_notifier_test.dart`, `test/features/railway_search/presentation/search_results_screen_test.dart`, and `test/features/railway_search/presentation/service_result_card_test.dart` for submit, loading, success, empty, 400, 403, 503, duplicate submit, criteria edit clearing, out-of-order responses, displayed values/order, and retry. Card tests must verify the supplied planned date and time, the later-day/“next day” label, and preservation of the supplied local clock without device-timezone conversion. Depends on T023. Trace: US1/US3; FR-005–007, FR-009–016, FR-021–022; SR-004, SR-013, SR-089, SR-109, SR-114; SC-001, SC-003, SC-004.
- [ ] T029 [US1] Implement submission/result state in `lib/features/railway_search/application/search_results_notifier.dart`; send only origin ID, destination ID, and selected origin travel date, and accept a response only for its matching criteria generation. Depends on T020, T023, T028. Trace: US1; FR-004–007, FR-010–011, FR-021–022; SR-013, SR-114.
- [ ] T030 [P] [US1] Implement service result cards in `lib/features/railway_search/presentation/service_result_card.dart`, presenting returned service/route identifiers and planned origin/destination calls without sorting or recalculation. Depends on T017, T019. Trace: US1; FR-006, FR-009, FR-018–019; SR-004, SR-013, SR-103, SR-104.
- [ ] T031 [US1] Implement the Results screen in `lib/features/railway_search/presentation/search_results_screen.dart` with loading, populated, and 200-empty states, retained criteria, retry, and change-criteria navigation. Depends on T029, T030. Trace: US1/US3; FR-006–007, FR-010–016, FR-017; SR-004, SR-013, SR-114.
- [ ] T032 [US1] Integrate Search submission with Results navigation in `lib/features/railway_search/presentation/railway_search_screen.dart`; clear the previous result snapshot immediately on any criterion edit and require an explicit resubmission. Depends on T029, T031. Trace: US1; FR-005–007, FR-017, FR-021; SR-013.
- [ ] T033 [US1] Run the prewritten Results widget and result-card suites in `test/features/railway_search/presentation/search_results_screen_test.dart` and `test/features/railway_search/presentation/service_result_card_test.dart`; fix failures in `lib/features/railway_search/presentation/search_results_screen.dart` and `lib/features/railway_search/presentation/service_result_card.dart`. Verify HTTP 200 with an empty `services` collection shows the no-results state and is not confused with validation, backend, or network failures; also verify planned date/time and later-day labels preserve backend values without device-timezone conversion. Depends on T031. Trace: US1/US3; FR-006–007, FR-009–016; SR-004, SR-013, SR-114; SC-001, SC-003, SC-004.
- [ ] T034 [US1] Run the prewritten criteria-edit and duplicate/late-response suite in `test/features/railway_search/application/search_results_notifier_test.dart`; assert previous results clear immediately and abandoned responses cannot reopen Results. Depends on T029, T032. Trace: US1; FR-017, FR-021–022; SR-013, SR-114.

**Checkpoint**: Search results are independently reviewable with deterministic responses.

---

## Phase 5 — Service Details (US2, Priority: P1)

**Goal**: Open a selected result, load service metadata and ordered route stops independently,
and return to the same search/results state.

**Independent Test**: Fake the two API reads separately; verify planned fields, ordered stops,
partial success, targeted retry, next-day cue, and Back restoration.

- [ ] T035 [US2] Write service-detail state, widget, and navigation tests in `test/features/railway_search/application/service_detail_notifier_test.dart`, `test/features/railway_search/presentation/service_detail_screen_test.dart`, and `test/features/railway_search/presentation/service_detail_navigation_test.dart` for independent outcomes, empty stops, partial failures, one-section retry, planned data, next-day cue, duplicate reads, and Back restoration/late completion. Depends on T032. Trace: US2/US3; FR-008–017, FR-022; SR-003, SR-004, SR-109, SR-114.
- [ ] T036 [US2] Implement independent service-detail and route-stop reads in `lib/features/railway_search/application/service_detail_notifier.dart` using result `serviceId` and `routeId`; retain the search summary and ignore completions after route departure. Depends on T020, T021, T035. Trace: US2/US3; FR-008, FR-010, FR-012–017, FR-022; SR-003, SR-004, SR-109, SR-114.
- [ ] T037 [P] [US2] Implement service identity and schedule sections in `lib/features/railway_search/presentation/service_detail_summary.dart`; label result origin/destination calls and `firstDepartureTimeLocal` as planned data and keep them distinct. Depends on T017, T019. Trace: US2; FR-006, FR-008–009; SR-004.
- [ ] T038 [P] [US2] Implement ordered route stops in `lib/features/railway_search/presentation/route_stops_list.dart`, sorting/presenting by contract `stopOrder` only and showing an explicit empty-stop state without invented times. Depends on T017. Trace: US2; FR-008–009; SR-003.
- [ ] T039 [US2] Implement the Details route/screen in `lib/features/railway_search/presentation/service_detail_screen.dart` with independent service and stop sections, loading feedback, unavailable section messaging, retry, and Back. Depends on T006, T036–T038. Trace: US2/US3; FR-008, FR-010, FR-012–017, FR-022; SR-003, SR-004, SR-109, SR-114.
- [ ] T040 [US2] Run the prewritten Details widget tests in `test/features/railway_search/presentation/service_detail_screen_test.dart` and fix failures in `lib/features/railway_search/presentation/service_detail_screen.dart`; verify both partial-failure directions and schedule labels. Depends on T039. Trace: US2/US3; FR-008–009, FR-012–016; SR-003, SR-004, SR-114.
- [ ] T041 [US2] Run the prewritten Results → Details → Back tests in `test/features/railway_search/presentation/service_detail_navigation_test.dart`; verify the exact criteria and result list remain restored. Depends on T032, T039. Trace: US1/US2; FR-017, FR-022; SR-013, SR-004.

**Checkpoint**: US2 details are independently reviewable, including partial data and Back.

---

## Phase 6 — Error Handling and Quality (US3, Priority: P2; cross-cutting checks)

**Goal**: Ensure all failure classes produce accurate, accessible recovery actions and that
stale/previously loaded data is not presented as newly refreshed or live.

**Independent Test**: Inject each documented HTTP/transport outcome into search and detail;
verify it is distinct from empty success and only appropriate recovery actions are offered.

- [ ] T042 [US3] Write end-to-end error mapping tests in `test/core/network/api_failure_mapping_test.dart` covering search 400, detail path-ID 400, conditional 403, detail 404, 500/default, 503, malformed body, timeout, and network loss. Trace: US1/US2/US3; FR-011–016, FR-020; SR-089, SR-114.
- [ ] T043 [US3] Write recovery-state, recovery-widget, pending-navigation, and provider-disposal tests in `test/features/railway_search/application/recovery_state_test.dart`, `test/features/railway_search/presentation/recovery_feedback_test.dart`, `test/features/railway_search/presentation/pending_navigation_test.dart`, and `test/features/railway_search/application/provider_disposal_test.dart` for criteria retention, explicit retries, retained sibling detail data, local last-retrieved time, not-refreshed indicators, Back during loading, late-response suppression, and ignoring completions after notifier disposal. Depends on T029, T036. Trace: US1/US2/US3; FR-011–017, FR-022; SR-109, SR-114.
- [ ] T044 [US3] Implement status-to-passenger-message mapping in `lib/features/railway_search/application/recovery_message_mapper.dart`; search 400 remains correctable, detail 400/404 affects only its section, 403 is Back-only without login/retry, and 500/503/network failures can retry the failed read. Depends on T013, T042. Trace: US1/US2/US3; FR-011–015; SR-089, SR-114.
- [ ] T045 [US3] Implement retained-data freshness metadata in `lib/features/railway_search/application/retrieval_metadata.dart`; label retained data “not refreshed” and use client retrieval time only, never a source update time. Depends on T043. Trace: US1/US2/US3; FR-016; SR-109.
- [ ] T046 [US3] Implement retry/back and accessible unavailable/validation messaging in `lib/core/widgets/async_feedback.dart` and `lib/features/railway_search/presentation/retry_section.dart`; expose no retry or login action on 403. Depends on T044, T045. Trace: US1/US2/US3; FR-010–019; SR-089, SR-103, SR-109, SR-114.
- [ ] T047 [US3] Run recovery widget tests in `test/features/railway_search/presentation/recovery_feedback_test.dart` and fix failures in `lib/features/railway_search/presentation/retry_section.dart` and `lib/core/widgets/async_feedback.dart` for 400, 403, 404, 500, 503, no-network, safe text, and retry/back affordances. Depends on T046. Trace: US1/US2/US3; FR-007, FR-011–016; SR-089, SR-114.
- [ ] T048 [US3] Run pending-navigation and provider-disposal tests in `test/features/railway_search/presentation/pending_navigation_test.dart` and `test/features/railway_search/application/provider_disposal_test.dart`, proving Back remains available during search/detail loading, late completions neither mutate nor reopen departed routes, and disposed notifiers ignore completions. Shared-client close-once behavior is verified by T014. Depends on T029, T036, T043. Trace: US1/US2/US3; FR-017, FR-022; SR-013, SR-109, SR-114.
- [ ] T049 [P] [US1] Write semantics/accessibility tests in `test/features/railway_search/presentation/search_accessibility_test.dart` for accessible names, selected/disabled state, focus order, and loading/result/error announcements. Trace: US1/US2/US3; FR-018–019; SR-103 (Should Have), SR-104 (Should Have).
- [ ] T050 [US1] Improve reusable controls for text scaling, legibility, non-color status cues, and operable touch targets in `lib/core/widgets/async_feedback.dart`, `lib/features/railway_search/presentation/station_picker.dart`, `lib/features/railway_search/presentation/railway_search_screen.dart`, `lib/features/railway_search/presentation/search_results_screen.dart`, `lib/features/railway_search/presentation/service_result_card.dart`, `lib/features/railway_search/presentation/service_detail_summary.dart`, `lib/features/railway_search/presentation/route_stops_list.dart`, `lib/features/railway_search/presentation/service_detail_screen.dart`, and `lib/features/railway_search/presentation/retry_section.dart`; do not elevate SR-103/SR-104 from Should Have. Depends on T049. Trace: US1/US2/US3; FR-018–019; SR-103 (Should Have), SR-104 (Should Have); SC-006.

**Checkpoint**: Search and detail failure behavior is distinct, tested, and reviewable.

---

## Phase 7 — Real Backend Integration

**Purpose**: Verify true anonymous reads against a reachable, seeded SmartRail backend; mock
responses do not count as integration evidence.

- [ ] T051 Create device-backed integration test setup in `integration_test/railway_search/integration_test_config.dart` that requires explicit `SMART_RAIL_API_BASE_URL`, fails fast if absent, and never silently substitutes mock data. Trace: US1/US2; FR-001, FR-020; SR-013, SR-089.
- [ ] T052 [P] [US1] Add anonymous station selection and service-search integration coverage in `integration_test/railway_search/anonymous_service_search_test.dart`; compare displayed IDs, criteria, and planned calls with the actual API response. Depends on T051. Trace: US1; FR-001–007, FR-020–021; SR-002, SR-004, SR-013, SR-089.
- [ ] T053 [P] [US2] Add real service-detail and route-stop integration coverage in `integration_test/railway_search/service_detail_test.dart`; compare service/route fields, stop order, schedule values, and independent section outcomes with actual responses. Depends on T051. Trace: US2; FR-008–009, FR-012–017, FR-020, FR-022; SR-003, SR-004.
- [ ] T054 [US1] Run the real-backend integration tests on an Android emulator against a reachable backend with seeded published data; record the environment identifier, seed references, command, and evidence in `specs/001-railway-service-search/verification-matrix.md`. If the backend URL or seed is unavailable, record `BLOCKED` and the missing prerequisite; do not report the unexecuted run as `PASS`. Use a host-reachable URL (`10.0.2.2` for a host-local Android emulator where applicable). Depends on T052, T053. Trace: US1/US2; FR-001–009, FR-020; SR-002, SR-003, SR-004, SR-013, SR-089; SC-001, SC-004, SC-005.
- [ ] T055 [US2] (Supplementary, non-gating) Run the anonymous search/detail smoke flow on a reachable physical device when available and record device/network evidence or `BLOCKED`/`NOT RUN` in `specs/001-railway-service-search/verification-matrix.md`; do not reuse emulator localhost. This task is not a dependency of primary integration acceptance unless an approved requirement establishes physical-device testing. Depends on T052, T053. Trace: US1/US2; FR-001–009, FR-020; SR-002, SR-003, SR-004, SR-013, SR-089.

**Checkpoint**: Mocked contract tests remain separate from recorded real-service evidence.

---

## Phase 8 — Final Verification

**Purpose**: Verify the resolved dependency graph, analysis/tests, device behavior, requirement
traceability, and all five clarification decisions. Do not invent latency or reliability targets.

- [ ] T056 Re-run `flutter pub get` and verify the committed `pubspec.lock` resolves the package constraints in `pubspec.yaml` without introducing unapproved dependencies. Depends on T001. Trace: US1/US2/US3; FR-001, FR-020; SR-089, SR-114.
- [ ] T057 Run `flutter analyze` and record the result in `specs/001-railway-service-search/verification-matrix.md`; resolve feature warnings/errors before acceptance. Depends on T001–T050. Trace: US1/US2/US3; FR-001–022; SR-002, SR-003, SR-004, SR-013, SR-089, SR-103, SR-104, SR-109, SR-114.
- [ ] T058 Run all unit, API-client, state, widget, navigation, and accessibility tests with `flutter test` and record pass/fail evidence in `specs/001-railway-service-search/verification-matrix.md`. Depends on T010–T050. Trace: US1/US2/US3; FR-001–022; SR-002, SR-003, SR-004, SR-013, SR-089, SR-103, SR-104, SR-109, SR-114.
- [ ] T059 Run primary device-backed acceptance smoke tests with `flutter test integration_test/railway_search -d <android-emulator-id>` and the explicit seeded backend URL; record results in `specs/001-railway-service-search/verification-matrix.md`. If prerequisites are absent, record `BLOCKED` or `NOT RUN`, never `PASS`. Depends on T054. Trace: US1/US2; FR-001–009, FR-017, FR-020, FR-022; SR-002, SR-003, SR-004, SR-013, SR-089; SC-001, SC-004, SC-005.
- [ ] T060 Verify the five clarifications (edit clears results; scheduled date/time and next-day cue; partial detail recovery; 403 Back only; Back during loading/late suppression) against automated evidence and record each result in `specs/001-railway-service-search/verification-matrix.md`. Depends on T028, T034, T040–T050, T058. Trace: US1/US2/US3; FR-009, FR-014–017, FR-021–022; SR-013, SR-089, SR-109, SR-114.
- [ ] T061 Complete requirement-to-test traceability for all FR-001–FR-022, applicable SR-002/003/004/013/089/109/114, and SR-103/104 stretch status in `specs/001-railway-service-search/verification-matrix.md`; mark each result `PASS`, `FAIL`, `BLOCKED`, or `NOT RUN` with evidence or a reason. Include explicit SC mapping: SC-001 → T033/T054; SC-002 → T022/T026; SC-003 → T033/T042/T047; SC-004 → T033/T040/T054; SC-005 → T041/T054; SC-006 → T049/T050; SC-007 → T040/T047. Recording traceability does not require external integration prerequisites. Depends on T057, T058, T060. Trace: US1/US2/US3; FR-001–022; SR-002, SR-003, SR-004, SR-013, SR-089, SR-103, SR-104, SR-109, SR-114; SC-001–SC-007.
- [ ] T062 Record remaining environment, approval, or contract blockers and final quickstart commands in `specs/001-railway-service-search/quickstart.md`; do not mark blocked or unexecuted integration evidence as passed. This reporting can be completed when T054/T059 are `BLOCKED` or `NOT RUN`. Depends on T057, T058, T061. Trace: US1/US2/US3; FR-001–022; SR-002, SR-003, SR-004, SR-013, SR-089, SR-103, SR-104, SR-109, SR-114.

**Checkpoint**: Acceptance evidence and any remaining gates are explicit; no task is complete
until its evidence is reviewed.

---

## Dependencies and Execution Order

### Phase Dependencies

- **Phase 1 — Foundation**: Begins immediately. T001 → T002; T003 → T004; T008 creates all
  route placeholders before T006 imports/registers them; T004/T005/T008 → T006 → T007. T009
  only depends on T002. The Phase 1 checkpoint is a compiling app shell.
- **Phase 2 — API and Models**: Depends on T001–T002. Tests precede their implementation;
  T011/T013/T019 feed the transport, endpoint, and repository tasks T015–T021.
- **Phase 3 — Station Selection**: Depends on shared app/domain/repository foundation
  (T006–T008, T017, T020–T021).
- **Phase 4 — Search Results**: Depends on Phase 3 criteria state and Phase 2 API repository.
- **Phase 5 — Details**: Depends on the result's service/route IDs and Phase 2 models/API;
  Back restoration integrates the completed Results route.
- **Phase 6 — Recovery/Quality**: Depends on the search and detail states; its tests exercise
  US1, US2, and US3 with injected outcomes.
- **Phase 7 — Real Integration**: Emulator/backend integration is the primary path and
  depends on completed app/API/features plus a reachable seeded backend. Missing prerequisites
  are recorded as `BLOCKED`/`NOT RUN`; physical-device smoke is supplementary.
- **Phase 8 — Final Verification**: Local verification, traceability, and blocker reporting
  can complete independently of external integration execution; preserve separate mock-test
  and real-backend evidence.

### User Story Dependencies

- **US1 (P1)**: Phase 1 and 2 foundations, then Phases 3–4; independently reviewable with
  fakes before Phase 7 real-backend acceptance.
- **US2 (P1)**: Shared Phase 1/2 plus US1 result selection (T029–T032) to obtain service and
  route IDs; then Phase 5 detail work. Its data and partial-failure tests remain independent
  through fake repositories.
- **US3 (P2)**: Uses US1/US2 read states and shared typed failures. Recovery can be tested
  independently with fakes after the repository layer, but end-to-end wiring follows Phases
  3–5 and is completed in Phase 6.

### Explicit Task Dependencies

- T001 → T002 → T015–T016 → T021.
- T003 → T004; T008 → T006 → T007.
- T010 → T011; T012 → T013 → T015; T014 → T015/T016; T018 → T019. T015 closes its
  app-scoped client on provider-container disposal; T048 verifies notifier disposal behavior.
- T017/T019 → T020 → T021 → T022 → T023 → T025 → T026/T027.
- T028 → T029 → T031 → T032 → T033/T034.
- T035 → T036 → T039 → T040/T041.
- T042/T043 → T044/T045 → T046 → T047; T048 depends on T029/T036; T049 → T050.
- T051 → T052/T053 → T054 → T059. T055 is supplementary and independent of the primary path.
- T057/T058/T060 → T061 → T062; T061/T062 remain possible when T054/T059 are blocked or not
  run, and record those statuses without implying a pass. The task dependencies are also listed on each
  final-verification task.

### Genuine Parallel Opportunities

`[P]` is used only when files differ and prerequisites are complete:

- After T002, T003 (configuration tests), T005 (shared UI foundation), and T009 (Android
  manifest) can proceed independently; T004 waits for T003.
- In Phase 2, T010 (DTO tests), T012 (Problem parser tests), T014 (HTTP API tests), and T018
  (timestamp tests) touch separate files and can be authored independently; each production
  implementation task waits for its corresponding tests.
- After T023, station picker (T024) and app screen integration (T025) are separate files,
  but T025 consumes T024; therefore they should be completed in that order, not in parallel.
- In Phase 5, T037 (service summary) and T038 (route stop list) can proceed in parallel after
  their shared models exist; T039 composes them afterward.
- Integration tests T052 and T053 use separate files and can be prepared in parallel after
  T051; real emulator execution T054 requires both. T055 may be run independently as a
  supplementary physical-device smoke when its URL is reachable.

## Parallel Example: Independent Detail Presenters

```text
After T017/T019 are complete, implement in parallel:
Task: T037 service summary presenter in lib/features/railway_search/presentation/service_detail_summary.dart
Task: T038 ordered stops presenter in lib/features/railway_search/presentation/route_stops_list.dart
Then integrate both in T039 service_detail_screen.dart.
```

## Implementation Strategy

1. Complete Phase 1 and Phase 2 to establish the package/app shell and tested API boundary.
2. Deliver **US1 first** through Phases 3–4 as the MVP increment; validate it independently
   with fake data before adding details.
3. Add **US2** through Phase 5 and verify partial-detail recovery and Back restoration.
4. Complete **US3 and cross-cutting quality** through Phase 6.
5. Keep Phase 7 integration evidence separate from mocked tests; block only on missing
   reachable seeded backend/device prerequisites and record that state honestly.
6. Complete Phase 8 before claiming acceptance. Do not introduce backend/database work,
   timetable calculations, live status, or unapproved performance/reliability targets.

## Notes

- Every implementation task uses `- [ ] Tnnn` format and remains unchecked until completed.
- `[P]` marks only independent work in distinct files; `[US1]`, `[US2]`, `[US3]` map to the
  approved story labels in `spec.md`.
- Every task description includes concrete file paths and US/FR/SR traceability.
- The real API base URL and seeded data are external prerequisites; integration tests must
  fail fast if no URL is supplied and must not substitute mocks.
- Do not create backend endpoints or modify the approved OpenAPI contract.
