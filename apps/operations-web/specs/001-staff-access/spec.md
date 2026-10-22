# Feature Specification: Staff Authentication & Role-Aware Operations Foundation

**Feature Branch**: `feature-01` (current Git branch at review; feature directory remains `specs/001-staff-access`)

**Created**: 2026-10-19

**Status**: Approved

**Specification Version**: 0.1.0

**Input**: User description: Establish authenticated Railway Controller and Station Staff access, role-aware operations navigation, safe session handling, and sign-out for the SmartRail Operations Web.

## Feature Approval Record

- **Feature**: 001-staff-access — Staff Authentication & Role-Aware Operations Foundation
- **Specification version**: 0.1.0
- **Specification status**: Approved
- **Approval authority**: SmartRail Project Owner
- **Approval date**: 2026-10-19
- **Approved contract reference**: Backend Feature 002 `GET /api/me`, [`identity-permissions.openapi.yaml`](../../../SmartRail/specs/002-identity-permissions/contracts/identity-permissions.openapi.yaml), approved for client integration on 2026-10-19
- **Backend verification evidence reference**: [`quickstart.md`](../../../SmartRail/specs/002-identity-permissions/quickstart.md) and [`requirements.md`](../../../SmartRail/specs/002-identity-permissions/checklists/requirements.md) in Backend Feature 002; these are backend evidence only and do not establish Operations Web acceptance
- **Operations Web verification evidence**: Client tests and live integration are not run; see [quickstart.md](quickstart.md). Client acceptance remains pending.
- **Constitution ratification**: Operations Web Constitution v1.0.0 ratified by the SmartRail Project Owner on 2026-10-19
- **Outstanding external dependencies**: Cognito environment/app-client configuration; API base URL, CORS, TLS and hosting; controlled role-mapped test identities; an approved owning-feature protected operation for resource-scope acceptance; production monitoring and deployment
- **Approval boundary**: This approval covers the reviewed Feature 001 specification and its approved client integration scope. It does not approve production deployment, production Cognito configuration, or full SmartRail platform acceptance.

## Clarifications

### Session 2026-10-19

- Q: Which browser sign-in flow is approved for the Operations Web? → A: Cognito managed login using OAuth 2.0 Authorization Code with PKCE. Specific client libraries, OAuth configuration values, and token handling implementation are for technical planning.
- Q: Does the approved identity response include assigned station context? → A: No. `GET /api/me` returns only `subject` and `roles`; use a generic Station Staff shell until an approved owning-feature response supplies station context.
- Q: What is the approval state of Backend Feature 002 for client integration? → A: Approved by the SmartRail Project Owner on 2026-10-19; the approved contract is available for client integration. This does not establish production Cognito configuration or deployment readiness.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Railway Controller enters the operations workspace (Priority: P1)

A Railway Controller authenticates through the approved Cognito browser flow and reaches the Controller workspace when SmartRail confirms the Controller role. This feature establishes protected entry and role-aware navigation; it does not build the live operations dashboard or controller workflows.

**Why this priority**: Controller access is necessary for authorized operational use, while the interface must not imply that authentication alone grants an operational permission.

**Independent Test**: With a controlled identity whose current SmartRail role mapping contains Controller, complete the approved sign-in flow and confirm the caller reaches the Controller workspace shell. Confirm `/api/me` supplies the role used for presentation and no journey, incident, or publication workflow is performed.

**Acceptance Scenarios**:

1. **Given** a person with a valid Cognito identity and an active Controller role mapping, **When** they complete the approved sign-in flow and the Operations Web reads their current identity, **Then** they reach the Controller workspace shell and see only navigation affordances approved for the returned role set.
2. **Given** the same authenticated Controller, **When** they attempt to open a protected route in the Operations Web, **Then** the application permits entry to the role-appropriate shell while the backend remains responsible for authorizing every protected API operation.
3. **Given** a valid authenticated identity with no active SmartRail role mapping, or with only non-staff roles such as Passenger or Driver, **When** `/api/me` returns the current role set, **Then** the user is not treated as Controller or Station Staff and receives a safe no-operational-access state.

---

### User Story 2 - Station Staff enters a restricted workspace (Priority: P1)

A Station Staff member reaches the Station Staff workspace when SmartRail confirms that role. A caller with multiple active roles may receive only the union of presentation affordances mapped to the roles actually returned. The application does not infer the user's assigned stations from the identity response; any protected station or resource access is decided by the backend using current scope evidence.

