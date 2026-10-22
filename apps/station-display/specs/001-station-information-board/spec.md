# Feature Specification: SmartRail Station Information & Departure Board

**Feature Branch**: `001-station-information-board`

**Created**: 2026-10-18

**Status**: Draft — product clarifications resolved; backend contract dependencies remain

**Input**: User description: "$speckit-specify — SmartRail Station Information & Departure Board"

## Clarifications

### Session 2026-10-18

- Q: How is a deployed Station Display associated with its station? → A: Each display uses a deployment-provided stable station identifier validated against public, published Railway Network station information; there is no passenger-facing selector or station-management UI.
- Q: Which display profiles are approved prototype targets? → A: Landscape 1920 × 1080 and 1366 × 768; no viewing-distance, hardware-certification or additional-resolution claim is approved.
- Q: What automatic refresh and freshness behavior is approved? → A: While connected, the display automatically obtains updated authoritative railway information without passenger interaction. During connectivity failure, cached information is identified as cached or possibly outdated. After reconnection, the display revalidates against authoritative information before removing the stale indication. Numeric intervals and backend-dependent contract semantics remain unresolved.

### Session 2026-10-19

- Q: What refresh policy should the prototype use with the now-verified Journeys & Status station-departures snapshot? → A: Use configurable HTTP polling, defaulting to 30000 ms, through the existing approved GET endpoint. Fetch immediately after station validation; while connected, poll without overlapping requests and recalculate the `Asia/Colombo` station-local service date for every request, including across midnight. A reconnect event may request immediate revalidation. After a failed refresh, retain the last successful data only as cached/possibly outdated until successful revalidation. The interval is a prototype client policy, not a production freshness SLA, backend latency guarantee, push contract, or cache-expiry threshold. Preserve backend ordering, exact identity and source values; do not derive delay, status or ETA. Invalid interval configuration must not start polling or cause uncontrolled requests.

## User Scenarios & Testing

### User Story 1 - Show departures for the configured station (Priority: P1)

A passenger looking at a station display needs to identify upcoming trains serving that station and understand their published scheduled times. The display is associated with one station at a time.

**Why this priority**: A useful station board must first show the correct station's departures.

**Independent Test**: Provide station identity and published departure data, then verify the board identifies the station and shows only departures for it in the returned order.

**Acceptance Scenarios**:

1. **Given** a deployment-provided stable station identifier matches a public, published station and authoritative departure data is available, **When** the display opens, **Then** it identifies that station and shows its appropriate upcoming departures with their published scheduled times.
2. **Given** departure data contains services for other stations, **When** the board presents the selected station's departures, **Then** it does not show departures belonging only to another station.
3. **Given** no appropriate upcoming departures are returned, **When** the board presents the result, **Then** it shows a clear empty state and does not imply that scheduled service search results are confirmed station departures.
4. **Given** the deployment-provided station identifier is missing, invalid, unavailable or unpublished, **When** the display starts, **Then** it does not present another station's information and exposes a clear configuration or unavailable state without offering a passenger selector or station-management UI.

### User Story 2 - Understand published operational changes (Priority: P1)

A passenger needs to see available, authorized operational information—such as a platform change, delay or cancellation—alongside the planned schedule, with the meaning and freshness of each value clear.

**Why this priority**: Passengers must be able to distinguish a plan from an authoritative operational update when deciding where and when to board.

**Independent Test**: Present a departure with scheduled values and an authorized operational update, then verify the board preserves the scheduled meaning and clearly identifies the update and its updated time when provided.

**Acceptance Scenarios**:

1. **Given** published scheduled time is available but no current operational status is available, **When** the departure is shown, **Then** the time is identified as scheduled and the board does not imply live confirmation.
2. **Given** an authorized platform change is available, **When** the affected departure is shown, **Then** the new platform is clearly identified as an operational update and its update time is shown when supplied by the authoritative data.
3. **Given** an authorized delay is available, **When** the affected departure is shown, **Then** the delay/status is identified using its published meaning and is not calculated or inferred by the display.
4. **Given** a journey is authoritatively cancelled, **When** its departure is shown, **Then** cancellation is prominently identified with a text label or symbol as well as any color treatment.
5. **Given** a platform, delay, ETA or other operational value is absent or unavailable, **When** the board presents the departure, **Then** it shows unknown/unavailable as appropriate and does not invent a value.

