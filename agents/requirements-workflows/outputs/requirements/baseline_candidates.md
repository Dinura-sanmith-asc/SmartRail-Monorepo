# SmartRail Baseline Requirement Candidates — Complete Reviewed Edition

> **Source:** *final-assessment-scoped 2.md* (28 numbered sections). This is a faithful baseline extraction, **not** the final refined requirements or approved MVP scope. Added items are traceable to the original brief, not to simulated stakeholder opinions.

**Source sections:** 28  
**Original candidates preserved:** 116 (BASE-001–BASE-116)  
**Additional atomic/source-specific candidates:** 176 (BASE-117–BASE-292)  
**Total extracted baseline candidates:** 292  
**Uncovered numbered sections:** 0

## How to Interpret This File

- **EXPLICIT:** The assessment clearly requests an action, capability or submission item. Explicit does not always mean that every example must be implemented in the MVP.
- **TO_BE_DEFINED:** The assessment directs the engineer to define, investigate or choose a still-unspecified capability or rule; its detailed implementation is **not** supplied in the brief.
- **ILLUSTRATIVE:** The assessment uses wording such as *may*, *for example*, *possible*, *could include*, or *such as*. These are candidates/context, **not mandatory features**.
- **PRODUCT_*** classifications concern product capabilities, data, integrations or qualities; **ASSESSMENT_DELIVERABLE** is evidence/submission work; **ENGINEERING_PROCESS** is an engineering activity or assessed practice.
- Original grouped candidates remain for backward traceability. New atomic candidates deliberately overlap some grouped summaries so individual capabilities and deliverables cannot be silently lost. **Deduplicate at the next requirements-consolidation stage.**
- Exact technical values (latency, refresh intervals, cache lifetimes, permissions, station coverage) are **not invented** where the brief leaves them unspecified.

## Important Scope Clarifications from the Source

1. **Station experience:** Section 23 requires a functional prototype of passenger-information displays, interactive terminals, **or both**. A terminal is a possible feature in Section 7, not an independently compulsory station implementation.
2. **Five app categories:** Section 15 says these *may include* five application types. The separate mandatory implementation evidence in Section 23 is a backend, a meaningful operations web experience, passenger mobile, operational mobile/equivalent, station prototype, and simulated external catering integration.
3. **Passenger accounts/public information:** Section 14 says public access *may* be provided and *some* passenger functionality *may* need an account. Driver authentication and organizational access for railway management are stated more strongly.
4. **Examples vs requirements:** Individual live-location options, kiosk functions, possible operational roles and other examples are recorded as **ILLUSTRATIVE**, not automatically made mandatory.
5. **Individual scope:** Section 25 explicitly allows a smaller coherent scope, but warns against reducing the assessment to a single application.
6. **Assessment readiness:** Section 28 lists things the reviewer *may* ask you to do. These are possible evaluation exercises, not nine separate application features or submission documents.

---

## Candidate Catalogue

## BASE-001 — Independent Solution Design and Build

**Source:** BRIEF-1
**Classification:** ENGINEERING_PROCESS
**Strength:** EXPLICIT

Independently design and build a solution for a fictional public railway transportation organization as an individual assessment.

**Evidence:** you are expected to independently design and build a solution for a fictional public railway transportation organization. This is an individual assessment.

**Clarification:** None

## BASE-002 — Requirements Discovery and Refinement

**Source:** BRIEF-1
**Classification:** ENGINEERING_PROCESS
**Strength:** EXPLICIT

Discover and refine requirements from the initial business problem and identify missing information and assumptions.

**Evidence:** Understand an initial business problem. Discover and refine requirements. Identify missing information and assumptions.

**Clarification:** What mechanisms or processes should be used to document discovered requirements and assumptions?

## BASE-003 — AI Assistant Utilization

**Source:** BRIEF-1
**Classification:** ENGINEERING_PROCESS
**Strength:** EXPLICIT

Use AI assistants and agents effectively throughout the development lifecycle.

**Evidence:** Use AI assistants and agents effectively throughout the development lifecycle.

**Clarification:** Are there any specific logging or reporting requirements regarding how AI assistants were utilized?

## BASE-004 — User Experience Design

**Source:** BRIEF-1
**Classification:** ENGINEERING_PROCESS
**Strength:** EXPLICIT

Design appropriate and usable experiences for the people interacting with SmartRail applications.

**Evidence:** Design appropriate user experiences.

**Clarification:** Are there specific design systems, branding guidelines, or accessibility standards to follow?

## BASE-005 — Software Architecture Design

**Source:** BRIEF-1
**Classification:** ENGINEERING_PROCESS
**Strength:** EXPLICIT

Design a maintainable software architecture.

**Evidence:** Design a maintainable software architecture.

**Clarification:** Are there preferred technology stacks or architectural patterns required for the backend, web, and mobile solutions?

## BASE-006 — Multi-Tier Solution Implementation

**Source:** BRIEF-1
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

Implement backend, web, and mobile solutions.

**Evidence:** Implement backend, web, and mobile solutions.

**Clarification:** None

## BASE-007 — External System Integration

**Source:** BRIEF-1
**Classification:** PRODUCT_INTEGRATION
**Strength:** EXPLICIT

Integrate with external systems.

**Evidence:** Integrate with external systems.

**Clarification:** What specific external systems need to be integrated, and what are their interface specifications?

## BASE-008 — Quality Assurance Practices

**Source:** BRIEF-1
**Classification:** ENGINEERING_PROCESS
**Strength:** EXPLICIT

Establish appropriate quality assurance practices.

**Evidence:** Establish appropriate quality assurance practices.

**Clarification:** What is the expected test coverage or specific testing frameworks to be used?

## BASE-009 — Non-Functional Engineering Considerations

**Source:** BRIEF-1
**Classification:** PRODUCT_NON_FUNCTIONAL
**Strength:** EXPLICIT

Consider security, reliability, accessibility, observability, and operational requirements in the solution.

**Evidence:** Consider security, reliability, accessibility, observability, and operational requirements.

**Clarification:** What are the specific quantitative targets for reliability and security compliance?

## BASE-010 — Technical Decision Justification

**Source:** BRIEF-1
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

Make technical decisions and justify them.

**Evidence:** Make technical decisions and justify them.

**Clarification:** In what format should technical decisions and justifications be documented?

## BASE-011 — Modernize Passenger Experience and Operations

**Source:** BRIEF-2
**Classification:** PRODUCT_FUNCTIONAL
**Strength:** EXPLICIT

Provide a digital platform called SmartRail to modernize the passenger experience and improve visibility into railway operations.

**Evidence:** The organization wants to modernize the passenger experience and improve visibility into railway operations through a new digital platform called SmartRail.

**Clarification:** None

## BASE-012 — Passenger Information Provision

**Source:** BRIEF-2
**Classification:** PRODUCT_DATA
**Strength:** EXPLICIT

Provide passengers with accurate information about train services, routes, stations, upcoming trains, expected arrival and departure times, current train locations, and service disruptions.

**Evidence:** The platform should provide passengers with accurate information about: Train services. Routes. Stations. Upcoming trains. Expected arrival and departure times. Current train locations where appropriate. Service disruptions and changes.

**Clarification:** None

## BASE-013 — Multi-User System Digital Capabilities

**Source:** BRIEF-2
**Classification:** PRODUCT_FUNCTIONAL
**Strength:** EXPLICIT

Provide digital systems for passengers, train drivers and operational staff, railway stations, central railway operations, and external service providers.

**Evidence:** The organization also wants digital systems for: Passengers. Train drivers and operational staff. Railway stations. Central railway operations. External service providers.

**Clarification:** Which of these multi-user systems are in-scope for implementation versus high-level design?

## BASE-014 — Multi-Application Architecture

**Source:** BRIEF-2
**Classification:** PRODUCT_FUNCTIONAL
**Strength:** EXPLICIT

Deliver multiple applications and services rather than a single application as the complete solution.

**Evidence:** The complete solution will therefore contain multiple applications and services rather than being a single application.

**Clarification:** None

## BASE-015 — Passenger Mobile Application

**Source:** BRIEF-3
**Classification:** PRODUCT_FUNCTIONAL
**Strength:** EXPLICIT

Provide a mobile application that helps passengers understand the railway network and their journeys.

**Evidence:** Passengers should have access to a mobile application that helps them understand the railway network and their journeys.

**Clarification:** Should the mobile application target iOS, Android, or both?

## BASE-016 — Passenger Journey Inquiry Support

**Source:** BRIEF-3
**Classification:** PRODUCT_FUNCTIONAL
**Strength:** ILLUSTRATIVE

Investigate passenger-facing capabilities suggested by the brief, including train choice, next services, running status, location, stops, arrival estimates, disruptions, platform/timing changes and connections.

**Evidence:** A passenger may want to understand questions such as: Which train should I take? When is the next train? Is my train currently running on time? Where is the train now? Which stations will the train stop at? When will it reach my station? Are there service disruptions? Has the platform or departure time changed? What are the available connections from another station?

**Clarification:** None

## BASE-017 — Intuitive Passenger Experience

**Source:** BRIEF-3
**Classification:** PRODUCT_NON_FUNCTIONAL
**Strength:** EXPLICIT

Provide an intuitive mobile application experience suitable for everyday commuters as well as occasional travelers.

**Evidence:** The passenger application should provide an intuitive experience suitable for everyday commuters as well as occasional travelers.

**Clarification:** None

## BASE-018 — Determine Passenger Information and Functionality

**Source:** BRIEF-3
**Classification:** PRODUCT_FUNCTIONAL
**Strength:** TO_BE_DEFINED

Determine what information and functionality would be valuable to passengers.

**Evidence:** You should determine what information and functionality would be valuable to passengers.

**Clarification:** What specific user research or feature prioritization criteria should be applied to determine valuable passenger functionality?

## BASE-019 — Domain Model Representation

**Source:** BRIEF-4
**Classification:** ENGINEERING_PROCESS
**Strength:** TO_BE_DEFINED

Determine an appropriate domain model for representing the railway network and its operations.

**Evidence:** You should determine an appropriate domain model for representing the railway network and its operations.

**Clarification:** What are the specific bounds and persistence expectations for the railway network domain model?

## BASE-020 — Live Train Journey Information

**Source:** BRIEF-5
**Classification:** PRODUCT_FUNCTIONAL
**Strength:** EXPLICIT

Provide passengers with information about active train journeys including current progress.

**Evidence:** An important part of the system is providing passengers with information about active train journeys.

**Clarification:** None

## BASE-021 — Live Information Mechanism

**Source:** BRIEF-5
**Classification:** ENGINEERING_PROCESS
**Strength:** TO_BE_DEFINED

Investigate and design the mechanism used to obtain, calculate, communicate, and update live information.

**Evidence:** The mechanism used to obtain, calculate, communicate, and update live information should be investigated and designed by you.

**Clarification:** What are the source data feeds and latency requirements for live train information updates?

## BASE-022 — Digital Passenger-Information Displays

**Source:** BRIEF-6
**Classification:** PRODUCT_FUNCTIONAL
**Strength:** EXPLICIT

Include digital passenger-information displays in the proposed railway-station experience, with concrete locations and information chosen during design.

**Evidence:** Railway stations should contain digital passenger-information displays.

**Clarification:** None

## BASE-023 — Display Information Prioritization

**Source:** BRIEF-6
**Classification:** ENGINEERING_PROCESS
**Strength:** TO_BE_DEFINED

Investigate what information is most important for digital displays in different locations.

**Evidence:** However, you should investigate what information is most important in different locations.

**Clarification:** What stakeholder requirements define the specific information needs for each display location type?

## BASE-024 — Display Connectivity and Resiliency

**Source:** BRIEF-6
**Classification:** PRODUCT_NON_FUNCTIONAL
**Strength:** EXPLICIT

Consider how displays receive updated information and how they behave when connectivity is unreliable.

**Evidence:** The solution should consider how these displays receive updated information and how they behave when connectivity is unreliable.

**Clarification:** What fallback behavior or cached information must displays show during network outages?

## BASE-025 — Possible Interactive Station Terminal Experience

**Source:** BRIEF-7
**Classification:** PRODUCT_FUNCTIONAL
**Strength:** ILLUSTRATIVE

Selected railway stations may include interactive public terminals that let passengers explore railway information without installing the mobile app; a terminal is not separately mandatory when the assessment allows a station-display prototype instead.

**Evidence:** selected railway stations may contain interactive public terminals. These terminals should allow passengers to explore railway information without requiring them to install the passenger mobile application.

**Clarification:** None

## BASE-026 — Station Terminal Deployment

**Source:** BRIEF-7
**Classification:** PRODUCT_NON_FUNCTIONAL
**Strength:** EXPLICIT

