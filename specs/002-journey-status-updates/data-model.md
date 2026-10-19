# Feature 002 Data Model: Journey Status and Updates

**Source of truth**: The implemented and approved Journeys & Status OpenAPI at `SmartRail/specs/003-journeys-status/contracts/journeys-status.openapi.yaml`. Request/response DTO parsing belongs to the Feature 002 data layer. Domain types below are passenger-facing and immutable; fields stay within public projections.

## Identifiers and date values

| Value | Contract type | Meaning and rule |
|---|---|---|
| `journeyInstanceId` | UUID string | Opaque identity of one dated operational journey. Use only for `/api/journeys/{id}` and `/updates`; never substitute `serviceId`. |
| `serviceId` | UUID string | Reusable scheduled service identity from Feature 001. Used only to associate station-departure candidates with the selected service. |
| `scheduleId` | UUID string | Referenced published schedule identity. |
| `updateId` | UUID string | Immutable identity for one public operational update and the session alert deduplication key. |
| `journeyStopId` | UUID string | Stop identity in a journey snapshot or update change. |
| `stationId` | UUID string | Station identity from Railway Network. |
| `scheduleRevision`, `revision`, `stopOrder` | integer | Backend-supplied positive revision/order values; do not recalculate or reorder. |
| `operationDate` | date (`YYYY-MM-DD`) | Journey's original railway operation date. Keep separate from station-local departure date. |
| `stationLocalDepartureDate` | date (`YYYY-MM-DD`) | Date used to match a station call in Asia/Colombo, supplied by the departure projection. |
| `firstDepartureOccurrence` | time | Contract time for first schedule departure occurrence; keep source value. |
| source timestamps | offset-bearing `date-time` strings | Preserve original raw representation and offset; do not convert to device timezone. |

## API DTOs

### `StationDepartureDto`

Required fields: `journeyInstanceId`, `serviceId`, `operationDate`, `stationLocalDepartureDate`, `revision`, `status`, `stationId`, `stopOrder`, `scheduledDepartureAt` (nullable), and `ambiguity`. Optional/nullable fields: `stationName`, `operationalDepartureAt`, `platform`, `source`, `updatedAt` per exact API schema required list. `status` is the seven-value `JourneyStatus`; `ambiguity` is `none|ambiguous`.

The `GET /api/stations/{id}/departures` result is an array; empty means no candidates for the requested station-local date. Server order is effective departure instant ascending then journey identity. Client filters by selected `serviceId` and exact selected `stationLocalDepartureDate`; it does not sort. The DTO retains each `journeyInstanceId` even if public labels are identical.

### `JourneySnapshotDto`

Required fields: `journeyInstanceId`, `serviceId`, `scheduleId`, `scheduleRevision`, `operationDate`, `firstDepartureOccurrence`, `revision`, `status`, `stops`, `source`, `publishedAt` (nullable). Optional/nullable: `lastUpdatedAt`, `eta` (object or null). Status is `Scheduled|Running|Delayed|Interrupted|Cancelled|Completed|Terminated`; source is `ScheduledBaseline|ControllerManual|ApprovedRailwayFeed`.

`stops` is the internally consistent snapshot list. Preserve the response's stop ordering; do not reorder by inferred timetable. Each stop has required `journeyStopId`, `stopOrder`, `stationId`, `ambiguity`; optional `stationName`; nullable planned/operational arrival/departure `date-time`s, `platform`, `timingSource`, and `timingUpdatedAt`. Ambiguity is `none|ambiguous`.

A null `publishedAt` means no official operational publication time exists and the status is a schedule baseline. Do not present an official update time if null. The current server projection emits `eta: null`.

### `JourneyUpdateDto`

Required fields: `updateId`, `journeyInstanceId`, `revision`, `kind`, `status`, `source`, `publishedAt`, and `publicChange`; `correctionOfUpdateId` is nullable and required for `kind=correction`. `kind`: `status|stopChange|routeDiversion|correction`; source: `ControllerManual|ApprovedRailwayFeed`.

