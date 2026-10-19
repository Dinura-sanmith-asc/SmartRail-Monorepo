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
| SC-001 | Selected service/date opens only a matching returned journey identity; no synthetic journeys. | `test/features/journey_status/application/journey_lookup_notifier_test.dart`; `test/features/journey_status/application/journey_candidate_selection_test.dart`; `integration_test/journey_status/journey_lookup_integration_test.dart` (T020–T023, T061, T063) | NOT RUN | |
| SC-002 | All seven statuses remain distinct and communicate meaning without color alone. | `test/features/journey_status/application/journey_status_notifier_test.dart`; `test/features/journey_status/presentation/journey_status_screen_test.dart`; accessibility suite (T027, T031–T032, T057–T058, T062) | NOT RUN | |
| SC-003 | Complete update history stays in ascending revision order; empty differs from missing/error. | `test/features/journey_status/presentation/journey_updates_list_test.dart`; `test/features/journey_status/application/journey_status_notifier_test.dart` (T029–T033, T062) | NOT RUN | |
| SC-004 | Supplied ETA includes source/freshness/time; null/absent ETA is unavailable and never derived. | `test/features/journey_status/data/journey_api_dtos_test.dart`; `test/features/journey_status/presentation/journey_status_screen_test.dart` (T006–T007, T027, T031–T032, T062) | NOT RUN | |
| SC-005 | Loading, empty, documented errors, unexpected failures, malformed success, timeout/network, retry and stale states are distinct. | `test/core/network/api_failure_mapping_test.dart`; `test/features/journey_status/presentation/journey_recovery_test.dart`; state/widget suites, including synthetic diagnostic-redaction checks (T010–T013, T034–T041, T050–T052, T057) | NOT RUN | |
| SC-006 | Significant in-app alerts use the same update ID/revision as history and no fabricated/driver change. | `test/features/journey_status/application/seen_journey_update_tracker_test.dart`; `test/features/journey_status/application/journey_update_alerts_test.dart`; `test/features/journey_status/presentation/journey_update_alert_test.dart` (T042–T049, T062) | NOT RUN | |
| SC-007 | Anonymous flow starts from Feature 001 and Back returns to retained search/service context. | `test/features/journey_status/presentation/journey_status_navigation_test.dart`; `test/features/railway_search/presentation/journey_status_entry_navigation_test.dart`; Android integration (T053–T063) | NOT RUN | |

## Functional Requirements

| FR | Planned tasks/evidence | Status | Evidence / prerequisite |
|---|---|---|---|
| FR-001 | T024–T026, T053–T056, T059; journey entry from existing details | NOT RUN | |
| FR-002 | T006–T016, T020–T026, T061; exact service/date candidate matching | NOT RUN | |
| FR-003 | T020–T026, T061; no synthesis and explicit ambiguous selection | NOT RUN | |
| FR-004 | T027–T032, T062; seven supplied status labels | NOT RUN | |
| FR-005 | T027–T032, T062; planned/operational/source values separate | NOT RUN | |
| FR-006 | T006–T009, T027–T032, T062; order preserved and no inferred stop data | NOT RUN | |
| FR-007 | T006–T009, T027, T031–T032, T062; supplied ETA or unavailable only | NOT RUN | |
| FR-008 | T006–T007, T029–T033, T062; complete ordered history/empty distinction | NOT RUN | |
| FR-009 | T003, T006–T007, T029–T030, T044–T047, T062; public IDs/details only | NOT RUN | |
| FR-010 | T042–T049; session baseline, new update identity, duplicate suppression, open/manual only | NOT RUN | |
| FR-011 | T027–T028, T034–T039; manual refresh and distinguishable states | NOT RUN | |
| FR-012 | T028, T034–T035, T038–T040, T050–T052; independent section failures/retry | NOT RUN | |
| FR-013 | T036–T040; retained data labeled not refreshed and times distinguished | NOT RUN | |
| FR-014 | T010–T013, T050–T052; operation-specific response/error behavior | NOT RUN | |
| FR-015 | T021, T023, T026, T041, T053–T056, T059; Back and late-response suppression | NOT RUN | |
| FR-016 | T024, T029–T032, T038–T039, T047–T048, T050–T058; accessible feedback | NOT RUN | |
| FR-017 | T005, T012–T013, T018–T019, T061–T062, T070; only anonymous public GETs/no catering/auth/write | NOT RUN | |

## Approved Requirement Traceability

| SR | Priority | Planned tasks/evidence | Status | Evidence / prerequisite |
|---|---|---|---|---|
| SR-001 | Must Have | T032, T062 | NOT RUN | |
| SR-007 | Must Have | T001, T006–T009, T020–T026, T061, T063 | NOT RUN | |
| SR-010 | Must Have | T003, T027–T033, T044–T048, T062 | NOT RUN | |
| SR-013 | Must Have | T020–T026, T053–T061, T063 | NOT RUN | |
| SR-019 | Must Have | T002, T006–T009, T027–T032, T062 | NOT RUN | |
| SR-021 | Must Have | T002, T006–T009, T027, T031–T032, T062 | NOT RUN | |
| SR-022 | Must Have | T003, T029–T030, T044–T048, T062 | NOT RUN | |
| SR-025 | Must Have | T003, T042–T049, T062–T063 | NOT RUN | |
| SR-089 | Must Have | T004–T005, T010–T019, T050–T064 | NOT RUN | |
| SR-098 | Should Have | T030, T032, T045, T070 | NOT RUN | |
| SR-100 | Must Have | T003, T006–T007, T010–T013, T029–T030, T044–T052, T057, T067, T070; synthetic `SENTINEL_PRIVATE_TOKEN_123`, `SENTINEL_INTERNAL_ACTOR_456`, and `SENTINEL_TRACE_SECRET_789` are placed only in simulated Problem Details/diagnostic metadata, then asserted absent from passenger messages, rendered widgets, and accessibility semantics. No real credentials or personal data are used. | NOT RUN | |
| SR-102 | Must Have | T024, T027, T031–T032, T047–T048, T057–T058 | NOT RUN | |
| SR-103 | Should Have | T024, T047, T057–T058 | NOT RUN | |
| SR-104 | Should Have | T031–T032, T057–T058 | NOT RUN | |
| SR-109 | Must Have | T034–T041, T050–T052, T064 | NOT RUN | |
| SR-113 | Must Have | T018–T019, T059, T070 | NOT RUN | |
| SR-114 | Must Have | T004–T005, T010–T013, T020–T026, T034–T041, T050–T064 | NOT RUN | |

## Final Execution Record

| Check | Status | Command / device | Result / evidence |
|---|---|---|---|
| `flutter pub get` | NOT RUN | | |
| `flutter analyze` | NOT RUN | | |
| `flutter test` | NOT RUN | | |
| Feature 001 regression suite | NOT RUN | | |
| Real backend Android emulator integration | NOT RUN | | |
| Physical-device smoke (supplementary) | NOT RUN | | |
| `git diff --check` | NOT RUN | | |

