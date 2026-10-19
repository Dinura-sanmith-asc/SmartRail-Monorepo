# Feature Specification: SmartRail Identity & Permissions

**Feature Branch**: `feature-02/Identity-&-Permissions`

**Created**: 2026-10-17

**Status**: Approved

## Approval Record

- **Feature identifier**: `002-identity-permissions`
- **Specification version**: Current review candidate (no prior document version recorded)
- **Current approval status**: Approved for client integration
- **Approval authority**: SmartRail Project Owner
- **Approval date**: 2026-10-19
- **Contract reference**: [Approved Identity & Permissions OpenAPI contract](contracts/identity-permissions.openapi.yaml) and [authorization boundary](contracts/authorization-boundary.md)
- **Verification evidence reference**: [Feature 002 local verification record](quickstart.md) and [requirements/evidence checklist](checklists/requirements.md)
- **Outstanding external dependencies**: production Cognito environment configuration and monitoring; role and station-scope assignment governance; readiness of owner-module scope providers; audit retention/access policy; client acceptance for SR-093 and SR-114; platform-wide MVP acceptance.

This approval covers the completed backend specification and its existing API contract for client integration. It does not constitute production deployment approval or full SmartRail platform acceptance. Backend verification evidence is recorded in the linked references; tests were not rerun as part of this approval update.

**Input**: User description: Define the authentication and authorization foundation for SmartRail passenger, driver, station-staff and railway-controller workflows.

## Clarifications

### Session 2026-10-17

Option B in the first clarification is the operative role-source decision. The historical token-group grant wording in the second clarification is preserved verbatim for traceability but is SUPERSEDED and is not operative authorization behavior. Its union rule applies only to active roles resolved from SmartRail's internal mapping; JWT group claims do not establish active grants.

- Q: Which model should determine a caller's active SmartRail roles, combine multiple roles and apply role revocation? -> A: Option B: Cognito authenticates the caller; SmartRail's controlled internal mapping keyed by verified Cognito subject is authoritative for active application roles. Multiple active roles combine only explicit RBAC grants; resource-scope and ownership checks always apply. Role revocation takes effect on the next authorization check against the active mapping. If the mapping cannot be verified, protected access is denied. JWT group claims are not trusted as active SmartRail grants.
- **SUPERSEDED historical source wording** (preserved verbatim; not operative authorization behavior). Q: If a user belongs to more than one Cognito group, how should SmartRail combine the permissions granted by those roles? -> A: Allow the union of explicit grants in the approved RBAC matrix for all groups on the validated token; continue enforcing every applicable ownership and resource-scope check. **Operative under Option B**: apply this explicit-grant union to active roles read from SmartRail's internal mapping, never to JWT group claims.
- Q: What authoritative evidence should SmartRail use to check station, journey and passenger-order scope, and what should it do when that evidence is missing, stale or revoked? -> A: Use each owning module's current source: Identity & Permissions maintains station-staff station assignments; Driver Operations owns driver-to-journey assignments; Catering Adapter verifies passengers against its order-owner association; Incident Management provides the target incident/resource relationship. Missing, stale, revoked or unverifiable evidence grants no access.
- Q: What should `GET /api/me` return when the caller has a valid Cognito identity, including when that identity has no recognized SmartRail role? -> A: Return only `{ subject, roles }`, where subject is the caller's Cognito subject and roles lists recognized SmartRail roles; return an empty roles list when none are recognized. Do not return resource scopes or personal profile fields.
- Q: If a caller sends a malformed or expired bearer token to one of the four anonymous Railway Network GET endpoints, should SmartRail still serve the public response? -> A: Ignore malformed or expired optional bearer tokens on these four public reads and process the request anonymously; protected operations still reject invalid or expired tokens.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Access public railway information (Priority: P1)

A person can view approved public railway information without signing in. Authentication must not become a prerequisite for the four existing Railway Network read operations.

**Why this priority**: Public information is an explicit approved access class and must remain distinct from protected operations (SR-089).

