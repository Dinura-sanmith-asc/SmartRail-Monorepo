# WS-006 — Driver Pre/Post-Journey Workflows, Precision Stopping, and External Roles

## Participants

- driver
- product_owner

## Agreements

- SmartRail should reject scope creep for external roles by avoiding multi-user operational modules for shunters, maintenance, or medical staff directly inside the cab app.
- Post-journey checklists must avoid workflow gridlock that could trap a train or driver in an un-cleared state during degraded operations or emergency reliefs.
- Any dynamic braking guides or precision stopping assistance implemented for the MVP must be strictly advisory to account for real-time adhesion and avoid driver distraction.
- Auto-population of journey logs and reports requires a streamlined verify or edit mechanism rather than a bureaucratic text-entry hurdle to protect turnaround times.
- A clear system precedence rule is needed, with operational consensus that verbal or written instructions from Control or signallers override on-board systems, which override static paper documents.

## Disagreements

- The Product Owner advocates dropping dynamic braking guides for the MVP to avoid complexity and liability, whereas the driver argues that completely abandoning stopping assistance increases overshoot risks and cognitive load during station approaches.
- The driver requests DMI visibility into which external roles (such as shunters or maintenance) gave clearance or permission, while the product owner warns that this would introduce a sprawling enterprise integration beast and blow up MVP scope.

## Ownership Questions

- Who owns the operational authority to manually unlock a driver's terminal or override a frozen post-journey checklist when connectivity fails or an emergency relief driver takes over mid-shift?
- Who is responsible for reconciling critical data discrepancies flagged by the system between paper, Control, and on-board data before a journey proceeds?
- How are verbal instructions from Control officially logged into SmartRail if the data link is degraded to ensure audit trails match ground execution?

## Scope Questions

- Should external role interactions be limited to a single 'Control Authority' flag, leaving shunter and maintenance clearances as verbal or legacy workflows managed outside SmartRail for the MVP?
- To what extent should real-time status updates from external systems be integrated into the DMI versus handled strictly via Control?

## Assumptions to Validate

- The assumption that more auto-population is always better, without accounting for the risk of drivers blindly signing off on large generated logs.
- The assumption that post-journey sign-off enforcement can rely entirely on cloud-based connectivity without offline-first local securement and deferred syncing capabilities.

## Decision Points

### Decision 1

Should precision stopping guidance (dynamic braking guides) be included in the MVP as an advisory feature, or deferred to a later phase?

**Options:**

- Include advisory dynamic braking guides in the MVP to assist with station approaches.
- Defer dynamic braking guides entirely to future phases to minimize scope and liability.

**Affected findings:** CF-035

### Decision 2

What is the boundary of external role integration for the MVP DMI?

**Options:**

- Display granular status clearances from multiple external roles (Shunters, Maintenance, Signallers) directly on the DMI.
- Limit DMI external integration to a binary 'Control Authority/Clearance' status, keeping other external role handshakes in legacy or verbal workflows.

**Affected findings:** CF-040

### Decision 3

How should the system handle offline-first post-journey sign-offs and securement validation?

**Options:**

- Require real-time cloud connectivity to complete journey sign-off and lock in audit trails.
- Support offline-first local securement with deferred cloud syncing backed by a regulated manual fallback protocol.

**Affected findings:** CF-037, CF-038


## Unresolved Questions

- What is the exact fallback protocol when the DMI freezes or loses connectivity during active operation or terminal arrival?
- How can the system legally and operationally guarantee train securement during a digital software freeze without creating a runaway train or security liability risk?