If an interactive station terminal is chosen, design it for deployment on dedicated station devices.

**Evidence:** The terminal should be designed as an application suitable for deployment on dedicated station devices.

**Clarification:** None

## BASE-027 — Station Terminal Technology and Interaction Model

**Source:** BRIEF-7
**Classification:** ENGINEERING_PROCESS
**Strength:** TO_BE_DEFINED

Determine the appropriate technology and interaction model for the station terminal application.

**Evidence:** You should determine the appropriate technology and interaction model.

**Clarification:** What are the specific hardware constraints, accessibility standards, and preferred technology stacks for dedicated station devices?

## BASE-028 — Driver and Train Operator Application Access

**Source:** BRIEF-8
**Classification:** PRODUCT_FUNCTIONAL
**Strength:** EXPLICIT

Provide train drivers and relevant operational personnel with access to a dedicated application supporting their operational responsibilities during a train journey.

**Evidence:** Train drivers or relevant train operational personnel should have access to a dedicated application. The application should support their operational responsibilities during a train journey.

**Clarification:** None

## BASE-029 — Driver Application Scope and Permissions

**Source:** BRIEF-8
**Classification:** ENGINEERING_PROCESS
**Strength:** TO_BE_DEFINED

Investigate and define the information required by drivers and operators, and specify which information they should be permitted to update.

**Evidence:** You should investigate what information the driver or train operator would reasonably require and which information they should be permitted to update.

**Clarification:** What are the regulatory and safety requirements governing what data a train operator is permitted to modify while on duty?

## BASE-030 — Railway Operations Platform

**Source:** BRIEF-9
**Classification:** PRODUCT_FUNCTIONAL
**Strength:** EXPLICIT

Provide a web-based system for managing and overseeing railway operations that allows authorized users to understand what is currently happening across the railway network.

**Evidence:** The railway organization requires a web-based system for managing and overseeing railway operations. The operations platform should allow authorized users to understand what is currently happening across the railway network.

**Clarification:** None

## BASE-031 — Operations Platform Roles and Responsibilities

**Source:** BRIEF-9
**Classification:** PRODUCT_FUNCTIONAL
**Strength:** TO_BE_DEFINED

Identify appropriate user roles and responsibilities for the railway operations platform, supporting multiple access levels beyond a single administrator role.

**Evidence:** The users of this system should not simply be treated as a single Administrator role... You should identify appropriate user roles and responsibilities.

**Clarification:** What is the complete organizational hierarchy and required permission matrix for each operational department?

## BASE-032 — Disruption Handling and Communication

**Source:** BRIEF-10
**Classification:** PRODUCT_FUNCTIONAL
**Strength:** EXPLICIT

The system must provide an appropriate mechanism for handling disruptions and communicating relevant information to affected users.

**Evidence:** The system should provide an appropriate mechanism for handling disruptions and communicating relevant information to affected users.

**Clarification:** None

## BASE-033 — Disruption Information Propagation

**Source:** BRIEF-10
**Classification:** ENGINEERING_PROCESS
**Strength:** TO_BE_DEFINED

Determine how disruption information propagates through the different applications.

**Evidence:** You should determine how disruption information propagates through the different applications.

**Clarification:** How should disruption information propagate through the different applications?

## BASE-034 — External Catering Platform Integration

**Source:** BRIEF-11
**Classification:** PRODUCT_INTEGRATION
**Strength:** EXPLICIT

SmartRail must integrate with the external catering platform.

**Evidence:** SmartRail must integrate with this external platform.

**Clarification:** None

## BASE-035 — Catering Integration Data Provision

**Source:** BRIEF-11
**Classification:** PRODUCT_DATA
**Strength:** ILLUSTRATIVE

Investigate which railway data the external catering platform needs, such as services, routes, stations, scheduled or estimated arrival times and service changes.

**Evidence:** The catering provider may require railway information such as: Train services. Train routes. Station information. Scheduled arrival times. Actual or estimated arrival times. Service changes.

**Clarification:** None

## BASE-036 — Food Options Discovery

**Source:** BRIEF-12
**Classification:** PRODUCT_FUNCTIONAL
**Strength:** EXPLICIT

Passengers using SmartRail must be able to discover available food options associated with their journey.

**Evidence:** Passengers using SmartRail should be able to discover available food options associated with their journey.

**Clarification:** None

## BASE-037 — Food Ordering and Collection Arrangement

**Source:** BRIEF-12
**Classification:** PRODUCT_FUNCTIONAL
**Strength:** EXPLICIT

Passengers must be able to select food and arrange to receive or collect the order at an appropriate point during the journey.

**Evidence:** A passenger should be able to select food and arrange to receive or collect the order at an appropriate point during the journey.

**Clarification:** None

## BASE-038 — Investigate Catering Ordering and Fulfillment Workflow

**Source:** BRIEF-12
**Classification:** ENGINEERING_PROCESS
**Strength:** TO_BE_DEFINED

Investigate questions regarding the ordering and fulfillment workflow, including station support, order placement timing, modification rules, delay handling, missed collections, cancellations, payment processing ownership, menu ownership, availability changes, order association, and collection confirmation.

**Evidence:** You should investigate questions such as: Which stations support catering? When can an order be placed? How close to arrival can an order be modified? What happens if a train is delayed? What happens if the passenger misses the collection station? Can an order be cancelled? Who owns payment processing? Who owns the authoritative food menu? How are food availability changes communicated? How are orders associated with passengers and journeys? How is successful collection confirmed?

**Clarification:** What are the specific operational rules and ownership boundaries for the catering ordering and fulfillment workflow?

## BASE-039 — User Event Notifications

**Source:** BRIEF-13
**Classification:** PRODUCT_FUNCTIONAL
**Strength:** TO_BE_DEFINED

Determine which important events require notifications, which users receive them and how they are delivered; the brief leaves notification scope and mechanisms unspecified.

**Evidence:** Different users may need to receive information when important events occur.

**Clarification:** What specific events trigger notifications, and through which channels must they be delivered?

## BASE-040 — Publicly Accessible Railway Information

**Source:** BRIEF-14
**Classification:** PRODUCT_FUNCTIONAL
**Strength:** ILLUSTRATIVE

Some railway information may be accessible without authentication; determine the public-access boundary.

**Evidence:** Some railway information may be publicly accessible.

**Clarification:** None

## BASE-041 — Passenger Account Authentication

**Source:** BRIEF-14
**Classification:** PRODUCT_FUNCTIONAL
**Strength:** ILLUSTRATIVE

Some passenger functionality may require an account; determine which features require passenger authentication.

**Evidence:** Some passenger functionality may require an account.

**Clarification:** None

## BASE-042 — Driver Application Authentication

**Source:** BRIEF-14
**Classification:** PRODUCT_FUNCTIONAL
**Strength:** EXPLICIT

Driver applications must require authentication for operational users.

**Evidence:** Driver applications require authenticated operational users.

**Clarification:** None

## BASE-043 — Railway Management Organizational Access

**Source:** BRIEF-14
**Classification:** PRODUCT_FUNCTIONAL
**Strength:** EXPLICIT

Railway management functionality must require organizational access.

**Evidence:** Railway management functionality requires organizational access.

**Clarification:** None

## BASE-044 — Role-Based Permissions

**Source:** BRIEF-14
**Classification:** PRODUCT_FUNCTIONAL
**Strength:** TO_BE_DEFINED

Determine the different organizational permissions needed across operational roles; the brief gives role differentiation as an expected design consideration.

**Evidence:** Different organizational roles may require different permissions.

**Clarification:** None

## BASE-045 — Identity and Authorization Model Definition

**Source:** BRIEF-14
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** TO_BE_DEFINED

Define an appropriate identity and authorization model covering the authentication and permission requirements.

**Evidence:** You should define an appropriate identity and authorization model.

**Clarification:** What are the specific identity provider integrations and compliance requirements for the authorization model?

## BASE-046 — Application Suite Identification

**Source:** BRIEF-15
**Classification:** PRODUCT_FUNCTIONAL
**Strength:** ILLUSTRATIVE

Consider five example application categories: passenger mobile, driver/operator, station terminal, station display and railway-operations web; do not interpret the illustrative list as five independently mandatory implementation deliverables.

**Evidence:** These may include: 1. Passenger Mobile Application 2. Driver / Operator Application 3. Station Terminal Application 4. Station Information Displays 5. Railway Operations Web Application

**Clarification:** Are there any mandatory applications out of this list that must be delivered in the initial scope?

## BASE-047 — Design System and Language Strategy

**Source:** BRIEF-15
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** TO_BE_DEFINED

Determine whether and how the multiple applications share a common design language.

**Evidence:** You should determine whether and how these applications share a common design language.

**Clarification:** What are the technical constraints or target platforms for establishing a shared design system?

## BASE-048 — Passenger Experience in Busy Stations

**Source:** BRIEF-15
**Classification:** PRODUCT_NON_FUNCTIONAL
**Strength:** EXPLICIT

Design experiences that passengers can understand quickly while moving through a busy railway station.

**Evidence:** Special attention should be paid to designing experiences that passengers can understand quickly while moving through a busy railway station.

**Clarification:** Are there specific accessibility or cognitive load guidelines that must be met for station environments?

## BASE-049 — Unreliable Network Environments

**Source:** BRIEF-16
**Classification:** PRODUCT_NON_FUNCTIONAL
**Strength:** TO_BE_DEFINED

Determine how relevant applications behave under conditions of unreliable network connectivity.

**Evidence:** You should determine how relevant applications behave under these conditions.

**Clarification:** What specific behaviors or offline modes are required for applications during network outages?

## BASE-050 — Lifecycle Security Integration

**Source:** BRIEF-17
**Classification:** ENGINEERING_PROCESS
**Strength:** EXPLICIT

Incorporate security throughout the development lifecycle rather than only after development completion.

**Evidence:** Security should be incorporated throughout the development lifecycle.

**Clarification:** None

## BASE-051 — Structured Quality Engineering Approach

**Source:** BRIEF-18
**Classification:** ENGINEERING_PROCESS
**Strength:** EXPLICIT

Demonstrate a structured quality engineering approach for the project.

**Evidence:** The project should demonstrate a structured quality engineering approach.

**Clarification:** None

## BASE-052 — Test Selection and Justification

**Source:** BRIEF-18
**Classification:** ENGINEERING_PROCESS
**Strength:** TO_BE_DEFINED

Decide which tests provide the most value and justify the selected approach.

**Evidence:** You should decide which tests provide the most value and justify the selected approach.

**Clarification:** What specific criteria or stakeholder requirements should be used to determine test value and justify the approach?

## BASE-053 — Platform Operation Capability

**Source:** BRIEF-19
**Classification:** PRODUCT_NON_FUNCTIONAL
**Strength:** EXPLICIT

The railway organization must be capable of operating the platform in production.

**Evidence:** The railway organization must be capable of operating the platform in production.

**Clarification:** None

## BASE-054 — Data Collection Identification and Classification

**Source:** BRIEF-20
**Classification:** ENGINEERING_PROCESS
**Strength:** EXPLICIT

Identify what information the platform collects and classify the data appropriately.

**Evidence:** You must identify what information the platform collects and classify the data appropriately.

**Clarification:** None

## BASE-055 — Data Privacy and Handling Documentation

**Source:** BRIEF-20
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

Document important privacy and data-handling decisions.

**Evidence:** Important privacy and data-handling decisions should be documented.

**Clarification:** None

## BASE-056 — AI Usage Demonstration

**Source:** BRIEF-21
**Classification:** ENGINEERING_PROCESS
**Strength:** EXPLICIT

Demonstrate where AI was used, how generated output was validated, and where your own judgment changed or rejected AI suggestions.

**Evidence:** You should demonstrate where AI was used, how generated output was validated, and where your own judgment changed or rejected AI suggestions.

**Clarification:** None

## BASE-057 — Discover Missing Requirements and Gaps

**Source:** BRIEF-22
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

Identify missing actors, business processes, failure scenarios, states, security, operational, accessibility requirements, integration cases, conflicts, ambiguities, and important assumptions.

**Evidence:** You should identify: - Missing actors. - Missing business processes. - Missing failure scenarios. - Missing states. - Missing security requirements. - Missing operational requirements. - Missing accessibility requirements. - Missing integration cases. - Conflicting requirements. - Ambiguous requirements. - Important assumptions.

**Clarification:** What formal criteria will be used to measure the completeness of the discovered requirements?

## BASE-058 — Simulate Stakeholder Discussions Using AI Agents

**Source:** BRIEF-22
**Classification:** ENGINEERING_PROCESS
**Strength:** ILLUSTRATIVE

Use AI agents to simulate discussions with various railway roles such as passenger, driver, station manager, railway controller, customer-service representative, catering provider, security engineer, QA engineer, accessibility specialist, and product owner.

