# Railway Controller Discovery Analysis

## 1. Passenger compensation management

- **Category:** SCOPE_CANDIDATE
- **Origin:** EXPLICIT
- **Discovery:** TOCs manage passenger compensation during delays and incidents, but it represents a broader operational subsystem that could be linked.
- **Source basis:** Mentioned during delay management and operational incidents where TOCs handle passenger compensation.
- **Scope signal:** EXTENSION_CANDIDATE
- **Open question:** Should the system track or integrate with passenger compensation workflows?

## 2. On-board ticket purchasing or fare management

- **Category:** SCOPE_CANDIDATE
- **Origin:** CLEAR_INFERENCE
- **Discovery:** The stakeholder interview touches on passenger management and alternative transport arrangements, but does not explicitly detail ticket purchasing functions.
- **Source basis:** Mention of ticket acceptance on other routes and alternative travel options.
- **Scope signal:** EXTENSION_CANDIDATE
- **Open question:** None

## 3. Indoor station navigation

- **Category:** SCOPE_CANDIDATE
- **Origin:** CLEAR_INFERENCE
- **Discovery:** Station staff manage passenger flow and communicate platform changes, but specialized indoor navigation for passengers was not discussed.
- **Source basis:** Platform changes and station staff responsibilities in guiding passengers.
- **Scope signal:** EXTENSION_CANDIDATE
- **Open question:** None

## 4. Advanced passenger seat reservations

- **Category:** SCOPE_CANDIDATE
- **Origin:** CLEAR_INFERENCE
- **Discovery:** Short-forming, journey terminations, and cancellations impact passenger seating, but seat reservations are not part of the network controller's core toolset.
- **Source basis:** Short-forming and train cancellations affecting passenger capacity.
- **Scope signal:** EXTENSION_CANDIDATE
- **Open question:** None

## 5. Emergency Services

- **Category:** MISSING_ACTOR
- **Origin:** EXPLICIT
- **Discovery:** Emergency services (police, fire, ambulance) take command of life safety aspects during unexpected journey terminations and major incidents.
- **Source basis:** Mentioned under unexpected journey terminations and operational incidents as taking command of life safety.
- **Scope signal:** REVIEW_NEEDED
- **Open question:** What level of direct system access or communication interface do emergency services require?

## 6. Infrastructure Engineers

- **Category:** MISSING_ACTOR
- **Origin:** EXPLICIT
- **Discovery:** Infrastructure teams are responsible for carrying out repairs, assessing track/signal safety, and accessing the track following incidents.
- **Source basis:** Mentioned in operational incidents and unexpected journey terminations regarding repairs and track access.
- **Scope signal:** REVIEW_NEEDED
- **Open question:** How will engineering teams log onto the system to request and release track access authority?

## 7. Real-time Network Map display

- **Category:** FUNCTIONAL
- **Origin:** EXPLICIT
- **Discovery:** The system needs to provide a visual display of all active trains, schedules, actual positions, speeds, and signal indications.
- **Source basis:** Listed under general information and tools for a network controller.
- **Scope signal:** CORE_CANDIDATE
- **Open question:** None

## 8. Incident Management System

- **Category:** FUNCTIONAL
- **Origin:** EXPLICIT
- **Discovery:** The controller uses an incident management system to log, track, and manage all operational disruptions.
- **Source basis:** Listed under general tools as the Incident Management System.
- **Scope signal:** CORE_CANDIDATE
- **Open question:** None

## 9. Communication systems integration

- **Category:** FUNCTIONAL
- **Origin:** EXPLICIT
- **Discovery:** Direct links are required to drivers (GSMR/radio), signallers, station managers, TOC control rooms, and emergency services.
- **Source basis:** Listed under communication systems in the controller's general tools.
- **Scope signal:** REVIEW_NEEDED
- **Open question:** Which specific communication protocols (GSMR, radio, text, voice) must be integrated directly into the software interface?

## 10. Performance monitoring and dashboards

- **Category:** FUNCTIONAL
- **Origin:** EXPLICIT
- **Discovery:** Dashboards showing network punctuality, delay attribution, and resource availability are necessary for strategic monitoring.
- **Source basis:** Listed under performance monitoring tools.
- **Scope signal:** CORE_CANDIDATE
- **Open question:** None

## 11. Infrastructure and weather status monitoring

- **Category:** FUNCTIONAL
- **Origin:** EXPLICIT
- **Discovery:** The system must display real-time information on track conditions, signalling faults, power supply, engineering works, and live weather updates.
- **Source basis:** Described under infrastructure status and weather monitoring sections.
- **Scope signal:** CORE_CANDIDATE
- **Open question:** None

## 12. Missing journey states during incidents

- **Category:** STATE
- **Origin:** EXPLICIT
- **Discovery:** Journey states such as 'Incident-affected - Stationary', 'Awaiting recovery', 'Passenger evacuation underway', and 'Line Blocked' need to be tracked.
- **Source basis:** Explicitly listed under missing journey states in section 7.
- **Scope signal:** CORE_CANDIDATE
- **Open question:** None

## 13. Late notification of cancellation