**Why this priority**: Station Staff need an authenticated entry point, and the distinction between role access and resource scope prevents a broad role from implying access to every station.

**Independent Test**: With a Station Staff identity and approved scope fixtures in the backend authorization test environment, confirm that the role selects the Station Staff shell and that an out-of-scope resource decision is denied by the backend. The Operations Web feature adds no resource operation API.

**Acceptance Scenarios**:

1. **Given** a person with a valid Cognito identity and an active Station Staff role mapping, but no active Controller role, **When** they complete the approved sign-in flow and the Operations Web reads their current identity, **Then** they reach the Station Staff workspace and do not receive Controller-only navigation affordances.
2. **Given** Station Staff has an active scope for Station A, **When** a request is evaluated for a protected resource at Station B by its owning backend feature, **Then** the backend denies the out-of-scope request and returns no protected data or state change; the Operations Web presents a safe denial if that response is received.
3. **Given** a Station Staff identity whose station-scope evidence is missing, revoked, stale, or unavailable, **When** an owning backend feature evaluates a scoped operation, **Then** the backend grants no access; the Operations Web does not use a client-selected station, role label, or navigation state as proof of scope.

---

### User Story 3 - A person receives safe feedback when access is unavailable (Priority: P1)

A person who is not authenticated, whose session is no longer valid, or whose role cannot perform an action receives clear feedback without access to protected operational information. This story covers UI access states only; it does not implement operational business actions.

**Why this priority**: Safe denial is part of the authentication boundary and prevents the interface from exposing protected data or suggesting that an unauthorized operation succeeded.

**Independent Test**: Exercise protected entry and the identity contract with no token, an invalid or expired token, a valid identity without a permitted role, an authorization denial, an unavailable authorization authority, and a service/network failure. Confirm the appropriate safe state is shown and no protected content is rendered.

**Acceptance Scenarios**:

1. **Given** an unauthenticated person, **When** they open a protected Operations Web route, **Then** no protected operational content is displayed and the application presents the approved sign-in entry.
2. **Given** a missing, malformed, invalid, or expired Cognito access token, **When** a protected request is made, **Then** the backend rejects it as unauthenticated, the Operations Web does not treat it as a successful response, and the user is asked to authenticate again.
3. **Given** an authenticated Station Staff user whose returned role set does not include Controller, **When** they attempt to access a Controller-only route or operation, **Then** Controller-only navigation is unavailable and any protected API request is still evaluated by backend authorization; a denial does not change protected state.
4. **Given** a valid identity without the role or resource permission for a protected operation, **When** the backend returns a forbidden response, **Then** the Operations Web displays a safe access-denied state and does not expose protected response data.
5. **Given** the backend cannot verify current role or scope authority, **When** it returns an authorization-unavailable response, **Then** the Operations Web grants no access, explains that access information is temporarily unavailable, and offers only a safe recovery action.
6. **Given** the identity or workspace request fails with a service error or loss of network, **When** the user views the affected state, **Then** the application displays an understandable error and retry option; retry does not bypass authentication or authorization.
7. **Given** the person cancels Cognito managed login or the sign-in callback does not establish a valid session, **When** the Operations Web handles that outcome, **Then** the person remains unauthenticated, protected content remains unavailable, and the application provides a safe way to retry sign-in.

---

### User Story 4 - An authenticated staff member signs out (Priority: P2)

An authenticated staff member can leave the Operations Web and remove access to protected routes in that application. Cognito-managed browser-session logout is used only when the approved app-client logout configuration is available; this feature does not claim that an already-issued access token is revoked.

**Why this priority**: Staff must be able to end their application session safely, while the product must be precise about the distinction between closing the local workspace and revoking a bearer token.

**Independent Test**: Sign in with either operational role, sign out, navigate directly to a protected route, and use browser back navigation. Confirm the Operations Web no longer presents protected content or sends a protected request using credentials retained by the application.

**Acceptance Scenarios**:

1. **Given** an authenticated Controller or Station Staff member, **When** they choose sign out, **Then** the Operations Web removes its authenticated application state and returns to the unauthenticated entry.
2. **Given** a staff member has signed out, **When** they revisit a protected route or use browser back navigation, **Then** protected content remains inaccessible until authentication is completed again.
3. **Given** an application sign-out has completed, **When** Cognito-managed logout configuration is available, **Then** the browser follows that configured logout behavior; regardless of remote token lifetime, the Operations Web keeps protected routes closed and does not claim that local sign-out revoked the bearer token at the backend.