**Evidence:** You may use AI agents to simulate discussions with roles such as: - Passenger. - Driver. - Station manager. - Railway controller. - Customer-service representative. - Catering provider. - Security engineer. - QA engineer. - Accessibility specialist. - Product owner.

**Clarification:** Are specific AI agent frameworks or prompt logs required for submission?

## BASE-059 — Consolidate Discoveries into Refined Requirements

**Source:** BRIEF-22
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

Consolidate the discoveries from the requirement discovery challenge into refined requirements.

**Evidence:** You should consolidate these discoveries into your own refined requirements.

**Clarification:** None

## BASE-060 — Provide Requirements Documentation Artifacts

**Source:** BRIEF-23
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

Provide refined requirements, identified assumptions, user personas or actors, user journeys, user stories or equivalent requirements, acceptance criteria, and identified edge cases.

**Evidence:** Refined requirements. - Identified assumptions. - User personas or actors. - User journeys. - User stories or equivalent requirements. - Acceptance criteria. - Identified edge cases.

**Clarification:** None

## BASE-061 — Provide Product and UX Artifacts

**Source:** BRIEF-23
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

Provide user flows, wireframes, high-fidelity designs where appropriate, design-system definition, and accessibility considerations.

**Evidence:** User flows. - Wireframes. - High-fidelity designs where appropriate. - Design-system definition. - Accessibility considerations.

**Clarification:** None

## BASE-062 — Provide Architecture Documentation Artifacts

**Source:** BRIEF-23
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

Provide high-level solution architecture, application boundaries, major system components, data ownership, integration architecture, authentication and authorization approach, and important architecture decisions and trade-offs.

**Evidence:** High-level solution architecture. - Application boundaries. - Major system components. - Data ownership. - Integration architecture. - Authentication and authorization approach. - Important architecture decisions and trade-offs.

**Clarification:** None

## BASE-063 — Implement a Working Backend

**Source:** BRIEF-23
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

Implement a working backend demonstrating appropriate domain modeling, APIs, data persistence, validation, authorization, integration, error handling, and observability.

**Evidence:** A working backend demonstrating appropriate: - Domain modeling. - APIs. - Data persistence. - Validation. - Authorization. - Integration. - Error handling. - Observability.

**Clarification:** None

## BASE-064 — Implement a Web Application

**Source:** BRIEF-23
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

Implement a web-based operational experience demonstrating meaningful railway-management functionality.

**Evidence:** A web-based operational experience demonstrating meaningful railway-management functionality.

**Clarification:** None

## BASE-065 — Implement a Passenger Mobile Application

**Source:** BRIEF-23
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

Implement a mobile experience supporting important passenger journeys.

**Evidence:** A mobile experience supporting important passenger journeys.

**Clarification:** None

## BASE-066 — Implement an Operational Mobile Application

**Source:** BRIEF-23
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

Implement a mobile application or equivalent experience for train drivers or operational personnel.

**Evidence:** A mobile application or equivalent experience for train drivers or operational personnel.

**Clarification:** None

## BASE-067 — Implement a Station Experience Prototype

**Source:** BRIEF-23
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

Implement a functional prototype representing station passenger-information displays, interactive station terminals, or both.

**Evidence:** A functional prototype representing either or both: - Station passenger-information displays. - Interactive station terminals.

**Clarification:** None

## BASE-068 — Implement External Catering Integration

**Source:** BRIEF-23
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

Implement a working integration with a simulated external catering platform, which may be mocked, simulated, or implemented as a small independent application.

**Evidence:** A working integration with a simulated external catering platform. The external platform may be mocked, simulated, or implemented as a small independent application where appropriate.

**Clarification:** None

## BASE-069 — Provide Quality Assurance Artifacts and Testing Evidence

**Source:** BRIEF-23
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

Provide a test strategy covering the full spectrum of quality, functional test scenarios, automated tests for selected 5 end-to-end flows, identified defects, evidence of testing, and a quality assessment of the final product.

**Evidence:** Test strategy covering full spectrum of quality - Functional test scenarios. - Automated tests - Automating selected 5 end to end flows as they feel like most critical - Identified defects. - Evidence of testing. - Quality assessment of the final product.

**Clarification:** None

## BASE-070 — Provide Security Artifacts and Testing Evidence

**Source:** BRIEF-23
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

Provide a basic threat model, important security decisions, identified risks, security controls implemented, and evidence of relevant security testing.

**Evidence:** Basic threat model. - Important security decisions. - Identified risks. - Security controls implemented. - Evidence of relevant security testing.

**Clarification:** None

## BASE-071 — Document AI Tool Usage and Workflows

**Source:** BRIEF-23
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

Document which AI tools or agents were used, assigned responsibilities, important prompts or workflows, output validation methods, examples of incorrect or insufficient AI outputs, examples of human judgment improving results, and significantly modified AI-generated implementations with reasons.

**Evidence:** Document: - Which AI tools or agents you used. - What responsibilities were assigned to them. - Important prompts or workflows. - How their output was validated. - Examples where AI output was incorrect or insufficient. - Examples where your own judgment improved the result. - Any AI-generated implementation you significantly modified and why.

**Clarification:** None

## BASE-072 — Select and Justify Technology Stack

**Source:** BRIEF-24
**Classification:** ENGINEERING_PROCESS
**Strength:** EXPLICIT

Select appropriate technologies for the proposed solution and explain why they are appropriate for the problem being solved.

**Evidence:** You may select technologies appropriate to your proposed solution unless additional constraints are provided by the assessors. Choosing a technology is not sufficient by itself. You should be able to explain why it is appropriate for the problem being solved.

**Clarification:** None

## BASE-073 — Explore Broader Problem

**Source:** BRIEF-25
**Classification:** ENGINEERING_PROCESS
**Strength:** EXPLICIT

The engineer must explore the broader railway problem space.

**Evidence:** 1. Explore the broader problem.

**Clarification:** None

## BASE-074 — Identify Important Capabilities

**Source:** BRIEF-25
**Classification:** ENGINEERING_PROCESS
**Strength:** EXPLICIT

The engineer must identify the important capabilities of the system.

**Evidence:** 2. Identify the important capabilities.

**Clarification:** None

## BASE-075 — Define Achievable Assessment Scope

**Source:** BRIEF-25
**Classification:** ENGINEERING_PROCESS
**Strength:** EXPLICIT

The engineer must define an achievable assessment scope.

**Evidence:** 3. Define an achievable assessment scope.

**Clarification:** None

## BASE-076 — Prioritize Requirements

**Source:** BRIEF-25
**Classification:** ENGINEERING_PROCESS
**Strength:** EXPLICIT

The engineer must prioritize requirements.

**Evidence:** 4. Prioritize requirements.

**Clarification:** None

## BASE-077 — Identify MVP Scope

**Source:** BRIEF-25
**Classification:** ENGINEERING_PROCESS
**Strength:** EXPLICIT

The engineer must identify what belongs in the MVP.

**Evidence:** 5. Identify what belongs in your MVP.

**Clarification:** None

## BASE-078 — Document Deferred Items

**Source:** BRIEF-25
**Classification:** ENGINEERING_PROCESS
**Strength:** EXPLICIT

The engineer must clearly document what has been deferred.

**Evidence:** 6. Clearly document what has been deferred.

**Clarification:** None

## BASE-079 — Explain Prioritization Rationale

**Source:** BRIEF-25
**Classification:** ENGINEERING_PROCESS
**Strength:** EXPLICIT

The engineer must explain why particular capabilities were prioritized.

**Evidence:** 7. Explain why particular capabilities were prioritized.

**Clarification:** None

## BASE-080 — Build Selected Scope to Quality

**Source:** BRIEF-25
**Classification:** ENGINEERING_PROCESS
**Strength:** EXPLICIT

The engineer must build the selected scope to an appropriate level of quality.

**Evidence:** 8. Build the selected scope to an appropriate level of quality.

**Clarification:** None

## BASE-081 — Transform Incomplete Problem into Requirements

**Source:** BRIEF-26
**Classification:** ENGINEERING_PROCESS
**Strength:** EXPLICIT

The engineer must demonstrate the ability to transform an incomplete problem into meaningful requirements.

**Evidence:** Requirement Discovery | Ability to transform an incomplete problem into meaningful requirements

**Clarification:** None

## BASE-082 — AI-Assisted Engineering Usage

**Source:** BRIEF-26
**Classification:** ENGINEERING_PROCESS
**Strength:** EXPLICIT

The engineer must effectively use, orchestrate, and validate AI assistants or agents.

**Evidence:** AI-Assisted Engineering | Effective use, orchestration, and validation of AI assistants or agents

**Clarification:** None

## BASE-083 — Product Thinking Demonstration

**Source:** BRIEF-26
**Classification:** ENGINEERING_PROCESS
**Strength:** EXPLICIT

The engineer must demonstrate an understanding of users, priorities, workflows, and business value.

**Evidence:** Product Thinking | Understanding users, priorities, workflows, and business value

**Clarification:** None

## BASE-084 — UI/UX Implementation

**Source:** BRIEF-26
**Classification:** ENGINEERING_PROCESS
**Strength:** EXPLICIT

Demonstrate usability, consistency, accessibility and design-system thinking as evaluation criteria for the created UI/UX.

**Evidence:** UI/UX | Usability, consistency, accessibility, and design-system thinking

**Clarification:** None

## BASE-085 — Architecture Boundaries and Decisions

**Source:** BRIEF-26
**Classification:** ENGINEERING_PROCESS
**Strength:** EXPLICIT

Demonstrate architecture boundaries, responsibility separation and well-reasoned technical decisions.

**Evidence:** Architecture | Appropriate boundaries, responsibility separation, and technical decisions

**Clarification:** None

## BASE-086 — Backend Engineering Standards

**Source:** BRIEF-26
**Classification:** ENGINEERING_PROCESS
**Strength:** EXPLICIT

Demonstrate maintainable, correct backend engineering, APIs, data design and business logic.

**Evidence:** Backend Engineering | Maintainability, correctness, APIs, data design, and business logic

**Clarification:** None

## BASE-087 — Frontend Engineering Standards

**Source:** BRIEF-26
**Classification:** ENGINEERING_PROCESS
**Strength:** EXPLICIT

Demonstrate usable, maintainable and responsive frontend engineering.

**Evidence:** Frontend Engineering | Usable, maintainable, responsive application development

**Clarification:** None

## BASE-088 — Mobile Engineering Experience

**Source:** BRIEF-26
**Classification:** ENGINEERING_PROCESS
**Strength:** EXPLICIT

Demonstrate an appropriate mobile experience and awareness of platform considerations.

**Evidence:** Mobile Engineering | Appropriate mobile experience and platform considerations

**Clarification:** What specific mobile platforms or devices must be targeted by the mobile experience?

## BASE-089 — Integration with Catering Platform

**Source:** BRIEF-26
**Classification:** ENGINEERING_PROCESS
**Strength:** EXPLICIT

Demonstrate robust communication with the external catering platform as an assessed integration concern.

**Evidence:** Integration | Robust communication with the external catering platform

**Clarification:** What are the exact technical protocols, APIs, and authentication mechanisms required for the external catering platform?

## BASE-090 — QA and Testing Strategy

**Source:** BRIEF-26
**Classification:** ENGINEERING_PROCESS
**Strength:** EXPLICIT

The engineer must implement a meaningful testing strategy and provide evidence of quality.

**Evidence:** QA | Meaningful testing strategy and evidence of quality

**Clarification:** None

## BASE-091 — Secure-by-Design Engineering

**Source:** BRIEF-26
**Classification:** ENGINEERING_PROCESS
**Strength:** EXPLICIT

Demonstrate secure-by-design engineering practices in the assessment solution.

**Evidence:** Security | Secure-by-design engineering practices

**Clarification:** None

## BASE-092 — Operations and Observability

**Source:** BRIEF-26
**Classification:** ENGINEERING_PROCESS
**Strength:** EXPLICIT

Demonstrate observability, reliability and production-readiness thinking in the solution.

**Evidence:** Operations | Observability, reliability, and production-readiness thinking

**Clarification:** None

## BASE-093 — Technical Judgment and Decision Justification

**Source:** BRIEF-26
**Classification:** ENGINEERING_PROCESS
**Strength:** EXPLICIT

The engineer must make and justify technical decisions instead of blindly following AI suggestions.

**Evidence:** Technical Judgment | Ability to make and justify decisions instead of blindly following AI suggestions

**Clarification:** None

## BASE-094 — Communication of Requirements and Trade-offs

**Source:** BRIEF-26
**Classification:** ENGINEERING_PROCESS
**Strength:** EXPLICIT

