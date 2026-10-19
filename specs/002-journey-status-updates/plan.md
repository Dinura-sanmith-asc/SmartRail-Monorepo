# Implementation Plan: Journey Status and Updates

**Branch**: `002-journey-status-updates` | **Date**: 2026-10-20 | **Spec**: [spec.md](spec.md)

**Input**: Approved Feature 002 specification and two resolved UX clarifications.

## Summary

Extend the existing anonymous Feature 001 Flutter app with a journey lookup and official status/history view. Reuse Feature 001's selected scheduled-service identity, origin station and travel date to call the public station-departures endpoint. Match candidates by returned `serviceId` and `stationLocalDepartureDate`; open a single unambiguous match or require explicit selection of each ambiguous candidate. Fetch snapshot and update history only with the selected opaque `journeyInstanceId`. Keep both reads independent, show backend-published values with source and freshness context, detect in-app updates on view entry/manual refresh, and preserve Feature 001 navigation context. The three public reads are exact backend contract operations; no authentication, write operations, polling, push, or unsupported journey values are introduced.

## Technical Context

**Language/Version**: Dart 3.13.2; Flutter 3.47.2 in the current Passenger project.
**Primary Dependencies**: Existing `flutter_riverpod ^3.4.3`, `go_router ^18.1.0`, `http ^1.6.0`, `intl ^0.20.3`; Flutter SDK. No new package/code generation.
**Storage**: None. Journey view data and seen update IDs stay in app memory for the active session. No persistent/offline cache.
**Testing**: Existing `flutter_test`, `integration_test`, Riverpod provider overrides/fakes and `http/testing.dart` MockClient. Real Android emulator integration remains separate from mocks.
**Target Platform**: Existing Android and iOS Flutter application; Android emulator is primary device-backed integration acceptance path per Feature 001.
**Project Type**: Existing single Flutter passenger application.
**Performance Goals**: No approved latency or scale SLO. Use existing bounded HTTP timeout and show loading feedback; do not invent numeric acceptance targets.
**Constraints**: Anonymous GET only; exact documented paths and fields; no `serviceId`/`journeyInstanceId` conflation; no client-authoritative status, ETA, timetable eligibility, intermediate values, controller publication, driver observations, incident details, catering dependency, push, subscription, or polling. Preserve Feature 001 behavior. Keep SR-103 and SR-104 at Should Have.
**Scale/Scope**: One service-to-dated-journey lookup, explicit candidate selection when required, one selected journey snapshot, complete public update history, session-only seen-update tracking, and navigation back to existing service details.

## Constitution Check

*GATE: Passed before Phase 0 research; re-checked after design.*

| Principle | Plan check | Outcome |
|---|---|---|
| I. Approved Scope and Traceability | Feature maps SR-001/007/010/013/019/021/022/025/089/098/100/102/103/104/109/113/114 to passenger-only behavior. Must/Should priorities remain as in spec; SR-103/104 are Should Have. SC-001–007 map to tests in this plan. | PASS |
| II. Flutter Architecture and Ownership | Adds a separate feature-based presentation/application/domain/data boundary; reuses Riverpod, GoRouter and injected repository/client; no unnecessary abstraction or packages. | PASS |
| III. Authoritative Backend Integration | Uses three exact public Journeys & Status GETs; strict typed parsing; environment base URI; bounded requests. Backend owns candidate projection and official status. | PASS |
| IV. Security, Privacy, and Accessibility | Anonymous reads have no Authorization header. No protected writes, driver/private data, or passenger auth. Status/errors use accessible non-color semantics and safe messages. | PASS |
| V. Verified Delivery and Honest Information | Covers loading/empty/validation/access-denied/unavailable/retry/stale states, partial reads and explicit update freshness. Mocks are separate from real backend acceptance. | PASS |

No constitution exception or additional approval decision is required. The Q1 ambiguous-candidate choice and Q2 open/manual-refresh alert timing are settled in the spec. First successful history read establishes a baseline as a technical interpretation of “new update”; see [research.md](research.md).

## Existing Application and Reuse Boundaries

Existing Feature 001 uses a single Flutter app. Reuse:

