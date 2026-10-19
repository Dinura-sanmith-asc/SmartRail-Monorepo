# Quickstart: Station Display Validation

This guide describes validation scenarios for implementation. It does not claim the feature or future live endpoints are currently implemented.

## Prerequisites

- Node.js 24.x (the inspected local environment is 24.18.0; the installed Vite 8 requires Node 20.19+ or 22.12+).
- npm and repository dependencies installed with the committed lockfile.
- During fixture-only UI work, use an explicitly labeled contract fixture. Do not identify fixture output as live railway data.
- For live acceptance, an environment with the approved Journeys & Status contract and a published test station/departure dataset is required.

## Current repository checks

The existing scripts are:

```powershell
npm run lint
npm run build
```

These validate linting and TypeScript/production compilation. They do not prove acceptance behavior or live integration.

## Planned feature test commands

Implementation tasks must add and verify test scripts before relying on them:

```powershell
npm run test:unit
npm run test:e2e
```

Use Vitest/React Testing Library for state and component behavior and Playwright for browser scenarios. Playwright should run the Vite app with its test web server; viewport contexts must use the two approved landscape profiles.

## Validation scenarios

1. **Configured station identity**: Set the deployment station ID to a published station in the fixture; confirm the board title matches its published name. Repeat with missing, malformed and unpublished IDs; expect configuration/unavailable state and no fallback station.
2. **Station isolation and schedule meaning**: Feed departures for two station IDs through a labeled fixture; confirm only the configured station's dated departures are shown. Confirm a reusable service search result alone never appears as a confirmed station departure.
3. **Operational changes**: Provide source-authoritative platform, delay and cancellation fixture values for a dated departure. Confirm the correct row changes, scheduled values remain clearly planned, text/symbol communicates status and update time is shown only if supplied with verified semantics. Leave some operational values absent and confirm none are inferred.
4. **Automatic update**: In a controlled adapter fixture, make a newer authoritative response available while the board is connected. Confirm the board updates without user action. Do not assert a fabricated interval or latency.
5. **Connectivity and recovery**: After one successful response, interrupt the source; confirm the prior data is visibly cached/possibly outdated and last update appears only when reliable. Restore connectivity; confirm stale indication remains until a successful authoritative revalidation. Start offline with no prior response and confirm unavailable state.
6. **Approved display profiles**: Run the primary board scenario at 1920 × 1080 and 1366 × 768. Confirm station identity, scheduled time, service identity and operational status remain readable and important disruption information is not hidden at either profile.
7. **Accessibility and passive use**: Inspect semantic headings/status announcements and WCAG 2.2 AA criteria. Confirm status is understandable without color. Confirm no station selector, station-management, controller or kiosk actions are exposed.
8. **Live contract acceptance (blocked)**: Repeat the station/departure, operational update, automatic refresh and recovery scenarios against the backend only after its contract, public projection, revision/time semantics and error behavior are approved. Record source contract version and verification evidence; fixtures alone cannot pass this gate.

## Expected result

Fixture scenarios may validate the independent UI and state presentation. The feature is not live-integration complete until the future endpoint contracts are verified and the display passes the source acceptance checks for SR-054, SR-056, SR-058 and SR-059, with the station prototype evidence for SR-061 and applicable cross-cutting requirement evidence.

## Browser verification evidence — T038–T044

Executed in Chromium on 2026-10-19 using the fixture-only Playwright project. Station identity was still checked through the published station lookup; only `GET /api/stations` was intercepted with a deterministic station-list response. A development-only fixture control supplied the deterministic platform-change example. No live backend was used.

### Approved landscape profiles

`npm run test:e2e` passed all 12 fixture browser tests. The profile scenarios passed at 1920 × 1080 and 1366 × 768. Both verified the station name, scheduled times, service identities, platform-change text, delay, cancellation, and persistent `DEMO DATA — NOT LIVE` label. Assertions found no horizontal overflow and confirmed required information remained in the viewport. Each profile also captured a full-page screenshot:

- [1920 × 1080 populated board](./evidence/station-board-1920x1080.png)
- [1366 × 768 populated board](./evidence/station-board-1366x768.png)

The screenshots were visually reviewed. At both profiles the hierarchy and all required values were visible; the 1366 × 768 view is denser, but the tested content did not clip or leave the viewport. The displayed delay, platform change, and cancellation are synthetic fixture examples, not operational railway information.

### Accessibility checks and limitations

Real-browser axe checks ran the `wcag2a`, `wcag2aa`, `wcag21a`, `wcag21aa`, and `wcag22aa` rule tags against populated views at both profiles, plus stale and unavailable views. All four axe analyses reported zero violations. Browser assertions also verified heading hierarchy, text labels for operational values and cancellation, explicit unknown/unavailable text, and a readable stale warning. Status meaning is present in text as well as visual styling.

The screenshots received a manual visual review for hierarchy, visibility, wrapping, and apparent contrast. No manual contrast ratio measurements, keyboard-only evaluation, screen-reader evaluation, assistive-technology compatibility review, or physical display/viewing-distance review was performed. Axe passing and this visual inspection do not establish full WCAG 2.2 AA conformance; those manual checks remain necessary before an accessibility-conformance claim.

