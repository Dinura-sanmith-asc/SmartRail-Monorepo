# Feature Specification: Journeys & Status

**Feature Branch**: `003-journeys-status`
**Created**: 2026-10-18
**Status**: Draft — business clarifications required
**Input**: Approved SmartRail MVP requirements and RBAC/API baseline

## User Scenarios & Testing

### User Story 1 — Passenger views a dated journey (Priority: P1)

A passenger selects a scheduled service and travel date and opens its dated journey. SmartRail presents the journey’s published status, relevant scheduled and operational stop information, and an arrival estimate only when an authoritative estimate is available. The passenger can distinguish current published information from stale or unavailable information.

**Why this priority**: A dated journey and its authoritative status are the basis of the approved passenger status and disruption workflows.

**Independent Test**: Given a published scheduled service and a dated journey with controlled official updates, retrieve the journey and verify journey identity, status, stop order, revision/update time, and estimate freshness. Verify absent predictions are represented as unknown/unavailable rather than fabricated.

**Acceptance Scenarios**:

1. **Given** a scheduled service has a journey instance for the requested operation date, **When** a passenger reads that journey, **Then** SmartRail returns its current officially published status and relevant published journey information.
2. **Given** two dated journey instances reference the same scheduled service and operation date, **When** each has a different official status, **Then** each journey retains its own identifier and status.
3. **Given** no ETA is supported or its source is unavailable, **When** a passenger views the journey, **Then** the ETA is shown as unavailable, unknown, or last known with its update time; no value is guessed.
4. **Given** the journey has no matching published operational information, **When** it is requested, **Then** the response distinguishes unavailable information from a valid journey with no additional updates.

### User Story 2 — Controller publishes an official operational change (Priority: P1)

An authorized Controller reviews a dated journey and publishes an allowed status, cancellation, platform, or operational timing change. The publication records who made it and when, advances the official journey information consistently, and becomes the source used by dependent passenger and station views. A Driver observation may inform review but cannot itself change official journey state.

**Why this priority**: The approved requirements reserve authoritative journey updates and cancellation decisions to authorized railway roles, specifically Controllers under the approved RBAC matrix.

**Independent Test**: Submit an allowed change as a Controller and verify persisted official state, actor/time evidence, and identical revision in dependent reads. Repeat as Station Staff and Driver and verify denial without state change. Submit a Driver observation and verify it remains an observation.

**Acceptance Scenarios**:

1. **Given** a Controller is authorized for the journey, **When** they publish an allowed status or stop change, **Then** the official journey state and its audit evidence are updated.
2. **Given** a caller is not authorized to publish, **When** they attempt an official status, cancellation, platform, or timing change, **Then** the change is denied and official state is unchanged.
3. **Given** a Driver submits a progress observation, **When** it is accepted, **Then** it is attributed to its author and remains non-authoritative until an authorized Controller publishes an official change.
4. **Given** a publication affects more than one permitted consumer, **When** those consumers read the journey, **Then** they see the same official revision and source/update time.

### User Story 3 — Passenger and station consumers see a disruption consistently (Priority: P1)

A passenger viewing a searched or selected service and a station display viewing departures see relevant published delay, cancellation, route, platform, and departure-time changes. An approved change is visible in the corresponding journey and station views, with a clear updated time. The approved in-app alert behavior does not require native device push.

**Why this priority**: Disruption visibility is an explicit MVP passenger and station outcome, and conflicting operational versions could mislead users.

**Independent Test**: Publish a controlled disruption and verify that journey detail, affected station departure view, and configured in-app alert use the same official revision. Verify an unaffected service is not changed.

**Acceptance Scenarios**:

1. **Given** an authorized Controller publishes a delay, cancellation, route, platform, or departure-time change, **When** a passenger views the affected service, **Then** the relevant change is connected to that service and its publication time is visible.
2. **Given** a published change affects a station departure, **When** the station departure view refreshes, **Then** it shows the correct service’s updated operational information.
3. **Given** a significant cancellation or platform change is published, **When** the configured in-app channel is available, **Then** a matching passenger alert and journey status are shown from the same revision.
4. **Given** a station display cannot refresh central information, **When** it presents cached information, **Then** it identifies the information as cached, stale, or unavailable with a last-updated indication.