The engineer must be able to explain requirements, decisions, trade-offs, and outcomes.

**Evidence:** Communication | Ability to explain requirements, decisions, trade-offs, and outcomes

**Clarification:** None

## BASE-095 — Final Stakeholder Presentation Delivery

**Source:** BRIEF-27
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

The engineer must present the solution to stakeholders from the railway organization as a narrative rather than isolated screens.

**Evidence:** At the end of the assessment, you should present the solution as if demonstrating it to stakeholders from the railway organization. The presentation should tell a story rather than simply demonstrate individual screens.

**Clarification:** None

## BASE-096 — Presentation of Discovered Requirements

**Source:** BRIEF-27
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

The final presentation must explain what requirements were discovered.

**Evidence:** - What requirements you discovered.

**Clarification:** None

## BASE-097 — Presentation of Assumptions

**Source:** BRIEF-27
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

The final presentation must explain what assumptions were made.

**Evidence:** - What assumptions you made.

**Clarification:** None

## BASE-098 — Presentation of Intentional Exclusions

**Source:** BRIEF-27
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

The final presentation must explain what was intentionally excluded.

**Evidence:** - What you intentionally excluded.

**Clarification:** None

## BASE-099 — Presentation of Scope Prioritization

**Source:** BRIEF-27
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

The final presentation must explain how the scope was prioritized.

**Evidence:** - How you prioritized the scope.

**Clarification:** None

## BASE-100 — Presentation of Architecture Decisions

**Source:** BRIEF-27
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

The final presentation must explain important architecture decisions.

**Evidence:** - Important architecture decisions.

**Clarification:** None

## BASE-101 — Presentation of UX Decisions

**Source:** BRIEF-27
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

The final presentation must explain important UX decisions.

**Evidence:** - Important UX decisions.

**Clarification:** None

## BASE-102 — Presentation of Security Considerations

**Source:** BRIEF-27
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

The final presentation must explain security considerations.

**Evidence:** - Security considerations.

**Clarification:** None

## BASE-103 — Presentation of QA Strategy

**Source:** BRIEF-27
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

The final presentation must explain the QA strategy.

**Evidence:** - QA strategy.

**Clarification:** None

## BASE-104 — Presentation of AI Contribution

**Source:** BRIEF-27
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

The final presentation must explain how AI contributed to the development process.

**Evidence:** - How AI contributed to the development process.

**Clarification:** None

## BASE-105 — Presentation of AI Challenge and Rejection Rationale

**Source:** BRIEF-27
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

The final presentation must explain where AI-generated suggestions were challenged or rejected.

**Evidence:** - Where AI-generated suggestions were challenged or rejected.

**Clarification:** None

## BASE-106 — Presentation of Future System Improvements

**Source:** BRIEF-27
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

The final presentation must explain what would be improved if the system were continued beyond the assessment.

**Evidence:** - What you would improve if the system were continued beyond the assessment.

**Clarification:** None

## BASE-107 — Individual Ownership of Submitted Work

**Source:** BRIEF-28
**Classification:** ENGINEERING_PROCESS
**Strength:** EXPLICIT

The submitted work must represent the candidate's own understanding and decision-making.

**Evidence:** the submitted work must represent **your own understanding and decision-making**

**Clarification:** None

## BASE-108 — Capability to Explain Generated Code

**Source:** BRIEF-28
**Classification:** ENGINEERING_PROCESS
**Strength:** ILLUSTRATIVE

Be prepared, if requested during review, to explain generated code; this is a possible review exercise, not a separate mandatory deliverable.

**Evidence:** Explain generated code.

**Clarification:** None

## BASE-109 — Capability to Modify an Existing Feature

**Source:** BRIEF-28
**Classification:** ENGINEERING_PROCESS
**Strength:** ILLUSTRATIVE

Be prepared, if requested during review, to modify an existing feature; this is a possible review exercise, not a separate mandatory deliverable.

**Evidence:** Modify an existing feature.

**Clarification:** None

## BASE-110 — Capability to Debug an Issue

**Source:** BRIEF-28
**Classification:** ENGINEERING_PROCESS
**Strength:** ILLUSTRATIVE

Be prepared, if requested during review, to debug an issue; this is a possible review exercise, not a separate mandatory deliverable.

**Evidence:** Debug an issue.

**Clarification:** None

## BASE-111 — Capability to Explain Architectural Decisions

**Source:** BRIEF-28
**Classification:** ENGINEERING_PROCESS
**Strength:** ILLUSTRATIVE

Be prepared, if requested during review, to explain an architectural decision; this is a possible review exercise, not a separate mandatory deliverable.

**Evidence:** Explain an architectural decision.

**Clarification:** None

## BASE-112 — Capability to Change a Requirement

**Source:** BRIEF-28
**Classification:** ENGINEERING_PROCESS
**Strength:** ILLUSTRATIVE

Be prepared, if requested during review, to change a requirement; this is a possible review exercise, not a separate mandatory deliverable.

**Evidence:** Change a requirement.

**Clarification:** None

## BASE-113 — Capability to Explain the Data Model

**Source:** BRIEF-28
**Classification:** ENGINEERING_PROCESS
**Strength:** ILLUSTRATIVE

Be prepared, if requested during review, to explain your data model; this is a possible review exercise, not a separate mandatory deliverable.

**Evidence:** Explain your data model.

**Clarification:** None

## BASE-114 — Capability to Demonstrate Integrations

**Source:** BRIEF-28
**Classification:** ENGINEERING_PROCESS
**Strength:** ILLUSTRATIVE

Be prepared, if requested during review, to demonstrate how an integration works; this is a possible review exercise, not a separate mandatory deliverable.

**Evidence:** Demonstrate how an integration works.

**Clarification:** None

## BASE-115 — Capability to Explain AI Recommendation Decisions

**Source:** BRIEF-28
**Classification:** ENGINEERING_PROCESS
**Strength:** ILLUSTRATIVE

Be prepared, if requested during review, to explain why a particular AI recommendation was accepted or rejected.

**Evidence:** Explain why a particular AI recommendation was accepted or rejected.

**Clarification:** None

## BASE-116 — Capability to Describe Alternative Approaches

**Source:** BRIEF-28
**Classification:** ENGINEERING_PROCESS
**Strength:** ILLUSTRATIVE

Be prepared, if requested during review, to describe alternative approaches you considered.

**Evidence:** Describe alternative approaches you considered.

**Clarification:** None

## BASE-117 — Cross-Discipline Engineering Capability

**Source:** BRIEF-1
**Classification:** ENGINEERING_PROCESS
**Strength:** EXPLICIT

Demonstrate the ability to work across multiple software-engineering disciplines in a coherent end-to-end solution.

**Evidence:** Work across multiple software engineering disciplines.

**Clarification:** None

## BASE-118 — Depth and Coherence Expectations

**Source:** BRIEF-1
**Classification:** ENGINEERING_PROCESS
**Strength:** EXPLICIT

Demonstrate sound understanding and justified choices across disciplines without assuming expert depth in every area.

**Evidence:** you are not expected to demonstrate expert-level depth in every area

**Clarification:** None

## BASE-119 — Treat Brief as Incomplete

**Source:** BRIEF-1
**Classification:** ENGINEERING_PROCESS
**Strength:** EXPLICIT

Treat the assessment as an initial business brief and actively identify what is missing rather than assuming it specifies a complete solution.

**Evidence:** initial business brief rather than a complete requirements specification

**Clarification:** None

## BASE-120 — Train Service Information

**Source:** BRIEF-2
**Classification:** PRODUCT_DATA
**Strength:** EXPLICIT

Provide passengers with information about railway train services.

**Evidence:** Train services.

**Clarification:** None

## BASE-121 — Route Information

**Source:** BRIEF-2
**Classification:** PRODUCT_DATA
**Strength:** EXPLICIT

Provide passengers with railway route information.

**Evidence:** Routes.

**Clarification:** None

## BASE-122 — Station Information

**Source:** BRIEF-2
**Classification:** PRODUCT_DATA
**Strength:** EXPLICIT

Provide passengers with station information.

**Evidence:** Stations.

**Clarification:** None

## BASE-123 — Upcoming Trains

**Source:** BRIEF-2
**Classification:** PRODUCT_DATA
**Strength:** EXPLICIT

Provide passengers with information about upcoming train services.

**Evidence:** Upcoming trains.

**Clarification:** None

## BASE-124 — Arrival and Departure Expectations

**Source:** BRIEF-2
**Classification:** PRODUCT_DATA
**Strength:** EXPLICIT

Provide expected train arrival and departure times.

**Evidence:** Expected arrival and departure times.

**Clarification:** None

## BASE-125 — Conditional Current Train Location

**Source:** BRIEF-2
**Classification:** PRODUCT_DATA
**Strength:** EXPLICIT

Provide current train-location information where appropriate and available.

**Evidence:** Current train locations where appropriate.

**Clarification:** None

## BASE-126 — Service Disruptions and Changes

**Source:** BRIEF-2
**Classification:** PRODUCT_DATA
**Strength:** EXPLICIT

Provide information about service disruptions and operational changes.

**Evidence:** Service disruptions and changes.

**Clarification:** None

## BASE-127 — Train Choice Discovery

**Source:** BRIEF-3
**Classification:** PRODUCT_FUNCTIONAL
**Strength:** ILLUSTRATIVE

Investigate how passengers identify an appropriate train for their intended journey.

**Evidence:** Which train should I take?

**Clarification:** None

## BASE-128 — Next Train Discovery

**Source:** BRIEF-3
**Classification:** PRODUCT_FUNCTIONAL
**Strength:** ILLUSTRATIVE

Consider displaying the next relevant train for a passenger.

**Evidence:** When is the next train?

**Clarification:** None

## BASE-129 — Train Punctuality Visibility

**Source:** BRIEF-3
**Classification:** PRODUCT_FUNCTIONAL
**Strength:** ILLUSTRATIVE

Consider presenting whether a train is early, on time or late.

**Evidence:** Is my train currently running on time?

**Clarification:** None

## BASE-130 — Current Train Position

**Source:** BRIEF-3
**Classification:** PRODUCT_FUNCTIONAL
**Strength:** ILLUSTRATIVE

Consider showing current train position where data permits.

**Evidence:** Where is the train now?

**Clarification:** None

## BASE-131 — Intermediate Stops

**Source:** BRIEF-3
**Classification:** PRODUCT_FUNCTIONAL
**Strength:** ILLUSTRATIVE

Consider displaying the ordered station stops for a service.

**Evidence:** Which stations will the train stop at?

**Clarification:** None

## BASE-132 — Estimated Station Arrival

**Source:** BRIEF-3
**Classification:** PRODUCT_FUNCTIONAL
**Strength:** ILLUSTRATIVE

Consider showing when an active service is expected at a selected station.

**Evidence:** When will it reach my station?

**Clarification:** None

## BASE-133 — Passenger Disruption Awareness

**Source:** BRIEF-3
**Classification:** PRODUCT_FUNCTIONAL
**Strength:** ILLUSTRATIVE

Consider making service disruptions visible to passengers.

**Evidence:** Are there service disruptions?

**Clarification:** None

## BASE-134 — Platform and Time Changes

**Source:** BRIEF-3
**Classification:** PRODUCT_FUNCTIONAL
**Strength:** ILLUSTRATIVE

Consider showing changes in platform allocation and departure time.

**Evidence:** Has the platform or departure time changed?

**Clarification:** None

## BASE-135 — Available Connections

**Source:** BRIEF-3
**Classification:** PRODUCT_FUNCTIONAL
**Strength:** ILLUSTRATIVE

Consider exposing viable connecting train services.

**Evidence:** What are the available connections from another station?

**Clarification:** None

## BASE-136 — Domain Entity Candidate — Railway stations

**Source:** BRIEF-4
**Classification:** PRODUCT_DATA
**Strength:** ILLUSTRATIVE

Evaluate representation of railway stations in the railway network domain model.

**Evidence:** Railway stations.

**Clarification:** None

## BASE-137 — Domain Entity Candidate — Train services

**Source:** BRIEF-4
**Classification:** PRODUCT_DATA
**Strength:** ILLUSTRATIVE

Evaluate representation of train services in the railway network domain model.

**Evidence:** Train services.

**Clarification:** None

## BASE-138 — Domain Entity Candidate — Railway routes

**Source:** BRIEF-4
**Classification:** PRODUCT_DATA
**Strength:** ILLUSTRATIVE

Evaluate representation of railway routes in the railway network domain model.

**Evidence:** Railway routes.

**Clarification:** None

## BASE-139 — Domain Entity Candidate — Train schedules

**Source:** BRIEF-4
**Classification:** PRODUCT_DATA
**Strength:** ILLUSTRATIVE

