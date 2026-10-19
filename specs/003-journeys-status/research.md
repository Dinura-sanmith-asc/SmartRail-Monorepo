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

An ordinary update cannot transition a terminal state back to active. A Controller correction must identify the prior update and append a new revision; do not update/delete historical rows. Each official operational publication receives exactly one immutable `UpdateId` stored with its `JourneyRevision`. A correction reference must resolve to an earlier revision's `UpdateId` for the same journey. Do not maintain a separate authoritative publication-history table. The initialization baseline may have a revision without an `UpdateId`; internal initialization audit is stored separately and is never in public update history.

A diversion is a full replacement ordered stop set for that journey only. It preserves the Feature 001 route and schedule, and earlier journey stop snapshots remain readable. Retained stops keep `journeyStopId`; new stops receive server-generated IDs; removed stops remain in old snapshots. Each supplied ID must belong to the current journey revision, appear at most once, and identify an existing stop. The server never infers retained identity from station/order; omitted IDs mean new stops. Duplicate, invalid, or foreign IDs are rejected with 400 and the transaction makes no state change.

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

The approved follow-up contract decision defines `GET /api/stations/{id}/departures?date=YYYY-MM-DD` as a station-local scheduled departure calendar date in `Asia/Colombo`; order results by the effective known departure instant, then `journeyInstanceId` as a deterministic tie-break. Preserve journey `operationDate` separately. For an overnight journey, each stop is filtered by the local calendar date of its scheduled departure, even when this differs from operation date. For a diversion-only station stop with no scheduled departure, use a verified published operational departure time to determine local date; if neither time is available, do not assign it to a date result or invent a time. No arbitrary page size or freshness cutoff is introduced.

`GET /api/journeys/{id}/updates` returns the complete public operational update history in ascending journey revision order, without arbitrary pagination limits. Include only official operational publications with an `UpdateId`; exclude the initial journey snapshot and internal initialization audit events. A journey with no official operational updates returns an empty array. Use safe public source labels `ControllerManual` and `ApprovedRailwayFeed`. Each update has an explicit closed `publicChange` schema; no unrestricted change payload or private evidence is projected. Public correction links reference an earlier update from the same journey.

Public projections omit raw Controller subjects, Cognito claims, private audit evidence, and Driver observation IDs. They include only approved source classification, server publication time, revision, update ID, same-journey correction reference, and explicit validated public change fields. The optional observation ID is accepted only after successful owner verification and is never returned publicly.

### Errors and failures

Use safe `application/problem+json` responses. Preserve Feature 002 outcomes for missing/invalid credentials (401) and insufficient role/scope (403). For a supplied Driver observation reference: malformed format or stop/reference payload is 400; missing or inaccessible observation is 404 without cross-resource disclosure; stale, revoked, superseded, or journey-incompatible evidence is 409; unavailable owner verification is 503. A publication that requires observation evidence cannot proceed unless the owner verifies access and association with the target current journey. Other invalid requests are 400; unknown journey/stop/station is 404; stale `expectedJourneyRevision` is 409; unavailable authorization, schedule, or owner evidence is 503. Never return SQL, stack traces, tokens, keys, raw identity claims, or connection strings. A valid journey with no public operational updates is `200` plus an empty array; internal initialization audit is not a public update. No failed required verification can produce a publication.

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
| FR-020 / SC-015 | Feature 003 backend | Station-local calendar date, overnight operationDate preservation, diverted-stop date basis, and missing departure-time behavior. |
| FR-021 / SC-016 | Feature 003 backend | Ascending complete public operational history; initial snapshot/internal initialization audit excluded; one same-journey UpdateId correction chain. |
| FR-022 / SC-017 | Feature 003 backend | Retained/new/removed stop identity behavior and atomic rejection of duplicate, invalid, or foreign IDs. |
| FR-023 / SC-018 | Feature 003 backend + Driver Operations boundary | Observation ownership and target-journey association with approved error mapping and no-write denial cases. |
| SR-044 | Incident Management + Feature 003 boundary | Incident owner can associate an incident to journeys; display context is read-only and does not infer status. |
| SR-001, SR-038, SR-064–066, SR-102, SR-109, SR-114 | Owning clients/modules + release E2E | Cross-boundary operational access, text status, refresh/freshness, denial/error/no-network states; not closed by this backend plan alone. |

## Unresolved decisions and approvals

### Operational approvals before production activation

- Named role/authority and operating procedure for journey initialization and manifest issuance; production command remains deny-all until approved.
- Trusted source-system and source-instance-key governance, ID ownership, conflict escalation, and audit retention/approver policy.
- Environment-specific production authorization/scope mapping and permitted deployment settings remain governed by Features 001/002 and deployment approvals.

### Resolved contract decisions

The following six details were explicitly approved on 2026-10-19 and are operative across the spec, contract, data model, and tests: station-local departure-date filtering and overnight/diversion behavior; complete ascending public operational history excluding initialization; stable stop identity and invalid-reference rejection; closed public source/change metadata; Driver observation ownership/error semantics; and one immutable `UpdateId` on each official publication revision. No contract-detail approval remains outstanding. Production initialization authority, source-key governance, and audit retention remain operational approval gates documented in [journey-initialization.md](contracts/journey-initialization.md).
