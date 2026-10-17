# Research: Railway Network & Train Search

**Feature**: `001-railway-network-search`  
**Date**: 2026-10-16

This document records plan decisions and proposals. Source requirements and approved clarifications remain authoritative; recommendations marked **approval required** are not approved business rules.

## Decisions

### Use the existing .NET 10 solution and project boundaries

**Decision**: Keep the feature inside the existing six projects and use the currently empty Domain, Application and Infrastructure projects. Use ASP.NET Core Minimal APIs, matching folders across layers, focused EF query/import interfaces and no additional module project.

**Rationale**: ADR-007 and the constitution require one modular monolith with inward dependencies. The existing project references already follow the intended direction: API references Application and Infrastructure; Application references Domain; Infrastructure references Application and Domain. `Program.cs` is only a template weather API today.

**Alternatives considered**: Separate projects for Railway Network, a generic repository framework, mediator/CQRS framework, or a second deployment. Each adds a boundary the constitution does not authorize or the feature does not need.

### Use EF Core 10 with the Npgsql 10 provider and PostgreSQL 18

**Decision**: Add a mutually compatible EF Core 10 / `Npgsql.EntityFrameworkCore.PostgreSQL` 10.x package set at implementation time. Pin Docker to PostgreSQL 18 and select an available PostgreSQL 18 engine patch in the AWS deployment region. Match the PostgreSQL major version locally and in RDS.

**Rationale**: The solution targets `net10.0` and the existing OpenAPI dependency is 10.0.12. EF Core 10 targets .NET 10 and is an LTS release. Npgsql publishes an EF Core 10 provider; exact patch versions should be pinned together when package references are introduced. AWS documents RDS PostgreSQL 18 support and available patch versions; the deployment region's exact engine version must be rechecked before provisioning.

**Alternatives considered**: A different database or provider conflicts with the approved architecture. Pinning a PostgreSQL minor patch before confirming the AWS region could prevent local/deployment parity.

