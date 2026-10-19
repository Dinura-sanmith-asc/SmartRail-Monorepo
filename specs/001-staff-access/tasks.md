---
description: "Dependency-ordered implementation tasks for Staff Authentication & Role-Aware Operations Foundation"
---

# Tasks: Staff Authentication & Role-Aware Operations Foundation

**Input**: Design documents from `specs/001-staff-access/`

**Prerequisites**: Project-owner-approved Feature 001 specification and technical plan; `research.md`, `data-model.md`, `contracts/identity-client.md`, and `quickstart.md` provide implementation decisions and boundaries.

**Tests**: Test-driven work was explicitly requested. Each story writes its focused automated tests before the implementation and confirms the new tests fail for the expected reason before making them pass. Do not report mocked OAuth/API tests as live Cognito/backend acceptance.

**Organization**: Tasks are grouped by setup, foundation, and the four specification user stories. Checkboxes record only work completed with evidence; the accepted baseline is not itself completion evidence.

**Implementation baseline approval**: The SmartRail Project Owner accepted this 57-task baseline on 2026-10-19 for subsequent implementation. Milestone A records only evidenced completion; later implementation tasks remain unchecked.

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Parallelizable only when the task owns a distinct file and has no unmet dependency.
- **[Story]**: User-story label matching `spec.md`.
- Every task names the exact implementation or test file target.

## Phase 1: Setup (Project Foundation)

**Purpose**: Prepare the existing React 19, TypeScript, and Vite application for the planned feature and its test stack. Do not implement staff-access behavior in this phase.

- [X] T001 Add only the plan-approved compatible React Router, `react-oidc-context`/`oidc-client-ts`, Vitest/jsdom, Testing Library/user-event, MSW, and Playwright dependencies in `package.json` and update the lockfile; verify package peer compatibility before selecting versions. Installed compatible versions and `npm ls --depth=0` passed; npm reported 0 vulnerabilities. Verified `.gitignore` ignores Node build/dependency outputs, logs, and env files (retaining `.env.example`) and flat ESLint ignores cover `node_modules`, build outputs, coverage, and minified JS.
- [X] T002 [P] Configure Vitest with the Vite config and jsdom environment in `vite.config.ts` and `src/test/setup.ts`. Vitest 5.0.5 loaded the jsdom setup and MSW lifecycle successfully during focused test discovery.
- [X] T003 [P] Configure Playwright browser-test discovery and local web-server settings in `playwright.config.ts`. Config import and test directory/web-server URL assertions passed; browser execution remains for final verification.
- [X] T004 Add deterministic `test` and `test:e2e` scripts while preserving existing scripts in `package.json`. `npm run test -- --run` and `npm run test:e2e -- --help` resolved to Vitest and Playwright.
- [X] T005 Configure a reusable MSW server and baseline request handlers in `src/test/msw.ts` for isolated component/network tests. The Vitest setup imported and started the handler server without error.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Establish public runtime configuration, typed API/error boundaries, and shared accessible presentation states used by all stories. No role-specific route may grant access before this foundation exists.

