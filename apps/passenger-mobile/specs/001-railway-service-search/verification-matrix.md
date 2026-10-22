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
| SC-001 | T033, T054 | Result/card assertions against submitted criteria; emulator integration comparison with backend response. | PASS | T033 local result/card assertions passed. T054/T059 passed on emulator `emulator-5554` against backend `http://10.0.2.2:5236` with seeded origin `10000000-0000-0000-0000-000000000001`, destination `10000000-0000-0000-0000-000000000002`, date `2026-10-17`; displayed service matched GET response `FTR-001` / service `30000000-0000-0000-0000-000000000001`. Command and output recorded below. |
| SC-002 | T022, T026 | Fake/spy repository asserts exactly zero `repository.search` calls for each invalid-criteria case. | PASS | `flutter test --no-pub` passed; `search_controller_test.dart` verifies missing origin, destination, date and identical stations. |
| SC-003 | T033, T042, T047 | HTTP 200 empty collection renders no-results; mapping/recovery distinguish validation and backend/network failures. | PASS | Results notifier/screen, API failure mapping and recovery widget suites passed for empty, 400, 403, 404, 500, 503 and network outcomes. |
| SC-004 | T033, T040, T054 | Result/detail tests verify returned stop order, supplied date/time, next-day cue and planned labels. | PASS | T033/T040 local assertions passed. Emulator run matched backend planned calls `2026-10-17T23:20:00+05:30` (Colombo Fort) and `2026-10-18T01:25:00+05:30` (Galle); route stops were Colombo Fort order 1 then Galle order 2. The app preserved supplied date/time values and showed the later-day cue. |
| SC-005 | T041, T054 | Navigation tests and emulator flow show exact criteria/results restored after Back. | PASS | T041 navigation tests passed; T054/T059 emulator flow passed Search → Results → Details → Back with criteria/results retained. |
| SC-006 | T049, T050 | Accessible names, selected/disabled states, keyboard focus order, announcements, enlarged text and operable controls. | PASS | `search_accessibility_test.dart` passed in the 106-test suite; analyzer clean. Physical assistive-technology review is supplementary. |
| SC-007 | T040, T047 | Both partial-failure directions preserve the successful section and targeted retry. | PASS | Detail notifier and widget tests passed for both service/stop failure directions and retrying only the failed section. |

## Requirement Coverage

Final local verification passed with `flutter test --timeout 120s` (106 tests) and `flutter analyze` (no issues). Local verification uses fixture/mock responses; it does not replace primary real-backend acceptance.

| Requirement | Approved priority | Local automated evidence | Local status | Real-backend evidence |
|---|---|---|---|---|
| FR-001–FR-004 | As approved in spec | station/API, anonymous search, criteria and validation tests | PASS | Success-path integration passed; negative/error behavior is verified by local tests, not a live error-response run |
| FR-005–FR-007 | As approved in spec | submission, results, empty-state and result-card tests | PASS | Success-path integration passed; negative/error behavior is verified by local tests, not a live error-response run |
| FR-008–FR-009 | As approved in spec | detail/route-stop screen and model tests | PASS | Success-path integration passed; negative/error behavior is verified by local tests, not a live error-response run |
| FR-010–FR-016 | As approved in spec | loading, operation-specific failures, retry and partial-data tests | PASS | Success-path integration passed; negative/error behavior is verified by local tests, not a live error-response run |
| FR-017 | As approved in spec | results/details navigation and Back/late-response tests | PASS | Success-path integration passed; negative/error behavior is verified by local tests, not a live error-response run |
| FR-018–FR-019 | As approved in spec | semantics, announcements, text scaling and control operation tests | PASS | Not required for mock/device-independent evidence |
| FR-020 | As approved in spec | API client tests assert only four approved GET paths and documented fields | PASS | Success-path integration passed; negative/error behavior is verified by local tests, not a live error-response run |
| FR-021–FR-022 | As approved in spec | date/criteria serialization, duplicate/late response and disposal tests | PASS | Success-path integration passed; negative/error behavior is verified by local tests, not a live error-response run |
| SR-002, SR-003, SR-004, SR-013, SR-089 | Preserve approved tiers | anonymous reads, service/stop mapping, scheduled-only presentation and API tests | PASS | Success-path integration passed; negative/error behavior is verified by local tests, not a live error-response run |
| SR-109, SR-114 | Preserve approved tiers | client retrieval metadata, recovery and lifecycle tests | PASS | Success-path integration passed; negative/error behavior is verified by local tests, not a live error-response run |
| SR-103, SR-104 | Should Have (not elevated) | accessibility names/states, announcements and enlarged-text operation tests | PASS | Not required for mock/device-independent evidence |

