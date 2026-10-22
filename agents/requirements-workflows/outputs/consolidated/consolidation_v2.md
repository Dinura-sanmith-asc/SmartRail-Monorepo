# SmartRail Cross-Stakeholder Consolidation

**Original discoveries:** 134

**Final consolidated findings:** 88

**Uncovered discoveries:** 0

## CF-001 — Catering System Integration, Menus, and Catalog Management

- **Type:** OVERLAP
- **Stakeholders:** catering_provider
- **Categories:** FUNCTIONAL, INTEGRATION, BUSINESS_PROCESS
- **Source refs:** CATERING_PROVIDER-001, CATERING_PROVIDER-002, CATERING_PROVIDER-003
- **Summary:** The catering provider's system integrates with SmartRail by providing structured catalogs containing menus, pricing, allergen information, preparation instructions, availability rules, station identifiers, and collection deadlines.
- **Workshop required:** True
- **Unresolved question:** How often must menu data be refreshed if SmartRail caches the data?
- **Rationale:** These discoveries cover the core catalog setup, station identifier mapping, and eligibility/order deadlines which form the foundational integration requirements for catering.

## CF-002 — Payment Responsibility, Refunds, and Financial Liabilities

- **Type:** OWNERSHIP_AMBIGUITY
- **Stakeholders:** catering_provider
- **Categories:** AMBIGUITY, CONFLICT
- **Source refs:** CATERING_PROVIDER-004, CATERING_PROVIDER-008
- **Summary:** Ambiguity regarding whether SmartRail handles transactions directly or passes payment details to the catering provider, how refunds are processed, and who bears financial liability for wasted food due to delays or missed collections.
- **Workshop required:** True
- **Unresolved question:** Who handles the actual financial transaction with the passenger and processes refunds, and who covers the cost of wasted food due to severe train delays or missed collections?
- **Rationale:** Both findings address financial accountability, transaction flow, and loss liabilities between SmartRail and the catering provider.

## CF-003 — Real-time Order Lifecycle and Status Tracking

- **Type:** AGREEMENT
- **Stakeholders:** catering_provider
- **Categories:** FUNCTIONAL, STATE, OPERATIONAL
- **Source refs:** CATERING_PROVIDER-005, CATERING_PROVIDER-006, CATERING_PROVIDER-007
- **Summary:** Both SmartRail and the catering provider require robust real-time tracking, including idempotent order creation, asynchronous status updates from the kitchen, and dynamic adjustments based on train status and delay notifications.
- **Workshop required:** True
- **Unresolved question:** Does SmartRail provide a new estimated arrival time or just a delayed flag to the catering provider?
- **Rationale:** These items deal with the operational mechanics of real-time bidirectional communication, order state management, and idempotency.

## CF-004 — Catering Collection Verification and Validation

- **Type:** UNIQUE_IMPORTANT_FINDING
- **Stakeholders:** catering_provider
- **Categories:** FUNCTIONAL
- **Source refs:** CATERING_PROVIDER-009
- **Summary:** Collection point staff must validate passengers' QR codes or order numbers against order details during physical collection.
- **Workshop required:** False
- **Unresolved question:** What is the fallback procedure if dedicated staff are not present at the collection point or scanners fail?
- **Rationale:** Specific functional requirement for the physical handoff and verification phase of catering orders that does not overlap with ordering or system-level messaging.

## CF-005 — Catering Cancellation Cut-off Enforcement and Arbitration

- **Type:** OPEN_QUESTION
- **Stakeholders:** catering_provider
- **Categories:** AMBIGUITY
- **Source refs:** CATERING_PROVIDER-010
- **Summary:** Unclear whether SmartRail's UI should fully enforce cancellation cut-offs or if the catering system should act as the final arbiter.
- **Workshop required:** True
- **Unresolved question:** Should SmartRail enforce cut-offs in the UI or act purely informatively while letting the catering system enforce rules?
- **Rationale:** Deals specifically with user interface logic versus backend system rule enforcement for cancellations.

## CF-006 — System Downtime, Resiliency, and Guaranteed Messaging

- **Type:** AGREEMENT
- **Stakeholders:** catering_provider, customer_service
- **Categories:** FAILURE_SCENARIO
- **Source refs:** CATERING_PROVIDER-011, CUSTOMER_SERVICE-006
- **Summary:** Both catering and customer service contexts highlight risks associated with system slowdowns, downtime, and the need for robust recovery mechanisms or fallback options.
- **Workshop required:** True
- **Unresolved question:** How do SmartRail and the catering provider reconcile lost order creation messages or achieve eventual consistency during network failures?
- **Rationale:** Both findings focus on system availability, performance degradation, and the operational impact of downtime.

## CF-007 — Advanced Catering Features and Partial Cancellations

- **Type:** SCOPE_TENSION
- **Stakeholders:** catering_provider
- **Categories:** SCOPE_CANDIDATE
- **Source refs:** CATERING_PROVIDER-012
- **Summary:** Potential extension scope to support complex order operations such as partial item cancellations and passenger-specific preference management.
- **Workshop required:** True
- **Unresolved question:** Should the system support partial cancellations of individual order items?
- **Rationale:** Represents a capability that expands complexity and project scope beyond standard whole-order management.

## CF-008 — Customer Support Escalation, Dispute Resolution, and Support Ownership

- **Type:** OWNERSHIP_AMBIGUITY
- **Stakeholders:** catering_provider, customer_service
- **Categories:** AMBIGUITY
- **Source refs:** CATERING_PROVIDER-013
- **Summary:** Lack of clarity on who serves as the primary point of contact for passengers during order disputes and how support inquiries are handled.
- **Workshop required:** True
- **Unresolved question:** Who is the primary contact point for the passenger when resolving order disputes, and how are issues escalated?
- **Rationale:** Highlights organizational boundaries and responsibilities for resolving customer complaints and order disputes.

## CF-009 — Customer Service Access to Real-Time Journey and Delay Information

- **Type:** OVERLAP
- **Stakeholders:** customer_service
- **Categories:** FUNCTIONAL
- **Source refs:** CUSTOMER_SERVICE-001, CUSTOMER_SERVICE-002
- **Summary:** Customer service representatives require quick access to real-time schedule data, platform assignments, station layouts, train amenities, delay reasons, and estimated durations.
- **Workshop required:** False
- **Unresolved question:** None
- **Rationale:** Both findings outline core information requirements for CSR staff to adequately answer passenger inquiries.

## CF-010 — Single Source of Truth and Data Consistency Across Channels

