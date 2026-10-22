# Implementation Plan: SmartRail Identity & Permissions

**Branch**: `feature-02/Identity-&-Permissions` | **Date**: 2026-10-18 | **Spec**: [spec.md](spec.md)

**Input**: Clarified Feature 002 specification (approved by the SmartRail Project Owner on 2026-10-19 for client integration), SmartRail constitution, approved MoSCoW baseline and RBAC/API inventory, and the implemented Feature 001 modular-monolith foundation.

**Document and delivery status**: The specification was approved on 2026-10-19 for client integration after the recorded backend implementation and local verification. This records the approval chronology and does not establish production deployment readiness or platform-wide acceptance.

## Summary

Add Identity & Permissions inside the existing six-project .NET 10 modular monolith. Amazon Cognito authenticates users with access JWTs. After cryptographic and claim validation, SmartRail uses the verified Cognito `sub` to query PostgreSQL for the current internal role assignments on every protected authorization decision. JWT group claims never grant SmartRail roles. Active mapped roles combine only explicit permissions in the approved RBAC matrix; every relevant owner, station, journey or order scope check remains mandatory. A committed role revocation affects the next authorization check; unavailable role or scope evidence grants no protected access.

Expose only the already-approved authenticated `GET /api/me` operation for this feature, returning the caller's subject and active recognized roles. Preserve the four Feature 001 public reads, their response contracts, and their behavior when an invalid or expired optional token is present. No role, scope, user or account administration endpoint is introduced.

## Clarification Consistency Gate

- **Pass after supersession annotation**: the first clarification's approved Option B is the operative role source: a SmartRail internal role mapping keyed by verified Cognito subject.
- The former token-group union sentence is retained as historical wording and marked **SUPERSEDED**. Its explicit-grant union is operative only over currently active roles read from the internal mapping. Cognito JWT group claims do not supply authorization roles.
- The other four accepted clarification records remain unchanged. The union of active mapped roles, resource-scope evidence, caller-only `/api/me`, and optional-token treatment on public reads remain consistent with the operative decision.
- No approved role grant or Feature 001 contract is changed. The exact SR wording and acceptance checks remain in [spec.md](spec.md).

## Technical Context

**Language/Version**: C# / ASP.NET Core .NET 10 (`net10.0`); existing projects target .NET 10.

**Primary Dependencies**: Existing EF Core 10.0.12, Npgsql EF provider 10.0.3, ASP.NET Core OpenAPI 10.0.12, xUnit 2.9.3 and `WebApplicationFactory` 10.0.12. Add `Microsoft.AspNetCore.Authentication.JwtBearer` 10.0.12, matching the repository's ASP.NET Core package line. Use built-in authentication/authorization, EF Core, and the existing Problem Details support; add no identity framework or microservice.

**Storage**: Existing PostgreSQL 18.6 Compose development/test hosts and the existing `SmartRailDbContext`/EF migration pipeline. Add identity-owned tables through an additive migration; do not alter the initial Railway Network migration or its behavior.

**Testing**: Existing `SmartRail.UnitTests` and `SmartRail.IntegrationTests` projects. Use unit tests for domain and Application decisions, and `WebApplicationFactory` plus the existing isolated, real-PostgreSQL fixture for migration, persistence, API, and authorization integration tests.

**Target Platform**: Existing .NET 10 API, deployed as the SmartRail modular monolith on AWS Lambda behind API Gateway HTTP API; local development uses Docker Compose PostgreSQL.

**Project Type**: REST web service, modular monolith.

**Performance Goals**: No auth latency or throughput target is approved. Perform an indexed current-role lookup for each protected authorization decision; do not add process caching because role revocation must take effect on the next check. Establish operational targets only through an approved deployment decision.

**Constraints**: Verify Cognito issuer, RSA signature/JWKS key, expiry/lifetime, `token_use=access`, and an allowed user-app `client_id`. If an expected API resource audience is configured, require an `aud` claim matching it; missing or mismatched audience is invalid. If no expected audience is configured, accept only access tokens without `aud`; reject tokens carrying an unconfigured audience. Never substitute `client_id` for `aud` or accept an unvalidated resource-bound token. Never accept ID tokens as API access tokens. Keep JWT group claims out of authorization. Preserve current six-project dependency directions, public Railway Network routes/contracts, PostgreSQL and deny-by-default policy. No public administration API or separate identity service.

