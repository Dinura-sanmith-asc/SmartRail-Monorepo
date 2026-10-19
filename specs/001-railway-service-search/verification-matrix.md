# Feature 001 Verification Matrix

This is the evidence record for Feature 001. All entries begin as `NOT RUN`; update a
status only when the stated check has actually been executed and attach a test result,
command output, or review record.

## Status Definitions

| Status | Meaning | Evidence rule |
|---|---|---|
| `PASS` | The check was executed and every stated assertion passed. | Record the command or review method and link/name its result. Integration requires the actual configured backend and seeded published data. |
| `FAIL` | The check was executed and at least one stated assertion failed. | Record the failing assertion and relevant output. Do not use `FAIL` for an unexecuted check. |
| `BLOCKED` | The check could not be executed because a required external prerequisite is unavailable or an approval gate is unresolved. | Name the missing prerequisite and owner/source if known. It is not a pass and must not count as acceptance evidence. |
| `NOT RUN` | The check has not been attempted, or is intentionally supplementary and was not run. | State the reason when known. It is not a pass and must not count as acceptance evidence. |

Missing backend URL, seeded data, emulator, or physical-device connectivity must be recorded
as `BLOCKED` or `NOT RUN` as appropriate. Reporting those statuses, maintaining requirement
traceability, running local analysis, and running automated tests do not depend on those
external prerequisites. The Android emulator with a real seeded backend is the primary
integration acceptance path. Physical-device smoke is supplementary unless an approved
requirement says otherwise. Never substitute mocks for real-backend evidence or mark an
unexecuted integration run `PASS`.

## Success-Criteria Evidence

| Criterion | Verification tasks | Expected evidence | Status | Evidence / reason |
|---|---|---|---|---|
| SC-001 | T033, T054 | Result/card assertions against submitted criteria; emulator integration comparison with backend response. | BLOCKED | Mocked result/card assertions passed in `flutter test` (T033); real response comparison not run because API URL and seeded data are not configured (T054). |
| SC-002 | T022, T026 | Fake/spy repository asserts exactly zero `repository.search` calls for each invalid-criteria case. | PASS | `flutter test --no-pub` passed; `search_controller_test.dart` verifies missing origin, destination, date and identical stations. |
| SC-003 | T033, T042, T047 | HTTP 200 empty collection renders no-results; mapping/recovery distinguish validation and backend/network failures. | PASS | Results notifier/screen, API failure mapping and recovery widget suites passed for empty, 400, 403, 404, 500, 503 and network outcomes. |
| SC-004 | T033, T040, T054 | Result/detail tests verify returned stop order, supplied date/time, next-day cue and planned labels. | BLOCKED | Fixture/widget assertions passed for scheduled fields, preserved clock text, next-day cue and ordered stops (T033/T040); comparison with real API data is blocked with T054. |
| SC-005 | T041, T054 | Navigation tests and emulator flow show exact criteria/results restored after Back. | BLOCKED | Local navigation and route-stack tests passed (T041); emulator acceptance with backend data is blocked with T054. |
| SC-006 | T049, T050 | Accessible names, selected/disabled states, keyboard focus order, announcements, enlarged text and operable controls. | PASS | `search_accessibility_test.dart` passed in the 106-test suite; analyzer clean. Physical assistive-technology review is supplementary. |
| SC-007 | T040, T047 | Both partial-failure directions preserve the successful section and targeted retry. | PASS | Detail notifier and widget tests passed for both service/stop failure directions and retrying only the failed section. |

## Requirement Coverage

Local automated coverage passed with `flutter test --no-pub` (106 tests) and `flutter analyze --no-pub` (no issues). Local verification uses fixture/mock responses; it does not replace primary real-backend acceptance.

| Requirement | Approved priority | Local automated evidence | Local status | Real-backend evidence |
|---|---|---|---|---|
| FR-001–FR-004 | As approved in spec | station/API, anonymous search, criteria and validation tests | PASS | BLOCKED (T054/T059) |
| FR-005–FR-007 | As approved in spec | submission, results, empty-state and result-card tests | PASS | BLOCKED (T054/T059) |
| FR-008–FR-009 | As approved in spec | detail/route-stop screen and model tests | PASS | BLOCKED (T054/T059) |
| FR-010–FR-016 | As approved in spec | loading, operation-specific failures, retry and partial-data tests | PASS | BLOCKED (T054/T059) |
| FR-017 | As approved in spec | results/details navigation and Back/late-response tests | PASS | BLOCKED (T054/T059) |
| FR-018–FR-019 | As approved in spec | semantics, announcements, text scaling and control operation tests | PASS | Not required for mock/device-independent evidence |
| FR-020 | As approved in spec | API client tests assert only four approved GET paths and documented fields | PASS | BLOCKED (T054/T059) |
| FR-021–FR-022 | As approved in spec | date/criteria serialization, duplicate/late response and disposal tests | PASS | BLOCKED (T054/T059) |
| SR-002, SR-003, SR-004, SR-013, SR-089 | Preserve approved tiers | anonymous reads, service/stop mapping, scheduled-only presentation and API tests | PASS | BLOCKED (T054/T059) |
| SR-109, SR-114 | Preserve approved tiers | client retrieval metadata, recovery and lifecycle tests | PASS | BLOCKED (T054/T059) |
| SR-103, SR-104 | Should Have (not elevated) | accessibility names/states, announcements and enlarged-text operation tests | PASS | Not required for mock/device-independent evidence |

## Integration and Build Evidence

- T054 and T059: `BLOCKED` — `SMART_RAIL_API_BASE_URL`, seeded origin/destination UUIDs, and a seeded travel date are not configured. Android emulator `emulator-5554` is present.
- T055: `NOT RUN` — no physical device is available; this is supplementary.
- `flutter build apk --debug` was attempted once and stopped after approximately two minutes without Gradle progress output. Existing Gradle daemon logs document `Unable to establish loopback connection` during startup. No Gradle/security/architecture workaround was applied; APK verification is not passed.

SC-001, SC-004, and SC-005 remain `BLOCKED` overall because their real-backend portions have not run; local automated portions passed. This matrix records traceability independently of external prerequisites and never treats unexecuted integration checks as passed.
