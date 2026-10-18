# Tasks: SmartRail Identity & Permissions

**Input**: Approved Feature 002 design documents in `specs/002-identity-permissions/`.

**Prerequisites**: Constitution, `spec.md`, `plan.md`, `research.md`, `data-model.md`, `quickstart.md`, `contracts/`, and the requirements checklist.

**Architecture**: Existing .NET 10 six-project Clean Architecture solution. No new projects or microservices. Cognito authenticates; PostgreSQL active role mappings authorize. Preserve the approved RBAC matrix, Feature 001 behavior and manual AWS deployment boundary.

**Task format**: `- [ ] T### [P?] [US#?] Description (depends on T###)`

- `[P]` marks tasks that can proceed concurrently because they edit separate files and have no unfinished task dependency.
- `[US1]` through `[US4]` trace work to the Feature 002 user stories in `spec.md`.
- All paths are repository-relative. Tasks are intentionally unchecked; this file does not claim implementation completion.

## Phase 1 — Foundation and Authentication Setup

**Purpose**: Prepare Cognito configuration and API package foundations in the existing solution.

- [X] T001 [P] Add `Microsoft.AspNetCore.Authentication.JwtBearer` 10.0.12 to `src/SmartRail.Api/SmartRail.Api.csproj`.
- [X] T002 [P] Define strongly typed Cognito issuer, allowed human `client_id` values, optional expected API audience and validation settings in `src/SmartRail.Api/Authentication/CognitoJwtOptions.cs`.
- [X] T003 Bind and validate Cognito settings at startup, requiring trusted issuer and allowed client IDs while enforcing the configured/missing/present `aud` policy, in `src/SmartRail.Api/Authentication/CognitoJwtOptionsValidator.cs` (depends on T002).
- [X] T004 Add a secret-free local configuration example in `src/SmartRail.Api/appsettings.Development.example.json` and document .NET User Secrets/environment-variable overrides in `src/SmartRail.Api/Authentication/README.md` (depends on T002, T003).

**Phase verification gate**: Restore and build `SmartRail.slnx`; confirm no credentials or AWS values are committed, required local Cognito settings fail clearly when missing, and no new project or AWS resource is introduced.

## Phase 2 — Identity Domain Models and RBAC Rules

**Purpose**: Define internal role codes, assignment/audit invariants and the approved explicit-grant evaluator.

- [X] T005 [US3] Define the canonical role-code enum `Passenger`, `Driver`, `StationStaff`, and `Controller` (with no `Public` assignment) in `src/SmartRail.Domain/Identity/RoleCode.cs` (depends on T001–T004).
- [X] T006 [P] [US3] Implement role grant/revocation lifecycle invariants and opaque Cognito-subject handling in `src/SmartRail.Domain/Identity/Entities/RoleAssignment.cs` (depends on T005).
- [X] T007 [US3] Implement append-only role grant/revocation audit evidence with actor, action, UTC time and optional controlled evidence reference in `src/SmartRail.Domain/Identity/Entities/RoleAssignmentAudit.cs` (depends on T006).
- [X] T008 [P] [US3] Implement station-scope grant/revocation lifecycle invariants referencing the existing Railway Network station identity in `src/SmartRail.Domain/Identity/Entities/StationScopeAssignment.cs` (depends on T005).
- [X] T009 [US3] Implement append-only station-scope grant/revocation audit evidence in `src/SmartRail.Domain/Identity/Entities/StationScopeAssignmentAudit.cs` (depends on T008).
- [X] T010 [US3] Encode only the approved role/action grants and explicit multi-role grant union in `src/SmartRail.Domain/Identity/RolePermissionCatalog.cs` without broadening or changing the matrix in `specs/002-identity-permissions/spec.md` (depends on T005).
- [X] T011 [P] [US3] Test role assignment invariants, active/revoked transitions, re-grant lifecycle, and audit event rules in `tests/SmartRail.UnitTests/Identity/RoleAssignmentTests.cs` (depends on T006, T007).
- [X] T012 [P] [US3] Test station-scope lifecycle, station identity, audit event rules and revocation in `tests/SmartRail.UnitTests/Identity/StationScopeAssignmentTests.cs` (depends on T008, T009).
- [X] T013 [P] [US3] Test exact approved matrix grants, multiple-role explicit-grant union, unknown codes and no-grant defaults in `tests/SmartRail.UnitTests/Identity/RolePermissionCatalogTests.cs` (depends on T010).

