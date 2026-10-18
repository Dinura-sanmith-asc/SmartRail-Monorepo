# Railway Network Client Integration Map

**Purpose**: Design-time map from approved public Railway Network API operations to mobile
client behavior. This is not a replacement for, or amendment to, the OpenAPI contract.

**Authority**: [`docs/contracts/railway-network.openapi.yaml`](../../../docs/contracts/railway-network.openapi.yaml)
and the approved passenger specification. All operations are anonymous public reads
(`security: []`). Do not send credentials or add endpoints.

## Environment and Request Rules

- Configure a base URL per environment; OpenAPI server `/` is relative to that base. Do
  not commit a production host or treat device `localhost` as the development server.
- Build query/path values with `Uri`, percent-encode as normal, and use UUID strings supplied
  by the API. Never synthesize an ID.
- Set JSON `Accept` headers; do not send an `Authorization` header for these public reads.
- Search `date` is an ISO full date for the scheduled departure date at the selected origin.
  It is not the service's first-call date for every overnight service.
- Decode using UTF-8 response bytes. Parse only documented schema fields. `Problem` is
  `application/problem+json`; successful objects are `application/json`.
- One bounded timeout is applied consistently to each request. On timeout, transport error,
  or connectivity loss, mark the affected data unavailable; retry only after passenger action.
- Do not expose raw backend text, `traceId`, or stack diagnostics. Keep diagnostic IDs out of
  passenger presentation.

## Operations

| Operation | Request values | Successful result | Documented responses and mobile behavior |
|---|---|---|---|
| `GET /api/stations` | None | `StationList { items: Station[] }`; empty `items` means no published stations. | `200`: show choices or explicit no-stations state. `503`: unavailable with Retry. `default`: safe unexpected/unavailable state with Retry where read can be repeated. |
| `GET /api/services?origin={uuid}&destination={uuid}&date={YYYY-MM-DD}` | Required selected station IDs and origin departure calendar date. | `ServiceSearchResponse { originStationId, destinationStationId, travelDate, services }`; empty `services` is valid no-match. Each choice includes service/route IDs/codes, revision, and planned origin/destination calls. | `200`: show results or empty state. `400`: keep criteria, show returned field errors only if the `errors` map identifies fields; otherwise general correction text. `503`: unavailable with Retry. `default`: safe unavailable/error with Retry. |
| `GET /api/services/{id}` | `serviceId` from selected search result. | `ServiceDetail { serviceId, serviceCode, route { routeId, routeCode, name }, schedule { revision, firstDepartureTimeLocal } }`. | `200`: render service/route identity and separately labeled planned first-call time. `400`: malformed path ID; show safe invalid/unavailable state for this section, preserve result summary and route stops, and provide Back (no retry of unchanged ID). `404`: mark service-detail section not found; keep result summary and route stops. `503`/`default`: mark section unavailable and allow retry of only this read. |
| `GET /api/routes/{id}/stops` | `routeId` from selected search result. | `RouteStops { routeId, routeCode, name, stops[] }`; each stop has `stopOrder`, `stationId`, `stationName`. Empty stops is successful and must be described as no published stop details. | `200`: show route identity and stops by `stopOrder`, or explicit empty-stop state. `400`: malformed path ID; show safe invalid/unavailable state for this section, preserve service detail and result summary, and provide Back (no retry of unchanged ID). `404`: mark route-stop section not found; keep service detail and result summary. `503`/`default`: mark section unavailable and allow retry of only this read. |

The operation response list does not explicitly enumerate `403`; however, SR-114/RBAC and the
clarified passenger spec require behavior if access-denied reaches the client. On 403, show
access unavailable, provide Back, and do not prompt login or offer retry. This describes
client behavior only and does not add a response or server authorization rule to the API.

## Shared Error Mapping

The `Problem` schema requires `type`, `title`, and integer `status`; `detail`, `instance`,
`code`, `errors`, and `traceId` are optional. Parse required fields as protocol input, but
do not require optional members. A malformed/non-Problem error body falls back to status
classification and generic passenger text. HTTP 400 uses criteria correction only for the
service-search operation. On the two detail operations, 400 represents a malformed path
identifier, so report the affected section as unavailable and provide Back rather than
asking the passenger to edit an internal identifier.

| Status/result | Classification | Retry policy | View behavior |
|---|---|---|---|
| 200 non-empty | success | None | Present only response fields. |
| 200 empty collection | empty success | Edit criteria / Back | Distinct from all failures. |
| 400 | validation/request input | Correct input; retry after edit | Preserve choices/date; no empty-result message. |
| 403 | access denied | No Retry, no login | Access unavailable + Back. |
| 404 | missing published resource | No blind retry | Mark affected detail section unavailable; keep successful sibling and result summary. |
| 500/default unexpected | server error | Retry failed read | Safe unavailable state; never expose internal details. |
| 503 | source unavailable | Retry failed read | Safe unavailable state. |
| Timeout/socket/DNS/no network | transport unavailable | Retry failed read | Safe unavailable state and preserve criteria/loaded data. |
| Invalid successful JSON/schema | protocol failure | Retry the read | Unavailable/protocol state; never coerce malformed data into empty results. |

For refresh failure with retained previously retrieved data, label the retained view “not
refreshed” and show client retrieval time if known. That timestamp is not the railway
source's update timestamp. Do not persist cached data across app restarts in this feature.

## Scheduling and Display Contract

- `ScheduledCall.scheduledAt` is an offset-bearing planned local schedule timestamp. Preserve
  raw wire text and derive display date/clock/offset from the literal value; never normalize
  to the phone's time zone or describe it as live/observed/estimated.
- Show a clear “next day” cue when a call's calendar date differs by a later day than the
  selected origin travel date. Keep full date and time visible as specified.
- `firstDepartureTimeLocal` from service detail is a planned first-call time only. Keep it
  distinct from the result's searched-origin call; do not claim equivalence unless values
  match.
- Route stop objects have order and station names but no scheduled time. Never synthesize
  intermediate times, train eligibility, service operating days, status, or disruptions.

## Concurrency, Retry, and Navigation

- Prevent/ignore duplicate Search submission while that criteria request is pending.
- Associate each request with the criteria or service context generation that started it.
  Ignore completion after criteria change, route pop, or owning state disposal. Aborting the
  HTTP request is an optimization; ignoring late completion is required even if abort fails.
- Keep Back available during all reads. Do not let a response navigate or restore a route
  after passenger departure.
- Service detail and route stops may load concurrently because both IDs are already in the
  search result. Store independent outcomes. A Retry on one failed section issues that
  operation only and retains the successful section.
- Retry after user action only; do not add automatic retries or new retry semantics.