- **Type:** UNIQUE_IMPORTANT_FINDING
- **Stakeholders:** customer_service
- **Categories:** INTEGRATION
- **Source refs:** CUSTOMER_SERVICE-003
- **Summary:** Inconsistencies between CSR systems, public screens, and mobile apps erode trust, necessitating a definitive authoritative source for real-time information.
- **Workshop required:** True
- **Unresolved question:** What is the definitive authoritative source for real-time journey information?
- **Rationale:** Addresses cross-channel data consistency and architecture governance which is unique to multi-channel information delivery.

## CF-011 — Extended Passenger Services: Ticketing, Compensation, and Travel Disruptions

- **Type:** SCOPE_TENSION
- **Stakeholders:** customer_service
- **Categories:** SCOPE_CANDIDATE
- **Source refs:** CUSTOMER_SERVICE-004, CUSTOMER_SERVICE-005
- **Summary:** Potential inclusion of advanced support scope such as ticketing support, refunds, compensation management, hotel accommodations, and private taxi arrangements for stranded passengers.
- **Workshop required:** True
- **Unresolved question:** Should SmartRail include ticketing support, refunds, and compensation management, and are hotel/taxi arrangements part of system or operator policy scope?
- **Rationale:** Both items introduce significant scope expansion possibilities regarding major passenger disruptions, ticketing, and financial compensation.

## CF-012 — Language Barrier and Translation Support in Customer Service

- **Type:** UNIQUE_IMPORTANT_FINDING
- **Stakeholders:** customer_service
- **Categories:** FAILURE_SCENARIO
- **Source refs:** CUSTOMER_SERVICE-007
- **Summary:** Inability to communicate effectively with passengers who speak different languages without dedicated translation support.
- **Workshop required:** False
- **Unresolved question:** How should language barriers be handled within the system?
- **Rationale:** Focuses specifically on multi-lingual communication challenges during customer service interactions.

## CF-013 — Real-time Facility Status and Accessibility Equipment Updates

- **Type:** OPEN_QUESTION
- **Stakeholders:** customer_service
- **Categories:** BUSINESS_PROCESS, ACCESSIBILITY
- **Source refs:** CUSTOMER_SERVICE-008, CUSTOMER_SERVICE-012
- **Summary:** There is a lack of clarity on how quickly customer service representatives are informed about out-of-order station facilities and accessibility equipment such as lifts and ramps in real time.
- **Workshop required:** True
- **Unresolved question:** How are broken or out-of-order station facilities and accessibility equipment reported in real time to customer service?
- **Rationale:** Both discoveries concern real-time tracking and visibility of station facility and accessibility equipment statuses.

## CF-014 — Dedicated Ticketing Agent Escalation

- **Type:** UNIQUE_IMPORTANT_FINDING
- **Stakeholders:** customer_service
- **Categories:** MISSING_ACTOR
- **Source refs:** CUSTOMER_SERVICE-009
- **Summary:** Complex ticketing and routing issues require escalation paths to a dedicated ticketing agent if SmartRail does not cover ticketing support directly.
- **Workshop required:** False
- **Unresolved question:** None
- **Rationale:** Addresses a specific missing actor escalation path for complex ticketing that does not overlap with other findings.

## CF-015 — External Handoffs for Medical, Security, and Lost Children

- **Type:** UNIQUE_IMPORTANT_FINDING
- **Stakeholders:** customer_service
- **Categories:** MISSING_ACTOR
- **Source refs:** CUSTOMER_SERVICE-010
- **Summary:** Medical issues, security concerns, lost children, and unruly passengers require clear handoff and escalation protocols to Station Management, First Aid, or British Transport Police.
- **Workshop required:** False
- **Unresolved question:** None
- **Rationale:** Defines vital emergency and safety handoff actors that must be preserved independently.

## CF-016 — Alternative Transport Activation Process

- **Type:** UNIQUE_IMPORTANT_FINDING
- **Stakeholders:** customer_service
- **Categories:** BUSINESS_PROCESS
- **Source refs:** CUSTOMER_SERVICE-011
- **Summary:** A defined process is missing for deciding when and how replacement transport like buses or taxis is activated and communicated during significant service disruptions.
- **Workshop required:** True
- **Unresolved question:** Who decides on and arranges replacement transport, and how is it communicated?
- **Rationale:** Focuses specifically on the operational business process for replacement transport during major delays.

## CF-017 — Spontaneous and Pre-booked Assistance Protocols

- **Type:** UNIQUE_IMPORTANT_FINDING
- **Stakeholders:** customer_service
- **Categories:** ACCESSIBILITY
- **Source refs:** CUSTOMER_SERVICE-013
- **Summary:** Clear operational protocols are required to handle both pre-booked and spontaneous mobility assistance, including management of assistance no-shows.
- **Workshop required:** True
- **Unresolved question:** None
- **Rationale:** Distinctly targets assistance handling workflows and failure scenarios rather than equipment status tracking.

## CF-018 — Unified Booking and Tracking Assistance System

- **Type:** UNIQUE_IMPORTANT_FINDING
- **Stakeholders:** customer_service
- **Categories:** INTEGRATION
- **Source refs:** CUSTOMER_SERVICE-014
- **Summary:** An integrated system is desired for booking and tracking mobility assistance requests across multi-operator journeys.
- **Workshop required:** True
- **Unresolved question:** None
- **Rationale:** Represents an extension candidate scope item for cross-operator assistance integration.

## CF-019 — Inter-Operator Ticket Acceptance and Rules

- **Type:** UNIQUE_IMPORTANT_FINDING
- **Stakeholders:** customer_service
- **Categories:** INTEGRATION
- **Source refs:** CUSTOMER_SERVICE-015
- **Summary:** Rules for ticket validity and acceptance across alternative routes or different operators during disruptions are unclear and require easy visibility and communication.
- **Workshop required:** True
- **Unresolved question:** How can inter-operator ticket validity rules be easily viewed and communicated during disruptions?
- **Rationale:** Deals with inter-operator ticketing rules during disruptions, distinct from assistance bookings.

## CF-020 — Complaint Logging System

- **Type:** UNIQUE_IMPORTANT_FINDING
- **Stakeholders:** customer_service
- **Categories:** FUNCTIONAL
- **Source refs:** CUSTOMER_SERVICE-016
- **Summary:** Customer service representatives need access to a designated system to accurately log complaints and track escalations.
- **Workshop required:** False
- **Unresolved question:** None
- **Rationale:** Specifies core functional tooling requirements for complaint handling.

## CF-021 — Dynamic and Constantly Changing Estimates

- **Type:** UNIQUE_IMPORTANT_FINDING
- **Stakeholders:** customer_service
- **Categories:** STATE
- **Source refs:** CUSTOMER_SERVICE-017
- **Summary:** Constantly shifting arrival and departure time estimates erode passenger trust and diminish customer service credibility during delays.
- **Workshop required:** False
- **Unresolved question:** None
- **Rationale:** Captures a state stability issue affecting passenger trust and CSR communications.

