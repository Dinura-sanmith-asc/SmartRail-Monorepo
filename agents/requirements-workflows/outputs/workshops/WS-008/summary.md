# WS-008 — Passenger Journey Disruption, Navigation, and Support Services

## Participants

- passenger
- customer_service

## Agreements

- Offline tickets and cryptographic token access are essential features, not optional nice-to-haves, to address station signal loss and dead device scenarios (CF-052).
- Conflicting information between app notifications and frontline staff creates distrust and customer service desk friction, making synchronized data sharing critical.
- Passengers cannot exclusively self-serve out of disruptions, and digital tools must account for the reality that stranded passengers will rely heavily on frontline customer service staff.
- SmartRail and customer service systems must maintain integrated visibility to prevent passengers from having to manually explain their digital re-booking history to staff.

## Disagreements

- The scope of SmartRail's compensation management (CF-056): Passengers want SmartRail to act as a single, unified window for auto-populated claims, whereas customer service highlights operational, legal, and multi-operator financial settlement risks if SmartRail acts as a central financial clearinghouse without backend agreements.
- The feasibility and impact of customized push notification thresholds (CF-051): Passengers want to customize alerts for minor delays (e.g., 5 minutes), while customer service warns this could generate thousands of low-impact inquiries and overwhelm staff capacity during critical disruptions.
- The balance between automated self-service and frontline capacity: Passengers demand instant, automated app-based alternative re-routing even when staff are overwhelmed, whereas customer service warns that automation detached from physical train capacity and staff visibility creates severe operational bottlenecks.

## Ownership Questions

- Who owns the trigger and execution of active physical rescue or assistance when an accessibility breakdown occurs (e.g., a 'ghost lift' trapping a passenger) versus automated digital alerts (CF-043, CF-053)?
- Who is operationally responsible for simultaneously pushing synchronized alternative routing data to both the SmartRail app and frontline customer service screens?
- Which entity holds liability and financial ownership for multi-operator Delay Repay payouts under a centralized compensation model (CF-056)?

## Scope Questions

- Does SmartRail's scope include acting as a central financial clearinghouse for multi-operator compensation claims, or is it strictly an information portal and redirection tool (CF-056)?
- Does the scope of offline mode encompass full digital token/cryptographic verification at barriers, or does it require supplementary physical paper print backups and SMS-based fallback codes (CF-052)?
- To what level of granularity must station accessibility data be mapped (e.g., specific measurements versus general tags) (CF-043)?

## Assumptions to Validate

- The assumption that passengers can entirely self-serve out of disruptions via app notifications without impacting frontline staff workload.
- The assumption that offline ticket authentication can be securely implemented without constant live cloud connectivity at station barriers.
- The assumption that train operating companies will agree to a unified financial settlement and compensation clearinghouse model managed through SmartRail.

## Decision Points

### Decision 1

How should SmartRail handle offline ticket authentication during signal loss or device failure at station barriers?

**Options:**

- Implement cryptographic offline tokens stored locally on the device.
- Provide station-based temporary paper backup ticket printing.
- Use SMS-based fallback codes for barrier validation.

**Affected findings:** CF-052

### Decision 2

What is the official policy and automation level for missed connections and re-booking?

**Options:**

- Provide fully automated, guaranteed re-booking and issue a digital 'Protected Connection Pass' to the app instantly.
- Present a list of alternative options in the app for the passenger to manually choose while frontline staff issue physical overrides.
- Require passengers to seek manual re-validation and authorization directly from station staff.

**Affected findings:** CF-055

### Decision 3

What is the operational model for compensation management (Delay Repay)?

**Options:**

- SmartRail acts as a centralized financial clearinghouse managing auto-populated, one-click claims across all operators.
- SmartRail serves as an integrated information desk that auto-populates forms but redirects final submission to individual operator portals.

**Affected findings:** CF-056

### Decision 4

How should push notification thresholds be managed for day-of-travel disruptions?

**Options:**

- Allow passengers to fully customize their notification thresholds (e.g., alerting for 5-minute delays).
- Enforce a fixed system-wide threshold for significant delays (e.g., 15+ minutes) to prevent operational panic and help-desk overload.

**Affected findings:** CF-051


## Unresolved Questions

- What specific threshold constitutes a significant delay for triggering a push notification, and what level of detail will disruption explanations contain (CF-051)?
- What is the exact accuracy level of coach stopping position predictions, and how will real-time lift and escalator statuses be consistently reported from all stations (CF-053)?
- What operational feedback loop ensures that unexpected station infrastructure failures update passenger apps and customer service screens instantly to prevent stranded passengers (CF-043, CF-053)?