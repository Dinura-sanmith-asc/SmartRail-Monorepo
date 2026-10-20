# Local development data for Features 001–004

This manually invoked tool adds one versioned, synthetic demonstration dataset to the existing database. It never migrates at startup, truncates a table, deletes a record, resets a database, or registers an HTTP administration endpoint. Existing API DI and production maintenance defaults are unchanged. The tool is separate from the API/AppHost and is never started automatically.

## Safety and configuration

Run from the repository root. The executable requires `DOTNET_ENVIRONMENT=Development`; `ASPNETCORE_ENVIRONMENT`, if present, must also equal `Development`. It accepts only **Host=127.0.0.1, Port=5432, Database=smartrail_dev, Username=smartrail_dev**. `localhost`, test port 5433, alternate databases/users, multiple hosts and remote/RDS connections are rejected. It reads the API's existing Development User Secrets through the existing design-time factory; environment variables take precedence. Docker Compose `.env` does not configure .NET automatically.

Use `ConnectionStrings:SmartRailDbContext` in API User Secrets, with your existing password. If the current secret uses `localhost`, change only its host to `127.0.0.1` before seeding. Keep passwords out of command history and source control; use Visual Studio Manage User Secrets or your established secret-editing workflow. `Check` prints only the fixed accepted target and never opens a database connection. No connection-string values or provider exceptions are printed by the seeder.

The guarded PowerShell wrapper sets Development only when existing environment values are absent or already Development, then restores them. The seed itself additionally requires both explicit flags `--confirm-local-development` and `--enable-development-sample-categories`. No production category code has been approved by this work.

## Commands — run only after approval to change development data

Stop local API/Aspire writers during migration/seeding; do not stop or reset PostgreSQL. Back up valuable local data first. Use the existing Compose development service; no additional PostgreSQL database is created.

```powershell
# Read configuration and validate the exact target; no database connection/write.
.\dev\local-data\Invoke-DevSeed.ps1 -Action Check

# Apply only the existing reviewed migrations through Feature 004.
.\dev\local-data\Invoke-DevSeed.ps1 -Action Migrate

# Add/resume the development dataset; prints safe station/service/journey/incident IDs.
.\dev\local-data\Invoke-DevSeed.ps1 -Action Seed
```

The migration wrapper runs this existing EF command only after the exact-target check. The additive Controller overview schema change is migration `20261020094644_ControllerOperationsData`; review the migration before running the wrapper:

```powershell
dotnet ef database update 20261020094644_ControllerOperationsData --project src/SmartRail.Infrastructure --startup-project src/SmartRail.Api --context SmartRailDbContext
```

The guarded seeding command is:

```powershell
$env:DOTNET_ENVIRONMENT = 'Development'
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --project tools/SmartRail.DevSeeder -- --confirm-local-development --enable-development-sample-categories
```

The executable refuses pending migrations and never applies them. Existing migrations remain unchanged, and `20261020094644_ControllerOperationsData` is additive: `20261017145843_InitialRailwayNetwork`, `20261018005918_IdentityPermissions`, `20261019065713_JourneysStatus`, `20261019203203_IncidentManagement`, and `20261020094644_ControllerOperationsData`.

## Dataset manifest and expected effects

See [manifest.json](manifest.json). It is generated from the executable's single fixed manifest without reading configuration or contacting a database:

```powershell
dotnet run --project tools/SmartRail.DevSeeder -- --manifest
```

Source naming/topology follows the existing coastal reference fixture (Colombo Fort → Galle), extended to Matara with clearly marked development sample names. Times, services, dates, platform and incidents are **synthetic**, not verified real-world railway operating information. Reserved IDs start with `d0040000-`; source keys/codes use `DEV-SEED` and provenance `smartrail-dev-v1`.

| Record | Expected new count on a fresh dataset |
|---|---:|
| Stations / import runs / station-change audit | 3 / 1 / 3 |
| Routes / ordered route stops | 3 / 7 |
| Services / published schedule revisions / calling points | 7 / 7 / 19 |
| Complete date manifests / service-operation-date rows | 3 / 17 |
| Role assignments / role audit events | 4 / 4 |
| Station-scope assignments / scope audit events | 2 / 2 |
| Journeys / journey revisions / revision stops / initialization audits | 8 / 14 / 40 / 8 |
| Incidents / immutable revisions / associations / idempotency records | 4 / 10 / 15 / 10 |

Counts describe additions without prior matching data. Existing active manual-auth role/scope mappings are reused, so fewer assignments may be added. Unrelated records are preserved. Both **2026-11-01 and 2026-11-02** have complete synthetic timetable evidence for all five seed schedules. Journeys use operation date 2026-11-01:

| Service | Departure Asia/Colombo | Journey scenario / revisions |
|---|---|---|
| DEV-SEED-001 | 08:00 | Scheduled / 1 |
| DEV-SEED-002 | 10:00 | Running / 2 |
| DEV-SEED-003 | 12:00 | Delayed, platform DEV-2 and departure +15 minutes / 3 |
| DEV-SEED-004 | 14:00 | Cancelled / 2 |
| DEV-SEED-005 | 23:20 | Scheduled overnight / 1 |

