# Feature Specification: Journeys & Status

**Feature Branch**: `003-journeys-status`
**Created**: 2026-10-18
**Status**: Draft — clarification gates approved; ready for technical planning
**Input**: Approved SmartRail MVP requirements and RBAC/API baseline

## User Scenarios & Testing

### User Story 1 — Passenger views a dated journey (Priority: P1)

A passenger selects a scheduled service and travel date and opens its dated journey. SmartRail presents the journey’s published status, relevant scheduled and operational stop information, and an arrival estimate only when an authoritative estimate is available. The passenger can distinguish current published information from stale or unavailable information.

**Why this priority**: A dated journey and its authoritative status are the basis of the approved passenger status and disruption workflows.

**Independent Test**: Given a published scheduled service and a dated journey with controlled official updates, retrieve the journey and verify journey identity, status, stop order, revision/update time, and estimate freshness. Verify absent predictions are represented as unknown/unavailable rather than fabricated.

**Acceptance Scenarios**:

1. **Given** a scheduled service has a journey instance for the requested operation date, **When** a passenger reads that journey, **Then** SmartRail returns its current officially published status and relevant published journey information.
2. **Given** two dated journey instances reference the same service, schedule revision, operation date, and planned first-call occurrence but have distinct authoritative IDs, **When** a passenger reads departures, **Then** both remain separate; if published public context cannot distinguish them, both are marked ambiguous and neither is auto-selected.
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

1. **Given** a Driver Operations observation is available, **When** an authorized Controller reviews it, **Then** the observation remains owned by Driver Operations, its stable ID may be referenced in the Controller's official-update evidence, and official state changes only through the Controller's separate authorized publication.
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
- A terminal status is corrected; require an authorized, linked correction publication and retain the prior event. Ordinary updates cannot reopen a terminal journey.
- An update is based on a stale journey revision or races another Controller update; reject the stale write without partial state change so the caller can refresh and retry.
- A Driver observation is missing, duplicated, stale, or unverifiable; it must not change official journey status.
- A linked incident affects multiple journeys or is later resolved; preserve Incident Management ownership and do not infer journey status from incident lifecycle alone.
- A downstream notification, client refresh, or dependent module is unavailable; retain the official publication and make delivery/read condition distinguishable without fabricating success.
- A Controller publishes a route diversion through the approved status-publication operation; update only the dated journey's ordered calling points, preserve Feature 001's reusable scheduled route, retain prior revision evidence, and expose the committed revision consistently through dependent reads.
- Feature 002 role or resource-scope authority is unavailable, stale, or revoked during an operational request; deny the write and leave official state unchanged.

## Requirements

### Approved Requirement Traceability

The following source wording and acceptance checks are transcribed from the approved `FINAL UNIFIED REQUIREMENTS` workbook. All listed requirements are **Must Have**. “Feature owner” distinguishes direct Feature 003 delivery from requirements whose implementation belongs to a dependent module or client; those downstream requirements remain open until their owning feature supplies evidence.

| ID | Approved requirement wording | Approved acceptance check | Feature 003 relationship |
|---|---|---|---|
| SR-007 | An operational journey instance shall be distinguishable from its reusable scheduled service and shall carry its own current progress and status. | Two departures of the same scheduled service can have distinct IDs and statuses. | Direct — authoritative dated journey identity and independent state; the approved identifier decision is recorded below. |
| SR-010 | Published journey and disruption status shall originate from authorized railway sources and be shared consistently with all dependent applications. | Same service/event revision is visible across subscribed UIs, including source and updated time. | Direct — authoritative publication and consistent revision. |
| SR-019 | The passenger app shall display an active journey status such as scheduled, running, delayed, interrupted, cancelled or completed using authoritative data. | Status labels reflect published journey-state transitions. | Direct backend data; passenger rendering is a client obligation. |
| SR-021 | Passengers shall be able to view estimated arrival times where available and an appropriate unavailable indicator when predictions cannot be supported. | ETA shows data freshness and does not invent values when feed is missing. | Direct — ETA availability and freshness data; no prediction fabrication. |
| SR-022 | Passengers shall see relevant delay, cancellation, route, platform and departure-time changes connected to a searched or selected service. | Publishing a disruption updates related service-detail presentation. | Direct publication/read model; the approved route-diversion schema is recorded below. |
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

