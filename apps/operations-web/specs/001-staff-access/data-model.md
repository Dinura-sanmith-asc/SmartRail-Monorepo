# Data Model: Staff Authentication & Role-Aware Operations Foundation

This client feature has no persistent database or new backend entity. The following are in-memory UI projections of the approved backend contract and OIDC session.

## StaffIdentity

| Field | Type | Source/validation | Use |
|---|---|---|---|
| `subject` | non-empty opaque string | Verified Cognito subject echoed by `GET /api/me`; runtime type/length check only, no UUID assumption | Session identity correlation in memory; not rendered unless a later approved UX requires it |
| `roles` | unique array of recognized labels | Current `GET /api/me` response: `Passenger`, `Driver`, `Station Staff`, `Controller`; validate against OpenAPI enum | Derive the presentation affordance set; never use as backend permission evidence |

The response has exactly `subject` and `roles` per the approved OpenAPI schema. An empty roles array is a valid authenticated identity with no operational workspace. An unrecognized role or malformed response yields no expanded access and is handled as invalid identity data, not coerced into a staff role.

## OidcSession (volatile)

| Field | Type | Lifetime | Constraint |
|---|---|---|---|
| OIDC user/access token | Library-owned memory object | Current page lifetime or earlier expiry/sign-out | No storage in browser persistence, URLs, logs, or cache |
| Access-token expiry | timestamp from validated OIDC token result | Same as token | At expiry, close protected UI and require managed-login reauthentication; no silent renewal requirement |
| OIDC transaction state | state, nonce, PKCE verifier and internal return path | One redirect round-trip in temporary per-tab state store | Library-generated and validated; never contains bearer tokens; clear after success/failure and reject mismatches |

The OIDC library's token-bearing user store must be explicitly configured for memory; do not accept library defaults that persist tokens to `sessionStorage` or `localStorage`. The temporary transaction store is distinct from token storage and exists only so the redirect callback can verify the authorization response.

## WorkspaceAccess (derived, not persisted)

| Derived value | Rule |
|---|---|
| `isController` | True only when current `/api/me.roles` includes `Controller` |
| `isStationStaff` | True only when current `/api/me.roles` includes `Station Staff` |
| visible staff affordances | Union of the corresponding Controller and Station Staff presentation mappings for recognized roles returned in the current identity response |
| station context | Not present in this model; `/api/me` has no station assignment fields |
| backend permission | Not represented or cached; only the backend authorization result for a specific protected operation/target is authoritative |

## ApplicationAccessState

Represent view lifecycle with a discriminated state: `unauthenticated`, `redirecting`, `callback-processing`, `identity-loading`, `authenticated`, `no-operational-access`, `reauthentication-required`, `forbidden`, `authority-unavailable`, `service-error`, `offline`, and `signing-out`. Transitions clear `StaffIdentity` and protected view data on sign-out, expiry, invalid credentials, or identity failure. A 503 must not transition to `no-operational-access`; only a successful 200 with an empty/nonstaff role array may do so.

## Relationships and State Transitions

`OidcSession` → authenticated bearer request to `GET /api/me` → validated `StaffIdentity` → derived `WorkspaceAccess` → route shell. Authentication identity and SmartRail roles remain separate: Cognito establishes the user; backend current role mapping supplies roles. A later protected endpoint independently checks its RBAC action and target scope. Client route selection does not establish resource access.