## CF-022 — Vulnerable Passenger Support Protocol

- **Type:** UNIQUE_IMPORTANT_FINDING
- **Stakeholders:** customer_service
- **Categories:** BUSINESS_PROCESS
- **Source refs:** CUSTOMER_SERVICE-018
- **Summary:** A defined procedure and handover protocol is needed to proactively support vulnerable passengers, children, the elderly, or those with cognitive disabilities during major cancellations or platform changes.
- **Workshop required:** True
- **Unresolved question:** What are the specific handover and support protocols for vulnerable passengers?
- **Rationale:** Focuses on specialized care processes for vulnerable groups distinct from general alternative transport.

## CF-023 — Critical Safety Notices Delivery and Acknowledgement

- **Type:** UNIQUE_IMPORTANT_FINDING
- **Stakeholders:** driver
- **Categories:** AMBIGUITY
- **Source refs:** DRIVER-001
- **Summary:** The mechanism for delivering and receiving mandatory auditable acknowledgements for critical safety notices prior to shift start is currently unclear.
- **Workshop required:** True
- **Unresolved question:** How are critical safety notices delivered and acknowledged before proceeding?
- **Rationale:** Identifies critical pre-shift safety notice requirements for drivers.

## CF-024 — System Assignment Provider Downtime and Manual Fallback

- **Type:** UNIQUE_IMPORTANT_FINDING
- **Stakeholders:** driver
- **Categories:** FAILURE_SCENARIO
- **Source refs:** DRIVER-002
- **Summary:** A robust and reliable manual fallback process must be established for shift assignments in the event of provider system downtime.
- **Workshop required:** True
- **Unresolved question:** What is the exact manual fallback process if the assignment system is down?
- **Rationale:** Highlights system resilience and fallback needs during assignment outages.

## CF-025 — Journey Assignment Acknowledgement Mechanism

- **Type:** UNIQUE_IMPORTANT_FINDING
- **Stakeholders:** driver
- **Categories:** AMBIGUITY
- **Source refs:** DRIVER-003
- **Summary:** It is ambiguous whether journey assignment acknowledgement should be a basic button press or require an active review and confirmation of key details.
- **Workshop required:** False
- **Unresolved question:** Is acknowledgement a simple button press or an active confirmation of key details?
- **Rationale:** Addresses the specific interaction design ambiguity for journey assignments.

## CF-026 — Single Source of Truth Precedence for Journey Information

- **Type:** CONFLICT
- **Stakeholders:** driver
- **Categories:** CONFLICT
- **Source refs:** DRIVER-004
- **Summary:** Discrepancies can occur between on-board systems, Control, and paper documents, requiring a clearly defined order of precedence.
- **Workshop required:** True
- **Unresolved question:** Which source takes precedence if on-board system data differs from Control or paper documents?
- **Rationale:** Directly represents a conflicting information source scenario requiring governance decisions.

## CF-027 — DMI Journey Data Loading and Error Handling

- **Type:** UNIQUE_IMPORTANT_FINDING
- **Stakeholders:** driver
- **Categories:** FUNCTIONAL
- **Source refs:** DRIVER-005
- **Summary:** The Driver Machine Interface must support loading journey details during pre-departure checks and provide proper error handling for display or loading failures.
- **Workshop required:** False
- **Unresolved question:** None
- **Rationale:** Outlines core functional expectations for DMI journey initialization.

## CF-028 — DMI Complete Failure Protocol and Manual Operation Fallback

- **Type:** UNIQUE_IMPORTANT_FINDING
- **Stakeholders:** driver
- **Categories:** AMBIGUITY
- **Source refs:** DRIVER-006
- **Summary:** Protocols and fallback guidelines are unclear in the event of a complete DMI failure during pre-departure checks, specifically regarding manual train operation capability.
- **Workshop required:** True
- **Unresolved question:** Can the train be operated manually with fallback information if the DMI fails completely?
- **Rationale:** Presents a critical safety-related operational ambiguity during total system failure.

## CF-029 — Distinction Between Minor and Major Train Defects

- **Type:** UNIQUE_IMPORTANT_FINDING
- **Stakeholders:** driver
- **Categories:** AMBIGUITY
- **Source refs:** DRIVER-007
- **Summary:** Clear operational criteria are missing to distinguish between minor defects that permit cautious operation and major defects requiring immediate attention.
- **Workshop required:** True
- **Unresolved question:** What criteria define a minor defect versus a major defect?
- **Rationale:** Clarifies definitions needed for fault management and safe train dispatch.

## CF-030 — System Role in Preventing Early Departures

- **Type:** UNIQUE_IMPORTANT_FINDING
- **Stakeholders:** driver
- **Categories:** FUNCTIONAL
- **Source refs:** DRIVER-008
- **Summary:** The extent of automated system enforcement or active prevention regarding early departures, beyond displaying scheduled times, is undefined.
- **Workshop required:** False
- **Unresolved question:** What role does the system play in actively preventing early departures?
- **Rationale:** Explores the functional boundaries of system enforcement versus advisory displays.

## CF-031 — Platform Clear Verification Integration

- **Type:** UNIQUE_IMPORTANT_FINDING
- **Stakeholders:** driver
- **Categories:** INTEGRATION
- **Source refs:** DRIVER-009
- **Summary:** Platform clear verification is primarily visual, but potential integration options with platform edge detection systems need clarification.
- **Workshop required:** False
- **Unresolved question:** Is there any integration with platform edge detection systems for verifying platform clear status?
- **Rationale:** Investigates potential safety system integrations for platform departure checks.

## CF-032 — System Behavior and Data Buffering During Connectivity Loss

- **Type:** OVERLAP
- **Stakeholders:** driver
- **Categories:** FAILURE_SCENARIO, OPERATIONAL
- **Source refs:** DRIVER-010, DRIVER-019
- **Summary:** Both discoveries address how the system should handle connectivity drops during operations by gracefully degrading, holding or caching valid instructions, buffering reports locally, and indicating status clearly.
- **Workshop required:** True
- **Unresolved question:** How long can the system operate on cached data during connectivity loss?
- **Rationale:** These two discoveries share the same underlying issue of network disconnections and operational continuity for the driver.

## CF-033 — Precedence of Safety-Critical Conflicting Information

- **Type:** UNIQUE_IMPORTANT_FINDING
- **Stakeholders:** driver
- **Categories:** SECURITY
- **Source refs:** DRIVER-011
- **Summary:** Safety-critical information must have clear precedence when conflicting information occurs, such as between DMI speed and trackside signs.
- **Workshop required:** False
- **Unresolved question:** None
- **Rationale:** Stands alone as a specific requirement regarding information conflict priority.

