# Data Model: Incident Management

This is a planning model for an additive EF Core migration in the shared `SmartRailDbContext`. Exact SQL types and indexes are finalized in implementation tasks without changing the invariants below. Feature 001/003 authoritative data is referenced by identifiers and owner verification, not copied into Incident Management.

## Entities

### Incident

| Field | Meaning / invariant |
|---|---|
| `IncidentId` | Opaque server-generated UUID primary key. |
| `CategoryCode` | Required code resolved through the approved controlled category catalogue. No enum/code list is invented in this plan. |
| `Description` | Required report content; transport and safe-display validation apply. Numeric limits are not invented. |
| `State` | `Reported`, `In Progress`, `Resolved`, or `Closed`; only approved forward transitions. |
| `Revision` | Positive optimistic concurrency revision; starts at 1 and increases once for each accepted PATCH mutation. |
| `ReporterSubject` | Verified opaque Cognito subject from Feature 002; private and never returned in public API projections or logged. |
| `ReportedAt` | Server-generated UTC report timestamp; immutable. |
| `OccurredAt` | Optional reporter-supplied occurrence time, stored separately from `ReportedAt`. |
| `CreatedAt` / `UpdatedAt` | Server-managed persistence timestamps where needed for ordering/diagnostics; not substitutes for requirement timestamps. |

### IncidentRevision

Immutable snapshot/evidence for each accepted incident revision. Keyed by `(IncidentId, Revision)` and written in the same transaction as the aggregate mutation. Includes current state, verified actor subject, server UTC timestamp, a safe server-owned source classification (`IncidentManagement`), and the association-set delta or snapshot needed to reproduce that revision. For a lifecycle transition it also includes previous and new state, which supplies the required SR-042 evidence. A non-lifecycle association-only revision has no transition pair. Updates/deletes are rejected. This is the single Feature 004 revision/evidence history, not a competing lifecycle table or full SR-051 chronology. Feature 003 remains authoritative for journey publication history. The actor subject and raw internal evidence are not returned by the API.

### IncidentAssociation

One row per independently identified link, with `AssociationId`, `IncidentId`, resource kind (`Station`, `Service`, `Journey`, `Disruption`), owner resource ID, optional disruption `UpdateId`/revision reference, `AddedAt`, `AddedRevision`, and nullable `RemovedAt`/`RemovedRevision`. Active links are those without a removal revision. Duplicate active `(IncidentId, kind, resource identity)` is prevented by a unique/partial unique index. Re-adding after removal creates a new association row, preserving history.

- Station/service IDs refer to Feature 001-owned resources and are verified by its owner interface.
- Journey ID refers to a Feature 003 `JourneyInstanceId`; new links must verify active state at association time.
- A disruption link identifies the specific Feature 003 published update using its stable `UpdateId` and revision. It is not a copied disruption record or status.
- Later journey cancellation/completion or disruption supersession does not mutate or remove the association. Explicit PATCH removal ends its active interval.
- All links remain historical; current read projection includes only active links, with historical links not exposed unless a separately approved contract allows it.

### IncidentIdempotencyRecord

Stores verified actor subject, operation (`POST /api/incidents` or `PATCH /api/incidents/{id}`), idempotency key, canonical request hash, resulting HTTP outcome/response projection, resource ID and server timestamp. Unique key `(ActorSubject, Operation, IdempotencyKey)`. Do not store bearer tokens or passwords. Identical payload replay returns the stored result; differing payload under the same scoped key conflicts. Record retention/expiry is unspecified and must not be invented.

## Relationships and constraints

- `Incident` 1-to-many `IncidentRevision` and `IncidentAssociation`; restrictive deletion behavior preserves revision evidence and references.
- `Incident` primary key is opaque; all associations and audit rows have explicit foreign keys.
- `Revision >= 1`; incident category and state are required; reporter subject and report time are required.
- `IncidentRevision` is append-only; a lifecycle transition pair must match the approved graph, actor and time are required, and revision is unique per incident.
- `IncidentAssociation` resource kind and owner ID are required; disruption links require the approved stable update reference. No duplicate active association.
- `IncidentIdempotencyRecord` has a unique scoped idempotency key and non-null payload hash/outcome.
- No cascade delete of incidents, evidence or associations. Correctness uses transaction boundaries and EF concurrency tokens/conditional updates for expected revision.
- Use restrictive FKs to local Feature 001 resources where those entities share the database. Feature 003 links are verified through its owner contract; do not impose a duplicate/competing status FK/history table. If a local FK can target a stable Feature 003 key without constraining module ownership, document and verify the exact mapping before migration.

## Mutation transactions

**POST**: validate identity, grant/scope, category and every supplied owner reference before commit. In one transaction insert Incident revision 1, initial active associations, and the idempotency result. A failed check or write leaves no incident or link.

**PATCH**: load incident and association set; authorize against current role evidence and all current station links; verify any new references; compare `ExpectedRevision`. Apply allowed forward transition and/or add/remove deltas, increment revision once, append the immutable `IncidentRevision` (including transition evidence when state changes), and store the idempotency result in one transaction. Stale revisions and conflicting retries perform no mutation. Removed associations retain their immutable historical interval.

## Read projections

The API projection includes only incident ID, category code, description, current state, revision, safe source classification (`IncidentManagement`), server report time, optional occurrence time, and currently active permitted associations. It excludes reporter subject, controller/actor subjects, private evidence, credentials, raw claims, and full internal audit chronology. The public Feature 003 JourneySnapshot is unchanged and includes no private incident record.