**Phase verification gate**: Run the focused Domain unit tests; verify role codes match the data model, public is not an assigned role, and domain projects have no EF Core, API, Cognito SDK or AWS dependencies.

## Phase 3 — PostgreSQL Persistence and Auditing

**Purpose**: Add the Identity schema and transaction-safe role/scope audit persistence through the existing shared context.

- [X] T014 [P] [US3] Map `RoleAssignment` required fields, revocation checks, active-subject lookup and partial active `(subject, role)` uniqueness in `src/SmartRail.Infrastructure/Identity/Configurations/RoleAssignmentConfiguration.cs` (depends on T006).
- [X] T015 [P] [US3] Map the restrictive FK, required audit evidence and `(RoleAssignmentId, OccurredAtUtc)` index in `src/SmartRail.Infrastructure/Identity/Configurations/RoleAssignmentAuditConfiguration.cs` (depends on T007).
- [X] T016 [P] [US3] Map station FK, lifecycle checks and partial active `(subject, station)` uniqueness in `src/SmartRail.Infrastructure/Identity/Configurations/StationScopeAssignmentConfiguration.cs` (depends on T008).
- [X] T017 [P] [US3] Map station-scope audit FK, required actor/action/time and history index in `src/SmartRail.Infrastructure/Identity/Configurations/StationScopeAssignmentAuditConfiguration.cs` (depends on T009).
- [X] T018 Register the four Identity entity sets in the existing context without changing Feature 001 mappings in `src/SmartRail.Infrastructure/RailwayNetwork/SmartRailDbContext.cs` (depends on T014–T017).
- [X] T019 Define Application-owned role and station-scope persistence ports consumed by Infrastructure in `src/SmartRail.Application/Identity/Persistence/IdentityPersistenceContracts.cs` (depends on T006–T009).
- [X] T020 Implement current active role lookup and transactional grant/revoke plus role-audit writes in `src/SmartRail.Infrastructure/Identity/Persistence/RoleAssignmentRepository.cs` (depends on T018, T019).
- [X] T021 Implement current station-scope lookup and transactional scope grant/revoke plus scope-audit writes in `src/SmartRail.Infrastructure/Identity/Persistence/StationScopeRepository.cs` (depends on T018, T019).
- [X] T022 Create the additive EF Core migration named `IdentityPermissions` using an EF Core-generated migration ID (for example, `dotnet ef migrations add IdentityPermissions`); place the generated migration and designer files in the existing `src/SmartRail.Infrastructure/Migrations/` directory and update the shared `SmartRailDbContextModelSnapshot.cs` there; retain the Feature 001 migration unchanged (depends on T014–T021).
- [X] T023 [P] [US3] Test the additive migration, columns, checks, foreign keys, active-row uniqueness and indexes using the real isolated PostgreSQL fixture in `tests/SmartRail.IntegrationTests/Identity/IdentityMigrationAndConstraintTests.cs` (depends on T022).
- [X] T024 [P] [US3] Test role grant/revoke/re-grant audit history, verified actor/action/time, atomic rollback and append-only behavior in `tests/SmartRail.IntegrationTests/Identity/RoleAssignmentAuditTests.cs` (depends on T020, T022).
- [X] T025 [P] [US3] Test station-scope grant/revoke/re-grant audit history, actor/action/time, transaction rollback and append-only behavior in `tests/SmartRail.IntegrationTests/Identity/StationScopeAuditTests.cs` (depends on T021, T022).
- [X] T026 [P] [US3] Test current role/scope lookups observe committed revocation on the next check and reject invalid station references in `tests/SmartRail.IntegrationTests/Identity/IdentityCurrentStateTests.cs` (depends on T020–T022).

**Phase verification gate**: Run real PostgreSQL integration tests on isolated local test databases; verify audit changes roll back with assignment changes, audit history survives revocation/re-grant, and migration history contains only the additive Identity migration beyond Feature 001.

## Phase 4 — Application Authorization Use Cases

**Purpose**: Resolve current roles and module-owned scope evidence, evaluate authorization, expose the caller identity projection, and keep maintenance commands non-HTTP and deny-by-default.

