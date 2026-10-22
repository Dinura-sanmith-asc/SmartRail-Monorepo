# Implementation Plan: Staff Authentication & Role-Aware Operations Foundation

**Branch**: `feature-01` (current Git branch at review) | **Date**: 2026-10-19 | **Spec**: [spec.md](spec.md)

**Plan Version**: 0.1.0 | **Plan Status**: Approved

**Input**: Clarified Feature 001 specification and SmartRail Operations Web constitution. Backend Feature 002 and its existing OpenAPI contract are approved for client integration as of 2026-10-19.

## Summary

Turn the Vite starter into a small, role-aware protected-entry application. Use React Router for presentation routing, `react-oidc-context`/`oidc-client-ts` for Cognito managed login using Authorization Code + PKCE, and a typed native `fetch` adapter for the single identity operation `GET /api/me`. Keep access tokens in memory only. Resolve the current roles through the backend identity response; derive the Controller and generic Station Staff navigation shells from those roles. The UI remains presentation-only: SmartRail backend authorization owns every protected resource and operation decision.

This plan excludes live operations data and workflows. It does not change backend contracts, provision Cognito/AWS, or claim that live Cognito integration has been tested.

## Technical Context

**Language/Version**: TypeScript 6.x with React 19, React DOM 19, and Vite 8 from the existing `package.json`; target modern browsers with Web Crypto, History, and Fetch support.

**Primary Dependencies**:
- `react-router` in Declarative mode for browser routing and nested role-aware shells.
- `react-oidc-context` with its `oidc-client-ts` peer for OIDC callback/session integration. Configure Authorization Code + PKCE S256; disable automatic silent renewal and token revocation on sign-out; override the token-bearing user store with volatile in-memory storage. Use a separate short-lived browser state store for the redirect transaction and PKCE verifier; it must not contain access, ID, or refresh tokens.
- Native `fetch` behind one typed `currentIdentityClient`; no API-client dependency is justified for one GET operation.
- React Context and `useReducer` for feature-level identity/workspace view state; no additional global state library.
- Vitest, jsdom, React Testing Library, `@testing-library/user-event`, MSW, and Playwright for deterministic unit/component/network/browser checks. These were installed during Milestone A; exact versions are recorded in `package.json` and `package-lock.json`.

**Storage**: No application database. Keep the OIDC user/access-token state in JavaScript memory only. Use a temporary per-tab redirect transaction store for state, nonce, and PKCE verifier across Cognito's full-page redirect; clear it after callback success, failure, cancellation recovery, or stale-transaction cleanup. Never persist bearer tokens in localStorage, sessionStorage, cookies, URLs, logs, or query caches.

**Testing**: Vitest + jsdom + Testing Library for reducer, parser, and accessible component behavior; MSW for actual `fetch` requests against the approved `/api/me` response/error contract; Playwright for browser history, callback routing, keyboard, and back-navigation flows using controlled identity/API fixtures. Real Cognito/backend integration is a separate configured-environment acceptance check, not a mocked test result.

**Target Platform**: Browser SPA served by the existing Vite development/build toolchain; production hosting and callback origins are environment-owned and not configured by this feature plan.

**Project Type**: React single-page web application.

**Performance Goals**: No numeric latency or availability target is approved. Make the identity request bounded (proposed client timeout: 15 seconds, a UI hang limit rather than an API SLO), display loading/unavailable states, and allow a user-initiated retry. Do not retry authentication failures or replay protected actions.

**Constraints**: Preserve the approved spec and backend OpenAPI. Cognito is the credential authority; only `/api/me` supplies role labels. `/api/me` does not return station scopes. Do not add endpoints, roles, station assignments, backend authorization behavior, silent renewal, global token revocation, or business workflows. No client secret or production endpoint may be committed. UI route checks never substitute for backend authorization.

**Scale/Scope**: One identity projection, four recognized backend role labels, two operational workspace shells, and shared authentication/recovery states. No operational dashboard, journey views, incidents, publication, cancellation, or station/resource data is included.

## Constitution Check

### Pre-design gate

- **I. Requirements and traceability — PASS**: The feature cites SR-038, SR-046, SR-089, SR-090, SR-091, SR-093, SR-100, and SR-114 with backend/client ownership distinguished. It does not claim the full SR-038 overview or SR-046 cancellation acceptance.
- **II. React architecture — PASS**: Responsibilities are separated into app initialization, routes, authentication/session, API integration, workspace presentation, shared accessible states, and configuration. Dependencies are limited to documented feature needs.
- **III. Identity and least privilege — PASS**: Current backend roles drive presentation; backend role/resource policies remain authoritative. Tokens are not persisted and expiry closes protected application access.
- **IV. Railway data integrity — PASS**: No authoritative train/journey/status data or operational workflow is added.
- **V. Approved backend integration — PASS**: Use the approved Feature 002 contract and its exact `GET /api/me` surface. No production address is hardcoded and no unsupported API is introduced.
- **VI. Operational safety — PASS**: The client does not perform protected railway operations; route visibility is never treated as authorization.
- **VII. User experience and accessibility — PASS**: Loading, denial, expiry, failure, recovery, keyboard, focus, and assistive-technology states are planned.
- **VIII. Quality and verification — PASS with external integration gate**: Mocks are isolated and explicitly not live integration. Real Cognito acceptance remains blocked until the project owner supplies environment configuration and test identities.
- **IX. Spec-driven development — PASS for planning**: This plan follows the clarified Feature 001 spec. No implementation tasks or code changes are part of this phase.

