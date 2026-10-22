# Customer Service Discovery Analysis

## 1. Access to Real-Time Schedule and Infrastructure Data

- **Category:** FUNCTIONAL
- **Origin:** EXPLICIT
- **Discovery:** Customer service representatives need quick access to real-time schedule data, platform assignments, station layout details, and train-specific amenities to answer passenger enquiries.
- **Source basis:** The stakeholder requires real-time schedule data, platform numbers, station layout, and train amenities to perform enquiries.
- **Scope signal:** CORE_CANDIDATE
- **Open question:** None

## 2. Access to Delay Reasons and Duration

- **Category:** FUNCTIONAL
- **Origin:** EXPLICIT
- **Discovery:** CSRs require the specific reason for delays and estimated durations to properly inform and advise passengers.
- **Source basis:** The stakeholder mentions requiring real-time status of delayed trains and crucially the reason for the delay.
- **Scope signal:** CORE_CANDIDATE
- **Open question:** None

## 3. Single Source of Truth and Data Consistency

- **Category:** INTEGRATION
- **Origin:** EXPLICIT
- **Discovery:** Inconsistencies between the CSR system, public screens, and mobile apps erode passenger trust. A single authoritative source is required.
- **Source basis:** The stakeholder notes failure scenarios involving conflicting info where their system shows one thing and public screens another.
- **Scope signal:** CORE_CANDIDATE
- **Open question:** What is the definitive authoritative source for real-time journey information?

## 4. Ticketing, Refunds, and Compensation Support

- **Category:** SCOPE_CANDIDATE
- **Origin:** EXPLICIT
- **Discovery:** Processing refunds, issuing compensation, and handling complex ticketing or routing are outside direct CSR control and need definition regarding system inclusion.
- **Source basis:** The stakeholder explicitly mentions that ticketing support, direct compensation, and refunds are scope candidates or questions for future definition.
- **Scope signal:** EXTENSION_CANDIDATE
- **Open question:** Should SmartRail include ticketing support, refunds, and compensation management?

## 5. Hotel Accommodation and Private Taxi Arrangement

- **Category:** SCOPE_CANDIDATE
- **Origin:** EXPLICIT
- **Discovery:** Arranging hotel accommodation or private taxis for individuals stranded by major disruptions is noted as a potential scope item or policy question.
- **Source basis:** The stakeholder states that arranging hotel accommodation or private taxis is a scope candidate/question.
- **Scope signal:** EXTENSION_CANDIDATE
- **Open question:** Are hotel and taxi arrangements part of the system or operator policy scope?

## 6. System Slowdown and Downtime Impact

- **Category:** FAILURE_SCENARIO
- **Origin:** EXPLICIT
- **Discovery:** System slowdowns or downtime prevent CSRs from accessing information, leading to delays and passenger frustration.
- **Source basis:** The stakeholder identifies system slowdown/downtime as a failure scenario where information cannot be accessed.
- **Scope signal:** CORE_CANDIDATE
- **Open question:** None

## 7. Language Barrier and Translation Support

- **Category:** FAILURE_SCENARIO
- **Origin:** EXPLICIT
- **Discovery:** Inability to communicate effectively with passengers who speak different languages without translation support.
- **Source basis:** The stakeholder lists language barrier as a failure scenario causing communication difficulties.
- **Scope signal:** REVIEW_NEEDED
- **Open question:** How should language barriers be handled within the system?

## 8. Real-time Facility Status Updates

- **Category:** BUSINESS_PROCESS
- **Origin:** EXPLICIT
- **Discovery:** Unclear how quickly CSRs are informed about out-of-order facilities like broken lifts and whether a real-time status update system exists.
- **Source basis:** The stakeholder lists facility status as an unclear responsibility regarding real-time status updates.
- **Scope signal:** CORE_CANDIDATE
- **Open question:** How are broken or out-of-order station facilities reported in real time to customer service?

## 9. Dedicated Ticketing Agent

- **Category:** MISSING_ACTOR
- **Origin:** CLEAR_INFERENCE
- **Discovery:** Complex ticketing and routing issues are escalated to a dedicated ticketing agent if SmartRail does not include ticketing support.
- **Source basis:** Escalation paths mention a dedicated ticketing agent.
- **Scope signal:** REVIEW_NEEDED
- **Open question:** None

