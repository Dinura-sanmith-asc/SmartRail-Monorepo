# Incident Authorization and Ownership Contract

## Protected operations and unchanged grants

All Incident Management operations require a valid Feature 002 identity and the current approved action grant:

| Operation | Existing action | Controller | Station Staff |
|---|---|---|---|
| `GET /api/incidents` | `ViewIncidents` | Global incident scope | Current scope must cover every station attached to each returned incident |
| `POST /api/incidents` | `CreateIncident` | Global incident scope | At least one station association must be in current station scope; a service-only report does not supply station scope |
| `PATCH /api/incidents/{id}` | `UpdateIncidentLifecycle` | Global incident scope | Current scope must cover every station associated with the incident |

These are existing grants, not new grants. Controller global scope is explicitly approved for Feature 004 and does not remove authentication, current role lookup, action grant, target/reference validation, or fail-closed requirements. Other roles receive only their existing matrix grants.

## Per-request decision process

1. Feature 002 validates the Cognito token and provides the verified subject. Never accept caller-supplied role, actor or scope fields.
2. Resolve current active role mapping and action grants from the authoritative Feature 002 store for every protected request. Missing/revoked role denies; unavailable mapping returns safe 503.
3. Resolve target incident existence and owner references. Unknown or inaccessible resources map to 404; required owner unavailability maps to 503.
4. For Controller, a current explicit grant permits global incident scope. It does not bypass role resolution or target validation.
5. For Station Staff, use Feature 004's complete active station-association set. For view/update, authorize every associated station against Feature 002's existing current station-scope check. Any verified missing station grant denies the whole item/request; any unverifiable scope returns unavailable and no protected write. Empty station set provides no Station Staff scope. Journey links never imply station access.
6. For Station Staff creation, require at least one reported station in current scope; a service-only report does not establish station scope. Verify every supplied station/service reference with its owner. If multiple stations are linked, subsequent read/update requires scope over all of them, so the creator may not retain access unless all are in scope. Controller creation remains under the current global incident grant.
7. Recheck authorization within the command's mutation boundary as needed to avoid using stale scope snapshots; do not cache decisions across requests.

## Existing Feature 002 interface constraint

Feature 002 `IIncidentResourceEvidence.ResolveIncidentAsync` returns one `StationId` and one optional `JourneyId`. That evidence is insufficient to establish the Feature 004 all-associated-stations rule by itself. Feature 004 must authorize each station association through the existing Feature 002 station-scope path. It may implement the existing evidence adapter to establish incident existence for the Feature 002 boundary, but must not treat a single `StationId` result as proof of all required scopes. Do not alter Feature 002 contracts or its grants.

## Owner verification

- Feature 001 verifies station and scheduled service IDs and current reference validity.
- Feature 003 verifies `JourneyInstanceId`, active status when a new link is created, and the exact published disruption `UpdateId`/revision when supplied.
- Incident Management stores association identifiers and historical link state only; it does not copy owner state or issue Feature 003 publication commands.
- Missing/inaccessible owner resource maps to 404. Unavailable required owner verification maps to 503 and the transaction persists no incident/link.
- Existing links remain historical if a journey becomes terminal or a disruption is superseded; no automatic association deletion or journey publication occurs.
- No production fake provider is permitted. Test doubles are confined to test projects.

## Public/private boundary

Incident APIs are protected. They exclude reporter and actor subjects, raw authorization evidence, JWT/group claims, credentials and private owner data. Feature 003 public journey/update/departure contracts remain unchanged and contain no incident record or private incident context. Operations composes its authorized incident context with official Feature 003 data under its own approved operation boundary.