- `lib/app/app_config.dart`, `app/providers.dart`, `sharedHttpClientProvider`, and app-level lifecycle ownership. The shared `http.Client` is created once and closed by `ref.onDispose` on its owner. Feature 002 must not close it.
- `lib/core/network/http_api_client.dart`, `api_failure.dart`, and `problem_details_parser.dart` for URI construction, UTF-8 decoding, timeout, safe Problem Details and failure handling. Add journey operation classifications carefully and preserve Feature 001 mappings/tests.
- Riverpod auto-dispose family notifiers, request-generation guards, `RetrievalMetadata`, and `core/widgets/async_feedback.dart` for independent sections, stale state and accessible feedback.
- `go_router` push/pop stack and existing app theme.
- Feature 001's immutable `ScheduledServiceChoice`, origin `ScheduledCall.stationId`, and `TravelDate` as inputs only. Do not move journey domain state into `railway_search`.

New feature boundaries:

```text
lib/features/journey_status/
  presentation/   # journey entry/choice and status/history screens/components
  application/    # lookup/status notifiers, view state, session alert tracking
  domain/         # candidate, selection context, snapshot, stop, update, repository
  data/           # strict journey DTOs, API wrapper, repository mapping

test/features/journey_status/
  data/ application/ domain/ presentation/
integration_test/journey_status/
```

Shared composition files (`lib/app/providers.dart`, `lib/app/router.dart`, and Feature 001 service-details entry point) are single-owner integration changes. Keep all other implementation work in journey-specific files. Add journey tests without moving/reworking Feature 001 tests.

## Architecture and State Design

### Navigation and journey lookup

From Feature 001 service details, add a clearly labeled journey-status entry action. Push a journey lookup/candidate route carrying the selected service context: service ID, origin station UUID, origin/destination display context as already available, and selected `TravelDate`. Preserve the service detail below it on the GoRouter stack.

Lookup state is distinct from journey status:

- Initial → loading → valid empty / one match / candidate choices / recoverable unavailable.
- Issue only `GET /api/stations/{originStationId}/departures?date={YYYY-MM-DD}`.
- Client confirms both `candidate.serviceId == selected.serviceId` and `candidate.stationLocalDepartureDate == selected TravelDate`; it does not calculate eligibility or infer operation date.
- A unique row marked `none` can open automatically. Multiple results or any ambiguous row remain explicit distinct rows with accessible ordinal labeling if necessary. A tap selects one exact `journeyInstanceId`; identical descriptive data does not merge identities.
- No matching row is a successful no-journey state, not an API error. Back returns to the same Feature 001 details and its active search/results context.

### Status and history state

`JourneyStatusNotifier` (route-scoped, auto-dispose family keyed by journey identity) owns two independently tracked sections:

| Section | Success states | Recoverable outcomes | Non-retry outcomes |
|---|---|---|---|
| Snapshot | loading, current snapshot | 500/unexpected, 503, timeout/network, malformed 2xx; preserve prior snapshot as not refreshed and retry this read | 400 malformed journey path ID, 404 missing journey, 401/403 access unavailable; Back, no unchanged retry/login |
| Updates | loading, history or valid empty | 500/unexpected, 503, timeout/network, malformed 2xx; preserve prior history as not refreshed and retry this read | 400 malformed journey path ID, 404 missing journey, 401/403 access unavailable; Back, no unchanged retry/login |

Initial entry requests snapshot and history independently. Manual refresh starts both reads. Per-section Retry touches only that read. A successful sibling remains visible when the other fails. Failed refresh retains its prior confirmed data only with a visible `Not refreshed` indicator and client retrieval time. Official timestamps remain separately labeled.

Snapshot contains one internally consistent revision. History is a separate complete ascending-revision read. If successful snapshot and history report different revisions because publication occurred between calls, retain their sections without synthesizing one combined revision; show that information is temporarily incomplete/inconsistent and permit refresh. Empty history is a valid state with a distinct “No published updates” message.

### Update detection and alerts (Q2)

