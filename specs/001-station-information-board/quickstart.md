# Quickstart: Station Display Validation

This guide records fixture verification and the live contract integration status. Contract implementation does not by itself establish live environment acceptance.

## Prerequisites

- Node.js 24.x (the inspected local environment is 24.18.0; the installed Vite 8 requires Node 20.19+ or 22.12+).
- npm and repository dependencies installed with the committed lockfile.
- During fixture-only UI work, use an explicitly labeled contract fixture. Do not identify fixture output as live railway data.
- For live acceptance, set `STATION_DISPLAY_LIVE_STATION_ID` and `STATION_DISPLAY_LIVE_API_BASE_URL` to a verified backend environment with a published test station/departure dataset.

## Current repository checks

The existing scripts are:

```powershell
npm run lint
npm run build
```

These validate linting and TypeScript/production compilation. They do not prove acceptance behavior or live integration.

## Test commands

```powershell
npm run test:unit
npm run test:e2e
npm run test:e2e:live
```

`npm run test:e2e` runs only fixtures. `npm run test:e2e:live` refuses to start unless the two live environment variables above are set, then selects the live API source. Use Vitest/React Testing Library for state and component behavior and Playwright for browser scenarios.

## Validation scenarios

1. **Configured station identity**: Set the deployment station ID to a published station in the fixture; confirm the board title matches its published name. Repeat with missing, malformed and unpublished IDs; expect configuration/unavailable state and no fallback station.
2. **Station isolation and schedule meaning**: Feed departures for two station IDs through a labeled fixture; confirm only the configured station's dated departures are shown. Confirm a reusable service search result alone never appears as a confirmed station departure.
3. **Operational changes**: Provide source-authoritative platform, delay and cancellation fixture values for a dated departure. Confirm the correct row changes, scheduled values remain clearly planned, text/symbol communicates status and update time is shown only if supplied with verified semantics. Leave some operational values absent and confirm none are inferred.
4. **Prototype polling**: After station validation, verify an initial snapshot request and configurable 30000 ms default polling using fake timers or a controlled clock. Verify each request recalculates the current `Asia/Colombo` date across midnight, requests never overlap, invalid interval configuration does not start polling, reconnect can trigger immediate revalidation, and updated source data appears without passenger action. Do not use real 30-second sleeps or assert production latency.
5. **Connectivity and recovery**: After one successful response, interrupt the source; confirm the prior data is visibly cached/possibly outdated and last update appears only when reliable. Restore connectivity; confirm stale indication remains until a successful authoritative revalidation. Start offline with no prior response and confirm unavailable state.
6. **Approved display profiles**: Run the primary board scenario at 1920 × 1080 and 1366 × 768. Confirm station identity, scheduled time, service identity and operational status remain readable and important disruption information is not hidden at either profile.
7. **Accessibility and passive use**: Inspect semantic headings/status announcements and WCAG 2.2 AA criteria. Confirm status is understandable without color. Confirm no station selector, station-management, controller or kiosk actions are exposed.
8. **Live contract acceptance (T026 complete)**: Journeys & Status OpenAPI v1.0.0 is approved and the T025 adapter maps its station departure snapshots. After the existing additive Journeys & Status migration was applied to local Development, one explicitly identified acceptance journey was initialized through the backend `JourneyInitializationRepository` workflow, using the fixture service/schedule already present in the database. On 2026-10-19, `npm run test:e2e:live` passed 2/2 in Chromium against the Aspire API: the configured Colombo Fort board rendered the real dated projection, and malformed-date/unpublished-station requests returned the documented `application/problem+json` responses. The prototype polling policy is now approved for T029; live polling verification T030 and central-service outage/revalidation acceptance still need to pass in a verified environment.

## Expected result

Fixture scenarios validate independent UI and state presentation. T026 verifies the live departure snapshot and API error contract. The project owner has approved the 30000 ms default prototype HTTP polling policy recorded in the contract register; this does not define a production freshness SLA or backend delivery guarantee. T029 may now implement the poller. Live SR-058 verification remains pending T030, and SR-059/T047 remain incomplete until live outage/revalidation evidence passes.

## Approved prototype refresh policy

Use configurable HTTP polling of the verified Journeys & Status v1.0.0 `GET /api/stations/{id}/departures?date=YYYY-MM-DD` snapshot endpoint. Fetch after station validation, then poll while connected at `VITE_STATION_DEPARTURE_REFRESH_INTERVAL_MS`, default 30000 ms. The variable is public and normally embedded at Vite build time; do not put secrets in it. Missing configuration uses the default; invalid configuration must not enable polling or produce a zero-delay loop. Implement tests with fake timers or a controlled clock, not real 30-second sleeps.

