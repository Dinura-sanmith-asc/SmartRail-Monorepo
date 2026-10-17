# Authorization Boundary Contract

This document defines the Feature 002 interface obligations for authorization consumers. It does not add HTTP endpoints or change the approved RBAC grants.

## Decision inputs and outcomes

An authorization evaluation receives the authenticated subject from the validated Cognito access token, an exact approved RBAC action identifier, and the target resource context required by the owning module. It returns one of:

- `Allowed`: the active internal role mapping includes an explicit matrix grant and every applicable current scope/ownership check succeeds.
- `Denied`: the mapping is verified but no grant applies, or authoritative evidence confirms the caller is out of scope / not the owner.
- `Unavailable`: the active role mapping or required owning-module evidence cannot be verified. The protected operation receives no grant; API behavior is a safe 503 when the authority is unavailable.

No boolean or missing-value fallback may turn an unavailable lookup into `Allowed`. A denied or unavailable decision must be evaluated before protected state changes.

## Identity and role inputs

1. API validates Cognito access token signature and key, issuer, lifetime, `token_use=access`, and allowlisted human `client_id`; validate resource audience when configured/present.
2. The validated `sub` is the only identity key and is treated as opaque text.
3. Resolve active roles from Identity's PostgreSQL mapping at each protected authorization check. Do not cache roles or accept `cognito:groups`, client-supplied role values, or scope claims as grants.
4. Combine only explicit grants for all currently active recognized roles in the approved matrix. Every “own” or “scoped” grant also requires its owner-module evidence.
5. A valid principal with no active role mapping is authenticated but has no protected grant. `/api/me` returns an empty role array only after a successful mapping lookup. A failed mapping lookup is unavailable, not empty.

## Resource evidence ownership

| Resource check | Authoritative owner | Required evidence | Missing, stale, revoked, or unverifiable |
|---|---|---|---|
| Station Staff to station | Identity & Permissions | Current active subject-to-station scope assignment | Deny; unavailable authority yields `Unavailable` |
| Driver to journey | Driver Operations | Current assignment for this verified driver and journey | Deny; unavailable authority yields `Unavailable` |
| Station incident to station/journey | Incident Management owns incident relationship; Identity supplies station scope | Current incident relationship plus current station scope for scoped staff | Deny; unavailable authority yields `Unavailable` |
| Passenger to catering order | Catering Adapter | Current order-owner association matches verified subject | Deny; unavailable authority yields `Unavailable` |

Identity does not store driver assignments, incidents, or catering orders. Each module exposes an Application-level evidence port that it implements using its own rules and persistence. No direct cross-module table access is permitted.

## HTTP mapping

- Missing, malformed, expired, invalid-signature, wrong-issuer, wrong-token-use or disallowed-client credential on a protected endpoint: `401`.
- Valid identity with verified but insufficient role/action/scope/ownership: `403`.
- Current role map or required authority unavailable: `503`, with no protected effect.
- `GET /api/me`: `200 { subject, roles }` for verified mapping, including `roles: []`; `401` for invalid/missing token; safe `503` if role mapping cannot be verified.
- Feature 001 public GET routes remain explicitly anonymous. Optional malformed/expired bearer credentials are ignored there; existing response contracts and public projections are unchanged.
- Use generic RFC 9457 Problem Details. Never return raw tokens, claims, passwords, signing keys, SQL errors or sensitive resource-existence details.

## Unresolved operational gates

Production use requires explicit approval of the role assignment/revocation operators and bootstrap process; station-scope assignment operators; audit retention and access policy; and per-environment Cognito issuer/client/audience/expiry configuration. These gates do not authorize public administration APIs. Controlled isolated fixtures may be used for automated tests.
