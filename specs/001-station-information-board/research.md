# Research: Station Information & Departure Board

**Date**: 2026-10-18  
**Scope**: Resolve planning decisions using the approved station display spec, local project, approved source documents, and current official tooling documentation. This research does not define the future Journeys & Status contract.

## Decision 1 — Preserve the existing frontend stack and organize by feature

**Decision**: Keep React + TypeScript + Vite + Tailwind CSS. Add a small application configuration/bootstrap boundary, a `station-display` feature boundary, and shared UI/API primitives only as needed. Do not add a router or global state framework for a single passive screen.

**Rationale**: The project already uses React 19.3.1, TypeScript 6.0.3, Vite 8.3.4, Tailwind 4.3.3 and the React/Tailwind Vite plugins. Current source is a starter shell (`src/App.tsx`, `src/App.css`, `src/index.css`, `src/main.tsx`), with no established feature structure or test suite. A feature boundary matches the constitution while keeping the small application simple.

**Alternatives considered**: Replace the toolchain or add a router/state framework. Rejected because the approved stack is already installed and the feature is one public display view.

## Decision 2 — Keep station reference and live departure ownership separate

**Decision**: Validate the deployment station ID against the approved public station list. Treat the existing route/service endpoints as reference/planned-schedule sources only. Use an isolated feature adapter boundary for future dated station departures and operational updates, but do not define a response schema or a proposed OpenAPI contract before backend approval.

**Rationale**: The checked-in Railway Network OpenAPI defines `GET /api/stations`, `GET /api/routes/{id}/stops`, `GET /api/services` and `GET /api/services/{id}`. It explicitly states that service search is planned schedule information and not a dated operational journey or live status. The handoff and RBAC/API inventory list `/api/stations/{id}/departures`, `/api/journeys/{id}` and `/api/journeys/{id}/updates` as future endpoints, with contract behavior still to be specified.

**Alternatives considered**: Filter reusable service search results to synthesize departures or assume future endpoint fields. Rejected because these would misrepresent scheduled services and invent backend behavior.

## Decision 3 — Make refresh orchestration replaceable and freshness conservative

**Decision**: Keep refresh/delivery isolated behind an adapter/state boundary. While connected, the display must obtain authoritative changes automatically. During loss, retain the last successful data in active runtime memory, mark it stale/cached, and show last-update only where its meaning is verified. On recovery, keep the warning until authoritative revalidation succeeds. If there is no prior successful response, show unavailable.

**Rationale**: This meets approved Q3 behavior while leaving polling versus push, intervals, version/order semantics, timestamp meaning and cache expiry for the Journeys & Status contract and owner. The constitution expressly prohibits inventing those thresholds.

**Alternatives considered**: Choose a polling interval or durable storage/TTL now. Rejected because neither cadence nor cache validity is approved. Durable offline persistence is deferred; it would change exposure and stale-data risk.

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

The backend owner must approve the exact departure schema; “upcoming” and date/time/timezone ordering semantics; change notification or polling mechanism and interval; revision/order rules; source/update timestamp meanings; cache expiry/freshness thresholds; and API error/unavailable-value behavior. These are not resolved by current OpenAPI or frontend-library guidance. No future contract is generated here.
