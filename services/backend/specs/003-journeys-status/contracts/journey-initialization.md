# Internal Journey Initialization Boundary

This document describes the non-HTTP initializer required by the approved Gate 1 decision. It is not a public API and does not add a route or RBAC grant.

## Application boundary

Proposed ports in `SmartRail.Application.JourneysStatus`:

- `IJourneyInitializationAuthority`: verifies trusted invocation and source authority. Production registration is `DenyAllJourneyInitializationAuthority` until the named operational authority and procedure are approved.
- `IJourneyScheduleLinkVerifier`: verifies the referenced Feature 001 service, exact published schedule revision, complete operation date, and first-call departure occurrence.
- `IJourneyInitializationRepository`: performs idempotent create/read and appends initialization/conflict audit evidence transactionally.
- `IJourneyInstanceIdIssuer`: accepts an authoritative opaque source ID when provided, otherwise issues one UUID once for the accepted source key and persists the mapping. Never derive it from service/date/occurrence.

The command is invoked only by an internal trusted host/CLI or controlled manifest import after a future task approves its operational surface. This plan does not choose an HTTP or interactive admin mechanism. No public caller can invoke it. An implementation must make test fixtures supply an isolated fake authorized identity without enabling any normal Development/Production bypass.

## Input evidence

Required validated values:

- Source system and stable source instance key.
- Authoritative journey instance ID, if the source has one.
- Feature 001 service ID, schedule ID/revision, operation date, and first-call scheduled departure occurrence.
- Manifest/source revision or equivalent provenance.
- Verified actor subject and trusted authority evidence.

The actor and accepted timestamp are server-derived. Reject missing IDs/keys, malformed provenance, unknown/unpublished schedule references, incomplete operation-date data, or an unresolvable departure occurrence. Do not infer a journey from a service schedule alone.

## Replay and conflict rules

| Case | Result |
|---|---|
| New source key, valid linkage, authorized actor | Issue/persist one journey ID, initial snapshot, and accepted audit evidence atomically. |
| Same source key and same authoritative journey ID/linkage | Idempotent success returning the existing journey; do not append a second journey. Provenance is retained as source evidence but does not change identity equality. An optional replay audit can be appended if approved retention policy requires it. |
| Same source key with different ID or linkage | Reject; do not alter journey; append a conflict audit in a separate transaction. |
| Same journey ID with a different source key/linkage | Reject and audit conflict; never merge or rebind. |
| Different authoritative IDs with identical schedule linkage | Preserve both as distinct journeys; do not place a uniqueness constraint on the linkage composite. Passenger selection must expose ambiguity where public descriptors cannot distinguish them. |
| Missing, revoked, or unverifiable authority/evidence | Deny/fail closed; no journey write. |
| Audit store unavailable after rejection | Return unavailable and log only redacted operational identifiers; do not claim the rejected conflict has durable audit evidence. |

## Transaction and audit invariants

- A successful initialization creates the root, a baseline revision without a public `UpdateId`, initial stop snapshot, and accepted internal audit in a single PostgreSQL transaction. The baseline and internal audit are excluded from `GET /api/journeys/{id}/updates`.
- Unique `(source_system, source_instance_key)` and the journey primary key provide race-safe duplicate protection. A concurrent identical replay resolves to the committed row; a concurrent conflicting write is rejected and audited.
- Conflict auditing occurs only after the candidate state transaction has been rolled back/rejected, in a separate append-only transaction. Thus rejected state does not partially persist while conflict evidence can be retained.
- Audit captures decision, source key/system, candidate or accepted journey identity, schedule linkage, provenance, actor when verified, and server timestamp. Never store JWTs, passwords, signing keys, or unbounded raw manifest payloads.
- Every delete/update of historical audit or revision rows is prohibited by application persistence guards and tested. Additional database-level enforcement/retention is an operational review item.

## Operational approval gate

Before production activation, the project owner must approve named authority/role, operator identity verification, source-system/key governance, manifest format and provenance, conflict handling, audit retention, and invocation controls. Until then the production adapter denies every initialization command. Local test fixtures may exercise the authorized path only in isolated test hosts/databases.