- **FR-001**: The system MUST represent each operational journey as a dated instance distinct from its reusable Feature 001 scheduled service, with its own stable, opaque `journeyInstanceId`, progress, and official status. The identifier MUST be issued by an authoritative journey-instance source and MUST NOT be derived from the planned-occurrence composite. The schedule linkage MUST retain the scheduled service ID, published schedule revision, operation date, and that revision's scheduled departure occurrence at the first calling point. Multiple journey instances MAY share that same composite and MUST remain distinct. With no external railway feed, a controlled, non-public initialization manifest and restricted internal command MAY issue the identifier only under an explicitly authorized railway maintenance process; until that authority is approved or verifiable, initialization MUST fail closed. Initialization evidence MUST record the verified actor, source/provenance, linkage, timestamp, and audit evidence. Reprocessing the same source instance key with identical linkage MUST be idempotent. Reuse of an identifier or source key with conflicting linkage MUST be rejected and audited. Missing or unverifiable authoritative identity MUST be rejected; the system MUST NOT invent an identifier or merge instances.
- **FR-002**: A journey MUST retain a reference to the authoritative scheduled service and ordered station calling points from Feature 001. Journey operations MUST NOT rewrite reusable service schedules or station reference data.
- **FR-003**: The system MUST keep planned schedule values, estimated values, observed values, and officially published operational values distinguishable. Each displayed estimate or operational update MUST expose its applicable source and freshness/update time.
- **FR-004**: A passenger or anonymous public consumer MUST be able to read approved published journey status and applicable station departure information using the existing public access boundary. Public responses MUST contain only information approved for public visibility.
- **FR-005**: The system MUST support the approved published statuses: scheduled, running, delayed, interrupted, cancelled, and completed, and the approved termination update where applicable. Allowed transitions are: Scheduled to Running, Delayed, Interrupted, or Cancelled; Running, Delayed, and Interrupted may transition among those three active states or to Completed or Terminated. Cancelled means the journey did not commence; Terminated means it ended before its planned final stop; Completed means it reached its planned end. Completed, Cancelled, and Terminated are terminal for ordinary updates. An authorized Controller may correct an erroneous terminal publication only through a new linked, audited correction. Stale-revision updates MUST be rejected for refresh and retry; no additional state or transition may be inferred.
- **FR-006**: Only an authenticated Controller with current Feature 002 authorization for the journey/resource MAY publish official journey status, cancellation, platform, or operational timing changes permitted by the approved RBAC matrix. Station Staff and Driver MUST NOT perform Controller-only actions.
- **FR-007**: Each official update MUST record the verified publishing actor, publication time, affected journey, and ordered revision/evidence sufficient for dependent consumers to identify the same published state. When a Controller uses a Driver observation as evidence, the official update MAY reference that observation by its stable ID; the observation remains owned by Driver Operations and its contents are not duplicated as an Identity/Journeys record. A correction MUST link to the prior publication it corrects, preserve that evidence, and become current only through its own authorized publication. Publication MUST be auditable.
- **FR-008**: A Driver progress report MUST remain an attributed, non-authoritative observation owned by Driver Operations. A Controller MAY consume it through the existing authorized observation read and reference its stable observation ID in a separately authorized official update. The approved update schema carries this optional reference; the observation MUST NOT directly change a journey's official status, schedule, or signaling state. Do not add a review endpoint.
- **FR-009**: The system MUST expose an ETA only when supported by an authoritative prediction; otherwise it MUST communicate unavailable, unknown, or last-known status and freshness. It MUST NOT guess a location or ETA.
- **FR-010**: Published delays, cancellations, platform, departure-time, and route changes MUST be associated with the affected journey/service so dependent passenger and station views can present the correct change. An approved route diversion is journey-specific: the existing status-publication operation carries a complete replacement ordered calling-point set under one revision, retains prior revision evidence, and MUST NOT alter Feature 001's reusable scheduled route. Dependent reads expose that committed revision without mixing calling-point versions. The existing operation inventory is preserved.
- **FR-011**: A single approved update MUST be identifiable consistently across permitted passenger, station, driver, and operational consumers by its revision and source/update time. No separate communications-officer approval workflow is introduced.
- **FR-012**: Station departure information MUST relate the station’s upcoming scheduled departures to the latest known official journey status and update time. Station filtering and automatic refresh/receive behavior are client responsibilities, supported by the approved status data.
- **FR-013**: Significant approved changes MUST support the configured in-app alert behavior. Native device push is not required by the approved minimum implementation. No numeric “prompt” latency or polling target is introduced because none is approved.
- **FR-014**: Journey reads and official changes MUST respect Feature 002 authentication, RBAC, current resource-scope and ownership decisions. Missing, stale, revoked, or unverifiable authorization evidence MUST NOT authorize a protected operation. This feature MUST NOT change the approved grants.
- **FR-015**: The feature MUST interoperate with Incident Management’s affected-journey context without owning incident creation or lifecycle, and with Driver Operations’ assignments/observations without owning those records.
- **FR-016**: The feature MUST distinguish a valid journey with no additional updates from missing journey data, incomplete/unavailable authoritative publication data, invalid input, stale-revision conflict, and denied access. Failure responses MUST not expose credentials, personal data, or internal exception details.
- **FR-017**: If an external operational feed is absent or unreliable, an authorized Controller MUST be able to publish a supported manual update with actor and timestamp; absent feeds MUST NOT cause fabricated status, position, or ETA.
- **FR-018**: Feature 003 MUST remain within the approved Journeys & Status operation inventory: journey detail read, journey updates read, station departures read, Controller journey-status publication, and Controller stop/platform/timing publication. It MUST NOT add an unapproved route or review/approval operation. The approved schemas assign complete journey-specific route diversions to the existing status-publication operation and stop-specific platform/timing changes to the existing stop operation. Driver observations remain owned by Driver Operations and cannot themselves authorize or substitute for official publication; an optional stable observation ID may be linked as evidence in an official update after verification through the existing authorized read.
- **FR-019**: The feature MUST NOT implement incident lifecycle, driver assignment/observation ownership, independent client presentation states, or train control/signaling functions.

