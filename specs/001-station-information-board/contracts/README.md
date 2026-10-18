# Contract Register: Station Information & Departure Board

This register records existing contract authority and required future seams. It intentionally does not define an API for future endpoints. The backend owns contract design, railway data and operational rules.

## Approved existing contract

Source: [Railway Network OpenAPI](../../../docs/contracts/railway-network.openapi.yaml)

| Operation | Use in this feature | Contract status / boundary |
|---|---|---|
| `GET /api/stations` | Validate the deployment station ID and obtain its published station identity/name. | Implemented contract. Public list of published station records; `stationId` and `name` are required. No live departures implied. |
| `GET /api/routes/{id}/stops` | Optional ordered route reference only if a specified display need requires it. | Implemented contract. Route reference data; not a live journey/departure source. |
| `GET /api/services` | Not a source for the departure board. | Implemented planned-service search contract; responses are planned schedules, not dated operational journeys or live status. |
| `GET /api/services/{id}` | Optional planned service detail only if needed to label a supported reference value. | Implemented planned-service contract; does not provide live journey state. |

The display MUST NOT use service search as a fallback for station departures.

## Future dependencies — contract not verified

| Expected operation from the approved RBAC/API inventory | Required capability | Required backend decision before live integration |
|---|---|---|
| `GET /api/stations/{id}/departures` | Dated upcoming station departures, scheduled values and latest available authorized operational status. | Response schema and field meanings; station/date/time/timezone rules; upcoming eligibility and ordering; source timestamp/revision; optional/unknown values; public projection; pagination if applicable; error behavior. |
| `GET /api/journeys/{id}` | Journey detail only if the departure response does not itself contain the approved public operational projection. | Whether the display needs this call, and its public response schema, field meanings, freshness and errors. |
| `GET /api/journeys/{id}/updates` | Change/revision information only if the approved refresh design requires it. | Whether this call supports change detection; revision/order and timestamp semantics; delivery and error behavior. |

These paths are recorded as expected future dependencies only. This document does not claim they are implemented, does not define request/response bodies, and does not approve a transport/refresh mechanism. Live integration must wait for backend-owned contract publication and verification.

## Frontend boundary

The feature's client-facing adapter boundary is conceptually responsible for:

1. Reading deployment station identity and validating it using published station data.
2. Requesting station departures only from the approved Journeys & Status source when available.
3. Mapping approved response meaning to the board without adding railway business rules.
4. Exposing connection/request state and last successful data to the presentation layer.

The UI consumes a normalized view only after contract mapping is approved. No TypeScript shape is specified here because the future source schema is not yet available.

## Error and data-trust rules

- Current station reference errors follow the checked-in OpenAPI Problem response contract.
- Future departure/journey errors and unknown-value conventions remain backend dependencies.
- Never render fixtures as live information; fixtures must be clearly identified in development/test contexts.
- Never convert missing values, old data, or scheduled service data into confirmed operational status.
