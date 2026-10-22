# SmartRail Qa Review

**Findings:** 4

## QA-001 — Unsynchronized Catering Order State During Railway Disruption

**Category:** QA_GAP
**Severity:** HIGH
**Area:** External Catering Integration & Passenger Mobile App

**Finding:** Although AD-011 modifies order-state synchronization to rely on idempotent requests, status refresh, and optional webhooks, there is no explicit testable requirement defining how the Passenger Mobile App handles food orders when a train experiences sudden cancellation or major en-route delays while an order is in 'PREPARING' or 'READY' status.

**Candidate requirement:** SmartRail must query the external catering provider API to refresh order state whenever a journey experiences an operational cancellation or a delay exceeding 15 minutes, and display a clear contextual warning to the passenger regarding collection feasibility.

**Sources:** AD-003, AD-011, AD-014

**Question:** How does the passenger know if their food order is still valid or will be fulfilled at a different station when a train is unexpectedly terminated?

**Verification:** Simulate a live cancellation event on an active food order, verify that the SmartRail backend calls the catering provider status API, and confirm that the passenger mobile app displays the updated order state and collection advisory within 10 seconds.

**Architect status:** PENDING REVIEW

## QA-002 — Missing Explicit Handling for Expired Offline Cache in Driver DMI

**Category:** MISSING_REQUIREMENT
**Severity:** HIGH
**Area:** Driver / Train Operator Mobile Application

**Finding:** While AD-030 establishes that cached operational data must have explicit validity and time limits, it lacks a concrete testable definition of what the Driver Mobile App (DMI) UI displays and how the driver is blocked or guided when the TTL expires during prolonged network loss.

**Candidate requirement:** When cached operational schedule and route data exceeds its defined validity limit without network reconnection, the Driver Application must transition into an explicit 'Stale Data / Degraded Mode' visual state, disable dependent operational actions, and prompt the driver to execute approved voice/radio fallback procedures.

**Sources:** AD-030, AD-031, AD-034

**Question:** What exact visual indication and operational restriction occurs on the driver's device when the offline cache TTL expires?

**Verification:** Mock a persistent network outage on the Driver DMI, allow the cache TTL to expire, and verify that the UI renders a prominent warning banner, blocks schedule modification actions, and logs the degraded state transition.

**Architect status:** PENDING REVIEW

## QA-003 — Ambiguous Station Display Behavior During Network Disconnection

**Category:** AMBIGUITY
**Severity:** MEDIUM
**Area:** Railway Station Digital Displays

**Finding:** The brief and decisions emphasize local resilience and cached data for stations during connectivity loss (AD-070, AD-073), but fail to specify whether digital displays should show an explicit 'offline/cached' indicator to passengers when real-time updates are lost, risking passenger confusion during platform changes.

**Candidate requirement:** Station digital passenger-information displays must show a discreet, persistent visual indicator whenever the displayed schedule data is operating from local cache due to central network loss.

**Sources:** AD-022, AD-070, AD-073

**Question:** Will passengers waiting on a platform know if the departure board data is live or frozen during a network outage?

**Verification:** Disconnect the central data feed to a simulated station display terminal and verify that the screen continues to render cached departures while displaying an explicit 'Cached Data' indicator.

**Architect status:** PENDING REVIEW

## QA-004 — Audit Trail Completeness for Local Station Safety Overrides

**Category:** QA_GAP
**Severity:** MEDIUM
**Area:** Railway Operations Platform & Station Terminals

**Finding:** AD-079 requires recording actor, timestamp, reason, affected service, and resulting state for local station safety overrides, with offline synchronization, but does not define the conflict-resolution mechanism if two conflicting overrides are synced simultaneously after connectivity recovery.

**Candidate requirement:** When offline station safety override events are synchronized back to the Central Operations Platform, the system must validate timestamps and chronological sequence, logging any conflicting concurrent actions for manual controller review without corrupting the authoritative journey state.

**Sources:** AD-071, AD-079

**Question:** How does the central operations platform handle out-of-order or conflicting offline safety override sync events from different stations?

**Verification:** Simulate simultaneous offline safety overrides from two connected terminals on the same line, restore connectivity, and verify that both events are recorded in the audit trail in chronological order while flagging potential operational conflicts for the controller.

**Architect status:** PENDING REVIEW

## Overall Observations

- The evaluated decisions successfully prune out-of-scope enterprise complexities such as dynamic multi-operator financial settlement, automated hotel booking, and full physical asset management.
- Testing and QA strategies must focus heavily on degraded states, cache expiration handling, and idempotent event synchronization since offline resilience is a core recurring theme across multiple applications.
- Integration points, particularly with the external catering provider, require robust simulation of API timeouts and delayed responses to ensure passenger applications fail gracefully.