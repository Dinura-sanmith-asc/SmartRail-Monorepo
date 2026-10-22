# Implementation Plan: Railway Search and Service Details

**Branch**: `001-railway-service-search` | **Date**: 2026-10-18 | **Spec**: [spec.md](spec.md)

**Input**: Approved feature specification and clarification decisions.

## Summary

Build an anonymous, read-only passenger flow for choosing stations and a travel date,
searching the Railway Network API, and inspecting a selected scheduled service. Use a
feature-based Flutter structure with Riverpod for state and dependency composition,
`go_router` for the three-screen stack, and an injectable `package:http` client behind a
repository. Parse only the approved OpenAPI fields, preserve scheduled timestamp text, and
keep service detail and route-stop failures independently recoverable. Package selection
and compatibility research is recorded in [research.md](research.md); exact DTOs and API
mapping are in [data-model.md](data-model.md) and [contracts/railway-network-client.md](contracts/railway-network-client.md).

## Technical Context

**Language/Version**: Dart 3.13.2; Flutter 3.47.2 (installed project toolchain).

**Primary Dependencies**: Flutter SDK; proposed direct packages: `flutter_riverpod ^3.4.3`,
`go_router ^18.1.0`, `http ^1.6.0`, and `intl ^0.20.3`. Existing `cupertino_icons` remains
as generated project scaffolding unless design review removes it. No JSON generator or
separate dependency-injection package.

**Storage**: None for this feature. Keep station, criteria, results, and last-retrieved
metadata in memory for the active navigation session; no persistence or offline cache.

**Testing**: Existing `flutter_test`; Riverpod provider overrides/fakes; `http/testing.dart`
MockClient for deterministic API tests. Real-backend integration tests run only with an
explicit environment URL and seeded public data.

**Target Platform**: Android and iOS Flutter app. The repository contains both platform
projects; only Android emulator configuration is present in the current development setup.

**Project Type**: Existing single Flutter mobile application.

**Performance Goals**: No approved response-time SLO or scale target exists. Show loading
feedback as soon as reads begin; enforce bounded request timeouts (proposed 10 seconds,
configurable for test/deployment) and measure actual latency during integration validation.

**Constraints**: No production API base URL is committed; inject one per build/runtime
environment. Use only the four public GET operations in the approved OpenAPI contract.
Keep unauthenticated reads anonymous; do not add token handling. Do not infer route/date
eligibility, invent stop times, or treat planned schedules as live status. Honor the five
recorded clarification decisions, preserve SR-103/SR-104 as Should Have stretch work, and
do not create backend or API-contract changes.

**Scale/Scope**: Three passenger screens (search, results, scheduled service details), one
station collection, one active search result set, and two independent detail reads. No
multi-user app state, long-term cache, or new backend capability.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-checked after Phase 1 design.*

| Principle | Plan check | Outcome |
|---|---|---|
| I. Approved Scope and Traceability | Keeps SR-002/003/004/013 and applicable SR-089/109/114 in scope; SR-103/104 remain Should Have. Test matrix maps each Must/Should expectation to tests. | PASS |
| II. Flutter Architecture and Ownership | Feature-based presentation/application/domain/data boundaries; explicit repository and typed model boundary; selected libraries justified in research. | PASS |
| III. Authoritative Backend Integration | Uses four exact public GET operations, environment configuration, typed parsing and bounded timeouts; backend owns publication and eligibility. | PASS |
| IV. Security, Privacy, and Accessibility | No login or credentials for public reads; safe error mapping; accessibility verification retains Should Have priority. | PASS |
| V. Verified Delivery and Honest Information | Defines loading/success/empty/error/retry/stale states and tests; timestamps stay planned and offset-preserving. | PASS |

No constitution exception or complexity justification is required. No unresolved feature
clarification is carried into the technical design. The base URL and deployment environment
are external configuration inputs, not new API semantics.

## Architecture and Design

### Application entry and configuration

Replace the generated counter app at `lib/main.dart` only during implementation. It will
initialize a single `ProviderScope`, build `MaterialApp.router`, and compose an environment
configuration, one closable HTTP client, API data source, repository, and providers. The
app-scoped `railwayApiClientProvider` owns the shared `http.Client`, creates it once for the
application scope, and registers `Client.close()` with `ref.onDispose`. It is injected into
the API data source and closed when the owning `ProviderContainer`/`ProviderScope` is disposed;
widgets and individual screen controllers do not close the shared client. Screen/session
notifiers own request generations and invalidate them in their disposal callback. A late
completion must be ignored after generation change, route departure, or notifier disposal,
whether or not the transport supports aborting the request. Tests verify client closure and
late-completion suppression on provider disposal. Read
`SMART_RAIL_API_BASE_URL` from a compile-time define per environment; reject a missing or
malformed value in a visible development configuration error instead of silently using a
production host. Keep secrets out of this public-read configuration. Android release builds
must declare Internet permission; emulator-to-host access uses `10.0.2.2` when appropriate,
while a physical device needs a host address reachable on its network. Do not enable
cleartext traffic as a production workaround.