### Edge Cases

- `/api/me` returns `roles: []`: show no-operational-access feedback and do not infer a role from token group claims or client state.
- `/api/me` returns `503` because the current role mapping cannot be verified: grant no operational access and show a safe temporary-unavailability state.
- A current role is revoked after the workspace shell has been displayed: a later protected backend request remains authoritative; handle its denial without presenting the action as successful. The UI refresh timing is not an authorization control.
- A response contains an unrecognized role label: do not map it to Controller or Station Staff; show no additional protected navigation and record only safe diagnostics.
- A user has multiple active recognized roles: show only navigation affordances mapped to the returned role labels; every protected operation remains subject to the backend's active role, permission, and resource checks.
- A user selects a station or resource not in their scope: do not treat the selection as authorization evidence; the owning backend feature must decide access.
- Identity or session errors contain Problem Details or diagnostic fields: show only safe user-facing feedback and never render tokens, claims, credential values, SQL details, or authorization internals.
- The browser is offline during sign-in, identity loading, or retry: do not expose cached protected content as current or treat a pending request as authenticated success.
- Cognito managed login is cancelled or its callback fails: remain unauthenticated, expose no protected content, and provide a safe retry path without replaying a protected action.
- Sign-out is followed by browser back navigation or a stale client-side route: protected content stays unavailable in the Operations Web.

## Requirements *(mandatory)*

### Approved Requirement Traceability

The wording, priorities, and acceptance checks below are copied from the approved `FINAL UNIFIED REQUIREMENTS` sheet in `requirements.xlsx`. Treatment identifies this feature's boundary and does not change the source priority or acceptance check.

| ID | Priority | Approved requirement wording | Approved acceptance check | Treatment in this feature |
|---|---|---|---|---|
| SR-038 | Must Have | Authorized railway staff shall access a web-based overview of current services, journey states and important incidents. | Role-authorized login can view live operations summary; unauthorized users cannot. | This feature establishes authenticated, role-aware entry only. The live overview and its data are a separate Operations Dashboard feature and are not claimed complete here. |
| SR-046 | Must Have | Only roles authorized under railway policy may confirm a train cancellation; the app shall not independently decide to cancel a service. | Station and driver accounts cannot execute controller-only cancellation API. | Authorization boundary only. This feature adds no cancellation action or API; backend Feature 002 and the owning operational feature retain enforcement and acceptance evidence. |
| SR-089 | Must Have | Public railway information shall be distinguishable from functions requiring passenger or railway-staff authentication. | Anonymous viewer can access permitted service details but not protected operational APIs. | Establish the protected Operations Web entry boundary; public railway reads remain owned by their existing backend feature. |
| SR-090 | Must Have | Operational web and driver applications shall authenticate their users before exposing protected railway information or write actions. | Protected endpoint rejects missing/invalid token and respects session expiry. | In scope for protected workspace entry and client session handling using Cognito managed login with OAuth 2.0 Authorization Code and PKCE. |
| SR-091 | Must Have | Server-side authorization shall restrict operational changes based on organizational role and the target station/journey/resource; front-end hiding alone is insufficient. | Station credentials cannot call controller-only cancellation endpoint; role/resource tests pass. | Backend authorization is authoritative. The UI's role-aware navigation is presentation only; out-of-scope operational endpoints are not added or called. |
| SR-093 | Must Have | Driver and operational sessions shall use supported secure credential storage and controlled expiration, with no plaintext tokens in app cache. | Test device storage contains no plaintext access tokens. | In scope for the browser client: no plaintext token persistence in application storage, safe expiration handling, and no credentials in diagnostics. Storage mechanism depends on the approved Cognito browser flow. |
| SR-100 | Must Have | Sensitive personal information and credentials shall not be exposed in verbose API errors or unrestricted application logs. | Negative tests show tokens/redacted identifiers absent from API error responses. | In scope for user-facing authentication/authorization errors and client diagnostics. The API's safe error/log behavior is owned and tested by the backend. |
| SR-114 | Must Have | The system shall provide meaningful loading, no-data, access-denied, error and retry states for primary passenger and operational workflows. | Each primary view has tested no-results, 403, 500 and no-network cases. | In scope for this feature's protected entry, identity loading, access-denied, service-error, offline, and retry states; later dashboard views require their own acceptance. |