### User Story 3 - See changes without manually reloading (Priority: P1)

A passenger needs the connected board to reflect newly available authoritative departure information without operating the display.

**Why this priority**: A passive station screen must keep pace with published changes to remain useful.

**Independent Test**: Change the authoritative departure revision while the display is connected and verify the visible board updates without a passenger action or manual page reload.

**Acceptance Scenarios**:

1. **Given** the display is connected and a new authorized departure update becomes available, **When** the update is made available to the display, **Then** the board reflects the updated departure and its provided updated time without manual reload.
2. **Given** the update service is unavailable, **When** the display cannot obtain a newer revision, **Then** it does not claim that the visible information is current and communicates the applicable unavailable or stale state.
3. **Given** the display is connected and the backend makes a newer authoritative revision available, **When** the display's automatic update process obtains it, **Then** the board presents the updated information without passenger interaction; no numeric interval or timing guarantee is implied.
4. **Given** the configured station has been validated, **When** the display starts or its prototype polling interval elapses while connected, **Then** it requests the station-departure snapshot using the current `Asia/Colombo` service date; the default configured interval is 30000 ms, and the date is recalculated for each request.
5. **Given** a refresh is already in flight or a refresh fails, **When** a scheduled poll or reconnect event occurs, **Then** the display does not overlap requests, does not present prior data as guaranteed current, and retains the cached/possibly outdated warning until a successful response is received.

### User Story 4 - Recognize disconnected or stale information (Priority: P1)

A passenger needs to know when a display has lost central connectivity and its visible information may no longer be current.

**Why this priority**: Clear freshness information prevents cached data from being mistaken for confirmed live status.

**Independent Test**: Load departure information, simulate loss and restoration of connectivity, and verify stale/unavailable indication during disconnection and authoritative refresh before presenting the display as current again.

**Acceptance Scenarios**:

1. **Given** previously obtained information remains visible after central connectivity is lost, **When** the loss is detected, **Then** the board identifies that information as cached or possibly outdated and shows its last update time when available.
2. **Given** connectivity is lost before any departure information has been obtained, **When** the board cannot load data, **Then** it shows a service-unavailable state and no fabricated departure information.
3. **Given** connectivity is restored, **When** the display obtains the latest authoritative information, **Then** it updates the board and removes the stale warning only after that information is available.
4. **Given** a value was unknown or unavailable before disconnection, **When** cached information is displayed, **Then** the display does not convert that value into a confirmed value.

### User Story 5 - Read the board from its approved display environment (Priority: P2)

A passenger viewing the board from the intended station display environment needs legible, consistently organized information and status cues that do not depend on color alone.

**Why this priority**: Passengers must be able to scan departures and understand status from the display locations approved for deployment.

**Independent Test**: Review the board at each approved landscape profile—1920 × 1080 and 1366 × 768—and assess required information hierarchy, legibility, contrast and status interpretation.

**Acceptance Scenarios**:

1. **Given** either approved landscape display profile (1920 × 1080 or 1366 × 768), **When** the board presents a populated departure list, **Then** station identity, scheduled time, service identity and available operational status are distinguishable and readable at that profile.
2. **Given** a status is indicated with color, **When** a passenger cannot distinguish that color, **Then** the same status remains understandable from text or a symbol.
3. **Given** disruption information is presented, **When** a passenger scans the display, **Then** the disruption is visually distinguishable from planned schedule information and is not conveyed by color alone.

### User Story 6 - Use a passive passenger-information display (Priority: P2)

A passenger needs a public information screen that presents railway information without exposing railway management actions.

**Why this priority**: The Station Display communicates public information; railway operations remain the responsibility of authorized backend workflows and distinct applications.

**Independent Test**: Inspect all passenger-facing states and available interactions and verify that the display offers no station-management, controller, privileged write or interactive terminal workflow.

**Acceptance Scenarios**:

1. **Given** the display is available to passengers, **When** they view or interact with its public information surface, **Then** they can read station and departure information without being offered management or controller actions.
2. **Given** an operational update is displayed, **When** a passenger views it, **Then** the display presents the published information and does not allow the passenger to create or alter that information.
3. **Given** a requested behavior belongs to an interactive station terminal, **When** the display specification is reviewed, **Then** that behavior is excluded unless separately approved as a scope change.

