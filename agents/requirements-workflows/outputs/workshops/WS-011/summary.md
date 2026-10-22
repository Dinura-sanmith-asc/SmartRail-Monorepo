# WS-011 — Station Local Incident Management, Displays, and Resilience

## Participants

- station_manager
- product_owner

## Agreements

- Local human safety on the ground must ultimately supersede automated network optimization algorithms.
- Station staff must have a frictionless, immediate veto or override authority over platform allocations when local safety constraints demand it.
- The system should avoid Enterprise Asset Management (EAM) bloat, utilizing a simple and reliable 'report and resolve' closure loop for facility outages rather than a heavy corporate maintenance suite.
- Fully automated, real-time density ingestion without manual human context or constraints is risky for the MVP and requires caution.

## Disagreements

- The Station Manager insists that basic automated threshold alerts (CCTV or barrier velocity metrics) feeding into the station interface are required immediately to prevent crushing hazards, whereas the Product Owner views automated density ingestion as a post-MVP enhancement.
- The Station Manager worries that relying on manual local triggers for crowding will cause operational blindness or excessive cognitive load, while the Product Owner cautions against scope creep and the technical risks of automated ingestion.

## Ownership Questions

- Who specifically owns the definition of pre-approved announcement templates versus ad-hoc override privileges for public announcements?
- Who is responsible for establishing the RACI matrix for station communication protocols and managing safety-critical override guardrails?
- Where does the boundary lie between software functional requirements defined by the Product Owner and hardware/infrastructure resilience specifications managed by engineering teams?

## Scope Questions

- What is the exact boundary between a simple local incident logger and complex tiered incident management workflows for the MVP?
- To what extent must offline resilience and backup power capabilities be deployed across all station tiers versus high-priority nodes during a wide-area network failure?
- How far can SmartRail's local incident input extend before crossing into a heavy, multi-tiered corporate maintenance suite?

## Assumptions to Validate

- Assumption that network-wide optimization algorithms can seamlessly and automatically ingest real-time local station density metrics without manual intervention or contextual modifiers (CF-088).
- Assumption that stations can rely purely on manual local context triggers during peak operational hours without risking operational blindness or delayed crowd safety responses.

## Decision Points

### Decision 1

When a Station Manager executes a local safety veto on a platform allocation, does it take effect instantly at the local interface without waiting, and how is it communicated back to central control?

**Options:**

- Immediate local override with automatic, real-time broadcast of impact to central control for downstream visibility.
- Local override that queues for central dispatcher approval before taking effect.

**Affected findings:** CF-079, CF-088

### Decision 2

What is the MVP implementation scope for real-time station density metrics and automated threshold alerts?

**Options:**

- Include basic automated threshold alerts and velocity metrics in the MVP to avoid manual cognitive overload during crowds.
- Defer automated density ingestion to post-MVP and rely strictly on manual local context triggers for the initial release.

**Affected findings:** CF-088, CF-084

### Decision 3

What level of offline resilience and local caching architecture is mandated for station displays, PA systems, and incident logging during a complete network severance?

**Options:**

- Full local feature redundancy and autonomous caching across all stations regardless of tier.
- Tiered offline resilience prioritized strictly by station category and regulatory safety mandates.

**Affected findings:** CF-081, CF-082, CF-085


## Unresolved Questions

- Is there a dedicated local station status module for staff to input unprompted local incidents or infrastructure failures?
- Can staff broadcast ad-hoc or pre-recorded messages, and does the system feature automated verification for announcements to prevent panic?
- Can local staff override sections of network-controlled station displays to publish local operational messages?
- What is the formal workflow to close the loop on reported infrastructure failures and update their operational status?
- Does SmartRail contain distinct tiered incident management workflows for varying levels of local severity?
- What core functionality can be maintained locally via backup power and offline cache if network connectivity is completely severed?
- What is the exact audit and audit logging requirement when a Station Manager executes a network-safety override?