SR-102 (color-independent train/disruption status) is not a functional requirement of this feature because it does not display train or disruption status. Any later operational view that displays status must trace SR-102 and its source acceptance check. Other Operations Web requirements concerning status editing, incidents, publication, and journey workflows are explicitly outside this feature.

### Scope and Boundaries

**In scope**

- Initiating Cognito managed login using OAuth 2.0 Authorization Code with PKCE.
- Establishing protected entry to a role-aware Operations Web shell for Railway Controller and Station Staff.
- Reading the authenticated caller's current SmartRail role labels through the existing identity contract.
- Selecting presentation and navigation affordances by returned role, including safe no-role behavior.
- Respecting backend decisions for authentication, authorization, station/resource scope, and authority availability.
- Handling unauthenticated, forbidden, expired-session, unavailable-authority, service-error, offline, loading, and retry states.
- Signing out of the Operations Web and preventing access to protected content from that application until authentication resumes.
- Keyboard operation, focus behavior, accessible names, screen-reader feedback, readable text, and non-color-dependent auth/access states.

**Out of scope**

- Backend Feature 002 changes, new identity endpoints, user/role/scope administration, or changes to Cognito configuration.
- Creating user accounts, sign-up, password reset, credential administration, or role assignment.
- Implementing the live operations dashboard, journey search/status, status publication, cancellation, incidents, driver observations, station data, or any other operational workflow.
- Calling or implementing Journey Feature 003 APIs, including dashboard/overview or journey operations APIs.
- Treating hidden controls, route guards, client-selected roles, token group claims, or station selectors as server authorization.
- Displaying station scope from `/api/me`; its contract does not return scope data.
- Claiming that local application sign-out revokes a previously issued Cognito token or invalidates the Cognito-managed browser session unless an approved contract confirms that behavior.

### Authentication Contract and Integration Dependencies

The available Identity & Permissions OpenAPI contract is `../../../SmartRail/specs/002-identity-permissions/contracts/identity-permissions.openapi.yaml`. Its only HTTP operation is authenticated `GET /api/me`; it is not a sign-in, sign-out, refresh, or role-administration API. The contract defines a successful response with exactly `subject` and `roles`. `subject` is the opaque verified Cognito subject. `roles` contains recognized active SmartRail labels (`Passenger`, `Driver`, `Station Staff`, `Controller`) and may be empty. The contract returns no profile fields or station/resource scopes. It documents `401` for a missing/invalid access token and `503` when the current role mapping cannot be verified. Backend authorization maps known permission denials to `403` and authority unavailability to `503` for protected operations. The approved browser sign-in choice is Cognito managed login using OAuth 2.0 Authorization Code with PKCE. Cognito app-client, callback/logout URI, issuer/domain, and scope values MUST come from approved environment configuration; this feature does not invent their values or an additional SmartRail endpoint.

The current backend implementation validates Cognito access tokens and resolves active SmartRail roles from its current internal mapping; Cognito group claims and client-supplied roles are not grants. The backend contract does not define browser refresh-token handling or Cognito session revocation. An expired access token MUST stop protected work and return the user to managed login; the client MUST NOT replay a protected action as successful. Sign-out MUST clear the Operations Web session and protected route state. The app MUST NOT claim that this local action revokes an already-issued token; Cognito-managed logout may be used only with the approved app-client logout configuration. Silent renewal and global revocation are not claimed by this feature.

The API contract deliberately omits station scopes. The initial Station Staff shell MUST remain generic until an approved owning-feature response supplies station context. Actual resource authorization remains backend-enforced, and no new Identity endpoint is introduced to expose scopes.

Backend Feature 002 (`../../../SmartRail/specs/002-identity-permissions/spec.md`) is **Approved** by the SmartRail Project Owner on 2026-10-19 for client integration, including its existing [OpenAPI contract](../../../SmartRail/specs/002-identity-permissions/contracts/identity-permissions.openapi.yaml). Its recorded verification evidence is referenced from the backend quickstart and checklist; this clarification workflow did not rerun backend tests. Approval does not establish production Cognito configuration, operational monitoring, or deployment readiness.

### Functional Requirements

