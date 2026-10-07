# Station Manager Discovery Analysis

## 1. Pre-opening physical walk-through

- **Category:** BUSINESS_PROCESS
- **Origin:** EXPLICIT
- **Discovery:** The station team performs a physical walk-through before the first train to check platforms, ticket gates, lifts, escalators, PA systems, and information boards, while printing out schedules.
- **Source basis:** My team does a physical walk-through: checking platforms for debris, ensuring ticket gates are operational, checking the status of lifts and escalators, testing PA systems, and making sure the information boards are displaying the initial scheduled services correctly.
- **Scope signal:** CORE_CANDIDATE
- **Open question:** None

## 2. Real-time manifest of train arrivals and departures

- **Category:** FUNCTIONAL
- **Origin:** EXPLICIT
- **Discovery:** SmartRail is expected to provide a real-time manifest of early trains including predicted tracks and pre-planned cancellations or significant delays.
- **Source basis:** I'd expect SmartRail to provide a real-time manifest of the first few hours' trains, including their predicted arrival/departure tracks and any pre-planned cancellations.
- **Scope signal:** CORE_CANDIDATE
- **Open question:** None

## 3. Accessibility facilities operational status monitoring

- **Category:** FUNCTIONAL
- **Origin:** EXPLICIT
- **Discovery:** SmartRail should display the operational status of all lifts, escalators, and accessible toilets to immediately put up signs and update accessible route information.
- **Source basis:** SmartRail should show me the current operational status of all our lifts, escalators, and accessible toilets.
- **Scope signal:** CORE_CANDIDATE
- **Open question:** None

## 4. Station Health Dashboard

- **Category:** FUNCTIONAL
- **Origin:** EXPLICIT
- **Discovery:** Information staff require a terminal providing a clear, immediate dashboard of Station Health showing all critical infrastructure status at a glance.
- **Source basis:** I need a SmartRail terminal that gives me a clear, immediate dashboard of Station Health: all critical infrastructure status at a glance.
- **Scope signal:** CORE_CANDIDATE
- **Open question:** None

## 5. Input mechanism for newly discovered local issues

- **Category:** AMBIGUITY
- **Origin:** EXPLICIT
- **Discovery:** It is unclear how staff can input the status of a newly discovered issue like a broken ticket machine or power outage into SmartRail for others to see, and whether a local station status module exists.
- **Source basis:** How do I input the status of a newly discovered issue into SmartRail for others to see? Is there a local station status module I can update?
- **Scope signal:** REVIEW_NEEDED
- **Open question:** Is there a dedicated local station status module for staff to input unprompted local incidents or infrastructure failures?

## 6. Data discrepancy between system prediction and visual reality

- **Category:** FAILURE_SCENARIO
- **Origin:** EXPLICIT
- **Discovery:** SmartRail might predict an arrival time while the train is physically stuck outside the signal box, raising the question of who corrects this and whether manual display overrides are permitted.
- **Source basis:** What if SmartRail predicts an arrival at 07:15, but I can see with my own eyes the train is stuck outside the signal box? Who corrects this?
- **Scope signal:** REVIEW_NEEDED
- **Open question:** What is the formal procedure and audit trail for overriding incorrect automated system predictions?

## 7. Platform allocation versus local conditions and safety

- **Category:** CONFLICT
- **Origin:** EXPLICIT
- **Discovery:** SmartRail might propose a platform change to optimize network flow, but station managers need the ability to veto it if the platform is blocked, requires cleaning, or is over-capacity.
- **Source basis:** If SmartRail proposes a platform change to optimize network flow, but that platform is currently blocked... I need the ability to challenge or veto that allocation immediately.
- **Scope signal:** REVIEW_NEEDED
- **Open question:** Who has ultimate authority to override platform allocation disputes between local safety constraints and network optimization algorithms?

## 8. Division of authority for platform changes and actions

- **Category:** OPERATIONAL
- **Origin:** EXPLICIT
- **Discovery:** Major platform re-shuffles for an entire line require central authorization, whereas single train changes due to a local issue can be handled locally if they do not impact the wider network.
- **Source basis:** A major platform re-shuffle for an entire line, yes, central can decide. But for a single train due to a local issue... I need the flexibility to make a local call.
- **Scope signal:** CORE_CANDIDATE
- **Open question:** None

## 9. Capabilities of automated PA announcement system

- **Category:** AMBIGUITY
- **Origin:** EXPLICIT
- **Discovery:** It is ambiguous whether SmartRail only handles arrival/departure specific announcements or if staff can pre-record standard messages and trigger them, or type ad-hoc text for broadcasting.
- **Source basis:** Can I pre-record standard announcements... or does it only do arrival/departure specific ones? What about unscheduled announcements, like for a lost child?
- **Scope signal:** REVIEW_NEEDED
- **Open question:** Can staff broadcast ad-hoc, typed, or pre-recorded text messages using the automated PA system via SmartRail?

## 10. Automated announcement inaccuracies causing confusion

- **Category:** FAILURE_SCENARIO
- **Origin:** EXPLICIT
- **Discovery:** An automated announcement might state incorrect platform details, causing panic and confusion, requiring quick manual intervention or system correction.
- **Source basis:** An automated announcement goes out stating Train 1234 to City Centre on Platform 3 when it's actually on Platform 5. Panic. Confusion.
- **Scope signal:** REVIEW_NEEDED
- **Open question:** Does the system feature automated verification for announcements, or is full reliance placed on manual staff corrections during errors?

