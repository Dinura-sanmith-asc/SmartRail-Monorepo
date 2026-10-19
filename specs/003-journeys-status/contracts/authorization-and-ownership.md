# Authorization and Ownership Boundary

Feature 003 consumes Feature 002's authentication and authorization decisions. It does not create roles, alter grants, or duplicate owner data.

## Approved operation/action mapping

| Operation | Access | Feature 002 action/scope |
|---|---|---|
| `GET /api/journeys/{id}` | Anonymous published projection | Existing `ViewPublishedJourneyStatus` public read grant. |
| `GET /api/journeys/{id}/updates` | Anonymous published projection | Existing `ViewPublishedJourneyStatus` public read grant. |
| `GET /api/stations/{id}/departures` | Anonymous published projection | Existing `ViewStationDepartures` public read grant. |
| Status/cancellation/diversion `PATCH` | Authenticated Controller | Current active `PublishOfficialJourneyStatus` grant; this approved Controller grant has global journey scope. Validate the target journey and operation. Cancellation additionally requires the separate existing `ConfirmJourneyCancellation` grant. |
| Stop platform/timing `PATCH` | Authenticated Controller | Current active `ChangePlatformOrOperationalTiming` grant with global Controller journey scope; validate the target journey and stop. |

The route-diversion publication uses the already approved official journey publication grant. A route diversion does not grant schedule editing. The optional observation reference requires the existing authorized Driver Operations observation read; it does not add an observation review or mutation operation.

## Decision flow

For every PATCH request, API validates the Feature 002 Cognito access token and sends only the verified subject to Application. Application resolves the current active internal roles from PostgreSQL for that operation and evaluates the exact Feature 002 action. The approved Controller grant supplies global journey scope; no Controller-to-journey assignment is required. The target journey, stop and requested operation are still validated before persistence. Cancellation separately evaluates `ConfirmJourneyCancellation`. Driver and Station Staff resource restrictions and all applicable owner checks remain enforced. A request that includes a Driver observation ID additionally validates the reference through Driver Operations. Domain transition and revision checks run before persistence. Missing, revoked, insufficient or unavailable authorization evidence denies the operation or returns safe unavailable behavior without a write.

Missing/invalid credentials map to 401; verified but insufficient role/scope to 403. These existing Feature 002 outcomes are unchanged. If a publication supplies a Driver observation ID, malformed reference is 400; missing or inaccessible observation is 404 without revealing cross-resource existence; stale, revoked, or journey-incompatible evidence is 409; unavailable Driver Operations verification is 503. The owner must confirm the observation is accessible and associated with the target journey/current operational context before commit. No official update is committed unless required verification succeeds. Stale expected journey revision is 409. Error bodies and logs must not expose tokens, raw claims, actor subject, SQL, stack trace, or connection details.

## Ownership

- Identity & Permissions owns active role mapping and station scope. For Controller publication, the approved Controller action grant is global across journeys; Feature 003 supplies and validates the target journey and does not introduce Controller assignment data. Identity's existing Driver and Station Staff resource-scope checks remain authoritative. Feature 003 does not cache allow decisions.
- Driver Operations owns driver assignments and observations. Feature 003 stores only an optional stable observation reference in private audit evidence after the owner confirms it is valid and readable.
- Incident Management owns incident records, lifecycle, affected-journey associations, and authorized incident-context reads. The operations client composes owner-authorized incident context with Feature 003 official journey data. This is the approved interpretation of the baseline requirement that incident impact appear on affected journey views; the requirement wording is unchanged. Feature 003's public `JourneySnapshot`, update history, and station-departure projections contain no private incident records. No Feature 003 route or RBAC grant is added.
- Feature 003 Application defines `IIncidentJourneyContextReader` as a framework-neutral owner boundary for an authorized caller and journey identity. The contract result distinguishes available context, verified no-context, denied access, and unavailable owner service; only Incident Management decides what context the caller may read. Until the owner module supplies a genuine provider, production integration returns unavailable. Test doubles are confined to isolated tests. The operations client must not infer incident context from public journey data or treat an unavailable owner read as an empty result.
- Railway Network owns reusable schedule, route, station, service, and publication completeness. Feature 003 references these identifiers and never edits their rows.
- Journeys & Status owns dated instance identity, official status, operational stop snapshots, revision history, and publication audit.

Public projections omit private actor and Driver observation identifiers. Client-supplied roles, scopes, actor subjects, or timestamps are ignored/rejected; server evidence is authoritative.

## Significant-change and client boundary

Feature 003's existing journey and update reads expose cancellation/platform change type with the immutable `UpdateId`, journey revision, safe source, and publication timestamp. The passenger client owns configured in-app alert delivery and presentation and must associate the visible alert/status with that same revision. There is no notification API, native push service, or backend claim of delivery in this contract. Full SR-025/SC-007 acceptance remains open until the passenger-client behavior is implemented and tested.

SR-044 acceptance remains open until Incident Management demonstrates its authorized affected-journey read and the operations client demonstrates composition with official Feature 003 journey data.