### Edge Cases

- The deployment-provided station identifier is missing, malformed, unknown, unavailable or refers to an unpublished station; the display must not fall back to another station.
- The station is valid and published but has no available departure data or no upcoming departures.
- A valid station has no upcoming departures, or returned departures contain missing optional operational values.
- Scheduled data is available while journey status, platform or delay data is unavailable; each value retains its own meaning and availability.
- The update source returns an error, an older or duplicate revision, a missing revision, or no usable source/update timestamp; handling and ordering depend on the verified backend contract and must not be guessed.
- The display disconnects before first load, disconnects while showing data, or reconnects while newer updates are being published.
- A cancellation, platform change or delay affects a departure while older cached data remains visible.
- At either approved profile, content pressure must not silently hide station identity or important disruption information; other physical viewing-distance and hardware assumptions are not approved.

## Requirements

### Approved Requirement Traceability

The following wording and priorities are copied from the approved requirements inventory, which transcribes the authoritative workbook. Their acceptance checks are preserved verbatim.

| ID | Priority | Approved requirement wording | Approved source acceptance check |
|---|---|---|---|
| SR-054 | Must Have | The station display experience shall show appropriate upcoming departure services with scheduled times and latest known operational statuses. | Display filters services for its station and updates when a departure changes. |
| SR-056 | Must Have | Station information shall clearly show updated platforms, delays and cancellation notices from the authoritative feed. | Publishing a platform change updates the correct display and exposes updated time. |
| SR-058 | Must Have | Station information clients shall automatically refresh or receive updates when central service state changes. | Connected display refreshes after authorized update without manual reload. |
| SR-059 | Must Have | When central connectivity is lost, station displays shall identify their information as cached or possibly outdated instead of showing it as guaranteed live. | Offline simulation shows last-updated label or a visible service-unavailable indicator. |
| SR-061 | Must Have | The SmartRail assessment implementation shall include a functional station experience representing passenger-information displays, interactive terminals, or both. | A working station-specific UI is included; both display and kiosk are not compulsory. |

### Applicable Cross-Cutting Requirements

These approved requirements apply to shared data or information meaning needed by the Station Display. Their exact wording and priorities are retained.

| ID | Priority | Approved requirement wording | Approved source acceptance check |
|---|---|---|---|
| SR-001 | Must Have | SmartRail shall provide an integrated passenger, railway operations, driver/operator, and station-information experience backed by shared services rather than isolated, inconsistent applications. | A journey update is retrievable through the appropriate passenger, operations, driver and station channels. |
| SR-002 | Must Have | The platform shall maintain identifiable railway stations and expose station names and essential location/service details to passenger and operational channels. | Station records can be created, retrieved, referenced by services, and displayed. |
| SR-003 | Must Have | The platform shall represent railway routes and their ordered station sequences. | Route stops are returned in journey order with unambiguous station IDs. |
| SR-004 | Must Have | The platform shall maintain train services associated with routes and identifiable service schedules. | Every published service references a valid route, schedule and service identifier. |
| SR-010 | Must Have | Published journey and disruption status shall originate from authorized railway sources and be shared consistently with all dependent applications. | Same service/event revision is visible across subscribed UIs, including source and updated time. |
| SR-035 | Must Have | Missing live train positions or ETA predictions shall be shown as unknown/last known rather than guessed. | Feed-unavailable scenario shows an explicit unknown or stale label. |

SR-001 to SR-004 provide shared context and reference identity; they do not authorize the display to replace a station departure feed with scheduled service-search results. SR-010 and SR-035 constrain the meaning of shared status and missing live values. SR-054 through SR-061 define the display-specific scope. No other priority tier is promoted into Must Have scope here. The station display is the single prototype selected under SR-061; an interactive Station Terminal is not required.

### Functional Requirements

