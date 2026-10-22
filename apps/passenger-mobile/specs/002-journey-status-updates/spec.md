# Feature Specification: Journey Status and Updates

**Feature Branch**: `002-journey-status-updates`

**Created**: 2026-10-19

**Status**: Draft — backend contract verified; passenger UX clarifications resolved

**Input**: User description: "SmartRail Passenger — Journey Status & Updates"

## Clarifications

### Session 2026-10-19 — Approved Decisions

- Q1: When multiple dated journey candidates are marked ambiguous, should each remain a passenger choice or should details be blocked? **Answer:** Show each candidate as a separate choice and require an explicit passenger tap. Keep every returned journey identity distinct, visibly communicate ambiguity, and never auto-select a candidate, even if the public labels appear identical.
- Q2: Without push, subscription, or polling contracts, when should an in-app change alert appear? **Answer:** Alert when opening or manually refreshing the journey view detects a new update. Associate the alert with the update identity and revision; do not imply automatic background delivery or periodic refresh.

Both decisions are approved and incorporated into the acceptance scenarios and functional requirements below.

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Open the dated journey for a selected service (Priority: P1)

A passenger who has selected a published scheduled service in Feature 001 can find and open the corresponding dated operational journey for the selected origin and travel date. The app keeps the reusable scheduled service identity separate from the dated journey identity.

**Why this priority**: Current official status belongs to one dated journey instance. A scheduled service alone does not identify which departure instance the passenger is viewing.

**Independent Test**: From an existing Feature 001 service detail, use its origin, travel date, and service identity to retrieve public station departure choices. Select a matching dated journey and verify its opaque journey identity is used for the status view. Verify no match does not create a synthetic journey and ambiguous candidates are never selected automatically.

**Acceptance Scenarios**:

1. **Given** a passenger is viewing a scheduled service with an origin and travel date, **When** they open journey status, **Then** the app finds dated departures at that origin for that station-local date and presents only candidates associated with the selected service.
2. **Given** exactly one unambiguous dated journey matches, **When** the passenger opens it, **Then** the app requests and displays that journey by its returned journey identity, distinct from the scheduled service identity.
3. **Given** no dated journey matches the selected service and date, **When** the lookup completes successfully, **Then** the app explains that dated journey information is not available and does not synthesize one from the schedule.
4. **Given** multiple dated journeys match and the backend marks one or more candidates ambiguous, **When** the choices are shown, **Then** each candidate is presented as a separate choice with an ambiguity cue, and the passenger must explicitly tap one; the app does not auto-select a candidate even when labels appear identical.

### User Story 2 — Inspect official journey status and updates (Priority: P1)

A passenger can see the selected dated journey’s backend-published status, planned and operational stop information, safe update source and time, and ordered public update history. The view distinguishes the scheduled baseline from official operational publications and never treats driver observations as official status.

**Why this priority**: Passengers need current, authoritative information to understand whether a train is scheduled, running, delayed, interrupted, cancelled, completed, or terminated.

**Independent Test**: Use controlled public journey responses for the supported status values, a scheduled-only baseline, operational stop changes, and ordered update history. Compare the displayed status, stop data, source, revision, and publication time with the response. Verify private actor data and driver observation details are absent.

**Acceptance Scenarios**:

1. **Given** the journey read returns a published status, **When** its details load, **Then** the passenger sees the status text and the journey revision and source/update time supplied by the backend where present.
2. **Given** a journey has only its scheduled baseline and no official publication time, **When** its details load, **Then** the app labels the status as scheduled/baseline and does not imply a current official update time.
3. **Given** the journey contains planned and operational stop values, **When** the passenger inspects its stops, **Then** planned and operational values are visibly distinguished, kept in the returned stop order, and not recalculated or converted into other status claims.
4. **Given** a valid journey has no public updates, **When** the updates read returns an empty collection, **Then** the app shows a distinct no-updates message while retaining the journey snapshot.
5. **Given** public updates exist, **When** the passenger views the history, **Then** entries appear in backend revision order and show the supplied change type, status, safe source, publication time, and applicable public change details.
### User Story 3 — Refresh information and understand its freshness (Priority: P1)

