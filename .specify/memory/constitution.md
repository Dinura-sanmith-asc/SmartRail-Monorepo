<!--
Sync Impact Report (review aid; remove before committing)
- Version: unratified template -> 1.0.0 (initial project constitution).
- Replaced placeholder principles with nine SmartRail principles; no prior project principles removed.
- Added sections: Engineering Constraints; Development Workflow and Quality Gates.
- Source alignment: 117 requirements (53 Must, 42 Should, 16 Could, 6 Won't), ADR-007,
  approved RBAC and 21-operation inventory, six original diagrams, current solution projects.
- Template compatibility: spec, plan and tasks templates read this constitution at runtime.
  Their example text is unchanged. The generic tasks template's optional-test example does
  not waive SmartRail's mandatory verification. No generated template was modified.
- Follow-up placeholders: none. Feature-level transitions, schemas, thresholds and commercial
  assumptions remain for approved feature specifications, not this constitution.
-->
# SmartRail Software Engineering Constitution

## Core Principles

### I. Approved Scope and End-to-End Traceability

The approved workbook is the authority for requirement IDs, wording, priority and source
acceptance checks. The baseline contains 117 requirements: 53 Must Have, 42 Should Have,
16 Could Have and 6 Won't Have for this release. Every Must Have MUST be implemented and
verified across its applicable backend, mobile, web, integration, security, quality or
deployment surface. A Must Have MUST NOT be silently omitted, downgraded or marked complete
from an endpoint or build result alone. Every feature MUST maintain a trace from SR ID to
specification, plan, task, implementation, automated test and verification evidence. Entry of
any other requirement into release scope requires an explicit, recorded scope decision.

### II. Specification Before Business Implementation

Feature work MUST follow Specify -> Clarify -> Plan -> Tasks -> Implement -> Test -> Verify.
An approved feature specification MUST precede business implementation. It MUST identify
applicable SR IDs, actors, workflows, preconditions, outcomes, states and transitions,
validation, permissions and ownership, failure and edge cases, acceptance checks and exclusions.
A plan MUST identify affected modules, data and migrations, OpenAPI contracts, authentication,
external boundaries, failure handling and automated tests. Tasks MUST be independently
understandable and traceable. Meaningful ambiguity MUST be resolved and recorded before the
affected behavior is implemented; an unsupported assumption MUST NOT replace an approved rule.

### III. One Modular Backend with Inward Dependencies

SmartRail MUST use one deployable ASP.NET Core .NET 10 modular monolith with pragmatic Clean
Architecture and feature-based organization. The six owned modules are Railway
Network, Journeys & Status, Incident Management, Driver Operations, Identity & Permissions,
and Catering Adapter. `SmartRail.Domain` MUST contain business rules without API, persistence
or AWS dependencies. `SmartRail.Application` MUST depend on Domain and define use cases and
required interfaces without depending on Infrastructure. `SmartRail.Infrastructure` MUST
implement persistence and external adapters and may depend on Application and Domain.
`SmartRail.Api` MUST expose REST, enforce authentication and authorization, compose
implementations and invoke use cases. Cross-module writes MUST pass through the owning
module's rules. Additional SmartRail deployments, generic repositories, event brokers, framework layers or
per-module projects require a documented need and an approved architecture change.

### IV. Explicit Sources of Truth and Data Integrity

PostgreSQL with Entity Framework Core MUST preserve primary and foreign keys, uniqueness,
required fields, relationships, transaction boundaries, concurrency controls and audit
information appropriate to each feature. Scheduled train services MUST be distinct from dated
journeys. Planned, estimated, observed and officially published information MUST remain
distinguishable. Only an authorized railway publication may change official journey state;
driver observations MUST NOT do so automatically. Unknown or stale location, ETA and status
MUST be shown honestly. Cognito owns credentials. The independent catering provider owns menu,
order, fulfilment and commercial state; SmartRail may hold references or cached provider
responses without claiming them as authoritative. The ERD is a design reference; feature plans
MUST resolve its gaps before migration or publication logic is implemented.

### V. Server-Enforced Identity, Scope and Safety

The approved RBAC matrix governs public, passenger, driver, station-staff and controller
access. The backend MUST verify Cognito JWTs and enforce role, ownership and target
station/journey scope for protected operations. Frontend controls MUST NOT be the security
boundary. Drivers may submit observations for assigned journeys but MUST NOT publish official
status. Only authorized controllers may publish official journey status, platform/timing
changes and cancellation decisions. Station incident access MUST respect station scope;
passengers MUST NOT access another passenger's orders or protected information. Service-to-
service catering credentials MUST remain separate. Inputs, displayed text, errors,
configuration and logs MUST be handled so that forged updates, executable markup, secrets and
sensitive data are not accepted or exposed. Every protected operation MUST have positive and
negative authorization tests that also check unauthorized state changes do not occur.

### VI. Documented API and External Contracts

The approved 21-operation SmartRail API inventory is the route baseline. Each affected feature
plan MUST refine it into testable OpenAPI methods, parameters, request/response schemas,
authentication, resource policies, validation, status/error behavior and concurrency rules.
Public responses MUST expose only permitted published railway information. Additional routes
require a documented link to approved behavior or a necessary technical function; a route's
presence alone does not establish requirement coverage. The separate catering API boundary
MUST use authenticated communication, validated responses, idempotent order submission,
safe retries, timeouts, status refresh and honest uncertain-result states. Catering failure
MUST NOT disable core railway search, incident or status functions. SmartRail MUST NOT claim
payment success or implement settlement/liability rules without an approved commercial decision.

### VII. Evidence-Based Quality and Five Cross-Boundary Flows

Applicable domain, use-case, API, PostgreSQL persistence, authorization, validation, external
integration, failure/recovery and client tests MUST verify source acceptance checks. Test
selection MUST match the behavior and risk of each feature; a passing build or mocked
component alone is insufficient. The release MUST automate and pass five flows across their
real application and integration boundaries: (1) passenger search and journey status;
(2) controller disruption publication reflected in passenger and station views; (3) driver
assignment access and authorized observation, with controller review/publication where
needed for live status; (4) passenger order through the independent catering service; and
(5) scoped station incident reporting and lifecycle/impact. Failed tests MUST be repaired and
rerun before affected behavior is marked verified.

### VIII. Honest, Accessible and Observable Operation

Clients MUST provide meaningful loading, empty, denial, error, retry, unavailable and stale
states for applicable approved workflows. Status meaning MUST be conveyed by text or symbols
as well as color. External-service faults MUST fail gracefully without fabricating railway or
provider outcomes. APIs and deployment MUST provide safe health diagnostics and structured
logs sufficient to diagnose database and integration failures without exposing credentials or
personal data. Local development and demo deployment MUST be reproducible. Measurable
freshness, polling, retention, reliability or performance values MUST be agreed in the
relevant feature or deployment specification; they MUST NOT be invented by this constitution.

### IX. Approved Stack and Deployable Boundaries

The selected backend stack is C# / ASP.NET Core .NET 10, EF Core, PostgreSQL in Docker locally
and Amazon RDS PostgreSQL for deployment, REST/OpenAPI, Amazon Cognito JWT, AWS API Gateway,
AWS Lambda, AWS SAM and CloudWatch. Mobile clients use Flutter/Dart; web clients use
React/TypeScript. .NET tests use xUnit and persistence integration tests use PostgreSQL.
Feature plans MUST preserve these choices unless an explicit architectural decision changes
them. SmartRail's railway database MUST NOT revert to DynamoDB because older diagrams show it.
Deployment plans MUST provide secure Lambda-to-RDS connectivity, database access controls and
a deliberate external-service egress design. The catering service MUST retain separate API,
credential and data ownership.

## Engineering Constraints

- The current solution projects are `SmartRail.Api`, `SmartRail.Application`,
  `SmartRail.Domain`, `SmartRail.Infrastructure`, `SmartRail.UnitTests` and
  `SmartRail.IntegrationTests`. Plans MUST use the actual repository layout, including
  separate test projects, rather than illustrative paths in generic templates.
- The approved workbook controls requirement scope; later recorded architecture decisions
  control superseded technology labels in the original diagrams. The RBAC/API PDF controls
  its permission and route inventories. Supporting diagrams are evidence, not authority to
  restore outdated database or network assumptions.
- Lifecycle transition graphs, detailed API/provider schemas, station and ownership rules,
  review/publication behavior and measurable nonfunctional thresholds belong in approved
  feature specifications and plans. Commercial payment and liability ownership remains an
  assumption until externally agreed; no implementation may present it as confirmed.
- Delivery schedules, resource estimates and spending targets are planning inputs, not
  constitutional compliance criteria.

## Development Workflow and Quality Gates

1. **Specify and clarify:** Record SR IDs and source acceptance checks; settle material
   ambiguities and identify any proposed scope or architecture change for approval.
2. **Plan and create tasks:** Check module ownership and dependency direction; define data,
   OpenAPI, authorization, integration and test evidence. Every task MUST link back to a
   specification outcome and applicable SR IDs.
3. **Implement and test:** Apply reviewed migrations; test domain/use cases, real database and
   API behavior, client states and external faults as applicable. Enforce authorization on the
   server and verify denied operations leave state unchanged.
4. **Verify and close:** Update the trace from requirement through evidence. A feature is done
   only when its approved specification, relevant contracts, migrations, clients, failure
   paths and automated acceptance checks pass with no unresolved critical defect.
5. **Release gate:** All 53 Must Have requirements MUST have implementation and verification
   evidence, and all five cross-boundary E2E flows MUST pass. The deployment MUST pass a
   documented clean-start and smoke test before release is declared complete.

## Governance

This constitution governs SmartRail specifications, plans, tasks, implementation and reviews.
Reviewers MUST check traceability, architecture boundaries, data ownership, authorization,
contracts and test evidence at the applicable quality gate. A feature specification cannot
silently override a principle. An amendment or exception MUST identify the source decision,
rationale, affected SR IDs and artifacts, migration or verification impact, and explicit
approval by the project decision owner before dependent implementation. Approved requirement
wording changes belong in the requirements baseline first; technology or ownership changes
need an architectural decision. Resolve conflicts by following the approved workbook for
scope, the latest approved architecture decision for implementation choices, and the RBAC/API
source for permissions and route baselines. Record unresolved conflicts and stop only the
affected feature until they are decided.

Versions use semantic `MAJOR.MINOR.PATCH`: MAJOR for incompatible principle changes,
MINOR for new principles or material guidance, PATCH for clarification without policy change.
This is the initial constitution; subsequent ratification and amendment dates MUST be recorded
here. The sync impact comment at the top is review material and MUST be removed before the
constitution is committed.

**Version**: 1.0.0 | **Ratified**: 2026-10-16 | **Last Amended**: 2026-10-16
