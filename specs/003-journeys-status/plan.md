# Implementation Plan: SmartRail Journeys & Status

**Branch**: `feature-03/Journeys-&-Status` | **Date**: 2026-10-19 | **Spec**: [spec.md](spec.md)

**Input**: Clarified Feature 003 specification, approved Feature 001 schedules, Feature 002 identity/authorization, and approved MoSCoW and RBAC/API baselines.

## Summary

Add dated operational journeys to the existing six-project .NET 10 modular monolith. A journey has an authoritative opaque `journeyInstanceId`, while the service, published schedule revision, operation date, and first-calling-point departure occurrence preserve its planned schedule linkage. Distinct journey IDs may share that linkage. A controlled, non-public initializer issues and audits identities; it is deny-by-default until production authority is approved. Official status, diversion, and stop changes append immutable revisions and audit evidence in PostgreSQL. Existing journey/update reads expose safe, detectable cancellation/platform evidence by change type, UpdateId, revision, source, and publication time; the passenger client owns alert presentation/delivery. Reads project one committed revision consistently. Controller writes resolve current Feature 002 roles/grants on every operation; the approved Controller grant has global journey scope, while the target journey/action is validated and cancellation requires its separate grant. Driver observations remain owned by Driver Operations and never alter official status by themselves. Incident Management owns incident associations and authorized context reads; the operations client composes that context with Feature 003 journey data, without adding incident fields to public snapshots.

## Technical Context

**Language/Version**: C# / ASP.NET Core .NET 10 (`net10.0`).

**Primary Dependencies**: Existing EF Core 10.0.12, Npgsql EF Core 10.0.3, ASP.NET Core OpenAPI 10.0.12, xUnit and `WebApplicationFactory`; no new project or framework is proposed.

**Storage**: Existing PostgreSQL 18 Docker Compose services and shared `SmartRailDbContext`; add an additive Journeys & Status migration and update the shared model snapshot. Never alter prior migration files.

**Testing**: `tests/SmartRail.UnitTests` for Domain/Application rules; `tests/SmartRail.IntegrationTests` for isolated real-PostgreSQL persistence and API tests. Reuse `PostgreSqlTestDatabaseFixture`, which creates a unique database on loopback port 5433 and rejects non-test connection targets.

**Target Platform**: Existing .NET API; AWS Lambda/API Gateway remains the approved target, but this feature is developed and verified locally. No AWS provisioning or deployment is part of implementation.

**Project Type**: REST web service, modular monolith.

**Performance Goals**: No new latency, throughput, polling, retention, or availability threshold is approved. Add only indexes justified by the specified lookups and validate query behavior with PostgreSQL acceptance data.

**Constraints**: Preserve all six projects and inward dependency direction. Domain has no API/EF/AWS dependencies; Application depends on Domain and owns ports/use cases; Infrastructure owns EF/PostgreSQL and cross-module adapters; API owns HTTP/authentication mapping. Reuse Feature 001 schedule and station IDs without mutating its published schedule. Reuse Feature 002 Cognito authentication, active PostgreSQL role mapping, exact RBAC grants, and current resource-scope decisions. Define an Application-owned incident-context port, but do not fabricate its production provider before Incident Management exists. The operations client composes authorized incident context; public journey projections remain incident-free. Do not add operations, grants, modules, or client workflows.

**Scale/Scope**: Dated journey identity, stop snapshots, official updates, controlled initialization, station departure projection, and five existing API operations. Significant cancellation/platform evidence is available through existing reads; passenger alert delivery is downstream client work. Incident context remains owner-read and operations-client composition. User count, journey volume, retention period, feed freshness, and notification latency remain unquantified.

## Approved Decisions and Design Proposals

- **Gate 1 approved**: `journeyInstanceId` is a separate authoritative identity. The approved composite is schedule linkage, not a unique journey key. A controlled non-public manifest/command supports no-feed initialization. Record actor, provenance, source key, schedule linkage and time; same source key with identical linkage is idempotent; conflicting linkage is rejected and audited; distinct IDs sharing a composite remain distinct. Ambiguous passenger selection is explicitly represented and never auto-resolved.
- **Gate 2 approved**: OpenAPI `1.0.0`, no `/v1` path prefix, exactly five existing operations. Diversion is a journey-specific full ordered stop replacement on the status PATCH; stop PATCH changes only one stop's platform/timing. Writes require `expectedJourneyRevision`; actor/time are server-derived; optional verified `driverObservationId` and correction linkage are audit evidence. Stale revisions return 409 with no partial write. Reads identify one consistent published revision.
- **Lifecycle approved**: scheduled to active/cancelled states; active states may transition among active states or to completed/terminated. Cancelled means not commenced, terminated means ended before planned final stop, completed means planned end reached. Terminal states are not reopened by ordinary updates; an authorized Controller may append a linked correction. Current status mapping is in [research.md](research.md).
- The concrete persistence shapes, UUID representation, concurrency token, and minimal query fields in this plan are technical design proposals consistent with those approvals. No proposed representation changes the approved requirement behavior or RBAC.

## Constitution Check

