# WS-005 — Driver Safety Notices, System Fallbacks, and DMI Operations

## Participants

- driver
- railway_controller

## Agreements

- Drivers must never act as IT fallback administrators or attempt to debug software, reboot servers, or troubleshoot hardware while operating a train.
- Manual fallback operations and degraded states cannot be handled as isolated local transactions and must involve real-time notification and coordination with Network Control.
- Unmonitored, indefinite reliance on stale cached data during connectivity losses represents a critical risk to train separation and safety integrity.
- Drivers must not invent manual schedules, route authorities, or make unilateral safety judgements during system outages.

## Disagreements

- The driver advocates for strict rulebook fallbacks including immediate stops or clear boundaries, whereas the controller is concerned that uncoordinated or immediate stops on active mainlines can create secondary hazards, gridlock, and cascade delays.
- The driver emphasizes that a controller's desire to maintain timetable flow must not pressure them into moving a train with critical defects, while the controller asserts absolute operational authority to clear pathing and manage network access during unexpected failures.

## Ownership Questions

- Who bears the procedural responsibility to initiate contact when a DMI goes dark or connectivity drops (the driver calling control or the central system detecting the drop and prompting the controller)?
- Who holds the operational authority to grant temporary extensions to offline caching limits versus ordering an immediate stop when a train hits maximum buffering times in a dead zone?

## Scope Questions

- Can a train ever be operated under manual fallback information if the DMI fails completely, or does a total DMI failure mandate an immediate stop and rescue/pilotman operation?
- How extensive can voice-based communications be during degraded states, or should voice commands be strictly limited to essential safety commands rather than acting as a substitute data channel?

## Assumptions to Validate

- The assumption that drivers can manage fallback operations locally without direct real-time integration into central control visibility.
- The assumption that continuous voice dialogue between drivers and controllers during system outages is practical without inducing severe cognitive overload and radio congestion.

## Decision Points

### Decision 1

What is the explicit operational protocol and state transition when a total DMI failure occurs at line speed?

**Options:**

- Enforce an immediate automatic safe brake application to a complete stop.
- Transition into a standardized analog/audio-alert 'crawl-to-siding' fallback mode authorized exclusively via central control.

**Affected findings:** CF-028

### Decision 2

What are the hard limits on offline data caching before mandatory safety actions are triggered?

**Options:**

- Enforce a strict, hard time/distance limit (in minutes or miles) that automatically triggers a mandatory stop-and-communicate rule.
- Allow controlled, controller-authorized extensions under specific degraded visibility protocols.

**Affected findings:** CF-032


## Unresolved Questions

- How does the central SmartRail system visually represent 'blind' or degraded trains on the dispatcher board to ensure controllers apply proper manual spatial separation when real-time telemetry is lost?
- What specific rulebook criteria define the exact boundary between minor defects permitting cautious operation and major defects requiring immediate line protection and withdrawal from service?
- What mechanism will deliver and receive mandatory auditable acknowledgements for critical safety notices prior to shift start during system outages?