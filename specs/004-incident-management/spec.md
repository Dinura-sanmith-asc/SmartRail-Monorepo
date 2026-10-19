# Feature Specification: Incident Management

**Feature Branch**: `feature-04-incident-management`  
**Created**: 2026-10-19  
**Status**: Draft — source scope is traced; business and contract clarifications remain before planning  
**Input**: Approved SmartRail MVP requirements, RBAC/API baseline, and accepted Feature 002/003 ownership decisions

## User Scenarios & Testing

### User Story 1 — Report an incident (Priority: P1)

An authorized station or operations user reports a railway incident with its location, affected service or station, description, category, and timestamp. SmartRail records the report and links it to the relevant railway reference. The record identifies its reporting actor through the authenticated identity.

**Why this priority**: Incident reporting is the entry point for the approved incident workflow (SR-041).

**Independent Test**: Submit a valid report as an authorized station user within permitted scope and as a Controller using the approved incident grants. Verify that the report is retrievable and linked to the valid railway reference. Repeat with missing fields, invalid references, an unauthorized role, and an out-of-scope station; verify no incident is created for rejected requests.

**Acceptance Scenarios**:

1. **Given** an authenticated user with the applicable incident-creation grant and required scope, **When** they submit a complete incident report with valid railway references, **Then** the incident is saved, assigned an identifier, attributed to the authenticated actor, timestamped, and linked to the relevant railway entity. (SR-041; SR-091)
2. **Given** required report information is missing or invalid, **When** the report is submitted, **Then** the request is rejected with a useful error and no incident is created.
3. **Given** the caller lacks a grant, has been revoked, or cannot establish the required station scope, **When** they submit a report, **Then** access is denied or reported unavailable according to Feature 002 and no state changes.
4. **Given** a station, service, or journey reference is unknown, retired, inaccessible, or cannot be verified, **When** it is included in a report, **Then** SmartRail does not fabricate or silently accept the relationship; its final response and whether the report may be saved without that association require contract clarification.

---

### User Story 2 — View and track authorized incidents (Priority: P1)

Authorized personnel view incidents within their permitted scope and track their recorded lifecycle state and relevant impact context. Station Staff see only incident resources allowed by their current station scope. Operations clients may combine incident context with official Feature 003 journey information through the approved ownership boundary.

**Why this priority**: SR-038 requires authorized staff to see important incidents in an operations overview, and SR-044 requires impact to be visible on affected journey views.

**Independent Test**: Create controlled incidents linked to stations, services, and journeys. Read them as an in-scope Station Staff member, an out-of-scope Station Staff member, a Controller with approved incident access, and an unauthorized identity. Verify the response contains only authorized incident context. In the integrated operations workflow, compose that context with Feature 003 official journey data and verify no private incident fields appear in anonymous journey responses.

**Acceptance Scenarios**:

1. **Given** an authenticated, authorized user requests incidents within their allowed scope, **When** incident information is available, **Then** the user receives the permitted incident information and relevant linked railway references.
2. **Given** a Station Staff user requests an incident outside their current station scope, **When** access is evaluated, **Then** the protected information is denied without revealing sensitive existence details.
3. **Given** role, scope, or owner evidence is missing or unavailable, **When** a protected incident read is requested, **Then** access is not granted and the request fails safely.
4. **Given** an operations client displays an affected journey, **When** it combines authorized incident context with official Feature 003 journey data, **Then** the incident impact is visible to the authorized operations user while the public JourneySnapshot remains free of private incident data. Full SR-044 acceptance remains open until Incident Management and the operations client are implemented and tested.

---

### User Story 3 — Track an incident through its lifecycle (Priority: P1)

Authorized personnel update an incident through the approved progression from reported, through operational handling, to resolved or closed. Accepted lifecycle changes are auditable. Invalid or unauthorized changes do not alter the incident.

