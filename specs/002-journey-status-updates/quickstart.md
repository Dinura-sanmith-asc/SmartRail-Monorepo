# Quickstart: Journey Status and Updates Validation

This guide validates Feature 002 after implementation. It does not install packages or implement code. Use the existing repository dependencies and commands.

## Prerequisites

- Feature 001 application and existing automated tests pass before Feature 002 changes.
- Flutter SDK from the existing repository environment; dependencies already resolved by `pubspec.lock`.
- For real integration: reachable SmartRail backend with Journeys & Status migrations/data initialized, an existing Feature 001 station/service, and at least one dated journey linked to that service at the selected origin/date. Public GET routes remain anonymous.
- Android emulator for the primary device-backed integration path. For host-local API, configure the emulator-reachable URL using `10.0.2.2:<backend-port>` where applicable. Use the backend's active configured URL; do not assume a hardcoded port or production host.

## Deterministic local verification

Run from the Passenger repository root:

```powershell
flutter analyze
flutter test
```

The local suite must include:

1. DTO contract parsing for valid, nullable, empty, missing, malformed, unexpected field/type, enum, discriminator, and invalid timestamp cases.
2. Exact anonymous GET method/path/query requests for the three operations and no Authorization header.
3. 200 candidates, no match after exact service/date filtering, empty update history, and documented 400/404/503 outcomes.
4. Unexpected 401/403, 500/default, timeout, network failure, invalid successful body, safe messages, and no retry for unchanged malformed IDs or access denial.
5. Candidate auto-open only for one unambiguous row; multiple/ambiguous candidates remain distinct and require explicit selection even when public labels match.
6. Seven distinct status values; planned vs operational values; null/current/stale/last-known ETA; raw timestamp offset/date/time preservation; no inferred values.
7. Snapshot and history independent loading, success, empty, failure, retry, and retained-not-refreshed behavior; differing revisions are not merged.
8. Alert baseline on first successful history read, new update detection on later open/manual refresh, identity/revision association, duplicate suppression, failed-read baseline retention, and no polling/background timer.
9. Search → service details → journey lookup/candidate choice → status/history → Back, pending-read Back, late-response suppression, and Feature 001 regression coverage.
10. Accessibility semantics and changing-status announcements; preserve Should Have priority for SR-103/SR-104.

Mocked tests provide deterministic unit/widget/state evidence only; they do not satisfy real integration acceptance.

## Real backend Android emulator validation

1. Start the seeded backend using its documented local setup and record its actual listening URL.
2. Confirm anonymously that station departures return candidates for the selected origin/date and that a selected row's `serviceId` matches the Feature 001 choice. Record `journeyInstanceId` separately.
3. Launch the Passenger integration suite with all four explicit API and fixture values, for example:

```powershell
flutter test integration_test/journey_status -d <android-emulator-id> --dart-define=SMART_RAIL_API_BASE_URL=http://10.0.2.2:<actual-port> --dart-define=SMART_RAIL_TEST_ORIGIN_STATION_ID=<seeded-origin-uuid> --dart-define=SMART_RAIL_TEST_SERVICE_ID=<seeded-service-uuid> --dart-define=SMART_RAIL_TEST_DATE=<seeded-yyyy-mm-dd>
```

Use the actual backend port and a seeded journey selected through station departures. Tests must fail clearly if configuration/data is absent and must not silently fall back to mocks.
4. Verify anonymous lookup, explicit candidate selection, snapshot status/revision/source/times/stops, complete update order, no-update behavior when applicable, and Back to retained Feature 001 service context.
5. Compare displayed values to actual HTTP responses. Do not alter test inputs to force a desired status. Record command, device, API URL type (redacted as appropriate), actual IDs/date, response values, and result in verification evidence when implementation tasks are created.

If API, seed data, or emulator is unavailable, local checks may still be reported. Record real integration as `BLOCKED` when a named prerequisite prevents execution, or `NOT RUN` when not attempted. Neither is a pass. Physical-device smoke is supplementary unless an approved requirement changes the primary acceptance device.

### Current local integration blocker (2026-10-20)

The API was reachable anonymously at `http://127.0.0.1:5237` (Android emulator host alias: `http://10.0.2.2:5237`). The only returned stations were Colombo Fort (`10000000-0000-0000-0000-000000000001`) and Galle (`10000000-0000-0000-0000-000000000002`). For that pair, the service search on `2026-10-17` returned service `30000000-0000-0000-0000-000000000001`, but station departures returned an empty array. On `2026-10-19`, station departures returned journey `45000000-0000-0000-0000-000000000019` for the same service, but the approved service search returned 503 `service_eligibility_unavailable`. Thus no currently observed input satisfies both the approved service search and dated-journey lookup path. Service detail and route-stop reads individually returned 200, but they cannot substitute for the missing linked service/date flow. T063 is BLOCKED until the backend presents compatible seeded data; do not run the UI suite with guessed values or report mocked/local tests as real integration evidence.

## Contract references

- [Journey API integration contract](contracts/journey-status-client.md)
- [Feature 002 data model](data-model.md)
- [Feature 002 specification](spec.md)
- [Feature 001 plan and client contract](../001-railway-service-search/plan.md)
- Backend source: sibling repository `SmartRail/specs/003-journeys-status/contracts/journeys-status.openapi.yaml`.