Evaluate representation of train schedules in the railway network domain model.

**Evidence:** Train schedules.

**Clarification:** None

## BASE-140 — Domain Entity Candidate — Stops

**Source:** BRIEF-4
**Classification:** PRODUCT_DATA
**Strength:** ILLUSTRATIVE

Evaluate representation of stops in the railway network domain model.

**Evidence:** Stops.

**Clarification:** None

## BASE-141 — Domain Entity Candidate — Platforms

**Source:** BRIEF-4
**Classification:** PRODUCT_DATA
**Strength:** ILLUSTRATIVE

Evaluate representation of platforms in the railway network domain model.

**Evidence:** Platforms.

**Clarification:** None

## BASE-142 — Domain Entity Candidate — Train vehicles

**Source:** BRIEF-4
**Classification:** PRODUCT_DATA
**Strength:** ILLUSTRATIVE

Evaluate representation of train vehicles in the railway network domain model.

**Evidence:** Train vehicles.

**Clarification:** None

## BASE-143 — Domain Entity Candidate — Drivers or operators

**Source:** BRIEF-4
**Classification:** PRODUCT_DATA
**Strength:** ILLUSTRATIVE

Evaluate representation of drivers or operators in the railway network domain model.

**Evidence:** Drivers or operators.

**Clarification:** None

## BASE-144 — Domain Entity Candidate — Service calendars

**Source:** BRIEF-4
**Classification:** PRODUCT_DATA
**Strength:** ILLUSTRATIVE

Evaluate representation of service calendars in the railway network domain model.

**Evidence:** Service calendars.

**Clarification:** None

## BASE-145 — Domain Entity Candidate — Operational journeys

**Source:** BRIEF-4
**Classification:** PRODUCT_DATA
**Strength:** ILLUSTRATIVE

Evaluate representation of operational journeys in the railway network domain model.

**Evidence:** Operational journeys.

**Clarification:** None

## BASE-146 — Multi-Station Journey Structure

**Source:** BRIEF-4
**Classification:** PRODUCT_DATA
**Strength:** TO_BE_DEFINED

Represent journeys that can pass through multiple railway stations and associate stops in journey order.

**Evidence:** A train may travel through multiple stations as part of a journey.

**Clarification:** None

## BASE-147 — Service Operating Patterns

**Source:** BRIEF-4
**Classification:** PRODUCT_DATA
**Strength:** TO_BE_DEFINED

Determine how schedules, operating days, routes and stopping patterns are represented for different services.

**Evidence:** With different stopping patterns.

**Clarification:** None

## BASE-148 — Live Train Current Location

**Source:** BRIEF-5
**Classification:** PRODUCT_FUNCTIONAL
**Strength:** ILLUSTRATIVE

Investigate whether reliable current-position data can be shown for active journeys.

**Evidence:** Where a train currently is.

**Clarification:** None

## BASE-149 — Last Passed Station

**Source:** BRIEF-5
**Classification:** PRODUCT_FUNCTIONAL
**Strength:** ILLUSTRATIVE

Investigate identifying the last station passed by an active train.

**Evidence:** Which station it has most recently passed.

**Clarification:** None

## BASE-150 — Next Expected Station

**Source:** BRIEF-5
**Classification:** PRODUCT_FUNCTIONAL
**Strength:** ILLUSTRATIVE

Investigate identifying the next station of an active journey.

**Evidence:** Which station it will arrive at next.

**Clarification:** None

## BASE-151 — Early On-Time Late Classification

**Source:** BRIEF-5
**Classification:** PRODUCT_FUNCTIONAL
**Strength:** ILLUSTRATIVE

Investigate representing running punctuality for active journeys.

**Evidence:** Whether it is running early, on time, or late.

**Clarification:** None

## BASE-152 — Live Estimated Arrival Time

**Source:** BRIEF-5
**Classification:** PRODUCT_FUNCTIONAL
**Strength:** ILLUSTRATIVE

Investigate representing estimated arrival information for active journeys.

**Evidence:** Its estimated arrival time.

**Clarification:** None

## BASE-153 — Cancelled or Interrupted Journey State

**Source:** BRIEF-5
**Classification:** PRODUCT_FUNCTIONAL
**Strength:** ILLUSTRATIVE

Investigate how cancelled and interrupted journey statuses are reported.

**Evidence:** Whether a journey has been cancelled or interrupted.

**Clarification:** None

## BASE-154 — Display Placement Candidate — Platforms

**Source:** BRIEF-6
**Classification:** PRODUCT_FUNCTIONAL
**Strength:** ILLUSTRATIVE

Consider station passenger-information displays at platforms.

**Evidence:** Platforms.

**Clarification:** None

## BASE-155 — Display Placement Candidate — Station entrances

**Source:** BRIEF-6
**Classification:** PRODUCT_FUNCTIONAL
**Strength:** ILLUSTRATIVE

Consider station passenger-information displays at station entrances.

**Evidence:** Station entrances.

**Clarification:** None

## BASE-156 — Display Placement Candidate — Waiting areas

**Source:** BRIEF-6
**Classification:** PRODUCT_FUNCTIONAL
**Strength:** ILLUSTRATIVE

Consider station passenger-information displays at waiting areas.

**Evidence:** Waiting areas.

**Clarification:** None

## BASE-157 — Display Placement Candidate — Large information screens

**Source:** BRIEF-6
**Classification:** PRODUCT_FUNCTIONAL
**Strength:** ILLUSTRATIVE

Consider station passenger-information displays at large information screens.

**Evidence:** Large information screens.

**Clarification:** None

## BASE-158 — Display Placement Candidate — Smaller terminal devices

**Source:** BRIEF-6
**Classification:** PRODUCT_FUNCTIONAL
**Strength:** ILLUSTRATIVE

Consider station passenger-information displays at smaller terminal devices.

**Evidence:** Smaller terminal devices.

**Clarification:** None

## BASE-159 — Departures and Arrivals on Station Displays

**Source:** BRIEF-6
**Classification:** PRODUCT_FUNCTIONAL
**Strength:** ILLUSTRATIVE

Consider showing upcoming station departures and arrivals, with information appropriate to display location.

**Evidence:** upcoming departures and arrivals

**Clarification:** None

## BASE-160 — Differentiate Platform and Main-Board Responsibilities

**Source:** BRIEF-6
**Classification:** ENGINEERING_PROCESS
**Strength:** TO_BE_DEFINED

Determine differing information priorities between platform displays and main-station information boards.

**Evidence:** a platform display and a main station information board may have different responsibilities

**Clarification:** None

## BASE-161 — Station Terminal Destination Search

**Source:** BRIEF-7
**Classification:** PRODUCT_FUNCTIONAL
**Strength:** ILLUSTRATIVE

Consider allowing kiosk users to search destinations.

**Evidence:** Searching for destinations.

**Clarification:** None

## BASE-162 — Station Terminal Route Exploration

**Source:** BRIEF-7
**Classification:** PRODUCT_FUNCTIONAL
**Strength:** ILLUSTRATIVE

Consider allowing kiosk users to explore routes.

**Evidence:** Exploring routes.

**Clarification:** None

## BASE-163 — Station Terminal Upcoming Trains

**Source:** BRIEF-7
**Classification:** PRODUCT_FUNCTIONAL
**Strength:** ILLUSTRATIVE

Consider allowing kiosk users to find upcoming trains.

**Evidence:** Finding upcoming trains.

**Clarification:** None

## BASE-164 — Station Terminal Station Details

**Source:** BRIEF-7
**Classification:** PRODUCT_FUNCTIONAL
**Strength:** ILLUSTRATIVE

Consider showing station information on public terminals.

**Evidence:** Viewing station information.

**Clarification:** None

## BASE-165 — Station Terminal Disruption Information

**Source:** BRIEF-7
**Classification:** PRODUCT_FUNCTIONAL
**Strength:** ILLUSTRATIVE

Consider allowing kiosk users to see service disruptions.

**Evidence:** Viewing disruptions.

**Clarification:** None

## BASE-166 — Station Terminal Passenger Services

**Source:** BRIEF-7
**Classification:** PRODUCT_FUNCTIONAL
**Strength:** ILLUSTRATIVE

Consider presenting available passenger services on station terminals.

**Evidence:** Exploring available passenger services.

**Clarification:** None

## BASE-167 — Driver Journey Assignments

**Source:** BRIEF-8
**Classification:** PRODUCT_FUNCTIONAL
**Strength:** ILLUSTRATIVE

Consider presenting assigned journeys in the driver/operator application.

**Evidence:** Assigned journeys.

**Clarification:** None

## BASE-168 — Driver Route Information

**Source:** BRIEF-8
**Classification:** PRODUCT_FUNCTIONAL
**Strength:** ILLUSTRATIVE

Consider presenting authorized journey route information.

**Evidence:** Route information.

**Clarification:** None

## BASE-169 — Driver Scheduled Stop List

**Source:** BRIEF-8
**Classification:** PRODUCT_FUNCTIONAL
**Strength:** ILLUSTRATIVE

Consider presenting scheduled stations and stops.

**Evidence:** Scheduled station stops.

**Clarification:** None

## BASE-170 — Driver Timings

**Source:** BRIEF-8
**Classification:** PRODUCT_FUNCTIONAL
**Strength:** ILLUSTRATIVE

Consider presenting timetable or journey timing information.

**Evidence:** Timing information.

**Clarification:** None

## BASE-171 — Driver Operational Notifications

**Source:** BRIEF-8
**Classification:** PRODUCT_FUNCTIONAL
**Strength:** ILLUSTRATIVE

Consider providing operational notifications relevant to the driver.

**Evidence:** Operational notifications.

**Clarification:** None

## BASE-172 — Driver Journey Status

**Source:** BRIEF-8
**Classification:** PRODUCT_FUNCTIONAL
**Strength:** ILLUSTRATIVE

Consider presenting current operational journey status.

**Evidence:** Journey status.

**Clarification:** None

## BASE-173 — Operations Actor Candidate — Network Operations Role

**Source:** BRIEF-9
**Classification:** ENGINEERING_PROCESS
**Strength:** ILLUSTRATIVE

Investigate the responsibilities and permissions of users performing network operations role.

**Evidence:** Network operations.

**Clarification:** None

## BASE-174 — Operations Actor Candidate — Scheduling Role

**Source:** BRIEF-9
**Classification:** ENGINEERING_PROCESS
**Strength:** ILLUSTRATIVE

Investigate the responsibilities and permissions of users performing scheduling role.

**Evidence:** Scheduling.

**Clarification:** None

## BASE-175 — Operations Actor Candidate — Station Management Role

**Source:** BRIEF-9
**Classification:** ENGINEERING_PROCESS
**Strength:** ILLUSTRATIVE

Investigate the responsibilities and permissions of users performing station management role.

**Evidence:** Stations.

**Clarification:** None

## BASE-176 — Operations Actor Candidate — Train Service Management Role

**Source:** BRIEF-9
**Classification:** ENGINEERING_PROCESS
**Strength:** ILLUSTRATIVE

Investigate the responsibilities and permissions of users performing train service management role.

**Evidence:** Train services.

**Clarification:** None

## BASE-177 — Operations Actor Candidate — Customer Information Role

**Source:** BRIEF-9
**Classification:** ENGINEERING_PROCESS
**Strength:** ILLUSTRATIVE

Investigate the responsibilities and permissions of users performing customer information role.

**Evidence:** Customer information.

**Clarification:** None

## BASE-178 — Operations Actor Candidate — Incident Management Role

**Source:** BRIEF-9
**Classification:** ENGINEERING_PROCESS
**Strength:** ILLUSTRATIVE

Investigate the responsibilities and permissions of users performing incident management role.

**Evidence:** Incident management.

**Clarification:** None

## BASE-179 — Operations Actor Candidate — Operational Reporting Role

**Source:** BRIEF-9
**Classification:** ENGINEERING_PROCESS
**Strength:** ILLUSTRATIVE

Investigate the responsibilities and permissions of users performing operational reporting role.

**Evidence:** Reporting.

**Clarification:** None

## BASE-180 — Operations Actor Candidate — System Administration Role

**Source:** BRIEF-9
**Classification:** ENGINEERING_PROCESS
**Strength:** ILLUSTRATIVE

Investigate the responsibilities and permissions of users performing system administration role.

**Evidence:** System administration.

**Clarification:** None

## BASE-181 — Disruption Scenario — Train Delays

**Source:** BRIEF-10
**Classification:** PRODUCT_FUNCTIONAL
**Strength:** ILLUSTRATIVE

Consider handling and communicating train delays in the disruption model.

**Evidence:** Train delays.

**Clarification:** None

## BASE-182 — Disruption Scenario — Train Cancellations