- [X] T006 [P] Write runtime-configuration validation tests in `tests/unit/config/runtimeConfig.test.ts` for missing API base URL, missing Cognito values, malformed URLs, and public configuration that must never contain a client secret. The initial run failed because the implementation module was not present yet (expected RED state).
- [X] T007 Implement typed runtime-configuration parsing and safe startup failure states in `src/config/runtimeConfig.ts` and `src/app/providers.tsx` to satisfy T006. `npm run test -- --run` passed runtime config cases for required values, malformed URLs, and rejecting client-secret keys without exposing values.
- [X] T008 [P] Write shared safe-error classification tests in `tests/unit/shared/safeError.test.ts` for 401, 403, 503, timeout, offline, malformed response, and redaction of tokens, claims, and backend internals. The initial run failed because the implementation module was not present yet (expected RED state).
- [X] T009 Implement bounded fetch/error classification helpers in `src/shared/api/safeError.ts` and `src/shared/api/fetchJson.ts` to satisfy T008; use `AbortController`, the planned 15-second client timeout, and never log bearer headers or response bodies. Utility behavior and safe classification passed in the full Vitest run; live API behavior was not tested.
- [X] T010 [P] Write accessible shared-state component tests in `tests/components/shared/AccessState.test.tsx` for loading, no-access, forbidden, unavailable, offline, retry, focus, keyboard operation, and non-color status meaning. Eight component assertions passed in the full Vitest run.
- [X] T011 Implement shared loading, denial, unavailable, offline, and retry presentation in `src/shared/ui/AccessState.tsx` and `src/shared/ui/AccessState.css` to satisfy T010. Eight component assertions passed, including keyboard retry and text status.
- [X] T012 Create the root application shell and route composition in `src/app/App.tsx`, `src/app/router.tsx`, and `src/main.tsx`, keeping callback ownership and protected content rendering for the user-story phases. `npm run build` passed; the shell contains no sign-in or operational routes.

**Checkpoint**: Test tooling and shared error/config/access-state boundaries are available. Cognito/API production values remain external.

---

## Phase 3: User Story 1 — Railway Controller enters the operations workspace (Priority: P1)

**Goal**: Complete Cognito managed login with PKCE, fetch the current backend identity, and show the Controller presentation shell only when the current `/api/me` roles include `Controller`.

**Independent Test**: A controlled protocol fixture exercises a valid code+PKCE callback and `/api/me` Controller response; the Controller shell appears without any business-workflow route. A no-role/non-staff response gets no Controller access.

### Tests for User Story 1 (write and observe expected failure before implementation)

- [ ] T013 [P] [US1] Add OIDC configuration and authorization-request tests in `tests/unit/staff-access/auth/oidcConfig.test.ts` for Authorization Code + PKCE S256, safe same-origin return paths, disabled automatic silent renewal, and disabled token revocation.
- [ ] T014 [P] [US1] Add browser protocol tests in `tests/e2e/staff-access/controller-login.spec.ts` for valid, missing, invalid/mismatched, and replayed/consumed OAuth state; valid, missing, and invalid authorization code; failed/cancelled authentication; exactly one callback processor; safe callback URL/history cleanup; and proof that every rejected callback leaves protected workspace inaccessible. Use a controlled local fixture and label results as mocked protocol evidence.
- [ ] T015 [P] [US1] Add identity response parser tests in `tests/unit/staff-access/identity/currentIdentity.test.ts` for exact `{ subject, roles }`, non-empty opaque subject, unique approved role labels, and valid empty roles.
- [ ] T016 [P] [US1] Add MSW contract and identity-lifecycle tests in `tests/components/staff-access/identity/currentIdentityClient.test.tsx` for authenticated `GET /api/me`, bearer header, valid Controller response, `401`, `503`, timeout, network failure, malformed 200, and no invented 403 response for `/api/me`; delay an identity response, sign out or expire/replace the session before releasing it, then assert the old response cannot restore identity, navigation, or protected content.
- [ ] T017 [P] [US1] Add Controller route and shell tests in `tests/components/staff-access/controller/ControllerWorkspace.test.tsx` for current Controller role, no role, Passenger/Driver-only roles, unknown labels, and direct protected-route navigation.

### Implementation for User Story 1