**Why this priority**: SR-042 directly requires a lifecycle, auditable valid transitions, and a dashboard update when an incident is closed.

**Independent Test**: Exercise the approved transition model after it is clarified. Verify permitted updates and audit evidence, reject each prohibited transition, and attempt concurrent updates from the same prior state. Confirm the operations dashboard reflects closure and denied or conflicting changes leave the record unchanged.

**Acceptance Scenarios**:

1. **Given** an incident in a state with an approved transition, **When** an authorized user makes that transition, **Then** the new state is recorded with sufficient audit evidence to verify the valid transition.
2. **Given** a requested transition is not approved for the incident's current state or actor, **When** it is submitted, **Then** it is rejected and the current state is unchanged.
3. **Given** an incident is closed, **When** the operations overview is refreshed, **Then** the overview reflects its closed state.
4. **Given** two authorized updates race against the same incident state, **When** one update has already changed the record, **Then** the other cannot silently overwrite it; conflict handling and correction behavior are subject to clarification.
5. **Given** an incident is created, updated, resolved, or closed, **When** the change is processed, **Then** Feature 003 journey status, cancellation, and official revisions are not changed automatically.

---

### User Story 4 — Associate an incident with affected railway resources (Priority: P1)

Authorized personnel associate an incident with its relevant station, service, and, where known, one or more active dated journeys and published service disruptions. Incident Management owns these associations. Feature 001 remains authoritative for railway reference data; Feature 003 remains authoritative for dated journeys and official journey updates.

**Why this priority**: SR-044 explicitly requires one incident to be associable with more than one active journey and its impact to appear on affected journey views.

**Independent Test**: Create an incident and associate it with two active journey instances and valid station/service references. Read the incident through authorized incident operations and verify both journey links remain distinct. Verify missing, stale, inaccessible, and unavailable journey-owner evidence never creates an unverified association. Confirm incident processing does not publish a journey update.

**Acceptance Scenarios**:

1. **Given** an incident has verified relationships to multiple active journeys, **When** an authorized user reads the incident impact, **Then** all linked journeys are represented separately and the incident impact is available to the authorized operations view. (SR-044)
2. **Given** a reference does not exist or is no longer active, **When** a user attempts to associate it, **Then** SmartRail rejects or safely reports the invalid relationship without substituting another resource.
3. **Given** the authoritative journey source is unavailable, **When** the requested association requires journey verification, **Then** the association is not granted or persisted as verified; no fake production provider or fallback relationship is used.
4. **Given** an incident is linked to a journey or published disruption, **When** the incident is created, changed, resolved, or closed, **Then** the official journey status and disruption publication remain unchanged unless a separately authorized Feature 003 operation is performed.

---

### User Story 5 — Compose incident context with official journey status (Priority: P1)

An operations client presents authorized Incident Management context alongside Feature 003's official journey data so railway staff can understand which incidents affect a journey and what status the railway has officially published. Incident Management does not become the source of official journey status, and Feature 003 does not own incident records.

**Why this priority**: SR-038 and SR-044 require an authorized operational overview and incident impact on affected journey views. The approved Feature 003 decision assigns composition to the operations client.

**Independent Test**: In an integrated environment with the Incident Management owner and the operations client, associate an incident to a journey, publish a separate official journey update, and verify the client shows both sources with their own timestamps/revisions and access rules. Resolve the incident and verify the client updates incident context without inventing a journey-status transition.

**Acceptance Scenarios**:

1. **Given** an authorized operations client requests current incident context and official journey status, **When** both owners are available, **Then** it presents incident impact and the authoritative Feature 003 status as distinct information.
2. **Given** Incident Management or Feature 003 information is unavailable, **When** the operations client refreshes, **Then** it identifies the unavailable source and does not display stale information as guaranteed current.
3. **Given** an anonymous passenger reads a public Feature 003 journey, **When** a private incident affects that journey, **Then** private incident records are not exposed in the public journey response.

## Edge Cases

