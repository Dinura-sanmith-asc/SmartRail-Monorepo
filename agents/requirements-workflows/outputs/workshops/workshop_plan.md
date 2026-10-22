# SmartRail Stakeholder Workshop Plan

Workshop candidates: 53

Planned workshops: 11

## WS-001 — Catering Integration, Operations, and Financial Liability

**Issue:** Discusses menu catalog synchronization, order lifecycle tracking, cancellation cut-offs, payment responsibility, refunds, and financial liability for wasted food during disruptions.

**Findings:** CF-001, CF-002, CF-003, CF-005, CF-007

**Participants:** catering_provider, product_owner

**Objective:** Determine data synchronization intervals, transaction ownership, financial liabilities for delays, and the extent of order cancellation features.

**Discussion questions:**

- How often must menu data be refreshed if SmartRail caches the data?
- Who handles the actual financial transaction with the passenger and processes refunds, and who covers the cost of wasted food due to severe train delays or missed collections?
- Does SmartRail provide a new estimated arrival time or just a delayed flag to the catering provider?
- Should SmartRail enforce cut-offs in the UI or act purely informatively while letting the catering system enforce rules?
- Should the system support partial cancellations of individual order items?

## WS-002 — Catering Disruption Resilience and Support Escalation

**Issue:** Addresses resiliency, message reconciliation during network failures, and customer service escalation paths for catering disputes.

**Findings:** CF-006, CF-008

**Participants:** catering_provider, customer_service

**Objective:** Establish reliable recovery mechanisms for lost catering messages and clarify the primary support contact for passenger order disputes.

**Discussion questions:**

- How do SmartRail and the catering provider reconcile lost order creation messages or achieve eventual consistency during network failures?
- Who is the primary contact point for the passenger when resolving order disputes, and how are issues escalated?

## WS-003 — Customer Service Journey Information, Ticketing, and Disruption Support

**Issue:** Covers authoritative real-time information sources, extended support scope (ticketing, compensation, hotels/taxis), and inter-operator ticket validity rules during disruptions.

**Findings:** CF-010, CF-011, CF-019

**Participants:** customer_service, product_owner

**Objective:** Define the single source of truth for journey data, establish system boundaries for compensation and alternative transport support, and determine how inter-operator rules are communicated.

**Discussion questions:**

- What is the definitive authoritative source for real-time journey information?
- Should SmartRail include ticketing support, refunds, and compensation management, and are hotel/taxi arrangements part of system or operator policy scope?
- How can inter-operator ticket validity rules be easily viewed and communicated during disruptions?

## WS-004 — Station Accessibility, Facility Updates, and Vulnerable Passenger Protocols

**Issue:** Focuses on real-time reporting of out-of-order station accessibility equipment, replacement transport activation, assistance protocols, and vulnerable passenger handover procedures.

**Findings:** CF-013, CF-016, CF-017, CF-018, CF-022

**Participants:** customer_service, station_manager

**Objective:** Clarify real-time facility status reporting workflows, decision ownership for replacement transport, mobility assistance tracking, and care protocols for vulnerable passengers.

**Discussion questions:**

- How are broken or out-of-order station facilities and accessibility equipment reported in real time to customer service?
- Who decides on and arranges replacement transport, and how is it communicated?
- What are the specific handover and support protocols for vulnerable passengers?

## WS-005 — Driver Safety Notices, System Fallbacks, and DMI Operations

**Issue:** Examines critical safety notices acknowledgement, manual fallback procedures for assignments and complete DMI failures, defect classifications, and offline buffering limits.

**Findings:** CF-023, CF-024, CF-028, CF-029, CF-032

**Participants:** driver, railway_controller

**Objective:** Define auditable safety notice workflows, manual fallback protocols during system outages, defect criteria, and connectivity resilience limits for drivers.

**Discussion questions:**

- How are critical safety notices delivered and acknowledged before proceeding?
- What is the exact manual fallback process if the assignment system is down?
- Can the train be operated manually with fallback information if the DMI fails completely?
- What criteria define a minor defect versus a major defect?
- How long can the system operate on cached data during connectivity loss?

## WS-006 — Driver Pre/Post-Journey Workflows, Precision Stopping, and External Roles

**Issue:** Addresses source precedence, precision stopping guidance, post-journey checklist enforcement, automated reporting integration, and external operational role interactions.

**Findings:** CF-026, CF-035, CF-037, CF-038, CF-040

**Participants:** driver, product_owner

**Objective:** Determine information source precedence, scope for dynamic braking guides, DMI checklist enforcement, reporting auto-population, and integration needs for external roles.

**Discussion questions:**

- Which source takes precedence if on-board system data differs from Control or paper documents?
- Should the system provide dynamic braking guides for precision stopping?
- Should train securing procedures be enforced via a mandatory DMI checklist?
- How much auto-population happens from the DMI to reporting systems, and what is the driver's validation role?
- Are these external operational roles direct users of SmartRail or interfacing via separate systems?

