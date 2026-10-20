# Driver Operations ? Local Verification

## Prerequisites
Use the isolated integrated Feature 005 worktree. PostgreSQL test service must be healthy on loopback port 5433; existing test fixture guards must reject development/production targets. Configure the existing secret connection mechanism without printing credentials. Never apply this feature migration to port 5432 as part of this run. No AWS commands.

## Build and automated verification
```powershell
dotnet restore SmartRail.slnx
dotnet build SmartRail.slnx
dotnet test SmartRail.slnx
git diff --check
```
The integration harness applies migrations to disposable test databases and seeds genuine approved owner data. Test authorities are isolated; normal maintenance stays deny-by-default. Do not silently skip failed database tests. Record final exact test counts/commit in the integration verification record.

## Signed JWT/API scenarios
Reuse the existing isolated local OIDC/JWKS WebApplicationFactory, real RSA signatures and PostgreSQL current roles. Initialize two real owner journeys and two assignments only through controlled setup. Verify:
1. Driver A `GET /api/driver/assignments` returns A's current duties with stops/timings/current revisions; Driver B is isolated; Controller sees both; missing token is 401.
2. Driver A posts `{requestId,text}` to own journey observations; result 201, server author/time/revision. Same authorized request replay creates no duplicate, changed payload 409, invalid body 400, other driver/Passenger/StationStaff denied.
3. Controller `GET /api/operations/journeys/{id}/observations` returns authored evidence; no review grant denied. Unavailable roles/owner returns 503 without writes.
4. Controller Feature 003 publication may reference valid same-journey observation; foreign, revoked or unverifiable evidence fails closed. Submission alone leaves official status/revisions unchanged.
5. Revoke Driver role/assignment and retry: denied on next request. Publish central status and reread assignments: new central revision visible. Preserve overnight instants/date.
6. Generated OpenAPI has only approved three additional paths; Features 001?004 regression suite remains green. Logs/errors/public projections contain no protected observation data or credentials.

## Manual testing boundary
Use existing `dev/manual-auth/README.md` and approved local issuer for real signed tokens; no new authentication bypass. Production Cognito remains authority for real sessions. Assignment-maintenance operations have no HTTP endpoint and remain production-default-denied; fixture provisioning must not become a public route. This guide does not claim Flutter refresh, storage or offline UI acceptance. All existing Feature 004 production/client gates remain open.