- [ ] T018 [US1] Implement the OIDC provider/configuration in `src/features/staff-access/auth/oidcConfig.ts` and `src/app/providers.tsx`, using PKCE S256 and setting `automaticSilentRenew` and `revokeTokensOnSignout` to false.
- [ ] T019 [US1] Configure the OIDC token-bearing user store as volatile memory and the separate short-lived per-tab transaction store in `src/features/staff-access/auth/sessionStore.ts`; store only OAuth state, nonce, PKCE verifier, and validated return path across redirect.
- [ ] T020 [US1] Implement the provider-owned callback lifecycle in `src/features/staff-access/auth/AuthProvider.tsx` and presentation-only `/auth/callback` route in `src/features/staff-access/auth/CallbackPage.tsx`; the route must never parse/exchange the authorization code a second time and must clear callback query data on success or failure.
- [ ] T021 [US1] Implement current identity types, strict runtime parser, and `GET /api/me` adapter in `src/features/staff-access/identity/currentIdentity.ts` and `src/features/staff-access/identity/currentIdentityClient.ts` using the approved contract only.
- [ ] T022 [US1] Implement identity/session state and stale-request invalidation in `src/features/staff-access/identity/identityState.ts` and `src/features/staff-access/identity/IdentityProvider.tsx`; cancel an in-flight identity request on sign-out/expiry where possible and use an equivalent generation/session-identity guard so a stale response cannot grant access after sign-out, expiry, or session replacement; satisfy the delayed-response test in T016.
- [ ] T023 [US1] Implement public sign-in entry, Controller shell, and Controller route gate in `src/features/staff-access/auth/SignInPage.tsx`, `src/features/staff-access/workspaces/ControllerWorkspace.tsx`, and `src/app/router.tsx`; only current `/api/me.roles` may select Controller presentation.
- [ ] T024 [US1] Run the focused tests from T013–T017 and record the results in `specs/001-staff-access/quickstart.md`; mocked fixture success is not Cognito acceptance.

**Checkpoint**: Controller shell works independently with current backend role data and no operational business workflows.

---

## Phase 4: User Story 2 — Station Staff enters a restricted workspace (Priority: P1)

**Goal**: Show a generic Station Staff workspace for the current Station Staff role while keeping resource-scope decisions entirely with the owning backend operation.

**Independent Test**: A Station Staff-only response selects a generic shell, gives no Controller affordances, and never infers station scope. A separate owning-backend scope fixture denies out-of-scope access; this client feature adds no scope API.

### Tests for User Story 2 (write and observe expected failure before implementation)

- [ ] T025 [P] [US2] Add Station Staff shell and role-union tests in `tests/components/staff-access/station/StationStaffWorkspace.test.tsx` for Station Staff-only, multiple recognized staff roles, and absence of Controller-only affordances unless Controller is also returned.
- [ ] T026 [P] [US2] Add negative resource-scope route/presentation tests in `tests/components/staff-access/station/resourceScopeBoundary.test.tsx` proving selected station/resource values never create access and the generic shell contains no inferred assignment.
- [ ] T027 [P] [US2] Add direct URL and backend-denial browser tests in `tests/e2e/staff-access/station-scope.spec.ts` proving route selection is presentation-only and an owning protected operation's out-of-scope denial reveals no data or success state.

### Implementation for User Story 2

- [ ] T028 [US2] Implement the generic Station Staff workspace and its navigation affordances in `src/features/staff-access/workspaces/StationStaffWorkspace.tsx` and `src/features/staff-access/workspaces/StationStaffWorkspace.css`; do not show a selected station as assigned scope.
- [ ] T029 [US2] Implement current-role presentation mapping and multi-role affordance union in `src/features/staff-access/routes/rolePresentation.ts` and integrate it with `src/app/router.tsx`; roles come only from the current `/api/me` response.
- [ ] T030 [US2] Implement route denial for missing/nonstaff role, wrong role, stale session, and crafted client navigation in `src/features/staff-access/routes/ProtectedRoute.tsx`; ensure backend authorization remains required for every future protected operation.
- [ ] T031 [US2] Run T025–T027 and record client presentation results and any owning-backend scope fixture dependency in `specs/001-staff-access/quickstart.md`; do not claim this feature itself proves backend resource authorization.

**Checkpoint**: Both staff shells are role-derived, Station Staff is generic, and no client route asserts station/resource permission.