**Independent Test**: Call each approved public Railway Network GET operation without credentials and verify its existing response contract remains available.

**Acceptance Scenarios**:

1. **Given** no access token, **When** a person requests `GET /api/stations`, `GET /api/routes/{id}/stops`, `GET /api/services?origin=&destination=&date=`, or `GET /api/services/{id}`, **Then** the request is evaluated as an anonymous public read and returns only the permitted published railway projection.
2. **Given** an anonymous caller, **When** that caller requests a protected operational action, **Then** the action is rejected and no protected operation is performed.
3. **Given** an authenticated user requests one of the four public Railway Network GET operations, **When** the request is evaluated, **Then** authentication does not change the operation's public access classification or expose protected information.

---

### User Story 2 - Authenticate to protected SmartRail functions (Priority: P1)

A passenger or railway employee presents a valid Cognito-issued access token before using a protected function. The service identifies the caller from the verified token and rejects missing, invalid or expired credentials.

**Why this priority**: Protected operational information and write actions require authenticated users; trustworthy identity is the prerequisite for permission checks (SR-090).

**Independent Test**: Exercise a protected operation using no token, a malformed or invalid token, an expired token and a valid token; verify only valid authenticated calls proceed to authorization.

**Acceptance Scenarios**:

1. **Given** a protected operation and no token, **When** a caller requests it, **Then** the request is rejected before protected information or write behavior is exposed.
2. **Given** a malformed, invalid or expired Cognito-issued token, **When** it is presented to a protected operation, **Then** the caller is not treated as authenticated and the operation is rejected.
3. **Given** a valid Cognito-issued token, **When** its owner requests `GET /api/me`, **Then** the response contains only that caller's Cognito subject and roles from the current SmartRail mapping, with an empty roles list if no active role is mapped; it does not accept a caller-selected identity or expose resource scopes or profile fields.
4. **Given** a valid token with insufficient permission, **When** the caller requests a protected operation, **Then** access is denied and the operation has no protected side effect.
5. **Given** a valid token whose group claim still names a role that has been revoked in SmartRail's mapping, **When** the caller makes the next protected request, **Then** authorization uses the current mapping and denies any action no longer granted.
6. **Given** the active role mapping cannot be verified, **When** the caller requests a protected operation, **Then** SmartRail grants no protected access.
7. **Given** an expected API resource audience is configured, **When** a Cognito access token omits `aud` or has a different `aud`, **Then** the protected request is rejected as unauthenticated; **when** no expected audience is configured, **Then** a token with `aud` is rejected until an expected value is configured, while a token without `aud` is evaluated using the other required token checks.

---

### User Story 3 - Enforce role, ownership and resource scope (Priority: P1)

Passengers, drivers, station staff and controllers can use only the protected information and actions granted to their role and the relevant passenger, assigned journey or station scope. The server makes and enforces each authorization decision.

**Why this priority**: Correct role and resource checks protect railway operations and personal information; hiding controls in a client is insufficient (SR-091).

**Independent Test**: Run positive and negative permission cases for each role and target scope, including a station-staff attempt at a controller-only cancellation and a user attempting to access another passenger's order.

**Acceptance Scenarios**:

1. **Given** a station-staff identity scoped to Station A, **When** that user requests an operation on Station A, **Then** the request may proceed only when the RBAC matrix grants that operation; **when** the same user targets Station B, **Then** the request is denied.
2. **Given** a driver assigned to Journey A, **When** the driver reads the driver's own assignment or submits a progress observation for Journey A, **Then** the matrix permits those actions; **when** the driver targets an unassigned journey or tries to publish official status, **Then** the request is denied.
3. **Given** a station-staff identity, **When** that user attempts a controller-only journey cancellation, **Then** the server denies the action regardless of the controls shown by the client.
4. **Given** an authenticated passenger, **When** the passenger requests their own catering order, **Then** the order may be read; **when** they request another passenger's order, **Then** access is denied.
5. **Given** a controller identity, **When** the controller requests an action explicitly granted to Controller in the approved RBAC matrix, **Then** the action is permitted subject to the target-resource and feature-specific rules.