## Integration and Build Evidence

- T054 and T059: `PASS` — `flutter test integration_test/railway_search -d emulator-5554` with all four explicit `--dart-define` values (see command below); 2 integration tests passed against the running SmartRail backend. The first attempt exposed ambiguous station finder selection; after correcting both test helpers, the full rerun passed.
- T055: `NOT RUN` — no physical-device smoke test was performed; this is supplementary and non-gating.
- `flutter build apk --debug --no-pub --verbose` passed using Android Studio JBR Java 21.0.10; Gradle reported `BUILD SUCCESSFUL` and produced `build/app/outputs/flutter-apk/app-debug.apk`. No Gradle, security, or architecture configuration was changed. Historical daemon logs show the earlier loopback initialization failure under Microsoft JDK 17.0.19; the successful build with the currently selected JDK indicates the blocker is resolved in this environment, without establishing the precise cause.

SC-001, SC-004, and SC-005 are `PASS` after both their local automated checks and primary emulator integration passed. T055 remains supplementary `NOT RUN`; it does not gate Feature 001 acceptance.

### Real Backend Inputs and Evidence

- Backend launch profile: `dotnet run --project src/SmartRail.Api`; host URL `http://localhost:5236`. Existing PostgreSQL Compose services were healthy and migrations `20261017145843_InitialRailwayNetwork` and `20261018005918_IdentityPermissions` were applied. No backend files were changed.
- Android emulator endpoint: `http://10.0.2.2:5236` (emulator `emulator-5554`).
- Search input: Colombo Fort `10000000-0000-0000-0000-000000000001` → Galle `10000000-0000-0000-0000-000000000002`, travel date `2026-10-17`.
- Matching published service: `FTR-001`, service ID `30000000-0000-0000-0000-000000000001`, route `CMB-GAL`, route ID `20000000-0000-0000-0000-000000000001`.
- Anonymous host HTTP requests to all four approved reads returned HTTP 200: `/api/stations`; `/api/services?origin=10000000-0000-0000-0000-000000000001&destination=10000000-0000-0000-0000-000000000002&date=2026-10-17`; `/api/services/30000000-0000-0000-0000-000000000001`; `/api/routes/20000000-0000-0000-0000-000000000001/stops`. Service response supplied departure `2026-10-17T23:20:00+05:30` and arrival `2026-10-18T01:25:00+05:30`; ordered stops were Colombo Fort (1), Galle (2).
- Emulator integration command (actual run; both tests passed):

```powershell
flutter test integration_test/railway_search -d emulator-5554 `
  --dart-define=SMART_RAIL_API_BASE_URL=http://10.0.2.2:5236 `
  --dart-define=SMART_RAIL_TEST_ORIGIN_ID=10000000-0000-0000-0000-000000000001 `
  --dart-define=SMART_RAIL_TEST_DESTINATION_ID=10000000-0000-0000-0000-000000000002 `
  --dart-define=SMART_RAIL_TEST_DATE=2026-10-17 --timeout 120s
```

Output ended `All tests passed!` with `+2` integration tests. This verifies anonymous station retrieval, seeded search and service display, details and ordered route stops, backend-value comparison, and Search → Results → Details → Back. Negative HTTP classifications remain verified by mocked/local tests, not by this successful-path seeded integration.