**Gate result**: No constitution violation is proposed. Production Cognito and deployment setup remain external dependencies; they are not inferred from backend Feature 002 approval.

## Architecture and Data Flow

1. `main.tsx` initializes the React root, runtime configuration validation, OIDC provider, and browser router.
2. Public entry renders a sign-in action. Selecting it begins Cognito managed login; the SPA never collects staff passwords.
3. Cognito returns to the configured callback URI. Exactly one callback processor owns authorization-response parsing and code exchange: the `react-oidc-context` provider handles the callback; the `/auth/callback` React route only displays provider processing/result state and MUST NOT invoke a second exchange. The provider/library validates transaction state and PKCE exchange. Missing, invalid, and replayed state fail closed. Remove code/state query values from the visible URL on success and failure before rendering a recovery state.
4. The auth provider holds the access token in memory. The app calls `GET {apiBaseUrl}/api/me` with `Authorization: Bearer <access token>` and validates the response at runtime.
5. A valid `200 { subject, roles }` response creates the feature identity projection. Controller and Station Staff affordances are derived only from recognized returned role labels. Multiple roles expose the union of their mapped presentation affordances. An empty or non-staff role set enters the no-operational-access state.
6. A token `401` clears protected application state and requires managed-login reauthentication without automatically repeating an operation. A `/api/me` `503` enters authority-unavailable state; it is not converted to an empty role list. A protected-operation `403` is shown as safe access denied. No backend operational action is added in this feature.
7. Sign-out first clears in-memory identity, token, protected route state, caches, and active OAuth transaction state. If remote managed-login logout is configured, navigate to Cognito's HTTPS `/logout` endpoint with the configured `client_id` and URL-encoded, app-client-registered `logout_uri`. If that navigation cannot be initiated or fails, local protected access remains cleared and the user stays on the signed-out entry. Do not claim access-token revocation. Browser back/direct navigation rechecks the local session and must not render cached protected content.

## Session and Route Design

Proposed routes: `/` unauthenticated entry, `/auth/callback` callback status route (not a second callback processor), `/controller/*` Controller shell, `/station-staff/*` generic Station Staff shell, and a safe not-found/denied route. These are client routes only; they do not imply corresponding backend endpoints. Nested protected route elements wait for auth initialization and identity loading before rendering protected shell content. Preserve and restore only validated same-origin in-app return paths.

| State | Entry condition | UI/access result |
|---|---|---|
| Unauthenticated | No in-memory OIDC user | Sign-in entry; protected routes show no protected content |
| Redirecting/callback processing | Managed-login redirect or callback pending | Accessible progress state; no protected shell |
| Identity loading | Valid access token; `/api/me` pending | Accessible progress state; no role guessed |
| Controller | `/api/me` includes `Controller` | Controller shell affordances; backend checks every API action |
| Station Staff | `/api/me` includes `Station Staff` | Generic Station Staff shell; no station inferred |
| Multi-role staff | Both staff labels returned | Union of the two mapped navigation affordance sets |
| No operational access | Empty, Passenger/Driver-only, or no recognized staff label | Safe no-access page; no restricted route or content |
| Unauthenticated/expired | Callback failure or protected request 401 | Clear memory state; explain reauthentication; do not replay protected work |
| Forbidden | Protected operation returns 403 | Safe denial, no sensitive resource detail or success claim |
| Authority unavailable | `/api/me` or protected API returns 503 | Distinct temporary-unavailable state and safe retry; not an empty role set |
| Service/offline | Timeout, network, or other service failure | Safe error/offline state; user-initiated retry preserves authorization |
| Signing out | User activates sign-out | Clear local protected state immediately; configured remote logout if available |

## API Client and Configuration

`currentIdentityClient` owns base-URL joining, bearer-header injection from the in-memory OIDC session, `AbortController` timeout/cancellation, HTTP classification, and runtime parsing. Accept only the OpenAPI identity shape: object with exactly `subject` (non-empty string) and `roles` (unique array of `Passenger`, `Driver`, `Station Staff`, `Controller`). Treat malformed/unknown fields or labels as invalid data and grant no additional UI access. Empty roles are a valid identity result; `503` is unavailable, not empty. Do not log request headers, tokens, claims, raw response bodies, or backend diagnostics.