### Key Entities

- **Dated Journey**: An operational instance with an authoritative opaque `journeyInstanceId`, linked to a scheduled service, the published schedule revision used at instantiation, the operation date, and the scheduled departure occurrence at the first calling point. Multiple instances may share this planned-occurrence composite and retain distinct IDs and official progress/status.
- **Journey Stop**: An ordered realized calling point for a journey, linked to a Feature 001 station and schedule reference. It can carry distinct planned, estimated, observed, and officially published platform/time values without changing the reusable schedule.
- **Official Journey Update**: An immutable or historically retained publication event for an authorized status, route-diversion, or operational change, carrying actor, time, revision, source, and affected journey/stop context. A route diversion changes only the dated journey's ordered calling points, not its reusable scheduled route.
- **Operational Observation**: A non-authoritative Driver Operations record associated with an assigned journey and attributed driver; owned and managed by Driver Operations. An official journey update may hold a stable-ID reference as evidence without copying or taking ownership of the observation.
- **Incident Impact Association**: A reference from Incident Management to one or more affected journeys/disruptions; ownership and lifecycle remain with Incident Management.
- **Publication Source/Revision**: Evidence identifying the authoritative origin and sequence/version of an update, used to keep dependent reads consistent and freshness visible.

### API and Access Boundary

The approved RBAC/API baseline permits Journeys & Status reads for journey detail, journey updates, station departures, and operations observations, plus Controller writes for journey status and journey stop status/platform/timing. Gate 2 defines the approved version 1.0.0 schema semantics for the five Journeys & Status operations without adding a path or grant. Public consumers may read only approved published status and station departure data. Official writes require the current Controller grant and resource authorization from Feature 002. Driver observation access and ownership remain with Driver Operations; the approved baseline provides Controller observation read access but no separate observation-review action.

## Success Criteria

### Measurable Outcomes

- **SC-001**: In tests with two journey instances for the same scheduled service, published schedule revision, operation date, and first-calling-point departure occurrence but distinct authoritative `journeyInstanceId` values, each instance retains its own independently published status without cross-journey leakage.
- **SC-011**: Reprocessing the same authoritative source instance key with identical schedule linkage is idempotent; reusing an ID or source key with conflicting linkage is rejected and audited; two distinct authoritative IDs sharing the same planned-occurrence composite remain separately addressable. Missing or unverifiable source identity is rejected without creating or merging a journey.
- **SC-012**: Tests accept every approved status transition and reject transitions outside the approved lifecycle; stale-revision writes leave state unchanged, and terminal corrections append linked audit evidence.
- **SC-013**: A published route diversion changes only the dated journey's ordered calling points; the reusable scheduled route and prior journey revision remain intact, and all dependent reads return the same new revision.
- **SC-014**: When a Controller uses a Driver observation, the official update may reference its stable observation ID; the observation remains owned by Driver Operations and does not itself change official status.
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

1. **Historical directional clarification (approved)**: Multiple journey instances may refer to the same scheduled service and operation date and must have distinct identities/status.
   **Follow-up identity clarification (approved)**: Use the scheduled service ID, published schedule revision, operation date, and that revision's scheduled departure occurrence at the first calling point as the instance key. Repeated processing of the same key is duplicate creation and must be idempotent/rejected. If distinct departures share an indistinguishable occurrence, require an authoritative occurrence discriminator before representing both. Passenger-facing departure results must let the passenger choose the intended occurrence and resolve to its journey identity without adding an operation. A journey remains linked to the schedule revision used for its instance; later schedule revisions do not silently re-key or rewrite that journey.
