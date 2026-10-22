# Implementation Plan: Incident Management

**Branch**: `feature-04-incident-management` | **Date**: 2026-10-19 | **Spec**: [spec.md](spec.md)

## Summary

Implement the three approved protected Incident Management operations in the existing .NET 10 modular monolith. Feature 004 owns incident records, the forward-only lifecycle, immutable transition evidence, and explicit impact associations. It validates station/service references through Feature 001 and journey/disruption references through Feature 003, without copying those modules' authoritative state or publishing journey changes. PostgreSQL transactions enforce revision, idempotency, and audit invariants. The approved category code catalogue is a production-enablement gate: production creation remains disabled until the business owner supplies the controlled values. Local implementation and automated verification may use explicit test-only values isolated from normal Development/Production configuration.

Direct Must Have scope is SR-041, SR-042, and SR-044. SR-038 remains an Operations endpoint/client dependency. Applicable identity, authorization, privacy, reliability and client requirements are traced in [spec.md](spec.md); backend evidence alone does not close owner-module or client acceptance.

## Technical Context

**Language/Version**: C# / .NET 10  
**Primary Dependencies**: ASP.NET Core Minimal APIs, EF Core 10, Npgsql, existing Feature 002 authorization, existing Feature 001/003 owner interfaces  
**Storage**: Existing PostgreSQL 18 database and shared `SmartRailDbContext`; additive migration  
**Testing**: xUnit; unit tests plus real PostgreSQL integration tests and API tests  
**Target Platform**: Existing local-first SmartRail service; AWS setup remains manual and outside this plan  
**Project Type**: Six-project modular monolith  
**Performance Goals**: No new numeric threshold; do not invent one  
**Constraints**: Exactly three incident routes; existing RBAC actions only; protected records; current authorization evidence; owner unavailability fails closed; no migration applied during planning  
**Scope**: Incident create/read/update, lifecycle, references and associations only. Operations overview and all client workflows remain owned elsewhere.

## Constitution Check

| Principle | Plan evidence |
|---|---|
| Approved scope and traceability | SR-041/042/044 remain direct Must Haves. SR-038 and applicable cross-cutting SRs are separately traced; downstream acceptance stays open. |
| Specification before implementation | Approved clarifications and category catalogue gate are recorded in the spec/checklist. This plan defines technical choices only. |
| Modular monolith and inward dependencies | Uses the existing six projects. Domain has no framework/persistence dependency; Application defines ports; Infrastructure implements; API transports and composes. |
| Sources of truth and integrity | Feature 001 owns stations/services; Feature 003 owns journeys/status/disruptions; Feature 004 stores references and associations only. |
| Server-side authorization | Current Feature 002 role/grants checked for each protected operation. Controller incident grants are global only for Feature 004; Station Staff must cover every linked station. |
| Approved API contracts | Exactly `GET /api/incidents`, `POST /api/incidents`, `PATCH /api/incidents/{id}`. `GET /api/operations/overview` remains Operations-owned. |
| Evidence-based quality | Domain, application, API, real PostgreSQL, security, owner-outage and regression tests planned. |
| Local-first and deployable boundaries | Reuses local PostgreSQL/test isolation and signed OIDC fixtures. No AWS provisioning/deployment. |

**Gate result**: Pass for planning. Category values are a business approval gate before incident creation can be enabled; plan and test scaffolding may proceed without inventing values.

## Architecture and Project Structure

No projects are added. Feature files are organized within the existing project structure:

```text
src/SmartRail.Domain/Incidents/
  Entities/Incident.cs
  Entities/IncidentRevision.cs
  Entities/IncidentAssociation.cs
  Entities/IncidentIdempotencyRecord.cs
  IncidentState.cs
  IncidentCategoryCode.cs
src/SmartRail.Application/Incidents/
  Persistence/                    # incident repository/unit-of-work ports
  References/                      # station/service/journey/disruption verification ports
  Categories/                      # controlled catalogue port
  Authorization/                   # incident access policy orchestration
  UseCases/                         # create, list, update handlers
src/SmartRail.Infrastructure/Incidents/
  Configurations/                   # EF mappings and indexes
  Persistence/                      # transactional PostgreSQL adapters
  References/                        # adapters to Feature 001/003 owner data
  Categories/                        # approved local/reference catalogue adapter
  Migrations/                        # generated additive migration in shared folder
src/SmartRail.Api/Endpoints/Incidents/
  IncidentEndpoints.cs
  Contracts/                         # request/response DTOs and mapping
tests/SmartRail.UnitTests/Incidents/
tests/SmartRail.IntegrationTests/Incidents/
```

