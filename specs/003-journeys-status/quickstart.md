# Journeys & Status Local Development Quickstart

This is a future implementation verification guide. It does not create code, migrations, data, or AWS resources. Use only the local Docker PostgreSQL services and isolated test database.

## Prerequisites

- Windows PowerShell from the repository root.
- .NET SDK compatible with the repository's `net10.0` projects.
- Docker Desktop and Docker Compose.
- Local development connection configured in API Development User Secrets; do not place credentials in this document or Git.
- `SMART_RAIL_TEST_ADMIN_CONNECTION` set in the current test shell to the dedicated Compose test host on loopback port 5433, database `postgres`, user `smartrail_test_admin`. The integration fixture rejects other targets and creates a disposable database per fixture.
- Local auth tests use the existing real-signature OIDC/JWKS fixtures under `tests/SmartRail.IntegrationTests/Identity`; do not configure a bypass or production auth exception.

## Start local PostgreSQL

```powershell
docker compose up -d postgres postgres-test
docker compose ps
```

Wait for both health checks to report healthy. The development service binds to `127.0.0.1:5432` / `smartrail_dev`; the test service binds to `127.0.0.1:5433`. Journeys integration tests must use only the latter.

Set the integration connection string without printing it. Use the repository's existing local secret/password setup; do not copy a production or RDS connection string:

```powershell
$env:SMART_RAIL_TEST_ADMIN_CONNECTION = Read-Host 'Enter local port-5433 test admin connection string'
```

Close the shell after tests to clear the variable. Do not use `Write-Host`, transcript logging, or command history to reveal credentials.

## Restore, build and test

```powershell
dotnet restore SmartRail.slnx
dotnet build SmartRail.slnx --no-restore
dotnet test SmartRail.slnx --no-build
```

The PostgreSQL fixture creates and drops an isolated database. A missing test service or connection is a failure to resolve, not a passing/skipped verification. Never point the test admin variable at `smartrail_dev` or AWS/RDS.

## Feature test evidence expected

- Domain/Application tests: approved status transition graph; terminal correction; role/scope denial; initialization validation and idempotency decisions; ambiguity is never auto-selected.
- PostgreSQL integration tests: additive migration, schedule/station FK behavior, unique source idempotency, distinct IDs sharing the composite, conflict audit, append-only revisions with one `UpdateId` per official publication, same-journey correction references, atomic stop snapshots, rollback, and one-writer/one-409 concurrent updates.
- API tests: exactly five approved endpoints; `date` filtering by station-local scheduled departure date in Asia/Colombo while preserving operationDate; overnight and diverted-stop cases with no invented times; complete ascending public update history excluding initialization; stable stop IDs and rejection of duplicate/foreign IDs; current PostgreSQL Controller role/grant checks on each protected operation, approved global Controller journey scope, separate cancellation grant, and Driver/Station Staff denial; observation verification 400/404/409/503 plus existing 401/403; no private evidence leakage; same revision in consistent reads; F001 and F002 regression suite.
- Significant-change evidence tests: publish cancellation and platform changes separately, then verify existing journey detail/update/station-departure reads expose the correct change type, immutable `UpdateId`, revision, safe source, and publication timestamp for each change. No notification endpoint or delivery service is introduced.
- Test fixtures may invoke the internal initializer under a deterministic test authority. No public initialization route or Development/Production authentication bypass is allowed.

## OpenAPI and contract review

After implementation, run the API in a local Development environment using the existing launch profile and local User Secrets. The built-in document is exposed at `/openapi/v1.json` in Development. Compare the generated document to [contracts/journeys-status.openapi.yaml](contracts/journeys-status.openapi.yaml): exactly the five paths, public-read security behavior, bearer protection on two PATCH operations, expected revision, response schemas, and safe errors. Do not expose the local API or test issuer outside loopback.

## Migration procedure (implementation phase only)

The migration has not been generated or applied by this plan. When its task is authorized, add the EF migration named `JourneysStatus` with EF-generated ID, inspect the generated Up/Down operations and shared snapshot, then apply it first to an isolated port-5433 test database. Do not edit prior migration files. Apply to `smartrail_dev` only under a separately approved local verification step; never run EF commands against AWS/RDS as part of Feature 003.

## Approved contract details

The six contract decisions approved on 2026-10-19 are synchronized in the spec and OpenAPI: station-local date filtering, update-history content/order, stable diverted-stop identity, closed public metadata, Driver observation failure behavior, and one `UpdateId` stored with each official publication revision. The approved Controller scope clarification uses Feature 002's existing global action grants; no Controller-to-journey assignment is created, and cancellation still checks its separate grant. No arbitrary pagination cap is added. Production initialization authority and audit governance remain approval gates; test initialization is limited to isolated fixtures.