2. **Historical directional clarification (approved)**: Define explicit transitions and audited corrections.
   **Follow-up lifecycle clarification (approved)**: Scheduled may transition to Running, Delayed, Interrupted, or Cancelled. Running, Delayed, and Interrupted may transition among those states or to Completed or Terminated. Cancelled means not commenced; Terminated means ended before the planned final stop; Completed means reached the planned end. Completed, Cancelled, and Terminated are terminal for ordinary updates. An authorized Controller may correct an erroneous terminal update through a new linked, audited publication; prior evidence is retained. A stale-revision update is rejected without changing state, and the caller must refresh before retrying.
3. **Historical directional clarification (approved)**: Keep the existing operation inventory and do not add a route-change or Driver-observation review operation.
   **Follow-up SR-022 clarification (approved; schema question resolved by Gate 2)**: A route diversion is a Controller-published change to the dated journey's ordered calling points; it does not change the reusable Feature 001 route/schedule. Journey detail, update history, and station-departure reads must identify the same new journey revision and preserve the prior revision. The existing operation inventory remains fixed. At the time, formal approval of existing operation schemas/semantics was required if they could not represent the diversion; Gate 2 below now records that approval and the selected schema.
   **Follow-up Driver-observation clarification (approved; schema question resolved by Gate 2)**: The Controller may read an observation through the existing authorized read and reference its stable observation ID in the existing official journey-update evidence. Driver Operations retains ownership; only a separate authorized Controller publication changes official status. At the time, schema approval was required if the reference could not be expressed; Gate 2 below now records that approval. Do not add a review mutation or copy observation ownership into Journeys & Status.

## Pre-planning Readiness Review

This review compares the approved sources available on 2026-10-18. The Gate 1 identity decision and Gate 2 schema/semantics decision below are approved. Neither changes the MoSCoW baseline, RBAC grants, or API operation inventory.

### Gate 1 — Journey Identity: APPROVED

The approved composite rule remains `scheduled service + published schedule revision + operation date + first-calling-point scheduled departure occurrence`. Feature 001 stores a schedule revision and planned call times; its `ServiceChoice` response exposes `serviceId`, `scheduleRevision`, selected origin/destination calls and scheduled times. It does not expose a physical journey-instance identifier. The approved sources do not constrain two separate operational instances from referring to the same planned occurrence. Therefore, distinct real departures can collide on the approved composite (for example, two operational instances of one scheduled departure); the composite identifies the planned occurrence, not necessarily the realized instance.

**Approved decision:** retain the approved composite as schedule linkage and search context, and add a distinct opaque `journeyInstanceId` issued by an authoritative journey-instance source. Do not derive it from the composite or generate replacement evidence in Journeys & Status. When no external feed exists, controlled initialization through a non-public manifest and restricted internal command is permitted only under an explicitly approved railway maintenance authority; absent or unverifiable authority denies initialization. Record the verified actor, source/provenance, schedule linkage, operation date, occurrence, timestamp, and audit evidence. This does not add a public operation or RBAC grant.

Approved duplicate and passenger-selection behavior:

- Same `journeyInstanceId` and source instance key with identical service/revision/date/occurrence linkage is an idempotent repeat; do not create another journey.
- Reuse of an ID or source instance key with conflicting linkage is rejected and audited.
- Different authoritative IDs with the same approved composite remain distinct journeys; never merge them.
- Missing or unverifiable instance evidence is rejected without creating a journey; never invent an ID or silently choose one.
- The existing station-departures read must identify each instance by its `journeyInstanceId` and provide sufficient public-safe context to select it. If available published context cannot distinguish colliding instances, the response must expose the ambiguity and must not auto-select one. The selected identity opens the existing journey-detail read. Feature 001's service-search contract remains unchanged.

The approved operation inventory has no journey-creation operation. The approved initialization/issuance process is restricted and non-public; its operator and governance mapping remain an operational approval gate. Gate 2 defines the existing station-departures response's safe selection and ambiguity representation.

### Gate 2 — Approved API Contracts: APPROVED

The approved RBAC/API baseline lists these five Journeys & Status operations and does not provide payload schemas: `GET /api/journeys/{id}`, `GET /api/journeys/{id}/updates`, `GET /api/stations/{id}/departures`, `PATCH /api/operations/journeys/{id}/status`, and `PATCH /api/operations/journeys/{id}/stops/{stopId}`. The following minimal schema and semantic extension was explicitly approved on 2026-10-18. It versions the contract document as OpenAPI `info.version: 1.0.0`; it does not add a `/v1` path prefix, operation, or RBAC grant.

