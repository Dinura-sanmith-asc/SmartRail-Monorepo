# Research: Station Information & Departure Board

**Date**: 2026-10-18  
**Scope**: Record planning decisions using the approved station display spec, local project, approved source documents, and current official tooling documentation. Journeys & Status OpenAPI v1.0.0 is an approved external contract; this research does not modify or extend it.

## Decision 1 — Preserve the existing frontend stack and organize by feature

**Decision**: Keep React + TypeScript + Vite + Tailwind CSS. Add a small application configuration/bootstrap boundary, a `station-display` feature boundary, and shared UI/API primitives only as needed. Do not add a router or global state framework for a single passive screen.

**Rationale**: The project already uses React 19.3.1, TypeScript 6.0.3, Vite 8.3.4, Tailwind 4.3.3 and the React/Tailwind Vite plugins. Current source is a starter shell (`src/App.tsx`, `src/App.css`, `src/index.css`, `src/main.tsx`), with no established feature structure or test suite. A feature boundary matches the constitution while keeping the small application simple.

**Alternatives considered**: Replace the toolchain or add a router/state framework. Rejected because the approved stack is already installed and the feature is one public display view.

## Decision 2 — Keep station reference and live departure ownership separate

**Decision**: Validate the deployment station ID against the approved public station list. Treat the existing route/service endpoints as reference/planned-schedule sources only. Use an isolated feature adapter boundary for dated station departures and operational updates, mapping only the approved Journeys & Status v1.0.0 contract; do not extend or redefine its response schema.

**Rationale**: The checked-in Railway Network OpenAPI defines `GET /api/stations`, `GET /api/routes/{id}/stops`, `GET /api/services` and `GET /api/services/{id}`. It explicitly states that service search is planned schedule information and not a dated operational journey or live status. The separately approved Journeys & Status v1.0.0 contract defines `/api/stations/{id}/departures`, `/api/journeys/{id}` and `/api/journeys/{id}/updates`; the station display uses the verified station snapshot projection for its board.

**Alternatives considered**: Filter reusable service search results to synthesize departures or extend the approved response with unsupported fields. Rejected because these would misrepresent scheduled services and invent backend behavior.

## Decision 3 — Make refresh orchestration replaceable and freshness conservative

**Decision**: Keep refresh orchestration in the departure source/adapter layer, independent of presentation components. The project owner approved configurable HTTP polling of the verified `GET /api/stations/{id}/departures?date=YYYY-MM-DD` snapshot endpoint for the prototype, with public Vite configuration variable `VITE_STATION_DEPARTURE_REFRESH_INTERVAL_MS` and 30000 ms default. Fetch after station validation; while connected, poll at that interval, compute the station-local date in `Asia/Colombo` on every request, and do not overlap requests or queue missed timer ticks. Missing interval configuration uses the default; invalid, non-positive, non-integer or unsupported timer values reject polling. Reconnect events may trigger immediate revalidation. A failed request marks the last successful data cached/possibly outdated; only a successful response clears that state. Preserve backend order and source values; do not derive delay, status or ETA.

**Rationale**: The verified v1.0.0 contract supplies a date-scoped snapshot and a station-local `Asia/Colombo` date parameter but no push or automatic delivery behavior. Repeated GET requests are therefore an explicitly approved prototype client policy, isolated from presentation. The 30000 ms default is not a production freshness SLA, backend latency guarantee, backend push contract, cache-expiry threshold or backend capacity commitment.

**Alternatives considered**: Backend push, a journeys update-history reader, a different prototype cadence, and durable storage/TTL. Push and cadence were not supplied by the API contract; the project owner selected HTTP snapshot polling for this prototype. The history endpoint provides no push/wait semantics and is not needed to fetch the current snapshot. Cache validity remains unresolved. Durable offline persistence is deferred because it changes exposure and stale-data risk.

## Decision 4 — Use a minimal Vite-aligned UI/browser test stack

**Decision**: Plan Vitest 5 with jsdom for unit/state tests; React Testing Library plus its `@testing-library/dom` peer, user-event and jest-dom helpers for component behavior; Playwright Test for real browser behavior at 1920 × 1080 and 1366 × 768. Consider axe-core with Playwright for automated accessibility checks; confirm contrast in a real browser.

**Rationale**: The lockfile resolves Vite 8.3.4 and Node is 24.18.0. Official Vitest 5 migration requirements are Vite ≥6.4 and Node ≥22.12, so the existing baseline meets them. Vitest reads Vite config, allowing the existing plugins to be reused. React Testing Library requires `@testing-library/dom` and is a behavior-focused helper, not a test runner. Playwright can start the Vite server and configure browser viewports. Packages are not currently installed and are not installed during planning.

**Alternatives considered**: Add Jest as another runner, use only DOM emulation for browser/display criteria, or replace Vite. Rejected: duplicative runner; simulated DOM cannot establish target viewport rendering/contrast; unnecessary toolchain replacement.

## Decision 5 — Keep offline cache in active session pending data policy

**Decision**: Plan to retain the last successful data response in active runtime memory for disconnect handling. Do not add persistent browser storage or a cache expiry rule. A page started offline without prior in-session data shows unavailable; after reconnection it revalidates before clearing stale state.

**Rationale**: Provides the approved cached/stale display behavior during a connected session while avoiding an unapproved retention/integrity duration. Persistent offline storage requires approved cache policy.

## Official sources consulted

- [Vite getting started and supported Node versions](https://vite.dev/guide/)
- [Vitest configuration with Vite](https://vitest.dev/config/)
- [Vitest 5 migration prerequisites](https://vitest.dev/guide/migration/)
- [React Testing Library installation and principles](https://testing-library.com/docs/react-testing-library/intro/)
- [Playwright test configuration, viewport and web server](https://playwright.dev/docs/test-configuration)
- [Playwright system requirements](https://playwright.dev/docs/intro)

## Unresolved backend decisions (explicit dependencies, not research guesses)

The approved v1.0.0 contract defines the station departure response, `Asia/Colombo` date parameter, backend ordering, field/null meanings and API errors. It does not promise upcoming-only filtering or define the prototype polling policy; the latter is a project-owner-approved frontend policy recorded above. Remaining decisions are final production refresh policy, backend capacity/operational guarantees, cache expiry/freshness thresholds, client revision comparison across snapshots, and any timestamp display meaning not verified for the UI. No new backend contract is generated here.