---

### User Story 4 - Keep sessions and access failures safe (Priority: P2)

Users receive a clear denial or recoverable error when access fails. Credentials, tokens and sensitive personal information remain protected in client storage, API responses and application logs.

**Why this priority**: Secure session handling and safe failures prevent credential disclosure and help users recover without weakening access controls (SR-093, SR-100, SR-114).

**Independent Test**: Test expired sessions, denied access, server errors and unavailable networks in applicable primary client workflows; inspect device storage and error/log output for exposed credentials or sensitive identifiers.

**Acceptance Scenarios**:

1. **Given** a driver or operational session has expired, **When** the user attempts a protected action, **Then** access is rejected and the client communicates that re-authentication is needed without retrying the protected action as successful.
2. **Given** an access denial or server failure, **When** the error is returned or logged, **Then** it contains no access token, credential or unrestricted sensitive personal information.
3. **Given** the driver or operational client stores session credentials, **When** device storage is inspected, **Then** access tokens are not present in plaintext application cache.
4. **Given** an applicable passenger or operational view experiences no data, access denial, server error or loss of network, **When** the state is shown, **Then** it provides the meaningful no-data, access-denied, error and retry behavior required by SR-114 without bypassing authorization.

### Edge Cases

- A token is correctly signed but expired, issued by an untrusted authority, or fails the configured resource-audience rule; it must not establish an authenticated caller.
- A token has no active recognized SmartRail role or has unrecognized role data; do not infer additional permissions. Multiple active internal roles combine only their explicit RBAC grants; JWT group claims do not establish roles.
- A caller has a valid role but the requested station, journey, incident or order has no matching scope/ownership evidence; deny access.
- A driver assignment or station scope is missing, stale or cannot be authoritatively checked; do not grant access by default.
- A passenger's token identifies a caller, but order ownership cannot be matched reliably; do not return another person's protected order information.
- The same user has a role that permits reading but not writing; a read grant must not imply a create, update, cancellation or publication grant.
- An unauthorized write attempt must be denied before state changes; retries or client-side controls must not convert a denial into success.
- A request to `GET /api/me` has no token or an invalid/expired token; it must not disclose identity data. A valid authenticated identity without a recognized SmartRail role receives its own subject and an empty role list, but no protected permission.
- A malformed or expired optional bearer token accompanies an otherwise anonymous Railway Network read; the request is processed anonymously and only the public projection is returned.
- A catering provider request is forged or malformed; Identity & Permissions does not process it, while the Catering Adapter must enforce its own authenticated provider boundary under SR-094.

## Requirements *(mandatory)*

### Approved Requirement Traceability

The following wording and acceptance checks are transcribed from the approved MoSCoW workbook. They remain authoritative and are not paraphrased as replacements.

| ID | Priority | Approved requirement wording | Approved acceptance check |
|---|---|---|---|
| SR-089 | Must Have | Public railway information shall be distinguishable from functions requiring passenger or railway-staff authentication. | Anonymous viewer can access permitted service details but not protected operational APIs. |
| SR-090 | Must Have | Operational web and driver applications shall authenticate their users before exposing protected railway information or write actions. | Protected endpoint rejects missing/invalid token and respects session expiry. |
| SR-091 | Must Have | Server-side authorization shall restrict operational changes based on organizational role and the target station/journey/resource; front-end hiding alone is insufficient. | Station credentials cannot call controller-only cancellation endpoint; role/resource tests pass. |
| SR-093 | Must Have | Driver and operational sessions shall use supported secure credential storage and controlled expiration, with no plaintext tokens in app cache. | Test device storage contains no plaintext access tokens. |
| SR-100 | Must Have | Sensitive personal information and credentials shall not be exposed in verbose API errors or unrestricted application logs. | Negative tests show tokens/redacted identifiers absent from API error responses. |
| SR-114 | Must Have | The system shall provide meaningful loading, no-data, access-denied, error and retry states for primary passenger and operational workflows. | Each primary view has tested no-results, 403, 500 and no-network cases. |