A passenger can request an updated journey snapshot and update history and can tell when a read succeeded, when the backend says an official update was published, and when the latest attempt failed.

**Why this priority**: A status screen that hides a failed refresh or presents old information as current can mislead a passenger during travel.

**Independent Test**: Load a journey, refresh with a newer revision, then simulate a failed refresh. Verify revision and backend freshness values update on success and retained values are explicitly marked not refreshed after failure.

**Acceptance Scenarios**:

1. **Given** journey information is displayed, **When** the passenger requests refresh, **Then** loading feedback appears and the app independently refreshes the journey snapshot and public update history.
2. **Given** a refresh succeeds with a newer revision, **When** the response is displayed, **Then** the app replaces the prior snapshot/history with the internally consistent returned revision and reports the backend source/time values without inventing a server update time.
3. **Given** a refresh fails after information has loaded, **When** the failure is shown, **Then** retained data is labeled not refreshed and the last successful client retrieval time, if available in the current app session, is not confused with the backend publication time.
4. **Given** the passenger leaves the screen during an in-progress read, **When** the response arrives later, **Then** it does not replace information on the screen the passenger left.

### User Story 4 — Notice significant changes and recover from unavailable information (Priority: P1)

A passenger receives clear in-app feedback for supported significant journey changes and can recover from missing, unavailable, denied, malformed, or network-failed reads without seeing internal diagnostics or being asked to sign in for public information.

**Why this priority**: Cancellation, delay, platform, route, or departure-time changes affect travel decisions; recovery behavior must preserve the distinction between confirmed data and a failed refresh.

**Independent Test**: Publish controlled public updates for delay, cancellation, platform, route, and operational-time changes, then retrieve the public reads and verify a matching in-app notice and status with the same update identity/revision. Separately test missing resources, service unavailability, network loss, and invalid successful data.

**Acceptance Scenarios**:

1. **Given** a new public update describes a supported significant change, **When** the passenger opens or manually refreshes the journey view and the update is detected, **Then** the in-app notice identifies the same journey revision and update identity as the visible official status/history. The same update is not alerted again during the active app session, and no background delivery or automatic polling is implied.
2. **Given** an unknown or unavailable ETA is returned, **When** the journey is displayed, **Then** the app shows an unavailable/unknown indication and does not calculate or invent an estimate.
3. **Given** the journey or station departure lookup returns a documented missing-resource response, **When** the passenger attempts to view it, **Then** the affected journey section explains that the information is unavailable and provides a route back to the selected scheduled service.
4. **Given** the backend reports unavailable information or the network request fails, **When** the passenger refreshes, **Then** a safe recoverable message and Retry action are shown; any previously loaded data remains visibly not refreshed.
5. **Given** a public read unexpectedly returns access denied, **When** the passenger sees the error, **Then** the app explains that access is unavailable without a sign-in prompt or disclosure of internal details.
6. **Given** the journey snapshot loads but its update history fails, **When** the page is displayed, **Then** the snapshot remains usable, only the history section is marked unavailable, and that section can be retried independently.

### Edge Cases

