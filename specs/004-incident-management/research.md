# Research: Incident Management

## Planning decisions

| Topic | Decision | Basis |
|---|---|---|
| Application shape | Add Feature 004 folders to the existing six projects; retain the modular monolith and inward Clean Architecture dependencies. | Constitution and current solution layout. |
| Incident authority | Feature 004 owns incident state, lifecycle and association records. Feature 001 owns station/service facts; Feature 003 owns journeys and official disruption/status facts. | Approved specifications and ownership contracts. |
| Category validation | Define an Application `IIncidentCategoryCatalog` port and Infrastructure adapter for an explicitly approved controlled list. Unknown values are invalid; unavailable/unconfigured catalogue is unavailable. Do not invent codes or accept arbitrary strings. | Approved clarification leaves the vocabulary as a pre-implementation business gate. |
| Role and scope | Reuse Feature 002 actions `CreateIncident`, `ViewIncidents`, `UpdateIncidentLifecycle`. Controller's approved Feature 004 incident scope is global but still requires current active role/grant evidence. Station Staff creation requires at least one reported station in current scope; view/update requires scope covering every linked station. | Approved RBAC/API baseline and explicit Feature 004 clarification. |
| Multiple station scope | Feature 002 `IIncidentResourceEvidence` exposes a single related station. Feature 004 must use its own authoritative active association set and invoke existing Feature 002 station-scoped authorization for each station. A single returned station cannot prove all-station scope. | Direct inspection of `AuthorizationContracts.cs`, `ResolveResourceScopeHandler.cs`, and `AuthorizeOperationHandler.cs`; no Feature 002 changes allowed. |
| Owner verification | Define ports for station/service and journey/disruption evidence with match, missing/inaccessible, inactive/stale and unavailable outcomes. Production uses genuine owner adapters only; absent or unavailable providers deny/fail unavailable. | Owner-module boundaries in Features 001/003. |
| Lifecycle/audit | Forward-only `Reported → In Progress → Resolved → Closed`; Closed terminal. Append immutable evidence on each accepted transition. Do not implement the Should Have correction/reopen workflow. | Approved clarification; preserves SR-042 without promoting SR-047/SR-051. |
| Concurrency and retries | Expected incident revision on every PATCH mutation; stable `Idempotency-Key` on POST/PATCH, scoped to verified subject and operation. Hash canonical request payload; identical retry returns persisted original outcome; different payload with same key returns 409. | Approved existing-operation contract. |
| PostgreSQL model | Add tables/configuration to the shared `SmartRailDbContext` with restrictive relations, unique association identities, transactionally stored revisions/audit/idempotency, and generated additive EF migration. | Current EF Core 10/PostgreSQL 18 setup and Constitution. |
| API contract | OpenAPI 3.1.0, document version 1.0.0; precisely three protected incident operations. No GET-by-ID or management endpoint. | Approved PDF and clarification. |
| Operations composition | Feature 004 supplies authorized incident context. Operations owns `GET /api/operations/overview` and the client composes it with Feature 003 official journey data. | Approved SR-038/SR-044 ownership decision. |
| Local verification | Reuse existing signed OIDC/JWKS fixtures and port-5433 isolated database fixture; never point integration tests at development port 5432. | Existing repository test setup and user constraint. |
| Deployment | Local-first; AWS creation and deployment are manual user work after local verification. | Constitution and project deployment decision. |

## Source requirement verification

The approved workbook's direct Feature 004 Must Haves are SR-041, SR-042 and SR-044. Their exact wording and acceptance checks remain in `spec.md`. SR-038 is a separate Operations dependency. SR-043 (Could), SR-045, SR-047, SR-051 (Should), and related client/stretch scope remain deferred as specified. The approved RBAC/API PDF lists the three Incident operations and existing grants only; `GET /api/operations/overview` is separately owned by Operations.

## Failure semantics

- Invalid body/query, malformed IDs, invalid category code, invalid transition shape: 400.
- Missing/invalid authentication: 401.
- Verified identity with no explicit grant or required station scope: 403.
- Unknown or inaccessible incident/reference: 404, avoiding existence disclosure.
- Stale expected revision or conflicting idempotency key use: 409.
- Current authorization, category catalogue, or required owner verification unavailable: 503 and no partial write.
- Cancellation during request propagates cancellation; transaction rollback is required for any incomplete write.

## Unresolved approval and implementation gates

- Category code values must be approved before successful incident creation can be enabled.
- Genuine Feature 001/003 owner adapters must be available for production reference verification; until then fail closed.
- Idempotency record retention duration has not been specified. Retain records as required for idempotent behavior; do not invent an expiry period.
- Operations and client acceptance for SR-038/SR-044 remains open beyond backend implementation.