## 10. Station Management, First Aid, and BTP

- **Category:** MISSING_ACTOR
- **Origin:** EXPLICIT
- **Discovery:** Medical issues, security concerns, lost children, and unruly passengers require escalation to Station Management, First Aid, or British Transport Police.
- **Source basis:** Escalation paths outline medical/security and lost children handoffs to Station Management, First Aid, or BTP.
- **Scope signal:** CORE_CANDIDATE
- **Open question:** None

## 11. Alternative Transport Activation Process

- **Category:** BUSINESS_PROCESS
- **Origin:** EXPLICIT
- **Discovery:** A defined process is missing for when and how alternative transport (buses, taxis) is activated and communicated during significant delays or cancellations.
- **Source basis:** The stakeholder notes alternative transport activation as a missing business process.
- **Scope signal:** CORE_CANDIDATE
- **Open question:** Who decides on and arranges replacement transport, and how is it communicated?

## 12. Real-time Accessibility and Lift Status Tracking

- **Category:** ACCESSIBILITY
- **Origin:** EXPLICIT
- **Discovery:** CSRs need real-time operational status of all accessibility equipment, such as lifts and ramps, to assist passengers with mobility needs.
- **Source basis:** The stakeholder highlights the need for real-time status of station accessibility features and notes out-of-date information as a failure scenario.
- **Scope signal:** CORE_CANDIDATE
- **Open question:** None

## 13. Spontaneous and Pre-booked Assistance Protocols

- **Category:** ACCESSIBILITY
- **Origin:** EXPLICIT
- **Discovery:** Protocols are needed for managing both pre-booked and spontaneous mobility assistance, including handling assistance no-shows.
- **Source basis:** The stakeholder discusses verifying pre-booked assistance, arranging spontaneous assistance, and failure scenarios like assistance no-shows.
- **Scope signal:** CORE_CANDIDATE
- **Open question:** None

## 14. Unified Booking and Tracking Assistance System

- **Category:** INTEGRATION
- **Origin:** EXPLICIT
- **Discovery:** A unified system is desired for booking and tracking assistance requests across journeys, potentially involving multiple operators.
- **Source basis:** Unclear responsibilities mention an integrated booking system for assistance requests across operators.
- **Scope signal:** EXTENSION_CANDIDATE
- **Open question:** None

## 15. Inter-Operator Ticket Acceptance and Rules

- **Category:** INTEGRATION
- **Origin:** EXPLICIT
- **Discovery:** Rules for ticket acceptance across different operators during disruptions are often unclear and vary widely.
- **Source basis:** Failure scenarios and missing processes note unclear ticket acceptance rules for alternative routes or operators.
- **Scope signal:** CORE_CANDIDATE
- **Open question:** How can inter-operator ticket validity rules be easily viewed and communicated during disruptions?

## 16. Complaint Logging System

- **Category:** FUNCTIONAL
- **Origin:** EXPLICIT
- **Discovery:** CSRs need access to a designated system to accurately and efficiently log complaints and track escalations.
- **Source basis:** Information required for complaints includes access to a complaint logging system.
- **Scope signal:** CORE_CANDIDATE
- **Open question:** None

## 17. Dynamic and Constantly Changing Estimates

- **Category:** STATE
- **Origin:** EXPLICIT
- **Discovery:** Constantly shifting arrival or departure time estimates erode passenger trust and CSR credibility.
- **Source basis:** Failure scenarios under delays list 'Constantly Changing Times' where estimates keep shifting.
- **Scope signal:** REVIEW_NEEDED
- **Open question:** None

## 18. Vulnerable Passenger Support Protocol

- **Category:** BUSINESS_PROCESS
- **Origin:** EXPLICIT
- **Discovery:** A defined process is needed for identifying and proactively supporting vulnerable passengers, unaccompanied minors, the elderly, or those with cognitive disabilities during major cancellations or platform changes.
- **Source basis:** Unclear responsibilities mention vulnerable passenger support during cancellations and specific handoff procedures.
- **Scope signal:** CORE_CANDIDATE
- **Open question:** What are the specific handover and support protocols for vulnerable passengers?