- A scheduled service exists but no dated operational journey was initialized for the origin date; do not infer or construct an instance.
- The same scheduled service has more than one dated journey candidate for the requested station-local date; preserve distinct journey IDs and present each ambiguous candidate separately and require an explicit passenger selection, as approved in Q1.
- A station-local departure date differs from the journey operation date for an overnight or diverted call; use the returned date fields for their stated meanings and do not rewrite the journey operation date.
- The selected service does not appear in the station departure response, even though the Feature 001 schedule remains published; report no matching dated journey.
- Journey detail exists but update history is empty; distinguish this valid result from a missing journey or failed request.
- Journey snapshot and update history return different revisions because a publication occurs between the independent reads; do not combine values as one confirmed revision. Refresh both reads and identify any temporarily inconsistent sections as not refreshed/incomplete.
- A returned status is `Terminated`; preserve this backend status distinctly from `Completed` or `Cancelled`.
- ETA is null or absent; show unavailable/unknown, not a calculated time. The current backend projection returns no ETA value.
- Planned, operational, and estimate timestamps have different values or update times; label each category and its supplied source/time without converting planned values into live status.
- A public response includes fields outside the approved contract, omits required fields, or has invalid types; do not display malformed data as confirmed status.
- A public read unexpectedly returns 401/403, 500, 400, 404, or 503; map only documented contract semantics, use safe generic feedback for undocumented failures, and never prompt sign-in for the approved anonymous reads.
- A driver observation or private initialization/audit record is not part of the public projections; the app must not expose or reinterpret one as official information.
- Incident Management owns incident details; absence of incident fields in the public journey contract must not be filled with inferred incident information.
- The passenger returns to scheduled service details; Back preserves Feature 001’s active search/service context.
## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The feature MUST be reachable from the existing Feature 001 scheduled service details without replacing or changing the search/results/service-detail behavior.
- **FR-002**: To find a dated journey, the passenger app MUST use the selected scheduled service identity, its origin station, and selected travel date to obtain public station-departure candidates, and MUST associate candidates only by the returned scheduled-service identity and station-local departure date. The app MUST treat journeyInstanceId as a separate opaque identity and MUST NOT treat serviceId as a dated journey ID.
- **FR-003**: The app MUST display a dated journey only after a candidate is selected or there is exactly one unambiguous match. It MUST NOT synthesize a journey from reusable service/schedule data or automatically select an ambiguous candidate. Each ambiguous candidate MUST remain separately selectable and require an explicit passenger tap, as approved in Q1.
- **FR-004**: The app MUST display the status returned by the public journey read, including Scheduled, Running, Delayed, Interrupted, Cancelled, Completed, or Terminated, without remapping one state to another. Status meaning MUST be conveyed by text or an accessible symbol and not color alone.
- **FR-005**: The app MUST distinguish planned schedule values and the scheduled baseline from published operational stop values, estimate values, update source, revision, and publication/update time whenever those values are supplied.
- **FR-006**: The app MUST present dated journey stops in the returned order and MUST NOT calculate missing intermediate times, locations, delays, or status values.
- **FR-007**: The app MUST display an ETA only when an authoritative ETA value is returned, with its supplied source, freshness, and update time. For null, absent, or unsupported ETA, the app MUST show an unavailable/unknown state and MUST NOT derive an ETA from planned times. In the inspected current backend implementation the journey projection leaves ETA null.
- **FR-008**: The app MUST present the public update history in its returned ascending journey-revision order and distinguish a valid empty update collection from a missing journey or failed read.
- **FR-009**: The app MUST identify passenger-visible change entries by their returned update identity, journey revision, safe source, publication time, and public change details. It MUST NOT expose driver observation IDs, raw actor subjects, private initialization evidence, or incident records.
- **FR-010**: The app MUST provide an in-app notice/status presentation for supported delay, cancellation, platform, route, or operational-time changes using only the published snapshot/update data. It MUST associate the notice with the same update identity and revision shown in the history. An alert MUST be emitted only when opening or manually refreshing the journey view detects a new update; the same update MUST NOT trigger a repeated alert during the active app session. The app MUST NOT imply native push, automatic polling, or background delivery.
- **FR-011**: The passenger MUST be able to manually refresh the journey snapshot and update history. Loading, successful refresh, empty history, refresh failure, retry, and retained-not-refreshed states MUST be distinguishable.
- **FR-012**: If journey snapshot and update-history reads succeed or fail independently, each loaded section MUST remain available, each failed section MUST be identified, and Retry MUST target the affected read only.
- **FR-013**: The app MUST preserve the last confirmed information after refresh failure only when it can label that information as not refreshed; it MUST distinguish the client’s successful retrieval time from the backend’s official publication/update time.
- **FR-014**: The app MUST provide useful handling for documented HTTP 400 invalid input, 404 missing resources, and 503 unavailable information; it MUST distinguish successful empty results from errors. An unexpected 401/403 on an approved anonymous read MUST NOT prompt for sign-in. Unexpected 5xx, malformed success data, timeout, and network failures MUST receive safe recoverable feedback without fabricated API semantics.
- **FR-015**: Back navigation MUST return to the existing scheduled service details while preserving Feature 001’s active search and result context. A late response after leaving the journey view MUST NOT update the screen the passenger left.
- **FR-016**: Passenger status, alert, loading, no-data, access-denied, error, stale, and retry feedback MUST be understandable to assistive technologies and must not rely on color alone. Text scaling and accessible control behavior retain the approved priority of the mapped requirements.
- **FR-017**: The feature MUST make no journey read dependent on Catering Adapter availability and MUST use only public journey, update, and station-departure information. Passenger authentication, driver observation, official status publication, incident management, and train-control behavior are outside the feature.

