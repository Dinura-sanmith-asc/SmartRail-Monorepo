# Implementation Plan: SmartRail Station Information & Departure Board

**Branch**: `001-station-information-board` | **Date**: 2026-10-18 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification and approved product clarifications for the SmartRail Station Information & Departure Board.

## Summary

Build one passive public station display using the repository's React, TypeScript, Vite and Tailwind stack. Validate a deployment-provided station ID against the published Railway Network station list, then render only departures returned for that station by the approved Journeys & Status v1.0.0 station-departures contract. Keep schedule values distinct from dated departures and operational updates. The approved prototype client policy is configurable HTTP polling of that snapshot, with a 30000 ms default, stale/unavailable state during failures, and revalidation before claiming recovery. Production cadence, backend capacity and freshness guarantees remain unresolved.

## Technical Context

**Language/Version**: TypeScript 6.0.3 (lockfile); React and React DOM 19.3.1 (lockfile)

**Primary Dependencies**: Vite 8.3.4, `@vitejs/plugin-react` 6.1.2 and Tailwind CSS / `@tailwindcss/vite` 4.3.3 (lockfile). Feature-required test dependencies are not installed: plan for Vitest 5, jsdom, React Testing Library, `@testing-library/dom`, `@testing-library/user-event`, `@testing-library/jest-dom`, and Playwright Test. Automated browser accessibility checks may use axe-core in Playwright; color contrast must be reviewed in a real browser, not jsdom.

**Storage**: No application database. Keep the last successful response in the running display session for disconnected presentation. Do not add durable browser storage, cache expiry or retention behavior until approved freshness/integrity policy exists. If the display starts offline with no successful response, show unavailable.

**Testing**: Existing scripts are `npm run lint` and `npm run build`. Add Vitest component/state tests, React Testing Library tests, and Playwright browser journeys as feature work. Node.js is v24.18.0 locally; Vite 8 and the proposed Vitest 5 baseline support this runtime. Exact added package versions must be pinned in the implementation change and lockfile.

**Target Platform**: Browser-based passive station display, landscape prototype viewports 1920 × 1080 and 1366 × 768. No hardware, viewing distance or additional resolution is approved. Vite requires Node.js 20.19+ or 22.12+ for development/build tooling.

**Project Type**: Standalone frontend web application integrated with the SmartRail REST backend.

**Performance Goals**: Prototype HTTP polling defaults to 30000 ms through public Vite configuration. This client interval is not a production freshness SLA, backend capacity commitment, or guaranteed update latency. No cache-expiry or freshness threshold is approved. Verify completeness and correctness at both approved viewports.

**Constraints**: Public read-only information; exactly one deployment-provided station; no passenger selector, station management, controller actions, kiosk, or guessed data. Existing Railway Network APIs provide reference/scheduled service information only. Journeys & Status v1.0.0 provides an approved date-scoped station-departure snapshot with verified schema, ordering, field meanings and errors. It does not define automatic delivery, “upcoming-only” semantics, a production refresh policy, cache expiry, client revision-comparison behavior, backend capacity guarantees, or all timestamp presentation meanings.

**Scale/Scope**: One display instance per configured station; one station context per display; one station departure board and its loading, empty, error, unavailable, disconnected/stale and recovered states.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Plan evidence | Gate |
|---|---|---|
| I. Approved Scope and Requirement Traceability | Plan, conceptual data model and test matrix map SR-054/056/058/059/061 plus applicable SR-001/002/003/004/010/035 to spec scenarios and verification. | PASS |
| II. Honest Railway Information and Display Responsibility | The display consumes the station-departure source; service search is not treated as a dated departure. Planned, operational, unknown and stale meanings remain separate. No operations UI is added. | PASS |
| III. Contract-First Integration and Data Ownership | Only the checked-in Railway Network contract is authoritative now. Future Journeys & Status contracts are documented as gates, without invented schemas or endpoint behavior. | PASS |
| IV. Accessible, Readable and Resilient Presentation | Text/symbol status, WCAG 2.2 AA, explicit connectivity states and both approved landscape profiles are in the test plan. No numeric freshness limit is invented. | PASS |
| V. Secure, Testable and Maintainable Frontend | Feature-based frontend; non-secret deployment configuration; Vitest, Testing Library, Playwright, TypeScript, ESLint and build checks are planned. | PASS |
| Governance and traceability | Decisions remain within approved scope and contracts. Product decisions Q1/Q2/Q3 behavior are recorded; backend contract unknowns remain assigned dependencies. | PASS WITH INTEGRATION GATE |

No constitution violation requires an exception. The v1.0.0 station-departure contract and T026 live snapshot verification are complete. Prototype polling can proceed under the recorded owner decision; production guarantees and final live update/recovery acceptance remain gated.

## Project Structure

### Documentation (this feature)

