# Feature 002 Verification Matrix

**Feature**: `002-journey-status-updates`
**Spec**: [spec.md](spec.md)
**Task list**: [tasks.md](tasks.md)
**Initial status**: All evidence is `NOT RUN`. Update a status only with recorded execution/review evidence.

## Status rules

- **PASS**: Executed and every assertion passed; record command/device and evidence.
- **FAIL**: Executed and one or more assertions failed; preserve failure evidence.
- **BLOCKED**: Not executable because a named external prerequisite is unavailable; state that prerequisite.
- **NOT RUN**: Not attempted or supplementary and intentionally not run.

`BLOCKED` and `NOT RUN` are not passes. Never mark integration PASS based on mocks or a written test that was not executed. SR-098 is Should Have/stretch and is not an MVP or Must Have gate; SR-103 and SR-104 also retain Should Have priority.

## Success Criteria

| SC | Required outcome | Planned evidence | Status | Evidence / prerequisite |
|---|---|---|---|---|
| SC-001 | Selected service/date opens only a matching returned journey identity; no synthetic journeys. | `test/features/journey_status/application/journey_lookup_notifier_test.dart`; `test/features/journey_status/application/journey_candidate_selection_test.dart`; `integration_test/journey_status/journey_lookup_integration_test.dart` (T020–T023, T061, T063) | BLOCKED | Local lookup/selection tests pass. T063 is blocked: the reachable backend has no date where service search and station departures return the same service/date candidate. Evidence and exact probes are recorded below and in quickstart. |
| SC-002 | All seven statuses remain distinct and communicate meaning without color alone. | `test/features/journey_status/application/journey_status_notifier_test.dart`; `test/features/journey_status/presentation/journey_status_screen_test.dart`; accessibility suite (T027, T031–T032, T057–T058, T062) | PASS | Full local `flutter test` passed; includes all seven status values and text/live-region accessibility assertions. No real-integration claim. |
| SC-003 | Complete update history stays in ascending revision order; empty differs from missing/error. | `test/features/journey_status/presentation/journey_updates_list_test.dart`; `test/features/journey_status/application/journey_status_notifier_test.dart` (T029–T033, T062) | PASS | Full local `flutter test` passed; ordered updates and successful-empty versus failed/missing states are covered. |
| SC-004 | Supplied ETA includes source/freshness/time; null/absent ETA is unavailable and never derived. | `test/features/journey_status/data/journey_api_dtos_test.dart`; `test/features/journey_status/presentation/journey_status_screen_test.dart` (T006–T007, T027, T031–T032, T062) | PASS | Full local `flutter test` passed; supplied values and null/absent ETA behavior are covered without derivation. |
| SC-005 | Loading, empty, documented errors, unexpected failures, malformed success, timeout/network, retry and stale states are distinct. | `test/core/network/api_failure_mapping_test.dart`; `test/features/journey_status/presentation/journey_recovery_test.dart`; state/widget suites, including synthetic diagnostic-redaction checks (T010–T013, T034–T041, T050–T052, T057) | PASS | Full local `flutter test` passed; includes operation-specific failure mapping, retry behavior, stale retention, and synthetic sentinel redaction. |
| SC-006 | Significant in-app alerts use the same update ID/revision as history and no fabricated/driver change. | `test/features/journey_status/application/seen_journey_update_tracker_test.dart`; `test/features/journey_status/application/journey_update_alerts_test.dart`; `test/features/journey_status/presentation/journey_update_alert_test.dart` (T042–T049, T062) | PASS | Full local `flutter test` passed; first successful history is baseline, later supported IDs/revisions are deduplicated, and no polling is used. |
| SC-007 | Anonymous flow starts from Feature 001 and Back returns to retained search/service context. | `test/features/journey_status/presentation/journey_status_navigation_test.dart`; `test/features/railway_search/presentation/journey_status_entry_navigation_test.dart`; Android integration (T053–T063) | BLOCKED | Local app-router entry/Back test passes with a fake repository. Real Android/backend acceptance remains blocked with T063; no mock evidence is counted as integration. |

## Functional Requirements