### Feature structure

Widgets are presentation-only. Riverpod notifiers own screen/session state and call a
domain repository interface. The data implementation calls a typed Railway Network client;
wire DTO parsing and conversion to feature models happen only at that boundary. Shared
loading, empty, unavailable, validation, planned-time, and accessible-feedback components
live under `core` and contain no railway policy.

```text
lib/
  main.dart
  app/
    app.dart
    app_config.dart
    providers.dart   # app-scoped shared API client owner
    router.dart
    theme/
  core/
    network/       # HTTP client, problem parsing, request/error primitives
    widgets/       # shared async-state and accessible feedback components
  features/
    railway_search/
      presentation/ # search, results, service-detail screens and controls
      application/  # providers/notifiers and immutable view state
      domain/       # station, criteria, service, route and repository interface
      data/         # API DTOs, parser, repository implementation
```

Keep this in the existing `lib/` project, without a separate app package or generated
layer. Platform configuration remains in `android/` and `ios/`. Tests follow the same
feature boundaries in `test/features/railway_search/` with `test/core/network/` for shared
HTTP parsing. This feature will replace—not coexist with—the starter counter screen when
implementation is approved.

### Packages and decisions

The minimum proposed set and alternatives are evaluated in [research.md](research.md):

| Need | Selection | Reason |
|---|---|---|
| State management and composition | `flutter_riverpod ^3.4.3` | Async-aware state, explicit repository providers, isolated overrides, and no separate DI container. Use hand-written Notifiers; skip generator. |
| Navigation | `go_router ^18.1.0` | Declarative route paths, parsed parameters, and a retained push/pop stack for Search → Results → Details. No auth redirects or route code generation. |
| HTTP | `http ^1.6.0` | Small cross-platform client, injectable `Client`, mock transport and abortable requests; no interceptor-heavy networking framework is needed. |
| Date formatting | `intl ^0.20.3` | Locale-aware date labels. Format calendar and schedule components without timezone conversion. |
| JSON | Dart `dart:convert` | Manual explicit DTO parsing is adequate for the small, stable contract and avoids generated code/build_runner. |
| Dependency injection | Riverpod providers and constructor parameters | Sufficient composition and test substitution without GetIt/injectable. |
| Tests | Existing `flutter_test`, `http/testing.dart`, Riverpod overrides | Existing widget-test SDK plus deterministic fakes; add no mock/codegen package initially. |

Recheck version solving against the installed Flutter 3.47.2/Dart 3.13.2 when dependencies
are introduced. No package is installed or added to `pubspec.yaml` in this planning stage.

## Screens, Routing, and State

Use three routes: `/railway/search`, `/railway/results`, and
`/railway/services/:serviceId`. Search is the root. Submit valid criteria once, show a
loading result state, then push Results for either a non-empty or empty 200 response.
Selecting a result pushes Details with its result summary and IDs. Back pops to the exact
previous state. Search criteria and successful results belong to the retained search flow,
not transient widget fields that disappear when a route is pushed. The Phase 1 app shell
must compile: all three route target placeholder screens are created before `router.dart`
imports/registers them, and `main.dart` then composes that router.

Search load has its own loading, ready, empty-stations, and unavailable/retry states. Form
state holds optional origin, destination, and a calendar-only travel date; same-station and
missing-field validation happens before sending. Backend validation remains authoritative
for unknown/unpublished IDs and date eligibility. Any criterion edit immediately removes
the old result snapshot and invalidates a pending search result so it cannot appear under
edited criteria.

Represent screen states explicitly rather than deriving errors from empty collections:

| Area | States and transitions |
|---|---|
| Stations | initial → loading → ready/empty/unavailable; retry reloads station list. |
| Search | editing/invalid → loading → results or empty; service-search 400 → criteria correction; 403 → access unavailable + Back only; 5xx/network/timeout → unavailable + retry. Duplicate submit is disabled or ignored. Search does not add a 404-specific state because its operation does not document 404. |
| Results | loading → populated/empty; criteria change clears results; retry repeats the same criteria; a late result after leaving/editing is discarded. |
| Details | service detail and route stops load independently; either can be data, empty stops, access-denied, not-found, or unavailable. A 400 for either detail operation identifies its affected section as invalid/unavailable, preserves successful sibling and result-summary information, provides Back, and does not retry the unchanged malformed ID. One retry for retryable failures touches only that read. Preserve successful or retained result-summary sections. |
| Previously loaded data | If shown after a failed refresh, mark not refreshed and show client retrieval time when available; never claim source update time or live freshness. |