```text
specs/001-station-information-board/
├── spec.md
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
└── contracts/
    └── README.md
```

`tasks.md` is intentionally left for `$speckit-tasks`.

### Source Code (repository root)

```text
src/
├── App.tsx                     # existing application composition; replace starter shell
├── App.css                     # existing starter stylesheet; remove/replace as feature styling moves
├── app/
│   └── config.ts               # public runtime/build configuration validation
├── features/
│   └── station-display/
│       ├── api/                # station reference client and departure port/adapter
│       ├── components/         # header, board, row, status, freshness/state views
│       ├── hooks/              # display lifecycle, refresh and connectivity state
│       ├── model/              # station/departure/display view types and mapping
│       └── station-display.css # feature-specific responsive display styling
├── shared/
│   ├── api/                    # shared HTTP/error primitives only when needed
│   └── ui/                     # small reusable presentation primitives
├── index.css                  # Tailwind entry and global accessible defaults
└── main.tsx                   # existing React bootstrap

src/features/station-display/**/*.test.ts(x)  # unit/component tests
playwright.config.ts
e2e/
└── station-display.spec.ts     # browser acceptance flows and approved viewports
```

**Structure Decision**: This is a single frontend with one feature, so retain the existing `src/App.tsx` composition and `src/main.tsx` bootstrap, then add a focused `features/station-display` boundary plus minimal configuration/shared areas. Replace the starter `App.css` styling as needed without preserving unused scaffold styles. Keep source/component tests colocated; keep browser-level acceptance flows in `e2e/`. Do not add a router or state-management framework for this single-screen passive display unless later evidence demonstrates a need.

## Architecture and Implementation Decisions

1. **Startup configuration**: Read a deployment-provided station ID and API base URL from Vite's public configuration surface (proposed names: `VITE_STATION_ID`, `VITE_API_BASE_URL`). These values are public and MUST contain no secret. Reject missing/malformed configuration before requesting departures. Verify the configured stable station ID against `GET /api/stations`; only a matching published station name is valid. Never fall back to an arbitrary station.
2. **Railway Network adapter**: Use the checked-in OpenAPI contract for station identity and any reference data actually needed. The route and service endpoints remain planned schedule/reference data; they do not supply a station departure board. Avoid fetching unrelated route/service detail unless a verified contract-supported display need requires it.
3. **Departure integration boundary**: Use the verified Journeys & Status v1.0.0 contract registered in `contracts/README.md`. The station-departures snapshot is sufficient for the current public board; journey detail and update-history reads are not needed for snapshot polling. Keep the adapter boundary separate from UI rendering and do not extend or rewrite the backend contract.
4. **Display state**: Represent loading, ready/current, empty, unavailable/configuration, error, disconnected with cached/stale information, and recovered states distinctly. Retain the last successful response in active runtime memory on disconnection. Show a last-updated value only when its authoritative meaning is verified and supplied. After reconnect, keep stale indication until an authoritative revalidation succeeds.
5. **Refresh ownership**: Keep polling orchestration in the departure source/adapter layer, independent of presentation components. Fetch immediately after station validation, then use configurable `VITE_STATION_DEPARTURE_REFRESH_INTERVAL_MS` with a 30000 ms default. This Vite variable is public and normally embedded at build time; it must contain no secret. Missing configuration uses the default; invalid configuration must reject polling rather than permit zero-delay/uncontrolled requests. Recompute the station-local date in `Asia/Colombo` for each request, preserve backend ordering and values, and prevent overlapping requests. A reconnect event may trigger an immediate revalidation. Failed refreshes retain stale/cached labeling until a successful response. This is a prototype client policy, not a backend push contract, production SLA, backend latency guarantee or cache-expiry decision. Fixtures remain visibly non-live and cannot satisfy live-integration acceptance.
6. **Status presentation**: Render scheduled time and operational values in separate labeled roles. Render status meaning as text/symbol in addition to color. Do not calculate or infer delay, platform, ETA, status or “upcoming” eligibility in the frontend.
7. **Display layout**: Use responsive layout rules and validate the two approved landscape profiles. Preserve all required fields and prominent disruption information at 1366 × 768 as well as 1920 × 1080. No touch/kiosk interaction is planned.
8. **Test stack**: Reuse the existing Vite configuration where possible for Vitest. Vitest 5 requires Vite ≥6.4 and Node ≥22.12; the lockfile resolves Vite 8.3.4 and local Node is 24.18. React Testing Library requires `@testing-library/dom`; jsdom is a separate test environment package. Playwright checks real-browser behavior and both profiles. No test packages are installed by this planning change.

## Requirement-to-Verification Map