**Related downstream Must Have requirement**: SR-094: "Catering integration shall authenticate provider/API requests and validate incoming payloads before accepting order-state changes." Its approved acceptance check is "Forged or malformed provider update is rejected without persisting altered state." The Catering Adapter owns that provider-to-SmartRail trust boundary; this feature defines no provider credential, webhook, or order-state workflow.

### Functional Requirements

- **FR-001 (SR-089)**: SmartRail MUST classify railway information as public or protected and require authentication only for protected functions. The four existing Railway Network GET operations MUST remain anonymous public reads even when a malformed or expired optional bearer token is supplied; such a token MUST be ignored and the request processed anonymously: `GET /api/stations`, `GET /api/routes/{id}/stops`, `GET /api/services?origin=&destination=&date=`, and `GET /api/services/{id}`. Their contracts and published-data projections remain owned by Feature 001.
- **FR-002 (SR-090)**: SmartRail MUST accept Cognito-issued access JWTs as the authentication credential for protected passenger and railway-staff workflows and MUST reject missing, invalid and expired credentials before protected information or write actions are exposed. When an expected API resource audience is configured, `aud` MUST be present and match; a missing or mismatched value MUST be rejected. If no expected audience is configured, a token with `aud` MUST be rejected until the expected value is configured; a token without `aud` remains subject to all other required validation.
- **FR-003 (SR-090, SR-091)**: SmartRail MUST distinguish the approved application roles Public, Passenger, Driver, Station Staff and Controller. For authenticated callers, SmartRail MUST resolve active roles from its controlled internal mapping keyed by the verified Cognito subject; Cognito group claims MUST NOT be trusted as active SmartRail grants. A public caller has no authenticated role. When the active mapping contains multiple SmartRail roles for a subject, SmartRail MUST combine only their explicit grants in the approved RBAC matrix; each grant remains subject to applicable ownership and resource-scope checks. Authentication alone MUST NOT imply any protected permission.
- **FR-004 (SR-091)**: SmartRail MUST enforce permissions on the server for every protected operation using the approved role-to-action grants and current authoritative evidence for the target station, journey or resource scope. Identity & Permissions supplies station-staff station assignments; Driver Operations supplies driver-to-journey assignments; Incident Management supplies incident-to-resource relationships; and Catering Adapter verifies passenger order ownership. A user interface MUST NOT be the authorization boundary.
- **FR-005 (SR-091)**: SmartRail MUST deny access when required role, ownership or resource-scope evidence is absent, stale, revoked, unverifiable or does not match the target. If the active role mapping or an authoritative scope source is unavailable or cannot be verified, SmartRail MUST grant no protected access. A denial MUST NOT change protected state.
- **FR-006 (RBAC baseline)**: SmartRail MUST apply the approved RBAC matrix recorded below as the cross-module authorization baseline. Module specifications own their operation behavior; this feature MUST NOT add operations or broaden grants.
- **FR-007 (API inventory)**: SmartRail MUST provide authenticated `GET /api/me` behavior for the caller represented by a valid token. The operation MUST NOT accept a user identifier that allows a caller to select another identity. It MUST return only the caller's Cognito subject and active SmartRail roles resolved from the internal mapping, excluding credentials, tokens, resource scopes and personal profile fields. A valid authenticated identity with no active role mapping receives an empty roles list; that identity receives no protected permissions. If the active mapping cannot be verified, the response MUST NOT assert active roles.
- **FR-008 (SR-093)**: Driver and operational clients MUST use supported secure credential storage and controlled session expiration; access tokens MUST NOT be stored in plaintext application cache. Applicable clients MUST reflect expiry and require renewed authentication before protected work proceeds.
- **FR-009 (SR-100)**: Authentication and authorization failures MUST NOT expose credentials, access tokens or unrestricted sensitive personal information through verbose API errors or unrestricted application logs. Error output MUST use redacted identifiers where identifiers are needed for diagnosis.
- **FR-010 (SR-114)**: Applicable primary passenger and operational workflows MUST present meaningful access-denied, no-data, error and retry states. A retry MUST preserve the same authorization requirement and MUST NOT imply an unauthorized operation succeeded.
- **FR-011 (SR-094 downstream)**: Catering Adapter specifications MUST separately define authenticated provider requests and payload validation before accepting provider order-state changes. This feature MUST NOT implement the catering provider integration.
- **FR-012 (scope boundary)**: This feature MUST NOT add public sign-up, password-reset, user administration, role administration, station-scope administration or other identity-management endpoints unless a separate approved requirement authorizes them. It MUST NOT expose station import or timetable publication through identity policy or HTTP.