- [X] T027 Define framework-neutral authorization inputs/outcomes and owner-module evidence ports for Driver Operations, Incident Management and Catering Adapter in `src/SmartRail.Application/Identity/Authorization/AuthorizationContracts.cs` (depends on T019, T026).
- [X] T028 Implement current active-role resolution by verified Cognito subject, including `Unavailable` versus verified-empty results, in `src/SmartRail.Application/Identity/Authorization/ResolveCurrentRolesHandler.cs` (depends on T019, T020, T027).
- [X] T029 Implement current station, assigned-journey, incident-resource and passenger-order evidence resolution through owner ports in `src/SmartRail.Application/Identity/Authorization/ResolveResourceScopeHandler.cs` (depends on T027, T028).
- [X] T030 Implement server-side permission decisions requiring explicit active-role grants plus every applicable current scope/ownership result in `src/SmartRail.Application/Identity/Authorization/AuthorizeOperationHandler.cs` (depends on T010, T028, T029).
- [X] T031 Implement `GET /api/me` query/handler returning only caller subject and active recognized internal role codes, with verified-empty distinct from unavailable, in `src/SmartRail.Application/Identity/Queries/GetCurrentIdentityHandler.cs` (depends on T028).
- [X] T032 Implement the non-HTTP grant-role command behind a deny-by-default maintenance-authority port in `src/SmartRail.Application/Identity/Maintenance/GrantRoleAssignmentHandler.cs` (depends on T019, T027).
- [X] T033 Implement the non-HTTP revoke-role command behind the same deny-by-default maintenance boundary in `src/SmartRail.Application/Identity/Maintenance/RevokeRoleAssignmentHandler.cs` (depends on T019, T027, T032).
- [X] T034 Implement the non-HTTP grant-station-scope command behind a deny-by-default maintenance-authority port in `src/SmartRail.Application/Identity/Maintenance/GrantStationScopeHandler.cs` (depends on T019, T027).
- [X] T035 Implement the non-HTTP revoke-station-scope command behind the deny-by-default maintenance boundary in `src/SmartRail.Application/Identity/Maintenance/RevokeStationScopeHandler.cs` (depends on T019, T027, T034).
- [X] T036 [P] [US3] Test role resolution, explicit-grant union, all scope-provider outcomes, ownership denial and unavailable-source fail-closed behavior in `tests/SmartRail.UnitTests/Identity/AuthorizationHandlerTests.cs` (depends on T028–T030).
- [X] T037 [P] [US3] Test role/scope maintenance commands reject by default and make no persistence calls when authority is absent or denied in `tests/SmartRail.UnitTests/Identity/IdentityMaintenanceAuthorizationTests.cs` (depends on T032–T035).

**Phase verification gate**: Run focused Application tests; demonstrate no grant from token/group input, every module-owned scope check is required where applicable, and denied/unavailable maintenance commands have no write effect.

## Phase 5 — JWT Authentication and REST API

**Purpose**: Enforce Cognito access-token validation and expose only the approved caller-only `GET /api/me` route.

- [X] T038 Configure the ASP.NET Core bearer handler for trusted Cognito HTTPS metadata/JWKS, signing-key refresh, issuer and lifetime validation, `token_use=access`, allowlisted human `client_id`, and disabled token saving in `src/SmartRail.Api/Authentication/CognitoJwtBearerExtensions.cs` (depends on T001–T004, T037).
- [X] T039 Implement Cognito claim validation for `client_id`, `token_use`, and the exact configured/missing/present `aud` rules; never use `client_id` as `aud` or groups as roles, in `src/SmartRail.Api/Authentication/CognitoAccessTokenValidator.cs` (depends on T002, T003, T038).
- [X] T040 Adapt Application `Allowed`, `Denied`, and `Unavailable` decisions to ASP.NET Core authorization requirements/handlers in `src/SmartRail.Api/Authorization/SmartRailAuthorizationHandler.cs` (depends on T027–T030, T038, T039).
- [X] T041 Register authentication/authorization middleware in the correct order and preserve the four existing explicit anonymous Railway Network endpoints in `src/SmartRail.Api/Program.cs` (depends on T038–T040).
- [X] T042 Define the `GET /api/me` response DTO and explicit code-to-label mapping (`StationStaff` to `Station Staff`) in `src/SmartRail.Api/Contracts/Identity/CurrentIdentityResponse.cs` (depends on T005, T031).
- [X] T043 Map only authenticated `GET /api/me`, derive subject from the validated principal, and map verified role lookup to the DTO in `src/SmartRail.Api/Endpoints/IdentityEndpoints.cs` (depends on T031, T040–T042).
- [X] T044 Map protected authentication failures to safe 401, known permission denials to 403 and unavailable authorization authority to 503 using existing Problem Details conventions in `src/SmartRail.Api/Errors/ProblemDetailsConfiguration.cs` (depends on T030, T038–T043).
- [X] T045 Register Identity repositories and Application handlers in existing dependency-injection composition and register a production `DenyAllIdentityMaintenanceAuthority` default in `src/SmartRail.Infrastructure/DependencyInjection.cs` and `src/SmartRail.Infrastructure/Identity/Maintenance/DenyAllIdentityMaintenanceAuthority.cs`; do not add a project or production grant path (depends on T020, T021, T027–T035).
- [X] T046 Add OpenAPI metadata for the exact `/api/me` contract, its security scheme, role labels and 200/401/503 responses in `src/SmartRail.Api/Endpoints/IdentityEndpoints.cs`, consistent with `specs/002-identity-permissions/contracts/identity-permissions.openapi.yaml` (depends on T042–T045).