## Downstream acceptance evidence (open)

- **FR-013 / SR-025 / SC-007:** Feature 003's implementation and integration tests prove cancellation/platform change evidence is available through existing reads with matching `UpdateId`, revision, source, and publication timestamp. Passenger-client specifications must own alert delivery/presentation tasks and tests; full SR-025/SC-007 acceptance stays open until an in-app alert and visible status are verified against the same revision.
- **SR-044 / FR-015:** Incident Management must provide the authorized affected-journey context read and preserve association ownership. The operations client must compose that context with official Feature 003 journey data. Full SR-044 acceptance stays open until both owner-module and client evidence exist. Public `JourneySnapshot` remains free of incident records.
- Documenting these obligations is not implementation evidence and does not close either acceptance item.

## Verified backend requirement-to-test evidence

The Feature 003 unit, PostgreSQL, and HTTP tests below are the backend evidence for this local implementation. The tests use the isolated PostgreSQL fixture on loopback port 5433. This matrix does not claim client-owned or unfinished-owner-module acceptance.

| Approved source / specification outcomes | Automated evidence | Acceptance state |
|---|---|---|
| SR-007; FR-001â€“FR-002; SC-001, SC-011 | `JourneyFoundationTests`, `JourneyPersistenceTests`, `JourneySelectionFlowTests` verify opaque instance IDs, same-occurrence instances, idempotent replay/conflicts, and passenger selection/ambiguity. | Backend verified. |
| SR-010, SR-048; FR-011; SC-003, SC-013 | `JourneyProjectionConsistencyTests`, `JourneyDepartureAndDiversionTests`, `JourneyReadApiTests`, `JourneySelectionFlowTests` verify consistent committed revision/source/time across dependent reads. | Backend verified. |
| SR-019, SR-021, SR-035; FR-003, FR-009; SC-004 | `GetJourneyHandlerTests`, `JourneyReadApiTests`, and `PublishJourneyStatusHandlerTests` verify planned/operational distinction and unknown ETA without fabricated values. | Backend verified; display behavior remains client-owned. |
| SR-022; FR-010, FR-022; SC-013, SC-017 | `PublishJourneyDiversionTests`, `JourneyDepartureAndDiversionTests`, and `JourneySelectionFlowTests` verify journey-only diversions, retained/new stop IDs, prior revisions, and rejection without partial writes. | Backend verified. |
| SR-025; FR-013; SC-007 | `JourneyReadApiTests` and T062 in `JourneySelectionFlowTests` verify separate cancellation and platform evidence by change type, UpdateId, revision, source, and publication time across existing reads. | Backend verified. Passenger in-app delivery/presentation remains open. |
| SR-031, SR-034, SR-040; FR-004â€“FR-008, FR-017, FR-021; SC-002, SC-006, SC-012, SC-016 | `PublishJourneyStatusHandlerTests`, `JourneyPublicationApiTests`, and `JourneyPublicationTests` verify lifecycle, Controller authorization, separate cancellation grant, actor/time/audit, corrections, and concurrency. | Backend verified. |
| SR-044; FR-015; SC-005, SC-014 | `ModuleBoundaryTests`, `IncidentContextBoundaryTests`, and `ModuleBoundaryIntegrationTests` verify owner separation and unavailable behavior. | Feature boundary verified. Incident Management provider and operations-client composition remain open. |
| SR-046, SR-091; FR-014; SC-006, SC-008 | `AuthorizationRevocationIntegrationTests`, `JourneyPublicationApiTests`, and `ExistingFeatureApiRegressionTests` verify current role checks, revocation, global Controller scope under the approved grant, separate cancellation authorization, Driver/Station Staff denial, and fail-closed unavailable evidence. | Backend verified; Feature 002 grants unchanged. |
| SR-054, SR-056; FR-012, FR-020; SC-015 | `GetStationDeparturesHandlerTests`, `JourneyDepartureAndDiversionTests`, and `JourneySelectionFlowTests` verify Asia/Colombo station-local date filtering, overnight operationDate preservation, required date validation, and no invented departure times. | Backend verified. Station-display refresh acceptance remains open. |
| SR-058 | `JourneyProjectionConsistencyTests`, `JourneyDepartureAndDiversionTests`, and `JourneySelectionFlowTests` verify backend revision consistency. | Backend verified; station-display refresh remains open until its client implementation and tests exist. |
| SR-069; FR-008, FR-023; SC-005, SC-014, SC-018 | `PublishJourneyStatusHandlerTests`, `JourneyPublicationTests`, `JourneyPublicationApiTests`, and `ModuleBoundaryIntegrationTests` verify observation ownership, association/error mapping, and no publication on failed/unavailable verification. | Feature boundary verified; genuine Driver Operations provider remains unavailable until its owner module exists. |
| SR-089, SR-090, SR-091; FR-014, FR-018; SC-006, SC-008 | `ExistingFeatureApiRegressionTests`, `IdentityOpenApiTests`, and `JourneysStatusOpenApiTests` verify anonymous public reads, protected writes, and unchanged authentication boundaries. | Backend verified. |
| FR-016, FR-018â€“FR-019; SC-009â€“SC-010 | `JourneyPublicationApiTests`, `JourneyReadApiTests`, `JourneysStatusOpenApiTests`, and final diff review verify safe errors, exactly five Feature 003 operations, no additional administration route, and scope/security checks. | Backend verified by the final local run; train-control functionality is excluded. |