**Source:** BRIEF-10
**Classification:** PRODUCT_FUNCTIONAL
**Strength:** ILLUSTRATIVE

Consider handling and communicating train cancellations in the disruption model.

**Evidence:** Train cancellations.

**Clarification:** None

## BASE-183 — Disruption Scenario — Platform Changes

**Source:** BRIEF-10
**Classification:** PRODUCT_FUNCTIONAL
**Strength:** ILLUSTRATIVE

Consider handling and communicating platform changes in the disruption model.

**Evidence:** Platform changes.

**Clarification:** None

## BASE-184 — Disruption Scenario — Temporary Station Closures

**Source:** BRIEF-10
**Classification:** PRODUCT_FUNCTIONAL
**Strength:** ILLUSTRATIVE

Consider handling and communicating temporary station closures in the disruption model.

**Evidence:** Temporary station closures.

**Clarification:** None

## BASE-185 — Disruption Scenario — Route Changes

**Source:** BRIEF-10
**Classification:** PRODUCT_FUNCTIONAL
**Strength:** ILLUSTRATIVE

Consider handling and communicating route changes in the disruption model.

**Evidence:** Route changes.

**Clarification:** None

## BASE-186 — Disruption Scenario — Unexpected Train Termination

**Source:** BRIEF-10
**Classification:** PRODUCT_FUNCTIONAL
**Strength:** ILLUSTRATIVE

Consider handling and communicating unexpected train termination in the disruption model.

**Evidence:** Unexpected train termination.

**Clarification:** None

## BASE-187 — Disruption Scenario — Operational Incidents

**Source:** BRIEF-10
**Classification:** PRODUCT_FUNCTIONAL
**Strength:** ILLUSTRATIVE

Consider handling and communicating operational incidents in the disruption model.

**Evidence:** Operational incidents.

**Clarification:** None

## BASE-188 — Catering Provider Owns Its Separate Service Platform

**Source:** BRIEF-11
**Classification:** PRODUCT_INTEGRATION
**Strength:** EXPLICIT

Respect that the catering provider operates its own food-services platform, distinct from SmartRail.

**Evidence:** The catering provider operates its own platform and is responsible for managing food services.

**Clarification:** None

## BASE-189 — Integration Supports Preparation and Collection

**Source:** BRIEF-11
**Classification:** PRODUCT_INTEGRATION
**Strength:** EXPLICIT

Design SmartRail-to-catering information exchange to support coordinating food preparation and collection with train journeys.

**Evidence:** The integration should allow the catering platform to coordinate food preparation and collection with train journeys.

**Clarification:** None

## BASE-190 — Catering Data Candidate — Train Services

**Source:** BRIEF-11
**Classification:** PRODUCT_INTEGRATION
**Strength:** ILLUSTRATIVE

Investigate whether the catering platform needs train services from SmartRail.

**Evidence:** Train services.

**Clarification:** None

## BASE-191 — Catering Data Candidate — Train Routes

**Source:** BRIEF-11
**Classification:** PRODUCT_INTEGRATION
**Strength:** ILLUSTRATIVE

Investigate whether the catering platform needs train routes from SmartRail.

**Evidence:** Train routes.

**Clarification:** None

## BASE-192 — Catering Data Candidate — Station Data

**Source:** BRIEF-11
**Classification:** PRODUCT_INTEGRATION
**Strength:** ILLUSTRATIVE

Investigate whether the catering platform needs station data from SmartRail.

**Evidence:** Station information.

**Clarification:** None

## BASE-193 — Catering Data Candidate — Scheduled Arrivals

**Source:** BRIEF-11
**Classification:** PRODUCT_INTEGRATION
**Strength:** ILLUSTRATIVE

Investigate whether the catering platform needs scheduled arrivals from SmartRail.

**Evidence:** Scheduled arrival times.

**Clarification:** None

## BASE-194 — Catering Data Candidate — Actual/Estimated Arrivals

**Source:** BRIEF-11
**Classification:** PRODUCT_INTEGRATION
**Strength:** ILLUSTRATIVE

Investigate whether the catering platform needs actual/estimated arrivals from SmartRail.

**Evidence:** Actual or estimated arrival times.

**Clarification:** None

## BASE-195 — Catering Data Candidate — Service Changes

**Source:** BRIEF-11
**Classification:** PRODUCT_INTEGRATION
**Strength:** ILLUSTRATIVE

Investigate whether the catering platform needs service changes from SmartRail.

**Evidence:** Service changes.

**Clarification:** None

## BASE-196 — Eligible Collection Station Option

**Source:** BRIEF-12
**Classification:** PRODUCT_FUNCTIONAL
**Strength:** ILLUSTRATIVE

Consider allowing passengers travelling through several stations to choose an eligible station for food collection.

**Evidence:** a passenger travelling through several stations may choose an eligible station where their order can be collected

**Clarification:** None

## BASE-197 — Catering Remains a Separate External System

**Source:** BRIEF-12
**Classification:** PRODUCT_INTEGRATION
**Strength:** EXPLICIT

Preserve the catering provider as a separate external system in the proposed architecture.

**Evidence:** The catering provider should remain a **separate external system**.

**Clarification:** None

## BASE-198 — Architectural Separation for Catering

**Source:** BRIEF-12
**Classification:** ENGINEERING_PROCESS
**Strength:** EXPLICIT

Reflect the third-party catering boundary in the architecture and integration contracts.

**Evidence:** Your architecture should reflect this separation.

**Clarification:** None

## BASE-199 — Catering Workflow Question — Supported Catering Stations

**Source:** BRIEF-12
**Classification:** ENGINEERING_PROCESS
**Strength:** TO_BE_DEFINED

Determine which stations offer catering and how eligibility is represented.

**Evidence:** Which stations support catering?

**Clarification:** None

## BASE-200 — Catering Workflow Question — Food Order Placement Window

**Source:** BRIEF-12
**Classification:** ENGINEERING_PROCESS
**Strength:** TO_BE_DEFINED

Determine when passengers may place food orders relative to their journeys.

**Evidence:** When can an order be placed?

**Clarification:** None

## BASE-201 — Catering Workflow Question — Food Order Modification Cut-Off

**Source:** BRIEF-12
**Classification:** ENGINEERING_PROCESS
**Strength:** TO_BE_DEFINED

Determine whether and until when catering orders can be modified.

**Evidence:** How close to arrival can an order be modified?

**Clarification:** None

## BASE-202 — Catering Workflow Question — Catering Delay Handling

**Source:** BRIEF-12
**Classification:** ENGINEERING_PROCESS
**Strength:** TO_BE_DEFINED

Define how train delays affect food preparation and collection.

**Evidence:** What happens if a train is delayed?

**Clarification:** None

## BASE-203 — Catering Workflow Question — Missed Catering Collection

**Source:** BRIEF-12
**Classification:** ENGINEERING_PROCESS
**Strength:** TO_BE_DEFINED

Define outcomes and recovery rules when a passenger misses a collection station.

**Evidence:** What happens if the passenger misses the collection station?

**Clarification:** None

## BASE-204 — Catering Workflow Question — Food Order Cancellation

**Source:** BRIEF-12
**Classification:** ENGINEERING_PROCESS
**Strength:** TO_BE_DEFINED

Decide whether catering orders can be cancelled and under which rules.

**Evidence:** Can an order be cancelled?

**Clarification:** None

## BASE-205 — Catering Workflow Question — Catering Payment Ownership

**Source:** BRIEF-12
**Classification:** ENGINEERING_PROCESS
**Strength:** TO_BE_DEFINED

Determine which system owns food-order payment processing.

**Evidence:** Who owns payment processing?

**Clarification:** None

## BASE-206 — Catering Workflow Question — Authoritative Food Menu

**Source:** BRIEF-12
**Classification:** ENGINEERING_PROCESS
**Strength:** TO_BE_DEFINED

Determine which system owns the definitive food catalog.

**Evidence:** Who owns the authoritative food menu?

**Clarification:** None

## BASE-207 — Catering Workflow Question — Food Availability Synchronization

**Source:** BRIEF-12
**Classification:** ENGINEERING_PROCESS
**Strength:** TO_BE_DEFINED

Determine how catering availability changes reach SmartRail.

**Evidence:** How are food availability changes communicated?

**Clarification:** None

## BASE-208 — Catering Workflow Question — Catering Order Journey Association

**Source:** BRIEF-12
**Classification:** ENGINEERING_PROCESS
**Strength:** TO_BE_DEFINED

Define how food orders are associated with passengers and railway journeys.

**Evidence:** How are orders associated with passengers and journeys?

**Clarification:** None

## BASE-209 — Catering Workflow Question — Food Collection Confirmation

**Source:** BRIEF-12
**Classification:** ENGINEERING_PROCESS
**Strength:** TO_BE_DEFINED

Determine how successful collection is recorded and communicated.

**Evidence:** How is successful collection confirmed?

**Clarification:** None

## BASE-210 — Notification Recipient and Event Policy

**Source:** BRIEF-13
**Classification:** ENGINEERING_PROCESS
**Strength:** TO_BE_DEFINED

Specify the user groups, event types and notification delivery patterns needed when important events occur.

**Evidence:** Different users may need to receive information when important events occur.

**Clarification:** None

## BASE-211 — Distinct Audience UX Needs

**Source:** BRIEF-15
**Classification:** ENGINEERING_PROCESS
**Strength:** TO_BE_DEFINED

Design each application for its intended audience rather than assuming one interaction pattern suits passengers, drivers, station users and operators.

**Evidence:** The solution contains several applications serving very different audiences.

**Clarification:** None

## BASE-212 — Connectivity Scenario — Trains with Weak Connectivity

**Source:** BRIEF-16
**Classification:** PRODUCT_NON_FUNCTIONAL
**Strength:** ILLUSTRATIVE

Consider how the affected SmartRail application behaves when trains with weak connectivity.

**Evidence:** Trains travelling through areas with weak connectivity.

**Clarification:** None

## BASE-213 — Connectivity Scenario — Disconnected Station Terminals

**Source:** BRIEF-16
**Classification:** PRODUCT_NON_FUNCTIONAL
**Strength:** ILLUSTRATIVE

Consider how the affected SmartRail application behaves when disconnected station terminals.

**Evidence:** Station terminals temporarily losing their network connection.

**Clarification:** None

## BASE-214 — Connectivity Scenario — Passengers with Intermittent Connectivity

**Source:** BRIEF-16
**Classification:** PRODUCT_NON_FUNCTIONAL
**Strength:** ILLUSTRATIVE

Consider how the affected SmartRail application behaves when passengers with intermittent connectivity.

**Evidence:** Mobile passengers with intermittent connectivity.

**Clarification:** None

## BASE-215 — Data and Privacy Question — Public Data Classification

**Source:** BRIEF-20
**Classification:** ENGINEERING_PROCESS
**Strength:** TO_BE_DEFINED

Identify which collected or delivered information is public.

**Evidence:** Which data is public?

**Clarification:** None

## BASE-216 — Data and Privacy Question — Passenger Data Classification

**Source:** BRIEF-20
**Classification:** ENGINEERING_PROCESS
**Strength:** TO_BE_DEFINED

Identify data associated with individual passengers.

**Evidence:** Which data belongs to passengers?

**Clarification:** None

## BASE-217 — Data and Privacy Question — Operational Data Classification

**Source:** BRIEF-20
**Classification:** ENGINEERING_PROCESS
**Strength:** TO_BE_DEFINED

Identify operational information and separate it from public or passenger data.

**Evidence:** Which data is operational?

**Clarification:** None

## BASE-218 — Data and Privacy Question — Data Retention

**Source:** BRIEF-20
**Classification:** ENGINEERING_PROCESS
**Strength:** TO_BE_DEFINED

Determine which information is retained and under which retention conditions.

**Evidence:** What information should be retained?

**Clarification:** None

## BASE-219 — Data and Privacy Question — Log Data Boundaries

**Source:** BRIEF-20
**Classification:** ENGINEERING_PROCESS
**Strength:** TO_BE_DEFINED

Determine what information is appropriate to include in logs.

**Evidence:** What should appear in logs?

**Clarification:** None

## BASE-220 — Data and Privacy Question — Catering Data Sharing

**Source:** BRIEF-20
**Classification:** ENGINEERING_PROCESS
**Strength:** TO_BE_DEFINED

Determine the necessary data shared with the external catering provider.

**Evidence:** What data should be shared with the catering provider?

**Clarification:** None

## BASE-221 — Data and Privacy Question — Role-Based Data Visibility

**Source:** BRIEF-20
**Classification:** ENGINEERING_PROCESS
**Strength:** TO_BE_DEFINED

Define data visibility according to organizational roles.

**Evidence:** What information should different organizational roles be able to view?

**Clarification:** None