- A report omits a required SR-041 field, includes malformed or executable markup, or exceeds an agreed input limit. Exact field limits and validation schemas are not specified by the source baseline and must not be invented as business thresholds.
- A station or service identifier is malformed, unknown, retired, or unavailable from Feature 001. The report must not silently link a different station or service.
- A journey identifier is malformed, unknown, no longer active, inaccessible, or unverifiable because Feature 003's owning source is unavailable. Unavailable evidence cannot authorize a relationship.
- One incident affects multiple active journeys. All associations remain distinct; a change to one link must not silently replace another.
- A reported incident is submitted twice because of a client retry or uncertain response. The approved baseline does not define a deduplication key or idempotency behavior; duplicate handling remains open for contract clarification.
- A caller's Cognito identity is valid but their role mapping is empty, revoked, or unavailable; or their Station Staff scope has changed. Apply Feature 002's current authorization evidence and fail closed.
- A Controller role has an incident action in the approved matrix without an explicit `scoped` annotation. Do not infer incident scope from Feature 003's Controller journey-scope decision; the incident-specific scope rule remains open.
- An update uses a lifecycle transition not supported by the approved model, attempts to reopen a terminal state, or races with another update. Reject invalid or stale updates without losing history; exact transitions, correction links, and conflict contract remain open.
- A journey changes state or a published disruption is updated while an incident association is being created. The two modules remain separately authoritative; no cross-module write or implicit status publication occurs.
- An incident is resolved or closed while an official journey remains delayed, interrupted, cancelled, or otherwise active. Incident closure never implies journey recovery, restoration, or reopening.
- Incident or railway-owner data is unavailable during a read. Return a safe unavailable state rather than a false empty list or fabricated current state.
- A sensitive actor identifier, private incident detail, raw exception, credential, or token could enter a public response, user-visible error, or unrestricted log. It must be excluded or safely redacted.
- A catering-provider outage occurs. Core incident reporting, reads, and lifecycle processing remain available (SR-113).
- The operations client loses connectivity or receives authorization denial. It shows the applicable loading, no-data, access-denied, error, retry, network, and freshness states without presenting old incident context as current (SR-109; SR-114).

## Requirements

### Approved requirement traceability

The wording, priority, approval disposition, and acceptance checks below are transcribed from `FINAL UNIFIED REQUIREMENTS` in the approved workbook. Direct incident requirements are separated from cross-feature and client obligations. `Must Have` behavior is MVP scope. Should Have and Could Have rows are retained as boundaries and are not promoted into the MVP.

