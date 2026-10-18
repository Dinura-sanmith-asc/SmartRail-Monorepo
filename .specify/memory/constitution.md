# SmartRail Station Display Constitution

## Core Principles

### I. Approved Scope and Requirement Traceability
Every feature specification, plan, task, implementation, test and verification record MUST identify the applicable approved requirement IDs and acceptance criteria. The requirements workbook is authoritative for scope, wording and priority. For this React Station Display, directly applicable Must Have requirements are SR-054 (upcoming departures with scheduled times and latest known operational statuses), SR-056 (authoritative platform, delay and cancellation changes), SR-058 (automatic refresh or updates), SR-059 (visible cached or possibly outdated state during central disconnection), and SR-061 (one functional station display prototype). Specs MUST preserve their accepted wording and MUST NOT silently add, omit, downgrade or defer approved requirements. Each applicable Must Have MUST trace from requirement through specification, plan, tasks, implementation, automated tests and verification evidence.

### II. Honest Railway Information and Display Responsibility
The display MUST distinguish station information, planned train schedules, and dated operational journey information. Scheduled times MUST NOT be presented as confirmed live times or statuses. Delays, cancellations, platform changes and other operational status MUST be shown only when received from an approved authoritative source; unknown or unavailable values MUST remain explicitly unknown or unavailable. Cached information MUST be visibly marked with its freshness or possible staleness. The passive Station Display MUST remain separate from the interactive Station Terminal; terminal-only behaviors and requirements MUST NOT be introduced into this application without approved scope change.

### III. Contract-First Integration and Data Ownership
The ASP.NET Core backend is the authoritative source for SmartRail railway data. The frontend MUST consume approved REST API contracts and MUST NOT invent endpoints, response fields, train positions, ETA values, platforms, delays or operational states. Existing Railway Network contracts cover `GET /api/stations`, `GET /api/routes/{id}/stops`, `GET /api/services`, and `GET /api/services/{id}`; these provide reference and planned schedule information, not live journey state. Journeys and Status endpoints listed as future integration MUST NOT be treated as implemented until their backend contracts and behavior are verified. Mock data MAY be used for isolated development and tests only when clearly identifiable as mock and impossible to confuse with live data. The client MUST NOT duplicate authoritative backend business rules or treat visual controls as authorization.

### IV. Accessible, Readable and Resilient Presentation
The display MUST present relevant information with clear hierarchy, readable typography, sufficient contrast, consistent layouts, and responsive behavior for approved display environments. Status MUST be conveyed with text or symbols as well as color. Applicable accessibility requirements MUST meet WCAG 2.2 AA. The interface MUST provide meaningful loading, empty, unavailable, API failure, disconnection, recovery and stale-data states. It MUST NOT silently present cached information as current. Refresh intervals, cache validity, operational thresholds, target resolutions and detailed layout behavior MUST be established in approved feature specifications rather than invented in the constitution.

### V. Secure, Testable and Maintainable Frontend
The approved frontend stack is React, TypeScript, Vite and Tailwind CSS. Code MUST use feature-based organization with clear boundaries among app configuration, feature components, shared UI, API clients, hooks/state, styling/utilities and automated tests. Add frameworks or architectural layers only when an approved need justifies them. The application MUST use secure API communication, safe configuration, safe rendering of external text and no frontend secrets. It MUST collect no unnecessary passenger data and expose no sensitive data through logs. Automated quality gates MUST include TypeScript, ESLint and production build verification, plus Vitest, React Testing Library and Playwright coverage appropriate to the feature. Tests MUST verify applicable acceptance criteria, including rendering, API behavior, failure and stale states, accessibility, supported display behavior and relevant passenger-information flows. A successful build alone is not completion evidence.

## Frontend Constraints

- The display presents public passenger information only and MUST NOT perform privileged backend operations. Backend authorization remains authoritative; frontend visibility is not a security control.
- The backend owns railway business logic and railway data. The display consumes approved REST contracts and MUST show planned and operational data according to the meaning and freshness represented by those contracts.
- One station display prototype is in scope. The interactive station terminal is not required; its search, dedicated-device and session-reset behaviors are outside this constitution's approved application scope.
- The codebase currently contains the Vite React/TypeScript entry points and `src` application and style files. Feature specifications MUST account for the actual repository structure and dependencies; this constitution does not assert that any test framework or live API integration is already installed or implemented.
- Frontend changes MUST NOT modify the approved workbook, backend constitution or OpenAPI contract as a side effect. Contract and requirement changes require their own documented approval and source updates.

## Spec-Driven Workflow and Quality Gates

1. **Specify and clarify:** Follow Specify → Clarify → Plan → Tasks → Analyze → Implement → Test → Verify. Approved specifications MUST precede implementation. Resolve material ambiguity for the affected behavior before implementing it.
2. **Plan and task:** Link each applicable requirement and acceptance criterion to specification outcomes, implementation tasks, tests and planned verification evidence. Identify dependencies on backend endpoints and mark unverified future contracts as dependencies.
3. **Implement and test:** Keep changes within approved scope and frontend boundaries. Exercise relevant normal, empty, failure, disconnected, recovery and stale-data behavior, and validate accessibility and display behavior at the resolutions defined by the specification.
4. **Verify and close:** Record evidence for every applicable Must Have requirement. A feature is complete only when its approved acceptance criteria pass and its evidence is recorded; a build result by itself is insufficient.

## Governance

This constitution governs Station Display specifications, plans, tasks, frontend implementation, API integration, tests and verification. The main SmartRail constitution governs shared ecosystem principles; this document carries forward only the frontend decisions needed here and does not replace backend-only rules. In a conflict, the approved requirements workbook controls requirement scope and accepted wording, approved API contracts control frontend integration, and approved project architecture decisions control technology choices. Conflicts MUST be documented and resolved by the appropriate project decision owner before dependent implementation proceeds.

A proposed amendment or exception MUST document its rationale, affected requirements and artifacts, and approval by the project decision owner. Changes to requirement wording or priority MUST be approved in the requirements baseline first. API or architecture changes MUST be approved and reflected in their governing source before frontend implementation depends on them. Reviewers MUST check traceability, data meaning and freshness, contract alignment, accessibility, security and test evidence at the relevant quality gate.

Constitution versions use semantic `MAJOR.MINOR.PATCH`: MAJOR for incompatible governance or principle changes, MINOR for new principles or materially expanded guidance, and PATCH for clarifications that do not change policy. The existing file contained only the template scaffold, so this is version 1.0.0.

**Version**: 1.0.0 | **Ratified**: 2026-10-18 | **Last Amended**: 2026-10-18