### User Story 4 — Operational teams use journey status without crossing module authority (Priority: P2)

Driver Operations can provide assigned-journey context and attributed observations; Incident Management can associate incidents with affected active journeys; and operational consumers can view the resulting official journey status. Each module remains authoritative for its own records and evidence. Feature 003 does not implement driver assignment, incident lifecycle, or signaling/control.

**Why this priority**: The approved end-to-end workflows depend on these boundaries while keeping ownership and authorization explicit.

**Independent Test**: Verify a Driver Operations assignment/observation and an Incident Management journey association can be consumed as evidence or linked context without either changing official journey state absent Controller publication.

**Acceptance Scenarios**:

1. **Given** a Driver Operations observation is available, **When** an authorized Controller reviews it, **Then** the observation’s author and content are available as evidence and official state changes only through the approved Controller publication operation.
2. **Given** Incident Management records an incident affecting one or more journeys, **When** journey consumers view the affected journeys, **Then** the association is visible without Journey & Status taking ownership of incident lifecycle transitions.
3. **Given** scope or authorization evidence from Feature 002 is absent, stale, revoked, or unavailable, **When** a protected operational change is attempted, **Then** access is denied or reported unavailable and no official state changes.

### Edge Cases

- A valid scheduled service has no dated journey instance for the requested operating date; do not synthesize a journey from the reusable schedule.
- Multiple journeys share a service and operating date, or a schedule revision changes after a journey is instantiated; identity and linkage follow the approved rule once clarified, without overwriting historical journey data.
- A service crosses midnight; the journey remains tied to its operating-date anchor even when later stops fall on the next calendar date.
- The requested journey, station, or stop does not exist, is not part of the journey, or is no longer published; return the contractually appropriate missing/unavailable outcome without leaking protected data.
- An update references an invalid status, stale revision, invalid stop order, or unsupported platform/time change; reject it without partial publication.
- Concurrent Controllers attempt updates from the same prior revision; do not silently lose or reorder an update, and preserve an auditable outcome.
- A valid journey has no additional updates, while another has an incomplete or unavailable publication source; keep these outcomes distinguishable.
- An ETA, position, update, or feed is missing, stale, delayed, or unavailable; show unknown/last-known with source and time where available, never a guessed value.
- A Controller cancels or terminates a journey and a later correction is attempted; apply only the clarified transition policy and retain prior publication evidence.
- A Driver observation is missing, duplicated, stale, or unverifiable; it must not change official journey status.
- A linked incident affects multiple journeys or is later resolved; preserve Incident Management ownership and do not infer journey status from incident lifecycle alone.
- A downstream notification, client refresh, or dependent module is unavailable; retain the official publication and make delivery/read condition distinguishable without fabricating success.
- Feature 002 role or resource-scope authority is unavailable, stale, or revoked during an operational request; deny the write and leave official state unchanged.

## Requirements

### Approved Requirement Traceability

The following source wording and acceptance checks are transcribed from the approved `FINAL UNIFIED REQUIREMENTS` workbook. All listed requirements are **Must Have**. “Feature owner” distinguishes direct Feature 003 delivery from requirements whose implementation belongs to a dependent module or client; those downstream requirements remain open until their owning feature supplies evidence.

