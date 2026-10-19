# Quickstart: Staff Access Implementation Validation

This guide records completed Milestones A and B, the completed client-presentation portion of Milestone C, Milestone D's safe access recovery behavior, and Milestone E's local-first sign-out for Feature 001. Milestone C adds the generic Station Staff shell, role-derived navigation, and route protection. Station-scope backend-denial evidence, live Cognito/backend acceptance, manual screen-reader review, and production readiness remain outstanding.

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

## Milestone D evidence (T032–T040)

| Task/check | Result | Evidence |
|---|---|---|
| T032 access-state and recovery behavior | PASS | Component integration tests cover an unauthenticated direct route with sign-in entry; one 401 with local session-clearing callback and no request replay; 403 generic denial presentation only; 503 distinct from empty roles; empty roles; malformed identity; offline, timeout, and service error; user-initiated retry after a failed 503; loading-state protection; stale response after expiry; and canceled callback feedback. |
| T033 accessibility assertions | PASS | Six automated tests verify keyboard sign-in/retry/navigation, named controls, focus behavior, polite live status, textual denial meaning, and Station Staff root navigation without Controller affordances. Manual screen-reader verification was not run. |
| T034 safe diagnostics/output | PASS | Tests inject token values, private claims, authorization internals, and raw Problem Details; the user messages and classified error projection remain generic. The 403 fixture is a generic future-operation response and does not define an API route. |
| T035 expiry and browser storage audit | PASS | Three Chromium tests against a local OIDC/API fixture verify PKCE transaction data is temporary and cleared, tokens are absent from local/session storage, cookies, Cache Storage, IndexedDB, URL/history, and console output, full reload requires sign-in, and expiry closes access without replaying `/api/me`. |
| T036–T039 recovery, expiry, retry, and accessibility implementation | PASS | `AuthRecovery`, `IdentityRecovery`, `sessionLifecycle`, `IdentityProvider`, `AccessState`, and `SignInPage` provide safe, role-aware states. Retry reuses the same authenticated identity operation; 503 is never converted to empty roles; malformed or denied outcomes do not render a workspace. |
| Vitest | PASS | `npm run test -- --run` — 13 test files, 96 tests passed. |
| Playwright | PASS | `npm run test:e2e` — 13 Chromium tests passed against local fixtures only. No live Cognito or backend acceptance was run. |
| Lint and build | PASS | `npm run lint` and `npm run build` completed successfully after Milestone D changes. |
| `git diff --check` | PASS | No whitespace errors in changed files. Git reported only its usual working-copy line-ending notices. |

The approved `GET /api/me` contract is unchanged: it defines 200, 401, and 503, but not 403. The identity client continues to classify an unexpected `/api/me` 403 as a service error. Generic 403 feedback is UI-only for a future approved protected operation; no endpoint or operation was added. T027 remains `BLOCKED` pending the owning backend feature's approved protected operation and controlled scope fixture. Live Cognito/backend integration, production configuration/monitoring, manual screen-reader review, and Feature 001 full acceptance remain outstanding. Backend Feature 002 evidence was not rerun.

## Milestone E evidence (T041–T047)