### Approved Requirement Traceability

The wording and priorities below are copied from the approved FINAL UNIFIED REQUIREMENTS workbook. Mobile requirements implement only the passenger-facing portion of platform-wide requirements; they do not transfer backend authority to the app.

| SR | Approved wording | Priority | Approved acceptance criterion | Feature application |
|---|---|---|---|---|
| SR-001 | SmartRail shall provide an integrated passenger, railway operations, driver/operator, and station-information experience backed by shared services rather than isolated, inconsistent applications. | Must Have | A journey update is retrievable through the appropriate passenger, operations, driver and station channels. | Integrate the passenger view with the shared published journey/update reads; no duplicated source of truth. |
| SR-007 | An operational journey instance shall be distinguishable from its reusable scheduled service and shall carry its own current progress and status. | Must Have | Two departures of the same scheduled service can have distinct IDs and statuses. | Preserve service and dated journey identities separately; support distinct candidates and ambiguity handling. |
| SR-010 | Published journey and disruption status shall originate from authorized railway sources and be shared consistently with all dependent applications. | Must Have | Same service/event revision is visible across subscribed UIs, including source and updated time. | Render backend-published source, revision, and times without client-authored official state. |
| SR-013 | Passengers shall be able to search railway services using an origin, destination and travel date. | Must Have | Valid search returns matching service choices; invalid pairs and no-result searches show useful feedback. | Feature 001 remains owner of search; Feature 002 starts from its selected service and criteria. |
| SR-019 | The passenger app shall display an active journey status such as scheduled, running, delayed, interrupted, cancelled or completed using authoritative data. | Must Have | Status labels reflect published journey-state transitions. | Display supplied status text, including the contract’s distinct Terminated value. |
| SR-021 | Passengers shall be able to view estimated arrival times where available and an appropriate unavailable indicator when predictions cannot be supported. | Must Have | ETA shows data freshness and does not invent values when feed is missing. | Show only a supplied ETA and freshness; current backend projection returns null. |
| SR-022 | Passengers shall see relevant delay, cancellation, route, platform and departure-time changes connected to a searched or selected service. | Must Have | Publishing a disruption updates related service-detail presentation. | Display approved published changes for the selected dated journey, tied to the matching revision. |
| SR-025 | Significant journey changes such as cancellation or platform alteration shall be communicated promptly using the configured available channels. | Must Have | A test cancellation triggers matching passenger notification and visible in-app status. | Provide the approved in-app notice/status; no native push service is documented. Alert timing is limited to journey-view open or manual refresh, as approved in Q2. |
| SR-089 | Public railway information shall be distinguishable from functions requiring passenger or railway-staff authentication. | Must Have | Anonymous viewer can access permitted service details but not protected operational APIs. | These approved public reads require no passenger login; do not add authentication. |
| SR-098 | SmartRail shall distinguish public service data, restricted operational data, and personal passenger/order data for storage and access decisions. | Should Have | Data dictionary shows classifications and role visibility. | Keep this feature within approved public projections; do not expose private operational or personal data. |
| SR-100 | Sensitive personal information and credentials shall not be exposed in verbose API errors or unrestricted application logs. | Must Have | Negative tests show tokens/redacted identifiers absent from API error responses. | Show safe passenger-facing errors; do not surface internal diagnostics or protected fields. |
| SR-102 | Train/disruption statuses shall convey meaning using text and/or symbols as well as color. | Must Have | Delayed/cancelled/normal remain understandable without color. | Statuses and notices use text and accessible non-color meaning. |
| SR-103 | Interactive passenger and station-terminal controls shall have accessible names, focus order and meaningful announcements for relevant changing journey information. | Should Have | Keyboard and screen-reader tests can complete supported journey lookup tasks. | Preserve Should Have priority; announce changed status and feedback accessibly. |
| SR-104 | Passenger-facing controls shall support legible typography, sufficient contrast, usable touch targets and text scaling appropriate to target devices. | Should Have | At increased text scale, essential information remains usable and controls remain operable. | Preserve Should Have priority; keep status, date/time, notices and actions legible and operable. |
| SR-109 | Passenger, driver, and station clients shall provide clear network-availability and data-freshness indications when live information cannot be refreshed. | Must Have | Offline simulator shows visible state/last updated time for each applicable client. | Clearly mark failed refreshes and retained information as not refreshed, with available client/backend times distinguished. |
| SR-113 | Failure of an external catering API shall not prevent core train search, operations incident management or passenger service-status access. | Must Have | Simulated catering outage leaves core railway features available. | Journey status access has no dependency on catering success. |
| SR-114 | The system shall provide meaningful loading, no-data, access-denied, error and retry states for primary passenger and operational workflows. | Must Have | Each primary view has tested no-results, 403, 500 and no-network cases. | Define each passenger-facing state; public-read access denial is unexpected and never triggers a sign-in prompt. |