**Phase verification gate**: Build `SmartRail.slnx`; inspect generated OpenAPI for only the approved new Identity operation; verify the four Feature 001 route declarations and anonymous access annotations remain unchanged.

## Phase 6 — Automated Security and Integration Testing

**Purpose**: Verify Cognito trust boundaries, role/scope decisions, public-route compatibility, privacy, and real database behavior with deterministic local fixtures.

- [ ] T047 Create a deterministic local OIDC discovery/JWKS fixture and Identity API test host using the existing isolated PostgreSQL fixture in `tests/SmartRail.IntegrationTests/Identity/IdentityApiTestHost.cs`, `tests/SmartRail.IntegrationTests/Identity/LocalOidcFixture.cs`, and test-only `tests/SmartRail.IntegrationTests/Identity/AuthorizationProbeEndpoint.cs`; register the probe only in the test host, never production (depends on T023, T038, T039, T045, T046).
- [ ] T048 [P] [US2] Test valid Cognito access tokens, bad signatures/issuers/keys, key rotation refresh, expiry/not-before, wrong `token_use`, disallowed `client_id`, absent/matching/mismatched `aud`, unconfigured present `aud`, and ID-token rejection in `tests/SmartRail.IntegrationTests/Identity/CognitoJwtValidationTests.cs` (depends on T047).
- [ ] T049 [P] [US2] Test `GET /api/me` 200 with exact external role labels, 200 with verified empty roles, caller-only subject, and safe 401/503 outcomes in `tests/SmartRail.IntegrationTests/Identity/CurrentIdentityEndpointTests.cs` (depends on T047).
- [ ] T050 [P] [US3] Test explicit multi-role grants, insufficient-role 403, scoped/owned allow and deny, unavailable authority 503, and denied write no-side-effect behavior through the test-only authorization probe in `tests/SmartRail.IntegrationTests/Identity/AuthorizationEndpointTests.cs`; do not add a production write route (depends on T047).
- [ ] T051 [P] [US1] Regress all four Feature 001 GET routes with no token, malformed optional token, and expired optional token; verify existing public response behavior and projection in `tests/SmartRail.IntegrationTests/Identity/PublicRailwayReadRegressionTests.cs` (depends on T047).
- [ ] T052 [P] [US4] Assert Problem Details and captured application logs omit raw tokens, passwords, full claims, signing keys, SQL/provider details and unrestricted sensitive identifiers in `tests/SmartRail.IntegrationTests/Identity/AuthenticationPrivacyTests.cs` (depends on T047, T044).
- [ ] T053 [P] [US3] Exercise the complete approved role/action matrix and module-owned scope/ownership combinations using isolated provider fakes in `tests/SmartRail.IntegrationTests/Identity/AuthorizationMatrixTests.cs` (depends on T047, T050).
- [ ] T054 [P] [US2] Verify local signing/JWKS fixtures cannot be activated by normal Development or Production composition and do not bypass bearer validation in `tests/SmartRail.IntegrationTests/Identity/TestAuthenticationIsolationTests.cs` (depends on T047).
- [ ] T055 [P] [US2] Compare generated Identity OpenAPI with the approved contract and assert `/api/me` is the only new Identity route, with no public role/scope administration route, in `tests/SmartRail.IntegrationTests/Identity/IdentityOpenApiTests.cs` (depends on T046, T047).
- [ ] T056 [P] [US3] Verify the API observes a committed role/scope revocation on the next authorization request, without caching a prior allow, using isolated real PostgreSQL in `tests/SmartRail.IntegrationTests/Identity/AuthorizationRevocationIntegrationTests.cs` (depends on T024, T025, T047).

