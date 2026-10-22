# Journey Status Client Integration Contract

This is a passenger-client integration guide for the existing SmartRail Journeys & Status API. It does not add or change backend routes. Canonical OpenAPI: `SmartRail/specs/003-journeys-status/contracts/journeys-status.openapi.yaml` in the backend repository.

## Base URL and request policy

- Read the configured API base URI from existing `AppConfig` / `SMART_RAIL_API_BASE_URL` environment configuration.
- Reuse app-owned `sharedHttpClientProvider`; do not create another long-lived client or close it from feature screens/notifiers.
- All three reads below are public and send no `Authorization` header. Do not add passenger sign-in or protected operations.
- Use bounded timeout, UTF-8 decoding, safe Problem Details, and typed failures through existing `HttpApiClient`/network primitives.
- Use UUID strings as path identifiers. Encode the selected origin's `TravelDate` as `YYYY-MM-DD` without timestamp conversion.

## Operations

| Operation | Request | Successful response | Documented errors |
|---|---|---|---|
| Station departure lookup | `GET /api/stations/{stationId}/departures?date={date}`. `stationId` is Feature 001 origin UUID. `date` is required station-local calendar date in `Asia/Colombo`. | 200 JSON array of `StationDeparture`; `[]` means no departures. | 400 invalid/missing station ID or date; 404 station missing; 503 departure data unavailable. |
| Journey snapshot | `GET /api/journeys/{journeyInstanceId}`. ID must be returned by a selected candidate. | 200 `JourneySnapshot`, one internally consistent revision. | 400 malformed ID; 404 journey missing; 503 journey data unavailable. |
| Public updates | `GET /api/journeys/{journeyInstanceId}/updates`. Same opaque selected journey ID. | 200 complete ascending-revision JSON array; `[]` is valid. | 400 malformed ID; 404 unknown journey; 503 updates unavailable. |

No passenger request is made to `PATCH /api/operations/...`; those are protected controller writes outside this feature. The backend enforces anonymous read behavior and owns status publication.

## Identity lookup and selection

1. From Feature 001 service details, pass selected `serviceId`, `origin.stationId`, and selected `TravelDate` to the Feature 002 journey entry flow.
2. Request station departures using `origin.stationId` and that calendar date.
3. Keep only rows with exact matching `serviceId` and `stationLocalDepartureDate` equal to selected date.
4. If no rows match, show “dated journey information is not available” as a valid empty state; do not construct an instance from service or schedule.
5. If exactly one matching row has `ambiguity=none`, it may open directly. For multiple candidates or any candidate marked `ambiguous`, display each returned identity as a separate choice and require a passenger tap, per approved Q1. Never choose the first row automatically. If documented labels are identical, accessible ordinal list labels may distinguish controls without claiming a backend distinction.
6. Use the explicitly selected row's `journeyInstanceId` for both journey API paths. Never substitute `serviceId`.

Departure rows are ordered by effective station departure instant, then journey ID as tie-breaker. Preserve returned order. `operationDate` and `stationLocalDepartureDate` are distinct; the latter is the query/matching date in Asia/Colombo. Scheduled departure is used when available; for a diversion-only stop without scheduled departure, the backend may return a verified operational departure. Do not calculate missing dates/times.

## Public data and display semantics

- Snapshot status enum is exactly `Scheduled`, `Running`, `Delayed`, `Interrupted`, `Cancelled`, `Completed`, `Terminated`.
- Snapshot source is `ScheduledBaseline`, `ControllerManual`, or `ApprovedRailwayFeed`; update records use `ControllerManual` or `ApprovedRailwayFeed`.
- Baseline `publishedAt` can be null. Do not present an official update time if null.
- Planned stop and operational stop date-times are separate. Preserve raw offset-bearing timestamp text; do not convert to device timezone.
- `eta` is optional/null. When present, fields are `value`, `source`, `updatedAt`, `freshness` (`current|stale|lastKnown`). Current backend implementation returns null; display unavailable, never calculate an ETA.
- Update history is complete, ascending by `revision`; initial baseline and private initialization audit events are excluded. Public changes are tagged `status`, `stopChange`, `routeDiversion`, or `correction` and include only documented public data. Actor subjects and driver observation IDs are never public.
- Keep backend source/publication/update times distinct from the client's last successful retrieval timestamp.

## HTTP and recovery mapping

| Response | Passenger behavior |
|---|---|
| 200 array with candidates | Filter by selected service/date; continue according to explicit selection rules. |
| 200 `[]` departures | Show no matching dated journey; Back to scheduled service details. |
| 200 snapshot | Display returned status and values without inference. |
| 200 `[]` updates | Show a distinct “No published updates” state alongside snapshot. |
| 400 | Invalid identifier/date/request context. Show safe unavailable/invalid selection feedback and Back. Do not retry unchanged malformed input. A 400 departure lookup is a client-context defect because values came from Feature 001, not a passenger correction prompt. |
| 401/403 | Unexpected for anonymous reads. Explain access is unavailable; no sign-in prompt or retry loop; Back remains available. |
| 404 | Explain selected station/journey information is no longer available; preserve unrelated loaded sections, provide Back, and do not retry the same missing identity automatically. |
| 500/unexpected status | Safe temporary-unavailable feedback with retry for only the failed read. Do not surface Problem internals or assume extra API semantics. |
| 503 | Safe railway-information-unavailable feedback and retry only the failed read. |
| Timeout/network failure | Safe connection/unavailable feedback; retain prior successful data marked not refreshed; allow section-specific retry. |
| Malformed 2xx | Treat as protocol/unavailable; never render malformed values as official; allow retry for the affected read. |

Snapshot and history reads are independent. A loaded section remains available when its sibling fails. Manual refresh starts both; an individual Retry starts only the failed read. If successful reads have different revisions, do not merge them into a synthetic single-revision view; identify incomplete/temporarily inconsistent information and allow refresh. Back remains available during requests; late results after route departure or selected-journey change are ignored.

## Alert detection

- No push, subscription, event stream, or polling operation exists.
- A successful first update-history read in an app session establishes the baseline for that journey and does not alert for pre-existing history.
- A later journey-view entry or manual refresh may detect update IDs absent from the successfully observed set and alert only for supported significant public changes (delay/cancellation status, platform, route diversion, or operational time).
- Associate each alert with the same `updateId` and `revision` presented in history. Do not invent change reasons.
- Advance the session-seen set only after a fully valid successful history response; a failed request must not suppress later alerts. Suppress the same update from alerting again during the active session. Session state is memory-only and resets on app restart.

## Test and evidence boundary

Mocked tests cover strict parsing, request construction, anonymous headers, response classification, state transitions, partial failure, retry targeting, alert de-duplication, and navigation disposal. Separate Android emulator integration tests use explicit API URL and backend-seeded data and verify actual departure selection, journey snapshot/history values, and Back behavior. Missing backend/device prerequisites are `BLOCKED` or `NOT RUN`, never passed.