---

## Phase 5: User Story 3 — A person receives safe feedback when access is unavailable (Priority: P1)

**Goal**: Fail closed for unauthenticated, expired, invalid, forbidden, unavailable, malformed, offline, and service-error outcomes, with accessible and safe recovery.

**Independent Test**: Exercise all identity contract and protected-operation error classes; verify no protected content/data or false success, safe redacted feedback, user-initiated retry, and keyboard/screen-reader usable status.

### Tests for User Story 3 (write and observe expected failure before implementation)

- [ ] T032 [P] [US3] Add access-state integration tests in `tests/components/staff-access/recovery/AccessRecovery.test.tsx` for unauthenticated route, callback cancel/failure, 401 reauthentication without replay, 403 safe denial on a protected operation only, 503 authority-unavailable, empty roles, malformed identity, offline, and timeout.
- [ ] T033 [P] [US3] Add accessibility tests in `tests/components/staff-access/recovery/AccessRecovery.accessibility.test.tsx` for keyboard-only sign-in/retry/navigation, visible focus, accessible names, status announcements, and meaning that does not rely on color.
- [ ] T034 [P] [US3] Add safe-output tests in `tests/unit/staff-access/recovery/safeDiagnostics.test.ts` injecting tokens, private claims, authorization internals, and raw Problem Details into failures and asserting none appear in rendered messages or diagnostics.
- [ ] T035 [P] [US3] Add expiry, reload, and browser-storage security audit tests in `tests/e2e/staff-access/session-expiry.spec.ts`; prove access, ID, and refresh tokens are absent from localStorage, sessionStorage, application cookies, persistent application caches, browser URLs/history, and client diagnostic logs; prove only temporary OAuth transaction state/PKCE verifier is retained across redirect separately from token-bearing session state and is cleaned after success/failure; prove full-page refresh without memory session requires reauthentication and expiry closes protected content without action replay.

### Implementation for User Story 3

- [ ] T036 [US3] Implement safe authentication and identity recovery states in `src/features/staff-access/auth/AuthRecovery.tsx` and `src/features/staff-access/identity/IdentityRecovery.tsx`; distinguish 401, 403, 503, malformed response, timeout, offline, and no-role outcomes.
- [ ] T037 [US3] Implement expiry handling and protected-state clearing in `src/features/staff-access/auth/sessionLifecycle.ts` and `src/features/staff-access/identity/IdentityProvider.tsx`; require fresh managed login and never silently renew or replay a protected action.
- [ ] T038 [US3] Implement safe retry and response redaction behavior in `src/features/staff-access/identity/currentIdentityClient.ts` and `src/shared/api/safeError.ts`; retry must call the same authenticated contract and must not convert 503 into empty roles.
- [ ] T039 [US3] Complete keyboard, focus, live-region, and accessible status behavior in `src/shared/ui/AccessState.tsx`, `src/features/staff-access/auth/SignInPage.tsx`, and the workspace navigation components.
- [ ] T040 [US3] Run T032–T035 and record results in `specs/001-staff-access/quickstart.md`; distinguish automated accessibility assertions from any manual screen-reader check.

**Checkpoint**: Every specified unavailable/denied state fails closed, is accessible, and exposes no sensitive details.

---

## Phase 6: User Story 4 — An authenticated staff member signs out (Priority: P2)

**Goal**: Immediately remove local authenticated state and protected access, then optionally navigate through Cognito managed-login logout using the approved Cognito parameter contract.

**Independent Test**: Sign out, simulate Cognito remote-navigation failure, revisit protected routes through direct URL and browser Back, and verify no token or cached protected content remains available to the application.

### Tests for User Story 4 (write and observe expected failure before implementation)