| ID | Approved requirement wording | Approved acceptance check | Feature 003 relationship |
|---|---|---|---|
| SR-007 | An operational journey instance shall be distinguishable from its reusable scheduled service and shall carry its own current progress and status. | Two departures of the same scheduled service can have distinct IDs and statuses. | Direct — dated journey identity and state. Identity/cardinality requires clarification below. |
| SR-010 | Published journey and disruption status shall originate from authorized railway sources and be shared consistently with all dependent applications. | Same service/event revision is visible across subscribed UIs, including source and updated time. | Direct — authoritative publication and consistent revision. |
| SR-019 | The passenger app shall display an active journey status such as scheduled, running, delayed, interrupted, cancelled or completed using authoritative data. | Status labels reflect published journey-state transitions. | Direct backend data; passenger rendering is a client obligation. |
| SR-021 | Passengers shall be able to view estimated arrival times where available and an appropriate unavailable indicator when predictions cannot be supported. | ETA shows data freshness and does not invent values when feed is missing. | Direct — ETA availability and freshness data; no prediction fabrication. |
| SR-022 | Passengers shall see relevant delay, cancellation, route, platform and departure-time changes connected to a searched or selected service. | Publishing a disruption updates related service-detail presentation. | Direct publication/read model; route-change publication shape needs clarification. |
| SR-025 | Significant journey changes such as cancellation or platform alteration shall be communicated promptly using the configured available channels. | A test cancellation triggers matching passenger notification and visible in-app status. | Direct in-app change/alert evidence; native device push is not required by the approved minimum implementation. No unapproved latency threshold is added. |
| SR-031 | The platform shall track each active journey progress and expose the latest authorized operational update to permitted consumers. | Authorized updates change the corresponding journey status across test clients. | Direct — latest official update and permitted reads. |
| SR-034 | SmartRail shall accept authorized operational updates and permit manual entry when dependable external railway feeds are unavailable. | Operator can publish a manual status change with actor and timestamp; no GPS feed needed for demo. | Direct — authorized manual publication; no external feed required. |
| SR-035 | Missing live train positions or ETA predictions shall be shown as unknown/last known rather than guessed. | Feed-unavailable scenario shows an explicit unknown or stale label. | Direct data freshness/availability; presentation is also a client obligation. |
| SR-040 | A suitably authorized railway user shall be able to record permitted journey status updates, including delays, cancellations and termination. | Authorized update persists; non-authorized actor is blocked; audit event is created. | Direct — Controller-authorized official publication and audit. |
| SR-046 | Only roles authorized under railway policy may confirm a train cancellation; the app shall not independently decide to cancel a service. | Station and driver accounts cannot execute controller-only cancellation API. | Direct — preserve approved Controller-only cancellation grant. |
| SR-048 | SmartRail shall propagate approved operational status changes to passenger and station channels without adding an unapproved separate communications-officer approval workflow. | One approved service change appears on multiple channels from same revision. | Direct — consistent publication; no extra approver role/workflow. |
| SR-054 | The station display experience shall show appropriate upcoming departure services with scheduled times and latest known operational statuses. | Display filters services for its station and updates when a departure changes. | Shared — Feature 003 station-departure data; station display client owns rendering/filter refresh. |
| SR-056 | Station information shall clearly show updated platforms, delays and cancellation notices from the authoritative feed. | Publishing a platform change updates the correct display and exposes updated time. | Shared — authoritative platform/status data and revision; client display owns presentation. |
| SR-058 | Station information clients shall automatically refresh or receive updates when central service state changes. | Connected display refreshes after authorized update without manual reload. | Downstream station-display client owns refresh/receive behavior; Feature 003 supplies the published change. |

### Related Approved Must Have Dependencies and Client Obligations

These approved requirements are traced because Feature 003 status and resource authorization participate in their workflows. They do not transfer ownership of other modules or clients into Feature 003.

#### Feature 001 Railway Network Prerequisites

| ID | Approved requirement wording | Approved acceptance check | Owning boundary |
|---|---|---|---|
| SR-002 | The platform shall maintain identifiable railway stations and expose station names and essential location/service details to passenger and operational channels. | Station records can be created, retrieved, referenced by services, and displayed. | Feature 001 owns station reference data; Feature 003 references its station identifiers. |
| SR-003 | The platform shall represent railway routes and their ordered station sequences. | Route stops are returned in journey order with unambiguous station IDs. | Feature 001 owns route/schedule calling points; Feature 003 uses them for dated journeys and realized stops. |
| SR-004 | The platform shall maintain train services associated with routes and identifiable service schedules. | Every published service references a valid route, schedule and service identifier. | Feature 001 owns scheduled services; Feature 003 distinguishes each dated operational journey from its service. |
| SR-013 | Passengers shall be able to search railway services using an origin, destination and travel date. | Valid search returns matching service choices; invalid pairs and no-result searches show useful feedback. | Feature 001 owns search; Feature 003 consumes a selected service and approved travel-date context. |