| FR | Planned tasks/evidence | Status | Evidence / prerequisite |
|---|---|---|---|
| FR-001 | T024–T026, T053–T056, T059; journey entry from existing details | PASS | Local automated checks for the mapped implementation passed; T063 real-backend acceptance remains separately blocked. |
| FR-002 | T006–T016, T020–T026, T061; exact service/date candidate matching | PASS | Local automated checks for the mapped implementation passed; T063 real-backend acceptance remains separately blocked. |
| FR-003 | T020–T026, T061; no synthesis and explicit ambiguous selection | PASS | Local automated checks for the mapped implementation passed; T063 real-backend acceptance remains separately blocked. |
| FR-004 | T027–T032, T062; seven supplied status labels | PASS | Local automated checks for the mapped implementation passed; T063 real-backend acceptance remains separately blocked. |
| FR-005 | T027–T032, T062; planned/operational/source values separate | PASS | Local automated checks for the mapped implementation passed; T063 real-backend acceptance remains separately blocked. |
| FR-006 | T006–T009, T027–T032, T062; order preserved and no inferred stop data | PASS | Local automated checks for the mapped implementation passed; T063 real-backend acceptance remains separately blocked. |
| FR-007 | T006–T009, T027, T031–T032, T062; supplied ETA or unavailable only | PASS | Local automated checks for the mapped implementation passed; T063 real-backend acceptance remains separately blocked. |
| FR-008 | T006–T007, T029–T033, T062; complete ordered history/empty distinction | PASS | Local automated checks for the mapped implementation passed; T063 real-backend acceptance remains separately blocked. |
| FR-009 | T003, T006–T007, T029–T030, T044–T047, T062; public IDs/details only | PASS | Local automated checks for the mapped implementation passed; T063 real-backend acceptance remains separately blocked. |
| FR-010 | T042–T049; session baseline, new update identity, duplicate suppression, open/manual only | PASS | Local automated checks for the mapped implementation passed; T063 real-backend acceptance remains separately blocked. |
| FR-011 | T027–T028, T034–T039; manual refresh and distinguishable states | PASS | Local automated checks for the mapped implementation passed; T063 real-backend acceptance remains separately blocked. |
| FR-012 | T028, T034–T035, T038–T040, T050–T052; independent section failures/retry | PASS | Local automated checks for the mapped implementation passed; T063 real-backend acceptance remains separately blocked. |
| FR-013 | T036–T040; retained data labeled not refreshed and times distinguished | PASS | Local automated checks for the mapped implementation passed; T063 real-backend acceptance remains separately blocked. |
| FR-014 | T010–T013, T050–T052; operation-specific response/error behavior | PASS | Local automated checks for the mapped implementation passed; T063 real-backend acceptance remains separately blocked. |
| FR-015 | T021, T023, T026, T041, T053–T056, T059; Back and late-response suppression | PASS | Local automated checks for the mapped implementation passed; T063 real-backend acceptance remains separately blocked. |
| FR-016 | T024, T029–T032, T038–T039, T047–T048, T050–T058; accessible feedback | PASS | Local automated checks for the mapped implementation passed; T063 real-backend acceptance remains separately blocked. |
| FR-017 | T005, T012–T013, T018–T019, T061–T062, T070; only anonymous public GETs/no catering/auth/write | PASS | Local automated checks for the mapped implementation passed; T063 real-backend acceptance remains separately blocked. |

## Approved Requirement Traceability

| SR | Priority | Planned tasks/evidence | Status | Evidence / prerequisite |
|---|---|---|---|---|
| SR-001 | Must Have | T032, T062 | PASS | Local automated checks for the mapped implementation passed; T063 real-backend acceptance remains separately blocked. |
| SR-007 | Must Have | T001, T006–T009, T020–T026, T061, T063 | PASS | Local automated checks for the mapped implementation passed; T063 real-backend acceptance remains separately blocked. |
| SR-010 | Must Have | T003, T027–T033, T044–T048, T062 | PASS | Local automated checks for the mapped implementation passed; T063 real-backend acceptance remains separately blocked. |
| SR-013 | Must Have | T020–T026, T053–T061, T063 | PASS | Local automated checks for the mapped implementation passed; T063 real-backend acceptance remains separately blocked. |
| SR-019 | Must Have | T002, T006–T009, T027–T032, T062 | PASS | Local automated checks for the mapped implementation passed; T063 real-backend acceptance remains separately blocked. |
| SR-021 | Must Have | T002, T006–T009, T027, T031–T032, T062 | PASS | Local automated checks for the mapped implementation passed; T063 real-backend acceptance remains separately blocked. |
| SR-022 | Must Have | T003, T029–T030, T044–T048, T062 | PASS | Local automated checks for the mapped implementation passed; T063 real-backend acceptance remains separately blocked. |
| SR-025 | Must Have | T003, T042–T049, T062–T063 | PASS | Local automated checks for the mapped implementation passed; T063 real-backend acceptance remains separately blocked. |
| SR-089 | Must Have | T004–T005, T010–T019, T050–T064 | PASS | Local automated checks for the mapped implementation passed; T063 real-backend acceptance remains separately blocked. |
| SR-098 | Should Have | T030, T032, T045, T070 | PASS | Local automated checks for the mapped implementation passed; T063 real-backend acceptance remains separately blocked. |
| SR-100 | Must Have | T003, T006–T007, T010–T013, T029–T030, T044–T052, T057, T067, T070; synthetic `SENTINEL_PRIVATE_TOKEN_123`, `SENTINEL_INTERNAL_ACTOR_456`, and `SENTINEL_TRACE_SECRET_789` are placed only in simulated Problem Details/diagnostic metadata, then asserted absent from passenger messages, rendered widgets, and accessibility semantics. No real credentials or personal data are used. | PASS | Local automated checks for the mapped implementation passed; T063 real-backend acceptance remains separately blocked. |
| SR-102 | Must Have | T024, T027, T031–T032, T047–T048, T057–T058 | PASS | Local automated checks for the mapped implementation passed; T063 real-backend acceptance remains separately blocked. |
| SR-103 | Should Have | T024, T047, T057–T058 | PASS | Local automated checks for the mapped implementation passed; T063 real-backend acceptance remains separately blocked. |
| SR-104 | Should Have | T031–T032, T057–T058 | PASS | Local automated checks for the mapped implementation passed; T063 real-backend acceptance remains separately blocked. |
| SR-109 | Must Have | T034–T041, T050–T052, T064 | PASS | Local automated checks for the mapped implementation passed; T063 real-backend acceptance remains separately blocked. |
| SR-113 | Must Have | T018–T019, T059, T070 | PASS | Local automated checks for the mapped implementation passed; T063 real-backend acceptance remains separately blocked. |
| SR-114 | Must Have | T004–T005, T010–T013, T020–T026, T034–T041, T050–T064 | PASS | Local automated checks for the mapped implementation passed; T063 real-backend acceptance remains separately blocked. |