- [ ] T041 [P] [US4] Add local sign-out unit/component tests in `tests/components/staff-access/signout/SignOut.test.tsx` for clearing in-memory identity/token, protected data cache, route state, and active transaction state before remote navigation.
- [ ] T042 [P] [US4] Add Cognito logout URL tests in `tests/unit/staff-access/auth/cognitoLogout.test.ts` requiring HTTPS managed-login `/logout`, configured `client_id`, URL-encoded registered `logout_uri`, and no generic `post_logout_redirect_uri` assumption or access-token-revocation claim.
- [ ] T043 [P] [US4] Add browser sign-out tests in `tests/e2e/staff-access/signout-history.spec.ts` for configured remote logout navigation, unavailable/failed navigation fallback, direct protected URL, browser Back, and no protected request after local sign-out.

### Implementation for User Story 4

- [ ] T044 [US4] Implement local-first sign-out and transaction/cache cleanup in `src/features/staff-access/auth/signOut.ts` and wire the accessible control into `src/features/staff-access/workspaces/WorkspaceNavigation.tsx`.
- [ ] T045 [US4] Implement Cognito managed-login remote logout URL construction/navigation in `src/features/staff-access/auth/cognitoLogout.ts` using configured `client_id` and registered allowed `logout_uri`; if configuration or navigation fails, retain the signed-out local entry and never claim token revocation.
- [ ] T046 [US4] Implement protected route revalidation and browser-history safety after sign-out in `src/app/router.tsx` and `src/features/staff-access/routes/ProtectedRoute.tsx`; do not restore cached protected views.
- [ ] T047 [US4] Run T041–T043 and record local logout and remote logout test results separately in `specs/001-staff-access/quickstart.md`.

**Checkpoint**: Local logout removes access even when Cognito remote logout is unavailable; no global revocation behavior is implied.

---

## Phase 7: Real Integration (Environment-Gated)

**Purpose**: Perform live browser/backend acceptance only after the identity/environment owners provide approved configuration and controlled test identities. Until then these tasks are BLOCKED and must remain unchecked.

- [ ] T048 Obtain and validate Cognito issuer, managed-login domain, public app-client ID, registered callback/logout URLs, approved scopes, PKCE S256 setting, and authorized test identities; this task is BLOCKED until the Cognito environment owner supplies configuration. Record non-secret environment identity in `specs/001-staff-access/quickstart.md` without committing credentials.
- [ ] T049 Obtain approved API base URL, TLS/CORS/hosting history fallback, and a test environment with current Controller and Station Staff mappings; this task is BLOCKED until the API/environment owner supplies configuration. Record dependency owners in `specs/001-staff-access/quickstart.md`.
- [ ] T050 Run browser-backed Cognito managed-login and real `GET /api/me` acceptance for Controller and Station Staff identities, including valid/invalid/expired sessions and configured Cognito logout; this task is BLOCKED until T048 and T049 are complete. Record evidence and separate UI logout from token revocation in `specs/001-staff-access/quickstart.md`.
- [ ] T051 Verify Station Staff allowed station scope, out-of-scope station, missing scope evidence, revoked scope, stale scope, and unavailable authorization authority against the same existing approved owning-feature protected operation; this task is BLOCKED because no approved protected-operation contract/test target is identified in Feature 001's contract set, until the owning backend feature supplies one and its controlled scope fixtures. Record the exact dependency and server-side evidence in `specs/001-staff-access/quickstart.md`; do not invent an endpoint or attribute the checks to `/api/me`.

---

## Phase 8: Polish & Final Verification

**Purpose**: Verify traceability, accessible/security behavior, the full client test suite, and documentation evidence without overstating environment-dependent acceptance.

