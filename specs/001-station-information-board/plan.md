# Implementation Plan: SmartRail Station Information & Departure Board

**Branch**: `001-station-information-board` | **Date**: 2026-10-18 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification and approved product clarifications for the SmartRail Station Information & Departure Board.

## Summary

Build one passive public station display using the repository's React, TypeScript, Vite and Tailwind stack. Validate a deployment-provided station ID against the published Railway Network station list, then render only departures returned for that station by the future Journeys & Status departure source. Keep schedule values distinct from dated departures and operational updates. The display must automatically refresh when authoritative data becomes available, expose stale/unavailable state during disconnection, and revalidate before claiming recovery. No future response schema or refresh mechanism is assumed; live integration is gated on an approved backend contract.

## Technical Context

**Language/Version**: TypeScript 6.0.3 (lockfile); React and React DOM 19.3.1 (lockfile)

**Primary Dependencies**: Vite 8.3.4, `@vitejs/plugin-react` 6.1.2 and Tailwind CSS / `@tailwindcss/vite` 4.3.3 (lockfile). Feature-required test dependencies are not installed: plan for Vitest 5, jsdom, React Testing Library, `@testing-library/dom`, `@testing-library/user-event`, `@testing-library/jest-dom`, and Playwright Test. Automated browser accessibility checks may use axe-core in Playwright; color contrast must be reviewed in a real browser, not jsdom.

**Storage**: No application database. Keep the last successful response in the running display session for disconnected presentation. Do not add durable browser storage, cache expiry or retention behavior until approved freshness/integrity policy exists. If the display starts offline with no successful response, show unavailable.

**Testing**: Existing scripts are `npm run lint` and `npm run build`. Add Vitest component/state tests, React Testing Library tests, and Playwright browser journeys as feature work. Node.js is v24.18.0 locally; Vite 8 and the proposed Vitest 5 baseline support this runtime. Exact added package versions must be pinned in the implementation change and lockfile.

**Target Platform**: Browser-based passive station display, landscape prototype viewports 1920 × 1080 and 1366 × 768. No hardware, viewing distance or additional resolution is approved. Vite requires Node.js 20.19+ or 22.12+ for development/build tooling.

**Project Type**: Standalone frontend web application integrated with the SmartRail REST backend.

**Performance Goals**: No numeric load, refresh, freshness or latency threshold is approved. Do not add one. Verify completeness and correctness at both approved viewports; final live update timing depends on the backend contract and an approved target.

**Constraints**: Public read-only information; exactly one deployment-provided station; no passenger selector, station management, controller actions, kiosk, or guessed data. Existing Railway Network APIs provide reference/scheduled service information only. Future departure/journey endpoints and their delivery, schema, revision, timestamp, error and cache semantics remain unverified integration dependencies.

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

No constitution violation requires an exception. The feature can be decomposed into tasks, but tasks that depend on live departures must remain blocked until backend contract approval and verification.

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
3. **Departure integration boundary**: Define a feature-owned adapter seam for station departures and operational updates. Expected future endpoints are `GET /api/stations/{id}/departures`, `GET /api/journeys/{id}`, and `GET /api/journeys/{id}/updates`; the latter two may be unnecessary if the station departure projection is sufficient. Isolate the seam from UI rendering so an approved response can be mapped without spreading contract details across components. Do not write a proposed OpenAPI file or response type for future endpoints. Live adapter work is gated on the Journeys & Status owner supplying and approving its contract.
4. **Display state**: Represent loading, ready/current, empty, unavailable/configuration, error, disconnected with cached/stale information, and recovered states distinctly. Retain the last successful response in active runtime memory on disconnection. Show a last-updated value only when its authoritative meaning is verified and supplied. After reconnect, keep stale indication until an authoritative revalidation succeeds.
5. **Refresh ownership**: Isolate refresh orchestration behind the adapter/state boundary. Do not choose polling versus push or a numeric interval until the backend contract and product owner settle them. A prototype fixture can drive UI work but is always marked as fixture/demo data and cannot satisfy live-integration acceptance.
6. **Status presentation**: Render scheduled time and operational values in separate labeled roles. Render status meaning as text/symbol in addition to color. Do not calculate or infer delay, platform, ETA, status or “upcoming” eligibility in the frontend.
7. **Display layout**: Use responsive layout rules and validate the two approved landscape profiles. Preserve all required fields and prominent disruption information at 1366 × 768 as well as 1920 × 1080. No touch/kiosk interaction is planned.
8. **Test stack**: Reuse the existing Vite configuration where possible for Vitest. Vitest 5 requires Vite ≥6.4 and Node ≥22.12; the lockfile resolves Vite 8.3.4 and local Node is 24.18. React Testing Library requires `@testing-library/dom`; jsdom is a separate test environment package. Playwright checks real-browser behavior and both profiles. No test packages are installed by this planning change.