An additional completed journey (`d0040000-0000-0000-0005-000000000005`) reuses the published DEV-SEED-001 schedule on operation date 2026-11-02. These records and times are synthetic development examples. The seed does not assign official train names or official public numbers; optional service identity fields remain null unless verified data is deliberately supplied.

Calling-point offsets from first departure are: Colombo departure 0; Galle arrival +120, departure +125; Matara arrival +180 minutes. For the overnight service, Galle departure and Matara arrival fall on November 2. Search travel date is departure date at the selected origin; the journey's operation date stays November 1. Searching Galle → Matara on November 2 also includes the November 1 overnight occurrence; station departures likewise filter by station-local departure date.

Roles use the existing local issuer subjects: `smartrail-manual-controller`, `smartrail-manual-station-staff`, `smartrail-manual-driver`, and `smartrail-manual-passenger`. Station Staff receives the Colombo and Galle seed-station scopes, excluding Matara. There are no credentials or JWTs in the dataset and no driver-assignment provider is invented. Driver-protected owner workflows remain fail-closed until Driver Operations exists.

Incidents use only `DEV-SAMPLE-OPERATIONAL` in the seeder's isolated service provider. Examples are Reported (Colombo), In Progress (Colombo + Galle, Running + Delayed journeys and the exact published Delayed UpdateId/revision), Resolved (Galle), and Closed (Matara). Each uses verified service/journey links. Normal forward lifecycle commands produce all 10 audit revisions; incident creation IDs and audit/publication IDs are server-generated once, then reused on reruns through stable source/idempotency keys. Controller sees all four; seed Station Staff sees the first three and cannot access the Matara-only closed incident. No incident mutation automatically publishes a journey status.

## Repeatability and failure behavior

Station import, timetable publication, identity grants, journey initialization/publication and incident commands use the existing owner workflows. The controlled initial route/service graph uses Domain constructors and EF persistence because Feature 001 exposes no route/service creation command; it is committed atomically, then the approved timetable publication handler validates/publishes it. There are no direct SQL data inserts.

Each approved command is transactional, including its audit/idempotency rows. The **entire seed is not one global transaction**: successful earlier steps remain after a later failure, and a rerun resumes them. Failure-injection tests prove no partial incident/audit/idempotency write survives a failed command. A PostgreSQL session advisory lock prevents simultaneous seeder runs. The lock is not a substitute for pausing ordinary API writers.

Reserved ID/source-key collisions, changed fixed reference data, unrelated date publications, invalid relationships or unavailable authority stop the run. Existing source/date publications are never overwritten. Existing manually advanced operational records are not reset. A manually revoked seed role/scope is not silently restored. Review the conflict and keep existing data; do not delete records to force a seed. Changing the dataset requires a new reviewed version or deliberate reconciliation.

## Swagger and real local JWTs

Follow [manual authentication README](../manual-auth/README.md) for the existing loopback-only RS256 issuer and local issuer/client configuration. No AWS Cognito setup is needed. For standalone ports:

```powershell
.\dev\manual-auth\Configure-ManualAuth.ps1
dotnet run --project tools/SmartRail.LocalAuthIssuer
```

In a second PowerShell window, start the API. For read-only exploration, normal Development startup needs no sample catalogue at all. To manually exercise incident POST with the sample category, use this explicit process-local configuration wrapper:

```powershell
.\dev\local-data\Start-SeededApi.ps1 -EnableDevelopmentSampleCategory
```

The wrapper sets `IncidentManagement__CategoryCatalog__Codes__0` only for that Development process and restores the shell on exit; it never writes appsettings/User Secrets. Do not set persistent sample categories, use this wrapper for Production, or carry the variable to AWS. Normal production configuration remains empty/fail-closed. Ensure you have no persistent category catalogue already configured; the sample wrapper refuses existing shell catalogue overrides. When using Aspire, stop its API before starting the standalone API wrapper, or use normal Aspire startup for seeded read-only checks. Do not mix issuer URLs: standalone uses issuer 5107/API 5236; Aspire uses issuer 5108/API 5237.

Open `http://localhost:5236/swagger`. Obtain a five-minute signed token from `http://127.0.0.1:5107/issue/Controller` (or StationStaff, Driver, Passenger). Paste only `access_token` into Swagger Authorize, then clear it after testing. Real RSA signature, issuer, client ID, token use and expiry validation remain active. Tokens carry no SmartRail roles; active PostgreSQL mappings authorize requests. Keep tokens out of logs, screenshots, saved collections and Git.

## Read-only verification

Use manifest IDs and the seed command's printed incident IDs. No token is required for stations, service search, journey detail/updates or station departures. Use the Controller token for incidents and `/api/me`:

```powershell
$api = 'http://localhost:5236'
$manifest = Get-Content .\dev\local-data\manifest.json -Raw | ConvertFrom-Json
$origin = $manifest.stations[0].id
$destination = $manifest.stations[2].id
Invoke-RestMethod "$api/api/stations"
Invoke-RestMethod "$api/api/routes/$($manifest.route.id)/stops"
Invoke-RestMethod "$api/api/services?origin=$origin&destination=$destination&date=2026-11-01"
Invoke-RestMethod "$api/api/services/$($manifest.services[0].serviceId)"
Invoke-RestMethod "$api/api/journeys/$($manifest.services[2].journeyInstanceId)"
Invoke-RestMethod "$api/api/journeys/$($manifest.services[2].journeyInstanceId)/updates"
# Check the approved OpenAPI query name: date (station-local Asia/Colombo date).
Invoke-RestMethod "$api/api/stations/$origin/departures?date=2026-11-01"
$jwt = (Invoke-RestMethod 'http://127.0.0.1:5107/issue/Controller').access_token
Invoke-RestMethod "$api/api/me" -Headers @{ Authorization = "Bearer $jwt" }
Invoke-RestMethod "$api/api/incidents" -Headers @{ Authorization = "Bearer $jwt" }
Invoke-RestMethod "$api/api/operations/overview?date=2026-11-01" -Headers @{ Authorization = "Bearer $jwt" }
Invoke-RestMethod "$api/api/operations/overview?date=2026-11-02" -Headers @{ Authorization = "Bearer $jwt" }
Remove-Variable jwt
```

`GET /api/operations/overview` is Controller-global or Station Staff station-scoped (`&stationId=<current-assigned-station-uuid>`). The response fields, metric definitions, Asia/Colombo operation-date semantics and unavailable-value behavior are documented in [operations-overview-api.md](../../docs/operations-overview-api.md). Open incidents count current Reported and In Progress records regardless of selected journey date. The API never substitutes a GUID or synthetic value for an official public service number.

Repeat with StationStaff: three seed incidents are visible; filtering by the Matara Closed incident yields no authorized match. Driver/Passenger incident reads are denied. Missing/expired credentials remain 401. Query additional records carefully: unrelated pre-existing data can legitimately appear alongside this dataset.

## Automated verification boundary

The executable rejects port 5433. Only the integration-test assembly can invoke its internal engine against a verified `smartrail_it_*` disposable database on loopback port 5433. Tests cover repeat counts/IDs, unrelated data preservation, references, overnight dates, lifecycle/audit continuity, failure rollback/resumption, revoked-role preservation, production execution refusal, wrong targets and unchanged default-deny/category composition. Tests never connect to `smartrail_dev`.

This tool does not close production category approval, Operations overview/incident composition, client acceptance or idempotency-retention gates.

## Verification performed (2026-10-20)

- Started from clean branch `feature-04-incident-management`, HEAD `ec248a5253375c8911ab4eaeabf7e50aab3fabd8`.
- Full solution build passed with isolated build output, 0 errors and the existing AppHost MessagePack `NU1902` warning. Isolated output avoided the executable locked by the already-running Aspire session; that session was left running.
- `dotnet test SmartRail.slnx --no-build` from normal repository test output passed: **187 unit + 141 integration = 328**, 0 failed and 0 skipped. Integration coverage includes **16 development-seeder tests**. Only disposable `smartrail_it_*` databases on port 5433 were migrated/seeded by the test harness.
- Existing contract tests require normal repository output to discover their checked-in contract files; do not use the temporary build-output directory for the full test run. The test projects were built in their normal output before the successful run.
- Both new PowerShell scripts parsed successfully; manifest export parsed as valid JSON; whitespace checks passed.
- The configuration-only `Check` correctly refused the current host setting because it is not the literal `127.0.0.1`. Port, database, username and schema checks passed. No connection was opened. Update only the host in local configuration before requesting migration/seeding approval; no secret was edited automatically.
- No migration or seed was executed against `smartrail_dev`; no AWS operation or change to prior migrations/API/business logic was made.

## Colombo Fort and Galle departure examples

The manifest now also includes two synthetic, development-only dated journeys attached to the existing published reference station records, not the separately named sample stations:

- Colombo Fort (`10000000-0000-0000-0000-000000000001`): service `DEV-SEED-REF-001`, departure 08:30 Asia/Colombo on 2026-11-03.
- Galle (`10000000-0000-0000-0000-000000000002`): service `DEV-SEED-REF-002`, departure 15:10 Asia/Colombo on 2026-11-03.

These are synthetic test times, not a real railway timetable. The two services run in opposite directions and each has one verified published calling point at the other station 130 minutes later. The seeder refuses to continue if the exact published Colombo Fort/Galle reference stations are unavailable or their reserved date already contains unrelated data. No migration is run by the seed command.

After seeding, check the existing read APIs anonymously:

```powershell
$api = 'http://localhost:5236'
Invoke-RestMethod "$api/api/stations/10000000-0000-0000-0000-000000000001/departures?date=2026-11-03"
Invoke-RestMethod "$api/api/stations/10000000-0000-0000-0000-000000000002/departures?date=2026-11-03"
Invoke-RestMethod "$api/api/services?origin=10000000-0000-0000-0000-000000000001&destination=10000000-0000-0000-0000-000000000002&date=2026-11-03"
```

The station-departure response should include the synthetic service for that station and date. The service search should return the Colombo Fort to Galle instance. Existing rows are preserved; unrelated departures may also appear if already present in the local database.