- [ ] T052 Review source, tests, and `specs/001-staff-access/quickstart.md` against the SR/FR/SC traceability matrix below; mark only checks with recorded evidence PASS and leave external checks BLOCKED or NOT RUN.
- [ ] T053 Run manual keyboard and representative screen-reader checks after T052, and record assistive technology/browser versions and outcomes in `specs/001-staff-access/quickstart.md`.
- [ ] T054 Run `npm run lint` and `npm run build` after T053; record actual command output/status in `specs/001-staff-access/quickstart.md`.
- [ ] T055 Run `npm run test -- --run` and `npm run test:e2e` after T054; record actual command output/status and fixture/environment boundary in `specs/001-staff-access/quickstart.md`.
- [ ] T056 Run `git diff --check` and review all changed feature files after T055 for accidental backend/API/requirement changes and secret material; record the result in `specs/001-staff-access/quickstart.md`.
- [ ] T057 Complete `specs/001-staff-access/checklists/requirements.md` after T056 only for evidence-backed client acceptance; retain open SR-093/SR-114 obligations until their client checks are recorded and preserve the owner-approved Feature 001 status unless the project owner changes it.

## Traceability Matrix

| Approved requirement / spec criteria | Planned evidence tasks | Boundary |
|---|---|---|
| SR-038, FR-004, SC-001 — Controller role-aware shell | T015–T024 | Presentation shell only; no live dashboard acceptance |
| SR-046, FR-005, SC-003 — Controller-only affordance boundary | T017, T025–T030, T051 | No cancellation workflow/API; backend remains authority |
| SR-089, FR-007, SC-002 — Anonymous protected-route denial | T017, T032, T035–T037 | Public railway reads are outside this client feature |
| SR-090, FR-001/002/008/011, SC-001/004 — Cognito login, identity, expiry | T013–T024, T032, T035–T038, T048–T050 | Mock protocol tests are not live Cognito acceptance |
| SR-091, FR-003/004/005/006, SC-003 — Current roles and scope boundary | T015–T017, T025–T031, T051 | No role inference, station scope, or new endpoint |
| SR-093, FR-011/012, SC-004/005 — Token safety and session behavior | T013–T014, T019–T020, T035, T041–T047, T050 | Verify no token persistence, reload reauth, expiry; no silent renew or revocation claim |
| SR-100, FR-009/010, SC-008 — Safe errors and diagnostics | T008–T011, T032, T034, T036–T040 | No raw backend detail, token, credential, or private claim disclosure |
| SR-114, FR-009/010/013, SC-006/007 — Error states and accessibility | T010–T011, T032–T040, T053 | Automated checks plus separately recorded manual assistive-technology check |
| FR-012, SC-005 — Sign-out and history behavior | T041–T047 | Local protected access cleared before optional Cognito logout |
| FR-014 — Excluded scope | T001–T057 review | No new identity endpoints, backend changes, Journey Feature 003 APIs, or business workflows |

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 Setup**: No feature implementation dependency; complete package and test-runner setup first.
- **Phase 2 Foundational**: Depends on Phase 1 and blocks all story implementation.
- **Phase 3 US1 (P1)**: Depends on Phase 2; establishes the shared authentication and identity provider used by later stories.
- **Phase 4 US2 (P1)**: Depends on US1 role and route infrastructure; adds the second presentation shell and scope-boundary tests.
- **Phase 5 US3 (P1)**: Depends on the auth/identity paths from US1 and routes/states from US2; hardens all failure and recovery paths.
- **Phase 6 US4 (P2)**: Depends on the session/provider and protected route infrastructure from US1–US3.
- **Phase 7 Real Integration**: T048 and T049 depend on external owners; T050 depends on both. T051 separately depends on an approved protected operation from its owning backend feature.
- **Phase 8 Polish**: Depends on implementation tasks; live integration items remain explicitly BLOCKED if prerequisites are absent.

### User Story Dependencies

- **US1 (P1)**: Can begin after Phase 2. It is the MVP increment for authenticated Controller presentation.
- **US2 (P1)**: Builds on US1's identity and route foundations because both roles share the same current identity projection and provider.
- **US3 (P1)**: Covers shared failure paths and therefore follows the identity and shell paths it protects.
- **US4 (P2)**: Uses the common session and route lifecycle and follows US1–US3.

