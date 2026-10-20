# Feature Specification: Driver Operations

**Feature Branch**: `codex/f005-integration`  
**Created**: 2026-10-20  
**Status**: Ready for implementation under the user's autonomous MVP authorization

**Input**: Deliver authenticated assigned-journey access, driver observations and Controller review using the existing approved operations and authorization.

## User Scenarios & Testing

### User Story 1 ? View assigned duties (Priority: P1)
A driver sees only current assignments, their ordered stops, scheduled timings and centrally published status/changes. Control may inspect assignments using its existing read grant.

**Why this priority**: An authenticated duty view is the prerequisite for safe scoped reporting.
**Independent Test**: Two drivers assigned to different journeys receive only their own current duties; Control sees current assignments.

**Acceptance Scenarios**:
1. Given an active driver role and assignment, when assignments are read, then the response identifies the assigned journey with ordered route stops, scheduled timings, central status, publication revision/timestamps and recent official updates.
2. Given no assignment, when an authorized driver reads assignments, then an empty result is returned; revoked assignments are excluded on the next read.
3. Given another driver, missing/expired credentials, revoked role or unverifiable authority, when access is attempted, then protected information is denied or unavailable without a false empty-success result.
4. Given Control publishes a new journey revision, when the next assignment read occurs, then the central revision/status and changes are reflected without an independent driver status source.

### User Story 2 ? Report an observation (Priority: P1)
An assigned driver submits a progress observation for staff review. It is evidence, never official status or a train-driving command.

**Independent Test**: A signed-in assigned driver reports text; another driver cannot report for that journey and journey state is unchanged.

**Acceptance Scenarios**:
1. Given a current assignment and submission grant, when a valid observation is submitted, then an immutable record receives a stable identity, verified author and server UTC timestamp.
2. Given invalid/blank text, client-supplied authority fields or unassigned journey, when submitted, then no observation is persisted.
3. Given a previously accepted request key, when an identical request is retried after current authorization is checked, then the existing result is returned; conflicting key reuse produces conflict.
4. Given an observation, when accepted, then the server captures the current journey revision and report UTC time; the request contains no caller-supplied author, status, revision or timestamp.

### User Story 3 ? Review and reference evidence (Priority: P1)
Control reads authored observations and may reference an observation through the already-approved official publication workflow.

**Independent Test**: Control reads a stored observation, references it in Feature 003 publication, and invalid, foreign or unavailable evidence prevents that publication.

**Acceptance Scenarios**:
1. Given an active Controller review grant, when observations for a known journey are read, then author, report time and observation text are available only in the protected projection.
2. Given a Driver, Passenger or Station Staff without review grant, when the protected review is requested, then access is denied.
3. Given a matching stored observation with its original assignment still active and stored journey revision present, when Feature 003 verifies it for the same journey and current authorized Controller, then verification succeeds; missing/foreign/unverifiable evidence does not succeed.
4. Given a submitted observation, when no separate Controller publication occurs, then official status, stop timing, schedules and signalling state remain unchanged.

### Edge Cases
- Empty lists differ from unavailable assignment, role or journey sources.
- Revoked assignment/role is checked again on retry; old tokens do not preserve revoked grants.
- Missing journey is not fabricated from an assignment row; owner failure returns safe unavailable.
- Overnight scheduled times and operation date are copied from Feature 003 without reinterpreting the operation date.
- Multiple driver assignments may refer to one journey; a driver/journey may have only one active assignment lifecycle.
- Unknown enum/status input cannot become a journey publication: observation input is plain text only.
- Text is data; clients must render it safely, never execute markup.

## Requirements

### Approved requirement evidence
Read directly from `FINAL UNIFIED REQUIREMENTS` in `docs/SmartRail-SDD-Handoff/evidence/requirements.xlsx`, columns A/D/G/O. Wording, priority and acceptance checks below are unchanged.

| ID | Priority | Exact approved requirement | Exact source acceptance check |
|---|---|---|---|
| SR-064 | Must Have | Drivers and authorized operational personnel shall sign in to an operational information application to access assigned journey information. | Valid driver sees assigned journeys; unauthorized user cannot access driver records. |
| SR-065 | Must Have | Driver users shall see assigned journeys, route, stopping order and relevant scheduled timings. | Driver can see scheduled assignment details for a selected duty. |
| SR-066 | Must Have | The driver application shall display authorized current journey status and recent service changes without asserting independent rail-control authority. | Changing central journey state refreshes the driver view. |
| SR-069 | Must Have | Where authorized, a driver shall be able to report a journey progress/status observation for railway staff review without altering schedules or signalling state. | Submitted report shows author and can be viewed by Control; invalid status changes blocked. |
| SR-072 | Must Have | SmartRail shall not implement train braking, door-side interlocks, movement authority, signalling commands or certified DMI functions; these remain in existing railway systems. | No train-driving command/control endpoints exist in the SmartRail application. |