- **FR-001**: Each display MUST use a deployment-provided stable station identifier from the authoritative Railway Network service. The display MUST validate that identifier against public, published station information and present information for exactly that station only. If the identifier is missing, invalid, unavailable or unpublished, it MUST show a configuration or unavailable state and MUST NOT fall back to another station. There MUST be no passenger-facing station selector or station-management UI.
- **FR-002**: The display MUST show upcoming station departures from the authoritative station-departure source. It MUST NOT substitute service-search results or treat a reusable scheduled service as a confirmed dated departure.
- **FR-003**: For each departure, the display MUST distinguish published scheduled time from available current operational status. It MUST preserve any source-provided meaning and update time.
- **FR-004**: The display MUST present authorized platform changes, delays and cancellations for the affected departure. Missing or unsupported operational information MUST be labeled unknown or unavailable rather than inferred.
- **FR-005**: After station validation, the display MUST fetch the station-departure snapshot and, while connected, MUST poll the approved station-departures GET endpoint at a deployment-configurable prototype interval defaulting to 30000 ms. The interval is supplied through the public Vite variable `VITE_STATION_DEPARTURE_REFRESH_INTERVAL_MS`; Vite variables are public and normally embedded at build time, so they MUST NOT contain secrets. A missing variable uses the default; a configured value MUST be a positive integer supported by the browser timer, and invalid values MUST prevent polling rather than create a zero-delay or uncontrolled request loop. The poller MUST recalculate the `Asia/Colombo` station-local service date for every request, skip/coalesce scheduled or reconnect triggers while a request is in flight rather than queue overlapping requests, and support immediate reconnect-triggered revalidation. On failure, existing information MUST remain marked cached/possibly outdated until successful backend revalidation. This is a prototype client policy only; it is not a production freshness SLA, backend latency guarantee, backend push contract, or cache-expiry threshold.
- **FR-006**: When central connectivity is unavailable, the display MUST show available previously obtained information only with a clear cached, stale or possibly-outdated indication and last-update time when available. If no usable prior information exists, it MUST show an unavailable state.
- **FR-007**: After connectivity returns, the display MUST successfully revalidate against authoritative information before representing the board as current or removing its stale indication.
- **FR-008**: The display MUST provide distinguishable loading, no-departures, unavailable, error, disconnected/stale and recovered states. State messaging MUST not imply a successful live update when one has not occurred.
- **FR-009**: The presentation MUST clearly identify the station and organize departure information for scanning at the two approved landscape prototype profiles: 1920 × 1080 and 1366 × 768. It MUST preserve station identity, scheduled departure time, service identity and available operational status at both profiles, without silently hiding important disruption information at the smaller profile. No physical viewing distance, hardware certification or additional resolution is approved.
- **FR-010**: Status and disruption meaning MUST be available through text or symbols as well as color, and essential disruption information MUST remain distinguishable from planned schedule information.
- **FR-011**: The display MUST remain a passive passenger-information experience. It MUST NOT expose station management, controller, privileged data-editing or interactive Station Terminal workflows.
- **FR-012**: The display MUST support WCAG 2.2 AA for applicable information and interactions. Detailed layout and display resolution decisions belong in this feature's approved design/plan, within that conformance requirement.

### Key Entities

- **Station context**: The single station whose passenger information this display presents, identified by the authoritative station identity.
- **Station departure**: A dated departure at the selected station with its service identity and published scheduled time, distinct from a reusable service schedule.
- **Operational update**: Authorized, source-provided information associated with a departure, such as status, platform, delay or cancellation, with source/update time and revision where available.
- **Display information state**: Whether the board is loading, current, empty, unavailable, disconnected, showing cached/stale information or recovered.

## Success Criteria

### Measurable Outcomes

- **SC-001**: In verification scenarios covering all five display-specific Must Have requirements, each approved acceptance check is demonstrated with recorded evidence.
- **SC-002**: In a station-data fixture containing departures for multiple stations, 100% of displayed departures belong to the configured station and no service-search-only result is represented as a station departure.
- **SC-003**: In every verification case with a published cancellation, delay or platform change, the affected departure shows the authorized value and its provided updated time; missing values are never fabricated.
- **SC-004**: In every connected update scenario, the display automatically obtains and presents updated authoritative information without manual passenger action. Prototype polling uses the configured interval (default 30000 ms); no production latency or freshness guarantee is asserted.
- **SC-005**: In every disconnection scenario, the board visibly indicates cached/possibly outdated data or service unavailability; it removes the warning only after authoritative information is obtained following reconnection.
- **SC-006**: At both approved landscape profiles (1920 × 1080 and 1366 × 768), reviewers can identify station, scheduled departure time, service identity and available operational status; important disruption information is not silently hidden at 1366 × 768, and status is understandable without relying on color alone.
- **SC-007**: Review confirms that the passenger-facing display exposes no privileged railway management actions or interactive terminal workflow.

