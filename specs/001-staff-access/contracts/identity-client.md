# Identity Client Integration Mapping

This document maps the Operations Web client to the **approved** Backend Feature 002 contract. It is not a replacement or modification of the backend OpenAPI contract.

## Source and operation

- Backend specification: [`../SmartRail/specs/002-identity-permissions/spec.md`](../../../../SmartRail/specs/002-identity-permissions/spec.md), Approved for client integration 2026-10-19 by SmartRail Project Owner.
- Canonical contract: [`identity-permissions.openapi.yaml`](../../../../SmartRail/specs/002-identity-permissions/contracts/identity-permissions.openapi.yaml).
- Operation: `GET {configured SmartRail API base URL}/api/me`.
- Authentication: Cognito access token as `Authorization: Bearer <access-token>`.
- The Cognito sign-in, callback, and logout interactions use Cognito managed login; they are not SmartRail API operations.

## Successful response

```json
{
  "subject": "opaque-cognito-subject",
  "roles": ["Station Staff"]
}
```

Require an object with exactly `subject` and `roles`. `subject` is a non-empty opaque string. `roles` is an array of unique labels from `Passenger`, `Driver`, `Station Staff`, and `Controller`; an empty array is valid. There are no station assignments, scopes, profile fields, login endpoints, refresh endpoints, or administration endpoints in this identity contract.

## Failure mapping

| Status/outcome | Meaning | Client behavior |
|---|---|---|
| `401` | Missing or invalid/expired access token | Clear identity/token state, show safe reauthentication entry, do not replay a protected operation |
| `403` | Known insufficient permission/scope on a protected operation owned by another backend feature | Safe access-denied state; no protected result or success claim. `GET /api/me` itself does not define a 403 response. |
| `503` | Current role mapping or required authorization authority unavailable | Distinct unavailable state; do not treat it as empty roles; offer safe user-initiated retry where appropriate |
| timeout/network failure | No reliable response | Safe service/offline state; no access inferred; retry preserves the same auth checks |
| malformed 200 | Response does not satisfy the OpenAPI identity shape | Fail closed, clear any prior protected identity, show safe service/identity error; do not infer roles |

The client uses HTTP status for classification and does not display raw backend details. It may retain only safe trace/correlation values if separately approved; this feature does not log tokens, full claims, or raw response bodies.

## Configuration boundary

The API base URL is environment-specific and not hardcoded. Cognito issuer, managed-login domain, public SPA app-client ID, callback/logout URIs, and approved scopes are environment configuration and must be registered/approved by the backend/Cognito owner. Browser configuration is public and must contain no client secret. Do not use placeholders as live endpoints and do not claim production setup before actual environment validation.

## Authorization boundary

`GET /api/me.roles` supplies the current presentation role set. The backend's internal role mapping is authoritative; JWT group claims, route names, client state, or selected station values are not authority. The client sends no station-scope query to this identity operation because its contract has no scope field. Every operational API checks its own action and resource scope on the server. No new backend API is proposed.