## CF-034 — Door Side Selection Verification and Alert

- **Type:** UNIQUE_IMPORTANT_FINDING
- **Stakeholders:** driver
- **Categories:** FUNCTIONAL
- **Source refs:** DRIVER-012
- **Summary:** The system should provide visual confirmation and an audible alert if an incorrect door side is selected during station stops.
- **Workshop required:** False
- **Unresolved question:** None
- **Rationale:** Unique functional safety/operational aid for station stops.

## CF-035 — Precision Stopping Guidance Support

- **Type:** SCOPE_TENSION
- **Stakeholders:** driver
- **Categories:** SCOPE_CANDIDATE
- **Source refs:** DRIVER-013
- **Summary:** Potential system capability to support precision stopping via dynamic braking guides.
- **Workshop required:** True
- **Unresolved question:** Should the system provide dynamic braking guides for precision stopping?
- **Rationale:** Represents an extension candidate that impacts scope and braking system integration.

## CF-036 — Emergency Communications and DMI Emergency Interfacing

- **Type:** OVERLAP
- **Stakeholders:** driver
- **Categories:** FUNCTIONAL
- **Source refs:** DRIVER-014, DRIVER-015
- **Summary:** SmartRail must prioritize emergency communications and reports over all other data, backed by a clear DMI emergency stop interface that automatically reports to Control.
- **Workshop required:** False
- **Unresolved question:** None
- **Rationale:** Both cover driver-initiated emergency signaling and reporting priorities.

## CF-037 — Post-Journey Checklist Enforcement on DMI

- **Type:** UNIQUE_IMPORTANT_FINDING
- **Stakeholders:** driver
- **Categories:** BUSINESS_PROCESS
- **Source refs:** DRIVER-016
- **Summary:** The system needs to ensure proper train securing procedures are followed through a mandatory DMI checklist.
- **Workshop required:** True
- **Unresolved question:** Should train securing procedures be enforced via a mandatory DMI checklist?
- **Rationale:** Focuses specifically on post-journey secure-down procedures.

## CF-038 — Integration Between DMI and Post-Journey Reporting Systems

- **Type:** UNIQUE_IMPORTANT_FINDING
- **Stakeholders:** driver
- **Categories:** INTEGRATION
- **Source refs:** DRIVER-017
- **Summary:** Integration is required between DMI real-time data and post-journey reporting systems regarding auto-population and driver validation roles.
- **Workshop required:** True
- **Unresolved question:** How much auto-population happens from the DMI to reporting systems, and what is the driver's validation role?
- **Rationale:** Highlights an interface and workflow gap for reporting data.

## CF-039 — Strict Operational Boundaries and Prohibitions

- **Type:** UNIQUE_IMPORTANT_FINDING
- **Stakeholders:** driver
- **Categories:** OPERATIONAL
- **Source refs:** DRIVER-018
- **Summary:** Drivers are restricted from performing specific actions without explicit authority, such as modifying schedules, bypassing safety interlocks, silencing critical alarms, or overriding safety systems.
- **Workshop required:** False
- **Unresolved question:** None
- **Rationale:** Enumerates core operational governance and system safety restrictions for drivers.

## CF-040 — External Operational Roles Interaction

- **Type:** OWNERSHIP_AMBIGUITY
- **Stakeholders:** driver
- **Categories:** MISSING_ACTOR
- **Source refs:** DRIVER-020
- **Summary:** The workflow relies on interactions with external operational roles such as Control, Signallers, Duty Managers, Shunters, Maintenance staff, and Medical personnel who provide authority, clearances, or briefs.
- **Workshop required:** True
- **Unresolved question:** Are these external operational roles direct users of SmartRail or interfacing via separate systems?
- **Rationale:** Identifies actor scope ambiguities across multiple railway personnel.

## CF-041 — Unified Journey Planner Platform

- **Type:** UNIQUE_IMPORTANT_FINDING
- **Stakeholders:** passenger
- **Categories:** SCOPE_CANDIDATE
- **Source refs:** PASSENGER-001
- **Summary:** A single national platform aggregates all routes, schedules, and real-time prices from all operators to help passengers find and compare options.
- **Workshop required:** False
- **Unresolved question:** None
- **Rationale:** Core passenger journey planning platform concept.

## CF-042 — Transparent Fares and Multi-Operator Handling

- **Type:** AGREEMENT
- **Stakeholders:** passenger
- **Categories:** FUNCTIONAL, AMBIGUITY
- **Source refs:** PASSENGER-002, PASSENGER-007
- **Summary:** Display all ticket types, terms, total cost upfront, and railcard discounts, while resolving how SmartRail handles non-integrated multi-operator ticketing and best value options.
- **Workshop required:** True
- **Unresolved question:** How will SmartRail handle scenarios where an optimal journey involves travel with multiple operators that don't traditionally integrate their ticketing?
- **Rationale:** Directly linked expectations around fare displays and the commercial complexities of multi-operator ticketing.

## CF-043 — Detailed Accessibility Information and Filtering

- **Type:** UNIQUE_IMPORTANT_FINDING
- **Stakeholders:** passenger
- **Categories:** FUNCTIONAL
- **Source refs:** PASSENGER-003
- **Summary:** Integrate comprehensive, verified accessibility information for every station and rolling stock type, enabling passengers to filter by specific needs.
- **Workshop required:** True
- **Unresolved question:** What level of granular detail will accessibility information provide (e.g., specific measurements vs. general tags)?
- **Rationale:** Specific passenger requirement focused on accessibility data depth.

## CF-044 — Proactive Disruption Warnings and Alternatives

- **Type:** UNIQUE_IMPORTANT_FINDING
- **Stakeholders:** passenger
- **Categories:** FUNCTIONAL
- **Source refs:** PASSENGER-004
- **Summary:** Highlight planned engineering works or long-term disruptions upfront during the planning stage and suggest suitable alternatives.
- **Workshop required:** False
- **Unresolved question:** How frequently will engineering work schedules be updated and verified by SmartRail from all operators?
- **Rationale:** Focuses on disruption management during passenger planning.

## CF-045 — Service and Occupancy Details

- **Type:** UNIQUE_IMPORTANT_FINDING
- **Stakeholders:** passenger
- **Categories:** FUNCTIONAL
- **Source refs:** PASSENGER-005
- **Summary:** Display information on available on-board services like WiFi, catering, and power sockets, along with estimated train occupancy levels.
- **Workshop required:** False
- **Unresolved question:** Will SmartRail provide predictive occupancy levels based on historical data and current booking trends, or just current real-time data?
- **Rationale:** Details on-board amenities and occupancy visibility.

## CF-046 — Integrated Onward Travel Information