- **FR-001 (SR-090)**: The Operations Web MUST require authentication before displaying protected operational workspace content. It MUST initiate authentication through Cognito managed login using OAuth 2.0 Authorization Code with PKCE. Authentication credentials MUST be owned by Cognito; the React application MUST NOT collect or validate staff passwords itself.
- **FR-002 (Identity contract)**: After the approved Cognito flow supplies an access token, the Operations Web MUST obtain the current caller identity only through the existing authenticated `GET /api/me` contract. It MUST NOT send a caller-selected subject or assume any additional Identity endpoint.
- **FR-003 (SR-090, SR-091)**: The Operations Web MUST use only the `roles` returned by the current `/api/me` response to select role-aware presentation. It MUST NOT grant a role from Cognito group claims, URL/query values, stale client data, or a user-editable value. An empty roles array MUST provide no Controller or Station Staff workspace access.
- **FR-004 (SR-038, SR-091)**: A returned `Controller` role MUST allow entry to the Controller workspace shell. A returned `Station Staff` role MUST allow entry to the Station Staff workspace shell. Passenger, Driver, empty, or unrecognized role sets MUST NOT grant staff workspace access. A role not returned by `/api/me` MUST NOT be inferred. This feature MUST NOT add live dashboard data or business-workflow navigation that belongs to another feature.
- **FR-005 (SR-046, SR-091)**: A caller whose returned role set does not include `Controller` MUST NOT receive Controller-only presentation affordances. Direct route access or a crafted client request MUST NOT be treated as authorized; every protected backend operation remains subject to server-side role and target-resource authorization. This feature MUST NOT implement cancellation or another controller-only business operation.
- **FR-006 (SR-091)**: The Operations Web MUST NOT treat a client-selected station, journey, role, or resource identifier as proof of authorization. It MUST present only data returned by an owning backend feature after that feature's authorization decision. This feature MUST NOT create or call an operational endpoint to test scope.
- **FR-007 (SR-089, SR-090)**: A person without an authenticated application session MUST be unable to view protected operational content. The application MUST provide an accessible sign-in entry and return to it when a protected route is requested without a valid application session.
- **FR-008 (SR-090, SR-114)**: When a protected request is rejected for missing, malformed, invalid, or expired credentials, the Operations Web MUST NOT present the request as successful or expose protected response data. It MUST end protected application access and provide a clear path to authenticate again without automatically replaying a protected action.
- **FR-009 (SR-091, SR-100, SR-114)**: When a protected operation returns `403`, the Operations Web MUST show a safe access-denied state and MUST NOT reveal protected resource details or present a side effect as successful. When the identity contract or a protected operation returns `503`, it MUST grant no access and show a distinct temporary-unavailability state with a safe retry path.
- **FR-010 (SR-100, SR-114)**: The Operations Web MUST show understandable loading, no-role/no-data, access-denied, service-error, offline, and retry states for this feature's primary entry and identity views. Error messages and client diagnostics MUST NOT expose access tokens, credentials, private claims, or unrestricted backend diagnostics.
- **FR-011 (SR-093)**: The application MUST NOT store access tokens in plaintext application storage or logs. On session expiry it MUST stop protected access until the user completes Cognito managed login using OAuth 2.0 Authorization Code with PKCE and a valid session is established. Specific client libraries, OAuth configuration values, credential storage, and token handling implementation belong to technical planning. The feature does not require silent renewal or global token revocation.
- **FR-012 (Sign-out)**: The Operations Web MUST provide a sign-out action that removes its authenticated application state and makes protected routes inaccessible in that application. It MUST NOT claim that a local sign-out revokes an already-issued bearer token or Cognito-managed browser session unless that behavior is defined by the approved Cognito flow.
- **FR-013 (Accessibility)**: All sign-in, workspace-entry, denial, error, retry, and sign-out controls MUST be operable by keyboard, have accessible names, use visible and predictable focus, and announce important state changes to assistive technology. Meaning MUST NOT depend on color alone.
- **FR-014 (Scope boundary)**: This feature MUST NOT create sign-up, password-reset, user-management, role-management, station-scope-management, or other identity endpoints. It MUST NOT implement operational business workflows or call Journey Feature 003 APIs.

### Key Entities