| ID | Approved requirement wording | Approved acceptance check | Owning boundary |
|---|---|---|---|
| SR-038 | Authorized railway staff shall access a web-based overview of current services, journey states and important incidents. | Role-authorized login can view live operations summary; unauthorized users cannot. | Operations client / Incident Management; Feature 003 supplies journey state. |
| SR-041 | Authorized operations or station users shall be able to record an incident with location, affected service/station, description, category and timestamp. | Incident is saved and linked to the relevant railway entity. | Incident Management owns incident creation and record. |
| SR-042 | Incident records shall support a simple lifecycle from reported through operational handling to resolved or closed. | Valid state transitions are auditable; closing a record updates dashboard. | Incident Management owns lifecycle. |
| SR-044 | An operational incident shall be associable with affected active journeys and published service disruptions where known. | One incident can affect more than one journey and its impact appears on affected journey views. | Incident Management owns incident-to-journey association; Feature 003 consumes/presents approved impact context. |
| SR-059 | When central connectivity is lost, station displays shall identify their information as cached or possibly outdated instead of showing it as guaranteed live. | Offline simulation shows last-updated label or a visible service-unavailable indicator. | Station display client; Feature 003 provides source/update metadata. |
| SR-064 | Drivers and authorized operational personnel shall sign in to an operational information application to access assigned journey information. | Valid driver sees assigned journeys; unauthorized user cannot access driver records. | Identity & Permissions and Driver Operations. |
| SR-065 | Driver users shall see assigned journeys, route, stopping order and relevant scheduled timings. | Driver can see scheduled assignment details for a selected duty. | Driver Operations/client; Feature 001 owns schedule references. |
| SR-066 | The driver application shall display authorized current journey status and recent service changes without asserting independent rail-control authority. | Changing central journey state refreshes the driver view. | Driver client; Feature 003 supplies official status. |
| SR-069 | Where authorized, a driver shall be able to report a journey progress/status observation for railway staff review without altering schedules or signalling state. | Submitted report shows author and can be viewed by Control; invalid status changes blocked. | Driver Operations owns observation submission and persistence; Feature 003 never treats it as official state by itself. |
| SR-102 | Train/disruption statuses shall convey meaning using text and/or symbols as well as color. | Delayed/cancelled/normal remain understandable without color. | Passenger, driver, station and operations clients. |
| SR-109 | Passenger, driver, and station clients shall provide clear network-availability and data-freshness indications when live information cannot be refreshed. | Offline simulator shows visible state/last updated time for each applicable client. | Owning clients; Feature 003 supplies status/update time and availability. |
| SR-114 | The system shall provide meaningful loading, no-data, access-denied, error and retry states for primary passenger and operational workflows. | Each primary view has tested no-results, 403, 500 and no-network cases. | Owning passenger, driver, station and operations client specifications; backend supplies appropriate outcomes. |
| SR-001 | SmartRail shall provide an integrated passenger, railway operations, driver/operator, and station-information experience backed by shared services rather than isolated, inconsistent applications. | A journey update is retrievable through the appropriate passenger, operations, driver and station channels. | Cross-module outcome — Feature 003 supplies a shared official journey update; clients and dependent modules own their respective reads. |
| SR-113 | Failure of an external catering API shall not prevent core train search, operations incident management or passenger service-status access. | Simulated catering outage leaves core railway features available. | Cross-module resilience obligation — Journeys & Status remains available independently of Catering Adapter failure. |
| SR-115 | The platform shall expose operational health/error information appropriate to diagnosing API, database and integration issues without disclosing secrets. | System health check and structured error logs report representative service failures. | Platform-wide observability dependency; Feature 003 failures must be diagnosable without exposing secrets. |
| SR-072 | SmartRail shall not implement train braking, door-side interlocks, movement authority, signalling commands or certified DMI functions; these remain in existing railway systems. | No train-driving command/control endpoints exist in the SmartRail application. | Explicit scope exclusion for Feature 003. |
| SR-089 | Public railway information shall be distinguishable from functions requiring passenger or railway-staff authentication. | Anonymous viewer can access permitted service details but not protected operational APIs. | Feature 002 owns the identity boundary; Feature 003 preserves anonymous published-status reads and protected Controller writes. |
| SR-090 | Operational web and driver applications shall authenticate their users before exposing protected railway information or write actions. | Protected endpoint rejects missing/invalid token and respects session expiry. | Feature 002 owns authentication; Feature 003 requires verified authentication for operational writes. |
| SR-091 | Server-side authorization shall restrict operational changes based on organizational role and the target station/journey/resource; front-end hiding alone is insufficient. | Station credentials cannot call controller-only cancellation endpoint; role/resource tests pass. | Feature 002 owns RBAC and scope decisions; Feature 003 enforces its authorization outcome for official journey changes. |