**Evidence**: [EF Core release/support table](https://learn.microsoft.com/en-us/ef/core/what-is-new/), [EF Core 10 release](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-10.0/whatsnew), [Npgsql EF Core 10 release](https://www.npgsql.org/efcore/release-notes/10.0.html), [RDS PostgreSQL engine versions](https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/PostgreSQL.Concepts.General.DBVersions.html), and [RDS PostgreSQL release history](https://docs.aws.amazon.com/AmazonRDS/latest/PostgreSQLReleaseNotes/doc-history.html).

### Represent published exact-date eligibility separately from operational journeys

**Decision**: Add exact service operation-date evidence to an immutable, versioned published schedule revision. Keep exactly one current published revision per service and preserve prior revisions as superseded history. Store ordered calls linked to route stops, with time offsets from the schedule's first-call departure. Use controlled, versioned timetable fixtures for local and integration environments. Each fixture publication carries explicit completeness for each covered operation date, including a complete date with zero services; a missing service row means “not operating” only for a date with a complete manifest. Do not generate recurrence, weekday, holiday, or advance calendars.

**Rationale**: SR-013 requires date- and direction-valid search. A route alone cannot prove a service calls at a station. Exact published date rows meet the feature's clarified behavior; an explicit completeness marker distinguishes no match from missing/incomplete source data. Calling points are the minimal indexed source needed to find arbitrary station pairs and their order. This is a search eligibility projection; it has no operational journey ID, status, live position, ETA, platform, incident, driver assignment or lifecycle state. Journeys & Status may later create its own `Journey` for an eligible service-date and own actual stops/status.

**Alternatives considered**: Route membership alone is incorrect where a service skips stations. Full recurring calendars implement deferred SR-005 behavior. A date-specific `Journey` owned by Railway Network would duplicate Journeys & Status. An origin/destination pair materialization avoids storing ordered calls but duplicates pair rows and requires regenerating them after schedule changes.

**Approval required**: Confirm the authoritative railway timetable source and who is authorized to publish/supersede revisions. Until the source can prove complete exact-date coverage, do not treat an absent service row as evidence that a service does not operate. The one-current-revision and history-preservation model is fixed for this plan; the production publication authority remains to be named.

### Use the selected origin's departure date for overnight services

**Decision**: Treat the request date as the local calendar date on which the service departs from the passenger's selected origin. Given a schedule's first-call departure time and the selected origin's departure offset, derive the schedule operation date that places that departure on the requested date. Search then verifies that exact operation date is published and complete.

**Rationale**: This is the user's approved SR-013 clarification. It allows a service that starts before midnight to be found for a downstream origin after midnight without assigning the destination arrival date as the travel date.

**Decision**: Use `Asia/Colombo` as the local railway schedule zone for this feature. Preserve the distinction between local schedule dates/times and UTC audit instants; API scheduled instants carry the `+05:30` offset. No multi-zone behavior is introduced. PostgreSQL `timestamp with time zone` stores instants in UTC and does not retain the original zone. See [PostgreSQL date/time types](https://www.postgresql.org/docs/current/datatype-datetime.html).

### Use a controlled, audited station import instead of a public write endpoint

**Decision**: A reviewed source-controlled import file is the production maintenance path. A station steward prepares it; an independently authorized Railway Network owner approves it; a restricted invocation of the import command validates and applies the batch transactionally. The import command is dispatched by the existing application host and is never an HTTP route. Seed local/test stations through the same validation logic. Upsert by the authoritative source identity (mapping to be confirmed), preserve station IDs, reject duplicate identities and invalid references, and never delete on omission.

**Rationale**: This implements the approved clarification and SR-002 creation check without claiming a route or permission absent from the PDF. The PDF's four Railway Network endpoints are read-only. A repeatable import is reviewable, testable and compatible with the existing solution without another project or public management UI.

**Approval gate**: A production maintainer role and approval authority must be named because the current RBAC matrix has no station-maintenance action. The Railway Reference Data Steward / Railway Network Data Owner split is a proposal only. The importer records source revision/hash, submitter and approver identities, execution time, counts and minimal per-station action/changed-field evidence. Do not add field-level before/after history or general-purpose audit workflows; broader audit beyond SR-002 import/change evidence may promote SR-012 (Should Have) and needs a separate scope decision.

**Alternatives considered**: A public station POST/PATCH route conflicts with the clarified scope. An authenticated staff API would add an operation and role to the approved contract inventory; consider it only through an explicit recorded API/RBAC change.

### Fail closed on ambiguous or incomplete publication data

**Decision**: Reject publication of an affected schedule when an origin or destination call is missing, call ordering is inconsistent, required endpoint timing is absent, or the relevant date manifest is incomplete. During search, report unavailable eligibility when source completeness cannot be established; do not fabricate a match or silently present a partial result list as complete. Use valid `200 []` only when all relevant publication data is complete and no service qualifies.

**Rationale**: The specification requires missing information to be distinguished from a true no-result state and prohibits eligibility inference from route membership alone.

**Approval required**: There is no approved timetable freshness age or source refresh SLA. Treat withdrawn/superseded publication as stale; do not expire data based on an invented duration. The owner must set freshness/retention behavior before an age-based stale decision is implemented.

### Withhold ambiguous repeated-station publications

**Decision**: Reject or withhold publication of an affected route/schedule if the same station ID occurs more than once and no approved calling-point occurrence rule resolves the passenger's boarding/alighting choice. Never silently select the first or last occurrence. Do not add a route-wide unique-station constraint; it would prohibit valid repeated calls and encode a broader rule than needed.

**Rationale**: The same station ID can occur at multiple ordered calls, making the requested origin/destination ambiguous. Choosing the first call silently could return the wrong departure time.

**Approval gate**: Before enabling repeated station IDs in a published route, the railway owner must approve deterministic occurrence-selection semantics. Until then, validation withholds that publication; unrelated unambiguous routes remain publishable.

### Use the four public GET contracts and `WebApplicationFactory` integration tests

**Decision**: Preserve the four API paths in the PDF; document request, DTO, validation, status and error behavior in the OpenAPI contract. Use `application/problem+json` for invalid, missing-resource and unavailable cases. Add `Microsoft.AspNetCore.Mvc.Testing` for application-host integration tests and real PostgreSQL isolation tooling during implementation.

**Rationale**: The API/RBAC PDF allows public reads and contains no station-write route. ASP.NET Core documents `WebApplicationFactory<TEntryPoint>` for integration tests; its test-host package is not currently referenced. RFC 9457 provides the standard Problem Details response model. PostgreSQL integration tests are required by the constitution.

**Alternatives considered**: Mock-only persistence tests do not verify PostgreSQL translations, FKs or unique constraints. An EF in-memory provider is not a real PostgreSQL substitute. Adding a new route now would exceed the approved contract inventory.

**Evidence**: [ASP.NET Core OpenAPI for Minimal APIs](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/aspnetcore-openapi), [ASP.NET Core integration tests](https://learn.microsoft.com/en-us/aspnet/core/test/integration-testing), and [RFC 9457 Problem Details](https://www.rfc-editor.org/rfc/rfc9457.html).

## Approval register

| Decision | Recommended proposal | Approval required before |
|---|---|---|
| Authoritative timetable input and date coverage | Versioned source publishes complete exact operation-date manifests, eligible schedules, ordered calls and endpoint schedule times | Search implementation treats date-row absence as not operating |
| Timetable publication/revision | One active published revision per service; supersede through a reviewed source revision; keep old records for audit | Migration or publication workflow is finalized |
| Station maintenance role and human review | Railway Reference Data Steward prepares; Railway Network Data Owner separately approves; no public write API | Production import permission is granted |
| Station public detail schema | Expose stable station ID and name until the data owner verifies which location/service details are sourced and approved for public display | Any additional station field is added to public API or fixtures |
| Service time zone | `Asia/Colombo` local schedule interpretation and `+05:30` offset for serialized scheduled instants | Configure local schedule parsing/serialization |
| Repeated station visits | Withhold affected publication until deterministic occurrence semantics are approved; never silently choose | A repeated-station publication is enabled |
| Stale schedule evidence | No age-based cutoff; explicit supersession/incomplete-manifest state only | Any age threshold or refresh expectation is implemented |
| Station audit depth | Import run and minimal per-station action/changed-field evidence only | Broader before/after history or audit functionality is proposed |
| Production publication authority | Name timetable publisher and station-import approver; infer no role from current RBAC | Production write credentials are granted |

## Repository inspection

The solution has the six expected projects. Existing references follow the constitution's layer direction. The API has only a template `/weatherforecast` route and OpenAPI setup; no entities, endpoints for Railway Network, EF context, migrations, auth policies or database configuration exist. The two xUnit projects have `Microsoft.NET.Test.Sdk`, xUnit and coverage packages; the integration project references API and Infrastructure but has no web test host or PostgreSQL fixture packages. The only current app package is `Microsoft.AspNetCore.OpenApi` 10.0.12. No code, package or project file was changed in this plan.