- **Staff identity projection**: The current caller's opaque subject and zero or more recognized role labels returned by the `/api/me` contract. It contains no profile or station-scope data.
- **Application session**: The authenticated state held by the Operations Web for the current user interaction. Its token lifecycle and browser storage behavior depend on the approved Cognito client flow.
- **Role-aware workspace**: The protected Operations Web shell and presentation available for a returned Controller or Station Staff role. It grants no backend permission by itself.
- **Resource authorization decision**: The owning backend feature's allow, deny, or unavailable outcome for a protected action and target resource. The browser does not create or override this decision.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: In the approved authentication test cases, 100% of identities with a current Controller mapping reach the Controller shell, and identities with a current Station Staff mapping reach the Station Staff shell.
- **SC-002**: 100% of unauthenticated attempts to protected Operations Web routes display no protected content.
- **SC-003**: 100% of empty-role, insufficient-role, out-of-scope, or unavailable-authority cases grant no additional client access; denied backend operations return no protected state change.
- **SC-004**: 100% of invalid or expired session cases stop protected application access and present a clear authentication recovery path without treating a protected action as successful.
- **SC-005**: 100% of sign-out checks prevent protected content from being reopened through direct navigation or browser back navigation in the signed-out application session.
- **SC-006**: All primary entry and identity views have verified loading, no-role/no-data, `401`, `403`, `503`, service-error, offline, and retry states; the outcomes are distinguishable without color alone.
- **SC-007**: All sign-in, workspace navigation, access-denied, retry, and sign-out controls can be completed with keyboard-only input and expose meaningful names and state changes to assistive technology.
- **SC-008**: No tested client error output or application diagnostic contains an access token, credential, or private token claim.

## Assumptions

- Cognito remains the approved authentication credential provider, and the API continues to accept validated Cognito access tokens as defined by the current Feature 002 implementation and OpenAPI contract.
- The active SmartRail role mapping, not Cognito group membership, is authoritative for roles. The backend checks the current mapping and applicable resource evidence for protected operations.
- `/api/me` is the only identity HTTP contract available to this feature. It returns current role labels but no station/resource scopes; the Station Staff shell remains generic until an approved owning-feature response supplies station context.
- The Operations Web shell does not include a live operations overview, journey details, incidents, status changes, publication, cancellation, or other operational workflows. Their applicable requirements remain owned by later feature specifications.
- Cognito managed login using Authorization Code with PKCE is the approved browser sign-in choice. Environment-specific app-client and callback/logout configuration must be supplied by the identity owner. The feature does not promise silent token renewal or global token revocation; expiry requires safe reauthentication, and sign-out removes access from the Operations Web.
- The sibling Feature 002 implementation checklist records backend identity, Cognito validation, authorization matrix, safe error, and API evidence. It also marks client-side SR-093 and SR-114 evidence open. These recorded results are not a substitute for React client acceptance.
- Backend Feature 002 is approved for client integration as of 2026-10-19. Production Cognito environment configuration, operational monitoring, and deployment remain external dependencies and are not implied by that approval.

## Authentication and Authorization Sources Reviewed

- Operations Web authority: `../../.specify/memory/constitution.md`.
- Approved platform requirement source: `../../docs/SmartRail-SDD-Handoff/evidence/requirements.xlsx`, `FINAL UNIFIED REQUIREMENTS` sheet; relevant exact wording and acceptance checks are reproduced above.
- RBAC and endpoint inventory: `../../docs/SmartRail-SDD-Handoff/evidence/rbac-api-contracts.pdf` and its transcription in `../../docs/SmartRail-SDD-Handoff/handoff.md`.
- Backend Feature 002 specification and decisions: `../../../SmartRail/specs/002-identity-permissions/spec.md` (Approved for client integration on 2026-10-19), `research.md`, `authorization-boundary.md`, and `plan.md`.
- Backend OpenAPI: `../../../SmartRail/specs/002-identity-permissions/contracts/identity-permissions.openapi.yaml`.
- Backend authentication and current identity implementation: `../../../SmartRail/src/SmartRail.Api/Authentication/`, `../../../SmartRail/src/SmartRail.Api/Endpoints/IdentityEndpoints.cs`, `../../../SmartRail/src/SmartRail.Api/Contracts/Identity/CurrentIdentityResponse.cs`, and API authorization middleware/handler.
- Backend tests and recorded evidence: `../../../SmartRail/tests/SmartRail.IntegrationTests/Identity/` (including `CurrentIdentityEndpointTests`, `IdentityOpenApiTests`, `CognitoJwtValidationTests`, `AuthorizationEndpointTests`, `AuthorizationMatrixTests`, and `AuthenticationPrivacyTests`) and the implementation verification table in Feature 002's checklist.
- React project inspection: the current `src/App.tsx` and `src/main.tsx` are the default Vite starter; `package.json` contains React/Vite dependencies but no authentication or routing package. No application behavior is assumed from the starter.
