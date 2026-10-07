# Product Owner Discovery Analysis

## 1. Assumed Initial Business Problem

- **Category:** BUSINESS_PROCESS
- **Origin:** CLEAR_INFERENCE
- **Discovery:** The railway currently suffers from inefficient and siloed incident management processes, leading to delayed responses, poor real-time communication with passengers, increased operational costs, and diminished customer satisfaction during disruptions.
- **Source basis:** The Product Owner inferred this core business problem based on the product name and common railway challenges since no initial business problem description was provided.
- **Scope signal:** CORE_CANDIDATE
- **Open question:** What is the official baseline business problem description for SmartRail?

## 2. Real-time Incident Reporting and Logging

- **Category:** FUNCTIONAL
- **Origin:** EXPLICIT
- **Discovery:** Authorised personnel such as train drivers, station staff, and the control centre need the ability to log incidents with location, type, severity, and initial observations.
- **Source basis:** Listed under essential capabilities for solving the assumed problem.
- **Scope signal:** CORE_CANDIDATE
- **Open question:** None

## 3. Incident Status and Lifecycle Management

- **Category:** STATE
- **Origin:** EXPLICIT
- **Discovery:** The system must track incidents from reported to resolved, including stages like investigation, action taken, and resolution.
- **Source basis:** Mentioned as an essential capability to track incident progress.
- **Scope signal:** CORE_CANDIDATE
- **Open question:** None

## 4. Internal Notification and Alerting System

- **Category:** FUNCTIONAL
- **Origin:** EXPLICIT
- **Discovery:** Automatically alert relevant operational teams such as control, maintenance, customer service, and train crew based on incident type and severity.
- **Source basis:** Identified as a core essential capability.
- **Scope signal:** CORE_CANDIDATE
- **Open question:** What are the precise triggers, recipients, and escalation paths for different incident types and severities?

## 5. Basic Passenger Information Dissemination

- **Category:** FUNCTIONAL
- **Origin:** EXPLICIT
- **Discovery:** Broadcast standardised, accurate delay and cancellation messages to passengers via multiple channels like station displays, app notifications, and website updates.
- **Source basis:** Listed as an essential capability for passenger communication.
- **Scope signal:** CORE_CANDIDATE
- **Open question:** What specific channels are included and what level of detail is provided?

## 6. Basic Resource Allocation for Operational Staff

- **Category:** FUNCTIONAL
- **Origin:** EXPLICIT
- **Discovery:** Provide a simple mechanism to assign immediate tasks or personnel to an ongoing incident, such as sending a maintenance crew to a specific location.
- **Source basis:** Described as an essential capability for managing operational staff during incidents.
- **Scope signal:** CORE_CANDIDATE
- **Open question:** None

## 7. Operational Dashboard and Reporting

- **Category:** FUNCTIONAL
- **Origin:** EXPLICIT
- **Discovery:** A high-level view of active incidents, their status, and basic performance metrics for operational managers.
- **Source basis:** Listed as an essential operational capability.
- **Scope signal:** CORE_CANDIDATE
- **Open question:** None

## 8. Additional Operational and Support Actors

- **Category:** MISSING_ACTOR
- **Origin:** EXPLICIT
- **Discovery:** Potential missing actors include Train Drivers/Onboard Staff, Station Staff/Ground Crew, Maintenance & Engineering Teams, Customer Service/Call Centre Agents, Regulatory Authorities, Emergency Services, and Public Relations/Corporate Communications.
- **Source basis:** Identified by the Product Owner as important missing actors.
- **Scope signal:** REVIEW_NEEDED
- **Open question:** None

## 9. Emergency Response and Governance Processes

- **Category:** BUSINESS_PROCESS
- **Origin:** EXPLICIT
- **Discovery:** Processes concerning emergency response protocols, data validation, post-incident analysis, staff training, and integration with ticketing, scheduling, and asset management systems need to be established.
- **Source basis:** Listed under important missing processes.
- **Scope signal:** REVIEW_NEEDED
- **Open question:** None

## 10. Definition of Real-time Latency