| ID | Priority / disposition | Exact approved requirement wording | Exact approved acceptance check | Feature 004 responsibility and related ownership |
|---|---|---|---|---|
| SR-001 | Must Have | SmartRail shall provide an integrated passenger, railway operations, driver/operator, and station-information experience backed by shared services rather than isolated, inconsistent applications. | A journey update is retrievable through the appropriate passenger, operations, driver and station channels. | Incident Management supplies its owned incident context. Operations and other clients compose it with owner data; integration acceptance is cross-module. |
| SR-010 | Must Have | Published journey and disruption status shall originate from authorized railway sources and be shared consistently with all dependent applications. | Same service/event revision is visible across subscribed UIs, including source and updated time. | Feature 003 owns official journey/disruption publication. Feature 004 records impact links and never publishes or changes official journey state. |
| SR-012 | Should Have / approved stretch only | Record the user/source, event time and previous/current value for material journey, incident and station information changes. | Audit history shows source, actor and timestamp for tested status changes. | The Must Have SR-042 minimum requires auditable lifecycle transitions. The broader material-change audit requirement remains stretch scope unless separately approved. |
| SR-038 | Must Have | Authorized railway staff shall access a web-based overview of current services, journey states and important incidents. | Role-authorized login can view live operations summary; unauthorized users cannot. | Existing `GET /api/operations/overview` belongs to Operations. Existing incident reads supply authorized context. Operations-client implementation and combined-view evidence remain necessary. |
| SR-041 | Must Have | Authorized operations or station users shall be able to record an incident with location, affected service/station, description, category and timestamp. | Incident is saved and linked to the relevant railway entity. | Feature 004 owns incident creation and its railway references. Feature 001 owns station/service identifiers. Feature 002 owns authentication, role mapping and resource scope. |
| SR-042 | Must Have | Incident records shall support a simple lifecycle from reported through operational handling to resolved or closed. | Valid state transitions are auditable; closing a record updates dashboard. | Feature 004 owns lifecycle and evidence for accepted transitions. Operations owns the overview presentation. Exact transition graph needs clarification. |
| SR-043 | Could Have / deferred enhancement | Incident entries shall include a simple severity classification to inform staff urgency and prioritization. | Severity visible in dashboard and can be changed only by authorized staff. | Not part of this MVP specification; no severity taxonomy or priority is introduced. |
| SR-044 | Must Have | An operational incident shall be associable with affected active journeys and published service disruptions where known. | One incident can affect more than one journey and its impact appears on affected journey views. | Feature 004 owns associations and authorized incident-context reads. Feature 003 owns journeys and published disruptions. The operations client composes context with official journey data; full acceptance remains open until owner-module and client evidence exists. |
| SR-045 | Should Have / approved stretch only | Operational users shall receive relevant in-app alerts or updated dashboard indicators for incidents and material changes within their responsibility. | Creation of a serious incident updates role-specific dashboard alerts. | Notifications and alert policy are not promoted into Must Have scope. |
| SR-046 | Must Have | Only roles authorized under railway policy may confirm a train cancellation; the app shall not independently decide to cancel a service. | Station and driver accounts cannot execute controller-only cancellation API. | Feature 003 owns cancellation publication and its separate grant. Incident create/update/resolve/close cannot confirm cancellation. |
| SR-047 | Should Have / approved stretch only | Authorized staff shall correct incorrect service/incident data without erasing the original update or presenting algorithmic predictions as absolute authority. | Correction stores prior value, author, time and reason. | A dedicated correction workflow is not promoted to Must scope. Any minimum lifecycle evidence remains subject to clarification. |
| SR-048 | Must Have | SmartRail shall propagate approved operational status changes to passenger and station channels without adding an unapproved separate communications-officer approval workflow. | One approved service change appears on multiple channels from same revision. | Feature 003 owns status publication. Incident context remains separately owned and does not trigger a journey update. |
| SR-049 | Should Have / approved stretch only | Station staff shall be able to report local platform safety concerns to Control through existing incident/communication pathways, without acquiring new train-routing authority. | Safety incident is visible centrally and traced to the reporting station. | Station Staff creation uses the existing scoped grant. A specialized safety workflow and client evidence remain stretch obligations. |
| SR-050 | Should Have / approved stretch only | Station staff shall report equipment outages, overcrowding and unplanned local issues through the shared incident-management workflow, rather than a separate maintenance module. | Staff can file and close local incidents through shared interface. | This is a downstream shared-client obligation beyond direct Must Have backend contract; do not create a maintenance module. |
| SR-051 | Should Have / approved stretch only | Log important incident actions including creation, changes, owner/actor, timestamps and closure history. | Reviewing a resolved incident exposes complete audit chronology. | Broader complete audit chronology remains stretch scope. Feature 004 still records evidence sufficient for the Must Have SR-042 auditable-transition check. |
| SR-053 | Could Have / deferred enhancement | The platform shall record railway incident information useful to existing external responders without creating mandatory emergency-service or third-party engineer user portals. | Incident can be reported/referenced; no external responder login required. | No external-responder portal or workflow is introduced. |
| SR-089 | Must Have | Public railway information shall be distinguishable from functions requiring passenger or railway-staff authentication. | Anonymous viewer can access permitted service details but not protected operational APIs. | Incident records and operational reads remain protected under Feature 002; public Feature 003 responses expose no private incident data. |
| SR-090 | Must Have | Operational web and driver applications shall authenticate their users before exposing protected railway information or write actions. | Protected endpoint rejects missing/invalid token and respects session expiry. | Feature 002 validates the user. Feature 004 requires authenticated identity for protected incident actions. |
| SR-091 | Must Have | Server-side authorization shall restrict operational changes based on organizational role and the target station/journey/resource; front-end hiding alone is insufficient. | Station credentials cannot call controller-only cancellation endpoint; role/resource tests pass. | Feature 004 uses current Feature 002 role and scope evidence. Station Staff incident scope follows current station assignment. Controller incident scope remains open and is not inferred from Feature 003. |
| SR-095 | Must Have | Operational and local station messages shall be displayed as data with appropriate output encoding and input validation, preventing script execution on displays or apps. | Injected markup is escaped and cannot execute JavaScript in station views. | Feature 004 validates incident text as data; owning web/station clients must render it safely and test injection cases. |
| SR-100 | Must Have | Sensitive personal information and credentials shall not be exposed in verbose API errors or unrestricted application logs. | Negative tests show tokens/redacted identifiers absent from API error responses. | Feature 004 errors and logs protect actor, incident, and credential information; integration security evidence is required. |
| SR-109 | Must Have | Passenger, driver, and station clients shall provide clear network-availability and data-freshness indications when live information cannot be refreshed. | Offline simulator shows visible state/last updated time for each applicable client. | Incident/operations clients must show availability and freshness for incident context. Their owning client specifications and tests remain open. |
| SR-113 | Must Have | Failure of an external catering API shall not prevent core train search, operations incident management or passenger service-status access. | Simulated catering outage leaves core railway features available. | Feature 004 remains independent of Catering Adapter availability; verify incident workflows during simulated catering outage. |
| SR-114 | Must Have | The system shall provide meaningful loading, no-data, access-denied, error and retry states for primary passenger and operational workflows. | Each primary view has tested no-results, 403, 500 and no-network cases. | Feature 004 supplies safe backend outcomes; operations/client specifications own visual states and automated workflow evidence. |
| SR-115 | Must Have | The platform shall expose operational health/error information appropriate to diagnosing API, database and integration issues without disclosing secrets. | System health check and structured error logs report representative service failures. | Feature 004 failures must be diagnosable through approved health/logging boundaries without exposing sensitive content. |
| SR-117 | Must Have | The chosen backend and application prototypes shall run in a repeatable documented environment with their dependent services and external catering simulation. | A clean start with documented steps launches core flows successfully. | Feature 004 participates in local-first repeatable verification; no AWS provisioning is authorized by this specification. |

