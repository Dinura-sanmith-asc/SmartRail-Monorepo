# Authorization and Ownership Boundary

Feature 003 consumes Feature 002's authentication and authorization decisions. It does not create roles, alter grants, or duplicate owner data.

## Approved operation/action mapping

| Operation | Access | Feature 002 action/scope |
|---|---|---|
| `GET /api/journeys/{id}` | Anonymous published projection | Existing `ViewPublishedJourneyStatus` public read grant. |
| `GET /api/journeys/{id}/updates` | Anonymous published projection | Existing `ViewPublishedJourneyStatus` public read grant. |
| `GET /api/stations/{id}/departures` | Anonymous published projection | Existing `ViewStationDepartures` public read grant. |
| Status/cancellation/diversion `PATCH` | Authenticated Controller | `PublishOfficialJourneyStatus` plus current journey scope; cancellation additionally requires existing `ConfirmJourneyCancellation`. |
| Stop platform/timing `PATCH` | Authenticated Controller | `ChangePlatformOrOperationalTiming` plus current journey scope. |

The route-diversion publication uses the already approved official journey publication grant. A route diversion does not grant schedule editing. The optional observation reference requires the existing authorized Driver Operations observation read; it does not add an observation review or mutation operation.

## Decision flow

For every PATCH request, API validates the Feature 002 Cognito access token and sends only the verified subject to Application. Application resolves current active internal roles from PostgreSQL, evaluates the exact Feature 002 action, then asks Feature 002 to evaluate current journey scope. Cancellation checks the additional approved cancellation permission. A request that includes a Driver observation ID additionally validates the reference through Driver Operations. Domain transition and revision checks run before persistence. The operation writes only after all evidence is verified and available.

Missing/invalid credentials map to 401; verified but insufficient role/scope to 403. These existing Feature 002 outcomes are unchanged. If a publication supplies a Driver observation ID, malformed reference is 400; missing or inaccessible observation is 404 without revealing cross-resource existence; stale, revoked, or journey-incompatible evidence is 409; unavailable Driver Operations verification is 503. The owner must confirm the observation is accessible and associated with the target journey/current operational context before commit. No official update is committed unless required verification succeeds. Stale expected journey revision is 409. Error bodies and logs must not expose tokens, raw claims, actor subject, SQL, stack trace, or connection details.

## Ownership

- Identity & Permissions owns role mapping and station scope; it evaluates journey scope using current authoritative assignments. Feature 003 does not cache allow decisions.
- Driver Operations owns driver assignments and observations. Feature 003 stores only an optional stable observation reference in private audit evidence after the owner confirms it is valid and readable.
- Incident Management owns incident records/lifecycle and incident-to-journey relationships. Feature 003 may consume approved read context but never creates/updates incident lifecycle or infers official journey status from an incident.
- Railway Network owns reusable schedule, route, station, service, and publication completeness. Feature 003 references these identifiers and never edits their rows.
- Journeys & Status owns dated instance identity, official status, operational stop snapshots, revision history, and publication audit.

Public projections omit private actor and Driver observation identifiers. Client-supplied roles, scopes, actor subjects, or timestamps are ignored/rejected; server evidence is authoritative.