## BASE-222 — Understand AI-Generated Output

**Source:** BRIEF-21
**Classification:** ENGINEERING_PROCESS
**Strength:** EXPLICIT

Ensure the engineer can explain and understand AI-generated work included in the solution.

**Evidence:** demonstrating that you understand the output produced by the tools you use

**Clarification:** None

## BASE-223 — Retain Human Accountability for AI Decisions

**Source:** BRIEF-21
**Classification:** ENGINEERING_PROCESS
**Strength:** EXPLICIT

Maintain personal responsibility for the delivered software and decisions even when AI assistance is used.

**Evidence:** You remain personally responsible for all decisions and delivered software.

**Clarification:** None

## BASE-224 — Permitted AI Support Activities

**Source:** BRIEF-21
**Classification:** ENGINEERING_PROCESS
**Strength:** ILLUSTRATIVE

Consider AI for discovery, stories, acceptance criteria, domain/architecture exploration, threat modeling, UX, code, tests, reviews, documentation and debugging, without assuming any single AI activity is compulsory.

**Evidence:** Examples of appropriate AI-assisted activities include:

**Clarification:** None

## BASE-225 — Discovery Coverage — Missing Actors

**Source:** BRIEF-22
**Classification:** ENGINEERING_PROCESS
**Strength:** EXPLICIT

Explicitly investigate, record and address missing actors as part of requirement discovery.

**Evidence:** Missing actors.

**Clarification:** None

## BASE-226 — Discovery Coverage — Missing Business Processes

**Source:** BRIEF-22
**Classification:** ENGINEERING_PROCESS
**Strength:** EXPLICIT

Explicitly investigate, record and address missing business processes as part of requirement discovery.

**Evidence:** Missing business processes.

**Clarification:** None

## BASE-227 — Discovery Coverage — Missing Failure Scenarios

**Source:** BRIEF-22
**Classification:** ENGINEERING_PROCESS
**Strength:** EXPLICIT

Explicitly investigate, record and address missing failure scenarios as part of requirement discovery.

**Evidence:** Missing failure scenarios.

**Clarification:** None

## BASE-228 — Discovery Coverage — Missing States

**Source:** BRIEF-22
**Classification:** ENGINEERING_PROCESS
**Strength:** EXPLICIT

Explicitly investigate, record and address missing states as part of requirement discovery.

**Evidence:** Missing states.

**Clarification:** None

## BASE-229 — Discovery Coverage — Missing Security Requirements

**Source:** BRIEF-22
**Classification:** ENGINEERING_PROCESS
**Strength:** EXPLICIT

Explicitly investigate, record and address missing security requirements as part of requirement discovery.

**Evidence:** Missing security requirements.

**Clarification:** None

## BASE-230 — Discovery Coverage — Missing Operational Requirements

**Source:** BRIEF-22
**Classification:** ENGINEERING_PROCESS
**Strength:** EXPLICIT

Explicitly investigate, record and address missing operational requirements as part of requirement discovery.

**Evidence:** Missing operational requirements.

**Clarification:** None

## BASE-231 — Discovery Coverage — Missing Accessibility Requirements

**Source:** BRIEF-22
**Classification:** ENGINEERING_PROCESS
**Strength:** EXPLICIT

Explicitly investigate, record and address missing accessibility requirements as part of requirement discovery.

**Evidence:** Missing accessibility requirements.

**Clarification:** None

## BASE-232 — Discovery Coverage — Missing Integration Cases

**Source:** BRIEF-22
**Classification:** ENGINEERING_PROCESS
**Strength:** EXPLICIT

Explicitly investigate, record and address missing integration cases as part of requirement discovery.

**Evidence:** Missing integration cases.

**Clarification:** None

## BASE-233 — Discovery Coverage — Conflicting Requirements

**Source:** BRIEF-22
**Classification:** ENGINEERING_PROCESS
**Strength:** EXPLICIT

Explicitly investigate, record and address conflicting requirements as part of requirement discovery.

**Evidence:** Conflicting requirements.

**Clarification:** None

## BASE-234 — Discovery Coverage — Ambiguous Requirements

**Source:** BRIEF-22
**Classification:** ENGINEERING_PROCESS
**Strength:** EXPLICIT

Explicitly investigate, record and address ambiguous requirements as part of requirement discovery.

**Evidence:** Ambiguous requirements.

**Clarification:** None

## BASE-235 — Discovery Coverage — Important Assumptions

**Source:** BRIEF-22
**Classification:** ENGINEERING_PROCESS
**Strength:** EXPLICIT

Explicitly investigate, record and address important assumptions as part of requirement discovery.

**Evidence:** Important assumptions.

**Clarification:** None

## BASE-236 — Discovery Quality as an Evaluation Dimension

**Source:** BRIEF-22
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

Document and evaluate the quality of the requirements-discovery process as part of the assessment.

**Evidence:** The quality of this discovery process forms part of the assessment.

**Clarification:** None

## BASE-237 — Expected Artifact — Refined Requirements

**Source:** BRIEF-23
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

Provide assessment evidence of refined requirements.

**Evidence:** Refined requirements.

**Clarification:** None

## BASE-238 — Expected Artifact — Identified Assumptions

**Source:** BRIEF-23
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

Provide assessment evidence of identified assumptions.

**Evidence:** Identified assumptions.

**Clarification:** None

## BASE-239 — Expected Artifact — User Personas or Actors

**Source:** BRIEF-23
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

Provide user personas or actors (the assessment uses "or" and does not demand both as distinct deliverables).

**Evidence:** User personas or actors.

**Clarification:** None

## BASE-240 — Expected Artifact — User Journeys

**Source:** BRIEF-23
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

Provide assessment evidence of user journeys.

**Evidence:** User journeys.

**Clarification:** None

## BASE-241 — Expected Artifact — User Stories or Equivalent Requirements

**Source:** BRIEF-23
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

Provide assessment evidence of user stories or equivalent requirements.

**Evidence:** User stories or equivalent requirements.

**Clarification:** None

## BASE-242 — Expected Artifact — Acceptance Criteria

**Source:** BRIEF-23
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

Provide assessment evidence of acceptance criteria.

**Evidence:** Acceptance criteria.

**Clarification:** None

## BASE-243 — Expected Artifact — Identified Edge Cases

**Source:** BRIEF-23
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

Provide assessment evidence of identified edge cases.

**Evidence:** Identified edge cases.

**Clarification:** None

## BASE-244 — Expected Artifact — User Flows

**Source:** BRIEF-23
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

Provide assessment evidence of user flows.

**Evidence:** User flows.

**Clarification:** None

## BASE-245 — Expected Artifact — Wireframes

**Source:** BRIEF-23
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

Provide assessment evidence of wireframes.

**Evidence:** Wireframes.

**Clarification:** None

## BASE-246 — Expected Artifact — High-Fidelity Designs When Appropriate

**Source:** BRIEF-23
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** TO_BE_DEFINED

Provide high-fidelity designs where appropriate for the chosen UX scope; the brief does not mandate high-fidelity designs for every interface.

**Evidence:** High-fidelity designs where appropriate.

**Clarification:** None

## BASE-247 — Expected Artifact — Design-System Definition

**Source:** BRIEF-23
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

Provide assessment evidence of design-system definition.

**Evidence:** Design-system definition.

**Clarification:** None

## BASE-248 — Expected Artifact — Accessibility Considerations

**Source:** BRIEF-23
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

Provide assessment evidence of accessibility considerations.

**Evidence:** Accessibility considerations.

**Clarification:** None

## BASE-249 — Expected Artifact — High-Level Solution Architecture

**Source:** BRIEF-23
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

Provide assessment evidence of high-level solution architecture.

**Evidence:** High-level solution architecture.

**Clarification:** None

## BASE-250 — Expected Artifact — Application Boundaries

**Source:** BRIEF-23
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

Provide assessment evidence of application boundaries.

**Evidence:** Application boundaries.

**Clarification:** None

## BASE-251 — Expected Artifact — Major System Components

**Source:** BRIEF-23
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

Provide assessment evidence of major system components.

**Evidence:** Major system components.

**Clarification:** None

## BASE-252 — Expected Artifact — Data Ownership

**Source:** BRIEF-23
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

Provide assessment evidence of data ownership.

**Evidence:** Data ownership.

**Clarification:** None

## BASE-253 — Expected Artifact — Integration Architecture

**Source:** BRIEF-23
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

Provide assessment evidence of integration architecture.

**Evidence:** Integration architecture.

**Clarification:** None

## BASE-254 — Expected Artifact — Authentication and Authorization Approach

**Source:** BRIEF-23
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

Provide assessment evidence of authentication and authorization approach.

**Evidence:** Authentication and authorization approach.

**Clarification:** None

## BASE-255 — Expected Artifact — Architecture Decisions and Trade-Offs

**Source:** BRIEF-23
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

Provide assessment evidence of architecture decisions and trade-offs.

**Evidence:** Important architecture decisions and trade-offs.

**Clarification:** None

## BASE-256 — Expected Artifact — Backend Domain Modeling

**Source:** BRIEF-23
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

Provide assessment evidence of backend domAIn modeling.

**Evidence:** Domain modeling.

**Clarification:** None

## BASE-257 — Expected Artifact — Backend APIs

**Source:** BRIEF-23
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

Provide assessment evidence of backend apis.

**Evidence:** APIs.

**Clarification:** None

## BASE-258 — Expected Artifact — Data Persistence

**Source:** BRIEF-23
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

Provide assessment evidence of data persistence.

**Evidence:** Data persistence.

**Clarification:** None

## BASE-259 — Expected Artifact — Input and Business Validation

**Source:** BRIEF-23
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

Provide assessment evidence of input and business validation.

**Evidence:** Validation.

**Clarification:** None

## BASE-260 — Expected Artifact — Backend Authorization

**Source:** BRIEF-23
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

Provide assessment evidence of backend authorization.

**Evidence:** Authorization.

**Clarification:** None

## BASE-261 — Expected Artifact — Backend Integration

**Source:** BRIEF-23
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

Provide assessment evidence of backend integration.

**Evidence:** Integration.

**Clarification:** None

## BASE-262 — Expected Artifact — Backend Error Handling

**Source:** BRIEF-23
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

Provide assessment evidence of backend error handling.

**Evidence:** Error handling.

**Clarification:** None

## BASE-263 — Expected Artifact — Backend Observability

**Source:** BRIEF-23
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

Provide assessment evidence of backend observability.

**Evidence:** Observability.

**Clarification:** None

## BASE-264 — Expected Artifact — Test Strategy Covering Full Spectrum

**Source:** BRIEF-23
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

Provide assessment evidence of test strategy covering full spectrum.

**Evidence:** Test strategy covering full spectrum of quality

**Clarification:** None

## BASE-265 — Expected Artifact — Functional Test Scenarios

**Source:** BRIEF-23
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

Provide assessment evidence of functional test scenarios.

**Evidence:** Functional test scenarios.

**Clarification:** None

## BASE-266 — Expected Artifact — Five Selected End-to-End Automated Flows

**Source:** BRIEF-23
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

Automate five selected end-to-end flows judged most critical and provide the corresponding test evidence.

**Evidence:** Automating selected 5 end to end flows as they feel like most critical

**Clarification:** None

## BASE-267 — Expected Artifact — Identified Defects

**Source:** BRIEF-23
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

Provide assessment evidence of identified defects.

**Evidence:** Identified defects.

**Clarification:** None

## BASE-268 — Expected Artifact — Testing Evidence

**Source:** BRIEF-23
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

Provide assessment evidence of testing evidence.

**Evidence:** Evidence of testing.

**Clarification:** None

## BASE-269 — Expected Artifact — Quality Assessment of Final Product

**Source:** BRIEF-23
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

Provide assessment evidence of quality assessment of final product.

**Evidence:** Quality assessment of the final product.

**Clarification:** None

## BASE-270 — Expected Artifact — Basic Threat Model

**Source:** BRIEF-23
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

Provide assessment evidence of basic threat model.

**Evidence:** Basic threat model.

**Clarification:** None

## BASE-271 — Expected Artifact — Important Security Decisions

**Source:** BRIEF-23
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

Provide assessment evidence of important security decisions.

**Evidence:** Important security decisions.

**Clarification:** None

## BASE-272 — Expected Artifact — Identified Security Risks

**Source:** BRIEF-23
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

Provide assessment evidence of identified security risks.

**Evidence:** Identified risks.

**Clarification:** None

## BASE-273 — Expected Artifact — Implemented Security Controls

**Source:** BRIEF-23
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

Provide assessment evidence of implemented security controls.

**Evidence:** Security controls implemented.

**Clarification:** None

## BASE-274 — Expected Artifact — Security Testing Evidence

**Source:** BRIEF-23
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

