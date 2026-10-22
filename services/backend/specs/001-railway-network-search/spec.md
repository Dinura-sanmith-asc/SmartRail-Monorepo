# Feature Specification: SmartRail Railway Network & Train Search

**Feature Branch**: `001-railway-network-search`

**Created**: 2026-10-16

**Status**: Draft — business clarifications resolved; ready for planning review

**Input**: User description: establish railway stations, ordered routes, scheduled services and passenger search by origin, destination and travel date.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Find a train service (Priority: P1)

A passenger selects an origin station, a destination station and a travel date, then sees relevant published train-service choices. The search gives useful feedback for an invalid pair, an empty result and an unavailable source. It does not present a schedule choice as a confirmed live train position or ticket reservation.

**Why this priority**: Origin/destination/date search is the approved passenger entry point (SR-013) and the first required end-to-end flow.

**Independent Test**: With a known reference dataset, a passenger can search without signing in and distinguish a matching service from invalid input, no matching service and temporary unavailability.

**Acceptance Scenarios**:

1. **Given** two identifiable stations, a valid travel date and a published service known to serve the origin before the destination on that date, **when** a passenger searches, **then** the matching service appears with an unambiguous service identifier and enough planned journey context to select it. (SR-013; SR-003; SR-004)
2. **Given** a route whose ordered stops put the destination before the origin, **when** the passenger searches that direction, **then** that route alone does not create a false matching service. (SR-003; SR-013)
3. **Given** the same station as origin and destination, an unknown station, a missing station or an invalid calendar date, **when** the passenger submits the search, **then** the search is rejected with specific, usable input feedback rather than an apparently successful empty result. (SR-013; SR-114)
4. **Given** a valid origin, destination and date with no matching published service, **when** the passenger searches, **then** a clear no-results state is shown and the entered criteria remain understandable for retry. (SR-013; SR-114)
5. **Given** reference information cannot be retrieved, **when** the passenger searches, **then** the passenger sees an unavailable/error state and a retry option, with no fabricated service choice. (SR-114)
6. **Given** a service crosses midnight after leaving the selected origin, **when** the passenger searches using the scheduled departure date from that origin, **then** the service is evaluated for that date even if its destination arrival is on the following calendar date. (SR-013)

---

### User Story 2 - Explore stations and ordered routes (Priority: P2)

A passenger or railway user can identify a station and inspect an ordered route so service choices use consistent railway reference data.

**Why this priority**: Station IDs and route sequence support service search and downstream passenger, station and operational experiences (SR-002; SR-003).

**Independent Test**: A known station is retrievable by a stable identifier, and the route's stops are returned in their defined order with unambiguous station identifiers.

**Acceptance Scenarios**:

1. **Given** a registered station, **when** a viewer retrieves station information, **then** its identifier, name and available essential location/service details are presented consistently. (SR-002)
2. **Given** a route with multiple ordered stops, **when** a viewer inspects it, **then** every stop is linked to the correct station identifier and appears in journey order, including when station names are similar. (SR-003)
3. **Given** an invalid route identifier or an unavailable reference source, **when** a viewer requests route details, **then** the experience distinguishes not found from temporary failure. (SR-003; SR-114)
4. **Given** an authorized railway reference-data maintainer and an approved station change, **when** the controlled maintenance process creates or updates a station record, **then** the record can be retrieved and referenced by routes and services without exposing a public station-management interface. (SR-002)

---

### User Story 3 - Inspect a scheduled service (Priority: P3)

A passenger or railway user can inspect a selected service's identity, route and schedule reference before opening any dated journey or live-status view.

**Why this priority**: A published service must refer to a valid route and identifiable schedule (SR-004); this also provides stable context for later journey features.

**Independent Test**: A known service can be retrieved and traced to a valid route, schedule and service identifier without requiring a live operational update.

**Acceptance Scenarios**:

1. **Given** a published service, **when** its details are requested, **then** the service identifier, associated route and identifiable schedule are shown; the route can be followed through ordered station stops. (SR-003; SR-004)
2. **Given** a service reference whose route or schedule is missing, **when** the data is reviewed for publication, **then** it cannot be treated as a valid published service. (SR-004)
3. **Given** that live journey status is absent, **when** the viewer inspects the scheduled service, **then** planned service information remains distinguishable from unavailable live information. (SR-004; dependency on SR-007 and SR-019)

### Edge Cases

