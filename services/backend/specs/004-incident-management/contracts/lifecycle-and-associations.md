# Lifecycle, Revision, Idempotency, and Association Contract

## Lifecycle

The only normal transitions are:

```text
Reported → In Progress → Resolved → Closed
```

`Resolved` means operational handling is complete. `Closed` means administratively concluded and is terminal. No reverse transition, reopening, dedicated correction workflow, severity workflow, or inferred status is in scope. SR-047 correction and SR-051 full chronology remain deferred.

Every accepted state change appends immutable evidence in the same transaction: incident ID, previous state, new state, verified actor subject, server timestamp, and resulting revision. A transition request must present the current expected incident revision. Stale requests return 409 and leave incident, associations, evidence and idempotency outcome unchanged apart from preserving an already committed replay record when applicable.

## Revision and idempotency

- Incident revision begins at 1. A successful PATCH mutation increments it exactly once, whether it changes lifecycle, associations, or both.
- POST and PATCH require a stable `Idempotency-Key`. Scope uniqueness to verified actor subject and operation; PATCH additionally resolves the resource in its canonical request hash.
- Store a canonical request hash and the original committed outcome. A replay with the same key and same request returns that original result. Same key with different request content returns 409.
- Concurrent key insertion is resolved by the unique constraint and a read of the committed record; never execute the same mutation twice.
- Do not invent key syntax length, expiry or cleanup policy. Apply transport safety validation while leaving business limits unspecified pending approval.

## Associations

Incident Management owns stable, individually identified links to Feature 001 stations/services and Feature 003 active dated journeys/specific published disruptions. POST can create initial links. PATCH can atomically add/remove links with `expectedRevision`. Validate new references against their owner before mutation. Reject malformed, duplicate, foreign or inactive-at-link-time references; unknown/inaccessible is 404, stale/incompatible evidence is 409, and unavailable verification is 503.

Each removal closes the active link at the resulting incident revision and retains its historical row. Re-adding later creates a new link identity. Existing links are not removed when an owner journey later becomes cancelled/completed or a disruption is superseded. Reads expose active authorized links only; internal history stays private unless an approved contract changes.

## Route and ownership invariants

- Exactly three operations: `GET /api/incidents`, `POST /api/incidents`, `PATCH /api/incidents/{id}`.
- No dedicated incident correction/reopen operation, GET-by-ID route, association route, or journey publication route.
- Incident changes never call Feature 003 status/publication behavior.
- `GET /api/operations/overview` remains Operations-owned and may reflect closure in its next authorized read; Feature 004 does not implement its client/dashboard behavior.