**Scope exclusion**: SR-072 prohibits SmartRail train braking, door-side interlocks, movement authority, signalling commands, and certified DMI functions. Feature 003 includes no such controls or endpoints.

### Functional Requirements

- **FR-001**: The system MUST represent each operational journey as a dated instance distinct from its reusable Feature 001 scheduled service, with its own stable identifier, progress, and official status. Multiple journey instances MAY reference the same scheduled service and operation date; each MUST remain independently identifiable. The business evidence distinguishing same-date departures remains to be specified without assuming that service/date alone is unique.
- **FR-002**: A journey MUST retain a reference to the authoritative scheduled service and ordered station calling points from Feature 001. Journey operations MUST NOT rewrite reusable service schedules or station reference data.
- **FR-003**: The system MUST keep planned schedule values, estimated values, observed values, and officially published operational values distinguishable. Each displayed estimate or operational update MUST expose its applicable source and freshness/update time.
- **FR-004**: A passenger or anonymous public consumer MUST be able to read approved published journey status and applicable station departure information using the existing public access boundary. Public responses MUST contain only information approved for public visibility.
- **FR-005**: The system MUST support the approved published statuses: scheduled, running, delayed, interrupted, cancelled, and completed, and the approved termination update where applicable. Status changes MUST follow an explicit approved transition model. Corrections MUST be retained as auditable publication evidence rather than silently overwriting or deleting prior evidence. The exact transition edges, termination representation, and reopening rules remain open; no additional state or transition may be inferred.
- **FR-006**: Only an authenticated Controller with current Feature 002 authorization for the journey/resource MAY publish official journey status, cancellation, platform, or operational timing changes permitted by the approved RBAC matrix. Station Staff and Driver MUST NOT perform Controller-only actions.
- **FR-007**: Each official update MUST record the verified publishing actor, publication time, affected journey, and ordered revision/evidence sufficient for dependent consumers to identify the same published state. Publication MUST be auditable.
- **FR-008**: A Driver progress report MUST remain an attributed, non-authoritative observation owned by Driver Operations. It MUST NOT directly change a journey’s official status, schedule, or signaling state.
- **FR-009**: The system MUST expose an ETA only when supported by an authoritative prediction; otherwise it MUST communicate unavailable, unknown, or last-known status and freshness. It MUST NOT guess a location or ETA.
- **FR-010**: Published delays, cancellations, platform, and departure-time changes MUST be associated with the affected journey/service so dependent passenger and station views can present the correct change. SR-022 also requires route changes, but their publication semantics are a known contract gap and remain uncovered until clarified; this feature adds no operation or inferred payload.
- **FR-011**: A single approved update MUST be identifiable consistently across permitted passenger, station, driver, and operational consumers by its revision and source/update time. No separate communications-officer approval workflow is introduced.
- **FR-012**: Station departure information MUST relate the station’s upcoming scheduled departures to the latest known official journey status and update time. Station filtering and automatic refresh/receive behavior are client responsibilities, supported by the approved status data.
- **FR-013**: Significant approved changes MUST support the configured in-app alert behavior. Native device push is not required by the approved minimum implementation. No numeric “prompt” latency or polling target is introduced because none is approved.
- **FR-014**: Journey reads and official changes MUST respect Feature 002 authentication, RBAC, current resource-scope and ownership decisions. Missing, stale, revoked, or unverifiable authorization evidence MUST NOT authorize a protected operation. This feature MUST NOT change the approved grants.
- **FR-015**: The feature MUST interoperate with Incident Management’s affected-journey context without owning incident creation or lifecycle, and with Driver Operations’ assignments/observations without owning those records.
- **FR-016**: The feature MUST distinguish a valid journey with no additional updates from missing journey data, incomplete/unavailable authoritative publication data, invalid input, and denied access. Failure responses MUST not expose credentials, personal data, or internal exception details.
- **FR-017**: If an external operational feed is absent or unreliable, an authorized Controller MUST be able to publish a supported manual update with actor and timestamp; absent feeds MUST NOT cause fabricated status, position, or ETA.
- **FR-018**: Feature 003 MUST remain within the approved Journeys & Status operation inventory: journey detail read, journey updates read, station departures read, Controller journey-status publication, and Controller stop/platform/timing publication. It MUST NOT add an unapproved route or review/approval operation. The approved boundary leaves route-change publication and explicit Driver-observation linkage uncovered pending contract clarification; an observation cannot itself authorize or substitute for official publication.
- **FR-019**: The feature MUST NOT implement incident lifecycle, driver assignment/observation ownership, independent client presentation states, or train control/signaling functions.

