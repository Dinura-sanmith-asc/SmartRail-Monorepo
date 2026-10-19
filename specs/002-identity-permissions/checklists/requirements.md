# Specification Quality Checklist: SmartRail Identity & Permissions

**Purpose**: Validate specification completeness and quality before proceeding to planning
**Created**: 2026-10-17
**Feature**: [spec.md](../spec.md)

**Status distinction**: The checklist records specification quality and backend verification evidence. The approval record is in [spec.md](../spec.md): approved by the SmartRail Project Owner on 2026-10-19 for client integration. Backend implementation and local verification are recorded complete. Production deployment readiness and platform-wide acceptance are not established here.

## Content Quality

- [x] No implementation details beyond mandated Cognito JWT and existing route-contract boundaries
- [x] Focused on approved user value and access-control needs
- [x] Written for product, operations and engineering reviewers
- [x] All mandatory template sections completed

## Requirement Completeness

- [x] Unresolved production role/scope governance approvals are explicitly listed and do not alter the defined authorization behavior
- [x] Requirements are testable and unambiguous within the approved RBAC boundary
- [x] Success criteria are measurable
- [x] Success criteria are technology-agnostic
- [x] Acceptance scenarios cover anonymous, authenticated, denied and scoped-access paths
- [x] Edge cases cover invalid/expired credentials, missing scope, ownership and safe failures
- [x] Scope clearly excludes unapproved identity administration and downstream feature workflows
- [x] Dependencies and assumptions are identified, including Cognito, resource-owning modules and SR-094
- [x] Exact wording, priority and source acceptance checks are retained for applicable Must Have requirements

## Feature Readiness

- [x] Each functional requirement maps to an approved SR, API-inventory entry, RBAC baseline or explicit scope boundary
- [x] User scenarios cover public access, protected authentication, role/scope authorization and session safety
- [x] Success criteria can be verified against the defined outcomes
- [x] Feature preserves Feature 001's four anonymous Railway Network reads and existing Clean Architecture/module ownership
- [x] Cross-module authorization responsibilities do not claim implementation ownership for Journeys, Incidents, Driver Operations or Catering
- [x] No public identity-management or administrative endpoint is introduced
- [x] Internal role codes and external `/api/me` labels are defined consistently, including the `StationStaff` / `Station Staff` representation mapping
- [x] Station-scope grant/revocation audit history is transactionally defined and has PostgreSQL verification coverage
- [x] Cognito access-token resource-audience behavior is explicit for configured, missing and mismatched `aud` claims
- [x] Local-first verification and user-operated manual AWS deployment boundaries are documented without changing the target architecture

## Notes

- Reviewed against the approved requirements workbook, RBAC/API PDF, SmartRail constitution, handoff and Feature 001 OpenAPI contract.
- SR-094 remains a downstream Catering Adapter obligation; its authenticated provider boundary is recorded as a dependency only.
- Production approval gates for maintaining the internal role mapping and station scopes are listed in `spec.md`; the authorization behavior and `/api/me` response contract are clarified.
- The second clarification answer is preserved verbatim; its token-group source wording is marked SUPERSEDED, while its explicit-grant union applies to roles from the internal mapping.
- The specification-quality checks above remain distinct from the implementation evidence recorded below.

## Implementation Verification Evidence — Phase 7 (2026-10-18)

Feature 002 backend evidence was verified by the complete local solution suite (72 unit tests and 34 integration tests, 0 failures, 0 skipped) and a dedicated local HTTP smoke test (1 passed). PostgreSQL persistence and API tests used isolated databases on the dedicated test service; the development Identity migration was applied only to local `smartrail_dev` on port 5432.

| Requirement | Feature 002 evidence | Feature/backend status | Overall MVP status |
|---|---|---|---|
| SR-089 | `PublicRailwayReadRegressionTests` and `IdentityLocalSmokeTests`: each of the four Feature 001 GET operations returns fixture public data without a token, malformed token or expired optional token. OpenAPI regression confirms the four route contracts remain present. | Verified for the backend public-read boundary. | Release E2E and cross-module acceptance remain outside this feature. |
| SR-090 | `CognitoJwtValidationTests`, `CurrentIdentityEndpointTests`, `IdentityLocalSmokeTests`: real RSA/JWKS signature, trusted issuer, expiry, `token_use`, allowed client, audience rules, invalid-token 401, mapped/empty identity 200 and unavailable mapping 503. | Verified for Feature 002 authentication and `/api/me`. | Feature 002 backend evidence recorded. |
| SR-091 | `AuthorizationEndpointTests`, `AuthorizationMatrixTests`, `AuthorizationRevocationIntegrationTests`: exact role/action matrix, multi-role union, station/journey/incident/order evidence, 403 denial, 503 unavailable evidence, no denied write effect, and role/scope revocation observed on the next request against PostgreSQL. | Verified for the Identity authorization boundary using isolated module-provider evidence fixtures. | Cross-module acceptance remains dependent on owner-module implementations and release E2E evidence. |
| SR-100 | `AuthenticationPrivacyTests`: Problem Details and captured application logs exclude raw bearer tokens, password-like values, private claims and signing-key identifiers; responses avoid provider/connection details. | Verified for Feature 002 API errors/logging. | Feature 002 backend evidence recorded. |
| SR-093 | JWT expiry is enforced by the API; Feature 002 does not persist access tokens. | Backend behavior verified. | **Open** until Flutter Driver and applicable operational clients demonstrate secure credential storage, no plaintext token cache and controlled session expiry with implementation and automated tests. |
| SR-114 | API returns safe 401/403/503 outcomes; authorization is checked server-side for protected work. | API portion verified. | **Open** until applicable Flutter passenger/driver and React operational primary workflows test loading, no-data, 403, 500, retry and no-network states. |

The repository did not contain the owning Flutter/React feature specifications or their implementation/test evidence at this checkpoint. Their links and evidence must be added when those specifications exist. Feature 002 backend tests do not close SR-093, the client portion of SR-114, or overall MVP acceptance.
