# Data Model: Journeys & Status

This is a planning model for the approved Feature 003 behavior. It extends the existing shared `SmartRailDbContext`; it does not replace Feature 001 schedule data or Feature 002 identity data.

## Source-of-truth boundaries

| Information | Owner | Feature 003 relationship |
|---|---|---|
| Stations, reusable routes, train services, published schedule revisions, calling points, operation-date completeness | Railway Network (Feature 001) | Required references; immutable schedule linkage and baseline stop values are copied as needed for historical journey snapshots. |
| Dated journey identity, current official state, journey-specific stop/platform/timing changes, publication revisions and audit evidence | Journeys & Status (Feature 003) | Authoritative for official dated operational information. |
| Cognito credentials and token validation | Cognito / Feature 002 API integration | Only verified subject is passed into Application; tokens/claims are not persisted as role evidence. |
| Active SmartRail roles and station-scope assignments | Identity & Permissions (Feature 002) | Queried on each protected authorization check; not duplicated. |
| Driver assignments and observations | Driver Operations | Accessed through owner interfaces; observation is optional publication evidence, never official state by itself. |
| Incident record and lifecycle | Incident Management | Associated/read through owner boundary where approved; not owned or inferred here. |

## Entity relationship overview

```text
Feature 001 Service ──< ScheduleRevision ──< ServiceOperationDate
                              │
                              └──< ServiceCallingPoint >── RouteStop >── Station
                                      (planned baseline)

JourneyInitializationSource 1──< Journey
Journey 1──< JourneyRevision 1──< JourneyRevisionStop
   │                 └──< JourneyUpdateEvidence
   └── current_revision (optimistic concurrency token)
```

## Proposed entities

### Journey

Aggregate/root for one dated operational instance.

| Field | Type/constraint | Purpose |
|---|---|---|
| `JourneyInstanceId` | UUID PK, never database-generated | Opaque authoritative external identity or generated identity from the approved controlled initializer. |
| `ServiceId` | UUID, required FK to Feature 001 | Reusable train service. |
| `ScheduleId` | UUID, required FK | Exact published Feature 001 schedule revision identity. |
| `ScheduleRevision` | integer, required | Revision value copied for explicit contract linkage; enforce matching referenced revision. |
| `OperationDate` | PostgreSQL `date`, required | Journey's approved operating-date anchor; not changed by later stop times crossing midnight. |
| `FirstDepartureOccurrence` | `time without time zone` plus approved occurrence discriminator if available | Schedule-linkage value for the first calling-point departure. It is not unique across journeys. See unresolved occurrence contract question. |
| `SourceSystem` | bounded string, required | Issuing manifest/feed namespace. |
| `SourceInstanceKey` | bounded string, required | Stable idempotency key within source system. Unique with `SourceSystem`. |
| `InitializedAt` | `timestamptz`, required | Server-recorded accepted initialization instant. |
| `InitializedBySubject` | opaque string, required for authorized command | Verified actor subject; never returned in public reads. |
| `InitializationProvenance` | structured JSON or normalized audit reference, required | Source revision/manifest evidence; avoid storing credentials or raw tokens. |
| `CurrentRevision` | positive integer, required, EF concurrency token | Revision currently exposed by read projections and used by conditional writes. |

No unique constraint is placed on service/schedule/date/occurrence. Distinct authoritative IDs must remain distinct even when every linkage field matches. A unique index on `(SourceSystem, SourceInstanceKey)` prevents duplicate source ingestion. Add a unique index on `JourneyInstanceId` by PK only; conflicting linkage is handled explicitly and audited.

### JourneyRevision

Immutable snapshot of official state after one successful publication.

| Field | Type/constraint | Purpose |
|---|---|---|
| `JourneyInstanceId`, `Revision` | composite PK; restrictive FK to Journey | Stable ordered history for one journey. |
| `Status` | recognized domain value | Resulting official status after the event. |
| `PublicationKind` | recognized value | Status, stop change, route diversion, correction, initialization/snapshot. |
| `PreviousRevision` | nullable positive integer | Revision from which this publication was made. |
| `PublishedAt` | server-generated `timestamptz`, required | Authoritative publication instant. |
| `PublishedBySubject` | opaque verified subject, required for Controller publication | Private audit actor; not in public response. |
| `Source` | safe bounded classification | Manual Controller publication or another approved source category; no claim of a feed not used. |
| `ChangeEvidence` | structured, validated JSON or normalized values | Changed fields and evidence needed to reconstruct the publication. Exclude credentials. |
| `DriverObservationId` | nullable opaque owner ID | Optional verified Driver Operations observation reference. Feature 003 does not own referenced observation. |
| `CorrectionOfUpdateId` | nullable UUID | Link to prior immutable update when this publication corrects it. |

The pair `(JourneyInstanceId, Revision)` is unique. Append-only behavior is enforced in application persistence and a DbContext audit-mutation guard; database permissions/trigger policy may provide additional protection if operationally approved. A normal update appends; it does not update prior revisions.

### JourneyRevisionStop

Full ordered calling-point snapshot for a specific journey revision.

