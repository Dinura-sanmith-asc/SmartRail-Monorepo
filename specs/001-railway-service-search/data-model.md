# Data Model: Railway Search and Service Details

This is a Flutter client model plan derived from the approved OpenAPI schemas. It does not
change server storage or extend the API. Keep response DTOs inside `features/railway_search/data/`
and expose immutable passenger-facing models through the domain/application boundary.

## Contract DTOs

All identifiers are UUID strings as represented in JSON. Parsers validate expected JSON
types, required fields, and date/date-time syntax; malformed successful responses become a
typed protocol/unavailable error. Optional contract properties remain nullable rather than
being fabricated.

| DTO | Contract fields | Rules |
|---|---|---|
| `StationListDto` | `items: List<StationDto>` | Required. Empty means no published station records. |
| `StationDto` | `stationId: String`, `name: String`, optional `locationDetails: List<String>?`, optional `serviceDetails: List<String>?` | ID and non-empty name required. Show only optional details actually returned. |
| `RouteStopsDto` | `routeId`, `routeCode`, `name`, `stops: List<RouteStopDto>` | All required; empty stops is an explicit successful response with no stop records. |
| `RouteStopDto` | `stopOrder: int`, `stationId`, `stationName` | All required; display in `stopOrder`. No stop timetable field exists. |
| `ServiceSearchResponseDto` | `originStationId`, `destinationStationId`, `travelDate`, `services: List<ServiceChoiceDto>` | All required; date is ISO full date; empty services is a successful no-match response. |
| `ServiceChoiceDto` | `serviceId`, `serviceCode`, `routeId`, `routeCode`, `scheduleRevision: int`, `origin: ScheduledCallDto`, `destination: ScheduledCallDto` | All required; preserve response identity and ordering. |
| `ScheduledCallDto` | `stationId`, `stationName`, `stopOrder: int`, `scheduledAt: String` | All required; scheduledAt is offset-bearing `date-time`, planned only. Preserve its raw text and local date/clock/offset components. |
| `ServiceDetailDto` | `serviceId`, `serviceCode`, `route: ServiceRouteDto`, `schedule: ServiceScheduleDto` | All required; distinct detail read from a search choice. |
| `ServiceRouteDto` | `routeId`, `routeCode`, `name` | All required. |
| `ServiceScheduleDto` | `revision: int`, `firstDepartureTimeLocal: String` | Both required; local time only, no date-specific operation or live state. |
| `ProblemDto` | required `type`, `title`, `status`; optional `detail`, `instance`, `code`, `errors: Map<String,List<String>>`, `traceId` | Parse optional extensions defensively. Never show internal stack/diagnostic data. |

Exact wire field spellings and response content types are authoritative in
[`railway-network.openapi.yaml`](../../docs/contracts/railway-network.openapi.yaml).

## Domain and Screen Models

### `Station`

- `id` (UUID string), `name`, optional location/service detail lists.
- A selection references one item from the currently loaded published-station response.
- Client blocks origin and destination with the same ID; server remains authoritative for
  unknown/unpublished IDs and any other search validity.

### `TravelDate`

- Calendar-only year, month, and day selected by the passenger.
- Serialize exactly as `YYYY-MM-DD` without UTC conversion or a time component.
- No client-authored past-date rule or maximum range; let the API decide eligibility.

### `SearchCriteria`

- Nullable `originStationId`, `destinationStationId`, `travelDate` during editing.
- Submittable only when all three exist and origin ID differs from destination ID.
- Any edit after result display clears the result snapshot and invalidates any in-flight
  response associated with the old criteria.

### `ScheduledCall`

- `stationId`, `stationName`, `stopOrder`, immutable `rawScheduledAt` string.
- Derived display components are the literal local date, wall-clock time, and optional
  offset text found in `rawScheduledAt`; do not turn it into a device-zone instant. The
  presentation layer labels it “planned” or “scheduled”.
- Compare the literal call date to the selected origin `TravelDate` for the approved later
  day cue. Do not calculate service runs or elapsed duration.

### `ScheduledServiceChoice`

- `serviceId`, `serviceCode`, `routeId`, `routeCode`, `scheduleRevision`, planned `origin`
  and `destination` calls.
- Keeps result values available to the detail screen even when a later detail read fails.
- Results preserve backend ordering because the API does not define client-side sorting.

### `ServiceDetail` and `RouteStops`

- `ServiceDetail`: service identity, route identity/name, schedule revision, and the
  separately labeled `firstDepartureTimeLocal` schedule time.
- `RouteStops`: route identity/name and ordered stop identity/name/order list.
- Model the two read outcomes independently; neither section is a prerequisite for rendering
  the other. No model contains intermediate-stop times, live status, ETA, disruption, or
  ticket/reservation fields.

## Error and Async State Types

Use a small typed failure classification at the repository boundary:

| Failure kind | Source | Passenger-facing treatment |
|---|---|---|
| `validation` | HTTP 400 from the search operation; optionally field-specific `errors` | Keep search criteria and allow correction; use field detail only when identified. |
| `invalidRequest` | HTTP 400 from a route-stop or service-detail operation (malformed path ID) | Mark only that detail section unavailable, retain sibling/result summary, and allow Back; repeating the same malformed ID is not useful. |
| `accessDenied` | HTTP 403 if returned by gateway/platform | Access unavailable; Back only, no login or retry. |
| `notFound` | HTTP 404 for service/route reads | Mark affected information unavailable; preserve other data and allow Back. |
| `unavailable` | HTTP 503, HTTP 500/default server response, timeout, socket/network failure | Mark only affected information unavailable and offer retry of that read. |
| `protocol` | Invalid JSON/type/required field in a successful response or unrecognized response shape | Safe unavailable message; do not present as empty data. |

Avoid representing failure as an empty list. A successful empty list and a failed list are
distinct states. For independently loaded service/route detail, each section tracks
`loading`, `data` (which may contain an empty stop list), `accessDenied`, `notFound`, or
`unavailable`, plus last successful client retrieval time where applicable.

Search flow state includes criteria, station-load state, one active request generation,
and a result state (`notSearched`, `loading`, `success`, `empty`, `validation`,
`accessDenied`, `unavailable`). A 404 is only expected from the detail reads, not the
service-search operation's declared response list. Apply search-criteria correction to the
search endpoint's 400; classify detail endpoint 400 as invalid request input because the
passenger cannot edit a path UUID. Keep prior results when pushing Details; clear immediately
on criteria edits. The retained in-memory state is session data, not persistent offline cache.

## Relationships

```text
SearchCriteria --origin--> Station
SearchCriteria --destination--> Station
SearchCriteria --1--> ServiceSearchResponse
ServiceSearchResponse --0..n--> ScheduledServiceChoice
ScheduledServiceChoice --1--> origin ScheduledCall
ScheduledServiceChoice --1--> destination ScheduledCall
ScheduledServiceChoice --1--> ServiceDetail request (serviceId)
ScheduledServiceChoice --1--> RouteStops request (routeId)
```

Service detail and route stops are sibling public reads. Their success/failure state is
independent; Back restores the criteria and service choices retained by the route stack.
