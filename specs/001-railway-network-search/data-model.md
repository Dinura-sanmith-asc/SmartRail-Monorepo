# Data Model: Railway Network & Train Search

**Status**: Planning proposal for implementation review. It does not alter the approved ERD or authorize unresolved business policies.

## Design boundary

The supplied ERD is the starting point: `STATION`, `ROUTE`, `ROUTE_STOP`, `TRAIN_SERVICE` and `SERVICE_STOP`. It does not show an explicit schedule version, an authoritative service-date source, or a completeness marker. The proposal adds only what the clarified SR-013 search needs. It does not create a dated operational journey, journey status, cancellation, ETA, actual arrival/departure, platform, driver assignment or incident.

The date in `ServiceOperationDate` is the schedule's base operation date at its first calling point. Search derives the requested origin's departure date from its scheduled offset. This lets a service starting before midnight be found at a later station on the following date without treating its date row as a Journey.

## Entities and fields

| Entity / table | Proposed fields | Purpose and rules |
|---|---|---|
| **Station / `station`** | `station_id UUID PK`; `station_code TEXT NOT NULL`; `name TEXT NOT NULL`; `location_description TEXT NULL`; `essential_service_details TEXT[] NOT NULL`; `is_published BOOLEAN`; `created_at`, `updated_at`; `version INTEGER` | Stable reference identity and public detail projection. Canonical station code is the import upsert key. Keep public detail values source-backed. The exact fields and code normalization must be confirmed against the authoritative railway source. |
| **Route / `route`** | `route_id UUID PK`; `route_code TEXT NOT NULL`; `name TEXT NOT NULL`; `is_published BOOLEAN`; `version INTEGER` | Reusable named route. Only published routes appear in public reads or published schedules. |
| **RouteStop / `route_stop`** | `route_stop_id UUID PK`; `route_id UUID FK`; `station_id UUID FK`; `stop_order INTEGER NOT NULL` | Ordered route sequence; `stop_order > 0`; unique `(route_id, stop_order)`. The station FK prevents dangling stops. Repeat station visits remain an approval gate; no unique `(route_id, station_id)` constraint until that policy is approved. |
| **TrainService / `train_service`** | `service_id UUID PK`; `service_code TEXT NOT NULL`; `route_id UUID FK`; `is_published BOOLEAN`; `version INTEGER` | Reusable service identity and route reference. A published service must have an active published schedule revision and valid route. |
| **ServiceScheduleRevision / `service_schedule_revision`** | `schedule_id UUID PK`; `service_id UUID FK`; `revision INTEGER NOT NULL`; `first_departure_local_time TIME NOT NULL`; `time_zone_id TEXT NOT NULL`; `is_published BOOLEAN`; `created_at`, `published_at` | Immutable planned schedule revision. Revision is unique per service. First departure anchors all call-point offsets. The zone is explicit configuration/data; proposed value `Asia/Colombo` requires approval. |
| **ServiceCallingPoint / `service_calling_point`** | `schedule_id UUID FK`; `route_stop_id UUID FK`; `arrival_offset_minutes INTEGER NULL`; `departure_offset_minutes INTEGER NULL`; composite PK `(schedule_id, route_stop_id)` | A service calls only at linked route stops. Route-stop order establishes calling order; offsets from first departure support the selected origin/destination times and overnight date calculation. Offsets are non-negative; a required origin departure and destination arrival must be present. Arrival must not be after departure at a call. Cross-row order and schedule/route consistency are validated before publication. This is an internal search eligibility index; the service-detail endpoint does not expose the complete list as an SR-005 feature. |
| **ServiceDateManifest / `service_date_manifest`** | `operation_date DATE PK`; `source_revision TEXT NOT NULL`; `is_complete BOOLEAN NOT NULL`; `published_at TIMESTAMPTZ NULL`; `published_by TEXT NULL`; `content_hash TEXT NULL` | States whether the authoritative source is complete for a schedule operation date, including a complete date with zero operating services. Missing or incomplete manifest means eligibility is unavailable, not a proven no-service date. No age-based expiry is set. |
| **ServiceOperationDate / `service_operation_date`** | `schedule_id UUID FK`; `operation_date DATE FK`; composite PK `(schedule_id, operation_date)` | An exact published date on which a schedule operates. No weekday recurrence, holiday calendar, or automatic date generation. `operation_date` anchors the first calling point; origin travel date may be later due to its offset. |
| **StationImportRun / `station_import_run`** | `import_run_id UUID PK`; `source_revision TEXT`; `content_hash TEXT`; `submitted_by TEXT`; `approved_by TEXT`; `started_at TIMESTAMPTZ`; `completed_at TIMESTAMPTZ`; `created_count`, `updated_count`, `unchanged_count` | Auditable record of a controlled import and its human approval. Keep it separate from public station data. |
| **StationChange / `station_change`** | `station_change_id UUID PK`; `import_run_id UUID FK`; `station_id UUID FK`; `action TEXT`; `changed_fields JSONB`; `before_values JSONB NULL`; `after_values JSONB`; `changed_at TIMESTAMPTZ`; `changed_by TEXT` | Minimal station change evidence. Restrict JSON to approved station fields; do not store secrets or unrelated user data. Any broader history requirement needs a separate SR-012 scope decision. |