- A route may contain repeated station names or, if later approved, repeated visits to one station; ordering must still identify the requested boarding and alighting stops unambiguously. Whether repeated station IDs are permitted remains a domain question, not an automatic rejection rule.
- A published service may belong to a route yet not call at every route station. Search eligibility requires authoritative published information to establish that the service operates on the requested travel date and calls at the selected origin before the destination. This pair-specific check does not define a complete operating calendar or stopping pattern.
- For a service crossing midnight, travel date means the scheduled departure date from the selected origin; arrival may occur on the following calendar date.
- A query date may have no eligible service, or eligibility data may be missing. A true no-match result and unavailable/incomplete eligibility information must not be conflated.
- A station, route or service may have been withdrawn or changed. Publication/retirement rules are not specified by the approved four requirements and need clarification before such records are exposed or altered.
- Network loss while a passenger is viewing cached information requires a visible unavailable/stale indication under SR-109; full passenger, driver and station offline behavior belongs to shared client specifications.

## Requirements *(mandatory)*

### Approved requirement traceability

The four primary rows below are copied from the [approved workbook](../../docs/SmartRail-SDD-Handoff/evidence/requirements.xlsx), `FINAL UNIFIED REQUIREMENTS`, rows 6, 7, 8 and 17. Priority and acceptance wording are preserved exactly.

| ID | Priority | Approved requirement wording | Source verification / acceptance check |
|---|---|---|---|
| SR-002 | Must Have | The platform shall maintain identifiable railway stations and expose station names and essential location/service details to passenger and operational channels. | Station records can be created, retrieved, referenced by services, and displayed. |
| SR-003 | Must Have | The platform shall represent railway routes and their ordered station sequences. | Route stops are returned in journey order with unambiguous station IDs. |
| SR-004 | Must Have | The platform shall maintain train services associated with routes and identifiable service schedules. | Every published service references a valid route, schedule and service identifier. |
| SR-013 | Must Have | Passengers shall be able to search railway services using an origin, destination and travel date. | Valid search returns matching service choices; invalid pairs and no-result searches show useful feedback. |

The [approved RBAC/API inventory](../../docs/SmartRail-SDD-Handoff/evidence/rbac-api-contracts.pdf) grants public read access for train, station and schedule search and lists station, route-stop, service-search and service-detail reads. Its table does not define request/response schemas or a station-management write operation. Station creation and maintenance therefore use controlled railway reference-data initialization/import and an authorized maintenance process, with no public station-management interface. The responsible role assignment and operating procedure remain planning details; the later technical plan must refine contracts without treating those gaps as approved new public behavior.

### Functional Requirements

- **FR-001 (SR-002)**: SmartRail MUST support registration and maintenance of stations with stable, unambiguous identities, names and available essential location/service details through controlled railway reference-data initialization/import and an authorized maintenance process. This does not require a public station-management interface.
- **FR-002 (SR-002)**: A viewer MUST be able to retrieve station information for use in service selection and downstream railway views. Public information MUST exclude protected operational data.
- **FR-003 (SR-003)**: Each route MUST identify an ordered sequence of station stops; retrieval MUST preserve that order and expose unambiguous station identifiers.
- **FR-004 (SR-004)**: Each published train service MUST have its own identifier and reference a valid route and identifiable schedule. Incomplete references MUST NOT be represented as a published service.
- **FR-005 (SR-004)**: A viewer MUST be able to retrieve a published service's planned identity, route and schedule context separately from any dated journey or live status.
- **FR-006 (SR-013)**: A passenger MUST be able to search published railway services with origin station, destination station and travel date.
- **FR-007 (SR-003; SR-013)**: A search result MUST be eligible only when authoritative published service information establishes that the service operates on the requested travel date and serves both selected stations in origin-before-destination order. Route membership alone MUST NOT establish that a service calls at a station. This is a pair-specific eligibility check; it does not introduce SR-005's fuller operating-day calendar or complete stopping-pattern behavior. For a service crossing midnight, travel date MUST mean the scheduled departure date from the selected origin.
- **FR-008 (SR-013)**: Search MUST distinguish invalid or incomplete criteria from a valid query with no matching result and give useful feedback for each.
- **FR-009 (SR-013; SR-114)**: Retrieval and search MUST distinguish temporary unavailability from a true no-result state, support retry, and avoid fabricating station, route or service information.
- **FR-010 (SR-089)**: Public readers MUST be able to access permitted railway reference and published service information without staff authentication; protected operational information remains separate.
- **FR-011 (SR-007 dependency)**: A reusable scheduled service MUST NOT be presented as a dated operational journey. This feature supplies reference context to Journeys & Status; it does not create, update or publish live journey state.