**Scale/Scope**: Identity policy, current role and station-scope persistence, the `/api/me` contract, and internal authorization ports for the other five modules. No approved user count, workload volume or latency SLO is available; make no capacity claim.

**Deployment approach**: Complete development and full-system verification in the local environment first. Keep local secrets/configuration separate from AWS environment values and use deterministic isolated authentication/JWKS fixtures only in tests. AWS resource creation, IAM, Cognito setup, RDS, API Gateway, Lambda, SAM and deployment operations are performed manually by the user with step-by-step guidance after local approval. Feature implementation MUST NOT provision AWS resources or run AWS deployment commands. Preserve the approved AWS target architecture.

## Constitution Check

### Pre-design gate

- **I. Approved scope and traceability — PASS**: SR-089, SR-090, SR-091, SR-093, SR-100 and SR-114 are preserved and mapped to design and test evidence. The historical group-claim wording is visibly superseded by Option B. RBAC grants are not changed.
- **II. Specification before implementation — exception in recorded chronology**: implementation and verification were recorded complete before the SmartRail Project Owner approved the specification on 2026-10-19. The approval is recorded as occurring on that date; it does not imply prior approval or change the recorded sequence.
- **III. Modular monolith and inward dependencies — PASS**: all work remains in the existing API, Application, Domain, Infrastructure and two test projects. Domain has no API, EF Core or AWS dependency; Infrastructure implements Application ports; API handles JWT/HTTP enforcement.
- **IV. Data integrity and explicit sources of truth — PASS**: Cognito owns credentials; PostgreSQL role assignments own active SmartRail roles; Identity owns station-staff station scopes; Driver Operations, Incident Management and Catering Adapter supply their own resource evidence. Every authorization source is explicit.
- **V. Server-enforced identity, scope and safety — PASS**: role, ownership and target scope are checked on the server. Missing or unverifiable authorization data fails closed. Drivers cannot publish official status; Controller-only actions remain as approved.
- **VI. Documented contracts — PASS**: the only Identity HTTP contract is the existing `GET /api/me` inventory entry. No other route or change to the four Feature 001 contracts is proposed.
- **VII. Evidence-based quality — PASS**: tests include real PostgreSQL, actual bearer validation behavior, denial side-effect checks, and all four anonymous Railway reads with invalid optional tokens.
- **VIII. Safe and honest operation — PASS**: safe 401/403/503 Problem Details, redacted diagnostics, and no token/credential logging are specified. No unapproved performance, retention or availability threshold is invented.
- **IX. Approved stack and deployment boundaries — PASS**: use .NET 10, EF Core 10, PostgreSQL 18.6 locally, Cognito JWT, AWS Lambda/API Gateway and the current solution projects.

### Post-design gate

**Design review: PASS with production gates recorded below.** The specification is approved for client integration as recorded in [spec.md](spec.md); this does not approve production deployment. The design does not depend on public role administration or unapproved role grants. Test fixtures may provide deterministic mapping rows in isolated test databases. Production activation is gated on approving who can provision/revoke mappings and station scopes, audit evidence/retention, environment-specific Cognito configuration and monitoring, and owner-module scope-provider readiness.

## Project Structure

### Documentation (this feature)

```text
specs/002-identity-permissions/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
└── contracts/
    ├── identity-permissions.openapi.yaml
    └── authorization-boundary.md
```

`tasks.md` is intentionally not created by this planning step.

### Source code (existing six-project solution)