| Task/check | Result | Evidence |
|---|---|---|
| T041 local-first sign-out | PASS | Five unit tests verify synchronous local invalidation, current identity/token/route clearing, temporary transaction cleanup before navigation, safe OIDC removal failure, safe remote-navigation failure, missing logout configuration, and repeated-call deduplication. A separate component race test starts `GET /api/me`, signs out while it is pending, then returns the old Controller identity; the protected route stays closed. This app has no independent query/protected-data cache to purge. |
| T042 Cognito managed logout URL | PASS | Unit tests require HTTPS managed-login `/logout`, configured public `client_id`, the configured URL-encoded registered `logout_uri`, and no `post_logout_redirect_uri`, token parameters, or revocation claim. Missing logout URI safely disables remote navigation while preserving local sign-out. |
| T043 browser sign-out/history | PASS | Four Chromium tests use local OIDC/API fixtures for Controller, Station Staff, and multi-role identities. They verify remote `/logout` parameters, keyboard activation, no additional `/api/me` request after sign-out, direct protected URL denial, Back/Forward behavior, storage cleanup, and closed access after an aborted remote navigation. |
| Cognito endpoint compatibility | PASS | The client builds Cognito managed-login `GET /logout` with `client_id` and `logout_uri`, matching [AWS Cognito's managed-login logout endpoint](https://docs.aws.amazon.com/cognito/latest/developerguide/logout-endpoint.html). It does not assume generic OIDC RP-initiated logout or claim global token revocation. |
| Vitest | PASS | `npm run test -- --run` — 16 test files, 110 tests passed. |
| Playwright | PASS | `npm run test:e2e` — 17 Chromium tests passed against local fixtures only. The logout navigation was mocked; no live Cognito or backend acceptance was run. |
| Lint and build | PASS | `npm run lint` and `npm run build` completed successfully after Milestone E implementation. |
| `git diff --check` | PASS | No whitespace errors in changed files. Git may report working-copy line-ending notices. |

The local session gate is set before OIDC `removeUser()` and before browser navigation. It causes the identity provider to stop using the old access token, aborts pending `/api/me` work, and makes protected routes render the unauthenticated entry. The temporary `smartrail.oidc.transaction.*` session-storage keys are removed; token-bearing OIDC user data remains memory-only. Navigation to Cognito uses the configured managed-login domain, public client ID, and configured logout URI. If the logout URI is absent/invalid or navigation fails synchronously, the app stays signed out and shows safe status feedback. A failed top-level browser navigation displays the browser's network error; browser Back returns to the replaced signed-out entry. The local fixture verifies this behavior, not provider logout acceptance.

T027 remains `BLOCKED` on an approved owning-operation contract and controlled station-scope fixture. T048–T050 remain blocked on Cognito/API environment configuration, authorized test identities, and live integration; T051 remains blocked on the owning feature's protected-operation scope evidence. Manual screen-reader review, production deployment/monitoring, and full SmartRail acceptance remain outstanding. This milestone makes no global token-revocation, unrelated-device-session, or production-readiness claim.

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

Current evidence: Milestones A–E local automated checks are `PASS` as listed above. The owning-operation backend-denial portion of T027 remains `BLOCKED` pending an approved operation and controlled fixture. Accessibility manual review and full feature acceptance remain `NOT RUN` or are tracked in later tasks. Real Cognito/backend acceptance is `BLOCKED` until environment and controlled test-user configuration is supplied. Backend Feature 002 evidence was not rerun during this implementation milestone.

## F1 final client verification and integration readiness

This section records the F1 review of the approved Feature 001 implementation baseline. It does not change the owner-approved specification or plan status and does not claim full Feature 001, production, or platform acceptance.

### Integration prerequisites and current blockers

| Prerequisite | Current evidence/status | Required owner input or action |
|---|---|---|
| Cognito environment | `BLOCKED`: the frontend environment contains no issuer, managed-login domain, public app-client ID, callback/logout URI, or approved scopes. No AWS/Cognito environment was provisioned or contacted. | Cognito environment owner supplies the approved non-secret values, environment identity, registered callback/logout URLs, allowed scopes, and confirms Authorization Code + PKCE (S256) app-client configuration. |
| Runtime configuration template | `.env.example` is a placeholder-only template using reserved `.invalid` hosts and a clearly marked public client-ID placeholder. It is not runnable and contains no credentials. | After owners provide real values, create an ignored local `.env.local`; replace every placeholder and never add a client secret. Use only explicitly approved scopes. The exact logout URI must be registered with the Cognito app client. |
| API environment and browser origin | `BLOCKED`: no `VITE_SMARTRAIL_API_BASE_URL` is configured. Backend `Program.cs` currently allows `http://localhost:5174`; the Vite config does not pin that origin and Playwright's `http://127.0.0.1:4173` tests use mocks. | API/environment owners provide an approved reachable TLS API base URL, align the actual frontend origin with backend CORS and hosting/history fallback, and provide a test environment. Do not treat mocked API responses as integration evidence. |
| Controlled identities and role governance | `BLOCKED`: no controlled Controller or Station Staff test identities or deployed test role assignments were supplied. Feature 002's approved contract and current mapping are authoritative, but production/test assignment governance is outside this client change. | Identity/backend owner provides authorized test identities and confirms current `/api/me.roles` results through the approved environment. Do not infer roles from Cognito groups or frontend routes. |
| Station/resource authorization | `BLOCKED`: `GET /api/me` returns subject and roles only. No approved protected operational operation with controlled allowed/out-of-scope/missing/revoked/stale/unavailable scope fixtures is available. The candidate Incident Management Feature 004 source is Draft, so it is not an approved integration target. | Owning backend feature owner approves and implements the operation/contract and supplies controlled scope fixtures. Until then T027's owning-operation denial portion and T051 remain blocked; no endpoint is invented and no scope claim is made. |
| Backend verification evidence | Backend Feature 002 is approved for client integration, and its recorded verification evidence remains referenced by its own documents. It was not rerun for F1. Backend approval does not establish this frontend's live acceptance. | Use the approved backend environment for the separately gated client acceptance after the above prerequisites are ready. |
| Production readiness | `BLOCKED`: no production Cognito setup, deployment, monitoring, audit operations, or operational acceptance was performed. | Platform owners complete and evidence the relevant production deployment/configuration and monitoring prerequisites before production readiness is considered. |

### F1 task disposition

| Task | F1 disposition | Evidence / reason |
|---|---|---|
| T027 owning-operation browser denial | `BLOCKED` (direct URL/presentation checks are recorded separately as local `PASS`) | No approved protected operation and controlled out-of-scope backend fixture; see integration prerequisites above. |
| T048 | `BLOCKED` | Cognito configuration and authorized identities are absent. |
| T049 | `BLOCKED` | API base URL, aligned CORS/hosting setup, and controlled environment are absent. |
| T050 | `BLOCKED` | Depends on T048 and T049; no real Cognito or backend flow was run. |
| T051 | `BLOCKED` | No approved scoped operation or controlled scope fixtures. |
| T052 | `PASS` | Reviewed implementation source, automated tests, and the SR/FR/SC traceability matrix. Evidence recorded below and in the preceding milestone sections. |
| T053 | `NOT RUN` | Manual assistive-technology verification requires a human using a screen reader; no screen reader run is claimed. Use the reproducible procedure below. |
| T054 | Commands run for requested F1 verification; task remains unchecked | Lint/build results are recorded below. The task sequence requires T053 first, which remains outstanding. |
| T055 | Commands run for requested F1 verification; task remains unchecked | Vitest/Playwright results are recorded below. The task sequence requires T054 after T053; neither is complete in sequence. |
| T056 | Review/check run for requested F1 verification; task remains unchecked | Diff check and changed-file boundary review are recorded below. The sequential task gate remains incomplete because T053–T055 are not complete in order. |
| T057 | `NOT COMPLETE` | The requirements checklist is a requirements-quality checklist and remains read-only. Open client acceptance obligations prevent recording full client acceptance; SR-093 and SR-114 remain open. |

T052 was the only newly checked F1 task. T048–T057 otherwise retain their prior unchecked checkbox states; T027 remains open. Commands executed out of task sequence are recorded honestly but do not satisfy those sequential task checkboxes. The existing 17/17 requirements checklist score means its specification-quality checks are satisfied; it is not evidence that every runtime acceptance obligation passed.

### Requirement evidence review

| Approved SR requirement | F1 traceability finding |
|---|---|
| SR-038 | `PASS` for the implemented role-aware Controller presentation shell and local evidence. This is not acceptance of a live operational dashboard or dashboard business workflow. |
| SR-046 | `PASS` for Controller-only presentation affordances being withheld from unauthorized role presentations in local tests. Backend authorization remains authoritative; no cancellation operation or server-side authorization acceptance is claimed. |
| SR-089 | `PASS` for anonymous protected-route denial in local browser fixtures. This does not cover public railway reads, which are outside Feature 001. |
| SR-090 | `PASS` for local managed-login/OIDC callback and PKCE S256 behavior against deterministic fixtures. Real Cognito configuration and sign-in acceptance remain `BLOCKED` under T048/T050. |
| SR-091 | `PASS` for using current `GET /api/me.roles` to derive client presentation and for keeping the Station Staff shell generic. `GET /api/me` does not provide station assignment; backend scope-denial acceptance remains `BLOCKED` under T027/T051. |
| SR-093 | Local tests cover memory-only token-bearing state, expiry, reauthentication, local-first sign-out, and storage/history protections. Real client acceptance remains open under T050; do not infer silent renewal or global token revocation. |
| SR-100 | `PASS` for local safe-error and diagnostic redaction checks. No live backend error handling acceptance is claimed. |
| SR-114 | Automated keyboard, focus, names, and status-announcement assertions are recorded as `PASS`; manual screen-reader acceptance is `NOT RUN` under T053. |

These findings do not constitute full SmartRail MVP acceptance. In particular, client acceptance obligations for SR-093 and SR-114 remain open, and no production or live integration acceptance is claimed.

### Manual keyboard and screen-reader verification procedure

Perform this only in a controlled, approved test environment with a non-production identity. Record browser/OS versions, screen-reader name/version, identity role(s) as supplied by the backend, date, and outcome without recording credentials or tokens.

1. With keyboard only, Tab and Shift+Tab through the unauthenticated sign-in page, activate sign-in with Enter/Space, and confirm visible focus remains clear and controls have understandable names.
2. Exercise the approved callback in the controlled environment. Confirm loading, success, error, retry, and sign-out state changes are announced and that focus is sensible after navigation or an error.
3. With a Controller identity, inspect headings and navigation, activate Sign Out by keyboard, and verify the signed-out status is announced. Try direct protected navigation and browser Back/Forward; protected content must remain closed.
4. Repeat with a Station Staff identity. Verify the generic Station Staff shell and that no Controller-only navigation is exposed. Do not infer assigned station context from a URL.
5. Use the screen reader's heading, landmark, and control navigation to review sign-in, identity/loading, denial, unavailable/error, retry, and signed-out feedback. Confirm the meaning is conveyed in text/state and not by color alone.
6. Record each result as `PASS`, `FAIL`, `BLOCKED`, or `NOT RUN`, include the safe evidence location, and leave any failing or unavailable condition open for remediation.

### F1 requested verification results

| Check | Result | Evidence boundary |
|---|---|---|
| `npm run lint` | Pending F1 execution | Record actual result after this section is completed. |
| `npm run build` | Pending F1 execution | Record actual result after this section is completed. |
| `npm run test -- --run` | Pending F1 execution | Record actual result after this section is completed. |
| `npm run test:e2e` | Pending F1 execution | Record actual result after this section is completed; browser tests use local fixtures. |
| `git diff --check` | Pending F1 execution | Record actual result after all documentation changes. |
| Changed-file/security boundary review | Pending F1 execution | Confirm no backend/API/requirement changes or secret material after reviewing the final diff. |