- **Type:** SCOPE_TENSION
- **Stakeholders:** passenger
- **Categories:** SCOPE_CANDIDATE
- **Source refs:** PASSENGER-006
- **Summary:** Provide basic information about local transport links at origin and destination stations.
- **Workshop required:** False
- **Unresolved question:** None
- **Rationale:** Expands system reach into multi-modal local transport links.

## CF-047 — Visual Seat Selection

- **Type:** SCOPE_TENSION
- **Stakeholders:** passenger
- **Categories:** SCOPE_CANDIDATE
- **Source refs:** PASSENGER-008
- **Summary:** Allow passengers to select specific seats from a visual coach layout highlighting amenities, accessible spaces, and proximity to toilets.
- **Workshop required:** False
- **Unresolved question:** None
- **Rationale:** Represents an advanced passenger booking feature affecting inventory integration.

## CF-048 — Special Assistance Integration

- **Type:** UNIQUE_IMPORTANT_FINDING
- **Stakeholders:** passenger
- **Categories:** FUNCTIONAL
- **Source refs:** PASSENGER-009
- **Summary:** Provide a dedicated section within the booking process to request and confirm special assistance directly with clear confirmation messages.
- **Workshop required:** False
- **Unresolved question:** Will the system differentiate between different types of assistance needs and tailor the request accordingly?
- **Rationale:** Covers dedicated special assistance workflows for bookings.

## CF-049 — Direct Retailer vs. Redirect Role

- **Type:** UNIQUE_IMPORTANT_FINDING
- **Stakeholders:** passenger
- **Categories:** AMBIGUITY
- **Source refs:** PASSENGER-010
- **Summary:** It is unclear whether SmartRail will act as a direct retailer for all train tickets or redirect users to individual operator sites.
- **Workshop required:** True
- **Unresolved question:** Will SmartRail act as a direct retailer for all train tickets, or will it redirect to individual operator sites for final purchase?
- **Rationale:** Represents an unmerged business model ambiguity regarding ticketing functionality.

## CF-050 — Data Security and Privacy Protocols

- **Type:** UNIQUE_IMPORTANT_FINDING
- **Stakeholders:** passenger
- **Categories:** SECURITY
- **Source refs:** PASSENGER-011
- **Summary:** Protocols must be defined for data security and privacy when storing passenger payment details and personal information.
- **Workshop required:** True
- **Unresolved question:** What protocols will be in place for data security and privacy when storing payment details and personal information?
- **Rationale:** Standalone security compliance discovery that requires explicit definition.

## CF-051 — Day of Travel Proactive Notifications and Disruption Updates

- **Type:** OVERLAP
- **Stakeholders:** passenger
- **Categories:** FUNCTIONAL
- **Source refs:** PASSENGER-012, PASSENGER-016
- **Summary:** Send push notifications for significant delays, cancellations, platform changes, live train tracking updates, and disruption explanations.
- **Workshop required:** True
- **Unresolved question:** What threshold constitutes a significant delay for a push notification and what level of detail will be provided in disruption explanations?
- **Rationale:** Both findings deal with proactive passenger notifications regarding live train status and disruptions.

## CF-052 — Signal Loss and Device Failure

- **Type:** UNIQUE_IMPORTANT_FINDING
- **Stakeholders:** passenger
- **Categories:** FAILURE_SCENARIO
- **Source refs:** PASSENGER-013
- **Summary:** Passengers facing phone battery failure, missing tickets, or signal loss at the station when trying to present mobile tickets.
- **Workshop required:** True
- **Unresolved question:** Will SmartRail have an offline mode for displaying mobile tickets in case of signal loss at the station?
- **Rationale:** Unique failure scenario concerning offline access and device troubles.

## CF-053 — Station Navigation, Maps, and Facility Status

- **Type:** AGREEMENT
- **Stakeholders:** passenger
- **Categories:** FUNCTIONAL
- **Source refs:** PASSENGER-014, PASSENGER-015
- **Summary:** Provide interactive GPS-enabled station maps with facilities, accessible routes, coach stopping positions, and real-time out-of-order alerts for lifts, escalators, and accessible toilets.
- **Workshop required:** True
- **Unresolved question:** What is the accuracy level of coach stopping position predictions and how will real-time lift status be consistently reported from all stations?
- **Rationale:** Both items relate to station-level passenger navigation and facility status features.

## CF-054 — Discreet Issue Reporting on Board

- **Type:** UNIQUE_IMPORTANT_FINDING
- **Stakeholders:** passenger
- **Categories:** FUNCTIONAL
- **Source refs:** PASSENGER-017
- **Summary:** A discreet feature allowing passengers to report non-emergency issues like heating problems or anti-social behavior directly to on-board staff.
- **Workshop required:** False
- **Unresolved question:** What is the service level agreement for responses to issues reported via the app?
- **Rationale:** Distinct extension capability for passenger-to-staff communication.

## CF-055 — Missed Connections and Connection Risks

- **Type:** UNIQUE_IMPORTANT_FINDING
- **Stakeholders:** passenger
- **Categories:** FAILURE_SCENARIO
- **Source refs:** PASSENGER-018
- **Summary:** Delays on initial trains putting connections at risk or causing missed connections without clear alternative guidance or protected status.
- **Workshop required:** True
- **Unresolved question:** Will SmartRail clearly indicate if a connection is protected and what level of automatic re-booking will be facilitated?
- **Rationale:** Specific failure scenario focusing on journey continuation and re-booking risks.

## CF-056 — Centralized Compensation and Support Services

- **Type:** OVERLAP
- **Stakeholders:** passenger
- **Categories:** BUSINESS_PROCESS, SCOPE_CANDIDATE
- **Source refs:** PASSENGER-019, PASSENGER-020
- **Summary:** Centralized and simplified processes for claiming Delay Repay, auto-populating journey details, and consolidated lost property searches.
- **Workshop required:** True
- **Unresolved question:** Will SmartRail directly manage compensation claims or redirect to operators, and will the lost property portal require real-time updates from all operators?
- **Rationale:** Both findings address post-journey administrative services and support platforms.

## CF-057 — Assumed Initial Business Problem

- **Type:** UNIQUE_IMPORTANT_FINDING
- **Stakeholders:** product_owner
- **Categories:** BUSINESS_PROCESS
- **Source refs:** PRODUCT_OWNER-001
- **Summary:** The railway currently suffers from inefficient and siloed incident management processes, leading to delayed responses, poor real-time communication with passengers, increased operational costs, and diminished customer satisfaction during disruptions.
- **Workshop required:** True
- **Unresolved question:** What is the official baseline business problem description for SmartRail?
- **Rationale:** Foundational product assumption that sets the baseline rationale for the system.

## CF-058 — Core Incident Management and Operational Capabilities