#### Shared journey identity and snapshot

`JourneySnapshot` and station-departure entries identify a journey with the authoritative opaque `journeyId` (`journeyInstanceId`), `serviceId`, the published schedule revision used at initialization, `operationDate`, and the first-calling-point scheduled departure occurrence. Each snapshot includes `journeyRevision`, current official status, ordered realized calling points, and applicable publication source/time. The identity composite remains schedule linkage; it is not a uniqueness key. Realized calling points carry stable journey-stop IDs, station IDs, and ordering; planned values remain distinguishable from officially published platform/timing values. Missing values are not inferred.

`GET /api/journeys/{id}` returns one internally consistent current snapshot. `GET /api/stations/{id}/departures` returns each applicable journey ID with the same snapshot identity/revision fields and passenger-safe departure context. A departure entry exposes a selection state and public label when available. If two instances cannot be distinguished by approved public context, each remains represented and is marked ambiguous; clients must not auto-select either instance. `GET /api/journeys/{id}/updates` returns immutable update events. Following a publication, each read reflects that committed revision unless a later publication has committed; each response identifies its revision and source/time, and no response may mix calling-point versions.

#### Publication and audit schemas

`PATCH /api/operations/journeys/{id}/status` requires `expectedJourneyRevision` and carries either an approved status change or a route-diversion change, or both in one publication. A diversion supplies the complete replacement ordered calling-point set for that dated journey. This is the operation that publishes a diversion because a single-stop patch cannot express a complete route replacement. It does not mutate Feature 001's reusable service, route, or schedule.

`PATCH /api/operations/journeys/{id}/stops/{stopId}` requires `expectedJourneyRevision` and carries only platform and/or operational timing changes for the identified journey stop. Both PATCH request schemas may include an optional opaque `driverObservationId` and `correctionOfUpdateId`. The observation is verified through Driver Operations' existing authorized read; its record remains owned by Driver Operations and is not copied. A correction appends a new event linked to the prior update and preserves that event.

The server derives the Controller actor from the validated identity and records the verified subject, publication time, source, affected change, update ID, current and previous journey revision, optional observation reference, and optional correction link in append-only audit evidence. Clients cannot supply or override the actor or publication timestamp. Public read projections expose the update ID, revision, source, publication time, and public change details, but not the Controller's raw subject or protected evidence.

Each successful write advances the journey revision atomically with its audit event and changed state. A stale `expectedJourneyRevision` returns RFC 9457 Problem Details with HTTP `409 Conflict` and makes no partial change; callers refresh before retrying. Existing authentication and authorization outcomes remain those defined by Feature 002; malformed requests, missing resources, and unavailable authorization/source evidence retain safe 400, 404, and 503 outcomes respectively. No new endpoint or grant is introduced.

#### Requirement coverage and preserved approvals

- **SR-007**: authoritative journey ID is distinct from its schedule-linkage composite and appears in journey and departure reads.
- **SR-022**: the existing status PATCH publishes a complete ordered diversion for one dated journey; reads expose its committed revision without rewriting Feature 001 schedules.
- **SR-040**: Controller identity/time and change evidence are server-recorded; expected revisions prevent stale or concurrent writes from silently overwriting state.
- **SR-048**: journey detail, update history, and station departures identify the committed revision and source/time; each response is internally consistent.
- **SR-069**: an optional stable observation ID may be linked to an official update after the existing authorized read; Driver Operations retains ownership and observations remain non-authoritative.

This approval resolves the API-schema gate only. It does not change the approved RBAC matrix or operation inventory, and it does not close separate client/other-module acceptance evidence or the operational approval for journey initialization authority.

## Assumptions

- The approved MoSCoW workbook controls requirement IDs, exact wording, priority, and acceptance checks; the RBAC/API PDF controls existing grants and operation inventory.
- The approved 53 Must Have requirements define MVP scope. This specification does not change the priority or wording of any baseline requirement.
- Feature 001 remains authoritative for stations, routes, scheduled services, ordered stops, schedule revisions, and published service-date completeness. Dated journeys are operational instances and do not replace scheduled services.
- Feature 001’s approved overnight-search interpretation remains: travel date means scheduled departure date from the selected origin. A journey’s operation date follows the published schedule’s date anchor; arrival after midnight does not create another journey. Multiple departures for the same service/date are permitted when their published first-calling-point departure occurrences distinguish them. A journey retains the schedule revision used to instantiate it; a later revision does not silently change its identity or planned baseline.
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
