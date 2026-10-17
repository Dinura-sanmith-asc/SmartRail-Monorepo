# Data Model: SmartRail Identity & Permissions

**Feature**: [spec.md](spec.md)
**Plan**: [plan.md](plan.md)
**Status**: Proposed for review; no migration or application code is created by this document.

## Ownership and persistence boundary

Identity & Permissions persists only SmartRail role assignments, their audit events, and station-staff-to-station scope assignments. Cognito remains the credential and authenticated-subject authority. The verified Cognito `sub` is stored as opaque text; it is not parsed as a UUID. Driver-to-journey assignment, incident relationships, and passenger order ownership remain owned by Driver Operations, Incident Management, and Catering Adapter respectively. Those modules provide current evidence through Application interfaces; Identity does not copy their records.

The entities are added to the existing `SmartRailDbContext` and PostgreSQL 18 / EF Core 10 migration pipeline. This is an additive migration after Feature 001. Existing Railway Network tables and the Feature 001 migration remain unchanged. Domain entities contain no EF Core, ASP.NET Core, Cognito SDK, or AWS references.

## Entities

### RoleAssignment

Represents one grant lifecycle for a Cognito subject and one recognized SmartRail role. Revocation closes that lifecycle; a later grant creates a new row so audit history remains append-only.

| Property | Type | Required | Meaning / invariant |
|---|---|---:|---|
| `Id` | UUID | Yes | Stable primary key. |
| `CognitoSubject` | text | Yes | Verified Cognito `sub`, opaque and case-sensitive as supplied by the authority; never a token or client-supplied identity. |
| `RoleCode` | constrained text | Yes | One recognized role: `Passenger`, `Driver`, `StationStaff`, or `Controller`. `Public` is anonymous access, not an assignment. |
| `GrantedAtUtc` | timestamptz | Yes | UTC grant instant. |
| `GrantedBySubject` | text | Yes | Actor subject from the controlled provisioning process; no credentials. |
| `RevokedAtUtc` | timestamptz | No | Null while active; otherwise UTC revocation instant. |
| `RevokedBySubject` | text | No | Required when revoked; null while active. |

**Invariants and constraints**

- `CognitoSubject` and `RoleCode` are non-empty; role values are constrained to the recognized role set.
- Active means `RevokedAtUtc IS NULL AND RevokedBySubject IS NULL`; revoked means both revocation fields are present. Revocation time cannot precede grant time.
- A partial unique index enforces at most one active `(CognitoSubject, RoleCode)` assignment. Re-grant after revocation inserts a new lifecycle row.
- Index by `(CognitoSubject)` with active predicate for request-time role lookup.
- Assignment changes are performed only through a controlled non-public process. No assignment mutation endpoint is defined.

### RoleAssignmentAudit

An append-only record of each role grant or revocation, committed in the same database transaction as the corresponding assignment change.

| Property | Type | Required | Meaning / invariant |
|---|---|---:|---|
| `Id` | UUID | Yes | Stable primary key. |
| `RoleAssignmentId` | UUID | Yes | Foreign key to the lifecycle row. Restrict delete. |
| `Action` | constrained text | Yes | `Granted` or `Revoked`. |
| `ActorSubject` | text | Yes | Verified actor identity recorded by the controlled process. |
| `OccurredAtUtc` | timestamptz | Yes | UTC event instant. |
| `EvidenceReference` | text | No | Optional controlled ticket/change reference; not arbitrary secrets or user profile data. |

**Invariants and constraints**

- Audit rows cannot be updated or deleted through application behavior; persistence tests verify append-only behavior.
- Grant event is inserted with a new assignment; revoke event is inserted with the revocation update, atomically.
- No access token, password, secret, raw JWT claim set, or unnecessary personal data is persisted.
- Retention duration and production audit access policy remain explicit operational approvals; this model does not invent them.
- Index by `(RoleAssignmentId, OccurredAtUtc)` for audit history.

### StationScopeAssignment

Represents the Identity-owned association between a subject with station-staff duties and an existing Railway Network station.

| Property | Type | Required | Meaning / invariant |
|---|---|---:|---|
| `Id` | UUID | Yes | Stable primary key. |
| `CognitoSubject` | text | Yes | Verified subject to which this scope is assigned. |
| `StationId` | existing Station key | Yes | Foreign key to the Railway Network station; restrict delete. |
| `GrantedAtUtc` | timestamptz | Yes | UTC grant instant. |
| `GrantedBySubject` | text | Yes | Actor from controlled provisioning process. |
| `RevokedAtUtc` | timestamptz | No | Null while active. |
| `RevokedBySubject` | text | No | Null while active; required when revoked. |

**Invariants and constraints**

- A partial unique index enforces at most one active `(CognitoSubject, StationId)` association.
- Reassignment after revocation creates a new lifecycle row.
- Lookup must use current active state on each protected authorization decision; no cached scope grants.
- This data establishes only station scope. It does not grant an action by itself; the approved RBAC matrix must also grant the action.
- Production assignment/revocation authority and audit procedure remain gated; no public scope-management API is approved.

## Relationships

```text
Cognito (credential authority)
  └── verified subject (`sub`, opaque text)
       ├── 0..* RoleAssignment ── 1..* RoleAssignmentAudit
       └── 0..* StationScopeAssignment ── 1 Railway Network Station

Driver Operations ── current driver-to-journey evidence (owner module)
Incident Management ── incident-to-station/journey evidence (owner module)
Catering Adapter ── passenger-to-order ownership evidence (owner module)
```

The three future-module evidence sources are not foreign-keyed into Identity tables. Each owning module answers its own current-scope query. Missing, revoked, stale, or unverifiable evidence results in no authorization grant; an unavailable authority is represented as `Unavailable` to the caller policy rather than as an empty verified result.

## Persistence and transaction rules

- Read current active role mapping from PostgreSQL for each protected authorization evaluation. Do not trust JWT group claims and do not cache results.
- A role grant/revoke updates the role row and inserts its audit event in one transaction. Rollback leaves both assignment and audit unchanged.
- Station scope grant/revoke is persisted with the same actor/time evidence and current-state constraints. Audit requirements for scope operations must follow the production approval before they are enabled; do not imply a retention period.
- Use restrictive foreign keys, required-field/check constraints, and partial unique indexes. PostgreSQL integration tests must exercise the actual migration and constraints.
- For API identity projection, if mapping lookup succeeds with zero active roles, return an empty role list. If lookup is unavailable, do not report an empty list as verified state; return safe service-unavailable behavior.

## Decisions intentionally left open

1. Production operators, bootstrap path, separation of duties, and approval process for role mapping changes.
2. Production operators and review/revocation process for station scopes.
3. Audit retention and access-control duration for role and scope evidence.
4. Environment-specific Cognito issuer, allowed human client IDs, optional resource audience, and token expiry configuration.

These gates do not prevent deterministic isolated test fixtures. They prevent enabling a production provisioning path without approval.
