# Research: Staff Authentication & Role-Aware Operations Foundation

**Feature**: [spec.md](spec.md)
**Date**: 2026-10-19

## Decisions

### 1. React Router Declarative mode

**Decision**: Use `react-router` Declarative mode with a browser router, nested public/protected route elements, and role-specific presentation shells.

**Rationale**: The existing app is a client-rendered React 19 + TypeScript + Vite SPA with no router installed. The feature needs URL matching, direct-navigation handling, and nested shells; it does not need server rendering, data loaders, route actions, or a framework runtime. Route protection remains a UX control only.

**Alternatives considered**: React Router Data/Framework modes (unneeded server/data abstractions for this shell); hand-built `window.location` routing (duplicates history and nested route behavior); TanStack Router (capable but adds another router and no needed capability beyond the selected declarative mode).

**References**: [React Router declarative installation for Vite](https://reactrouter.com/start/declarative/installation), [routing modes](https://reactrouter.com/start/modes), [declarative routing](https://reactrouter.com/start/declarative/routing).

### 2. Cognito managed login with `react-oidc-context` and `oidc-client-ts`

**Decision**: Use `react-oidc-context` as the sole authorization-response callback processor and `oidc-client-ts` as its protocol client for Authorization Code + PKCE S256, state validation, and access-token expiry events. The `/auth/callback` React route only presents provider processing/results and never processes the response or exchanges the code a second time. Explicitly disable automatic silent renewal and token revocation on sign-out. Replace the default token-bearing user store with in-memory storage. Preserve only short-lived per-tab authorization transaction state/PKCE verifier across the full-page redirect; it must not contain access, ID, or refresh tokens and must be cleared after processing or failure. Clean the callback URL after success and failure so code/state query values are not retained in browser history.

**Rationale**: This is a browser OIDC flow already selected by the product and does not require any SmartRail login/logout endpoint. The library has React integration and TypeScript protocol support. Its defaults require care: `oidc-client-ts` enables automatic silent renewal by default in current settings and its browser `UserManager` default user store uses `sessionStorage`. The plan overrides these defaults to meet the specification's explicit no-persistent-token and no-silent-renew boundaries.

**Alternatives considered**: AWS Amplify Auth supports Cognito managed redirects and custom memory storage, but is a broader AWS-specific abstraction; its documented default token storage is localStorage, so it too would require explicit storage replacement. Hand-built OAuth is rejected because state validation, PKCE, callback parsing, token lifecycle, and logout are security-sensitive protocol behavior. No browser package supersedes backend authorization.

**Implementation validation**: Verify the chosen release and React 19 compatibility, ensure PKCE method S256, state transaction persistence across redirect, missing/invalid/replayed state denial, exactly one code exchange, no tokens in persistent stores, `automaticSilentRenew=false`, `revokeTokensOnSignout=false`, callback URL cleanup, user cancellation, and sign-out/local-back behavior. The code verifier transaction may occupy per-tab session storage only as temporary OAuth state, never as a bearer token.

**References**: [oidc-client-ts docs and Cognito notes](https://github.com/authts/oidc-client-ts/blob/main/docs/index.md), [UserManager settings and default stores](https://github.com/authts/oidc-client-ts/blob/main/src/UserManagerSettings.ts), [react-oidc-context callback behavior](https://github.com/authts/react-oidc-context/blob/main/src/AuthProvider.tsx), [Cognito authorization endpoint](https://docs.aws.amazon.com/cognito/latest/developerguide/authorization-endpoint.html), [Cognito PKCE](https://docs.aws.amazon.com/cognito/latest/developerguide/using-pkce-in-authorization-code.html), [Cognito callback/logout app-client settings](https://docs.aws.amazon.com/cognito/latest/developerguide/user-pool-settings-client-apps.html).

### 3. Native `fetch`, runtime parsing, React Context and `useReducer`

**Decision**: Keep networking in a typed adapter using native `fetch`, `AbortController`, an environment-supplied API base URL, and an explicit runtime parser for the small `/api/me` response. Use React Context + `useReducer` for app-level identity view states. Do not add a general state/query library for this feature.

**Rationale**: Only one identity GET exists in the approved contract. Native browser APIs provide cancellation and timeout without extra dependencies. The identity response is two fields with a closed role-label enum, so an explicit guard is simpler than a new schema package. React's reducer/context cover loading, authenticated, no-access, expired, denied, unavailable, and error UI without duplicating the OIDC library's token/session store.

**Alternatives considered**: Axios (extra dependency without needed interceptors or transformation); generated OpenAPI client (single endpoint does not justify generation pipeline); Zod (a hand-written parser suffices for a fixed two-field shape); Redux/Zustand/TanStack Query (too broad for one noncached identity query; reconsider when operational dashboard data enters scope).

**References**: [React `useReducer`](https://react.dev/reference/react/useReducer), [React `createContext`](https://react.dev/reference/react/createContext), [MDN Fetch API](https://developer.mozilla.org/en-US/docs/Web/API/Fetch_API), [MDN AbortSignal.timeout](https://developer.mozilla.org/en-US/docs/Web/API/AbortSignal/timeout_static).

### 4. Vitest, Testing Library, MSW and Playwright

**Decision**: Use Vitest with jsdom for unit/component tests, React Testing Library + `user-event` for user-visible interaction, MSW for `/api/me` request/response conditions, and Playwright for cross-browser history, callback, keyboard, and back-after-sign-out checks.

**Rationale**: Vitest shares Vite's transform/config ecosystem. Testing Library asserts through accessible UI. MSW intercepts actual network requests so tests exercise the same fetch adapter as the app. Playwright covers real browser history and full-page navigation behaviors that DOM-only component tests cannot establish.

**Alternatives considered**: Jest (works but duplicates setup in this Vite project); direct `fetch` stubs (more tightly coupled and less representative than network-level MSW handlers); component tests alone for navigation (cannot prove browser history behavior); treating mocked OIDC as real Cognito acceptance (rejected).

**References**: [Vitest guide](https://vitest.dev/guide/index.html), [Vitest features/config sharing](https://vitest.dev/guide/features), [React Testing Library](https://testing-library.com/docs/react-testing-library/intro/), [Testing Library user-event](https://testing-library.com/docs/user-event/intro/), [MSW docs](https://mswjs.io/docs/), [Playwright Test](https://playwright.dev/docs/intro).

## External Decisions and Constraints

- Backend Feature 002 is approved for client integration; its authoritative contract is `../../../SmartRail/specs/002-identity-permissions/contracts/identity-permissions.openapi.yaml`, with only `GET /api/me` for identity.
- Cognito production issuer/domain, public app-client ID, redirect/logout URIs, approved scopes, API URL, CORS and hosting configuration are not supplied. No production values or credentials are invented here.
- Cognito managed-login remote logout is a browser navigation to `/logout` using `client_id` and an app-client-registered allowed `logout_uri`; the environment owner must provide those values. The client clears its own session before navigation and keeps local access closed if remote navigation fails. No access-token revocation is claimed. See [Amazon Cognito's logout endpoint contract](https://docs.aws.amazon.com/cognito/latest/developerguide/logout-endpoint.html).
- The contract returns role labels, not station assignments. Keep the initial Station Staff shell generic; backend scope checks remain authoritative.
- Backend Feature 002's previous verification is recorded in its quickstart/checklist. Planning inspected that evidence; no backend tests were run as part of this work.
- Live Cognito/backend acceptance is `BLOCKED` pending configured environment/test users and remains `NOT RUN` until it is executed. Contract mocks are not equivalent evidence.