### RBAC Baseline and Module Boundaries

This table preserves the permission matrix approved in `RBAC & API Contracts.pdf` (R = read, C = create, U = update, — = no grant). “Own” and “scoped” require server-side checks. The PDF combines Public / Passenger in its column; the stated distinction for authenticated passenger order actions is retained.

| Approved action | Public / Passenger | Driver | Station Staff | Controller |
|---|---|---|---|---|
| Search trains, stations and schedules | R | R | R | R |
| View published journey status | R | R | R | R |
| View station departures | R | R | R | R |
| View operations dashboard | — | — | R (scoped) | R |
| View driver assignments | — | R (own) | — | R |
| Submit driver progress observation | — | C (own) | — | — |
| Review driver observations | — | — | — | R |
| Publish official journey status | — | — | — | U |
| Change platform / operational timing | — | — | — | U |
| Confirm journey cancellation | — | — | — | U |
| Create incident | — | — | C (scoped) | C |
| View incidents | — | — | R (scoped) | R |
| Update incident lifecycle | — | — | U (scoped) | U |
| Discover catering options | R | R | R | R |
| Create catering order | C (authenticated passenger only) | — | — | — |
| View catering order | R (own) | — | — | — |

**Module ownership boundaries**:

- **Identity & Permissions**: establishes and verifies caller identity, supplies role and scope decisions, protects `GET /api/me`, and defines shared authorization outcomes. Cognito remains the credential authority.
- **Railway Network**: owns the four anonymous public reads listed in FR-001 and public scheduled/reference data. This feature does not change Feature 001.
- **Journeys & Status**: owns dated journey reads and official status publication. Only Controller has the matrix grants to publish official status, change platform/operational timing or confirm cancellation.
- **Driver Operations**: owns driver assignment and observation workflows. Drivers may access their own assignment and submit observations only for assigned journeys; they do not publish official status.
- **Incident Management**: owns incident lifecycle. Station Staff access is station-scoped; Controller grants follow the matrix.
- **Catering Adapter**: owns passenger-order access and the separate provider/API authentication boundary. Passengers may create an order when authenticated and may read only their own order; SR-094 validation is owned here.

This feature defines authorization policy boundaries for those modules; it does not implement their workflows or endpoint contracts beyond the approved `GET /api/me` inventory entry.

### Key Entities *(include if feature involves data)*

- **Authenticated principal**: The caller established by a valid Cognito-issued token and identified by a stable authority subject. It is distinct from credentials and from any client-supplied user identifier.
- **SmartRail role**: A recognized application role used with an action grant. Internal codes are `Passenger`, `Driver`, `StationStaff`, and `Controller`; `GET /api/me` labels are `Passenger`, `Driver`, `Station Staff`, and `Controller`, respectively. `Public` is an unauthenticated access class, not an assigned role or response label. This representation mapping does not change RBAC grants.
- **Active role mapping**: The controlled SmartRail association between a verified Cognito subject and its currently active application roles. This mapping, not JWT group claims, is authoritative for authorization; revocation is observed on the next authorization check.
- **Resource scope**: Evidence that a principal is associated with a target station, assigned journey, incident or owned passenger order where the approved matrix requires “scoped” or “own” access.
- **Station-scope audit history**: Append-only grant/revocation events for Identity-owned station scope assignments, transactionally associated with each change and recording the verified actor and UTC event time.
- **Authorization decision**: The server's allow or deny result for a principal, action and target resource. A denied decision has no protected state change.

