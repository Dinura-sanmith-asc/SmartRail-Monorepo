# Research: Journeys & Status

**Feature**: [spec.md](spec.md) · **Plan**: [plan.md](plan.md) · **Date**: 2026-10-19

## Decisions and evidence

### Existing architecture and project fit

The repository already has the six constitution-owned projects: `SmartRail.Api`, `SmartRail.Application`, `SmartRail.Domain`, `SmartRail.Infrastructure`, `SmartRail.UnitTests`, and `SmartRail.IntegrationTests`. All target .NET 10. Infrastructure uses EF Core 10.0.12 and Npgsql EF Core 10.0.3; API uses ASP.NET Core OpenAPI 10.0.12. Feature 001 and 002 are in the shared context and migration chain, so Feature 003 should add configurations and an additive migration in the same context rather than create a project or context.

The current dependency direction is API → Application/Infrastructure, Infrastructure → Application/Domain, and Application → Domain. API already orders authentication before authorization. Existing Feature 001 reads are explicitly anonymous; they must remain unchanged. Feature 002's `RolePermissionCatalog` contains the applicable actions. Controller currently has explicit grants for `PublishOfficialJourneyStatus`, `ChangePlatformOrOperationalTiming`, and `ConfirmJourneyCancellation`; do not add or broaden grants. The handler must still require current resource-scope evidence, not just a role grant.

### Feature 001 schedule linkage

Feature 001 owns `Station`, `RouteStop`, `Service`, `ServiceScheduleRevision`, `ServiceCallingPoint`, and operation-date completeness. A dated journey references the exact published schedule revision and date/occurrence it was created from. It may copy scheduled stop values into an immutable journey revision snapshot but must never rewrite Feature 001 schedule rows. Stops and journey status are operational facts, distinct from planned schedule values.

The approved identity rule makes `(service, schedule revision, operation date, first-call departure occurrence)` a linkage/deduplication context, not a unique key. Use the opaque issued journey ID as the root identity. Do not create a unique database index on the approved composite because distinct authorized IDs may represent distinct real departures at that same planned occurrence.

### Journey instance issuance and idempotency

Use a restricted Application command behind `IJourneyInitializationAuthority`; register a deny-all authority by default. The command accepts an authoritative `sourceSystem` and stable `sourceInstanceKey`, journey ID from the source when available or a generated UUID for the controlled no-feed manifest, schedule linkage, operation date, first-call occurrence, actor evidence, and provenance. The initializer validates the exact published schedule/date/occurrence link and refuses missing or unverifiable values.

Persist a unique key on `(sourceSystem, sourceInstanceKey)`. Replaying the same key with identical journey identity and schedule linkage returns the existing instance without creating another. Reuse of a key or ID with conflicting linkage is rejected and creates a conflict audit record without altering the journey. Different source keys/IDs with the same composite linkage are retained as different journeys. Do not synthesize a journey from a schedule search.

Unauthorized commands perform no state write. A business-level conflict needs durable evidence per Gate 1: first roll back/reject any journey mutation, then append a separate conflict-audit record in a safe transaction. If the audit authority/storage is unavailable, fail unavailable and emit only redacted diagnostics. Do not claim that an audit event survives in a transaction that was rolled back.

### Lifecycle, official publication and corrections

Use the approved lifecycle in `spec.md`. Model each successful publication as a new monotonically increasing journey revision. Each publication contains the resulting status and a complete ordered stop snapshot, preserving the source/planned values separately from operational values. Append an immutable event with publication type, verified actor subject, server time, safe source, previous/new revision, changed fields, optional verified Driver observation reference, and optional `correctionOfUpdateId`.

An ordinary update cannot transition a terminal state back to active. A Controller correction must identify the prior event and append a new revision; do not update/delete historical rows. A diversion is a full replacement ordered stop set for that journey only. It preserves the Feature 001 route and schedule, and earlier journey stop snapshots remain readable through update history. Stable stop IDs are retained for unchanged stops; new diversion stops receive new server-generated IDs; removed stops remain in historical snapshots. This stable-ID policy is a technical persistence choice and should be verified against the approved stop endpoint's `stopId` semantics before task generation.

