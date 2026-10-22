# SmartRail Security Review

**Findings:** 6

## SECURITY-001 — Unauthenticated Catering API Webhook Integration Risk

**Category:** SECURITY_RISK
**Severity:** HIGH
**Area:** External Catering Integration & Backend

**Finding:** The architecture introduces integration points with an external catering provider via API and asynchronous status updates, but lacks explicit requirements for cryptographic signature verification (HMAC), mutual TLS (mTLS), or payload validation for incoming provider webhooks and API responses, risking injection or spoofing of order fulfillment states.

**Candidate requirement:** All incoming webhooks and API payloads from the external catering provider must be authenticated using cryptographically secure mechanisms such as HMAC-SHA256 signatures and validated against a strict JSON schema before any order status changes are persisted.

**Sources:** AD-001, AD-003, AD-011

**Question:** How does SmartRail cryptographically verify that an incoming order status update or webhook originated from the legitimate external catering provider and not an attacker?

**Verification:** Perform a negative security test by sending forged webhook payloads lacking valid HMAC signatures to the SmartRail catering integration endpoint and verify that they are rejected with a 401/403 status.

**Architect status:** PENDING REVIEW

## SECURITY-002 — Driver Mobile Application Credential and Session Protection

**Category:** MISSING_REQUIREMENT
**Severity:** HIGH
**Area:** Driver / Train Operator Application

**Finding:** Drivers operate in mobile environments with potential device loss or theft while handling operational assignments, but the requirements lack specifications for secure token storage, biometric locking, short session timeouts, or remote wipe capabilities for the DMI/driver app.

**Candidate requirement:** The Driver application must store authentication tokens securely using platform-provided hardware-backed keystores (Keychain/EncryptedSharedPreferences), enforce biometric authentication or secure PIN unlock on startup, and automatically invalidate sessions upon prolonged inactivity.

**Sources:** AD-028, AD-030, AD-031

**Question:** Are driver authentication tokens stored in plaintext or accessible application storage where they can be extracted if the physical mobile device is compromised?

**Verification:** Inspect the mobile application's local storage and file system during a security review to confirm that sensitive session tokens and cached operational payloads are encrypted at rest.

**Architect status:** PENDING REVIEW

## SECURITY-003 — Privilege Escalation via Improper Backend Authorization on Station and Control APIs

**Category:** SECURITY_RISK
**Severity:** CRITICAL
**Area:** Railway Operations Platform & Station Terminals

**Finding:** While role-based authorization is acknowledged for operations, there is no explicit requirement for server-side tenant, station, or role checks on backend endpoints. Station terminal applications or compromised station staff accounts could potentially execute controller-only actions like service cancellations or platform overrides.

**Candidate requirement:** All backend API endpoints supporting operations, station display overrides, and incident creation must enforce strict server-side Role-Based Access Control (RBAC) and context validation verifying that the authenticated user's assigned station or role holds explicit authority for the target resource.

**Sources:** AD-059, AD-067, AD-076

**Question:** How does the backend ensure that a user authenticated as a station staff member cannot invoke administrative or central controller API routes?

**Verification:** Execute a broken object-level authorization (BOLA) and broken function-level authorization (FLA) test suite against operations endpoints using standard staff tokens attempting controller actions.

**Architect status:** PENDING REVIEW

## SECURITY-004 — Lack of Input Sanitization and Output Encoding on Station Public Displays

**Category:** SECURITY_RISK
**Severity:** MEDIUM
**Area:** Railway Station Digital Displays

**Finding:** Station displays and terminals render dynamic operational messages, incident text, and local station override notices. Without explicit input sanitization and output encoding requirements, stored Cross-Site Scripting (XSS) or display injection vulnerabilities could deface public screens.

**Candidate requirement:** All text inputs, incident descriptions, and operational notices rendered on station passenger-information displays and interactive terminals must be strictly sanitized on input and contextually encoded on output to prevent script execution or display corruption.

**Sources:** AD-022, AD-075, AD-076

**Question:** Are strings injected via local station status updates safely escaped before rendering on station public information displays?

**Verification:** Inject HTML/JavaScript payloads into station incident reports and verify through automated UI testing that the payload renders as harmless escaped text rather than executing script code.

**Architect status:** PENDING REVIEW

## SECURITY-005 — Tampering Risks in Offline Cached Operational Data for Drivers and Stations

**Category:** SECURITY_RISK
**Severity:** HIGH
**Area:** Connectivity and Resilience / Driver App / Station Terminals

**Finding:** Applications are required to cache operational data and support offline fallbacks during connectivity loss, but there are no controls specified to verify the cryptographic integrity or prevent tampering of locally cached schedules, safety notices, and operational parameters.

**Candidate requirement:** All operational data cached locally on driver devices or station terminals for offline fallback must be digitally signed by backend authorities, validated upon retrieval, and rejected if integrity checks fail or if expiration limits are exceeded.

**Sources:** AD-030, AD-070, AD-073

**Question:** What prevents a malicious user with physical or logical access to a station terminal or driver device from modifying cached JSON schedules or operational states on disk?

**Verification:** Manually alter cached operational files on a test device while offline and verify that the application detects the tampering, rejects the payload, and enters a secure degraded state.

**Architect status:** PENDING REVIEW

## SECURITY-006 — Exposure of Personal Identifiable Information (PII) in Audit Logs and Error Responses

**Category:** SECURITY_RISK
**Severity:** MEDIUM
**Area:** Audit Logs and Data Privacy

**Finding:** The system processes passenger accounts, food orders, and support handoffs, but lacks requirements regarding data minimization and the exclusion of sensitive PII (such as passenger names, contact info, or payment references) from application logs and verbose API error responses.

**Candidate requirement:** Application logging frameworks and error-handling routines must enforce strict data masking and sanitization to ensure that passenger PII, authentication credentials, and raw integration tokens are never written to log files or returned in API exception payloads.

**Sources:** AD-020, AD-050

**Question:** Are passenger names, email addresses, or catering order payment references visible in backend log streams during normal operations or exception handling?

**Verification:** Trigger simulated exception flows and invalid food-ordering requests, then inspect the resulting audit logs and error responses to verify that no PII or sensitive tokens are exposed.

**Architect status:** PENDING REVIEW

## Overall Observations

- The architect's decisions correctly establish boundaries by treating SmartRail as an operational and passenger information platform rather than a safety-critical train signaling or braking system.
- External integrations such as the catering service have been appropriately scoped to rely on the external provider for merchant-of-record payment and authoritative menu ownership.
- Offline resilience requirements for drivers and stations are well-considered pragmatically, though they introduce specific client-side caching integrity and synchronization security considerations that must be enforced during implementation.
- Role separation between general passengers, station staff, train drivers, and central controllers provides a solid foundation for implementing robust backend authorization controls.