Maintain an app-scope, memory-only map from `journeyInstanceId` to successfully observed `updateId` values. The first valid history response for a journey seeds the baseline and does not alert for every older history item. Subsequent successful reads on journey-view entry or manual refresh compare returned update IDs with the baseline. For newly observed IDs, present in-app notices only for supported significant status (delay/cancellation), platform, route-diversion, or operational-time changes, and associate each notice with exactly that history `updateId` and `revision`. A correction is evaluated only through its documented corrected public change; do not infer new fields. Update the seen set only after successful strict parsing. A failed response does not advance it. Returning to the view does not re-alert the same ID during the active session. The session map resets when the app process/session ends. No timer, polling, push, or background message service is added.

### Official values and freshness

- Keep all seven backend status strings distinct and use text/accessibility semantics as well as any color/icon.
- Show scheduled baseline separately from official operational publications. If `publishedAt` is null and source is `ScheduledBaseline`, do not claim there is a current official update.
- Label planned and operational stop calls separately and preserve backend order. Never derive delay, intermediate times, location, or ETA.
- Show ETA only when its full contract object exists, with value/source/freshness/update time. Current backend projection is `null`; display “Unavailable”/“Unknown”.
- Preserve original offset-bearing timestamp string and literal date/time/offset. Do not convert to device timezone. Keep `operationDate`, station-local departure date, API source timestamps, and client retrieval time distinct.
- Public history excludes initial baseline and private audit events. Never expose driver observation IDs, actor subjects, private initialization evidence, or incident records.

## API Contract and Error Boundary

Canonical backend file is `SmartRail/specs/003-journeys-status/contracts/journeys-status.openapi.yaml`; exact client mapping is in [contracts/journey-status-client.md](contracts/journey-status-client.md). Use only:

1. `GET /api/stations/{id}/departures?date=YYYY-MM-DD`
2. `GET /api/journeys/{id}`
3. `GET /api/journeys/{id}/updates`

All are anonymous (`security: []`/`.AllowAnonymous()`). Do not send authorization headers or add protected Controller `PATCH` calls. Strict DTOs cover `StationDeparture`, `JourneySnapshot`/`JourneyStop`/optional `Estimate`, `JourneyUpdate`/discriminated `PublicChange`, and RFC 9457 `Problem`.

| Response | Lookup behavior | Snapshot/history behavior |
|---|---|---|
| 200, non-empty | Filter exact service/date; no auto-selection of ambiguity | Render only supplied values and selected journey ID |
| 200, empty array | No matching dated journey | Empty updates is distinct no-updates success |
| 400 | Invalid station/date request; safe invalid selection, Back, no retry unchanged values | Malformed path ID; affected section unavailable, Back, no retry unchanged ID |
| 401/403 | Unexpected access-denied; Back, no sign-in/retry | Unexpected access-denied on affected section; no sign-in/retry |
| 404 | Station missing; lookup cannot continue, Back | Journey missing; affected section unavailable, preserve sibling, Back/no same-ID retry |
| 500/unexpected | Safe generic recoverable failure and retry | Retry only failed section; preserve successful sibling/prior data |
| 503 | Unavailable message and retry | Retry only failed section |
| timeout/network | Safe network failure/retry | Retry only failed section; old data marked not refreshed |
| malformed 2xx | Protocol/unavailable, retry lookup | Protocol/unavailable; preserve sibling, retry only failed section |

Do not expose `Problem.detail`, trace IDs, raw exceptions, internal logs or backend diagnostics to passenger display. No invented status semantics or retry cadence.

## Test Strategy and Success-Criteria Traceability

Use deterministic fakes/MockClient for local tests and separate real backend acceptance. Must Have behavior remains mandatory; SR-103/SR-104 tests retain Should Have labels and must not be reported as Must acceptance.

