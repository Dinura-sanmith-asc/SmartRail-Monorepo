# Driver Discovery Analysis

## 1. Delivery and acknowledgement method for critical safety notices

- **Category:** AMBIGUITY
- **Origin:** EXPLICIT
- **Discovery:** It is unclear how critical safety notices are delivered and acknowledged prior to shift start, and mandatory auditable confirmation needs to be established.
- **Source basis:** The driver notes a missing requirement regarding how critical safety notices are delivered and acknowledged and that it needs to be mandatory and auditable.
- **Scope signal:** REVIEW_NEEDED
- **Open question:** How are critical safety notices delivered and acknowledged before proceeding?

## 2. System assignment provider downtime during pre-assignment

- **Category:** FAILURE_SCENARIO
- **Origin:** EXPLICIT
- **Discovery:** The system providing shift assignments might experience downtime, requiring a robust, reliable, manual fallback process.
- **Source basis:** The driver identifies a scenario where the system providing assignments is down and highlights the need for a manual fallback process.
- **Scope signal:** REVIEW_NEEDED
- **Open question:** What is the exact manual fallback process if the assignment system is down?

## 3. Definition of active acknowledgement mechanism for journey assignments

- **Category:** AMBIGUITY
- **Origin:** EXPLICIT
- **Discovery:** It remains ambiguous whether acknowledgement of a journey assignment is a simple button press or requires active review and confirmation of key details.
- **Source basis:** The driver asks how acknowledgement is handled and suggests it should be an active confirmation rather than a simple button press.
- **Scope signal:** REVIEW_NEEDED
- **Open question:** Is acknowledgement a simple button press or an active confirmation of key details?

## 4. Single source of truth precedence for journey information

- **Category:** CONFLICT
- **Origin:** EXPLICIT
- **Discovery:** Discrepancies may occur between the on-board system, Control, or paper documents, and a clear precedence must be defined.
- **Source basis:** The driver questions what the single source of truth is if the on-board system differs from Control or paper documents.
- **Scope signal:** REVIEW_NEEDED
- **Open question:** Which source takes precedence if on-board system data differs from Control or paper documents?

## 5. DMI journey data loading and error handling

- **Category:** FUNCTIONAL
- **Origin:** CLEAR_INFERENCE
- **Discovery:** The Driver Machine Interface (DMI) must support loading journey details during pre-departure checks and handle potential loading errors.
- **Source basis:** The driver lists loading journey details into the DMI as an action and identifies DMI loading failure or display errors as potential failure scenarios.
- **Scope signal:** CORE_CANDIDATE
- **Open question:** None

## 6. DMI complete failure protocol and manual operation fallback

- **Category:** AMBIGUITY
- **Origin:** EXPLICIT
- **Discovery:** The protocol and fallback information available to the driver if the DMI fails completely during checks are unclear.
- **Source basis:** The driver asks what the protocol is if the DMI fails completely during checks and whether manual operation is still possible.
- **Scope signal:** REVIEW_NEEDED
- **Open question:** Can the train be operated manually with fallback information if the DMI fails completely?

## 7. Distinction between minor and major train defects

- **Category:** AMBIGUITY
- **Origin:** EXPLICIT
- **Discovery:** A clear distinction between a minor defect allowing cautious operation and a major defect requiring attention is currently missing.
- **Source basis:** The driver highlights a missing requirement for a clear distinction between 'minor defect' and 'major defect'.
- **Scope signal:** REVIEW_NEEDED
- **Open question:** What criteria define a minor defect versus a major defect?

## 8. System role in preventing early departures

- **Category:** FUNCTIONAL
- **Origin:** EXPLICIT
- **Discovery:** The extent of the system's role in detecting or preventing early departures beyond displaying scheduled times is undefined.
- **Source basis:** The driver questions what the system's role is in detecting/preventing early departures beyond just displaying scheduled time.
- **Scope signal:** REVIEW_NEEDED
- **Open question:** What role does the system play in actively preventing early departures?

## 9. Platform clear verification integration

- **Category:** INTEGRATION
- **Origin:** CLEAR_INFERENCE
- **Discovery:** Verification of platform clear status is primarily visual, but potential integration with platform edge detection needs clarification.
- **Source basis:** The driver notes that platform clear verification is primarily visual and asks about any integration with platform edge detection.
- **Scope signal:** EXTENSION_CANDIDATE
- **Open question:** Is there any integration with platform edge detection systems for verifying platform clear status?

## 10. Connectivity loss during en-route operations

- **Category:** FAILURE_SCENARIO
- **Origin:** EXPLICIT
- **Discovery:** Loss of connectivity prevents receiving new instructions or reporting issues, requiring the system to degrade gracefully by holding last valid instructions and warning the driver.
- **Source basis:** The driver identifies connectivity loss as a failure scenario where the system must degrade gracefully and hold valid instructions.
- **Scope signal:** CORE_CANDIDATE
- **Open question:** How long can the system operate on cached data during connectivity loss?