### Dependencies and scope boundaries

- **Shared platform data (SR-001, Must Have):** the railway network is the common reference source for passenger, operations, driver and station consumers. This feature provides station, route and service identifiers; cross-channel journey-update consistency is owned by later Journeys & Status work.
- **Dated journeys (SR-007, Must Have):** Journeys & Status owns operational journey instances and live progress. Search may need an approved date-eligibility source from that module, but this specification does not define journey creation or status transitions.
- **Public boundary and client states (SR-089, SR-109, SR-114, Must Have):** public railway reads and relevant search error/offline indications apply here. Full cross-client offline behavior and complete primary-view error coverage remain shared release obligations.
- **Failure isolation (SR-113, Must Have):** a catering outage must not block core train search; the separate integration specification owns catering behavior and its failure simulation.
- **Deferred features:** SR-005 (operating days and full stopping pattern), SR-014–017 (upcoming/alternative views and richer station/stop details) are Should Have; SR-018 connections is Could Have. They are not silently promoted by this search specification. No ticketing, availability inventory, live status publication, disruptions, driver operations or catering orders belong here.
- **Source model:** the original ERD shows `STATION`, `ROUTE`, `ROUTE_STOP`, `TRAIN_SERVICE` and `SERVICE_STOP`, but it does not settle date eligibility, skipped calls or station publishing rules. The original container and backend diagrams show shared consumers; later handoff decisions supersede their older database label.

### Key Entities *(include if feature involves data)*

- **Station:** Stable railway identity, display name and verified essential location/service details available to public or operational channels (SR-002).
- **Route / Route Stop:** Named route and ordered references to stations. Sequence position and station identity are distinct; the same station name cannot serve as the key (SR-003).
- **Train Service:** Reusable service identity associated with a route and identifiable schedule. This is planned railway information, not a dated journey or current status (SR-004).
- **Service Schedule / Calling Point:** Planned schedule context needed to describe service choices. Authoritative published information must establish date operation and the selected origin/destination calls in order for search eligibility. For overnight services, the applicable date is scheduled departure from the selected origin. This pair-specific eligibility does not define SR-005's fuller operating calendar or complete stopping pattern.
- **Operational Journey (external dependency):** A dated service instance with its own status, owned by Journeys & Status (SR-007); referenced only to preserve the boundary.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001:** In acceptance data, every registered station is retrievable with the same stable identifier and name across station and service references; the SR-002 record-creation check is demonstrated through the clarified registration workflow.
- **SC-002:** For every route in the acceptance data, all returned stops retain their defined order and correct station IDs, including a case with similar station names.
- **SC-003:** Every published service in the acceptance data has a valid service identifier, associated route and identifiable schedule; no incomplete service is shown as published.
- **SC-004:** Every defined valid search case returns only service choices whose origin precedes destination and whose date eligibility is established; no unsupported call or date is presented as available.
- **SC-005:** Each acceptance case for missing/invalid criteria, invalid station pair, no matching service and unavailable information produces a distinct, understandable result. These are scenario-completion checks, not new performance targets.
- **SC-006:** An anonymous passenger can complete the search and open permitted service details without a staff login, while no live operational status is inferred from schedule data.

## Assumptions

- Search presents published railway service choices, not seat availability, ticket inventory, guaranteed connections or live train movement.
- Station reference data is initialized/imported and maintained through an authorized railway process; no public station-management interface is part of this feature. The specific responsible role and operating procedure are to be assigned during planning.
- Station names may be similar, so stable identifiers and route position determine matching. Any station display name alone is insufficient to identify a stop.
- Travel date is a valid calendar date supplied by the passenger. For a service crossing midnight, it is the scheduled departure date from the selected origin. No past-date cutoff, advance-booking window, full operating-day calendar or performance threshold is approved for this feature.

## Clarifications

### Session 2026-10-16

- Q: How are stations registered and maintained under SR-002? → A: Use controlled railway reference-data initialization/import and an authorized railway maintenance process. Do not add a public station-management interface; assign the responsible role and operating procedure during planning.
- Q: What minimum rule determines SR-013 date and calling-point eligibility, including services crossing midnight? → A: Return a service only when authoritative published information establishes that it operates on the requested travel date and serves the selected origin before the destination. For overnight services, travel date is the scheduled departure date from the selected origin. This does not add SR-005's fuller operating calendar or stopping-pattern requirements.
