# Quickstart: Staff Access Implementation Validation

This guide records completed Milestones A and B and the completed client-presentation portion of Milestone C for Feature 001. Milestone C adds the generic Station Staff shell, role-derived navigation, and route protection. Station-scope backend-denial evidence, sign-out, later recovery work, live Cognito/backend acceptance, and production readiness remain outstanding.

## Prerequisites

- The approved implementation phase installed the plan-selected React Router, OIDC libraries, Vitest/jsdom, Testing Library, MSW, and Playwright dependencies. No environment credentials or API behavior are embedded in the repository.
- For deterministic client tests, use mocked OIDC callbacks and MSW handlers that match [the approved identity client mapping](contracts/identity-client.md). Label mocked auth as `PASS` only for the named client check, never as real Cognito integration.
- For live acceptance, obtain environment-specific Cognito issuer/domain, public app-client ID, callback/logout URIs, approved scopes, SmartRail API base URL/CORS, and controlled Controller and Station Staff test identities from their owners. No production values or secrets are included here.
- Station/resource-scope acceptance is blocked until the owning backend feature supplies an existing approved protected-operation contract and controlled fixtures for allowed, out-of-scope, missing, revoked, stale, and unavailable scope evidence. Feature 001 does not define or invent this operation.

## Planned commands

Available implementation checks:

```powershell
npm run lint
npm run build
npm run test -- --run
npm run test:e2e
```

`test` runs Vitest and `test:e2e` runs Playwright. Playwright uses only local placeholder runtime configuration and a controlled local OIDC/API fixture. No live Cognito or SmartRail backend acceptance was run.

## Milestone A evidence (T001–T012)

| Check | Result | Evidence |
|---|---|---|
| Runtime configuration parsing and rejection of client-secret keys | PASS | `npm run test -- --run`; 5 runtime config tests passed. |
| Safe API failure classification and message redaction | PASS | `npm run test -- --run`; 7 safe-error tests passed. Fetch helper uses a 15-second AbortController timeout; no live endpoint was called. |
| Shared accessible loading, denial, unavailable, offline, and retry states | PASS | `npm run test -- --run`; 8 component tests passed, including keyboard focus/Enter and text status. |
| Vitest/MSW setup and fixture loading | PASS | Full Vitest run loaded setup and MSW server. |
| Application build | PASS | `npm run build` completed TypeScript build and Vite production bundle. |
| Lint | PASS | `npm run lint` completed successfully. |
| Playwright browser tests | NOT RUN | Config discovery was checked; no E2E test files exist in Milestone A. |
| Cognito, backend, station-scope, and production acceptance | BLOCKED | Requires external environment configuration, controlled identities, and approved owning-feature scope operation/fixtures as described below. |

Milestone A result at completion: `npm run test -- --run` — 3 test files, 20 tests passed. The foundation shell at that point intentionally had no login, callback processor, protected workspace, or business operations.

## Milestone B evidence (T013–T024)

| Check | Result | Evidence |
|---|---|---|
| OIDC config, PKCE S256 request, safe return paths, no silent renewal/revocation | PASS | Full Vitest run; config uses the Cognito issuer with managed-login authorization/token endpoints, and the installed OIDC library generated the S256 challenge. |
| Memory token store and temporary redirect transaction | PASS | Unit/browser assertions confirmed the token-bearing user store is in-memory, no client secret is configured, redirect state is removed after callback, and no token was written to localStorage/sessionStorage. |
| Strict current identity parser | PASS | Full Vitest run validated exact `{ subject, roles }`, opaque non-empty subject, unique recognized roles, and empty role array. |
| `GET /api/me` adapter and lifecycle | PASS | MSW tests verified bearer header, valid Controller identity, 401, 503, timeout, offline, malformed 200, and stale response isolation after sign-out/session replacement. No 403 response was added to the identity fixtures or contract. |
| Controller route and callback behavior | PASS | Component and local-fixture browser tests verified role-gated direct navigation, successful and rejected callbacks, consumed-state replay denial, callback URL cleanup, one code exchange, and expiry closing protected content. |
| Vitest | PASS | `npm run test -- --run` — 7 test files, 66 tests passed. |
| Playwright | PASS | `npm run test:e2e -- --workers=1` — 8 Chromium tests passed against the local OIDC/API fixture. This is mocked protocol evidence only. |
| Lint and build | PASS | `npm run lint` and `npm run build` completed successfully after Milestone B implementation. |
| Live Cognito/backend and production acceptance | BLOCKED | Requires owner-supplied Cognito/API environment, registered URLs/scopes, controlled real test identities, hosting/CORS/TLS and production operations setup. |

The browser suite verifies the OIDC library's protocol behavior against a local fixture; it does not contact Cognito, prove the production app-client configuration, or count as live backend acceptance. The API response is intercepted by Playwright/MSW fixtures and does not establish production integration.