- **Type:** AGREEMENT
- **Stakeholders:** product_owner
- **Categories:** FUNCTIONAL, STATE
- **Source refs:** PRODUCT_OWNER-002, PRODUCT_OWNER-003, PRODUCT_OWNER-004, PRODUCT_OWNER-006, PRODUCT_OWNER-007
- **Summary:** Authorized personnel can log incidents with details, track their lifecycle from reported to resolved, automatically alert operational teams, allocate basic resources, and view operational dashboards.
- **Workshop required:** True
- **Unresolved question:** What are the precise triggers, recipients, and escalation paths for different incident types and severities?
- **Rationale:** Cohesive set of core operational capabilities requested by the product owner for managing incidents.

## CF-059 — Basic Passenger Information Dissemination (Operational View)

- **Type:** OVERLAP
- **Stakeholders:** product_owner
- **Categories:** FUNCTIONAL
- **Source refs:** PRODUCT_OWNER-005
- **Summary:** Broadcast standardised, accurate delay and cancellation messages to passengers via multiple channels like station displays, app notifications, and website updates.
- **Workshop required:** False
- **Unresolved question:** What specific channels are included and what level of detail is provided?
- **Rationale:** Defines the operational perspective of passenger communications.

## CF-060 — Additional Operational Actors and Governance Processes

- **Type:** AGREEMENT
- **Stakeholders:** product_owner
- **Categories:** MISSING_ACTOR, BUSINESS_PROCESS
- **Source refs:** PRODUCT_OWNER-008, PRODUCT_OWNER-009
- **Summary:** Identification of additional essential actors (train drivers, station staff, emergency services, etc.) and vital governance processes (emergency response, training, system integrations).
- **Workshop required:** True
- **Unresolved question:** None
- **Rationale:** Both findings highlight structural gaps regarding operational stakeholders and institutional processes.

## CF-061 — Advanced Post-MVP Feature Scope Candidates

- **Type:** SCOPE_TENSION
- **Stakeholders:** product_owner, railway_controller
- **Categories:** SCOPE_CANDIDATE, OPERATIONAL
- **Source refs:** PRODUCT_OWNER-012, PRODUCT_OWNER-013, PRODUCT_OWNER-014, PRODUCT_OWNER-015, PRODUCT_OWNER-016, PRODUCT_OWNER-018, RAILWAY_CONTROLLER-001
- **Summary:** Multiple discoveries identify complex, resource-intensive features such as full AI-powered predictive maintenance, personalized journey planning, passenger CRM, dynamic pricing, automated compensation, and end-to-end asset management that should be excluded from core incident response scope and deferred to later phases.
- **Workshop required:** True
- **Unresolved question:** Should passenger compensation workflows be tracked or integrated at a later phase?
- **Rationale:** These items represent significant scope expansion vectors that require careful prioritization and alignment between product vision and core incident response delivery.

## CF-062 — Unresolved Implementation, Latency, and Integration Requirements

- **Type:** OPEN_QUESTION
- **Stakeholders:** product_owner
- **Categories:** AMBIGUITY, INTEGRATION, ASSUMPTION
- **Source refs:** PRODUCT_OWNER-010, PRODUCT_OWNER-011, PRODUCT_OWNER-019
- **Summary:** Critical uncertainties remain concerning the precise definition of real-time latency, legacy system integration extent and capabilities, primary KPIs, budget, timeline, and regulatory compliance constraints.
- **Workshop required:** True
- **Unresolved question:** What are the exact latency thresholds, legacy integration requirements, KPIs, and regulatory constraints?
- **Rationale:** These technical and strategic unknowns pose significant architectural and planning risks that must be addressed prior to full implementation.

## CF-063 — Core MVP Incident Management and Dashboard Capabilities

- **Type:** AGREEMENT
- **Stakeholders:** product_owner, railway_controller
- **Categories:** FUNCTIONAL
- **Source refs:** PRODUCT_OWNER-017, RAILWAY_CONTROLLER-007, RAILWAY_CONTROLLER-008, RAILWAY_CONTROLLER-010
- **Summary:** Both product owner and railway controller expectations align on core functional capabilities required for the MVP, including incident reporting, status tracking, centralised dashboards, network map displays, internal communications, and performance monitoring.
- **Workshop required:** False
- **Unresolved question:** None
- **Rationale:** The core functional scope for operations and incident tracking is well-understood and mutually supported across stakeholder categories.

## CF-064 — Integration of External Actors: Emergency Services and Infrastructure Engineers

- **Type:** OWNERSHIP_AMBIGUITY
- **Stakeholders:** railway_controller
- **Categories:** MISSING_ACTOR, FUNCTIONAL
- **Source refs:** RAILWAY_CONTROLLER-005, RAILWAY_CONTROLLER-006, RAILWAY_CONTROLLER-009
- **Summary:** External entities such as emergency services (life safety command) and infrastructure engineers (track access and repairs) play critical operational roles, but their required system access levels, integration protocols, and communication workflows are currently undefined.
- **Workshop required:** True
- **Unresolved question:** What direct system access, communication protocols, and track access logging mechanisms do emergency services and infrastructure engineers require?
- **Rationale:** Clarifying how external safety-critical actors interact with SmartRail is essential for safety compliance and system design.

## CF-065 — On-board Ticket Purchasing and Fare Management Scope Exclusion

- **Type:** UNIQUE_IMPORTANT_FINDING
- **Stakeholders:** railway_controller
- **Categories:** SCOPE_CANDIDATE
- **Source refs:** RAILWAY_CONTROLLER-002
- **Summary:** Passenger management and alternative travel options are discussed, but on-board ticket purchasing functions are not part of network controller workflows or core incident management.
- **Workshop required:** False
- **Unresolved question:** None
- **Rationale:** Represents a distinct edge-case scope inference that must be explicitly tracked to prevent feature creep.

## CF-066 — Indoor Station Navigation Scope Exclusion

- **Type:** UNIQUE_IMPORTANT_FINDING
- **Stakeholders:** railway_controller
- **Categories:** SCOPE_CANDIDATE
- **Source refs:** RAILWAY_CONTROLLER-003
- **Summary:** While station staff manage passenger flow and platform changes during incidents, specialized indoor navigation systems for passengers were not included in operational requirements.
- **Workshop required:** False
- **Unresolved question:** None
- **Rationale:** Captures a distinct passenger-facing capability boundary identified during controller interviews.

## CF-067 — Advanced Passenger Seat Reservations Scope Exclusion

- **Type:** UNIQUE_IMPORTANT_FINDING
- **Stakeholders:** railway_controller
- **Categories:** SCOPE_CANDIDATE
- **Source refs:** RAILWAY_CONTROLLER-004
- **Summary:** Short-forming and train cancellations affect capacity, but granular seat reservations fall outside the network controller's core toolset and initial system scope.
- **Workshop required:** False
- **Unresolved question:** None
- **Rationale:** Ensures seat reservation features are consciously separated from operational network disruption management.