Use environment-provided, non-secret Vite values such as `VITE_SMARTRAIL_API_BASE_URL`, Cognito issuer, managed-login domain, app-client ID, callback URI, logout URI, and approved scopes. Vite-exposed values are public configuration, never secrets. Production values and actual registered redirect/logout URLs must be supplied and verified by the Cognito/backend environment owner. Do not commit `.env` secrets or placeholder values that look deployable.

## Dependency and Structure Decision

Use React Router Declarative mode: this application needs route matching and nested protected shells, not server loaders/actions or framework mode. Use `react-oidc-context` backed by `oidc-client-ts`: the provider is the single callback processor; the React callback route is presentation-only and never exchanges a code. Override its default token-bearing browser user store with memory storage, retain only short-lived OIDC transaction state in per-tab storage, and explicitly disable automatic silent renewal and token revocation on sign-out. For Cognito remote logout, use managed login's `/logout?client_id=...&logout_uri=...` with a registered allowed sign-out URL; generic OIDC `post_logout_redirect_uri` behavior is not assumed. Local logout cleanup precedes remote navigation and remains effective if navigation fails. Verify S256, missing/invalid/replayed state, callback cleanup, logout parameters/allowlist behavior, local fallback, React 19 compatibility, and storage behavior in implementation tests before relying on a library version.

Use native `fetch` instead of Axios or a generated client for one GET endpoint; use a small runtime parser rather than a schema dependency for the two-field response. Use React Context/`useReducer` instead of Redux, Zustand, or TanStack Query for the limited auth/workspace state. Add no UI kit; semantic HTML and shared CSS components are sufficient for this shell. Vitest aligns with Vite; Testing Library focuses assertions on user-visible behavior; MSW intercepts network requests without replacing the fetch client; Playwright covers real browser history and keyboard behavior.

### Proposed source structure

```text
src/
├── app/
│   ├── App.tsx
│   ├── router.tsx
│   └── providers.tsx
├── config/
│   └── runtimeConfig.ts
├── features/
│   └── staff-access/
│       ├── auth/             # OIDC provider/config, callback, session actions
│       ├── identity/         # GET /api/me adapter, response parser, identity state
│       ├── routes/           # protected route elements and role route selection
│       └── workspaces/       # Controller and generic Station Staff shells
├── shared/
│   ├── api/                  # bounded fetch and safe error classification
│   └── ui/                   # loading, error, denial, retry, accessible status
├── main.tsx
└── styles/
    └── index.css
tests/
├── unit/                     # parser, state reducer, role affordance mapping
├── components/               # Testing Library + MSW scenarios
└── e2e/                      # Playwright browser callback/history/keyboard flows
```

This is a proposed feature boundary, not a requirement to create every folder if a simpler organization is clearer during task decomposition.

## Verification Strategy and Requirement Traceability

### User stories, functional requirements, and success criteria

| Spec area | Planned checks |
|---|---|
| User Story 1 — Controller entry | Successful mocked/code+PKCE callback, current `/api/me` Controller response, protected Controller shell, no unsupported business route (FR-001–004, FR-007; SC-001–002). |
| User Story 2 — Station Staff entry and scope | Station Staff-only shell, generic station context, dual-role union, empty/out-of-scope/missing/unavailable scope behavior delegated to backend (FR-002–006; SC-001, SC-003). |
| User Story 3 — Safe access feedback | No session, callback cancellation/failure, malformed/expired credential, 401 reauthentication, 403 denial, 503 authority unavailable, network/service failure, retry and no-role state (FR-007–011; SC-002–004, SC-006–008). |
| User Story 4 — Sign-out | Clear memory state, close protected routes, direct URL and browser-back checks, configured Cognito logout navigation only (FR-008, FR-011–013; SC-004–005, SC-007–008). |

This maps the full 14-FR and eight-SC set across stories and checks. SR-038 dashboard and SR-046 cancellation remain explicit boundary-only coverage.

| Requirement | Planned verification evidence |
|---|---|
| SR-038 | Role shell navigation tests only; explicitly do not claim live operations overview acceptance. |
| SR-046 | Verify Station Staff/non-Controller presentation omits Controller-only affordances and direct route gives no access; backend cancellation authorization remains separately owned. |
| SR-089 | Unauthenticated protected-route test renders no protected data; public backend reads remain outside this client feature. |
| SR-090 | Isolated browser authorization-server fixture exercises code+PKCE/S256 challenge and exchange, one callback processor, missing/invalid/replayed state, cancellation, callback failure, missing/invalid/expired token, and 401 reauthentication. This fixture is not Cognito acceptance. |
| SR-091 | Controller, Station Staff, multi-role, empty, unknown role and stale route tests; assert UI never treats route state as backend authorization or station scope. |
| SR-093 | Inspect browser storage and logs during simulated auth to prove access/ID/refresh tokens are absent from localStorage, sessionStorage, application cookies, caches, URLs, and diagnostics; verify only temporary OAuth transaction state spans redirect, a full reload without in-memory session requires reauthentication, expiry closes protected content, and silent renewal is disabled. |
| SR-100 | Inject sensitive token/claim/provider details into failures and captured diagnostics; assert user output/logs are safe and redacted. |
| SR-114 | Test loading, no-role/no-data, 401, 403, 503, service error, offline, retry, and failed sign-in states using accessible assertions. |