Recalculate the date in `Asia/Colombo` for each request so the query date changes across midnight. Do not overlap or queue in-flight requests; skip timer ticks while one request is pending. A reconnect event may request immediate revalidation. On refresh failure, keep previously loaded data visibly cached/possibly outdated until a successful backend response. Preserve backend order, exact IDs and source values; do not derive delay, status or ETA. The OpenAPI snapshot is date-scoped and does not define an “upcoming-only” cutoff. This prototype interval is not a production freshness SLA, backend latency guarantee, push contract or cache-expiry threshold.

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
- `npm run test:unit -- --pool=threads --maxWorkers=1`: passed, 13 files and 50 tests.
- `npm run lint`: passed.
- `npm run build`: passed.
- `git diff --check`: passed.

The production build was inspected and contains no development browser-control identifiers. These results verify the fixture-driven frontend and do not constitute live backend acceptance. An earlier default-pool Vitest invocation stalled without output in this Windows environment; the single-worker thread-pool invocation above completed successfully.

## Consolidated requirement verification — T045

Requirement wording and priorities remain governed by the approved inventory and are reproduced in [spec.md](./spec.md#approved-requirement-traceability). This matrix records Station Display evidence separately from backend and cross-application acceptance. “Fixture-verified” means the frontend behavior was exercised with deterministic synthetic data; it is not evidence of authoritative railway data or live acceptance.

| Requirement | Story / acceptance scenarios | Frontend capability and tasks | Automated evidence | Browser / screenshot evidence | Fixture status | Live / overall status and remaining dependency |
|---|---|---|---|---|---|---|
| SR-054 | US1 scenarios 1–4; US2 scenarios 1, 5 | Configured-station board, source ordering, scheduled labels, empty state; T013, T016–T018, T021–T026, T038 | `station-header.test.tsx`, `departure-board.test.tsx`, `App.test.tsx`, `departure-row.test.tsx`, `station-departure-adapter.test.ts` | Fixture profile tests at both approved sizes; live contract spec passed 2/2 against local Development API | Live snapshot and error-contract verified for the local acceptance dataset | Journeys & Status v1.0.0; T026 passed 2026-10-19. Final T047 acceptance remains pending. |
| SR-056 | US2 scenarios 2–5 | Textual platform, status, operational time and unknown handling; delay is not derived; T019–T021, T025–T026, T038–T039 | `operational-change.test.tsx`, `departure-row.test.tsx`, `station-departure-adapter.test.ts` | Both fixture profile screenshots; live contract mapping exercised in the T026 run | Implemented and fixture/adapter-verified | Backend schema has no delay or service code; contract meanings are preserved. Full authoritative update-display acceptance remains pending T047. |
| SR-058 | US3 scenarios 1–5 | Source interface and automatic snapshot presentation without passenger action; T007, T027–T030 | `use-departure-updates.test.ts`, `station-departure-adapter.test.ts`; T029 should add fake-clock coverage for default/configured intervals, midnight date rollover, overlap prevention and failure state | Fixture revalidation browser checks; live polling verification T030 pending | Fixture behavior verified; prototype HTTP polling policy approved but not yet implemented/verified | T029 may implement the approved client poller. T030 must verify live snapshot updates; v1.0.0 itself specifies no delivery cadence or latency guarantee. |
| SR-059 | US4 scenarios 1–4 | Unavailable-before-first-load, stale last-success retention, and revalidation-gated recovery; T032–T037, T034 | `display-state.test.ts`, `use-revalidation.test.ts` | Four deterministic connectivity browser scenarios; axe includes stale and unavailable states | Implemented and fixture-verified | Live backend acceptance remains blocked: authoritative cache/freshness semantics and verified environment are not available; T047 pending. |
| SR-061 | US1 scenario 1; US5 scenarios 1–3; US6 scenarios 1–3 | Functional station-specific passive display; T013, T016, T038–T044 | `App.test.tsx`, component tests; passive-display suite | Both profile screenshots; populated, unavailable and stale passive checks | Implemented and fixture-verified for the Station Display prototype | Prototype evidence is present. This does not establish live data acceptance or physical deployment suitability. |
| SR-001 | Station Display-specific evidence: US2 scenarios 2–5 and US3 scenario 1 | UI preserves source-provided status meaning and can render source updates; T019–T021, T027–T028 | `operational-change.test.tsx`, `use-departure-updates.test.ts` | Fixture profile and update scenarios only | Partially verified for this application | External cross-application verification pending. Passenger, operations, driver/operator and station channels must retrieve the same journey update/revision. The SmartRail cross-application integration/release verification workstream owns evidence; assignment remains with the release owner. Full SR-001 acceptance is pending shared end-to-end verification. |
| SR-002 | US1 scenarios 1 and 4 | Validates configured stable station ID against published station records; T011–T016 | `config.test.ts`, `station-reference-client.test.ts`, `station-header.test.tsx` | Browser tests preserve the normal published-station lookup path with a deterministic `/api/stations` response | Partially verified: frontend mapping and fixture response tested | Full requirement acceptance remains external/backend-dependent because station record creation and shared channel exposure are outside this frontend. The existing OpenAPI contract covers public reads only. |
| SR-003 | US1 scenarios 2–3; US2 scenario 1 | Board preserves returned departure ordering and does not substitute service search; T018, T022–T023 | `departure-board.test.tsx`, `App.test.tsx` | Fixture profile checks source-provided order/content | Partially verified for the display boundary | External/backend verification pending for authoritative ordered route stops and stable station IDs. This board does not call route APIs or establish platform-wide route acceptance. |
| SR-004 | US2 scenario 1 | Scheduled service identity/time remain labeled as planned values; no service-search fallback; T017–T023 | `departure-row.test.tsx`, `departure-board.test.tsx`, `App.test.tsx` | Both profile scenarios show scheduled labels and service identity in fixtures | Partially verified for presentation semantics | External/backend verification pending that each published service references a valid route, schedule and service identity. Planned-service endpoints are not a confirmed-departure source. |
| SR-010 | US2 scenarios 2–5 | Renders supplied changes and preserves unknowns without deriving status or delay; T019–T021, T025–T026 | `operational-change.test.tsx`, `station-departure-adapter.test.ts` | Fixture profile and accessibility checks; live browser contract spec added | Contract-mapped, live unverified | Source, revision and timestamp values are retained/handled per v1.0.0; `updatedAt` is not labeled until its UI meaning is verified. Cross-channel consistency remains external. |
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
| `npm run build` | Passed (exit 0) | `tsc -b` and Vite 8.3.4 production build succeeded; 32 modules transformed. |
| `npm run test:unit -- --pool=threads --maxWorkers=1` | Passed: 13 files, 50 tests | Vitest 5.0.5; includes three Journeys & Status adapter contract-mapping cases. |
| `npm run test:e2e` | Passed: 12, failed: 0, skipped: 0 | Playwright Chromium `chromium-fixture` project; accessibility (3), connectivity/revalidation (4), display profiles (2), passive display (3). |
| `git diff --check` | Passed | No whitespace errors. |

The browser run covered both approved landscape profiles, stale/recovery scenarios, fixture labeling, and passive behavior. The four axe analyses reported zero violations. `npm run test:e2e` invokes only `--project=chromium-fixture`; `playwright.config.ts` separates the opt-in `chromium-live` project and the live contract spec. The initial live run failed on HTTP 503 because the development database had not yet received the Journeys & Status migration. After migration and seeding the identified acceptance journey through the backend initialization repository, the successful 2/2 live run is recorded above.

The production build output under `dist/` was searched for the development fixture-control module and control identifiers; none were present. Production composition selects the live station departure adapter; development defaults to the explicitly labeled controlled fixture. The live adapter is contract-tested but has not yet been accepted against a live backend. Passive-display browser assertions found no privileged or interactive kiosk UI.

### Acceptance boundaries and remaining evidence

- **Frontend:** The listed UI and state behavior is implemented and fixture-verified within the automated coverage above.
- **Live integration:** T024–T026 are complete against Journeys & Status v1.0.0. The local Development migration applied and T026 passed 2/2 live Playwright checks on 2026-10-19 using the backend's initialization repository to add one identified acceptance journey. The project-owner prototype polling decision allows T029 implementation; T030 live polling verification remains pending and makes no latency-SLA claim. T031 remains conditional. T047 is the final live acceptance gate and still needs live update, outage, stale-state, and reconnection evidence.
- **Cross-application:** SR-001 is not fully verified until shared journey update/revision evidence is obtained across passenger, operations, driver/operator and station channels. The release owner must assign the SmartRail cross-application integration/release verification workstream.
- **Accessibility:** Keyboard, assistive-technology, physical viewing and measured contrast reviews remain outstanding; do not claim WCAG 2.2 AA conformance from axe results alone.
- **Final acceptance evidence needed:** live station departure and operational projection results, approved automatic-update behavior, authoritative outage/revalidation evidence, shared-channel SR-001 evidence, and the manual accessibility reviews listed above.