`publicChange` is discriminated by `changeType`:

- `status`: required `status` enum.
- `stopChange`: required `journeyStopId`, with optional/nullable `platform`, `operationalArrivalAt`, and `operationalDepartureAt` (at least one change field supplied by the schema).
- `routeDiversion`: required ordered `divertedStops` array. Each item has journey stop and station IDs, `stopOrder`, nullable scheduled/operational times and platform.
- `correction`: required recursively typed `correctedChange`; top-level update supplies `correctionOfUpdateId` identifying an earlier update on the same journey.

The endpoint returns a complete array in ascending `revision` order, without arbitrary page limit. Empty is a valid success. Initial schedule baseline and private initialization events are excluded. Driver observation IDs, actor subjects, private evidence, incident records, and raw audit fields are not public DTO fields and must not be used by the parser or UI.

### `ProblemDetailsDto`

The responses use RFC 9457 Problem Details (`application/problem+json`). Contract fields include `type`, `title`, `status`, `detail`, `instance`, and `errors` (field to list-of-strings map); fields may be omitted according to the schema. Parse optional safe values but show only a generic passenger message. Never render internal diagnostics, raw actor identifiers, or exception content.

## Passenger domain models

| Domain type | Fields | Invariants/behavior |
|---|---|---|
| `JourneyCandidate` | journey ID, service ID, station/date/operation date, stop order, status, ambiguity, departure values, platform/source/time as supplied | Immutable; preserve source identity and values. Filter by selected service ID and station-local date. Ambiguous selection requires explicit tap. |
| `JourneySelectionContext` | Feature 001 service choice/reference, origin station ID, travel date, selected journey ID, candidate if available | Keeps service and dated journey IDs distinct and retains source context for Back navigation and labels. |
| `JourneyStatusSnapshot` | IDs/revisions/date/occurrence/status/source/publication and last-updated timestamps/ordered stops/optional ETA | Immutable projection of one response revision; no client-generated official fields. |
| `JourneyStopStatus` | stop/station IDs and name, order, planned/operational arrival and departure raw timestamps, platform, timing source/update time, ambiguity | Planned and operational values remain separately labeled; no inferred time/location/status. |
| `JourneyEstimate` | raw value timestamp, source, updated time, freshness enum | Present only when supplied; freshness is `current|stale|lastKnown`; null remains unavailable. |
| `JourneyUpdate` | update ID, journey ID, revision, kind, status, source, publication time, correction reference, typed public change | Preserve API history order; alert tracking uses update ID; supported notices point to the same ID/revision. |
| `JourneyPublicChange` | sealed/discriminated status, stop, diversion, or correction value | Never include private API/write data. Corrections reference an earlier same-journey update. |
| `JourneyReadMetadata` | client retrieval time and not-refreshed flag per section | Distinct from backend `publishedAt`, `lastUpdatedAt`, `timingUpdatedAt`, and departure `updatedAt`. |
| `SeenJourneyUpdates` | journey ID → set of update IDs, app session only | First successful history read establishes baseline; only successful history responses advance it; no persistent storage. |

## Validation and mapping rules

- Reject invalid/missing UUID, date, integer, enum, required field, discriminator, or malformed/offset-free timestamp where `date-time` is required. Nullable contract fields accept null; omission is accepted only where the schema permits it.
- Treat malformed 2xx data as protocol/unavailable; never display a partial snapshot as confirmed.
- Keep all supported statuses distinct. Reject unknown enum values as protocol failures rather than mapping them to a known status.
- Preserve backend candidate order and update revision order. Do not synthesize a journey, filter by status, calculate eligibility, reorder a route, or convert source timestamps to device time.
- Successful empty departure list after selected-service filtering means no matching dated journey. Successful `[]` updates means valid journey with no published updates. Neither is an error.
- `journeyInstanceId` is the only identity used to fetch snapshot/history after selection; `serviceId` is used only for matching the departure projection.