| Success criterion | Verification evidence planned |
|---|---|
| SC-001 — returned matching identity only; no synthetic journey | Journey selection domain/notifier tests for zero/one/multiple candidates and exact service/date match; selection navigation test asserts snapshot/history receive selected journey UUID only. |
| SC-002 — seven statuses distinct and accessible without color alone | Domain/DTO enum tests plus status widget semantics tests for all seven values without color dependence. |
| SC-003 — full history order; empty distinct from missing/failure | API/repository tests preserve returned revision order; widget/state tests for `[]`, 404 and network/server failures. |
| SC-004 — ETA honest | DTO/domain tests for ETA object and null; status UI tests confirm supplied metadata or unavailable and no derived value. |
| SC-005 — every read outcome distinguishable | MockClient operation/status matrix, state and widget tests for loading/empty/400/401/403/404/500/503/malformed/timeout/network/retry/stale; actual documented status behavior remains as contract specifies. |
| SC-006 — alerts use same update identity/revision | Session tracker tests for initial baseline, new supported update, unsupported change, retry after failed read and duplicate suppression; widget assertion compares alert and history IDs/revisions. |
| SC-007 — anonymous Feature 001 path and Back context | Request tests assert no Authorization; router/widget test enters from existing service detail, selects candidate, views status and pops back to same service/search state. |

Additional tests: disposal/late-response suppression, changed candidate ID isolation, partial read failures, revision mismatch handling, offset preservation, candidate identical labels and explicit taps, no polling, increased text scale, semantics/focus and Feature 001 regression suite.

## Implementation Phases and Parallel Boundaries

This plan does not generate `tasks.md`. Suggested implementation order for later task generation:

1. Confirm feature models/API error operation mapping and agree immutable interfaces; add fixture schemas and test helpers.
2. Build journey DTOs/API wrapper/repository with strict tests; no UI or route edits.
3. Build lookup/candidate selection state and tests using fake journey repository.
4. Build snapshot/history state, independent retries, metadata, update-ID session tracker, and tests.
5. Build lookup/candidate/status/history presentation with accessibility and partial/failure states.
6. Integrate one shared-owner change to app providers, GoRouter, and Feature 001 service-detail entry action; run navigation and Feature 001 regression tests.
7. Add separate Android emulator real-backend integration and record explicit evidence/status.
8. Run full local verification and requirement evidence review.

Safe parallel work after interfaces and fixture contract are fixed: (A) DTO/client/repository and parsing tests; (B) journey domain/state/notifier tests using fakes; (C) presentation components/tests against agreed immutable state. Keep `lib/app/providers.dart`, `lib/app/router.dart`, `lib/features/railway_search/presentation/service_detail_screen.dart`, `lib/core/network/api_failure.dart`, and their integration tests under one integration owner to avoid overlapping edits. Routing should merge after destination screens and providers compile. Do not parallelize overlapping changes to shared state types or API failure mapping.

## Risks and Remaining Gates

- Ambiguous rows may share every public descriptive field. Separate accessible rows can be rendered and tapped, but labels may not explain their real-world distinction; Q1 explicitly approves preserving them, and no new discriminator may be invented. Record any later UX change for product review.
- Current backend returns `eta: null`; estimate tests need contract fixtures, while actual integration must reflect unavailability.
- Read endpoint OpenAPI documents 400/404/503, not 401/403 or 500. Generic safe handling for unexpected statuses is required by the passenger spec, but integration cannot assume those statuses will be produced by the current backend.
- Recheck the backend contract hash/revision and actual reachable seeded backend before implementation/acceptance. No base URL/seed is committed as an app default.
- Automated/emulator integration is a mandatory acceptance gate when its prerequisites are available; report a missing backend/emulator as BLOCKED, never as PASS. Physical device smoke is supplementary unless an approved source changes this.

## Constitution Check (Post-Design)

| Principle | Design evidence | Outcome |
|---|---|---|
| I. Traceability | Spec's SR trace retained; this plan maps SC-001–SC-007 and test intent. SR-103/SR-104 retain Should priority. | PASS |
| II. Architecture/ownership | Separate feature layers; existing Riverpod/GoRouter/repository/network patterns reused; shared-file edits have single integration owner. | PASS |
| III. Backend authority | Exact three anonymous GET routes and DTOs; no backend rule duplication, writes, fabricated ETA or new endpoints. | PASS |
| IV. Security/accessibility | No login/protected operations; safe messages and public projection only; accessible non-color status and feedback. | PASS |
| V. Verified delivery | Unit/state/widget/navigation tests plus separately evidenced real Android backend integration; fresh/stale/empty/error behavior specified. | PASS |

No constitution violation or complexity exception remains. Planning is complete; implementation tasks and Flutter code are intentionally not generated here.