These are planned locations, aligned to the repository's actual projects. Reuse existing conventions and `SmartRailDbContext` configuration discovery rather than introduce generic frameworks or duplicate owner models.

### Dependency direction

- Domain owns state transitions, immutable event construction, revision and association invariants.
- Application owns use-case orchestration and framework-neutral ports. It depends only on Domain.
- Infrastructure implements repositories, category lookup, owner-reference verification, and EF Core persistence.
- API validates transport shape, derives the verified subject from Feature 002, maps results to the approved status codes, and never implements business authorization itself.
- Operations consumes the protected incident read and composes it with Feature 003 status. The Feature 004 API does not implement `/api/operations/overview`.

## Authorization and Cross-Module Evidence

Use existing `PermissionAction.CreateIncident`, `ViewIncidents`, and `UpdateIncidentLifecycle`; do not alter Feature 002's matrix. Resolve current active roles for each protected operation. Controller has global incident scope for these existing grants as explicitly approved for Feature 004; the role/grant is still checked against PostgreSQL every time, and the target incident/reference must be valid. Station Staff creation requires at least one reported station in current scope; a service-only report does not establish station scope. Verify every supplied reference with its owner. Reads and updates require current station scope for **every** station associated with the incident; journey links do not widen scope. When creation links multiple stations, later read/update is denied unless the caller covers all of them. Controller access remains governed by current global incident grants.

Feature 002's current `IIncidentResourceEvidence` returns at most one `StationId`, so it cannot alone establish the approved multi-station rule. Do not change Feature 002. Feature 004's Application use case must load the authoritative active station association set and evaluate each station through the existing Feature 002 station-scoped authorization path. Any denied station denies the incident operation; any unavailable role/scope evidence yields unavailable and no write. The Feature 004 Infrastructure adapter for `IIncidentResourceEvidence` verifies incident existence for the existing authorization boundary; it is not treated as proof that all stations are in scope. Test both the all-stations-required rule and this boundary explicitly.

Owner interfaces are required for Feature 001 station/service verification and Feature 003 journey/disruption verification. They return verified match, verified missing/inaccessible, stale/inactive where relevant, or unavailable. No production fake provider is allowed. Before owner modules are available, production registration must fail closed. Deterministic doubles exist only in tests. New journey associations must be active when added; later state changes preserve historical links. Incident writes never call Feature 003 publication commands.

## Data and Transaction Design

See [data-model.md](data-model.md). Each successful create or PATCH mutation is one transaction. Create atomically writes incident revision 1, initial active associations, and POST idempotency result. PATCH checks `ExpectedRevision`, applies a permitted state transition and/or association delta, increments revision once, appends transition evidence where state changes, and persists the idempotency result. Unique constraints arbitrate same-key races; a matching replay returns the persisted original outcome, while different request content under the same subject/operation/key returns 409. Stale revisions return 409 without any changes. EF migration is additive and generated by EF Core in the shared migrations directory; no earlier migration is edited and none is applied as part of planning.

## API and Error Contract

See [contracts/incident-management.openapi.yaml](contracts/incident-management.openapi.yaml), [contracts/authorization-and-ownership.md](contracts/authorization-and-ownership.md), and [contracts/lifecycle-and-associations.md](contracts/lifecycle-and-associations.md). OpenAPI is 3.1.0, contract version 1.0.0, and contains exactly three Incident operations. All are protected. No `GET /api/incidents/{id}` or administration endpoint is added. Safe RFC 9457 errors map malformed/unknown category to 400, missing/invalid credentials to 401, verified insufficient grants/scope to 403, unknown or inaccessible resources to 404, stale revision or idempotency conflict to 409, and unavailable authorization/owner/category authority to 503. Category values remain an approval gate; arbitrary values are never accepted.

Public Feature 003 responses remain unchanged and contain no private Incident records, reporter identity, audit evidence or internal links. Incident API projections include a safe server-owned `source: IncidentManagement` classification and report timestamps, but omit private reporter subject, actor evidence and raw audit history. The immutable internal revision evidence records actor, source and timestamp. `GET /api/operations/overview` remains in the approved Operations boundary.