## Requirement-to-Verification Map

| Requirement | Planned verification |
|---|---|
| SR-054 | Fixture with multiple stations: only configured station departures appear; scheduled time and latest available status are shown; empty list state is clear. Live acceptance awaits approved departure contract. |
| SR-056 | Authorized platform/delay/cancellation fixture updates the correct departure and displays source-provided updated time when contract semantics are verified. |
| SR-058 | State/adapter test and browser scenario show automatic update without a passenger action; do not assert interval or latency until approved. Live test awaits backend change-delivery contract. |
| SR-059 | Simulate disconnect before and after first successful response; verify unavailable vs visible cached/stale state; reconnect and verify warning remains until revalidation succeeds. |
| SR-061 | Browser acceptance demonstrates a working station-specific passive display at each approved landscape viewport. |
| SR-001 | Document/verify the station board consumes shared authoritative station/journey data rather than an independent source. |
| SR-002 | Configured station ID matches a published public station and its authoritative name is shown; invalid/unpublished IDs never fall back. |
| SR-003 | If ordered route data is displayed or consumed, confirm it remains in authoritative route order; do not add route UI solely to satisfy this shared platform requirement. |
| SR-004 | Display service identity/schedule context only when supported by published data and clearly distinct from dated journey state. |
| SR-010 | Only authorized published status is rendered; source/revision/update-time presentation follows verified response meaning. |
| SR-035 | Missing live position/ETA is explicitly unknown/last known; no display-generated estimate. |

## Implementation Phases and Dependency Gates

1. **Foundation (independent)**: establish proposed app/feature boundaries; validate public station configuration; create the test harness and fixture/live-data labeling. Can proceed without the future departure API.
2. **Board and reference identity (independent with fixtures)**: station header and configuration/unavailable states; board and row presentation; scheduled-versus-operational labeling; loading/empty/error states; responsive/accessibility work for both profiles. Fixtures must be visibly non-live.
3. **Connectivity/state handling (independent UI, contract-gated semantics)**: active-session stale presentation, no-data unavailable state and revalidation state transitions. Keep update freshness values/timestamps opaque until contract definition.
4. **Live departure adapter (blocked on backend)**: integrate only after approved departure/journey/update schemas, status projection, update mechanism, ordering, time semantics and error behavior exist. Map source values without adding client business rules.
5. **Integrated verification (blocked on environment and backend)**: contract-backed tests, authorized update propagation, reconnection/revalidation, both browser viewport profiles, WCAG 2.2 AA evidence and trace completion for relevant SR criteria.

## Risks and Open Dependencies

- **High — future contracts absent**: no departure response schema, operational field meanings, refresh delivery, revision order, timestamp semantics, cache validity or error conventions. Do not claim live completion or invent a contract. Backend owner must publish/approve and verify these before the plan's Implementation Phase 4 (Live departure adapter); this gate blocks live integration only, while fixture-backed React development and verification in Phases 1 to 3 proceed independently.
- **High — station date/time semantics**: “upcoming” eligibility, service date, timezone and cross-midnight ordering are not specified by the current Railway Network contract. The backend must define them for departure response; frontend must not derive them from reusable schedule search.
- **Medium — offline continuity**: active-session memory can display the last successful information after a disconnect; a fresh start while offline shows unavailable. Durable offline storage would require approved cache integrity/expiry policy and is not included by this plan.
- **Medium — test dependencies absent**: `package.json` has no Vitest, Testing Library or Playwright dependencies/scripts. Their addition belongs in implementation tasks; this planning change does not install them.
- **Medium — final freshness target unset**: no quantitative timing guarantee can be accepted until backend capability and product expectations are approved.

## Constitution Check (Post-Design)

| Gate | Result |
|---|---|
| Approved requirement traceability preserved | PASS — primary and applicable shared IDs map to planned tests/evidence above. |
| Public information only; no privileged management or kiosk scope | PASS |
| Backend owns status truth; future API schema not invented | PASS WITH LIVE-INTEGRATION GATE |
| Scheduled services remain distinct from dated departures | PASS |
| WCAG 2.2 AA and status text/symbol alternatives planned | PASS — evidence required at implementation verification. |
| Loading, empty, failure, stale and recovery states covered | PASS |
| No unsupported intervals, cache expiry, latency or screen assumptions | PASS |
| Feature-based React architecture and proportionate dependencies | PASS |

No constitutional exception is requested. Task generation may proceed for independent work while preserving the live integration block. Final feature verification remains NO-GO until the backend contract dependencies are resolved and tested.

## Complexity Tracking

No constitution violations or additional frameworks are proposed. No complexity exception is required.