### Key Entities

- **Dated Journey**: An operational instance linked to a scheduled service and operation date. Multiple instances may reference the same service/date; each has its own identity and current official progress/status. The evidence distinguishing same-date departures remains to be specified.
- **Journey Stop**: An ordered realized calling point for a journey, linked to a Feature 001 station and schedule reference. It can carry distinct planned, estimated, observed, and officially published platform/time values without changing the reusable schedule.
- **Official Journey Update**: An immutable or historically retained publication event for an authorized status or operational change, carrying actor, time, revision, source, and affected journey/stop context.
- **Operational Observation**: A non-authoritative Driver Operations record associated with an assigned journey and attributed driver; owned and managed by Driver Operations.
- **Incident Impact Association**: A reference from Incident Management to one or more affected journeys/disruptions; ownership and lifecycle remain with Incident Management.
- **Publication Source/Revision**: Evidence identifying the authoritative origin and sequence/version of an update, used to keep dependent reads consistent and freshness visible.

### API and Access Boundary

The approved RBAC/API baseline permits Journeys & Status reads for journey detail, journey updates, station departures, and operations observations, plus Controller writes for journey status and journey stop status/platform/timing. This is the complete approved interaction boundary for this feature; detailed schemas and response/error behavior belong in planning/contracts. No other operation is introduced. Public consumers may read only approved published status and station departure data. Official writes require the current Controller grant and resource authorization from Feature 002. Driver observation access and ownership remain with Driver Operations; the approved baseline provides Controller observation read access but no separate observation-review action.

## Success Criteria

### Measurable Outcomes

- **SC-001**: In tests with two journey instances for the same scheduled service and operation date, each instance has a distinct identifier and independently published status is returned without cross-journey leakage.
- **SC-002**: 100% of tested official updates identify the authorized Controller actor, publication time, affected resource, and revision; unauthorized attempts create no official update.
- **SC-003**: All tested passenger and station reads for one published change return the same revision, source, and update time.
- **SC-004**: In all test cases without a supported ETA/position, the system returns explicit unavailable/unknown/last-known information and produces no guessed value.
- **SC-005**: Tests confirm Driver observations never change official journey state unless a separately authorized Controller publication occurs.
- **SC-006**: Tests confirm Station Staff and Driver cannot perform Controller-only cancellation or official status publication.
- **SC-007**: A controlled cancellation/platform publication is reflected in the affected journey and station departure information, and the configured in-app notification evidence references the same revision.
- **SC-008**: Authorization tests deny protected changes when current role, station/resource scope, or owning-module evidence is absent, stale, revoked, or unavailable.
- **SC-009**: No test or API behavior in this feature issues train control, signaling, movement-authority, door-interlock, or certified DMI commands.
- **SC-010**: All direct Feature 003 Must Have requirements have linked acceptance tests before local completion; client- and other-module-owned requirements stay open until their own evidence is recorded.

## Clarification Decisions and Remaining Gaps