| Requirement | Planned verification |
|---|---|
| SR-054 | Fixture with multiple stations: only configured station departures appear; scheduled time and latest available status are shown; empty list state is clear. Live acceptance awaits approved departure contract. |
| SR-056 | Authorized platform/delay/cancellation fixture updates the correct departure and displays source-provided updated time when contract semantics are verified. |
| SR-058 | Fake-clock/source tests verify immediate initial fetch, configurable 30000 ms default polling, date recomputation across Asia/Colombo midnight, non-overlap and stale retention on failed refresh. T030 verifies live automatic snapshot re-fetch without asserting a production latency target. |
| SR-059 | Simulate disconnect before and after first successful response; verify unavailable vs visible cached/stale state; reconnect and verify warning remains until revalidation succeeds. |
| SR-061 | Browser acceptance demonstrates a working station-specific passive display at each approved landscape viewport. |
| SR-001 | Document/verify the station board consumes shared authoritative station/journey data rather than an independent source. |
| SR-002 | Configured station ID matches a published public station and its authoritative name is shown; invalid/unpublished IDs never fall back. |
| SR-003 | If ordered route data is displayed or consumed, confirm it remains in authoritative route order; do not add route UI solely to satisfy this shared platform requirement. |
| SR-004 | Display service identity/schedule context only when supported by published data and clearly distinct from dated journey state. |
| SR-010 | Only authorized published status is rendered; source/revision/update-time presentation follows verified response meaning. |
| SR-035 | Missing live position/ETA is explicitly unknown/last known; no display-generated estimate. |

## Implementation Phases and Dependency Gates

1. **Foundation (independent)**: establish proposed app/feature boundaries; validate public station configuration; create the test harness and fixture/live-data labeling. Can proceed without making calls to the verified departure API.
2. **Board and reference identity (independent with fixtures)**: station header and configuration/unavailable states; board and row presentation; scheduled-versus-operational labeling; loading/empty/error states; responsive/accessibility work for both profiles. Fixtures must be visibly non-live.
3. **Connectivity/state handling (independent UI, contract-gated semantics)**: active-session stale presentation, no-data unavailable state and revalidation state transitions. Keep update freshness values/timestamps opaque until contract definition.
4. **Live departure adapter and prototype polling**: T024–T026 establish the approved v1.0.0 snapshot contract and adapter. Implement the approved prototype HTTP polling policy independently of the UI; the delivery mechanism chosen here is repeated reads of the date-scoped snapshot endpoint. Preserve contract data and errors without adding client railway business rules. Backend capacity and production freshness policy remain open.
5. **Integrated verification (blocked on environment and backend)**: contract-backed tests, authorized update propagation, reconnection/revalidation, both browser viewport profiles, WCAG 2.2 AA evidence and trace completion for relevant SR criteria.

## Risks and Open Dependencies

- **High — production refresh policy**: the project owner approved a 30-second default prototype poll, but production cadence, backend capacity and freshness guarantees remain unapproved. Do not represent the prototype interval as an SLA.
- **High — station departure scope**: v1.0.0 defines `Asia/Colombo` date matching, ordering and overnight station-local date, but its response is not explicitly upcoming-only. Do not invent a cutoff or reorder/filter based on frontend schedule logic; resolve the product/backend boundary before claiming the complete upcoming-departures acceptance.
- **Medium — revision and timestamps**: the contract supplies per-journey revision/source/updatedAt but does not define frontend revision reconciliation across snapshots or whether/how every timestamp is displayed. Preserve values and defer unverified interpretations.
- **Medium — offline continuity**: active-session memory can display the last successful information after a disconnect; a fresh start while offline shows unavailable. Durable offline storage would require approved cache integrity/expiry policy and is not included by this plan.
- **Medium — test dependencies absent**: `package.json` has no Vitest, Testing Library or Playwright dependencies/scripts. Their addition belongs in implementation tasks; this planning change does not install them.
- **Medium — final freshness target unset**: prototype interval does not resolve cache expiry, freshness age, or production timing guarantees.

## Constitution Check (Post-Design)

| Gate | Result |
|---|---|
| Approved requirement traceability preserved | PASS — primary and applicable shared IDs map to planned tests/evidence above. |
| Public information only; no privileged management or kiosk scope | PASS |
| Backend owns status truth; approved API schema is consumed without extension | PASS WITH LIVE-INTEGRATION GATE |
| Scheduled services remain distinct from dated departures | PASS |
| WCAG 2.2 AA and status text/symbol alternatives planned | PASS — evidence required at implementation verification. |
| Loading, empty, failure, stale and recovery states covered | PASS |
| Prototype interval is owner-approved and explicitly separated from production SLA, cache expiry and latency guarantees | PASS |
| Feature-based React architecture and proportionate dependencies | PASS |

No constitutional exception is requested. Task generation may proceed for independent work while preserving the live integration block. Final feature verification remains NO-GO until the backend contract dependencies are resolved and tested.

## Complexity Tracking

No constitution violations or additional frameworks are proposed. No complexity exception is required.
