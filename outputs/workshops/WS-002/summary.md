# WS-002 — Catering Disruption Resilience and Support Escalation

## Participants

- catering_provider
- customer_service

## Agreements

- SmartRail and Customer Service must act as the primary interface for the passenger, ensuring passengers are not bounced to an external merchant or independent vendor for support.
- Frontline customer service agents should not be expected to troubleshoot backend network messaging failures, database logs, or API payloads.
- There is a clear separation of operational domains: the catering provider does not manage railway operations (schedules, platform changes, signals), and customer service does not manage kitchen inventory or food preparation workflows.

## Disagreements

- The catering provider challenges automatic or unverified "bill-backs" for service recovery, asserting they will not accept financial liability or arbitrary billing-backs unless there is objective proof the failure originated strictly from their kitchen side.
- Customer service challenges relying on eventual consistency or asynchronous webhooks for real-time dispute handling, arguing that agents cannot tell a passenger to wait 24 hours for systems to reconcile order status.

## Ownership Questions

- Who is the primary contact point and who owns the ultimate resolution for passenger order disputes?
- Who funds and authorizes immediate onboard service recovery alternatives, such as alternative snacks or complimentary vouchers provided when an order fails?
- Who absorbs the cost of wasted meal preparation during last-minute railway disruptions or sudden platform changes?
- How are refunds and financial reversals executed (e.g., whether SmartRail handles transactions directly and manages vendor chargebacks, or if refunds must go through the vendor's merchant account)?

## Scope Questions

- What is the exact boundary where the catering provider stops cooking when a train is delayed, cancelled, or rerouted?
- To what extent can frontline staff authorize comp meals or alternative items that pull from catering inventory without causing unbudgeted inventory shrink?

## Assumptions to Validate

- The assumption that the catering provider should act as a frontline support agent for passengers experiencing order disputes.
- The assumption that frontline customer service staff can act as an effective mediator or manual reconciler for technical messaging failures and asynchronous webhooks.

## Decision Points

### Decision 1

What is the definitive automated source of truth and real-time transaction state API for order status?

**Options:**

- Real-time synchronized status API read by both systems
- Asynchronous event-driven webhooks with idempotency keys
- Periodic batch reconciliation protocols

**Affected findings:** CF-006, CF-008

### Decision 2

What is the exact financial clearinghouse and merchant-of-record policy for disputed failures and wasted prep costs?

**Options:**

- SmartRail handles all financial transactions directly and manages automated vendor chargebacks behind the scenes
- SmartRail acts as a marketplace aggregator where refunds and chargebacks are processed through the vendor's merchant account via a pre-defined fault-determination protocol

**Affected findings:** CF-008

### Decision 3

What is the mandatory frontline service recovery protocol when an order fails?

**Options:**

- Frontline staff authorized to instantly issue complimentary onboard items/vouchers with automated cost apportionment
- Frontline staff strictly limited to logging a support ticket for future asynchronous refund review

**Affected findings:** CF-008


## Unresolved Questions

- How do SmartRail and the catering provider reconcile lost order creation messages or achieve eventual consistency during network failures without burdening frontline staff?
- What specific automated fault-determination protocol can accurately separate railway-induced failures from catering-side preparation errors for financial clearing?