`TEXT[]` and JSONB are proposed PostgreSQL representations, not source-approved field formats. If the authoritative source identifies fixed service-detail fields, replace the free-form proposal with those verified fields before implementation.

## Relationships

- One `Route` has many ordered `RouteStop` rows; each stop references one `Station`.
- One `Route` has many `TrainService` rows; a service has one selected route.
- One `TrainService` has schedule revisions; only a published revision can support public choices.
- One `ServiceScheduleRevision` has many `ServiceCallingPoint` rows and exact `ServiceOperationDate` rows.
- One `ServiceDateManifest` covers an operation date; zero or more service/date rows reference that date.
- Each station import run can record many station changes. Station, route, service and schedule references use `ON DELETE RESTRICT`; retirement is a publication action, not cascading deletion.
- Future Journeys & Status may create a dated `Journey` from a published service/date and reference the schedule revision. That module owns journey identity, realized stops, operational status, revisions and updates. It must not duplicate or modify Railway Network's reference schedule.

## Keys, checks and referential integrity

| Constraint | Proposed rule |
|---|---|
| Primary keys | UUID identifiers for mutable reference entities; composite keys for `(schedule_id, route_stop_id)` and `(schedule_id, operation_date)`; one manifest row per operation date |
| Unique keys | Canonical station code; route code; service code; `(service_id, revision)`; `(route_id, stop_order)`; `(schedule_id, route_stop_id)`; `(schedule_id, operation_date)` |
| Foreign keys | RouteStop → Route and Station; Service → Route; ScheduleRevision → Service; ServiceCallingPoint → ScheduleRevision and RouteStop; ServiceOperationDate → ScheduleRevision and ServiceDateManifest; audit rows → import run and Station |
| Checks | Positive route order and schedule revision; non-negative call offsets; `arrival_offset_minutes <= departure_offset_minutes` when both are present; manifest completeness is explicit; import counts non-negative |
| Cross-row validations | Every published service has a valid published route and schedule; every published schedule has ordered calling points at valid route stops; call order follows route order; a complete manifest exists before date records are published; all required dates/times are available before a choice is returned |

Do not add uniqueness on `(route_id, station_id)` or `(schedule_id, station_id)` until repeat visits are decided. Until then, publication validation should flag ambiguous repeated calls and withhold the affected schedule rather than choosing a visit silently.

## Search indexes

- Unique indexes on canonical `station_code`, `route_code` and `service_code`.
- `(route_id, stop_order)` unique index on route stops; `(station_id, route_id)` supports finding routes serving a station.
- `(service_id, revision)` unique index on schedule revisions; if approved, a partial unique index allows only one active published revision per service.
- Unique `(schedule_id, route_stop_id)` index supports service-call membership. The FK route-stop index links the call to station and order.
- Primary key on service date manifest; index `(operation_date, schedule_id)` for operation-date lookup (or retain composite PK and add the date-first index).
- Query plan starts from origin and destination calling points, joins route-stop order and exact operation date, and filters only published current data. Add further indexes only after explaining and measuring the PostgreSQL query against acceptance data.

## EF Core mapping and migration approach

Map feature entities through explicit configuration classes under `SmartRail.Infrastructure/RailwayNetwork`. Use PostgreSQL `date` for operation dates, `time without time zone` for a local schedule anchor, `timestamptz` for audit instants, UUID IDs, explicit string lengths once the source owner approves them, and Npgsql-supported arrays/JSONB only for the proposed free-form fields. Keep station/service reference rules in Domain and duplicate/transaction/persisted uniqueness enforcement in PostgreSQL.

There is no EF Core context or existing migration. Implementation should add the EF/Npgsql package set, `SmartRailDbContext`, configurations, one reviewed initial Railway Network migration and migration tests. The date manifest and eligible service rows must publish transactionally. Station import validates the complete input first, then applies upserts and audit records in one database transaction; an invalid record rolls back the batch. Never run schema creation or destructive seeding automatically in production startup.

## Decisions requiring approval

| Open decision | Recommended handling pending approval |
|---|---|
| Authoritative source and completeness guarantee | Do not mark a date complete until its source is verified; no row without a complete manifest is a service result or a confirmed no-service date. |
| Schedule revisions and publication/retirement | Use immutable revisions and explicit publication/supersession; retain history and filter only current published records. Confirm effective-date semantics before adding them. |
| Time zone and local date basis | Use `Asia/Colombo` for this single-country dataset, with offset-bearing API timestamps, after owner confirmation. |
| Repeat station visits and duplicate calls | Withhold affected publication until the railway owner selects a deterministic rule; do not encode an irreversible uniqueness constraint yet. |
| Stale/old complete manifests | Treat only explicit incomplete/superseded/revoked source publication as stale until an owner sets a freshness threshold. |
| Public station details | Confirm whether the proposed location description and free-form essential service detail list match the authoritative source and privacy boundary. |
| Data-maintainer roles and audit | Approve the proposed Steward/Owner split, production approval, and whether field-level audit remains within Feature 001 or promotes SR-012. |

