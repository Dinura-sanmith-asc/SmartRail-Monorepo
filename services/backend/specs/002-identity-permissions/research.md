# Research: SmartRail Identity & Permissions

**Feature**: [spec.md](spec.md)
**Date**: 2026-10-18

## Decisions

### 1. Cognito authenticates; SmartRail PostgreSQL mapping authorizes

**Decision**: Follow approved clarification Option B. Validate a Cognito access token, take its opaque `sub` as the authenticated principal, and read that subject's active SmartRail role assignments from the internal PostgreSQL mapping for every protected authorization check. Ignore `cognito:groups` and all client-supplied role/scope values for authorization. Combine only explicit grants belonging to current active mapping rows, then apply required resource ownership and scope checks. No active/unknown role grants nothing; unavailable mapping returns no authorization grant.

**Rationale**: This is the user's approved source-of-truth rule and provides role revocation at the next request-time authorization decision, even if a still-valid JWT contains old group claims. It aligns with the handoff's Identity & Permissions module ownership of Cognito-subject mappings and fits the current shared PostgreSQL/EF Core architecture.

**Alternatives considered**: JWT Cognito group claims as live SmartRail roles (rejected; can remain stale until token refresh and is expressly superseded); a new identity service (rejected; conflicts with the modular-monolith constitution); per-process role caching (rejected; conflicts with next-check revocation).

### 2. Validate Cognito access tokens using Cognito-specific claims

**Decision**: Use ASP.NET Core `JwtBearer` with HTTPS OIDC metadata/JWKS from a configured trusted Cognito User Pool issuer. Validate RSA signature and `kid`, exact issuer, lifetime/expiry, `token_use=access`, and `client_id` against configured human app clients. If an expected API resource audience is configured, require an `aud` claim and require it to match that expected audience; missing or mismatched `aud` is rejected. If no expected audience is configured, accept only access tokens with no `aud`; reject a token that carries an unconfigured audience. Never treat `client_id` as a substitute for `aud`. Reject ID tokens and app clients not approved for human SmartRail use.

**Rationale**: Cognito access tokens carry `client_id` and `token_use`; an access-token `aud` claim is present when resource binding is configured. Explicit behavior for configured, absent and mismatched audience values prevents accepting a resource-bound token without validating its intended API. Cognito documents `sub` as the stable user key and cautions against strict UUID-format validation. Access and ID tokens can have different signing keys. Signing keys are discovered and refreshed from JWKS rather than hard-coded. Microsoft's ASP.NET Core guidance supports `JwtBearer`, full signature/issuer/lifetime validation, metadata refresh and safe control of error-detail exposure.

**Alternatives considered**: Accept ID tokens or any Cognito-signed token (rejected; wrong use/client can cross a trust boundary); treat `client_id` as the API resource audience (rejected because they identify different trust boundaries); ignore an `aud` claim when no expected audience is configured (rejected because an unvalidated resource-bound token must not be accepted); static key pinning (rejected because Cognito rotates keys).

**References**:

- [Understanding the access token — Amazon Cognito](https://docs.aws.amazon.com/cognito/latest/developerguide/amazon-cognito-user-pools-using-the-access-token.html)
- [Verifying JSON web tokens — Amazon Cognito](https://docs.aws.amazon.com/cognito/latest/developerguide/amazon-cognito-user-pools-using-tokens-verifying-a-jwt.html)
- [Configure JWT bearer authentication in ASP.NET Core 10](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-10.0)
- [JwtBearerOptions — ASP.NET Core 10](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.authentication.jwtbearer.jwtbeareroptions?view=aspnetcore-10.0)

### 3. Read live authorization state for each protected request

**Decision**: Resolve active roles and needed scope evidence for each authorization evaluation with indexed database/module lookups and no authorization cache. Persist role grant/revocation and its audit event in one PostgreSQL transaction. Once revocation commits, the next check must observe the inactive mapping. A lookup failure yields `Unavailable` and no grant; do not convert an unknown result to an allow or empty verified role set.

**Rationale**: Gives the requested immediate-on-next-check revocation semantics with a simple source of truth. It incurs a database read and means protected operations depend on PostgreSQL/authoritative module availability. No numeric latency target is approved; indexed queries can be measured later.

**Alternatives considered**: JWT claim-only evaluation or TTL cache (rejected because stale grants outlive a role change); fail-open on database timeout (rejected); remove historic role rows on revoke (rejected because the audit history must remain).

### 4. Keep resource evidence in its owning module

**Decision**: Persist station-staff station scopes in Identity & Permissions, with an append-only actor/time/action audit event for every grant and revocation in the same transaction as the scope-state change. Verify persisted grant/revoke history, re-grant, actor/action/time values and atomic rollback using real PostgreSQL integration tests. Consume driver-to-journey evidence from Driver Operations, incident/resource relationships from Incident Management, and passenger/order ownership from Catering Adapter. Do not replicate those modules' operational or order records. Use framework-neutral Application ports; future owning modules implement their evidence queries through their own persistence rules. Missing, revoked, stale, or unverifiable evidence denies the grant; source outage returns unavailable without a write.

**Rationale**: Preserves the approved clarification and constitution's module-owned data rules while providing a single server-side authorization decision boundary.

**Alternatives considered**: Centralize every module's assignments/orders in Identity (rejected; duplicates ownership); trust caller-supplied resource IDs or token scope claims (rejected; untrusted/stale); introduce a policy engine or service bus (rejected as unnecessary architecture).

### 5. Expose only the inventoried self-identity operation

**Decision**: Add an OpenAPI contract for the existing approved `GET /api/me` inventory entry. It requires a verified Cognito access token and returns `{ subject, roles }` from the internal mapping. Persisted role codes are `Passenger`, `Driver`, `StationStaff`, and `Controller`; response labels are `Passenger`, `Driver`, `Station Staff`, and `Controller`. A valid caller with no active role receives an empty `roles` array. No caller-selected subject, resource scopes, profile attributes, credentials or token are returned. No other Identity HTTP operation is introduced.

**Rationale**: Implements the PDF's existing Identity endpoint inventory while honoring the clarified minimal caller-only response and avoiding unapproved account or role administration.

### 6. Keep authentication failures distinct from authorization failures

**Decision**: On a protected endpoint, invalid/missing/expired/wrong-client token -> 401; valid principal with known insufficient role/owner/scope -> 403; role/scope authority unavailable -> 503. Use generic RFC 9457 Problem Details and existing `ProblemDetailsConfiguration` conventions. A 403 or 503 write denial has no state change. On the four explicit anonymous Railway Network endpoints, ignore invalid/expired optional bearer credentials and serve the same public projection.

**Rationale**: Distinguishes authentication, authorization and dependency failure without revealing credentials or protected resource existence. It preserves the approved Feature 001 contract and the final clarification.

### 7. Use the current six-project solution and shared EF migration pipeline

**Decision**: Add Identity feature namespaces to the existing `SmartRail.Domain`, `SmartRail.Application`, `SmartRail.Infrastructure`, API, UnitTests and IntegrationTests projects. Add an additive migration to the existing shared `SmartRailDbContext` migration pipeline. Reuse built-in JwtBearer/authorization, OpenAPI, Problem Details and the existing per-class PostgreSQL integration fixture. Add only JwtBearer 10.0.12, aligned with existing .NET packages.

**Rationale**: Matches actual repository references and the ratified modular-monolith/Clean Architecture decisions. No new context, deployable, database, framework layer, external authorization product or project is justified.

### 8. Local-first implementation and user-operated AWS deployment

**Decision**: Implement and verify Feature 002 and the complete SmartRail system locally before AWS deployment. Keep local settings/secrets separate from AWS values. Use deterministic isolated signing keys, metadata and identity fixtures for tests only; test authentication configuration must not create a production bypass. AWS resource creation, IAM, Cognito environment configuration, RDS, API Gateway, Lambda, SAM, and deployment commands are performed manually by the user with step-by-step guidance after local verification. No AWS resources are provisioned and no AWS deployment command is run during feature implementation.

**Rationale**: This follows the user's explicit deployment workflow while preserving the approved AWS target architecture and keeping local verification reproducible.

**Database references**:

- [PostgreSQL 18: CREATE INDEX](https://www.postgresql.org/docs/current/sql-createindex.html) describes partial unique indexes as a way to enforce uniqueness over a subset of rows; use one for at most one active assignment per subject/role and subject/station.
- [EF Core indexes](https://learn.microsoft.com/en-us/ef/core/modeling/indexes) and [Npgsql EF Core indexes](https://www.npgsql.org/efcore/modeling/indexes.html) support modeling indexes through the existing provider.

## Approved-source verification

- The exact Must Have wording and workbook acceptance checks for SR-089, SR-090, SR-091, SR-093, SR-100 and SR-114 remain in `spec.md` and are mapped to acceptance tests in `plan.md`.
- The approved RBAC table in `spec.md` and PDF transcription in the handoff are the grant authority; no grants are modified by this design.
- Feature 001's OpenAPI marks its four routes anonymous. Their paths, schemas and public behavior remain unchanged; regression tests cover malformed/expired optional bearer tokens.
- The RBAC/API PDF inventories `GET /api/me` as authenticated with projection TBD; the approved clarification now specifies subject and roles only.

## Remaining production approvals (not technical research gaps)

The plan records but does not invent: authorized role-mapping operators/bootstrap and audit retention; authorized station-scope provisioning/review/revocation; per-environment Cognito user pool issuer, human app-client allowlist, resource audience (if enabled), expiry and operational revocation settings. These are production approval/release gates, not reasons to add public admin endpoints.