### Functional Requirements

- **FR-001 (SR-041)**: SmartRail MUST allow an authenticated user with the approved incident-creation grant and required resource scope to record an incident containing the approved location, affected service or station, description, category, and timestamp. No additional mandatory business field is introduced here.
- **FR-002 (SR-041)**: A successfully recorded incident MUST have a stable identifier, retain its verified railway references, and be attributable to the authenticated reporting identity. Identifier format and report-time semantics are not prescribed.
- **FR-003 (SR-041; SR-091)**: Incident creation MUST use Feature 002's current server-side role and applicable resource-scope decisions. Station Staff incident actions MUST respect current station scope. Missing, revoked, insufficient, or unavailable authorization evidence MUST NOT grant access or change state.
- **FR-004 (SR-038; SR-044)**: Authorized users MUST be able to retrieve incident context only within their approved access. Feature 004 owns incident reads and associations; the operations client composes that context with Feature 003 official journey data.
- **FR-005 (SR-042)**: Incident records MUST represent the approved lifecycle from reported through operational handling to resolved or closed. Only approved transitions may be accepted; every accepted transition MUST have enough evidence to verify that it was valid. The exact state vocabulary and transition graph remain unresolved.
- **FR-006 (SR-042)**: Closing an incident MUST cause the authorized operations overview to reflect its closed state. The operations overview remains the responsibility of the approved Operations operation and client composition boundary.
- **FR-007 (SR-044)**: An incident MUST be associable with one or more active dated journeys when known and with relevant station/service references. A single incident MUST support at least two distinct journey associations. Feature 003 is authoritative for journey identity and official disruptions.
- **FR-008 (SR-044; SR-010; SR-048)**: An incident association or lifecycle change MUST NOT automatically create, revise, cancel, resolve, or otherwise publish Feature 003 journey status. Official journey changes remain separate authorized Feature 003 operations.
- **FR-009 (SR-044)**: Incident impact MUST be available to an authorized operations view together with official journey status through client composition. Public Feature 003 journey, update, and departure responses MUST NOT include private incident records. Full SR-044 acceptance remains open until Incident Management and the operations client are verified together.
- **FR-010 (SR-046)**: Incident creation, editing, resolution, and closure MUST NOT substitute for the separate Feature 003 cancellation operation or its separate approved authorization grant.
- **FR-011 (SR-089; SR-090; SR-091)**: All protected incident reads and writes MUST require valid Feature 002 authentication and an explicit approved role/action grant. Resource-scope decisions MUST use current owner evidence; front-end visibility alone MUST NOT authorize an action.
- **FR-012 (SR-100; SR-115)**: Responses and logs MUST exclude access tokens, credentials, raw exceptions, private audit evidence, and unnecessary personal information while preserving safe diagnostic context.
- **FR-013 (SR-095)**: Incident descriptions and other user-supplied text MUST be treated as untrusted data and validated/encoded at applicable boundaries so it cannot execute as markup or script in consuming applications.
- **FR-014 (SR-109; SR-114)**: Backend behavior MUST distinguish invalid input, missing/inaccessible resources, denied access, unavailable authorities, and valid empty incident results. The precise HTTP mappings and collection-empty semantics remain a contract clarification. Owning clients MUST present loading, empty, denial, error, retry, network, and freshness states; overall client acceptance remains open until their specifications and tests exist.
- **FR-015 (SR-113)**: Incident reporting, authorized reads, and lifecycle processing MUST remain available when the independent catering API is unavailable.
- **FR-016 (SR-001; SR-117)**: Incident Management MUST use the shared SmartRail identity and railway references and support repeatable local verification without provisioning AWS resources.