### Cognito resource audience rule

The API MUST always validate Cognito signature/signing key, exact trusted issuer, lifetime, `token_use=access`, and an allowlisted human `client_id`. Resource-audience handling is conditional but fail-closed:

- When an expected API resource audience is configured, the access token MUST contain an `aud` claim matching one of the configured expected audience values. A missing or mismatched `aud` is invalid and receives 401 on protected operations.
- When no expected API resource audience is configured, an access token with no `aud` may be accepted after all other checks. If an `aud` claim is present, reject it until an expected audience is configured; do not accept an unvalidated resource-bound token.
- Do not substitute `client_id` for `aud`, and do not make the audience check silently optional when an expected value is configured.

### Success Criteria *(mandatory)*

#### Measurable Outcomes

- **SC-001**: 100% of the four approved Railway Network GET operations remain callable without authentication and expose only their existing permitted public projections.
- **SC-002**: 100% of protected-operation test cases reject missing, invalid or expired credentials before protected data or write effects are exposed.
- **SC-003**: 100% of approved role/action/resource-scope test cases produce the matrix-defined allow or deny result, including the station-staff/controller cancellation case and cross-owner passenger-order case.
- **SC-004**: 100% of denied write tests show no protected state change.
- **SC-005**: 100% of applicable driver and operational client storage checks find no plaintext access tokens, and negative response/log tests find no access tokens or unrestricted sensitive identifiers.
- **SC-006**: Each applicable primary passenger and operational workflow has tested no-data, access-denied, server-error and no-network/retry states as required by SR-114.

## Assumptions

- Cognito is the sole credential authority; SmartRail does not create or store user passwords.
- Cognito-issued access JWTs authenticate the caller. The API verifies signature, issuer, lifetime, `token_use=access`, and allowlisted `client_id` before using the verified Cognito subject to look up active SmartRail roles. If an expected resource audience is configured, `aud` must be present and match; if none is configured, `aud` must be absent. The internal mapping is authoritative; JWT group claims and client-supplied role or scope values are not trusted as active SmartRail permissions. A current mapping is checked for each authorization decision, and inability to verify it grants no protected access.
- The four Railway Network GET operations retain their already-approved anonymous access and response contracts.
- The approved RBAC/API PDF is authoritative for action grants and the endpoint inventory. Feature-specific modules refine how those actions work without broadening grants.
- The authenticated subject is the caller identity for `GET /api/me`; no arbitrary user lookup is implied by that endpoint. The response includes only subject and recognized roles, not resource scopes or profile fields.
- Active SmartRail role assignments are held in a controlled internal mapping keyed by verified Cognito subject. Assignment and revocation use a controlled non-public process; no public role-administration endpoint is approved. Cognito user enrollment, password recovery, operational account provisioning and scope-assignment procedures remain outside this feature unless separately approved.
- Authorization is deny-by-default whenever required identity, role, ownership or resource-scope evidence cannot be established.

## Remaining Operational Decisions

- SmartRail's internal role mapping is authoritative, but the organization must approve which operators may assign or revoke mappings and what audit evidence the production procedure retains. This is an operational approval gate; it does not authorize a public role-administration API.
- Identity & Permissions owns current station-staff scope records, but the authorized process for creating, reviewing and revoking those records must be defined before production use. No public station-scope administration endpoint is approved.
- Token issuer, audience, signing-key and expiry settings must be configured for each environment. Cognito JWT revocation/expiry controls remain deployment decisions; SmartRail role revocation is enforced from the current internal mapping on the next authorization check. The API must verify signature, issuer, audience and expiry and must not trust JWT group claims or client-supplied role or scope values.