## CF-068 — Infrastructure, Weather, and Station Health Monitoring

- **Type:** AGREEMENT
- **Stakeholders:** railway_controller, station_manager
- **Categories:** FUNCTIONAL
- **Source refs:** RAILWAY_CONTROLLER-011, STATION_MANAGER-003, STATION_MANAGER-004
- **Summary:** Railway controllers and station managers both require real-time visibility into track conditions, signalling faults, power supply, engineering works, live weather updates, and station accessibility facilities such as lifts and escalators.
- **Workshop required:** False
- **Unresolved question:** None
- **Rationale:** Direct alignment between controllers and station staff on the need for real-time operational status dashboards.

## CF-069 — Real-Time Train Manifests and Journey States

- **Type:** OVERLAP
- **Stakeholders:** railway_controller, station_manager
- **Categories:** FUNCTIONAL, STATE
- **Source refs:** RAILWAY_CONTROLLER-012, STATION_MANAGER-002
- **Summary:** Station managers expect a real-time manifest of early trains and pre-planned changes, while controllers highlight the need to track detailed journey states like incident-affected, stationary, awaiting recovery, and evacuation underway.
- **Workshop required:** False
- **Unresolved question:** None
- **Rationale:** Both findings concern the tracking and display of train operational states and schedules, looking at it from station operations and network control perspectives respectively.

## CF-070 — Platform Changes and Station Staff Resistance

- **Type:** CONFLICT
- **Stakeholders:** railway_controller
- **Categories:** CONFLICT, ACCESSIBILITY
- **Source refs:** RAILWAY_CONTROLLER-015, RAILWAY_CONTROLLER-020
- **Summary:** Controller-initiated platform changes can conflict with station staff concerns regarding accessibility, overcrowding, and insufficient notification time, posing safety and passenger experience issues.
- **Workshop required:** True
- **Unresolved question:** How should the system handle a veto or warning raised by station staff against a controller-initiated platform change?
- **Rationale:** Direct operational conflict between automated or controller-led routing and station-level constraints/accessibility requirements.

## CF-071 — Operational vs Commercial and Priority Trade-Offs

- **Type:** CONFLICT
- **Stakeholders:** railway_controller
- **Categories:** CONFLICT, AMBIGUITY
- **Source refs:** RAILWAY_CONTROLLER-016, RAILWAY_CONTROLLER-017, RAILWAY_CONTROLLER-024
- **Summary:** Competing priorities exist between TOC service maintenance, infrastructure safety, freight service priorities versus passenger diversions, and commercial versus operational cancellation boundaries.
- **Workshop required:** True
- **Unresolved question:** Can an operational cancellation be triggered automatically if a TOC fails to confirm resource availability by a specific threshold time?
- **Rationale:** Groups multiple stakeholder and priority conflicts regarding conflicting resource demands, safety obligations, and commercial decisions.

## CF-072 — Late Cancellation Notifications and Passenger Impact

- **Type:** OPEN_QUESTION
- **Stakeholders:** railway_controller
- **Categories:** FAILURE_SCENARIO
- **Source refs:** RAILWAY_CONTROLLER-013
- **Summary:** Late notification of cancellations leads to passengers waiting unnecessarily, raising questions on automated alerts to reduce the notification window.
- **Workshop required:** True
- **Unresolved question:** What automated alerts can reduce the window between a cancellation decision and passenger notification?
- **Rationale:** Highlights a critical failure scenario centered around timing and communication gaps.

## CF-073 — Unclear Authority and Manual Override Procedures

- **Type:** OWNERSHIP_AMBIGUITY
- **Stakeholders:** railway_controller, station_manager
- **Categories:** FAILURE_SCENARIO, AMBIGUITY
- **Source refs:** RAILWAY_CONTROLLER-014, STATION_MANAGER-006
- **Summary:** Unclear authority during cancellations, data discrepancies between system predictions and visual reality, and the lack of a defined manual override procedure create ambiguity in operational decision-making.
- **Workshop required:** True
- **Unresolved question:** What is the formal procedure and audit trail for overriding incorrect automated system predictions?
- **Rationale:** Combines issues concerning authority, command hierarchies, and manual corrections when system predictions mismatch reality.

## CF-074 — Strict Audit Logging and Comprehensive Data Archiving

- **Type:** AGREEMENT
- **Stakeholders:** railway_controller
- **Categories:** SECURITY
- **Source refs:** RAILWAY_CONTROLLER-018, RAILWAY_CONTROLLER-019
- **Summary:** All decisions, instructions, authorizations, communications, and real-time transit data must be logged with precise timestamps and identities for later analysis and security compliance.
- **Workshop required:** False
- **Unresolved question:** None
- **Rationale:** Consistently aligned requirements around audit trails, logging, and data archiving.

## CF-075 — Passenger Information Systems Integration and Business Process Flows

- **Type:** AGREEMENT
- **Stakeholders:** railway_controller
- **Categories:** INTEGRATION, BUSINESS_PROCESS
- **Source refs:** RAILWAY_CONTROLLER-021, RAILWAY_CONTROLLER-022
- **Summary:** System updates must integrate with Passenger Information Systems, station staff terminals, and respect the multi-party operational authorization flow involving controllers, signallers, staff, drivers, and TOCs.
- **Workshop required:** False
- **Unresolved question:** None
- **Rationale:** Both relate to core integration channels and multi-party stakeholder communication workflows.

## CF-076 — Automated Driver Route Competence Verification

- **Type:** SCOPE_TENSION
- **Stakeholders:** railway_controller
- **Categories:** ASSUMPTION
- **Source refs:** RAILWAY_CONTROLLER-023
- **Summary:** Assumption that drivers are verified for route competence prior to authorization raises scope questions regarding whether route competence should be tracked automatically within the system.
- **Workshop required:** True
- **Unresolved question:** Is driver route competence tracked automatically within the system or verified manually?
- **Rationale:** Introduces an external competency check that could significantly expand system integration scope.

## CF-077 — Station Pre-Opening Physical Walk-Through

- **Type:** UNIQUE_IMPORTANT_FINDING
- **Stakeholders:** station_manager
- **Categories:** BUSINESS_PROCESS
- **Source refs:** STATION_MANAGER-001
- **Summary:** Station staff execute a manual pre-opening walk-through procedure to inspect physical infrastructure, ticket gates, amenities, and print out daily schedules.
- **Workshop required:** False
- **Unresolved question:** None
- **Rationale:** Important foundational station operations process that must be understood in system design.

## CF-078 — Input Mechanism for Locally Discovered Station Issues