## Verification Strategy and Requirement Traceability

| Requirement/outcome | Planned evidence |
|---|---|
| SR-041 / FR-001–003 / SC-001 | Domain validation and authorization unit tests; API 201/400/401/403/404/503 tests; PostgreSQL atomic creation, idempotent POST, and no-write-on-failure tests. Successful create acceptance remains gated on approved category values. |
| SR-042 / FR-005–006 / SC-002, SC-003 | Transition graph and immutable evidence unit tests; PostgreSQL revision/audit transaction and stale-write tests; API 200/409/replay tests. Operations dashboard closure evidence for SC-003 is separately owned and remains open. |
| SR-044 / FR-007–009 / SC-004 | Owner-port tests; PostgreSQL distinct link, add/remove, historical-retention tests; no automatic Feature 003 update test; regression verifies public snapshot privacy. Operations client composition and owner-module evidence remain open. |
| SR-038 | Incident context API contract plus separate Operations-owned overview integration/client acceptance. Feature 004 backend does not close SR-038. |
| SR-089/090/091 / SC-005 | Authenticated route tests for missing/invalid identity, explicit current grants, all-stations scope, revoked/unavailable evidence, and unchanged Feature 001/003 reads. |
| SR-010/046/048 / SC-006 | Regression tests prove incident changes produce no Feature 003 publication; cancellation remains a separate authorized Feature 003 operation. |
| SR-095/100 / SC-007 | API DTO allowlist, captured safe-log assertions, malicious text handling, no reporter/actor subject in projection, no authorization evidence in public journey response. Client rendering evidence remains downstream. |
| SR-109/114 / SC-008 | Backend status/error distinctions are contract-tested; owning client loading/empty/denied/error/retry/no-network/freshness acceptance remains open. |
| SR-113/117 / SC-009 | Local clean-start and simulated Catering outage tests show authorized incident reporting, reads and lifecycle remain independent of Catering. |

Required regression suite includes all existing unit tests, isolated PostgreSQL integration tests, API integration tests using the existing real signed local OIDC/JWKS fixtures, and Feature 001/002/003 regression tests. Persistence tests must use the established port-5433 isolated database fixture and never the development database on port 5432. Add a Feature 004 collection or equivalent isolation mechanism to prevent concurrent destructive operations against one test database.

### Downstream acceptance stays open

- SR-038: Operations-owned `GET /api/operations/overview` and its authorized summary/client evidence.
- SR-044: genuine Feature 003 owner verification plus Operations-client composition showing each incident impact with correct official journey data.
- Incident UI, loading/empty/denied/error/retry/offline states, safe text display and lifecycle presentation belong to owning client specifications.
- No whole-MVP acceptance claim may be based on Feature 004 backend tests alone.

## Local Development and Deployment Boundary

Use the existing Docker PostgreSQL services and current local authentication fixture. Integration tests use only disposable databases on loopback port 5433. Do not run destructive tests against `smartrail_dev` on port 5432. Work locally through build, API tests, owner integration tests and regression before the user performs manual AWS setup. No AWS resources, deployment commands, IAM, Cognito, RDS, API Gateway, or Lambda configuration is part of Feature 004 planning or implementation.

## Remaining Gates and Risks

1. **Business gate — category codes**: approved category vocabulary must be supplied before production incident creation is enabled. Isolated local tests may use explicit test-only values; no default, free-text category, or silently permissive mode is allowed.
2. **Owner integration gate**: production Feature 001/003 verification adapters must be available and authoritative. Until then those calls fail closed; test doubles are test-only.
3. **Downstream acceptance**: Operations summary/composition and client evidence for SR-038/SR-044 remain open.
4. **Existing auth boundary**: Feature 002 incident evidence exposes one station. Feature 004 must perform per-associated-station checks via the existing station-scope path and test it; do not rely on the single-station evidence field for all-stations authorization.
5. **Idempotency retention**: preserve request records for the retry contract. No expiry/retention interval is specified; do not invent one or purge records until policy is approved.

These gates do not block technical task generation. Gate 1 blocks successful production incident creation; Gate 2 blocks accepting unverified owner links and production integration until genuine adapters exist.

## Complexity Tracking

No constitution exceptions or additional projects are proposed.
