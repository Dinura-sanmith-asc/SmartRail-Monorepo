# Contract Register: Station Information & Departure Board

This register records the approved public contract authority and the remaining integration decisions. The backend owns contract design, railway data and operational rules.

## Approved existing contract

Source: [Railway Network OpenAPI](../../../docs/contracts/railway-network.openapi.yaml)

| Operation | Use in this feature | Contract status / boundary |
|---|---|---|
| `GET /api/stations` | Validate the deployment station ID and obtain its published station identity/name. | Implemented contract. Public list of published station records; `stationId` and `name` are required. No live departures implied. |
| `GET /api/routes/{id}/stops` | Optional ordered route reference only if a specified display need requires it. | Implemented contract. Route reference data; not a live journey/departure source. |
| `GET /api/services` | Not a source for the departure board. | Implemented planned-service search contract; responses are planned schedules, not dated operational journeys or live status. |
| `GET /api/services/{id}` | Optional planned service detail only if needed to label a supported reference value. | Implemented planned-service contract; does not provide live journey state. |

The display MUST NOT use service search as a fallback for station departures.

## Approved Journeys & Status contract

Source: [Journeys & Status OpenAPI v1.0.0](../../../../SmartRail/specs/003-journeys-status/contracts/journeys-status.openapi.yaml)

| Operation | Use in this feature | Contract status / boundary |
|---|---|---|
| `GET /api/stations/{id}/departures?date=YYYY-MM-DD` | Read the public dated departure projection for the configured station. | Approved contract v1.0.0. Anonymous GET; required station-local date in `Asia/Colombo`; returns an array (empty means no matching departures), ordered by known departure instant then journey ID. It carries the station and journey IDs, schedule/operational departure timestamps, status, platform, source, revision, update timestamp, operation date, and ambiguity. Errors use RFC 9457 `application/problem+json` for 400, 404 and 503. |
| `GET /api/journeys/{id}` | Not needed for the board's current public projection. | Approved contract v1.0.0, but no additional display fields needed beyond the departures response. |
| `GET /api/journeys/{id}/updates` | Not needed for the current board projection. | Approved contract v1.0.0; returns complete public history in ascending revision order, but defines no push mechanism, cursor, wait behavior, or polling interval. It does not independently satisfy automatic delivery. |

The approved departures response has **no human-readable service code and no delay value**. The display may identify a departure using its exact `serviceId`; it must not invent a code or derive delay by subtracting scheduled and operational timestamps. A null/missing scheduled or operational value remains unknown. Cancellation is represented by the contract status (`Cancelled`); platform is source-provided. The response's `revision`, `source`, and `updatedAt` remain attached to the same departure snapshot. `ambiguity: ambiguous` must remain visible and must not cause the client to choose a departure automatically.

The endpoint is date-scoped, not explicitly “upcoming-only”; the contract includes departures matching that station-local date. The client supplies the current calendar date in `Asia/Colombo` for each request, including requests after midnight, and preserves the service's returned order. The OpenAPI defines no automatic delivery mechanism or cadence.

## Approved prototype refresh policy — recorded 2026-10-19

The project owner approved configurable HTTP polling of the verified station-departures snapshot endpoint for this prototype:

- Fetch an initial snapshot after the configured station has been validated.
- While connected, issue a new GET every configured interval. The default is 30000 ms.
- Read the public Vite configuration variable `VITE_STATION_DEPARTURE_REFRESH_INTERVAL_MS`. Vite variables are public and are normally embedded at build time; they must not contain credentials or secrets. An unset variable uses the default. A configured value must be a positive integer supported by the browser timer; invalid values reject polling and must not create a zero-delay or uncontrolled request loop.
- Recalculate the current station-local service date in `Asia/Colombo` for every request. Do not cache the date across midnight.
- Do not overlap or queue snapshot requests. Skip a scheduled poll tick while a request is in flight. A reconnect event may request immediate revalidation; if a request is already in flight, coalesce/ignore the duplicate trigger until that request settles.
- A failed request makes the retained last successful snapshot cached/possibly outdated. Keep that warning through subsequent failures and remove it only after a successful backend response.
- Preserve the response's order, exact `serviceId`, nullable/unknown values, status, platform, source, revision, ambiguity and timestamp values. Do not derive delay or ETA. The contract has no delay field, and display meaning for timestamps remains subject to verification.

This is a **prototype client policy**, not a production freshness SLA, guaranteed backend update latency, backend push/delivery contract, cache-expiry threshold, or backend capacity/operational guarantee. It does not change or supplement the approved OpenAPI contract.

## Remaining live integration decisions

| Decision | Current status | Required before live acceptance |
|---|---|---|
| Prototype automatic polling policy (T029) | Implemented per the project owner's approval: configurable repeated GET of the v1.0.0 station-departures snapshot, default 30000 ms. The API remains a snapshot and `/api/journeys/{id}/updates` returns history only. | Prototype behavior is client policy only; it makes no production or backend capacity commitment and adds no push contract. |
| Live automatic update verification (T030) | Not yet verified. The opt-in live test is implemented, but no dedicated API, seeded journey, or Controller test identity is configured in this workspace. The v1.0.0 OpenAPI itself promises no delivery cadence or latency. | Run against an approved dedicated development/test environment and verify a newly published backend snapshot appears on a subsequent poll without a manual action. Do not assert a timing SLA; record backend environment and observed result. |
| `GET /api/journeys/{id}/updates` for the display (T031) | Conditional and currently unnecessary for rendering: the station departure response already carries the current projection and revision. | Reconsider only if the backend approves it as necessary for the selected delivery mechanism. This endpoint alone does not establish delivery or cadence. |
| Live environment and representative data (T026, T047) | T026 verified against the migrated local Development API on 2026-10-19; the opt-in browser check returned a dated departure from an explicitly identified acceptance journey and passed the documented error cases. | T047 still requires live polling/update, operational-change and outage/revalidation evidence in an approved environment; the local T026 result does not complete release acceptance. |

Backend integration tests in `SmartRail/tests/SmartRail.IntegrationTests/JourneysStatus/Api/JourneySelectionFlowTests.cs` cover the endpoint's date requirement, malformed and missing dates, empty results, ambiguous identity, and synchronized cancellation/platform projections with consistent revisions and publication timestamps. `JourneysStatusOpenApiTests.cs` verifies published OpenAPI paths and required date metadata. These are backend contract tests, not live-environment acceptance for this React app.

## Frontend boundary

The feature's client-facing adapter boundary is conceptually responsible for:

1. Reading deployment station identity and validating it using published station data.
2. Requesting station departures only from the approved Journeys & Status source when available.
3. Mapping approved response meaning to the board without adding railway business rules.
4. Exposing connection/request state and last successful data to the presentation layer.

The UI consumes a normalized view through the T007 source boundary. The live adapter must map only the approved response semantics above; it must not supply synthetic `serviceCode` or `delay` values.

## Error and data-trust rules

- Current station reference errors follow the checked-in OpenAPI Problem response contract.
- Departure/journey errors use the approved RFC 9457 response contract; nullable times and platform must remain unknown when absent.
- Never render fixtures as live information; fixtures must be clearly identified in development/test contexts.
- Never convert missing values, old data, or scheduled service data into confirmed operational status.