## Real Backend / Emulator Prerequisite Evidence

**T063 status: BLOCKED — incompatible seeded date data.** The local API responded anonymously at `http://127.0.0.1:5237`; the emulator-reachable host URL is `http://10.0.2.2:5237`. Station IDs returned by `GET /api/stations`: Colombo Fort `10000000-0000-0000-0000-000000000001`, Galle `10000000-0000-0000-0000-000000000002`.

- `GET /api/services?origin=10000000-0000-0000-0000-000000000001&destination=10000000-0000-0000-0000-000000000002&date=2026-10-17` returned 200 with service `30000000-0000-0000-0000-000000000001` (`FTR-001`, route `20000000-0000-0000-0000-000000000001`).
- `GET /api/stations/10000000-0000-0000-0000-000000000001/departures?date=2026-10-17` returned 200 `[]`, so there is no candidate to open for that service/date.
- The same service search for `date=2026-10-19` returned 503 `service_eligibility_unavailable`.
- Departures for origin/date `2026-10-19` returned journey `45000000-0000-0000-0000-000000000019`, whose `serviceId` is `30000000-0000-0000-0000-000000000001`; this cannot be reached from the approved service search for that date because it returns 503.
- Service details and ordered route stops returned 200 for the service and route above. These responses do not resolve the missing consistent service/date lookup prerequisite.

The integration tests T061/T062 use explicit configuration and real API clients but were not executed on the emulator. Do not infer T063 PASS from those tests, local tests, or the successful individual GETs. Re-run T063 after backend data/API availability provides a date where the approved service search returns the service and station departures returns its matching dated journey.

## Final Execution Record

| Check | Status | Command / device | Result / evidence |
|---|---|---|---|
| `flutter pub get` | PASS | `flutter pub get` | Resolved existing dependencies; no package versions added or changed. |
| `flutter analyze` | PASS | `flutter analyze` | No issues found (final run completed in 5.4s). |
| `flutter test` | PASS | `flutter test --concurrency=1` | 188 tests passed; all local test suites completed. |
| Feature 001 regression suite | PASS | `flutter test test/features/railway_search --concurrency=1` | 84 tests passed, including search/results/detail, journey entry/Back, and timestamp behavior. |
| Integration test compilation review | PASS | `dart analyze integration_test/journey_status` (Agent D) | T061/T062 use explicit fail-closed configuration, real API clients, and the production router; static analysis reported no issues. Emulator execution was not performed. |
| Real backend Android emulator integration | BLOCKED | API host `127.0.0.1:5237`; emulator URL `10.0.2.2:5237`; no emulator integration command run | Named blocker: no compatible seeded service/date/departure journey, evidenced above. Physical-device smoke remains NOT RUN. |
| Physical-device smoke (supplementary) | NOT RUN | | |
| `git diff --check` | PASS | `git diff --check` | No whitespace errors. Git emitted only working-copy line-ending conversion warnings. |


**Remaining gate:** T063 is BLOCKED by the incompatible seeded service/date records above. It remains unchecked. Local PASS rows do not complete the mandatory Android emulator integration gate.