Approved minimum implementation scope copied verbatim from the workbook:

| SR | Approved minimum implementation |
|---|---|
| SR-001 | Minimum implementation: A journey update is retrievable through the appropriate passenger, operations, driver and station channels. |
| SR-007 | Minimum implementation: Two departures of the same scheduled service can have distinct IDs and statuses. |
| SR-010 | Minimum implementation: Same service/event revision is visible across subscribed UIs, including source and updated time. |
| SR-013 | Minimum implementation: Valid search returns matching service choices; invalid pairs and no-result searches show useful feedback. |
| SR-019 | Minimum implementation: Status labels reflect published journey-state transitions. |
| SR-021 | Minimum implementation: ETA shows data freshness and does not invent values when feed is missing. |
| SR-022 | Minimum implementation: Publishing a disruption updates related service-detail presentation. |
| SR-025 | Implement an in-app delay/cancellation/platform-change banner or event alert; native device push is not required. |
| SR-089 | Minimum implementation: Anonymous viewer can access permitted service details but not protected operational APIs. |
| SR-098 | Not committed this week. Attempt only after all 5 critical flows pass. |
| SR-100 | Minimum implementation: Negative tests show tokens/redacted identifiers absent from API error responses. |
| SR-102 | Minimum implementation: Delayed/cancelled/normal remain understandable without color. |
| SR-103 | Not committed this week. Attempt only after all 5 critical flows pass. |
| SR-104 | Not committed this week. Attempt only after all 5 critical flows pass. |
| SR-109 | Minimum implementation: Offline simulator shows visible state/last updated time for each applicable client. |
| SR-113 | Minimum implementation: Simulated catering outage leaves core railway features available. |
| SR-114 | Minimum implementation: Each primary view has tested no-results, 403, 500 and no-network cases. |

### Explicit Requirement Boundaries