## 11. Local-only messaging capabilities on station displays

- **Category:** AMBIGUITY
- **Origin:** EXPLICIT
- **Discovery:** It is unclear whether station displays are entirely controlled by network data or if part of a display can be used to show local-only messages like cafe opening times or temporary information point closures.
- **Source basis:** Can I use part of a display to show local-only messages... Or are they entirely controlled by network data?
- **Scope signal:** REVIEW_NEEDED
- **Open question:** Can local staff override sections of network-controlled station displays to publish local operational messages?

## 12. Direct incident and facility status entry

- **Category:** FUNCTIONAL
- **Origin:** EXPLICIT
- **Discovery:** Staff need to directly log broken lifts, facility outages, passenger illnesses, broken windows, or power flickers into SmartRail, including type, location, time, and actions taken.
- **Source basis:** If a lift breaks down, I need to input that directly into SmartRail... I should be able to input incident type, location, time, and actions taken.
- **Scope signal:** CORE_CANDIDATE
- **Open question:** None

## 13. Responsibility loop closure for maintenance and repairs

- **Category:** AMBIGUITY
- **Origin:** EXPLICIT
- **Discovery:** It is unclear who is responsible for ensuring an engineer actually fixes a reported issue and marking it back in service within SmartRail.
- **Source basis:** Who is responsible for ensuring the engineer actually fixes it and then marking it back in service in SmartRail? Is it the engineer, or do I have to chase it up?
- **Scope signal:** REVIEW_NEEDED
- **Open question:** What is the formal workflow to close the loop on reported infrastructure failures and update their operational status?

## 14. Different severity workflows for local incidents

- **Category:** AMBIGUITY
- **Origin:** EXPLICIT
- **Discovery:** It is not specified how SmartRail manages different levels of local incidents, such as distinguishing a minor platform spill from a security alert with distinct workflows.
- **Source basis:** How does SmartRail manage different levels of local incidents? A spill on the platform is different from a security alert. Does it have workflows for each?
- **Scope signal:** REVIEW_NEEDED
- **Open question:** Does SmartRail contain distinct tiered incident management workflows for varying levels of local severity?

## 15. Local internet or power connectivity loss during disruptions

- **Category:** FAILURE_SCENARIO
- **Origin:** EXPLICIT
- **Discovery:** During major disruptions, local internet or power could fail, rendering terminals, displays, and central communication inoperable and requiring a robust offline mode.
- **Source basis:** What if our local internet connection or power goes down during a major disruption? All our SmartRail terminals, displays, and communication with central are gone. We need a robust offline mode.
- **Scope signal:** CORE_CANDIDATE
- **Open question:** What core functionality can be maintained locally via backup power and offline cache if network connectivity is completely severed?

## 16. Actions requiring central authorization during disruptions

- **Category:** OPERATIONAL
- **Origin:** EXPLICIT
- **Discovery:** Station closures, significant changes to services affecting multiple stations, and declaring major incidents requiring emergency services coordination require central authorization.
- **Source basis:** Station closure, significant changes to services affecting multiple stations, declaring a major incident requiring emergency services coordination beyond local first response.
- **Scope signal:** CORE_CANDIDATE
- **Open question:** None

## 17. Network clearing versus local passenger overcrowding

- **Category:** CONFLICT
- **Origin:** EXPLICIT
- **Discovery:** Central control might want to clear a platform to alleviate network congestion, stranding hundreds of local passengers and creating a local safety hazard that staff must negotiate.
- **Source basis:** Central might want to clear a platform to alleviate network congestion, but that means hundreds of our passengers are now stranded, creating a local safety issue.
- **Scope signal:** REVIEW_NEEDED
- **Open question:** None

## 18. Disruption information requirements for staff

- **Category:** FUNCTIONAL
- **Origin:** EXPLICIT
- **Discovery:** During disruptions, staff require precise clarity on affected trains, new plans, alternative transport locations, and estimated resolution times rather than generic delayed statuses.
- **Source basis:** During a disruption, I need clarity on which trains are affected, what the new plan is, where alternative transport is, and estimated resolution times. Not just delayed.
- **Scope signal:** CORE_CANDIDATE
- **Open question:** None

## 19. Integration with local security systems

- **Category:** INTEGRATION
- **Origin:** EXPLICIT
- **Discovery:** The system needs integration with local security systems such as CCTV and access control for effective incident response.
- **Source basis:** Integration with local security systems (CCTV, access control) for incident response.
- **Scope signal:** EXTENSION_CANDIDATE
- **Open question:** None

## 20. SmartRail providing local context automatically

- **Category:** ASSUMPTION
- **Origin:** CLEAR_INFERENCE
- **Discovery:** It is assumed by staff that automated network solutions should inherently factor in local station context, passenger volume, and safety constraints without needing manual local intervention.
- **Source basis:** It cannot simply be a dictatorial system pushing network-wide decisions without local context or recourse.
- **Scope signal:** REVIEW_NEEDED
- **Open question:** To what extent can network optimization algorithms ingest real-time local station density metrics automatically?