1. **Approved — multiple instances per service/date**: Multiple journey instances may refer to the same scheduled service and operation date and must have distinct identities/status. The exact authoritative departure/instance discriminator remains to be defined in technical design without treating service/date as unique.
2. **Approved — explicit lifecycle with audited corrections**: Status changes must use an explicit transition model, and corrections must preserve an auditable record. The exact allowed transition edges, termination representation, and whether cancelled or completed journeys may be reopened remain undecided; do not infer them.
3. **Approved boundary — no new operation; gaps remain open**: Keep the existing operation inventory and add no route-change or Driver-observation review operation. The approved contract still does not explain how route changes under SR-022 are published or how a reviewed Driver observation is linked to official publication. These capabilities remain uncovered until the contract is clarified; do not infer payloads or behavior.

## Assumptions

- The approved MoSCoW workbook controls requirement IDs, exact wording, priority, and acceptance checks; the RBAC/API PDF controls existing grants and operation inventory.
- The approved 53 Must Have requirements define MVP scope. This specification does not change the priority or wording of any baseline requirement.
- Feature 001 remains authoritative for stations, routes, scheduled services, ordered stops, schedule revisions, and published service-date completeness. Dated journeys are operational instances and do not replace scheduled services.
- Feature 001’s approved overnight-search interpretation remains: travel date means scheduled departure date from the selected origin. A journey’s operation date follows the published schedule’s date anchor; arrival after midnight does not create another journey. Multiple departures for the same service/date are permitted and have distinct journey identities; the authoritative instance discriminator remains to be defined.
- Schedule times use the existing approved local-time basis (Asia/Colombo). No new timezone, recurrence, or calendar rule is introduced.
- Feature 002 is authoritative for verified identity, active roles, RBAC grants, and resource-scoped authorization. Current scope/ownership evidence comes from its owning module and unavailable evidence fails closed.
- Controllers are the only approved role for official journey status, cancellation, platform, and operational timing changes. No separate communications-officer approval is added.
- Driver observations are not authoritative. Driver Operations owns submission and persistence; the existing contract permits Controller reads but does not define a review mutation.
- In-app disruption notification is the approved minimum. Native device push, external feed integration, predictive ETA generation, and numeric freshness/latency/polling guarantees are not assumed.
- Incident Management owns incident records, lifecycle, and journey-impact links. Feature 003 exposes authoritative journey context and consumes approved impact references.
- Public journey/status and station-departure visibility follows the approved public-read boundary. No additional personal, driver, or protected operational information is exposed.
- Development and verification are local-first. AWS deployment and resource configuration remain a later manual user activity; this feature does not provision or deploy AWS resources.

## Dependencies

- **Feature 001 — Railway Network & Train Search**: authoritative station, scheduled-service, ordered-stop, schedule-revision and service-date data; preserve its API and domain contracts.
- **Feature 002 — Identity & Permissions**: verified Cognito identity, active SmartRail roles, exact approved grants, and current resource-scoped authorization. Controller writes depend on its fail-closed decisions.
- **Driver Operations**: assigned journey evidence and attributed, non-authoritative observations; this feature does not create assignments or observations.
- **Incident Management**: incident lifecycle and affected-journey associations; this feature does not own incident state transitions.
- **Passenger, station-display, driver, and operations clients**: status labels, accessible non-color meaning, freshness/network/loading/error/retry presentation, alert delivery, and refresh behavior need separate client specifications and tests. SR-025, SR-054, SR-056, SR-058, SR-059, SR-066, SR-102, SR-109, and SR-114 are not complete through backend implementation alone.

## Out of Scope

- Reusable railway network and schedule maintenance (Feature 001).
- Identity, token validation, role mapping, or RBAC policy changes (Feature 002).
- Driver assignment or observation lifecycle and incident creation/lifecycle (owning modules).
- New role grants, communications approval roles, or unapproved public APIs.
- Independent route reconstruction, timetable changes, signaling, movement authority, braking, door control, or certified train-driving displays.
- Unapproved external live feeds, predictive algorithms, guaranteed ETA, or invented service-level thresholds.
- AWS resource creation or deployment.