- **Category:** FAILURE_SCENARIO
- **Origin:** EXPLICIT
- **Discovery:** Late notification of cancellation can lead to passengers waiting unnecessarily at stations.
- **Source basis:** Identified explicitly under failure scenarios in the cancellations section.
- **Scope signal:** REVIEW_NEEDED
- **Open question:** What automated alerts can reduce the window between a cancellation decision and passenger notification?

## 14. Unclear authority during cancellations

- **Category:** FAILURE_SCENARIO
- **Origin:** EXPLICIT
- **Discovery:** Unclear authority can lead to delays in decision-making and inefficient network use during service cancellations.
- **Source basis:** Mentioned under failure scenarios in the cancellations section.
- **Scope signal:** REVIEW_NEEDED
- **Open question:** None

## 15. Station staff resistance to platform changes

- **Category:** CONFLICT
- **Origin:** EXPLICIT
- **Discovery:** Station staff may resist platform changes due to accessibility challenges, overcrowding, or insufficient passenger notification time, posing safety concerns.
- **Source basis:** Described under conflicting responsibilities in the platform changes section.
- **Scope signal:** REVIEW_NEEDED
- **Open question:** How should the system handle a veto or warning raised by station staff against a controller-initiated platform change?

## 16. TOC service maintenance vs. Infrastructure safety

- **Category:** CONFLICT
- **Origin:** EXPLICIT
- **Discovery:** Pressure from TOCs to maintain services can conflict with safety concerns from infrastructure engineers, requiring the controller to act as arbiter prioritizing safety.
- **Source basis:** Described under conflicting responsibilities in the operational incidents section.
- **Scope signal:** REVIEW_NEEDED
- **Open question:** None

## 17. Passenger train diversions vs. Freight service priorities

- **Category:** CONFLICT
- **Origin:** EXPLICIT
- **Discovery:** Diverting a critical passenger train might necessitate delaying or cancelling a vital freight service on the alternative route.
- **Source basis:** Described under conflicting responsibilities in the route changes section.
- **Scope signal:** REVIEW_NEEDED
- **Open question:** Does the system provide automated decision support to weigh the priority trade-offs between passenger and freight services?

## 18. Strict audit logging of all actions

- **Category:** SECURITY
- **Origin:** EXPLICIT
- **Discovery:** Every decision, instruction, and authorization must be logged with a precise timestamp and the identity of the authorizing individual.
- **Source basis:** Listed under key audit requirements across all scenarios.
- **Scope signal:** CORE_CANDIDATE
- **Open question:** None

## 19. Communication and data archiving

- **Category:** SECURITY
- **Origin:** EXPLICIT
- **Discovery:** All voice and text communications, real-time train movement data, and infrastructure status data must be archived for later analysis.
- **Source basis:** Listed under communication records and real-time data archiving requirements.
- **Scope signal:** CORE_CANDIDATE
- **Open question:** None

## 20. Reduced mobility passenger platform evaluation

- **Category:** ACCESSIBILITY
- **Origin:** EXPLICIT
- **Discovery:** Alternative platforms must be checked for accessibility and specific needs for passengers with reduced mobility during platform changes.
- **Source basis:** Mentioned under information needed for platform changes.
- **Scope signal:** REVIEW_NEEDED
- **Open question:** How will accessibility metadata for each platform be maintained and queried by the system?

## 21. Passenger Information Systems integration

- **Category:** INTEGRATION
- **Origin:** EXPLICIT
- **Discovery:** Updated plans, diversions, cancellations, and platform changes must be communicated to Passenger Information Systems and station staff.
- **Source basis:** Mentioned across delay mitigation, platform changes, and route changes.
- **Scope signal:** CORE_CANDIDATE
- **Open question:** None

## 22. Multi-party operational authorization flow

- **Category:** BUSINESS_PROCESS
- **Origin:** EXPLICIT
- **Discovery:** Departures, running order adjustments, and emergency stops rely on a strict chain of authority involving the Network Controller, Signallers, Station Staff, Drivers, and TOCs.
- **Source basis:** Detailed throughout sections 1 through 8 regarding who provides information, makes decisions, and authorizes actions.
- **Scope signal:** CORE_CANDIDATE
- **Open question:** None

## 23. Driver route competence verification

- **Category:** ASSUMPTION
- **Origin:** CLEAR_INFERENCE
- **Discovery:** It is assumed that the network controller or TOC can reliably verify whether an assigned driver is competent and familiar with a diversionary route before authorizing it.
- **Source basis:** Mentioned under route changes where driver familiarity with the route must be confirmed.
- **Scope signal:** REVIEW_NEEDED
- **Open question:** Is driver route competence tracked automatically within the system or verified manually?

## 24. Commercial vs Operational cancellation distinction

- **Category:** AMBIGUITY
- **Origin:** EXPLICIT
- **Discovery:** Cancellations involve a commercial decision by TOCs based on resources and a separate operational cancellation by the Network Controller to free up paths, but the exact workflow boundary is fluid.
- **Source basis:** Described in the cancellations section regarding TOC commercial decisions versus network controller operational cancellations.
- **Scope signal:** REVIEW_NEEDED
- **Open question:** Can an operational cancellation be triggered automatically if a TOC fails to confirm resource availability by a specific threshold time?
