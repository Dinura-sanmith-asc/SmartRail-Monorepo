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

## Final execution evidence — 2026-10-20

Verified implementation commit: `5ae2941c312434b4c6618c804057ec1c30676165` on `codex/f005-integration`. The final documentation commit records completion after this source revision passed. All 29 tasks are complete for the backend scope; downstream acceptance below remains OPEN.

| Check | Actual result |
|---|---|
| `dotnet restore SmartRail.slnx` | PASS |
| `dotnet build SmartRail.slnx --no-restore` | PASS, zero errors; one pre-existing AppHost NU1902 warning |
| `dotnet test SmartRail.slnx --no-build` | PASS: 205 unit + 174 integration tests, zero failures, zero skipped |
| New Driver coverage | 18 unit + 20 integration-project cases, including real PostgreSQL and cryptographically signed JWT HTTP tests |
| PostgreSQL migration | `20261020031244_DriverOperations` applied by fixtures only to disposable `smartrail_it_*` databases on loopback5433 |
| OpenAPI | YAML syntax and local/external references parsed; generated route/security/required-field/UUID/length/revision/owner-schema assertions passed |
| Git/architecture | Baseline-to-HEAD diff check passed; previous migrations, RBAC catalogue, authentication, project dependencies, configuration and AWS files unchanged |

TRX evidence is generated locally under the two existing test projects' ignored `TestResults/` directories with prefix `f005-complete`. It is intentionally not committed: signed-token test execution output is not a distributable source artifact. Counts above are from the completed run, not inferred from task checkboxes.

### Source acceptance evidence
All test paths below are under `tests/`.

| Source requirement | Backend implementation and executed evidence | Remaining acceptance |
|---|---|---|
| SR-064 | `DriverOperationsApiTests`: signed JWT, own duties, cross-driver/role denial, invalid/expired credentials and current role revocation. `DriverCurrentJourneyTests`: Controller/multiple-role collection and revoked/empty assignments. | Driver application's sign-in/session UI belongs to its client feature. |
| SR-065 | `DriverCurrentJourneyTests.NextDriverReadReflectsGenuineCentralStatusPlatformChangesAndOvernightInstants`: genuine ordered stops, scheduled times, journey identity and overnight calendar rollover preserved. | Driver client duty presentation remains open. |
| SR-066 | Same genuine HTTP test: Controller status/platform publication is reflected on next assignment read with revision3 and explicit safe update fields. | Full driver-client refresh acceptance OPEN; no backend-only claim of complete UI acceptance. |
| SR-069 | `DriverOperationsApiTests`: authored/UTC observations, protected Controller review, current reauthorization, duplicate/conflicting/concurrent retries, valid Feature003 reference, missing/foreign/revoked evidence rejection and no automatic official publication. `DriverOwnerAdapterTests`: unavailable/missing revision/ownership fail closed. | Driver/Control client presentation and complete cross-client E2E remain open. |
| SR-072 | Exact OpenAPI route inventory; Driver cannot publish official status; observation submission leaves official revisions unchanged. No driving-control operation was added. | Release-wide endpoint inventory must continue preserving this exclusion. |

`DriverOperationsPersistenceTests` additionally verify additive migration, active-assignment uniqueness, restrictive FKs, actual SQL failure rollback, immutable observation/audit history and regrant history. `DriverRevocationWriteTests` verifies a submission constructed before revocation cannot persist after the original assignment is revoked. Assignment writes recheck and lock that lifecycle transactionally; unique request conflicts detach failed entries before replay lookup. `DriverCurrentJourneyTests` proves normal DI denies maintenance with no writes and isolated test authority alone enables audited, repeatable grant/revoke. Test authorities and real RSA issuer fixtures do not activate in normal application registration.

### Analysis and review resolutions
Independent cross-artifact and code reviews found full FR-001–FR-011 / SC-001–SC-006 backend task coverage. The identified assignment-revocation race was fixed with a transaction and assignment-row lock/recheck; failed insert tracking was detached. Concrete Feature003 safe response DTOs and schema references replaced unrestricted owner objects, fixing otherwise empty polymorphic change serialization. Entity identity names were aligned. Strict OpenAPI and final-write revocation regression tests passed. An older Feature003 test now correctly expects404 for a missing genuine observation instead of503 from the unavailable-module placeholder; privacy assertions remain intact. No unresolved CRITICAL/HIGH implementation defect was found.

### Open gates and local recommendation
**GO for local backend review/integration.** No merge to the original feature branch, push, development-database migration, production assignment provisioning or AWS operation occurred.

The following remain OPEN: trusted production assignment-maintenance operator/procedure, retention approval, Flutter Driver refresh/duty UI, credential storage/session expiry and loading/no-data/denial/error/retry/no-network/freshness states (shared SR-093/SR-109/SR-114), and full release client E2E evidence. Existing Feature003/004 notification/Operations composition, production category and client gates are preserved. These are not falsely marked accepted by backend completion.

The existing AppHost transitive `MessagePack 2.5.302` NU1902 advisory (`GHSA-qhrr-8q5h-9q3h`, moderate) is still reported by restore/build; no warning was suppressed or unrelated dependency upgraded. It remains a separate dependency-maintenance issue.