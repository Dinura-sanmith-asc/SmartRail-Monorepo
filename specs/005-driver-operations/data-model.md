# Data Model ? Driver Operations

## DriverJourneyAssignment
`AssignmentId` UUID PK; `DriverSubject` required opaque verified subject; `JourneyInstanceId` required FK to existing Journey; `AssignedAtUtc`, `AssignedBySubject`, `SourceReference` required provenance; nullable `RevokedAtUtc` and `RevokedBySubject` paired. Active means unrevoked. Required valid IDs, nonblank subjects/provenance, UTC timestamps, revoke not before grant; revoke once. Regrant creates new lifecycle. Partial unique active `(DriverSubject, JourneyInstanceId)`; journey and active-driver lookup indexes. No Controller assignment requirement.

## DriverAssignmentAudit
`AuditId` UUID PK and restrictive `AssignmentId` FK; immutable grant/revoke action, verified actor, UTC event time, evidence. Grant/revoke and audit persist atomically. No audit edit/delete use case. Database and persistence mapping reject invalid lifecycle values; application persistence rejects mutation of immutable evidence.

## DriverObservation
`ObservationId` UUID PK; restrictive `AssignmentId` and `JourneyInstanceId` FKs; `DriverSubject`, `Text`, `ReportedAtUtc`, `JourneyRevision`, `RequestId`. Identity, author, report timestamp and captured revision are server-supplied; text trims to 1?4000 characters; request UUID nonempty. Unique `(DriverSubject, RequestId)` and journey/report-time lookup index. Same subject/key/journey/canonical text replay returns existing immutable observation after current authorization; other payload conflicts. No update/delete lifecycle. Observations do not own or change status, schedules, platforms or signalling state.

## Ownership and consistency
Assignment's journey exists in Feature 003. Observation subject/journey must match original active assignment. Captured revision must belong to same journey. Assignment revocation does not erase observations but prevents submission and makes original-assignment evidence incompatible for new official publication linkage. Feature 003 owns revisions, route changes and official update history; Driver Operations consumes projections and does not duplicate these authoritative tables. Additive generated `DriverOperations` migration only; existing migration IDs/files unchanged. Tests use disposable port-5433 databases, never development/RDS.
