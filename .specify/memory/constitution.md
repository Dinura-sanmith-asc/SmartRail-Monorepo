<!--
Sync Impact Report
Version change: template without a version (0.0.0) -> 1.0.0
Modified principles: PRINCIPLE_1_NAME -> I. Approved Scope and Traceability;
  PRINCIPLE_2_NAME -> II. Flutter Architecture and Ownership;
  PRINCIPLE_3_NAME -> III. Authoritative Backend Integration;
  PRINCIPLE_4_NAME -> IV. Security, Privacy, and Accessibility;
  PRINCIPLE_5_NAME -> V. Verified Delivery and Honest Information.
Added sections: Additional Engineering Constraints; Development Workflow and Quality Gates.
Removed sections: None.
Follow-up TODOs: Confirm the original ratification date; resolve passenger identity,
  live journey/status, and catering contracts before the dependent features are specified.
Sources considered: approved requirements workbook and inventory, SmartRail SDD handoff,
  RBAC/API contracts PDF, Railway Network OpenAPI contract, current Flutter project.
-->
# SmartRail Passenger Mobile Application Constitution

## Core Principles

### I. Approved Scope and Traceability
Every passenger feature MUST follow approved requirement wording, priority, scope limits,
and acceptance criteria in the requirements workbook. Specifications MUST identify the
applicable SR IDs, including applicable shared requirements, without assigning every
platform requirement to the passenger app. Each implemented feature MUST trace from SR ID
through specification, plan, tasks, Flutter implementation, automated tests, and acceptance
evidence. Must Have requirements in the app's approved scope MUST NOT be silently omitted;
Should Have, Could Have, and Won't Have classifications MUST retain their approved meaning.
This keeps the mobile release aligned with accepted evidence instead of creating scope by
assumption.

### II. Flutter Architecture and Ownership
The passenger client MUST use Flutter and Dart with feature-based organization and clear
presentation, application/state, domain, and data/API responsibilities. Widgets MUST focus
on presentation and interaction; business-facing workflows MUST be independently testable.
Dependencies MUST flow through explicit boundaries. State-management, routing, and
networking libraries MUST be selected in technical planning with a documented reason.
The client MUST NOT duplicate authoritative backend business rules or introduce layers,
frameworks, or abstractions without a demonstrated need. The client owns passenger
presentation, interaction, mobile state, and API clients; backend modules own railway and
provider data, policy, and official state.

### III. Authoritative Backend Integration
The existing SmartRail .NET backend and its owning modules remain authoritative for railway
business data and published operational state. The client MUST consume approved API
contracts, using strongly typed models, centralized environment configuration, bounded
timeouts, and consistent interpretation of documented success and error responses. It MUST
NOT hardcode production API addresses, invent unsupported fields, or duplicate timetable
eligibility calculations. Railway Network service responses describe planned schedules;
dated journeys and official live status MUST remain distinct. Passenger search for stations,
ordered route stops, date-eligible services, and service details MUST use the approved
Railway Network contract. Additional integrations MUST wait for approval of their owning
module contracts, including request/response schemas and authorization behavior. Contract
changes MUST be coordinated with the responsible module.

### IV. Security, Privacy, and Accessibility
Public railway search and permitted public reads MUST work without login. Protected
passenger functions MUST use the approved Amazon Cognito identity model and secure token
storage when their contracts are defined. Credentials and sensitive tokens MUST NOT be
stored in plaintext or exposed in logs or user-facing errors. The backend MUST enforce
authentication, authorization, passenger data ownership, and resource scope; client-side
role checks are presentation controls only. The app MUST NOT expose protected operational
information. Passenger status and notices MUST communicate meaning through text or symbols,
not color alone. Features MUST address approved accessibility needs, including screen-reader
semantics, text scaling, contrast, touch targets, focus, and understandable feedback, at the
priority approved for each requirement. This places security and accessibility in feature
design and verification while preserving the agreed requirement tiers.

### V. Verified Delivery and Honest Information
Each applicable passenger workflow MUST handle its defined initial, loading, success, empty,
validation, access-denied, unavailable, network-failure, and retry states. The app MUST
distinguish scheduled, estimated, actual, stale, and unavailable information. Missing or
stale railway data MUST be labeled accordingly; the app MUST NOT fabricate train positions,
ETAs, delay reasons, service availability, or provider order/payment states. Cached data MUST
NOT be presented as confirmed live information. Features MUST receive automated verification
appropriate to their contracts and risk, including unit, widget, API-client, state, navigation,
failure/retry, accessibility, and integration or end-to-end checks as applicable. External
APIs SHOULD be mocked for isolated tests; cross-system acceptance MUST use approved backend
integrations. Compilation or a rendered screen alone is not acceptance evidence. A feature
is done only when applicable acceptance criteria are verified, traceability is updated, and
no unresolved critical defect remains.

## Additional Engineering Constraints

- The passenger app MUST respect the wider platform ownership boundaries: Railway Network
  owns stations, routes, and schedules; Journeys & Status owns dated journeys and live
  status; Identity & Permissions owns authentication policy; Incident Management owns
  operational incident state; and the Catering Adapter integrates the independent provider.
- Public railway reads MUST use the documented unauthenticated contract where specified.
  Protected actions MUST follow the owning module's approved RBAC/API contract. A client
  interface MUST NOT imply authority to publish official railway changes.
- Catering menus and order state MUST remain provider-authoritative. Order creation and
  recovery MUST follow approved idempotency semantics; uncertain outcomes MUST be shown as
  pending or unknown until confirmed. Catering failures MUST NOT prevent core railway search
  or service-status access. SmartRail MUST NOT become a food-payment clearinghouse.
- Reusable service schedules MUST remain separate from dated journey instances. Planned
  times MUST NOT be described as observed or live times.
- Detailed screen flows, visual designs, component choices, and individual API examples
  belong in feature specifications and technical plans, not in this constitution.

## Development Workflow and Quality Gates

Every feature MUST follow the Spec Kit sequence: specify, clarify, plan, tasks, analyze,
implement, test, and verify. A specification MUST define approved SR IDs, passenger goals,
scenarios, functional behavior, validation, UI and failure states, accessibility expectations,
dependencies, and acceptance criteria. A technical plan MUST identify affected Flutter
features and layers, models, API contracts, state management, navigation, error handling,
authentication where applicable, and verification. Implementation MUST NOT begin until the
relevant specification and technical plan are approved.

Reviews MUST check requirement traceability, contract and ownership boundaries, security,
accessibility, state coverage, test evidence, and the absence of unsupported railway claims.
Cross-system acceptance MUST verify the agreed integrations. Full passenger-app completion
requires evidence for every applicable approved Must Have criterion; platform requirements
outside the passenger app's scope are not grounds to claim or deny mobile completion.

## Governance

This constitution governs passenger feature specifications, technical plans, task lists,
implementation, and code review. If a detailed practice conflicts with a principle here, the
principle governs until an approved amendment changes it. Amendments MUST be proposed with
their rationale, affected principles, requirement and contract impact, and migration or
follow-up work. The project owner or designated human approver MUST review and approve each
amendment before it becomes authoritative. Version numbers use semantic versioning: MAJOR for
backward-incompatible principle or governance changes, MINOR for new principles or materially
expanded guidance, and PATCH for clarifications and non-semantic refinements. Reviews MUST
record compliance findings and any accepted exception with its owner and resolution plan.
The project MUST resolve the ratification date before treating this initial constitution as
formally ratified.

**Version**: 1.0.0 | **Ratified**: TODO(RATIFICATION_DATE): confirm original adoption date | **Last Amended**: 2026-10-18
