# Conceptual Data Model: Station Information & Departure Board

This model describes information concepts and UI state from the approved feature spec. It is not a proposed API schema or persistence design. Fields beyond the existing station reference contract remain conceptual until Journeys & Status approves its contract.

## Entities

### Station Context

Represents the one station assigned to a deployed display.

- **Configured station identifier**: Stable identifier supplied through deployment configuration. It is required, public configuration and not a secret.
- **Published station identity**: Validated identity from the public Railway Network source. The approved current contract requires `stationId` and `name`; optional location/service detail arrays are only rendered if relevant and approved.
- **Validation state**: Missing/invalid configuration, loading validation, validated published station, or unavailable/unpublished station.

**Rules**: Exactly one configured station per display. The ID must match a public published station before departure information is shown. Missing, invalid or unpublished IDs never fall back to another station. No passenger selector or station-management capability exists.

### Station Departure

Represents a dated train departure serving the configured station; it is distinct from a reusable Train Service schedule.

- **Station association**: The configured station context for which the backend returned the departure.
- **Departure identity**: Opaque contract-provided identity; exact field and uniqueness semantics are TBD by the backend.
- **Service identity**: Published service label/identity where supplied by the approved departure response.
- **Scheduled departure time**: Planned time, retaining its published meaning and source timezone/date interpretation.
- **Operational values**: Optional current published status, platform, delay, cancellation or ETA only when the authoritative contract supplies them.

**Rules**: The frontend does not construct a dated departure from `GET /api/services`; it does not infer status, delay, platform, ETA, currentness, ordering or “upcoming” eligibility. A missing operational value remains unknown/unavailable.

### Operational Update

Represents an authorized source-provided change associated with a dated journey/departure.

- **Changed value**: Published status/platform/delay/cancellation or other field approved for the public view.
- **Source/update timestamp**: Shown only if supplied and its meaning has been defined by the backend contract.
- **Revision/order token**: Used only if the contract defines its comparison semantics; exact representation is TBD.

**Rules**: Update meaning and ordering belong to the authoritative backend. The display does not reinterpret a driver observation or calculate a status.

### Display Information State

Represents the board's overall user-visible data condition.

| State | Meaning |
|---|---|
| Loading | Configuration or authoritative information is being obtained. |
| Current | A successful authoritative response has been obtained and no connection-loss warning applies. |
| Empty | A successful departure response contains no upcoming departures. |
| Unavailable | Configuration, station identity, or required source is unavailable; no false departure is shown. |
| Error | A request failed; existing data may remain visible only with stale/cached marking. |
| Disconnected / stale | Connectivity is unavailable and the last successful response is retained and visibly marked as possibly outdated. |
| Revalidating | Connectivity returned, but latest authoritative information has not yet been successfully obtained. |
| Recovered | Revalidation succeeded; the updated board can be represented as current. |

**Rules**: A stale warning is removed only after successful authoritative revalidation. Last-update time is displayed only when a reliable source value with approved semantics exists. No numeric freshness threshold is embedded here.

## Relationships

- One deployed display has exactly one configured Station Context.
- One Station Context may have zero or more dated Station Departures in a successful response.
- A Station Departure may have zero or more source-authoritative Operational Updates.
- Display Information State governs how the station/departure data is presented, not the underlying railway truth.

## Contract and persistence boundaries

- Current station reference attributes are limited to the checked-in Railway Network OpenAPI contract.
- Future departure and journey attributes are intentionally not assigned JSON names, types or requiredness.
- No frontend database or durable browser cache is planned. Active-session retention is an implementation detail; persistence/expiry requires an approved policy.
- The current `/api/services` Train Service is a reusable planned schedule reference. It is not equivalent to a dated Station Departure.