For each new operation, assign an incrementing request generation and accept a completion
only if its generation and route/session owner still match. Abort transport work when the
client supports it, but treat generation checks/disposal checks as the correctness guard.
Back stays available while requests are pending. A completion after leaving a screen cannot
reopen it, navigate, or mutate the departed screen. Distinct pending state prevents duplicate
requests and duplicate route pushes.

## API Integration and Error Handling

Use `Uri` path/query builders with the configured base URI; send UUID IDs as contract strings
and travel date as `YYYY-MM-DD`. Decode UTF-8 response bytes and parse strictly against the
schemas. Shared error classification yields typed failures, not raw exceptions. `Problem`
parsing tolerates absent optional `detail`, `code`, `errors`, `instance`, or `traceId`; use
safe known field errors for 400, generic passenger language otherwise, and never display
stack traces or diagnostics. Do not assume every response body is JSON.

For 400 responses, apply FR-011 by operation: the service-search operation retains criteria
and allows the passenger to correct them; the service-detail and route-stop operations
identify only the affected section as invalid/unavailable, preserve successful sibling and
result-summary information, keep Back available, and do not retry an unchanged malformed
path ID. This does not change the API contract. The per-operation field and status matrix lives in
[contracts/railway-network-client.md](contracts/railway-network-client.md). Important
behavior: 200 with empty services is a valid empty result; 400 retains criteria; the
clarified public 403 state has Back but no login/retry; detail-operation 400/404 mark only
the affected detail section unavailable; 500/default, 503, transport errors and timeout are unavailable states with
retry for the failed read. The spec's 403 passenger action is honored although the current
OpenAPI operation response lists do not enumerate 403. No new server meaning is inferred.

Call service detail and route stops independently using the selected result's
`serviceId` and `routeId`; either failure leaves the other section usable. The result already
contains planned origin/destination calls, so preserve it in detail even if one or both detail
reads fail. `ServiceDetail.schedule.firstDepartureTimeLocal` is separately labeled as the
planned first-call time; do not claim it is the selected origin time unless it matches.
`RouteStops.stops` are presented by `stopOrder`; no intermediate schedule times exist.

Keep raw `ScheduledCall.scheduledAt` and extract its literal local date/clock/offset text
without converting it to UTC or device time. Use the date portion and selected origin
travel-date comparison for the later-day cue specified in the approved UX. `travelDate` is
a full calendar date, not a timestamp. Do not calculate which services should run, reorder
results, or augment any schedule.

## UI and Accessibility

Use the platform date picker and searchable station choice controls, result cards with
origin/destination planned calls, and an ordered route-stop list. Common async components
provide loading, empty, unavailable, validation, access-denied, and retry/back treatments.
Keep text explanations and icons/symbols independent of color. Supply semantic labels,
selected/disabled states, logical focus traversal, announcements for changing results and
errors, and usable layouts at text scaling and narrow/wide mobile sizes. SR-103 and SR-104
checks are explicitly stretch/Should Have and must not be reported as Must Have delivery.

## Testing Strategy and Traceability

Use isolated fixture-based tests by default and real-backend validation only when an
environment URL and seeded data are available. [quickstart.md](quickstart.md) defines the
future validation commands and setup.

| Test layer | Main verification | Trace |
|---|---|---|
| Dart unit/model | Required/optional DTO fields, UUID/date mapping, malformed JSON, offset-preserving planned-time display, safe Problem mapping | FR-004, FR-006, FR-009, FR-011–016, FR-020; SR-002/003/004/013/114 |
| HTTP client/repository | Exact four GET paths/query values, anonymous headers, 200 populated/empty, 400, 403, 404, 500/default, 503, invalid body, timeout/network errors | FR-001, FR-005, FR-008, FR-011–016, FR-020; SR-013/089/114 |
| Riverpod/application | All state transitions, duplicate submit, edit clearing, old request suppression, independent detail failures and per-section retry, last-retrieved metadata | US1–US3; FR-005, FR-007, FR-010–017, FR-021–022; SR-109/114 |
| Widget | Initial/loading/success/empty/error flows, station/date selection, useful validation, honest planned labels, accessible announcements and text scaling | FR-002–019; SR-002/004/013/103/104/109/114; SC-001–SC-004, SC-006–SC-007 |
| Navigation | Search → Results → Details → Back restores criteria/results; Back during each read; late completion does not reopen/alter departed route; verify provider disposal | FR-017, FR-021–022; SC-005; all five clarifications |
| Integration | Primary acceptance runs on Android emulator against approved backend/base URL and seeded data; physical-device smoke is supplementary; no invented fields or auth; record prerequisite status | SR-002/003/004/013/089; US1/US2; SC-001, SC-004–SC-005 |
| Accessibility acceptance | Semantics names/focus/announcements, increased text scale, contrast, touch-operability review; retain Should Have evidence tier | FR-018–019; SR-103/104 |