## 11. Precedence of safety-critical conflicting information

- **Category:** SECURITY
- **Origin:** EXPLICIT
- **Discovery:** When conflicting information occurs (e.g., DMI speed vs. trackside sign), safety-critical information must have clear precedence.
- **Source basis:** The driver notes that safety-critical information must have clear precedence when DMI and trackside signs conflict.
- **Scope signal:** CORE_CANDIDATE
- **Open question:** None

## 12. Door side selection verification and alert

- **Category:** FUNCTIONAL
- **Origin:** EXPLICIT
- **Discovery:** The system should ideally provide visual confirmation and an audible alert if the incorrect door side is selected during station stops.
- **Source basis:** The driver states that visual confirmation and an audible alert if an incorrect side is selected would be good to ensure correct door opening.
- **Scope signal:** CORE_CANDIDATE
- **Open question:** None

## 13. Precision stopping guidance support

- **Category:** SCOPE_CANDIDATE
- **Origin:** CLEAR_INFERENCE
- **Discovery:** The system could potentially support precision stopping via dynamic braking guides.
- **Source basis:** The driver asks how the system supports precision stopping, citing examples like dynamic braking guides.
- **Scope signal:** EXTENSION_CANDIDATE
- **Open question:** Should the system provide dynamic braking guides for precision stopping?

## 14. Emergency communications and reporting priority

- **Category:** FUNCTIONAL
- **Origin:** EXPLICIT
- **Discovery:** SmartRail must prioritize emergency communications and reports so they take precedence over all other data.
- **Source basis:** The driver states that SmartRail must prioritize emergency communications and reports, ensuring they take precedence over other data.
- **Scope signal:** CORE_CANDIDATE
- **Open question:** None

## 15. DMI emergency stop button and automatic reporting

- **Category:** FUNCTIONAL
- **Origin:** EXPLICIT
- **Discovery:** The DMI needs a clear emergency stop button/interface that automatically reports to Control during emergencies.
- **Source basis:** The driver asks if the DMI provides a clear 'Emergency Stop' button/interface that automatically reports to Control.
- **Scope signal:** CORE_CANDIDATE
- **Open question:** None

## 16. Post-journey checklist enforcement on DMI

- **Category:** BUSINESS_PROCESS
- **Origin:** CLEAR_INFERENCE
- **Discovery:** The system needs to ensure proper train securing procedures are followed, potentially through a mandatory DMI checklist.
- **Source basis:** The driver asks how the system ensures proper securing procedures are followed, suggesting a checklist that must be completed on the DMI.
- **Scope signal:** EXTENSION_CANDIDATE
- **Open question:** Should train securing procedures be enforced via a mandatory DMI checklist?

## 17. Integration between DMI and post-journey reporting systems

- **Category:** INTEGRATION
- **Origin:** EXPLICIT
- **Discovery:** Integration is required between DMI real-time data and post-journey reporting systems to determine auto-population and driver validation roles.
- **Source basis:** The driver highlights a missing requirement regarding the integration between DMI real-time data and post-journey reporting systems, including auto-population.
- **Scope signal:** REVIEW_NEEDED
- **Open question:** How much auto-population happens from the DMI to reporting systems, and what is the driver's validation role?

## 18. Strict operational boundaries and prohibitions

- **Category:** OPERATIONAL
- **Origin:** EXPLICIT
- **Discovery:** Drivers are restricted from performing specific actions without explicit authority, such as modifying schedules, bypassing safety interlocks, permanently silencing critical alarms, changing train identity, exceeding speed limits, passing signals at danger, overriding safety systems, modifying other crew data, disabling reporting, or editing events without an audit trail.
- **Source basis:** The driver lists specific actions they should NOT be allowed to perform without explicit, auditable authority across all stages.
- **Scope signal:** CORE_CANDIDATE
- **Open question:** None

## 19. Degraded mode and automatic report buffering during connectivity loss

- **Category:** OPERATIONAL
- **Origin:** EXPLICIT
- **Discovery:** During connectivity loss, the system must buffer reports locally (arrival/departure times) and transmit them upon reconnection while providing clear visual or audible connectivity status indicators.
- **Source basis:** The driver specifies that reports should be automatically buffered on the DMI during connectivity drops and transmitted later, alongside clear status indications.
- **Scope signal:** CORE_CANDIDATE
- **Open question:** None

## 20. Signaller, Duty Manager, Shunter, and Maintenance staff interaction

- **Category:** MISSING_ACTOR
- **Origin:** CLEAR_INFERENCE
- **Discovery:** The workflow relies on interactions with external operational roles such as Control, Signallers, Duty Managers, Shunters, Maintenance staff, and Medical personnel who provide authority, clearances, or briefs.
- **Source basis:** Throughout the journey stages, the driver references interactions and required authorities involving Control, Signallers, Duty Managers, shunt teams, and maintenance staff.
- **Scope signal:** REVIEW_NEEDED
- **Open question:** Are these external operational roles direct users of SmartRail or interfacing via separate systems?