### Key Entities

- **Incident**: A reportable railway event with a stable identifier, approved report fields, current lifecycle state, reporting identity, and relationships to relevant railway resources. The source baseline does not specify identifier format or an additional severity field.
- **Incident impact association**: A relationship owned by Incident Management between an incident and one or more relevant stations, services, active dated journeys, and known published service disruptions. Journey and disruption facts remain authoritative in their owning modules.
- **Incident lifecycle evidence**: Evidence sufficient to verify accepted changes through the Must Have lifecycle. The exact event fields and whether full immutable chronology is required are open; the broader complete audit-history check belongs to Should Have SR-051.
- **Reporting actor**: The verified SmartRail identity that submitted the report or change. Feature 002 owns authentication and active role mapping; client-supplied actor identities are not trusted.

## Success Criteria

### Measurable Outcomes

- **SC-001 (SR-041)**: Every valid incident test report containing the approved fields is retrievable by its stable identifier and linked to the valid railway entity; every invalid or unauthorized test report produces no incident state change.
- **SC-002 (SR-042)**: Every tested valid lifecycle transition follows the approved transition model and has verifiable audit evidence; every tested invalid or unauthorized transition leaves the incident unchanged. The transition model must be approved before this criterion can be finalized.
- **SC-003 (SR-042)**: After an incident is closed, the authorized operations overview shows its closed state in the next verified read.
- **SC-004 (SR-044)**: In acceptance tests, one incident can be associated with at least two distinct active journey IDs, and authorized operations users can see each impact with the correct official journey data. No private incident fields appear in anonymous journey responses. Full acceptance remains open until owner and client evidence exists.
- **SC-005 (SR-091)**: For each protected incident action, role and applicable scope tests demonstrate both an authorized outcome and a denied or unavailable outcome; every denied or unavailable write leaves incident and journey state unchanged.
- **SC-006 (SR-010; SR-046; SR-048)**: Incident create, update, resolve, and close tests produce zero automatic Feature 003 official journey publications; cancellation remains possible only through its separate authorized Feature 003 operation.
- **SC-007 (SR-100; SR-095)**: Security tests confirm incident text cannot execute as markup in supported clients, and response/log checks reveal no credentials, tokens, raw exceptions, or unnecessary private identifiers.
- **SC-008 (SR-109; SR-114)**: Each owning incident/operations client demonstrates loading, no-data, access-denied, error, retry, and no-network/freshness behavior before its overall MVP acceptance is closed.
- **SC-009 (SR-113; SR-117)**: A documented local clean start and a simulated catering outage both leave authorized incident reporting, reading, and lifecycle workflows available.