SR-009 remains Should Have: only the minimal persistent assignment prerequisite required by SR-064/065/069 is included; no HR/competence or assignment-management UI. Shared SR-090/091 authentication/RBAC and SR-093/100/109/114 session/privacy/client-state obligations remain owned with Feature 002 and applicable clients.

### Functional Requirements
- **FR-001 (SR-064)**: Verify the caller and current active application role for every protected operation using the existing identity authority.
- **FR-002 (SR-064, SR-065)**: Return current assignments for the verified driver only; the existing Controller assignment-read grant permits the complete current collection.
- **FR-003 (SR-065)**: Include assigned journey identity, route/stopping order and relevant scheduled timings from their owning railway/journey sources.
- **FR-004 (SR-066)**: Return current official status, revision, timestamps and recent official changes from Journeys & Status; do not assert observation text as central status.
- **FR-005 (SR-069)**: Accept nonblank plain-text observations only from a currently assigned caller with the existing submission grant; derive author from verified identity and report time on the server.
- **FR-006 (SR-069)**: Persist immutable authored observations linked to the assignment and journey; prevent cross-driver submissions and duplicate retry records.
- **FR-007 (SR-069)**: Permit only the existing review grant to read authored observations for a journey.
- **FR-008 (SR-069)**: Supply genuine stored-record observation verification to Feature 003, checking current review authorization and target journey compatibility before publication.
- **FR-009 (SR-072)**: Expose no train braking, interlock, movement-authority, signalling or certified DMI functionality; observation submission must not mutate official journey/schedule state.
- **FR-010 (SR-064, SR-069)**: Fail closed on missing, revoked or unverifiable role/assignment/owner evidence; errors and ordinary logs must not reveal credentials, private subjects or database internals.
- **FR-011 (SR-064, SR-065)**: Maintain assignment grant/revoke history through a restricted non-public mechanism, default-denied until trusted maintenance authority is explicitly supplied; preserve transactional audit integrity.

### Key Entities
- **DriverJourneyAssignment**: Stable assignment identity, verified opaque driver subject, journey identity, grant actor/time/provenance and optional revocation actor/time.
- **DriverAssignmentAudit**: Immutable evidence of assignment grant/revocation, linked to the lifecycle.
- **DriverObservation**: Immutable observation identity, assignment/journey linkage, verified author, text, server report UTC time, server-captured journey revision and retry identity.

## Success Criteria
- **SC-001**: Every authorized assignment scenario returns only the driver's current duties or the Controller-permitted collection, with correct ordered journey details.
- **SC-002**: Every verified central journey change is reflected by the next assignment read with matching revision; complete driver-client refresh acceptance remains open until that client is tested.
- **SC-003**: Every accepted observation is authored and timestamped; every unauthorized, invalid or unavailable-authority submission persists no observation.
- **SC-004**: Identical authorized retries produce one observation; conflicting retries change nothing.
- **SC-005**: Control can review and reference a genuine same-journey observation; no observation alone changes official status.
- **SC-006**: All three approved operations and negative authorization cases pass; no driving-control or public assignment-management operations exist.

## Clarifications and autonomous decisions ? 2026-10-20
The user's explicit autonomous delivery instruction authorizes simplest secure choices within the approved scope; no interactive approval is implied for extra scope.
- Assignment list has no identity selector: Driver receives own current assignments, Controller uses existing global assignment-read grant. Multiple-role grants union explicitly; submission still requires Driver's grant and own current assignment.
- Minimal assignments are persisted locally by controlled non-HTTP maintenance with default-deny production authority; no fake production assignment provider.
- Observation input is exactly `{ requestId, text }`, with a nonempty UUID and trimmed text length 1?4000 (technical input bound, not a new business threshold). Unknown authority/status fields are rejected. Server controls identity, report UTC time and captured journey revision.
- Request ID supports safe observation retries; authorization precedes replay lookup. No arbitrary pagination/retention/refresh thresholds are invented.
- Historic observations remain stored after assignment revocation; revocation prevents new submissions and makes that original assignment ineligible as publication evidence. Verification checks original assignment active, same journey, and stored journey revision exists; unavailable authority fails closed. No arbitrary observation age cutoff and no new observation-revocation lifecycle. Existing terminal journeys are not excluded from assigned-driver observations by an invented lifecycle rule.

## Dependencies and exclusions
Feature 001 owns reusable schedules/stations; Feature 003 owns dated journeys, official revisions and published changes; Feature 002 owns validated identity/current roles. Driver Operations owns assignments and observations. No new identity, status publication, notification, incident or catering functionality. SR-067/071/073/108 Should Have and SR-068/070/111 Could Have remain deferred. Flutter driver UI, secure storage, offline/loading/error/retry/freshness states, refresh behavior and complete release E2E acceptance remain open for owning client work. Production assignment-maintenance authority/provisioning procedure and retention policy remain operational approvals; they do not enable a default production write path.
