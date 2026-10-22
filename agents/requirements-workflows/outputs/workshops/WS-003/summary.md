# WS-003 — Customer Service Journey Information, Ticketing, and Disruption Support

## Participants

- customer_service
- product_owner

## Agreements

- Both stakeholders completely agree on the absolute necessity of establishing a definitive Single Source of Truth (SSOT) for live train data to eliminate conflicting information across mobile apps, station screens, and CSR dashboards.
- Both stakeholders recognize the critical importance of providing clear, immediate, and unambiguous visibility into inter-operator ticket validity rules during major disruptions.
- Both stakeholders agree that building a fully automated, real-time hotel booking engine and taxi dispatch system for the MVP is unrealistic, high-risk, and prone to scope creep.

## Disagreements

- Customer Service argues that stopping disruption recovery support at information, deep-links, and reference codes is inadequate and abandons passengers and frontline staff during a crisis, whereas the Product Owner maintains that SmartRail should focus strictly on communication, guidance, and eligibility transparency for the MVP rather than owning financial and logistical fulfillment.
- Customer Service advocates for actionable, streamlined digital voucher and authorization workflows within the SmartRail system to prevent helpdesk gridlock, while the Product Owner warns that automating financial and logistical recovery workflows risks turning SmartRail into a central clearinghouse of financial liability.

## Ownership Questions

- Who owns the authoritative entry and updating of 'Inter-Operator Ticket Validity' rules in the ecosystem when a disruption is declared?
- Where does the boundary lie between automated system policy and CSR frontline discretion when issuing emergency overrides, vouchers, or alternative route passes?

## Scope Questions

- Does the SmartRail MVP encompass extended passenger services such as compensation claims logging, digital voucher generation, and refund orchestration, or is it strictly limited to read-only information display and deep-links?
- Should SmartRail maintain session context and pre-populate disruption details into recovery workflows, or rely entirely on external operator websites?

## Assumptions to Validate

- The assumption by the Product Owner that providing deep-links and reference codes is sufficient for passenger disruption recovery without causing severe helpdesk congestion.
- The assumption by Customer Service that SmartRail software can securely handle structured digital voucher generation or authorization workflows without inheriting third-party financial and legal liabilities.

## Decision Points

### Decision 1

What is the exact boundary of extended passenger services (CF-011) for the MVP?

**Options:**

- Option A: Strictly limit the MVP to informing passengers of rights, displaying ticket validity rules, and providing deep-links or reference codes for manual external claims.
- Option B: Include structured digital voucher generation tools and integrated claims logging workflows within the SmartRail CSR interface while leaving physical fulfillment to operators.

**Affected findings:** CF-011

### Decision 2

How should inter-operator ticket validity rules and dynamic updates be integrated and enforced?

**Options:**

- Option A: Require manual pre-negotiated static rules uploaded by management prior to incidents.
- Option B: Implement a standardized, real-time repository feed that updates CSR interfaces and passenger apps dynamically during unfolding crises.

**Affected findings:** CF-019


## Unresolved Questions

- What is the exact agreed-upon handoff protocol and SLA when a passenger requires emergency accommodation or alternative transport during a disruption?
- How will real-time inter-operator ticket-acceptance agreements be ingested and validated from external third-party rail companies during an active crisis?