The complete specification crosswalk is: FR-001â€“FR-002 / SC-001 and SC-011 (identity/initialization); FR-003â€“FR-009 / SC-002, SC-004â€“SC-006 and SC-012, SC-014 (status, authorization, source/ETA, observations); FR-010â€“FR-012 / SC-003 and SC-013 (diversion and consistent projections); FR-013 / SC-007 (significant-change evidence; client alert remains open); FR-014â€“FR-019 / SC-005, SC-008â€“SC-010, SC-014 and SC-018 (authorization, owner boundaries, errors, contract, and prohibited scope); FR-020 / SC-015 (station-local date); FR-021 / SC-002, SC-012, SC-016 (audit and correction); FR-022 / SC-017 (stable stop identities); FR-023 / SC-018 (observation evidence failures). Each item points to the automated evidence in the table; crosswalk documentation itself is not acceptance evidence.

## Downstream follow-up artifacts

- **Passenger client feature specification**: add implementation tasks and automated tests for configured in-app cancellation/platform alert delivery and presentation, associated with the same `UpdateId` and revision as visible status. Keep SR-025 / SC-007 open until a client test demonstrates both.
- **Station-display client feature specification**: add implementation tasks and automated tests for refresh/update behavior using consistent journey revisions. Keep SR-058 open until client evidence passes.
- **Driver Operations feature specification**: provide the authorized observation and assignment owner interfaces and automated owner-side tests. Feature 003 continues to fail closed until genuine provider evidence is available.
- **Incident Management and operations-client feature specifications**: implement affected-journey associations and authorized context reads in Incident Management, then compose authorized incident context with official journey data in the operations client and test the integrated view. Keep SR-044 open until both owner-module and client evidence passes.
- **Flutter Driver and operational-client feature specifications**: retain secure credential/session handling and applicable loading, no-data, 403, error, retry, and no-network UI obligations in the owning client specifications, with their own automated tests.

## AWS boundary

Feature work remains local-first. AWS resource creation, Cognito configuration, IAM, RDS, API Gateway, Lambda, SAM, and deployment are manual user operations after complete local verification. This quickstart contains no AWS commands or AWS configuration.

## Final local verification evidence

Executed 2026-10-19 from the isolated Feature 003 integration worktree. `dotnet restore SmartRail.slnx` completed successfully; `dotnet build SmartRail.slnx --no-restore` succeeded with 0 warnings and 0 errors. `dotnet test SmartRail.slnx --no-build` passed the full solution suite: 138 unit tests and 68 PostgreSQL/API integration tests, 206 total, with 0 failures and 0 skipped tests. Persistence tests used disposable databases on the dedicated local test service at `127.0.0.1:5433`; no development or AWS/RDS database was targeted. The unit project was rerun independently to capture its exact count (138/138).

Feature 003 HTTP acceptance evidence includes the five approved operations, Controller global action scope from current Feature 002 role grants, separate cancellation grant, revoked/insufficient role denial, fail-closed unavailable role evidence, Driver and Station Staff denial, journey/station read consistency, cancellation/platform publication metadata, and Feature 001 anonymous-read regression. Production journey initialization remains deny-by-default. Automated test fixtures are isolated and are not an application authentication bypass.

`git diff --check` passed after final documentation and source review. The working changes are limited to the isolated Feature 003 integration worktree; the original feature branch was not modified or merged. No AWS resources or deployment commands were used. Backend evidence does not close passenger in-app alert delivery (SR-025/SC-007), Incident Management plus operations-client composition (SR-044), station-display refresh (SR-058), other downstream client acceptance, or production initialization-authority/audit-governance approvals.