```text
src/
├── SmartRail.Domain/Identity/                 # role values and assignment invariants
├── SmartRail.Application/Identity/             # role-resolution and authorization ports/use cases
├── SmartRail.Infrastructure/Identity/          # EF mappings and PostgreSQL adapters
├── SmartRail.Infrastructure/Migrations/        # additive Identity migration
├── SmartRail.Api/Authentication/               # Cognito JWT configuration and validation
├── SmartRail.Api/Authorization/                # HTTP policy adapter and safe challenges
├── SmartRail.Api/Endpoints/                    # GET /api/me only
└── SmartRail.Api/Program.cs                    # composition and middleware pipeline

tests/
├── SmartRail.UnitTests/Identity/                # Domain/Application decision tests
└── SmartRail.IntegrationTests/Identity/         # PostgreSQL, JWT and API tests
```

These are planned locations inside the current projects, not new projects. Reuse the assembly configuration scan in `SmartRailDbContext`; add Identity EF configurations without altering Railway Network entities, endpoints or OpenAPI. Add the Identity migration and update the shared EF model snapshot as required by the existing migration system.

**Structure Decision**: Extend the existing six-project Clean Architecture solution using feature namespaces. API owns ASP.NET Core authentication/authorization adapters and HTTP mapping; Application defines framework-neutral role/scope interfaces and decisions; Domain contains role-assignment invariants; Infrastructure owns EF Core and PostgreSQL. The existing public Railway Network route definitions remain anonymous and unchanged.

## Authentication Design

1. Register the ASP.NET Core `JwtBearer` handler in API composition. Configure a trusted Cognito User Pool issuer and HTTPS OIDC metadata/JWKS discovery. Never pin signing keys in source or configuration.
2. Accept **access tokens only**. Validate signature and `kid` against the issuer's current JWKS, exact issuer, lifetime/expiry, `token_use=access`, and `client_id` against an allowlist of the approved human application clients. Reject ID tokens, M2M client tokens, wrong clients, wrong issuers, unknown keys that remain invalid after metadata refresh, and malformed or expired tokens with 401 on protected operations.
3. For Cognito access tokens, `client_id` is the app-client identifier; `aud` is present when resource binding is used. If an expected resource audience is configured, require `aud` and validate it; missing or mismatched `aud` is rejected. If no expected audience is configured, accept an access token only when `aud` is absent; reject a token carrying an unconfigured `aud`. Never substitute `client_id` for the resource audience. No client secret or token is logged or persisted by the API. Disable token saving in the bearer handler.
4. Use the verified `sub` only as an opaque string key. Do not require UUID syntax. Do not use `cognito:groups`, roles supplied by a caller, or station/journey scope claims as SmartRail authorization evidence.
5. Use the bearer handler's metadata refresh/key rollover behavior. A new signing `kid` triggers a JWKS refresh; continue HTTPS validation and fail closed if the token cannot be verified.
6. Invoke `UseAuthentication` before `UseAuthorization`. Protected endpoint policies require authentication. Keep the four Feature 001 endpoints explicitly anonymous; do not add a global fallback policy unless all four remain explicitly exempt. Their public response must remain available when a malformed/expired optional bearer token is present.

## Authorization Design

- Resolve the current active roles from PostgreSQL by the verified Cognito `sub` on **each protected authorization decision**. No in-process, distributed, or JWT-claim role cache is permitted. After a committed revocation, the next authorization check observes it.
- Combine only explicit grants from all active mapped roles in the approved RBAC matrix. A role's lack of a grant does not broaden another grant. Apply all applicable target ownership and scope rules after role evaluation.
- A valid user with no active mapped roles is authenticated but receives no protected grant. `GET /api/me` still returns the caller's subject and an empty role list. A known insufficient grant is 403. If the role or target-scope source is missing, unavailable or cannot be verified, do not grant access; return a safe unavailable response where the authority is unavailable, and never perform a protected write.
- Identity & Permissions owns station-staff-to-station scope data. Driver Operations must answer whether the driver has a current assignment to the requested journey. Incident Management supplies incident-to-station/journey relationships. Catering Adapter supplies the authenticated passenger-to-order ownership decision. Identity stores none of the latter module's assignment or order data.
- Keep authorization action identifiers mapped to the exact approved matrix actions. Do not add or broaden grants; Controller and scoped Station Staff limits stay as specified in the feature spec.
- Define an Application-layer authorization decision port with framework-neutral subject/action/resource inputs and `Allowed`, `Denied`, or `Unavailable` outcomes. API authorization handlers adapt ASP.NET Core policies to this port. Owning modules implement their own evidence ports; no module reads another module's `DbContext` or directly edits its data.
- Maintain append-only grant/revocation audit events transactionally with role assignment changes and station-scope assignment changes. Each event records its assignment, verified actor subject, change kind, UTC instant and an optional controlled evidence/correlation reference. Scope grant/revoke events roll back with the scope state and are verified for event history, actor/action/time, rollback, re-grant and prohibited mutation/deletion in PostgreSQL integration tests. Never store tokens, credentials or unnecessary personal attributes in audit data. Do not expose assignment mutation through HTTP.