### Passive display checks

Three Playwright scenarios passed for populated, unavailable, and stale states. They confirmed the public display has no passenger station selector, buttons, links, input controls, station-management, controller, editing, publishing, authentication, account-management, kiosk, or terminal actions.

### Quality gate results for this batch

- `npm run test:e2e`: passed, 12 tests; zero skipped.
- `npm run test:unit`: passed, 12 files and 47 tests.
- `npm run lint`: passed.
- `npm run build`: passed.
- `git diff --check`: passed.

The production build was inspected and contains no development browser-control identifiers. These results verify the fixture-driven frontend and do not constitute live backend acceptance. Live integration remains contract-gated.

## Consolidated requirement verification — T045

Requirement wording and priorities remain governed by the approved inventory and are reproduced in [spec.md](./spec.md#approved-requirement-traceability). This matrix records Station Display evidence separately from backend and cross-application acceptance. “Fixture-verified” means the frontend behavior was exercised with deterministic synthetic data; it is not evidence of authoritative railway data or live acceptance.

| Requirement | Story / acceptance scenarios | Frontend capability and tasks | Automated evidence | Browser / screenshot evidence | Fixture status | Live / overall status and remaining dependency |
|---|---|---|---|---|---|---|
| SR-054 | US1 scenarios 1–4; US2 scenarios 1, 5 | Configured-station board, source ordering, scheduled labels, empty state; T013, T016–T018, T021–T023, T038 | `station-header.test.tsx`, `departure-board.test.tsx`, `App.test.tsx`, `departure-row.test.tsx` | Profile tests at 1920 × 1080 and 1366 × 768; screenshots below | Implemented and fixture-verified | Backend-dependent / blocked: approved `GET /api/stations/{id}/departures` contract and live acceptance T024–T026 are pending. Fixtures do not satisfy SR-054 acceptance. |
| SR-056 | US2 scenarios 2–5 | Textual platform, delay, cancellation and unknown-value presentation; T019–T021, T038–T039 | `operational-change.test.tsx`, `departure-row.test.tsx` | Both profile screenshots and populated-state axe/browser checks | Implemented and fixture-verified | Backend-dependent / blocked: authoritative operational projection, field meaning and update-time semantics require approved contract and T024–T026. |
| SR-058 | US3 scenarios 1–3 | Source interface and automatic snapshot presentation without passenger action; T007, T027–T030 | `use-departure-updates.test.ts` | `station-connectivity.spec.ts` verifies fixture revalidation; profile suite verifies rendered fixture changes | Implemented and fixture-verified | Backend-dependent / blocked: approved delivery mechanism, revision/timestamp semantics and live propagation T029–T030 are pending. No refresh interval is claimed. |
| SR-059 | US4 scenarios 1–4 | Unavailable-before-first-load, stale last-success retention, and revalidation-gated recovery; T032–T037, T034 | `display-state.test.ts`, `use-revalidation.test.ts` | Four deterministic connectivity browser scenarios; axe includes stale and unavailable states | Implemented and fixture-verified | Live backend acceptance remains blocked: authoritative cache/freshness semantics and verified environment are not available; T047 pending. |
| SR-061 | US1 scenario 1; US5 scenarios 1–3; US6 scenarios 1–3 | Functional station-specific passive display; T013, T016, T038–T044 | `App.test.tsx`, component tests; passive-display suite | Both profile screenshots; populated, unavailable and stale passive checks | Implemented and fixture-verified for the Station Display prototype | Prototype evidence is present. This does not establish live data acceptance or physical deployment suitability. |
| SR-001 | Station Display-specific evidence: US2 scenarios 2–5 and US3 scenario 1 | UI preserves source-provided status meaning and can render source updates; T019–T021, T027–T028 | `operational-change.test.tsx`, `use-departure-updates.test.ts` | Fixture profile and update scenarios only | Partially verified for this application | External cross-application verification pending. Passenger, operations, driver/operator and station channels must retrieve the same journey update/revision. The SmartRail cross-application integration/release verification workstream owns evidence; assignment remains with the release owner. Full SR-001 acceptance is pending shared end-to-end verification. |
| SR-002 | US1 scenarios 1 and 4 | Validates configured stable station ID against published station records; T011–T016 | `config.test.ts`, `station-reference-client.test.ts`, `station-header.test.tsx` | Browser tests preserve the normal published-station lookup path with a deterministic `/api/stations` response | Partially verified: frontend mapping and fixture response tested | Full requirement acceptance remains external/backend-dependent because station record creation and shared channel exposure are outside this frontend. The existing OpenAPI contract covers public reads only. |
| SR-003 | US1 scenarios 2–3; US2 scenario 1 | Board preserves returned departure ordering and does not substitute service search; T018, T022–T023 | `departure-board.test.tsx`, `App.test.tsx` | Fixture profile checks source-provided order/content | Partially verified for the display boundary | External/backend verification pending for authoritative ordered route stops and stable station IDs. This board does not call route APIs or establish platform-wide route acceptance. |
| SR-004 | US2 scenario 1 | Scheduled service identity/time remain labeled as planned values; no service-search fallback; T017–T023 | `departure-row.test.tsx`, `departure-board.test.tsx`, `App.test.tsx` | Both profile scenarios show scheduled labels and service identity in fixtures | Partially verified for presentation semantics | External/backend verification pending that each published service references a valid route, schedule and service identity. Planned-service endpoints are not a confirmed-departure source. |
| SR-010 | US2 scenarios 2–5 | Renders supplied changes/update times and preserves unknown values without deriving status; T019–T021 | `operational-change.test.tsx` | Fixture profile and accessibility checks show textual operational states | Partially verified with synthetic values | Backend-dependent / blocked: source authorization, shared revision and authoritative update-time meaning require approved contract and live verification; no fixture proves source authority or cross-channel consistency. |
| SR-035 | US2 scenario 5; US4 scenarios 2 and 4 | Explicit unknown/unavailable values; no inferred ETA/platform or stale-to-current conversion; T017–T019, T032–T037 | `departure-row.test.tsx`, `operational-change.test.tsx`, `display-state.test.ts` | Browser assertions cover explicit unknown/unavailable and stale labels | Implemented and fixture-verified for missing/unavailable values | Live feed-unavailable acceptance remains unverified; this application has no live position/ETA feed, and the fixture cannot prove production source behavior. |

### Accessibility, viewport and passive evidence — T045

- Approved profile evidence is captured at [1920 × 1080](./evidence/station-board-1920x1080.png) and [1366 × 768](./evidence/station-board-1366x768.png). These show the fixture departure board with scheduled times, service identity, platform change, delay, cancellation and the persistent `DEMO DATA — NOT LIVE` label.
- `station-connectivity.spec.ts` covers offline before initial success, offline after success, failed revalidation, and successful revalidation. The stale and unavailable states were included in axe checks. These are deterministic fixture states, not central-service outage acceptance.
- `station-passive-display.spec.ts` covers populated, unavailable and stale views and asserts no passenger controls or privileged/management/terminal actions.
- Axe scans covered populated boards at both profiles plus stale and unavailable states, using `wcag2a`, `wcag2aa`, `wcag21a`, `wcag21aa` and `wcag22aa` tags. All four scans reported zero violations.
- Outstanding manual reviews: keyboard-only, screen reader, contrast-ratio measurements, assistive-technology compatibility, and physical display-distance review. Automated scans and screenshot review do not establish complete WCAG 2.2 AA conformance.

## Consolidated frontend quality-gate results — T046

Executed on 2026-10-19 in the repository environment:

| Command | Result | Evidence / scope |
|---|---|---|
| `npm run lint` | Passed (exit 0) | ESLint completed without reported diagnostics. |
| `npm run build` | Passed (exit 0) | `tsc -b` and Vite 8.3.4 production build succeeded; 31 modules transformed. |
| `npm run test:unit` | Passed: 12 files, 47 tests | Vitest 5.0.5; component, API/configuration, model and hook tests. |
| `npm run test:e2e` | Passed: 12, failed: 0, skipped: 0 | Playwright Chromium `chromium-fixture` project; accessibility (3), connectivity/revalidation (4), display profiles (2), passive display (3). |
| `git diff --check` | Passed | No whitespace errors. |

The browser run covered both approved landscape profiles, stale/recovery scenarios, fixture labeling, and passive behavior. The four axe analyses reported zero violations. `npm run test:e2e` invokes only `--project=chromium-fixture`; `playwright.config.ts` separates the opt-in `chromium-live` project and the live contract spec. `npm run test:e2e:live` was not run because backend contracts are unapproved.

The production build output under `dist/` was searched for the development fixture-control module and control identifiers; none were present. Application/API source review found no new live departure endpoint or contract. The existing `/api/stations` lookup is the only station reference API used by this flow; departure/status demonstrations remain explicitly synthetic. Passive-display browser assertions found no privileged or interactive kiosk UI. No unrelated application feature was implemented in this verification batch.

### Acceptance boundaries and remaining evidence

- **Frontend:** The listed UI and state behavior is implemented and fixture-verified within the automated coverage above.
- **Live integration:** SR-054, SR-056 and SR-058 live acceptance, plus authoritative SR-059 recovery/freshness, remain unverified and backend-blocked. T024–T026, T029–T031 and T047 remain unchecked/blocked or conditional.
- **Cross-application:** SR-001 is not fully verified until shared journey update/revision evidence is obtained across passenger, operations, driver/operator and station channels. The release owner must assign the SmartRail cross-application integration/release verification workstream.
- **Accessibility:** Keyboard, assistive-technology, physical viewing and measured contrast reviews remain outstanding; do not claim WCAG 2.2 AA conformance from axe results alone.
- **Final acceptance evidence needed:** approved/versioned Journeys & Status contract, verified backend environment and test dataset, live automatic-update and outage/revalidation results, shared-channel SR-001 evidence, and the manual accessibility reviews listed above.