### Revision concurrency and read consistency

Use the business `JourneyRevision` as an application-managed EF Core concurrency token. A write transaction loads the expected current revision, checks transition and authorization conditions, appends event and full stop snapshot, then conditionally advances the journey's current revision. A competing writer based on the old revision fails the conditional update and transaction; map it to RFC 9457 `409 Conflict`. PostgreSQL unique constraints on `(journey_id, revision)` provide a second invariant. No process-local lock or read cache is authoritative.

Every journey detail, update-history, and station departure response reports its journey revision. Build the current journey and stop payload from a single SQL projection/query, or use a repeatable-read transaction when multiple queries are required. A response must not join a new journey revision with old stop data. A station departure response may contain several journeys, each with its own independently consistent revision.

These choices align with [EF Core optimistic concurrency](https://learn.microsoft.com/en-us/ef/core/saving/concurrency), [Npgsql concurrency-token support](https://www.npgsql.org/efcore/modeling/concurrency.html), and PostgreSQL's unique/index constraints. The plan uses the semantic revision field rather than a provider-specific hidden row version because the revision is already part of the approved contract and audit trail.

### Authorization and module ownership

For each protected status, cancellation, diversion, or stop update:

1. API validates the Cognito access JWT using Feature 002.
2. Application requests current mapped roles for the verified subject and the operation's exact approved action.
3. Feature 002 evaluates current resource scope for this journey.
4. If the update references `driverObservationId`, the Application adapter asks Driver Operations through its authorized read boundary and verifies that the observation exists and may be referenced. It does not copy or own the observation and does not let it update official status.
5. If any required evidence is missing, revoked, stale, or unavailable, deny or return unavailable before a state change.

Incident Management owns incident entities and lifecycle; Feature 003 may expose approved affected-journey context through an owner port only where required by the approved scenario. It must not derive cancellation or disruption status from incident state alone. No current owner module is reimplemented here.

### API/OpenAPI representation

The approved contract is OpenAPI 3.1.0, `info.version: 1.0.0`, with exactly the five paths in `contracts/journeys-status.openapi.yaml`. No `/v1` URL prefix is added. Reads are public projections; writes require bearer authentication and the existing Controller grant/scope rules. The contract excludes the private initialization command.

The approved Gate 2 establishes the schema semantics, but not every transport detail. This planning draft proposes `GET /api/stations/{id}/departures?date=YYYY-MM-DD`, where `date` is the station's requested operating date and result ordering follows published departure instant then journey ID as a stable tie-break. No feed freshness cutoff or arbitrary page size is introduced. `GET /api/journeys/{id}/updates` returns the complete immutable update history for the MVP. Before implementation, review whether operation-date versus station-local service-date semantics, response ordering, and history pagination need explicit business/API approval; do not silently add a date threshold, pagination cap, or selection rule.

Public projections omit raw Controller subjects, Cognito claims, and private Driver observation references. They include safe source labels, server publication time, revision, update ID, and approved public change details. A correction link may reference a public update ID. The optional observation ID is accepted only as a verified Controller evidence reference and is not returned publicly.

### Errors and failures

Use safe `application/problem+json` responses. Preserve Feature 002 outcomes for missing/invalid credentials (401), insufficient role/scope (403), and authorization evidence unavailable (503). For these operations, malformed request (400), unknown journey/stop/station (404), stale revision (409), and unavailable authoritative schedule/owner source (503) are separately represented. Never return SQL, stack traces, tokens, keys, raw identity claims, or connection strings. A valid journey with no updates is `200` plus an empty list; unavailable publication source is not represented as an empty result.

### Testing and local verification

Reuse xUnit and the real PostgreSQL fixture in `tests/SmartRail.IntegrationTests/RailwayNetwork/PostgreSqlTestDatabaseFixture.cs`; it accepts only a local test admin connection on `127.0.0.1:5433`, creates a unique database, and drops it after each fixture lifetime. Journey tests must never point to development port 5432 or any AWS/RDS target. Use existing isolated OIDC/JWKS and identity fixtures for API tests; do not add authentication bypasses.

Required test dimensions include identity collisions, replay/idempotency, conflicting keys, audit append-only behavior, additive migration/FKs/indexes, schedule linkage, status transitions and terminal correction, complete diversion snapshots and ordering, stop timing/platform changes, transaction rollback, concurrent stale writers, current role/scope revocation, optional observation verification, public-read privacy, 401/403/404/409/503 mapping, consistent journey/station reads, and regressions for Features 001 and 002. Map source acceptance checks in the future tasks and verification evidence; do not mark downstream client requirements complete from backend tests.

## Requirement-to-test evidence map for planning

| Requirement | Planned evidence owner | Core automated evidence |
|---|---|---|
| SR-007 | Feature 003 backend | Different journey IDs with same schedule composite persist/read independently; identity and schedule link returned. |
| SR-010 | Feature 003 + consuming clients | One committed revision/source/time is returned in journey and station projections; no unauthorized source. |
| SR-019 | Feature 003 backend + passenger client | Approved active/terminal status labels map from authoritative status; client rendering remains downstream. |
| SR-021, SR-035 | Feature 003 backend + passenger client | ETA absent/stale/unavailable is explicit and never synthesized; source and time retained. |
| SR-022 | Feature 003 backend + passenger client | Diversion/status/platform/time change appears for the selected journey; Feature 001 schedule unchanged. |
| SR-025, SR-048 | Feature 003 + configured channel/client | Change event and in-app notification/status are associated to same revision; no unapproved delivery threshold. |
| SR-031, SR-034, SR-040 | Feature 003 backend | Authorized manual update persists actor/time and latest revision; forbidden actors cannot mutate state. |
| SR-046 | Feature 003 backend | Only exact Controller cancellation grant succeeds; Driver/StationStaff denied and unchanged. |
| SR-054, SR-056, SR-058, SR-059 | Station-display client + Feature 003 backend | Departure/platform/status revision returned; client refresh, offline, stale and display behavior remains downstream. |
| SR-069 | Driver Operations + Feature 003 boundary | Driver observation remains owned/non-authoritative; verified ID may be referenced in Controller publication evidence. |
| SR-044 | Incident Management + Feature 003 boundary | Incident owner can associate an incident to journeys; display context is read-only and does not infer status. |
| SR-001, SR-038, SR-064–066, SR-102, SR-109, SR-114 | Owning clients/modules + release E2E | Cross-boundary operational access, text status, refresh/freshness, denial/error/no-network states; not closed by this backend plan alone. |

## Unresolved decisions and approvals

### Operational approvals before production activation

- Named role/authority and operating procedure for journey initialization and manifest issuance; production command remains deny-all until approved.
- Trusted source-system and source-instance-key governance, ID ownership, conflict escalation, and audit retention/approver policy.
- Environment-specific production authorization/scope mapping and permitted deployment settings remain governed by Features 001/002 and deployment approvals.

### Contract details to settle before implementation

- Confirm the proposed station departures `date` meaning and whether it is station-local calendar date or journey operation date for overnight services; no threshold is inferred.
- Confirm update-history pagination/retention strategy if the full immutable history can exceed client needs; the proposed MVP contract returns all updates and adds no arbitrary limit.
- Confirm the stable `stopId` behavior on diversion (reuse unchanged calls, generate new IDs for added calls, retain removed calls in prior revisions).
- Confirm safe public change-source vocabulary and whether a correction's prior update ID is public; raw actor subject and Driver observation ID remain private regardless.
- Confirm how a referenced Driver observation that is missing or revoked maps to 400/404/409, while unavailable Driver Operations evidence maps to 503; no publication may succeed without required verification.

These are design review items, not authorization to add API operations, RBAC grants, business thresholds, or client scope. Resolve them before task generation if they change externally observable behavior.
