# WS-001 — Catering Integration, Operations, and Financial Liability

## Participants

- catering_provider
- product_owner

## Agreements

- Partial item-level cancellations are out of scope for the MVP and orders must be treated as atomic units.
- The catering provider is the absolute source of truth for their culinary catalogs, pricing, allergen information, and station availability, with SmartRail avoiding manual alterations.
- Financial liability should generally follow operational ownership (SmartRail owns the railway journey; the caterer owns meal preparation).
- The catering system must act as the absolute arbiter for cancellation cut-offs via its API responses, with SmartRail deferring to these rules rather than doing local validation.

## Disagreements

- The catering provider requires dynamic, real-time Estimated Times of Arrival (ETAs) during disruptions to manage kitchen preparation, while the Product Owner argues that hyper-precise predictive signaling cannot be a hard dependency for the MVP and requires a pragmatic fallback like buffer windows.
- The catering provider expresses concern over SmartRail's handling of payments and merchant-of-record liabilities, whereas the Product Owner insists SmartRail must handle the front-end transaction for a single digital basket while utilizing an automated clearing/settlement mechanism behind the scenes.

## Ownership Questions

- Who owns the initial customer support desk complaint and resolution when an order is missing, cold, or incorrect upon collection?
- Who explicitly triggers refunds in the system and owns the operational mechanism for handling disputed refunds or wasted food claims?

## Scope Questions

- Should the system support partial cancellations of individual order items in future phases beyond the MVP?
- Should dynamic, real-time ETA data feeds be mandatory for the initial integration scope or deferred to a later iteration?

## Assumptions to Validate

- The assumption that third-party catering providers should absorb disruption costs or unmitigated financial risk for railway-induced delays.
- The assumption that SmartRail can act as an unshielded financial guarantor or merchant of record for vendor food waste without a structured settlement mechanism.

## Decision Points

### Decision 1

What is the exact commercial clearing and settlement model between SmartRail and the catering providers?

**Options:**

- SmartRail acts as the merchant of record with automated revenue splits, deductions, and a pre-agreed SLA compensation cap for railway-disrupted food waste.
- SmartRail acts purely as an external affiliate/discovery portal passing payment details and liability directly to the vendor's proprietary system.

**Affected findings:** CF-002

### Decision 2

How will train delays be communicated to the catering provider's API during disruptions?

**Options:**

- Provide dynamic, real-time Estimated Times of Arrival (ETAs) via webhooks to allow precise kitchen scheduling adjustments.
- Provide standard buffer windows or binary delay flags tied to scheduled departures to bypass complex predictive signaling dependencies for the MVP.

**Affected findings:** CF-003


## Unresolved Questions

- How often must menu data be refreshed if SmartRail caches the catalog data?
- What is the definitive API event contract and handling mechanism for network dropouts at stations to prevent double-preparation or ghost orders?
- How will proof of food preparation be formally established when a train is suddenly canceled shortly before arrival?