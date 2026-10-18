# Quickstart: Search and Service Details Validation

This guide is for implementation/acceptance after planning approval. No packages have been
installed and no validation commands were run during plan generation.

## Prerequisites

- Flutter 3.47.2 and Dart 3.13.2, matching the checked-out project.
- Dependencies later added through an approved implementation task and a successful
  `flutter pub get`.
- For deterministic test runs: fixture data and no backend URL.
- For real integration: a reachable SmartRail API base URL using the approved Railway
  Network contract and seeded published stations/services. Search and details must work
  anonymously.
- Android emulator can reach a host-local backend at `10.0.2.2:<port>`; a physical device
  needs a reachable network address. The URL is environment configuration, not a source
  literal. Use HTTPS for shared/demo backends.

## Local Build and Deterministic Validation

From the repository root after implementation:

```powershell
flutter pub get
flutter analyze
flutter test
```

Expected: analyzer reports no feature issues; tests use fake repositories and HTTP mock
responses and require no live service. The test suite should cover:

1. Station list loading, success, empty, unavailable, and retry.
2. Missing criteria and identical origin/destination produce local actionable validation
   and no request; query date is an unchanged `YYYY-MM-DD` calendar date.
3. Valid service search shows backend choices without sorting/recalculation; 200 empty is
   an empty state; 400 preserves criteria; 403 is access unavailable with Back only; 500,
   503, unexpected responses, and no network are unavailable with retry.
4. Details read the service and route endpoints independently. Failure of either leaves the
   successful section intact; retry targets only the failed endpoint; detail 400 malformed
   ID and 404 missing-resource outcomes affect only the corresponding section.
5. Search result planned call date/time/next-day presentation uses the response values and
   never device-timezone conversion. Service-detail first-departure time stays separately
   labeled. Route stops preserve API order and show no invented intermediate times.
6. Back through Search → Results → Details restores retained criteria/results. Back stays
   available while reads are pending; late responses after criterion edits or route departure
   do not alter or reopen the old view. Duplicate submissions do not create duplicate
   results or navigation.
7. Accessible names, status announcements, focus order and increased text scale remain
   operable. Report SR-103/SR-104 outcomes at their approved Should Have/stretch tier.

Run device-backed integration tests separately on an available emulator/device:

```powershell
flutter test integration_test/railway_search -d <device-id>
```

Expected: the selected device launches the app, exercises the public flow, and reports the
specific acceptance assertion that failed. The integration target must use the explicitly
configured environment and seeded data; it must not silently fall back to mocks.

## Real Backend Integration

Configure the environment-specific API root when running the app; substitute an actual
approved host and do not commit it as a production default:

```powershell
flutter run --dart-define=SMART_RAIL_API_BASE_URL=https://<approved-api-host>
```

For an Android emulator with the backend listening on the development host, use the
emulator-reachable equivalent, for example:

```powershell
flutter run --dart-define=SMART_RAIL_API_BASE_URL=http://10.0.2.2:<port>
```

Use local cleartext only in an isolated development environment if platform policy requires
an explicit debug-only allowance; do not relax release network security. Android release
manifest needs Internet permission. A physical-device run must use a host/API URL reachable
from that device and permitted by its firewall/TLS configuration.

Acceptance walk-through:

1. Open the app without signing in and wait for published stations.
2. Choose different origin and destination stations and a travel date; submit once.
3. Compare result station IDs, date, service/route IDs and planned calls against the API
   response; verify empty and error fixtures separately in automated tests.
4. Open a result; verify service detail and ordered stops match the two public reads.
5. Force each read to fail independently and verify sibling-section retention and targeted
   retry. Verify 403 behavior if the environment can exercise it without changing the API.
6. Back out during pending search/detail reads; verify no late response reopens or changes
   the departed screen.
7. Confirm every displayed time is scheduled/planned; later-day calls retain their supplied
   local date and clock time and show “next day”.

## Contract and Model References

- [Client integration/status mapping](contracts/railway-network-client.md)
- [Typed response and state model](data-model.md)
- [Approved OpenAPI contract](../../docs/contracts/railway-network.openapi.yaml)
- [Approved feature specification and acceptance scenarios](spec.md)

If the integration URL, public seeded data, or backend access is not available, record that
as an integration prerequisite and retain deterministic mocked evidence; do not substitute
fabricated railway responses for the real-backend acceptance run.