Provide assessment evidence of security testing evidence.

**Evidence:** Evidence of relevant security testing.

**Clarification:** None

## BASE-275 — Expected Artifact — AI Tools or Agents Used

**Source:** BRIEF-23
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

Provide assessment evidence of AI tools or agents used.

**Evidence:** Which AI tools or agents you used.

**Clarification:** None

## BASE-276 — Expected Artifact — AI Agent Responsibilities

**Source:** BRIEF-23
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

Provide assessment evidence of AI agent responsibilities.

**Evidence:** What responsibilities were assigned to them.

**Clarification:** None

## BASE-277 — Expected Artifact — AI Prompts or Workflows

**Source:** BRIEF-23
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

Provide assessment evidence of AI prompts or workflows.

**Evidence:** Important prompts or workflows.

**Clarification:** None

## BASE-278 — Expected Artifact — AI Output Validation

**Source:** BRIEF-23
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

Provide assessment evidence of AI output validation.

**Evidence:** How their output was validated.

**Clarification:** None

## BASE-279 — Expected Artifact — Incorrect or Insufficient AI Outputs

**Source:** BRIEF-23
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

Provide assessment evidence of incorrect or insufficient AI outputs.

**Evidence:** Examples where AI output was incorrect or insufficient.

**Clarification:** None

## BASE-280 — Expected Artifact — Human Judgment Improving AI Results

**Source:** BRIEF-23
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

Provide assessment evidence of human judgment improving AI results.

**Evidence:** Examples where your own judgment improved the result.

**Clarification:** None

## BASE-281 — Expected Artifact — Modified AI-Generated Implementations

**Source:** BRIEF-23
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

Provide assessment evidence of modified AI-generated implementations.

**Evidence:** Any AI-generated implementation you significantly modified and why.

**Clarification:** None

## BASE-282 — Station Experience Either/Or Scope

**Source:** BRIEF-23
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

Provide a functional station prototype showing passenger-information displays, interactive station terminals, or both; both are not separately mandatory.

**Evidence:** A functional prototype representing either or both:

**Clarification:** None

## BASE-283 — Backend Working Status

**Source:** BRIEF-23
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

Ensure the backend is working and demonstrates the specified engineering practices, not just diagrams or mock screens.

**Evidence:** A working backend demonstrating appropriate:

**Clarification:** None

## BASE-284 — Catering Mock/Simulation Allowed

**Source:** BRIEF-23
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

A working catering integration may use a mocked, simulated or small independent external catering application.

**Evidence:** The external platform may be mocked, simulated, or implemented as a small independent application where appropriate.

**Clarification:** None

## BASE-285 — Illustrative Technology Choices

**Source:** BRIEF-24
**Classification:** ENGINEERING_PROCESS
**Strength:** ILLUSTRATIVE

Recognize listed technologies and tools as examples rather than prescribed stacks; select appropriate technologies subject to assessor constraints.

**Evidence:** Technologies covered during the training may include areas such as:

**Clarification:** None

## BASE-286 — Avoid Unnecessary Full Railway Implementation

**Source:** BRIEF-25
**Classification:** ENGINEERING_PROCESS
**Strength:** EXPLICIT

Do not assume an individual assessment requires implementing every possible railway capability.

**Evidence:** You are **not expected to implement every possible railway capability**.

**Clarification:** None

## BASE-287 — Coherent Smaller Solution Preferred

**Source:** BRIEF-25
**Classification:** ENGINEERING_PROCESS
**Strength:** EXPLICIT

Choose a smaller coherent and well-engineered solution rather than a broad set of incomplete features.

**Evidence:** A smaller, coherent, well-engineered solution is preferable to a large collection of incomplete features.

**Clarification:** None

## BASE-288 — Maintain Multi-Application Ecosystem

**Source:** BRIEF-25
**Classification:** ENGINEERING_PROCESS
**Strength:** EXPLICIT

Do not reduce the system to a single application simply because the railway domain is broad.

**Evidence:** You should not reduce the assessment to a single application simply because the overall domain is large.

**Clarification:** None

## BASE-289 — Scope Management Assessed

**Source:** BRIEF-26
**Classification:** ENGINEERING_PROCESS
**Strength:** EXPLICIT

Demonstrate the ability to prioritize requirements and deliver a coherent individual solution.

**Evidence:** Scope Management | Ability to prioritize and deliver a coherent solution individually

**Clarification:** None

## BASE-290 — Process and Product Both Assessed

**Source:** BRIEF-26
**Classification:** ASSESSMENT_DELIVERABLE
**Strength:** EXPLICIT

Provide evidence both of a working product and of the engineering process personally followed.

**Evidence:** both the **product you create** and the **engineering process you personally use to create it**

**Clarification:** None

## BASE-291 — Avoid Unexplainable Features

**Source:** BRIEF-28
**Classification:** ENGINEERING_PROCESS
**Strength:** EXPLICIT

Avoid including features you cannot explain or maintain independently in the final submission.

**Evidence:** You should therefore avoid including functionality that you cannot explain or maintain yourself.

**Clarification:** None

## BASE-292 — Allowed Engineering Assistance Tools

**Source:** BRIEF-28
**Classification:** ENGINEERING_PROCESS
**Strength:** ILLUSTRATIVE

Recognize that assistants, agents, design/test/documentation tools and frameworks may be used while retaining personal engineering ownership.

**Evidence:** You may use:

**Clarification:** None

---

## Source Coverage Audit (28 Assessment Sections)

| Assessment section | Original IDs | Additional IDs | Total candidates |
|---|---|---|---:|
| BRIEF-1: Assessment Overview | BASE-001, BASE-002, BASE-003, BASE-004, BASE-005, BASE-006, BASE-007, BASE-008, BASE-009, BASE-010 | BASE-117, BASE-118, BASE-119 | 13 |
| BRIEF-2: Business Scenario | BASE-011, BASE-012, BASE-013, BASE-014 | BASE-120, BASE-121, BASE-122, BASE-123, BASE-124, BASE-125, BASE-126 | 11 |
| BRIEF-3: Passenger Experience | BASE-015, BASE-016, BASE-017, BASE-018 | BASE-127, BASE-128, BASE-129, BASE-130, BASE-131, BASE-132, BASE-133, BASE-134, BASE-135 | 13 |
| BRIEF-4: Railway Network | BASE-019 | BASE-136, BASE-137, BASE-138, BASE-139, BASE-140, BASE-141, BASE-142, BASE-143, BASE-144, BASE-145, BASE-146, BASE-147 | 13 |
| BRIEF-5: Live Train Information | BASE-020, BASE-021 | BASE-148, BASE-149, BASE-150, BASE-151, BASE-152, BASE-153 | 8 |
| BRIEF-6: Railway Station Digital Displays | BASE-022, BASE-023, BASE-024 | BASE-154, BASE-155, BASE-156, BASE-157, BASE-158, BASE-159, BASE-160 | 10 |
| BRIEF-7: Station Terminal Application | BASE-025, BASE-026, BASE-027 | BASE-161, BASE-162, BASE-163, BASE-164, BASE-165, BASE-166 | 9 |
| BRIEF-8: Driver / Train Operator Application | BASE-028, BASE-029 | BASE-167, BASE-168, BASE-169, BASE-170, BASE-171, BASE-172 | 8 |
| BRIEF-9: Railway Operations Platform | BASE-030, BASE-031 | BASE-173, BASE-174, BASE-175, BASE-176, BASE-177, BASE-178, BASE-179, BASE-180 | 10 |
| BRIEF-10: Service Disruptions | BASE-032, BASE-033 | BASE-181, BASE-182, BASE-183, BASE-184, BASE-185, BASE-186, BASE-187 | 9 |
| BRIEF-11: External Catering Service Integration | BASE-034, BASE-035 | BASE-188, BASE-189, BASE-190, BASE-191, BASE-192, BASE-193, BASE-194, BASE-195 | 10 |
| BRIEF-12: Passenger Food Ordering | BASE-036, BASE-037, BASE-038 | BASE-196, BASE-197, BASE-198, BASE-199, BASE-200, BASE-201, BASE-202, BASE-203, BASE-204, BASE-205, BASE-206, BASE-207, BASE-208, BASE-209 | 17 |
| BRIEF-13: Notifications | BASE-039 | BASE-210 | 2 |
| BRIEF-14: Identity and Access | BASE-040, BASE-041, BASE-042, BASE-043, BASE-044, BASE-045 | — | 6 |
| BRIEF-15: UI/UX and Design System | BASE-046, BASE-047, BASE-048 | BASE-211 | 4 |
| BRIEF-16: Connectivity and Resilience | BASE-049 | BASE-212, BASE-213, BASE-214 | 4 |
| BRIEF-17: Security | BASE-050 | — | 1 |
| BRIEF-18: Quality Assurance | BASE-051, BASE-052 | — | 2 |
| BRIEF-19: Observability and Operations | BASE-053 | — | 1 |
| BRIEF-20: Data and Privacy | BASE-054, BASE-055 | BASE-215, BASE-216, BASE-217, BASE-218, BASE-219, BASE-220, BASE-221 | 9 |
| BRIEF-21: AI-Assisted Development | BASE-056 | BASE-222, BASE-223, BASE-224 | 4 |
| BRIEF-22: Requirement Discovery Challenge | BASE-057, BASE-058, BASE-059 | BASE-225, BASE-226, BASE-227, BASE-228, BASE-229, BASE-230, BASE-231, BASE-232, BASE-233, BASE-234, BASE-235, BASE-236 | 15 |
| BRIEF-23: Expected Assessment Artifacts | BASE-060, BASE-061, BASE-062, BASE-063, BASE-064, BASE-065, BASE-066, BASE-067, BASE-068, BASE-069, BASE-070, BASE-071 | BASE-237, BASE-238, BASE-239, BASE-240, BASE-241, BASE-242, BASE-243, BASE-244, BASE-245, BASE-246, BASE-247, BASE-248, BASE-249, BASE-250, BASE-251, BASE-252, BASE-253, BASE-254, BASE-255, BASE-256, BASE-257, BASE-258, BASE-259, BASE-260, BASE-261, BASE-262, BASE-263, BASE-264, BASE-265, BASE-266, BASE-267, BASE-268, BASE-269, BASE-270, BASE-271, BASE-272, BASE-273, BASE-274, BASE-275, BASE-276, BASE-277, BASE-278, BASE-279, BASE-280, BASE-281, BASE-282, BASE-283, BASE-284 | 60 |
| BRIEF-24: Technical Freedom | BASE-072 | BASE-285 | 2 |
| BRIEF-25: Individual Scope Management | BASE-073, BASE-074, BASE-075, BASE-076, BASE-077, BASE-078, BASE-079, BASE-080 | BASE-286, BASE-287, BASE-288 | 11 |
| BRIEF-26: Assessment Focus | BASE-081, BASE-082, BASE-083, BASE-084, BASE-085, BASE-086, BASE-087, BASE-088, BASE-089, BASE-090, BASE-091, BASE-092, BASE-093, BASE-094 | BASE-289, BASE-290 | 16 |
| BRIEF-27: Final Demonstration | BASE-095, BASE-096, BASE-097, BASE-098, BASE-099, BASE-100, BASE-101, BASE-102, BASE-103, BASE-104, BASE-105, BASE-106 | — | 12 |
| BRIEF-28: Individual Ownership | BASE-107, BASE-108, BASE-109, BASE-110, BASE-111, BASE-112, BASE-113, BASE-114, BASE-115, BASE-116 | BASE-291, BASE-292 | 12 |

**Audit result:** All 28 numbered sections have source-linked candidates, and this reviewed edition includes individual entries for previously compressed feature and assessment-deliverable details. This is a source coverage audit, not a claim that all requirements have been formally validated by stakeholders.

## Review Corrections Made

- Preserved the original 116 `BASE-xxx` IDs so future cross-references remain stable; additions begin at BASE-117.
- Clarified that examples introduced by *may*, *for example*, or *possible* are illustrative and not automatically Must Have requirements.
- Corrected optional kiosk capability, passenger account/public-access wording, notification vagueness, and the Section 15 example application list.
- Reclassified Section 23 implementation evidence and Section 26 assessment focus statements to avoid presenting delivery/evaluation obligations as individual product functions.
- Expanded the railway domain model, active-train status scenarios, station displays, station terminals, driver information, operational roles, disruption types, catering workflows, privacy questions, and assessment artifacts into traceable source-specific candidates.
- Explicitly captured the Section 23 requirement to automate **five selected critical end-to-end flows**, plus security, QA, AI evidence, user flows, personas/actors, acceptance criteria and architecture deliverables.
- Corrected Section 28 possible demonstration questions to illustrative assessment-readiness items rather than guaranteed mandatory outputs.