## Assumptions

- The approved workbook controls requirement wording, priority, approval disposition, and source acceptance checks. The approved RBAC/API PDF controls current role grants and operation inventory.
- The three direct Incident Management Must Haves are SR-041, SR-042, and SR-044. SR-038 is a required operations-client dependency. Other Must Haves in the traceability table are preserved as applicable cross-feature, security, reliability, or client obligations rather than extra Incident Management functionality.
- Feature 001 owns station and scheduled-service reference data. Feature 003 owns dated journey instances, published journey status, cancellation, diversions, official journey revisions, and published disruptions. Feature 004 may reference owner data but does not write it directly.
- Feature 002 owns Cognito identity verification, current active SmartRail role mappings, the existing RBAC action grants, and station-staff station assignments. Incident-specific Controller resource scope is not defined here by borrowing Feature 003's global Controller journey-scope decision.
- Station Staff grants for Create incident, View incidents, and Update incident lifecycle are explicitly scoped in the approved matrix. Controller rows carry those grants without a `scoped` annotation; what resource scope that implies for Incident Management remains open.
- Incident creation or lifecycle changes never automatically publish official journey status, cancellation, or route change. Incident impact and official journey status remain separate facts that the operations client composes.
- A stable incident identifier and authenticated actor attribution are necessary to retrieve and secure incident records. Their external representation and retention details remain planning/contract matters.
- No numeric performance, retention, severity, polling, availability, or input-size thresholds are invented by this specification.
- All development and verification is local-first. AWS resource creation and deployment remain manual user activities after complete local system verification.

## Dependencies and Ownership Boundaries

- **Feature 001 — Railway Network & Train Search**: provides authoritative station and service identifiers. Feature 004 must validate references using its owner boundary and must not duplicate railway reference data.
- **Feature 002 — Identity & Permissions**: authenticates Cognito users and resolves current internal roles and station scope. Feature 004 must fail closed when current authorization evidence is missing or unavailable. No role or grant is added here.
- **Feature 003 — Journeys & Status**: owns journey identity, active journey state, official disruption publications, cancellation, and journey revisions. Feature 004 owns incident-to-journey associations and incident context. It must not update official journey status or place private incident data in public journey snapshots.
- **Operations API/client**: the approved inventory includes `GET /api/operations/overview`; the RBAC matrix grants View operations dashboard as Station Staff `R (scoped)` and Controller `R`. Operations owns the overview composition. It consumes authorized incident context and Feature 003's official journey data. Dashboard, refresh, error, and availability evidence remains a client/integration obligation.
- **Station, passenger, driver, and operations clients**: applicable authentication, safe text rendering, loading, empty, access-denied, error, retry, offline, freshness, and session states must be specified and tested by their owning client features. Feature 004 backend success does not close SR-038, SR-095, SR-109, or SR-114 client acceptance.
- **Catering Adapter**: remains an independent provider boundary. Incident Management must not depend on the catering API for core incident behavior.