## Milestone C evidence (T025–T031)

| Task/check | Result | Evidence |
|---|---|---|
| T025 Station Staff shell and role-union tests | PASS | Component tests cover Station Staff-only navigation, no Controller affordance without the Controller role, union navigation for both roles, active navigation, and keyboard operation. |
| T026 resource-scope presentation boundary | PASS | Component tests verify URL-selected station/resource values do not appear as assignments or grant the Station Staff route; Passenger-only and Controller-only identities are denied. A loading identity shows no protected shell, and removing Station Staff from the current role set removes the shell. |
| T027 direct URL browser tests | PASS | Local Playwright tests verify unauthenticated direct Station Staff and Controller URLs do not reveal workspace content; selected station/resource query values reveal no assigned scope. |
| T027 owning-operation backend-denial browser test | BLOCKED | No approved protected operational operation or controlled out-of-scope fixture is available in Feature 001's contract set. No endpoint or operation was invented; this portion remains open with T051. |
| T028–T030 generic shell, role mapping, and route guard | PASS | `npm run build` and the full test suites passed. Role affordances derive from current `/api/me.roles`; routes require their exact staff role and loading/changed role states fail closed. |
| Vitest | PASS | `npm run test -- --run` — 9 test files, 75 tests passed. |
| Playwright | PASS | `npm run test:e2e -- --workers=1` — 10 Chromium tests passed against local OIDC/API fixtures. These are client presentation/protocol checks, not live backend resource authorization. |
| Lint and build | PASS | `npm run lint` and `npm run build` completed successfully after Milestone C changes. |
| Live Cognito/backend and production acceptance | BLOCKED | Requires owner-supplied environment configuration, controlled real test identities, CORS/TLS/hosting setup, and production monitoring/deployment readiness. |

The Station Staff shell is generic and displays no assigned station or live operational data. Role-derived navigation and route guards control presentation only. Every future protected operation still requires backend authorization; this milestone makes no station-scoped API call and does not establish resource-scope acceptance.

## Acceptance checks

1. In-memory component mocks cover managed-login UI and error states. Browser tests use an isolated local authorization-server fixture or controlled protocol harness to exercise redirect/callback state and PKCE S256 challenge/exchange success and mismatch, missing/invalid/replayed callback state, exactly one callback processor/code exchange, cancelled/failed login, callback URL cleanup on success/failure, and expired-session reauthentication. Neither is real Cognito acceptance.
2. MSW `/api/me` cases cover Controller, Station Staff, multiple staff roles, empty roles, unknown/malformed response, `401`, `503`, timeout, and offline retry. Verify no `403` is attributed to `/api/me` itself.
3. Route/component tests cover no token, direct protected-route navigation, Controller-only affordances, generic Station Staff shell, union presentation for multiple staff roles, no operational role, stale role/session, sign-out, and no protected content on browser back.
4. Storage/log checks prove access/ID/refresh tokens are absent from localStorage, sessionStorage, application cookies, caches, URLs, and diagnostics. Verify that only temporary OAuth transaction state/PKCE verifier spans the authorization redirect, is validated and cleared after callback handling; a full page reload with no in-memory session requires reauthentication; expiry closes protected content; and silent renewal is disabled.
5. Sign-out tests verify that application state, cached protected data, and active transaction state are cleared before remote logout navigation. When Cognito logout configuration is present, assert HTTPS `/logout` navigation uses the configured `client_id` and registered allowed `logout_uri`. Simulate remote-navigation failure and verify local protected access remains closed. Do not assert or claim access-token revocation.
6. Accessibility checks use keyboard-only navigation and accessible names/focus/state announcements; manually verify representative sign-in, status, and error announcements with a screen reader; status meaning does not rely on color.
7. A separately gated real integration check uses configured Cognito managed login, a controlled Cognito user, real `GET /api/me`, and the configured backend. It is not satisfied by mocks.

## Evidence status vocabulary

- `PASS`: the named acceptance check ran against its specified boundary and passed; record command, environment, and evidence location.
- `FAIL`: the check ran and its expected outcome did not occur; record the failing assertion and safe diagnostic evidence.
- `BLOCKED`: execution cannot proceed because a prerequisite is missing, such as Cognito app-client/URI values, test identity, backend base URL/CORS, or deployment setup; name the owner/dependency.
- `NOT RUN`: the check has not been executed. Do not report a planned check as passed.

Current evidence: Milestones A and B and the client-presentation portion of Milestone C local automated checks are `PASS` as listed above. The owning-operation backend-denial portion of T027 remains `BLOCKED` pending an approved operation and controlled fixture. Later recovery/sign-out scope, accessibility manual review, and full feature acceptance remain `NOT RUN` or are tracked in their later task phases. Real Cognito/backend acceptance is `BLOCKED` until environment and controlled test-user configuration is supplied. Backend Feature 002 evidence was not rerun during this implementation milestone.

