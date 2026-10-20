# Operations Web feature to endpoint matrix

This matrix records the real SmartRail API operations used by the Operations Web. The API remains authoritative for role grants, ownership, and station scope.

| UI workflow | HTTP operation | Access and behavior |
| --- | --- | --- |
| Published station selector and incident resource references | `GET /api/stations` | Public published station projections; no assignment is inferred. |
| Station/date departure monitor | `GET /api/stations/{id}/departures?date={YYYY-MM-DD}` | Public dated journey departure projections in Asia/Colombo. |
| Controller journey detail | `GET /api/journeys/{id}` | Public internally consistent journey snapshot, opened from a known departure. |
| Controller journey publication history | `GET /api/journeys/{id}/updates` | Public published operational update history. |
| Controller protected Driver evidence review | `GET /api/operations/journeys/{id}/observations` | Protected Controller read for a known journey. Observation is non-authoritative evidence. |
| Controller official journey status | `PATCH /api/operations/journeys/{id}/status` | Protected; submits expected journey revision. Cancellation is explicitly confirmed in the UI. |
| Controller operational stop platform | `PATCH /api/operations/journeys/{id}/stops/{stopId}` | Protected; submits expected journey revision and platform. |
| Role/scope-filtered incident list | `GET /api/incidents` | Protected; server filters by current grants and station scope. |
| Incident report | `POST /api/incidents` | Protected; server validates category, resource ownership, grant, and scope. Uses an idempotency key. |
| Incident forward lifecycle transition | `PATCH /api/incidents/{id}` | Protected; uses expected incident revision and idempotency key. |
| Driver's own current assignments | `GET /api/driver/assignments` | Protected Driver read; backend resolves assignments from the authenticated subject. |
| Driver progress observation | `POST /api/driver/journeys/{id}/observations` | Protected; backend checks current assignment and deduplicates by request ID. Observation is non-authoritative evidence for Control. |

## Backend gaps and boundaries

- The approved requirements reference `GET /api/operations/overview`, but no such endpoint is registered in the inspected API. The Overview page therefore composes counts and the supported station departure feed; it does not claim to be the backend's operations overview.
- No journey collection/search endpoint is registered. Journeys are discoverable through the approved station/date departures operation; arbitrary journey IDs are not synthesized.
- No station assignment/scope read is registered. Station Staff sees public departure data and the server-filtered incident collection. The server checks every protected station-scoped write; a 403 is shown as a denial.
- The incident API returns current state and revision, but no incident audit/history read is registered. The UI reports current lifecycle state and does not fabricate an event timeline.
- The approved incident category catalogue is not enumerated in the contracts. The UI requires a category code and reports backend validation; it does not invent category values.
- No backend operational alert, push, WebSocket, or SignalR endpoint is registered. Current status is refreshed through the supported HTTP read operations.
- Catering discovery/order routes are absent from the live OpenAPI document, so no catering UI or simulated order state is presented.