Detailed test matrix and status rules are in [quickstart.md](quickstart.md). Mocked auth/API checks do not count as live Cognito/backend acceptance. The latter stays `BLOCKED` until environment values and controlled test identities exist; until then report it as `NOT RUN` with that blocker. `PASS`/`FAIL` apply only to checks actually executed, and no test result is asserted in this plan.

## External Dependencies and Readiness

| Dependency | Current state | Needed for |
|---|---|---|
| Backend Feature 002 `/api/me` contract | Approved for client integration 2026-10-19 | Client adapter and contract-based mocks |
| Cognito issuer, domain, app-client ID, callback/logout URIs, scopes | Not supplied/configured in this web repository | Real managed-login acceptance |
| Cognito public SPA app client with no client secret and PKCE S256 enabled | Environment owner configuration required | Real callback/token integration |
| Approved SmartRail API base URL, CORS for web origin, TLS and deployment hosting/history fallback | Environment/deployment configuration outstanding | Live browser-to-backend acceptance |
| Controlled Controller and Station Staff test identities/current role mapping | External test-environment setup required | Positive real-role acceptance |
| Existing approved owning-feature protected operation and controlled scope fixtures covering allowed, out-of-scope, missing, revoked, stale, and unavailable station-scope evidence | No approved protected-operation contract/test target is identified in Feature 001's contract set; the owning backend feature must supply the operation and controlled fixtures | Server-side resource-scope acceptance; T051 remains blocked until supplied |
| Production monitoring and deployment setup | Not performed by this feature | Production readiness; outside this plan's acceptance |
| SR-093 and SR-114 client verification | Planned only | Client acceptance after implementation and test evidence |

## Post-design Constitution Check

- All selected libraries support the existing React/TypeScript/Vite boundary and are justified by this feature's actual needs; the Milestone A implementation installed the approved dependencies. No production configuration or credentials are committed.
- No auth token is intentionally persisted; transaction-only callback state is short-lived and contains no bearer token.
- No route or client state is treated as backend authorization; current role projection drives presentation only.
- OpenAPI, role mapping, and backend scope rules remain unchanged. The Station Staff workspace is generic because `/api/me` carries no scope.
- Every external integration or unexecuted verification remains explicit; no live Cognito test or production readiness is claimed.

**Post-design gate**: PASS for planning. Real Cognito and production deployment acceptance remain external and are not required to generate implementation tasks; they must remain marked blocked/not run until configured.

## Complexity Tracking

No constitution violations or additional architecture layers are proposed. The auth context/provider and one typed API adapter are feature-scoped; no general-purpose data/cache framework or backend service is introduced.

## Plan Approval Record

- **Feature**: 001-staff-access — Staff Authentication & Role-Aware Operations Foundation
- **Technical plan version**: 0.1.0
- **Plan status**: Approved
- **Approval authority**: SmartRail Project Owner
- **Approval date**: 2026-10-19
- **Specification status**: Approved on 2026-10-19; the specification and plan have both been reviewed and approved under Constitution Principle IX
- **Approved backend contract reference**: Backend Feature 002 [`identity-permissions.openapi.yaml`](../../../SmartRail/specs/002-identity-permissions/contracts/identity-permissions.openapi.yaml), approved for client integration on 2026-10-19
- **Recorded backend verification reference**: Backend Feature 002 [`quickstart.md`](../../../SmartRail/specs/002-identity-permissions/quickstart.md) and [`requirements.md`](../../../SmartRail/specs/002-identity-permissions/checklists/requirements.md); backend evidence was inspected and not rerun for this plan
- **Client verification evidence**: Not run; planned acceptance and evidence status are in [quickstart.md](quickstart.md)
- **Constitution ratification**: Operations Web Constitution v1.0.0 ratified by the SmartRail Project Owner on 2026-10-19
- **Outstanding dependencies**: Cognito environment/app-client configuration; API base URL, CORS, TLS and hosting; controlled role-mapped test identities; approved owning-feature protected operation for resource-scope acceptance; production monitoring and deployment
- **Approval boundary**: This plan approval authorizes the described client implementation scope only. It does not approve production deployment, production Cognito configuration, or full SmartRail platform acceptance.
