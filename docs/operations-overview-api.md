# Operations daily overview API

Machine-readable contract: [operations-overview.openapi.yaml](operations-overview.openapi.yaml). The runtime Development OpenAPI document also exposes the same route and response DTO.

`GET /api/operations/overview?date=YYYY-MM-DD[&stationId=<uuid>]` returns one authorized daily overview and its current journey board. `date` is required and denotes `Journey.OperationDate`, whose railway calendar is Asia/Colombo. The API reads the selected date as a `DateOnly`; it does not use the server or browser clock. A station filter selects journeys whose current published calling points include that station.

The route requires a validated Cognito access token and the current Feature 002 `ViewOperationsDashboard` grant. Controllers can request the global overview or filter it to a station. Station Staff must supply a station ID and pass the existing current station-scope check; the endpoint adds no role grant. A missing/invalid token returns 401, a verified missing grant/scope returns 403, malformed filters return 400, and unavailable authorization or source data returns 503. The endpoint does not expose an administrative write path.

The response contains `operationalDate`, `metrics`, and a `journeys` array. Each journey includes `journeyId`, internal `serviceId`, `serviceCode`, optional `serviceDisplayName`, optional `publicServiceNumber`, `routeCode`/`routeName`, origin/destination from the first and last stop in the current revision, `operationDate`, planned departure/arrival, current status, nullable recorded delay minutes, nullable last reported station, `lastUpdateAt`, and nullable `assignedDriver`. No GUID is presented as a public train number. `serviceDisplayName` and `publicServiceNumber` are null when no verified published identity exists; clients should fall back to the public number, then the existing service code. `assignedDriver` is null because the current approved projections do not provide a safe Controller display identity.

Metric definitions:

- `totalScheduled`: all dated journey instances planned for the requested operation date, including journeys later cancelled or completed.
- `running`: journeys whose current official lifecycle state is `Running`.
- `delayed`: journeys whose current official state is `Delayed`, or whose latest timing observation in the current revision has a positive difference from its scheduled event. A running journey may also count as delayed.
- `openIncidents`: current authorized incidents in `Reported` or `In Progress`, from the existing incident use case and its current role/resource checks. This is a current control-room count and is independent of the selected journey operation date because incident reports have no approved operational-date filter. `Resolved` and `Closed` are excluded.
- `cancelled`: current official state is `Cancelled`.
- `completed`: current official state is `Completed`.

Delay and last-reported-station values use only timing observations whose source is `ControllerManual` or `ApprovedRailwayFeed` and which include a timing timestamp. The latest such event determines the displayed delay, clamped to zero when on-time/early; no observation or missing scheduled comparison yields null. The reported station is the station of that latest verified observation, not GPS-derived current location. The last update is the current publication timestamp, or initialization timestamp for the unchanged baseline. The repository reads each day from one PostgreSQL repeatable-read snapshot. Incomplete current journey/reference projections fail with 503 rather than fabricate fields.

The development seeder's added completed example is synthetic: journey `d0040000-0000-0000-0005-000000000005` uses the existing synthetic `DEV-SEED-001` service/schedule on 2026-11-02. The example does not assert a real train name or official public number. Run the guarded `dev/local-data/Invoke-DevSeed.ps1` workflow only after the approved migration is separately applied and local seed execution is authorized.