## HTTP Contract and Error Semantics

See [identity-permissions.openapi.yaml](contracts/identity-permissions.openapi.yaml) and [authorization-boundary.md](contracts/authorization-boundary.md).

- `GET /api/me` requires a valid Cognito access token and uses only the token's verified `sub`. It returns `{ subject, roles }`; roles are active recognized roles from the current internal mapping. A valid identity with no active role returns an empty array. No profile fields, access tokens, group claims or resource scopes are returned.
- Protected request with absent, malformed, invalid, wrong-client, ID or expired token: **401 Unauthorized** with generic RFC 9457 Problem Details; no token validation detail or token echo.
- Valid identity but no matching active role, action grant, owner or target scope: **403 Forbidden** with generic Problem Details; no sensitive target existence or personal information disclosure. Unauthorized writes leave database state unchanged.
- Role/scope authority unavailable or mapping cannot be verified: **503 Service Unavailable** with safe Problem Details and no grant. Keep it distinguishable from a known 403 without exposing database or module internals.
- `GET /api/me` for a valid principal with no mapped role: **200 OK** with `roles: []`. Mapping unavailable: safe unavailable response; never report an empty array as a verified mapping result.
- For the four public Railway Network GET operations, both absent and invalid/expired optional tokens are treated anonymously; only existing public data is returned. No change to Feature 001's OpenAPI document or response schemas.
- Preserve Problem Details type/title/status/code/trace identifier conventions from `ProblemDetailsConfiguration`; add authentication/authorization-safe status codes without exposing exceptions, secrets or raw claims.

## Database and Migration Design

Add Identity tables and constraints in an additive EF Core migration against the existing shared context and PostgreSQL 18.6. Exact fields and invariants are in [data-model.md](data-model.md). Use text for Cognito subjects, UUIDs for SmartRail assignment IDs and the existing station key, `timestamptz` for audit instants, restrictive foreign keys and partial unique indexes for active duplicate prevention.

Role grants and their audit records are committed in one transaction. A role is active only while its assignment row is not revoked. Re-grant after revocation creates a new assignment lifecycle and audit records; do not reactivate or rewrite the old audit entry. Station-scope grants and revocations also create append-only audit records in the same transaction; PostgreSQL tests prove rollback keeps state and history consistent. Use database uniqueness and validation, not only pre-checks. Station scope assignments reference the existing Railway Network station ID and are independently active/revoked. Do not persist driver assignments or catering orders in Identity.

The initial migration is owned by Feature 001 and remains unchanged. Generate one later Identity migration; inspect the migration and PostgreSQL tests before marking it ready. The role mapping's production write mechanism and database privilege model remain gated; test-only fixtures must run in the existing isolated PostgreSQL test databases.

## Security and Operational Approval Gates

- **Role-map administration**: approve the operators, bootstrap path, separation of duties and audit evidence/retention for granting and revoking roles. Until approved, no production role changes or public admin route are enabled.
- **Station scope administration**: approve who creates/reviews/revokes station scope assignments and audit evidence before enabling production station-scoped access.
- **Cognito deployment values**: configure each environment's user-pool issuer, allowed human app-client IDs, optional API resource audience, token-use rule and client token lifetimes. Do not put secrets in committed files. Production key rotation/revocation monitoring is a release gate.
- **Audit operations**: approve retention and access controls for append-only role and station-scope audit data. The data model stores actor and time but does not invent a retention period.
- **Scope-provider readiness**: protected operations owned by a future module remain unavailable until that module supplies and tests its current scope evidence interface.