## WS-007 — Passenger Ticketing, Multi-Operator Fares, and Retail Roles

**Issue:** Explores handling of multi-operator non-integrated ticketing, best value options, direct retail vs redirection roles, and data security protocols.

**Findings:** CF-042, CF-049, CF-050

**Participants:** passenger, product_owner

**Objective:** Clarify SmartRail's retailing boundaries, multi-operator fare integration logic, and security standards for passenger data.

**Discussion questions:**

- How will SmartRail handle scenarios where an optimal journey involves travel with multiple operators that don't traditionally integrate their ticketing?
- Will SmartRail act as a direct retailer for all train tickets, or will it redirect to individual operator sites for final purchase?
- What protocols will be in place for data security and privacy when storing payment details and personal information?

## WS-008 — Passenger Journey Disruption, Navigation, and Support Services

**Issue:** Covers push notification thresholds, offline ticket mode, station mapping accuracy, lift status, connection protection, compensation management, and lost property.

**Findings:** CF-043, CF-051, CF-052, CF-053, CF-055, CF-056

**Participants:** passenger, customer_service

**Objective:** Define notification thresholds, offline ticket display capabilities, accuracy expectations for navigation, connection protection rules, and compensation/lost property management.

**Discussion questions:**

- What level of granular detail will accessibility information provide (e.g., specific measurements vs. general tags)?
- What threshold constitutes a significant delay for a push notification and what level of detail will be provided in disruption explanations?
- Will SmartRail have an offline mode for displaying mobile tickets in case of signal loss at the station?
- What is the accuracy level of coach stopping position predictions and how will real-time lift status be consistently reported from all stations?
- Will SmartRail clearly indicate if a connection is protected and what level of automatic re-booking will be facilitated?
- Will SmartRail directly manage compensation claims or redirect to operators, and will the lost property portal require real-time updates from all operators?

## WS-009 — Core Incident Management, Baselines, and MVP Scope Boundaries

**Issue:** Focuses on baseline business problems, core incident lifecycle triggers, governance processes, deferred post-MVP scope, and system non-functional requirements like latency.

**Findings:** CF-057, CF-058, CF-060, CF-061, CF-062

**Participants:** product_owner, railway_controller

**Objective:** Confirm the baseline problem statement, core incident workflows, feature exclusions for MVP, and precise integration and latency targets.

**Discussion questions:**

- What is the official baseline business problem description for SmartRail?
- What are the precise triggers, recipients, and escalation paths for different incident types and severities?
- Should passenger compensation workflows be tracked or integrated at a later phase?
- What are the exact latency thresholds, legacy integration requirements, KPIs, and regulatory constraints?

## WS-010 — Railway Control, External Integrations, and Platform Allocation Conflicts

**Issue:** Addresses integration of emergency services and infrastructure engineers, handling of station staff platform change vetoes, operational vs commercial priorities, cancellation authority, and route competence.

**Findings:** CF-064, CF-070, CF-071, CF-072, CF-073, CF-076

**Participants:** railway_controller, station_manager

**Objective:** Resolve authority conflicts between network controllers and station staff regarding platform changes and cancellations, define emergency services access, and clarify automated rules.

**Discussion questions:**

- What direct system access, communication protocols, and track access logging mechanisms do emergency services and infrastructure engineers require?
- How should the system handle a veto or warning raised by station staff against a controller-initiated platform change?
- Can an operational cancellation be triggered automatically if a TOC fails to confirm resource availability by a specific threshold time?
- What automated alerts can reduce the window between a cancellation decision and passenger notification?
- What is the formal procedure and audit trail for overriding incorrect automated system predictions?
- Is driver route competence tracked automatically within the system or verified manually?

## WS-011 — Station Local Incident Management, Displays, and Resilience

**Issue:** Covers unprompted local incident input mechanisms, override authority for platform allocation and displays, PA announcement controls, infrastructure repair loop closure, tiered workflows, offline backup, and density metrics.

**Findings:** CF-078, CF-079, CF-081, CF-082, CF-083, CF-084, CF-085, CF-088

**Participants:** station_manager, product_owner

**Objective:** Establish local station control capabilities, override authorities, automated PA safeguards, maintenance workflow tracking, and offline local resilience.

**Discussion questions:**

- Is there a dedicated local station status module for staff to input unprompted local incidents or infrastructure failures?
- Who has ultimate authority to override platform allocation disputes between local safety constraints and network optimization algorithms?
- Can staff broadcast ad-hoc or pre-recorded messages, and does the system feature automated verification for announcements?
- Can local staff override sections of network-controlled station displays to publish local operational messages?
- What is the formal workflow to close the loop on reported infrastructure failures and update their operational status?
- Does SmartRail contain distinct tiered incident management workflows for varying levels of local severity?
- What core functionality can be maintained locally via backup power and offline cache if network connectivity is completely severed?
- To what extent can network optimization algorithms ingest real-time local station density metrics automatically?
