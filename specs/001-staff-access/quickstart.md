# Quickstart: Staff Access Plan Validation

This is a validation guide for the planned Feature 001 implementation. No application implementation or tests are claimed as complete by this document.

## Prerequisites

- Install project dependencies only during the separately approved implementation phase; this plan did not install packages.
- For deterministic client tests, use mocked OIDC callbacks and MSW handlers that match [the approved identity client mapping](contracts/identity-client.md). Label mocked auth as `PASS` only for the named client check, never as real Cognito integration.
- For live acceptance, obtain environment-specific Cognito issuer/domain, public app-client ID, callback/logout URIs, approved scopes, SmartRail API base URL/CORS, and controlled Controller and Station Staff test identities from their owners. No production values or secrets are included here.

## Planned commands

Commands after dependencies and test scripts are added during implementation:

```powershell
npm run lint
npm run build
npm run test -- --run
npm run test:e2e
```

The existing `package.json` currently defines `dev`, `build`, `lint`, and `preview` only. The `test` and `test:e2e` scripts are proposed; they are not currently runnable and no dependencies were installed for planning.

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