- **Type:** OPEN_QUESTION
- **Stakeholders:** station_manager
- **Categories:** AMBIGUITY
- **Source refs:** STATION_MANAGER-005
- **Summary:** It is currently ambiguous how station staff can input unprompted local incidents or infrastructure failures (e.g., broken ticket machine) into SmartRail for network-wide visibility.
- **Workshop required:** True
- **Unresolved question:** Is there a dedicated local station status module for staff to input unprompted local incidents or infrastructure failures?
- **Rationale:** Unique discovery regarding input mechanisms for station-specific faults that lacks a defined feature mapping.

## CF-079 — Platform Allocation and Network Flow vs Local Safety

- **Type:** CONFLICT
- **Stakeholders:** station_manager
- **Categories:** CONFLICT, OPERATIONAL
- **Source refs:** STATION_MANAGER-007, STATION_MANAGER-017
- **Summary:** SmartRail may propose network optimization platform changes that conflict with local safety, capacity constraints, or blocked platforms, requiring station managers to have veto or challenge authority.
- **Workshop required:** True
- **Unresolved question:** Who has ultimate authority to override platform allocation disputes between local safety constraints and network optimization algorithms?
- **Rationale:** Both sources address the tension between central network optimization and local platform safety/overcrowding, highlighting a direct operational conflict.

## CF-080 — Division of Authority for Platform Changes and Major Actions

- **Type:** OWNERSHIP_AMBIGUITY
- **Stakeholders:** station_manager
- **Categories:** OPERATIONAL
- **Source refs:** STATION_MANAGER-008, STATION_MANAGER-016
- **Summary:** Clear boundaries are needed between local operational authority for single train adjustments and central authorization required for major line-wide re-shuffles and station closures.
- **Workshop required:** False
- **Unresolved question:** None
- **Rationale:** These discoveries complement each other by outlining the spectrum of authority delegation between local and central control.

## CF-081 — Automated PA Announcement Capabilities and Error Handling

- **Type:** OPEN_QUESTION
- **Stakeholders:** station_manager
- **Categories:** AMBIGUITY, FAILURE_SCENARIO
- **Source refs:** STATION_MANAGER-009, STATION_MANAGER-010
- **Summary:** Uncertainty surrounds whether the automated PA system supports ad-hoc typing or pre-recorded messages, and how the system prevents or corrects panic-inducing announcement inaccuracies.
- **Workshop required:** True
- **Unresolved question:** Can staff broadcast ad-hoc or pre-recorded messages, and does the system feature automated verification for announcements?
- **Rationale:** Both findings focus on the functionality and risk profile of automated passenger announcements.

## CF-082 — Local Control of Station Displays and Messaging

- **Type:** OPEN_QUESTION
- **Stakeholders:** station_manager
- **Categories:** AMBIGUITY
- **Source refs:** STATION_MANAGER-011
- **Summary:** It is unclear if station displays are entirely network-controlled or if local staff can utilize portions of displays for local operational or informational messages.
- **Workshop required:** True
- **Unresolved question:** Can local staff override sections of network-controlled station displays to publish local operational messages?
- **Rationale:** Unique finding regarding local management of customer-facing visual displays.

## CF-083 — Direct Incident Entry and Infrastructure Repair Loop Closure

- **Type:** UNIQUE_IMPORTANT_FINDING
- **Stakeholders:** station_manager
- **Categories:** FUNCTIONAL, AMBIGUITY
- **Source refs:** STATION_MANAGER-012, STATION_MANAGER-013
- **Summary:** Staff require capabilities to directly log facility outages and incidents into SmartRail, alongside a defined workflow to ensure maintenance issues are resolved and status is updated.
- **Workshop required:** True
- **Unresolved question:** What is the formal workflow to close the loop on reported infrastructure failures and update their operational status?
- **Rationale:** Combines direct incident logging with the lifecycle maintenance tracking and loop closure requirements.

## CF-084 — Tiered Incident Management Workflows

- **Type:** OPEN_QUESTION
- **Stakeholders:** station_manager
- **Categories:** AMBIGUITY
- **Source refs:** STATION_MANAGER-014
- **Summary:** It is unspecified how SmartRail manages and distinguishes between varying levels of local incidents, such as minor platform spills versus major security alerts.
- **Workshop required:** True
- **Unresolved question:** Does SmartRail contain distinct tiered incident management workflows for varying levels of local severity?
- **Rationale:** Unique finding addressing severity-based workflow categorizations for local incidents.

## CF-085 — Local Offline Resilience and Connectivity Continuity

- **Type:** UNIQUE_IMPORTANT_FINDING
- **Stakeholders:** station_manager
- **Categories:** FAILURE_SCENARIO
- **Source refs:** STATION_MANAGER-015
- **Summary:** The system must provide a robust offline mode with backup power support to maintain core local functionality if network connections or power fail during major disruptions.
- **Workshop required:** True
- **Unresolved question:** What core functionality can be maintained locally via backup power and offline cache if network connectivity is completely severed?
- **Rationale:** Unique resilience requirement focusing on complete loss of local infrastructure connectivity.

## CF-086 — Granular Disruption Information Requirements for Staff

- **Type:** AGREEMENT
- **Stakeholders:** station_manager
- **Categories:** FUNCTIONAL
- **Source refs:** STATION_MANAGER-018
- **Summary:** Staff require precise operational details during disruptions—such as affected trains, recovery plans, alternative transport locations, and resolution times—rather than generic delay notices.
- **Workshop required:** False
- **Unresolved question:** None
- **Rationale:** Represents a clear functional expectation for disruption communication tools.

## CF-087 — Integration with Local Safety Systems

- **Type:** UNIQUE_IMPORTANT_FINDING
- **Stakeholders:** station_manager
- **Categories:** INTEGRATION
- **Source refs:** STATION_MANAGER-019
- **Summary:** SmartRail needs integration with local security assets including CCTV and access control to support effective station incident response.
- **Workshop required:** False
- **Unresolved question:** None
- **Rationale:** Unique integration requirement concerning physical security systems.

## CF-088 — Automatic Ingestion of Local Context in Network Algorithms

- **Type:** SCOPE_TENSION
- **Stakeholders:** station_manager
- **Categories:** ASSUMPTION
- **Source refs:** STATION_MANAGER-020
- **Summary:** There is an implicit assumption that network-wide optimization solutions must automatically incorporate local station context, density, and safety metrics without requiring manual intervention.
- **Workshop required:** True
- **Unresolved question:** To what extent can network optimization algorithms ingest real-time local station density metrics automatically?
- **Rationale:** Highlights a major scope tension regarding automated data ingestion versus manual oversight.

# Traceability Validation

PASS — All original source discoveries are represented.