- **Category:** AMBIGUITY
- **Origin:** EXPLICIT
- **Discovery:** The exact acceptable latency for incident reporting and updates (whether seconds or minutes) is unspecified.
- **Source basis:** Highlighted in the ambiguous requirements section.
- **Scope signal:** REVIEW_NEEDED
- **Open question:** What is the acceptable latency for incident reporting and updates?

## 11. Legacy System Integration Scope

- **Category:** INTEGRATION
- **Origin:** EXPLICIT
- **Discovery:** It is unclear which existing railway systems SmartRail must integrate with initially and whether the integration should be read-only or read-write.
- **Source basis:** Highlighted as a major architectural ambiguity.
- **Scope signal:** REVIEW_NEEDED
- **Open question:** Which existing railway systems must SmartRail integrate with initially and to what extent?

## 12. Full AI-Powered Predictive Maintenance

- **Category:** SCOPE_CANDIDATE
- **Origin:** EXPLICIT
- **Discovery:** Building sophisticated AI models for predictive maintenance from scratch requires vast data and time, and is considered too large for an initial release focused on response.
- **Source basis:** Identified as a stakeholder request that may unnecessarily expand scope and should be challenged.
- **Scope signal:** EXTENSION_CANDIDATE
- **Open question:** None

## 13. Personalised Passenger Journey Planning and Re-routing

- **Category:** SCOPE_CANDIDATE
- **Origin:** EXPLICIT
- **Discovery:** Real-time journey planning, alternative transport options, and individual passenger profile integration are highly complex tasks going beyond core incident response.
- **Source basis:** Listed as a scope candidate to challenge for the initial release.
- **Scope signal:** EXTENSION_CANDIDATE
- **Open question:** None

## 14. Comprehensive CRM for Passenger Loyalty Programs

- **Category:** SCOPE_CANDIDATE
- **Origin:** EXPLICIT
- **Discovery:** Building a full customer relationship management system is an entirely separate product initiative from incident management.
- **Source basis:** Identified as a scope expansion candidate.
- **Scope signal:** EXTENSION_CANDIDATE
- **Open question:** None

## 15. Dynamic Pricing and Automated Compensation Calculations

- **Category:** SCOPE_CANDIDATE
- **Origin:** EXPLICIT
- **Discovery:** Automated compensation and dynamic pricing involve complex financial systems with significant regulatory and audit implications.
- **Source basis:** Listed as a feature that should be excluded from core incident response scope.
- **Scope signal:** EXTENSION_CANDIDATE
- **Open question:** None

## 16. End-to-End Asset Management System

- **Category:** SCOPE_CANDIDATE
- **Origin:** EXPLICIT
- **Discovery:** SmartRail should interface with asset management for context, but building a brand new full asset management system is out of scope.
- **Source basis:** Highlighted as a scope candidate to be deferred or excluded from core product.
- **Scope signal:** EXTENSION_CANDIDATE
- **Open question:** None

## 17. MVP Capabilities Definition

- **Category:** FUNCTIONAL
- **Origin:** EXPLICIT
- **Discovery:** The MVP should include core incident reporting, status tracking, internal communication module, standardised passenger alerts, basic user roles, and a centralised dashboard with basic read-only integration points.
- **Source basis:** Described as capabilities that belong strictly in the MVP.
- **Scope signal:** CORE_CANDIDATE
- **Open question:** None

## 18. Deferred Capabilities for Post-MVP Phases

- **Category:** OPERATIONAL
- **Origin:** EXPLICIT
- **Discovery:** Advanced analytics, comprehensive resource optimisation, personalised passenger communication, emergency service integration, automated compensation processing, mobile apps for all roles, and two-way feedback can be deferred.
- **Source basis:** Listed as features that could reasonably be deferred to subsequent phases.
- **Scope signal:** EXTENSION_CANDIDATE
- **Open question:** None

## 19. Unanswered Implementation Questions

- **Category:** ASSUMPTION
- **Origin:** EXPLICIT
- **Discovery:** Several critical questions regarding KPIs, definition of an incident, legacy integration requirements, target users, budget, timeline, regulatory compliance, change management, and data ownership remain unanswered.
- **Source basis:** Highlighted as important questions that must be answered before implementation.
- **Scope signal:** REVIEW_NEEDED
- **Open question:** What are the primary KPIs, definition of an incident, legacy system dependencies, budget, timeline, and regulatory constraints?