### Parallel Opportunities

- In Phase 1, T002 and T003 can proceed in parallel; T004 must coordinate its package script edits with T001, so those two should be performed by one owner or serialized.
- In Phase 2, T006/T008/T010 tests target separate files and can proceed in parallel; matching implementations follow their tests.
- In US1, T013–T017 target separate tests and can proceed in parallel. Implementation ownership for `src/app/providers.tsx` (T018/T020) and `src/app/router.tsx` (T023/T029/T030/T046) must be serialized or assigned to one owner to avoid conflicting edits.
- In US2, T025–T027 are parallel. T028 and T029 target separate files; integrate after the tests and US1 route foundation.
- In US3, T032–T035 target separate test files and can proceed in parallel. Changes to `IdentityProvider.tsx`, `currentIdentityClient.ts`, `safeError.ts`, and shared accessible components must be serialized with earlier story edits to those same files.
- In US4, T041–T043 are parallel tests. T044 and T045 target separate files; integrate and test before T046 route-history changes.
- The external dependency requests represented by T048 and T049 can proceed in parallel; T050 cannot start until both are complete.
- In Phase 8, T052–T057 are sequential because each records or reviews evidence in `quickstart.md` or the requirements checklist. They are not marked `[P]`; complete each in task-ID order.

## Parallel Examples

### User Story 1 tests

```text
Task: T013 OIDC configuration tests in tests/unit/staff-access/auth/oidcConfig.test.ts
Task: T014 Controller login browser tests in tests/e2e/staff-access/controller-login.spec.ts
Task: T015 identity parser tests in tests/unit/staff-access/identity/currentIdentity.test.ts
Task: T016 /api/me client contract tests in tests/components/staff-access/identity/currentIdentityClient.test.tsx
Task: T017 Controller route tests in tests/components/staff-access/controller/ControllerWorkspace.test.tsx
```

### User Story 3 security/recovery tests

```text
Task: T032 recovery outcome tests in tests/components/staff-access/recovery/AccessRecovery.test.tsx
Task: T033 accessibility tests in tests/components/staff-access/recovery/AccessRecovery.accessibility.test.tsx
Task: T034 safe diagnostics tests in tests/unit/staff-access/recovery/safeDiagnostics.test.ts
Task: T035 expiry/reload browser tests in tests/e2e/staff-access/session-expiry.spec.ts
```

## Implementation Strategy

### MVP First

1. Complete Setup and Foundational phases.
2. Complete US1: provider-owned managed-login callback, current identity, and Controller shell.
3. Run US1's isolated tests and review the approved `/api/me` boundary.
4. Do not call the MVP production-ready: Cognito/API environment integration remains separately gated.

### Incremental Delivery

1. Complete US1, then US2 for generic Station Staff navigation and scope boundaries.
2. Complete US3 for complete safe failure/accessibility states.
3. Complete US4 for local-first sign-out and configured Cognito logout.
4. Complete environment-gated acceptance and final verification only when prerequisites exist.

## Notes

- Every task is unchecked at generation time; no implementation or tests are claimed complete.
- `[P]` means separate-file ownership with no unmet dependency, not merely that work could be delegated.
- The approved backend identity contract is `GET /api/me` with `subject` and current `roles`; it provides no station scope and defines 200, 401, and 503, not 403.
- Cognito sign-out uses its managed-login `/logout` contract with `client_id` and registered `logout_uri`. Local state is cleared first; failure of remote navigation cannot restore local access.
- One OIDC provider processes the callback. The React callback route is presentation-only.
- Feature 001 specification and plan are approved by the SmartRail Project Owner on 2026-10-19; the 57 tasks are the accepted implementation baseline. Do not edit the approved requirements, backend repository, OpenAPI contract, or business behavior without the relevant owner authorization.
