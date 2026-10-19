# Quickstart: Staff Access Implementation Validation

This guide records Milestone A foundation checks for Feature 001. The current application shell is not an authentication or operational workspace implementation; the user-story tasks remain outstanding.

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

`test` runs Vitest and `test:e2e` runs Playwright. Playwright configuration is present, but no browser end-to-end tests exist yet; `npm run test:e2e -- --list` reports zero tests. No live browser, Cognito, or backend acceptance was run.

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

Full automated result: `npm run test -- --run` — 3 test files, 20 tests passed. These are local unit/component checks, not live authentication or backend acceptance. The root shell intentionally contains no login, callback processor, protected workspace, or business operations.

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

Current planning evidence: all implementation and client checks are `NOT RUN`; real Cognito/backend acceptance is `BLOCKED` until environment and test-user configuration is supplied. Backend Feature 002's prior test evidence is recorded in the sibling backend quickstart/checklist and was not rerun during this planning task.