All five clarification answers must be explicit test cases: criterion edits clear results;
later-day calls preserve supplied date/time and receive the label; partial detail success
retains the good section and retries only the failed section; 403 shows Back only; and Back
during loading remains available while late completions are ignored. Automated contract and
state tests must cover all relevant documented response classes and network loss.

## Technical Risks and Prerequisites

| Risk/prerequisite | Impact | Mitigation or review note |
|---|---|---|
| No approved environment API host or seed data is present in the Flutter repository. | Real-backend integration cannot be completed from this checkout alone. | Obtain a reachable base URL and deterministic published data from the backend owner; keep URL outside source. |
| OpenAPI operation response lists omit 403 although SR-114 and the approved spec require access-denied handling. | A test environment may not produce this state through the current public operations. | Keep the specified conditional client behavior; exercise through a controlled gateway/test response if available; do not alter the API contract. |
| No response-time SLO is approved. | Timeout choice is provisional and slow backend requests may be classified unavailable. | Start with a configurable 10-second bounded timeout; validate on backend/device and tune without changing passenger semantics. |
| Android release manifest currently has no Internet permission; local backend may be cleartext-only. | Release calls will fail without permission; broad HTTP allowances weaken transport security. | Add Internet permission during implementation. Prefer HTTPS; confine any required local cleartext allowance to debug configuration. |
| The integration host or deterministic published seed data may be unavailable. | Real-backend acceptance cannot be executed yet. | Record `BLOCKED` with the missing prerequisite; never treat unexecuted integration as `PASS`. Complete independent local verification and matrix reporting. |
| A physical device/network may be unavailable. | Supplementary physical-device smoke cannot be run. | Record `BLOCKED` or `NOT RUN`; this does not block the primary Android emulator integration acceptance path. |

## Implementation Sequence (planning only)

1. App foundation: replace starter shell, configure app/theme/route shell/environment
   composition, and establish feature folders.
2. API client and models: implement typed contract DTOs, strict parsers, Problem mapping,
   environment URL, timeout and injectable transport.
3. Search state and station selection: load stations, represent selection/date, validate
   same-station/missing criteria, and define initial/loading/error states.
4. Search results: submit search, handle success/empty/error/retry, duplicate requests,
   criteria edit clearing and retention.
5. Service details and route stops: push details, request both reads independently, present
   planned result summary/service metadata/ordered stops and partial failures.
6. Failure recovery and navigation: complete 403/404/unavailable treatment, stale labels,
   section retry, Back during loading and response-generation guards.
7. Automated tests: unit/API/state/widget/navigation/accessibility suite mapped above.
8. Real backend integration: configure environment endpoint and seeded data; verify public
   anonymous reads on an Android emulator as the primary integration acceptance path. A
   physical-device smoke run is supplementary unless an approved requirement changes this.
9. Final acceptance verification: review all Must Have flow evidence and Should Have
   stretch status against spec/checklist before implementation completion.

This sequence is design guidance only. `tasks.md` is intentionally not created.

## Constitution Re-check After Design

All five gates remain PASS. The design keeps implementation boundaries inside the Flutter
passenger client, names only the approved endpoints and fields, does not add authentication
or offline persistence, preserves planned-data labeling and response distinctions, and
preserves each approved priority. No backend or OpenAPI changes are proposed. No unjustified
complexity or unresolved requirement clarification remains.

## Project Structure

### Documentation (this feature)

```text
specs/001-railway-service-search/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
└── contracts/
    └── railway-network-client.md
```

The existing `spec.md` and `checklists/requirements.md` remain the approved input artifacts.
No `tasks.md` is created.

### Source Code (repository root)

```text
lib/
├── main.dart
├── app/
│   ├── app.dart
│   ├── app_config.dart
│   ├── router.dart
│   └── theme/
├── core/
│   ├── network/
│   └── widgets/
└── features/railway_search/
    ├── presentation/
    ├── application/
    ├── domain/
    └── data/

test/
├── core/network/
└── features/railway_search/
    ├── data/
    ├── application/
    ├── presentation/

integration_test/
└── railway_search/
```

**Structure Decision**: Retain the current single Flutter app and introduce one
feature-based railway-search slice with presentation, application, domain, and data/API
responsibilities. Keep shared configuration, network primitives, and controls in `app/`
and `core/`; do not split projects or add architecture scaffolding.

## Complexity Tracking

No constitution violations.