## Assumptions

- The experience is one passive, public station display prototype associated with exactly one station through a deployment-provided stable station identifier, not an interactive kiosk, operations dashboard or passenger application.
- A station departure feed is the source for confirmed station departures. The Railway Network endpoints provide stations, routes and planned services only; they are insufficient to substitute for the station departure feed.
- Journeys & Status OpenAPI v1.0.0 is approved and its departures endpoint and response are verified for T024–T026. The prototype client uses this snapshot endpoint for HTTP polling; the contract itself does not define polling or push delivery.
- The display may retain previously obtained information for presentation during connection loss only if it clearly communicates that the information may be outdated. Cache policy and validity are not set here.
- Approved prototype targets are landscape 1920 × 1080 and 1366 × 768 only. No viewing distance, hardware certification or additional resolution is assumed.
- The prototype polling interval defaults to 30000 ms and is configurable as specified in FR-005. This does not establish production cadence, backend capacity, a freshness duration, cache expiry, retry schedule, operational threshold, client revision-comparison rule, or unverified timestamp display meaning.
- Isolated, clearly identified contract fixtures MAY support frontend UI development before live integration is available. Fixtures MUST NOT be presented as live data; final live-integration acceptance depends on verified backend behavior.
- Requirement inventory entries reproduce workbook wording and acceptance checks; the workbook remains the governing source.

## Dependencies and Clarifications

### Backend dependencies

Feature 001 currently implements these Railway Network endpoints:

- `GET /api/stations` — station identity and approved essential station details; supports station identification, not station departures.
- `GET /api/routes/{id}/stops` — ordered route reference data.
- `GET /api/services` — published service search and planned schedule information only.
- `GET /api/services/{id}` — published service details and schedule reference only.

The following Journeys & Status v1.0.0 operations are approved and verified; their use remains bounded by the contract register:

- `GET /api/stations/{id}/departures?date=YYYY-MM-DD` — date-scoped source for the station's published departure projection, requested with the current `Asia/Colombo` station-local date. The contract does not define “upcoming-only” filtering; do not add client-side operational eligibility rules or reorder the response.
- `GET /api/journeys/{id}` — approved but not needed when the station-departures projection contains the required public fields.
- `GET /api/journeys/{id}/updates` — approved complete history but not needed for snapshot polling; it defines no push, cursor, wait, or polling behavior.

The approved snapshot contract defines its response schema, station-local date, ordering, public fields, null values and errors. It does not define “upcoming-only” filtering, a delivery mechanism, client revision-comparison rules, backend capacity guarantees, production cadence, cache expiry/freshness thresholds, or all timestamp presentation meanings. The display MUST NOT infer those details or substitute Railway Network service search.

### Outstanding backend contract dependencies

Product decisions Q1–Q3 have been resolved for the prototype in the Clarifications session above. The following remain unresolved and MUST NOT be treated as settled by the client polling decision:

- Final production refresh policy, backend capacity, and operational guarantees.
- Cache expiry and numeric freshness thresholds.
- Client behavior for backend revisions across separately fetched snapshots; the contract supplies a per-journey revision but does not prescribe a client comparison/replay policy.
- Any timestamp display meaning not verified by the contract and UI review.
- The endpoint returns a date-scoped projection and does not define an “upcoming-only” cutoff; no cutoff is invented by the client.

These questions require verification and agreement with the relevant backend, operations or product owner before production-policy or final acceptance claims. No specification statement here expands the approved API contract.

## Out of Scope

- Interactive Station Terminal search, touch navigation, session reset or unattended kiosk behavior.
- Station or controller management, editing, publishing, authentication or privileged actions.
- Creating, changing or correcting railway source data, schedules, platforms, status or disruption decisions.
- Treating `GET /api/services` search results as confirmed station departures or live journeys.
- Inventing train locations, ETAs, delays, platforms, operational state, unapproved refresh intervals, cache validity or timing thresholds.
- Adding station arrivals, local safety notices or other deferred/non-Must Have behavior unless separately approved.