**Phase verification gate**: Run all unit and integration tests with real PostgreSQL persistence tests; no failed or silently skipped tests. Test-only identity fixtures use loopback/local test infrastructure and per-class isolated databases, never the development or production database.

## Phase 7 — Final Local Verification

**Purpose**: Complete end-to-end local verification and evidence before any separately guided manual AWS work.

- [ ] T057 Restore and build the complete solution with `dotnet restore SmartRail.slnx` and `dotnet build SmartRail.slnx`; record results in `specs/002-identity-permissions/quickstart.md` (depends on T001–T056).
- [ ] T058 Run `dotnet test SmartRail.slnx` against the isolated PostgreSQL test service; record passed, failed and skipped counts in `specs/002-identity-permissions/quickstart.md` (depends on T057).
- [ ] T059 Apply the Identity migration only to the explicitly verified local development database using the connection settings documented in `specs/002-identity-permissions/quickstart.md`; verify `__EFMigrationsHistory` and never target RDS or another remote database (depends on T022, T058).
- [ ] T060 Run local HTTP smoke checks for `/api/me`, all four anonymous Feature 001 GET routes, invalid/expired token behavior, role labels, and 401/403/503 outcomes with deterministic local fixtures in `tests/SmartRail.IntegrationTests/Identity/IdentityLocalSmokeTests.cs` (depends on T047–T059).
- [ ] T061 Record results from T055 and the existing Feature 001 OpenAPI regression test in `specs/002-identity-permissions/quickstart.md`; confirm no extra routes, live-status behavior, or unrelated module implementation (depends on T055, T060).
- [ ] T062 Update `specs/002-identity-permissions/checklists/requirements.md` with Feature 002 evidence for SR-089, SR-090, SR-091, SR-100 and the API portion of SR-114; update the downstream tracking section below with links to the owning Flutter/React feature specifications and their implementation/test evidence when available; keep SR-093 and the client portion of SR-114 explicitly open for overall MVP acceptance until that evidence exists (depends on T048–T061).
- [ ] T063 Review `specs/002-identity-permissions/quickstart.md` and `specs/002-identity-permissions/plan.md` to confirm local configuration is separate from AWS settings, production role/scope writes remain disabled pending approval, and AWS setup is manual after complete local system verification (depends on T057–T062).
- [ ] T064 Run `git diff --check` and review the final diff for Feature 002 scope, unchanged Feature 001 routes/contracts, no new projects, no production fixture bypass, and no AWS provisioning/deployment operations; record final local readiness evidence in `specs/002-identity-permissions/quickstart.md` (depends on T057–T063).

**Phase verification gate**: Feature 002 is locally ready only when the complete solution build/tests, real PostgreSQL migration and persistence checks, API smoke tests, requirement evidence, and scope review all pass. AWS resource setup and deployment are not part of this feature implementation; the user will perform them manually with step-by-step guidance after local verification.

## Dependencies & Execution Order

### Phase dependencies

- **Phase 1**: no dependencies; establishes package/config foundations.
- **Phase 2**: depends on Phase 1; creates Domain roles, audit models and policy rules.
- **Phase 3**: depends on Phase 2; Application persistence ports and EF mappings precede repositories and additive migration.
- **Phase 4**: depends on Phase 3; Application authorization/use cases build on verified persistence ports.
- **Phase 5**: depends on Phase 4; API authentication and `/api/me` compose the Application use cases.
- **Phase 6**: depends on Phase 5; security/API tests use deterministic local issuer/JWKS and isolated PostgreSQL fixtures.
- **Phase 7**: depends on Phases 1–6; final local system verification precedes manual AWS setup.

### Critical ordering constraints

- T005 precedes all role-valued entities and RBAC rules.
- T006–T009 precede their EF mappings; T014–T021 precede T022 migration generation.
- T019 ports precede T020/T021 Infrastructure implementations.
- T027–T030 authorization ports/handlers precede API policy integration T040.
- T038/T039 Cognito validation and T040 policies precede middleware composition T041.
- T047 local issuer/test host precedes API security integration tests T048–T056.
- T022 migration is tested before local development database application in T059.
- T057/T058 full build and tests precede final HTTP smoke checks and evidence updates.

## Parallel Opportunities

Parallelizable tasks are marked `[P]` only when they edit different files and have no unmet prerequisites:

- Phase 1: T001 with T002; T004 follows T002/T003.
- Phase 2: T006 and T008 after T005; T011–T013 after their model/catalog dependencies.
- Phase 3: T014–T017 mappings can proceed independently; T023–T026 test files can proceed after the migration and repositories exist.
- Phase 4: T036 and T037 are independent test files once their handlers exist.
- Phase 6: T048–T052 and T054–T056 use separate test files and may proceed after T047 and their other listed prerequisites, using isolated fixtures and the existing per-class database isolation strategy. T053 also uses a separate file, but depends on T050; it must not start until T050 is complete and may then proceed alongside other ready tasks.

## User Story Coverage and Independent Test Criteria

- **US1 — Access public railway information (SR-089)**: Independently verify all four Feature 001 GET routes with no token and with malformed/expired optional tokens; public status, response contract and projection remain unchanged (T051).
- **US2 — Authenticate to protected SmartRail functions (SR-090)**: Independently verify Cognito access-token validation cases and `GET /api/me` with active roles, empty roles, invalid credentials, audience failures and unavailable role mapping (T048, T049).
- **US3 — Enforce role, ownership and resource scope (SR-091)**: Independently verify exact matrix grants, multi-role union, current station/journey/order evidence, deny-by-default maintenance, audit persistence and next-check revocation behavior (T011–T013, T024–T026, T036–T037, T050, T053, T056).
- **US4 — Keep sessions and access failures safe (SR-093, SR-100, SR-114)**: Independently verify safe API errors/logs, token-expiry outcomes and that backend completion does not falsely claim Flutter/React secure-storage or loading/no-data/error/retry UI obligations (T052, T062).
## Requirement Traceability

| Approved requirement | Task coverage |
|---|---|
| SR-089 | T041, T051, T060–T062 preserve and verify the four anonymous public reads. |
| SR-090 | T001–T004, T038–T039, T047–T049, T060 verify Cognito authentication and expiry. |
| SR-091 | T005–T037, T040, T050, T053, T056 verify current role grants, scopes, ownership and deny-by-default behavior. |
| SR-093 | T062 preserves secure token-storage and session-expiry acceptance for downstream Flutter/React client specifications; client implementation is not claimed here. |
| SR-100 | T044, T052, T062 verify safe API errors/logs and trace evidence. |
| SR-114 | T044, T049, T052, T062 verify API failure outcomes and retain loading/no-data/error/retry client obligations for downstream Flutter/React specifications. |

## Downstream Client Tracking — Open for Overall MVP Acceptance

These approved Must Have obligations belong to the client feature specifications and are not implemented by Feature 002. T062 records Feature 002's API-side evidence but MUST NOT mark either requirement complete for overall MVP acceptance until the owning client specifications provide implementation and automated-test evidence.

| Requirement | Owning client work | Required downstream acceptance evidence | Overall MVP status |
|---|---|---|---|
| SR-093 | Flutter Driver client and applicable operational clients (including the React operational client) | Client-specific implementation tasks and automated tests demonstrate supported secure credential storage, no plaintext access tokens in application cache, and controlled session expiry that requires re-authentication before protected work continues. | Open until client evidence exists. |
| SR-114 | Applicable Flutter passenger and driver workflows and React operational workflows | Client-specific implementation tasks and automated tests cover loading, no-data/no-results, 403 access denial, 500 error, retry, and no-network states for each applicable primary workflow; retry preserves authorization and does not imply a denied operation succeeded. | Open until client evidence exists. |

The owning Flutter/React feature specifications MUST identify the affected primary views, link SR-093/SR-114, include their own implementation tasks and automated tests, and record passing acceptance evidence. Feature 002 does not add client functionality or claim these client obligations complete.

## Implementation Strategy

Implement foundation, domain and persistence before Application and API integration. Validate the migration and audit behavior against real isolated PostgreSQL. Then enable authentication and the caller-only `/api/me` operation while preserving Feature 001's anonymous contracts. Complete the security, privacy and API regression suite, then run the full SmartRail system locally. No task authorizes AWS provisioning or deployment.

**Local MVP increment**: verified Cognito access-token authentication, current PostgreSQL role resolution, approved RBAC decision interfaces, `GET /api/me`, safe 401/403/503 behavior, and regression proof that the four existing public reads remain anonymous. Flutter/React secure-storage and SR-114 UI-state work remains assigned to the client feature specifications.