## Approved API and RBAC Boundary

The approved PDF lists exactly these Incident Management operations:

| Operation | Approved roles and grants |
|---|---|
| `GET /api/incidents` | Station Staff: `R (scoped)`; Controller: `R` |
| `POST /api/incidents` | Station Staff: `C (scoped)`; Controller: `C` |
| `PATCH /api/incidents/{id}` | Station Staff: `U (scoped)`; Controller: `U` |

The separate Operations operation `GET /api/operations/overview` is granted to Station Staff as `R (scoped)` and Controller as `R`; it remains owned by Operations, not added to the Incident Management operation list.

`R`, `C`, and `U` are existing matrix grants. This specification does not add a route, grant, role, or separate communications-officer approval. The same matrix denies these incident operations to Public/Passenger and Driver. Feature 002 remains the authority for authentication and current role assignment. Station Staff access uses current station scope. Controller incident scope requires clarification; it is not copied from Feature 003.

The PDF does not define request/response schemas, list filters, pagination, resource projection, association payloads, lifecycle values/transitions, expected revision/conflict behavior, duplicate retry handling, or precise status-code mapping. The three approved operations can remain the operation boundary, but these details must be settled in planning/contract work before implementation. If a required behavior cannot be expressed by these existing operations, obtain explicit API approval rather than adding a route.

## Open Business and Contract Questions

These questions are intentionally unresolved; no answer is assumed in this draft.

1. **Incident authorization scope**: For the Controller's unqualified Create/View/Update incident grants, is access global to all incidents or constrained by a defined incident, station, or operational assignment? What exact evidence applies when an incident spans more than one station or journey? The Feature 003 global journey-scope decision does not answer this.
2. **Lifecycle and retry semantics**: What are the exact lifecycle state names and allowed transitions, including resolution versus closure, reopening, correction, concurrent update conflicts, and duplicate submissions after uncertain responses? Which minimum audit fields/reason are required by the Must Have lifecycle, distinct from Should Have SR-051's complete audit chronology?
3. **Incident and impact contract**: Within the existing GET/POST/PATCH operations, what exact fields represent report time versus incident occurrence time, category values, station/service/journey links, and a known published disruption? Can a report be saved when a referenced owner source is temporarily unavailable, or must creation wait? What status/error and conflict behavior should clients receive for malformed, missing, inaccessible, stale, or unavailable owner evidence?

### Clarification Record

- No Feature 004 business/API clarifications have been approved in this first specification draft. Questions 1–3 remain open and must be resolved before planning their affected behaviors.
- The previously approved Feature 003 incident-visibility decision is preserved: Incident Management owns incident records, affected-journey associations, and authorized incident-context reads; the operations client composes that context with Feature 003 official journey data; private incident records are not exposed in the public JourneySnapshot.

## Out of Scope and Deferred Baseline Items

- Any changes to Feature 001 railway reference data, Feature 002 identity/RBAC grants, or Feature 003 official journey lifecycle/publication.
- New public routes, roles, grants, or an incident-to-journey automatic status-publication workflow.
- Severity classification (SR-043, Could Have), incident notifications (SR-045, Should Have), a dedicated correction workflow (SR-047, Should Have), specialized platform-safety reporting (SR-049, Should Have), additional local-incident client acceptance (SR-050, Should Have), complete incident audit chronology beyond the SR-042 minimum (SR-051, Should Have), and external responder workflow/portal (SR-053, Could Have).
- Signaling, train control, automatic disruption prediction, emergency-service/third-party user portals, and any unapproved automated decision about cancellation or journey recovery.