These approvals are deployment/operational gates, not permission to add administrative endpoints. They do not change the approved RBAC matrix.

## Verification Plan

Use deterministic isolated fixtures and test through real PostgreSQL for persisted role/scope and audit behavior.

1. **Domain and Application unit tests**: role assignment invariants, active versus revoked assignment, union of multiple active mapped roles, no grants from unknown roles, empty mapping behavior, deny on unavailable resolver, and exact role/action decision outcomes.
2. **PostgreSQL integration tests**: additive migration and rollback; text Cognito subject; uniqueness of active `(subject, role)` and `(subject, station)` rows; assignment/audit atomicity; audit event history; revoke and regrant; current role lookup after committed revocation; station FK and stale/revoked scope denial; mapping query failure returns unavailable, not allow.
3. **JWT handler tests**: valid Cognito access token; invalid signature; wrong issuer; wrong/unknown `kid`; key rotation refresh; expired/not-yet-valid token; `token_use != access`; disallowed `client_id`; when an expected audience is configured, missing, matching and mismatched `aud`; when no audience is configured, absent `aud` acceptance and present `aud` rejection; ID-token rejection; absence of trusted subject. Use test keys/metadata isolated to tests, never a production bypass.
4. **API integration tests with `WebApplicationFactory`**: 401 missing/invalid/expired token, 403 authenticated but unauthorized role/scope, 503 unavailable role/scope source, safe Problem Details, no side effects on denied writes, and `/api/me` 200 with roles and 200 with an empty role array. Verify only the caller subject can be returned.
5. **Public regression tests**: run all four Feature 001 GET contracts with no token, malformed token and expired token; verify same anonymous status/body behavior and no private fields. Compare Feature 001 OpenAPI byte/semantic baseline or route/schema tests to prove it was not altered.
6. **Security/privacy tests**: inspect Problem Details, response headers and captured logs for raw token, password, claims, unredacted sensitive identity, or authorization-store exception details. Verify test fixture auth cannot be enabled in normal Development/Production composition.
7. **Cross-module tests**: fake module-owned scope providers at Application/API boundaries for current assigned/revoked/unknown evidence; real provider implementations and E2E tests remain the owning modules' planning/implementation responsibility.

## Requirement-to-Evidence Traceability

| Requirement | Planned design evidence | Acceptance verification |
|---|---|---|
| SR-089 | Public/protected route boundary; explicit anonymous Feature 001 exemptions; optional invalid bearer behavior | Anonymous and invalid-token smoke/integration tests against all four existing reads; published projection unchanged |
| SR-090 | Cognito access-token validation and protected endpoint authentication | JWT validation matrix; protected route returns 401 for absent, invalid or expired token |
| SR-091 | PostgreSQL role mapping, exact RBAC evaluator, owner and module-provided scope checks | Allow/deny matrix tests; station/controller case; driver assigned/unassigned case; passenger own/other order; verify no denied write changes state |
| SR-093 | No API token persistence; documented client secure-storage boundary and controlled expiry | Client/device storage acceptance belongs to client specs; token expiry is tested in API; no plaintext access token in applicable clients |
| SR-100 | Generic auth errors, Problem Details redaction and safe logs | Negative tests search error bodies/log captures for tokens and unrestricted identifiers |
| SR-114 | Distinct 401/403/503 behavior and clients receive meaningful access-denied/error states | API response tests plus applicable passenger/operational client-state tests in their owning specs |

## Complexity Tracking

No constitution violations or extra projects are proposed. No identity microservice, generic repository, external policy engine, token cache or public administration endpoint is added.

## Canonical role representation

Persist/evaluate only the internal role codes `Passenger`, `Driver`, `StationStaff`, and `Controller`. Serialize `/api/me.roles` as the labels `Passenger`, `Driver`, `Station Staff`, and `Controller`, respectively. `Public` is anonymous access, not a role assignment or response label. This representation mapping does not change RBAC grants.