- SR-020 (Could Have) current/last-known train location is not included; the passenger journey contract has no location value.
- SR-023 (Could Have) alternative services/rebooking information is not included.
- SR-024 (Could Have) notification subscription/device-level delivery is not included; no subscription or push contract exists.
- SR-026 (Won't Have — this release) configurable delay thresholds remain excluded.
- SR-090 describes authentication for operational web and driver apps; this feature’s approved public reads remain anonymous under SR-089. No passenger Cognito sign-in or token/session API is added.
- Incident records and affected-incident details belong to Incident Management and are absent from the public journey snapshot; no incident fields or incident endpoint are assumed.
- Driver observations belong to Driver Operations and remain non-authoritative; no observation endpoint is used by the passenger feature.

### Dependencies and Contract Boundaries

- **Passenger Feature 001 — Railway Search & Service Details** supplies the selected service, origin station, and travel-date context. Its existing routes, behavior, and API contract remain unchanged.
- **Backend Journeys & Status** supplies three documented anonymous reads: dated journey snapshot, public update history, and station departures filtered by station-local date in Asia/Colombo. Station departure results include both scheduled service identity and opaque journey identity, station-local departure date, original operation date, revision/status, safe source/time, and an ambiguity marker. These reads return public railway projections only.
- **Backend Railway Network** remains authoritative for station and reusable scheduled-service identities and schedules. A scheduled service is not itself a dated journey.
- **Identity & Permissions** owns authenticated operational access and Cognito/RBAC policy. The three passenger status reads are explicitly anonymous; no passenger authentication flow or protected write is part of this feature.
- **ETA availability**: the public schema permits an ETA object or null, but the inspected current journey projection returns null. The feature must show unavailable until an authoritative value is actually returned.
- **In-app alerts**: the update read exposes public changes, IDs, revisions, source, and publication time. There is no push, subscription, or notification-delivery API. The app may present an in-app alert only when opening or manually refreshing detects a new update; no background delivery guarantee is implied.
- **Incident Management** owns incident lifecycle and authorized incident context. Its APIs are not a dependency for passenger journey status in this feature.

### Key Entities *(include if feature involves data)*

- **Scheduled Service**: A reusable published service from Feature 001, identified by its own service identity and associated with an origin and selected travel date.
- **Dated Operational Journey**: One operational instance linked to a scheduled service and operation date, with a distinct opaque journey identity, current status, revision, stops, source, and available publication metadata.
- **Station Departure Candidate**: A public projection linking a station-local departure date and station call to a scheduled service and dated journey. It includes a status, revision, safe source/time, and an ambiguity indication.
- **Journey Stop**: An ordered calling point with separate planned and operational arrival/departure values and optional platform/timing metadata.
- **Official Journey Update**: A public update identified by its update ID and journey revision, with a supported change type, status, safe source, publication time, and only the applicable public change data.
- **ETA**: An optional authoritative estimate with value, source, update time, and freshness. It is unavailable in the currently implemented projection.
- **Client Retrieval State**: The client’s last successful retrieval and whether a later refresh succeeded; it is distinct from backend publication/update timestamps.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: In every controlled journey-selection test, a selected scheduled service/date opens only a returned matching dated journey identity; zero journeys are synthesized from scheduled service data.
- **SC-002**: All seven statuses supported by the public journey contract are displayed distinctly from their supplied values, and every displayed status communicates meaning without color alone.
- **SC-003**: Every successful public update-history response is rendered in ascending returned revision order; an empty history is distinguishable from a missing journey and a failed request in all applicable acceptance tests.
- **SC-004**: For every ETA fixture, a supplied estimate is displayed with its supplied source/freshness/time, while null/absent estimates produce an unavailable state and no derived ETA.
- **SC-005**: Each defined read outcome (loading, valid empty, documented 400/404/503, unexpected access denial/server response, malformed success, timeout/network failure, retry, and retained-not-refreshed data) is distinguishable in its applicable acceptance test.
- **SC-006**: Each in-app alert tested for a supported significant update displays the same journey revision and update identity as its official status/history entry; no alert is attributed to a driver observation or fabricated change.
- **SC-007**: The status workflow remains reachable anonymously from Feature 001 and returns to the same active service/search context on Back in all navigation acceptance tests.

## Assumptions

- Feature 001’s selected origin travel date is the station-local date used to look up departure candidates at the selected origin in Asia/Colombo. The backend separately supplies operation date; the app does not infer it from the selected date.
- The passenger reads only public projections. Public data returned by the backend has already passed the owning module’s publication and access rules.
- A refresh is an explicit passenger action or a foreground journey-view entry. Change alerts are detected on journey-view open or manual refresh and are not repeated for the same update during the active app session; no polling interval or background guarantee is assumed.
- The client retains journey state only for the active navigation session unless a later approved specification defines persistence. It does not claim offline status data is live.
- Existing Feature 001 timestamp display decisions apply to planned schedule values. Operational and estimate timestamps are displayed with their supplied date/time and source/freshness context, without conversion that changes the represented value.
- No response-time, notification-latency, polling, or reliability target is invented where the approved requirements and contracts provide none.

## Clarifications Resolved

Q1 and Q2 were approved on 2026-10-19 and are recorded in the Clarifications section. No unresolved UX decisions remain.