| Field | Type/constraint | Purpose |
|---|---|---|
| `JourneyInstanceId`, `Revision`, `JourneyStopId` | composite PK; FK to JourneyRevision | Stop identity within a historical revision. |
| `StopOrder` | positive integer | Order in this journey revision; unique within revision. |
| `StationId` | UUID, restrictive FK to Feature 001 Station | Station reference for this stop. |
| `RouteStopId` | nullable UUID, restrictive FK where original planned stop exists | Planned route-call link; diverted/new stops may not exist in reusable route. |
| `PlannedArrivalAt`, `PlannedDepartureAt` | nullable `timestamptz` | Values derived from the linked published schedule; immutable baseline. |
| `OperationalArrivalAt`, `OperationalDepartureAt` | nullable `timestamptz` | Explicitly published operational values. Missing values remain null, not inferred. |
| `Platform` | nullable bounded text | Official platform information when available. |
| `TimingSource`, `TimingUpdatedAt` | nullable safe source and `timestamptz` | Freshness/evidence for operational timing. |
| `AmbiguityState` | recognized value | Allows passenger departure projection to flag identical public descriptors; never silently auto-select. |

The full set of stops for a revision is inserted atomically. Unique `(JourneyInstanceId, Revision, StopOrder)` enforces order uniqueness. For diversion, retained stops keep stable `JourneyStopId`; newly added stops receive new IDs; prior revisions retain removed stops. Verify stop-ID semantics before implementation as identified in `research.md`.

### JourneyInitializationAudit

Append-only record of initialization decisions and conflicts.

| Field | Type/constraint | Purpose |
|---|---|---|
| `JourneyInitializationAuditId` | UUID PK | Stable audit event. |
| `SourceSystem`, `SourceInstanceKey` | required | Source identity involved. |
| `JourneyInstanceId` | nullable UUID | Accepted or conflicting target where known. |
| `ActorSubject` | nullable opaque subject | Verified maintainer for authorized action; nullable for rejected unauthenticated input only if policy permits recording it without identity claims. |
| `Decision` | recognized: accepted, idempotent replay, rejected conflict, rejected authorization, rejected validation | Evidence of decision outcome. |
| `ScheduleId`, `ScheduleRevision`, `OperationDate`, `FirstDepartureOccurrence` | nullable structured linkage | The accepted/candidate linkage; do not store unvalidated secrets or arbitrary payload. |
| `Provenance`, `OccurredAt` | structured source evidence and server `timestamptz` | Explains source and decision time. |

Audit writes are append-only. For conflicting identity, no Journey state is written; the conflict audit is appended in a separate transaction after rejection. Restrictive FKs are used only for accepted known journey/schedule relationships so rejected evidence can be recorded safely.

### JourneyUpdateEvidence (optional normalized table)

If change evidence cannot be represented safely and queryably in `JourneyRevision`, use one append-only event table with UUID `UpdateId`, journey/revision FK, source, verified actor, server time, optional observation ID, correction reference, and validated change payload. Do not duplicate the same authoritative history in two competing tables. Finalize one persistence representation during implementation task design.

## Lifecycle and revision invariants

- Only the approved transition graph in `spec.md` is valid. Terminal states are closed to normal updates; a correction is a new linked revision.
- `Cancelled` means not commenced; `Terminated` means ended before planned final stop; `Completed` means planned end reached.
- Each successful publication uses `expectedJourneyRevision`; if it is not the current value, reject with 409 and persist no partial state.
- Journey current revision, revision event, and complete revision-stop set are committed in one PostgreSQL transaction.
- No two revisions share the same `(JourneyInstanceId, Revision)`; revision numbers advance by one under the aggregate concurrency protocol.
- A Driver observation reference is verified through Driver Operations before commit; it never supplies official status automatically.
- The initialized schedule link must match an existing Feature 001 service, published schedule revision, operation-date completeness entry, and first-call occurrence. Missing or superseded source data is not treated as a successful empty journey.
- Public projection contains only published journey/station details, source class, revision and timestamps; raw actor subject and observation reference remain private.

## Indexes and constraints

- PK `journey_instance_id`.
- Unique `(source_system, source_instance_key)` for initialization idempotency.
- Index `(service_id, operation_date)` and `(schedule_id, operation_date)` for journey lookup; **not unique**.
- Unique `(journey_instance_id, revision)` on revision.
- Unique `(journey_instance_id, revision, stop_order)` and PK/FK on revision stops.
- Index `(station_id, operation_date)` through the current revision-stop projection/read path for station departures; verify with `EXPLAIN` before adding denormalization.
- Index `(journey_instance_id, published_at DESC)` for update history.
- FKs to Feature 001 schedule/service/date/station records and internal revision rows use restrictive delete behavior. Historical journey/audit data must not disappear when a reusable schedule is retired.
- Checks: positive revisions and stop order, required source/id fields, nonempty IDs, valid lifecycle values, publication time present, and valid expected/revision linkage.
- No uniqueness on the composite service/schedule/date/occurrence; doing so contradicts Gate 1.

## Migration approach

Add Feature 003 tables/configurations to the existing `SmartRailDbContext` and create an additive migration named `JourneysStatus` using EF-generated migration ID. Preserve both Feature 001/002 migration files and the shared snapshot history. Test migration `Up`/schema constraints on isolated PostgreSQL port 5433. Do not apply migration to development database as part of planning, and never target AWS/RDS. No code, migration, or database change is made by this document.