| Principle | Result | Plan evidence |
|---|---|---|
| I. Approved scope and traceability | PASS | [research.md](research.md) maps direct SR requirements and downstream evidence ownership; no Must Have is removed or declared accepted from backend-only evidence. |
| II. Specification before implementation | PASS | Uses clarified spec and approved gates; creates design artifacts only. `tasks.md` is intentionally absent. |
| III. Modular monolith and inward dependencies | PASS | Extends the current six projects; no new project, generic framework, or cross-module ownership transfer. |
| IV. Data integrity and sources of truth | PASS | Feature 001 owns reusable schedules/stations; Journeys owns dated instance and official publication; Driver Operations owns observations; Incident Management owns incident lifecycle. Revisions and audits are atomic and append-only. |
| V. Identity, scope, and safety | PASS | Feature 002 evaluates current mapped Controller grants on each write; the approved grant supplies global journey scope. The target is validated, cancellation requires its separate grant, and Driver/Station Staff scopes and owner evidence remain enforced. Missing/unavailable required evidence fails closed. No JWT group or caller-supplied actor is trusted. |
| VI. API contracts | PASS | Exactly five approved paths in [OpenAPI](contracts/journeys-status.openapi.yaml); no new grant/path and no mutation of Feature 001/002 contracts. |
| VII. Evidence-based quality | PASS WITH OPEN ACCEPTANCE | Domain, real PostgreSQL, API, OpenAPI, concurrency, denial-side-effect, and F001/F002 regression tests are planned and traced. Cancellation/platform evidence is backend-tested; passenger alert delivery and SR-044 operations-client composition remain open until client and owner-module evidence exists. |
| VIII. Honest operation | PASS | Missing/stale ETA and source information remains unknown/unavailable; reads expose revision/source/time; no invented freshness or latency threshold. |
| IX. Approved stack and deployment boundary | PASS | Existing .NET/EF/PostgreSQL stack; local-first only; AWS is manual user work after local verification. |

### Post-design gate

**PASS with downstream acceptance open.** The Gate 1 identity and Gate 2 API decisions, six follow-up contract details, and the 2026-10-19 FR-013/SR-025 and SR-044 resolutions are operative. No API-contract approval remains before task execution. Cancellation/platform evidence is supplied by existing journey/update reads; passenger-client alert delivery is separate and remains open. Incident Management owns authorized incident context, which the operations client composes with official journey data; no private incident data is added to public snapshots, and no production provider is assumed before the owner module exists. Production journey-initialization authority, operator/source-key governance, and audit retention/approval remain operational gates; use a deny-all implementation default until approved.

## Project Structure

### Documentation

```text
specs/003-journeys-status/
├── spec.md
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
└── contracts/
    ├── journeys-status.openapi.yaml
    ├── journey-initialization.md
    └── authorization-and-ownership.md
```

`tasks.md` is intentionally not created by this planning step.

### Existing source and test projects

```text
src/SmartRail.Domain/JourneysStatus/             # entities and lifecycle invariants
src/SmartRail.Application/JourneysStatus/         # use cases, repository and owner-evidence ports
src/SmartRail.Infrastructure/JourneysStatus/      # EF configuration, repositories, initializer
src/SmartRail.Infrastructure/Migrations/          # additive migration and shared snapshot
src/SmartRail.Api/Endpoints/JourneysStatus/       # five approved HTTP operations
src/SmartRail.Api/Program.cs                      # existing composition/middleware
tests/SmartRail.UnitTests/JourneysStatus/         # Domain and Application tests
tests/SmartRail.IntegrationTests/JourneysStatus/  # real PostgreSQL, API, contract tests
```

These are proposed feature folders in existing projects, not new projects. Follow local naming and existing feature organization if it differs; keep dependency direction unchanged.

## Delivery and Verification Approach

1. Use the approved contract semantics in `spec.md` and OpenAPI as the source for implementation tasks; preserve the separately open production initialization authority gate. Existing journey/update reads expose significant-change evidence; passenger alert delivery is owned by the passenger client and stays open until tested.
2. Add Domain types and lifecycle/identity invariants; add Application ports and handlers without framework dependencies.
3. Add Infrastructure mappings, repository transactions, revision concurrency, controlled initializer, and additive migration.
4. Add the five API operations and safe Problem Details mappings; bind each protected action to a current Feature 002 role/grant check. Controller grants are globally scoped across journeys, with target validation and the separate cancellation grant; preserve Driver/Station Staff scopes.
5. Verify Domain/Application behavior, isolated PostgreSQL schema/constraints/audits/rollback/races, HTTP positive and negative outcomes, OpenAPI, and F001/F002 regressions. Explicitly verify cancellation and platform change type, UpdateId, revision, source, and publication time across existing journey/update/station reads.
6. Run full solution build and tests locally, record SR-to-test evidence, and keep passenger alert delivery, SR-044 Incident Management/operations-client evidence, and other downstream UI/module evidence open until their owning work passes.

## Complexity Tracking

No constitution exception or additional project is proposed. Journey revision snapshots and immutable audit rows are necessary to provide the approved consistency, correction, and concurrency behavior.
