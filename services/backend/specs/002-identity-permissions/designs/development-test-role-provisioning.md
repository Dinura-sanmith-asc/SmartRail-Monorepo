# Proposed Design: Development Test Role Provisioning

**Feature**: `002-identity-permissions`
**Document version**: `0.2.0`
**Status**: Draft — selected trust model for owner approval; implementation not authorized
**Prepared**: 2026-10-20
**Related approved baseline**: Feature 002 specification, plan, tasks, and `contracts/identity-permissions.openapi.yaml`
**Change purpose**: Define a tightly bounded way to grant and revoke the existing `Controller` and `StationStaff` roles for two owner-approved real Cognito test identities in a development environment. This document does not modify the approved Feature 002 baseline.

## 1. Decision summary

No repository mechanism is currently approved for provisioning arbitrary real Cognito test subjects.

- `tools/SmartRail.DevSeeder` is the safest existing code path for its fixed synthetic demonstration identities. It calls `GrantRoleAssignmentHandler` and uses a seeder-only authority, but its target subjects and actor are fixed in `SeedManifest`; its exact-local-database checks and explicit seed flags do not make it a general real-Cognito role provisioner.
- `dev/manual-auth/Set-LocalRole.ps1` is restricted to a local Compose database and records grant/revoke audit rows, but it writes PostgreSQL directly, bypasses the maintenance handlers and authority, and uses a fixed local test actor. It MUST NOT be used for the proposed real-Cognito assignments.
- Normal Infrastructure registration uses `DenyAllIdentityMaintenanceAuthority`; the API exposes no role-administration endpoint. Both properties remain unchanged.

**Proposed architecture**: a separate, manually invoked development provisioning workflow, isolated from API startup and the existing sample seeder. It calls the existing grant/revoke handlers and repositories; it does not write role tables directly and does not add an HTTP endpoint. The handler remains the application entry point and the repository remains the transactional persistence boundary.

**Selected authorization model**: a managed, non-admin Windows provisioning host; operator identity derived from the Windows process access token and membership in an OS-managed provisioner group; and a one-operation approval manifest signed offline by the SmartRail Project Owner. A protected one-use ledger and the database audit primary key prevent replay. This design uses no AWS signing or approval infrastructure.

**Deployment readiness**: this model is technically feasible but not currently established. No managed host, OS group policy, protected trust-anchor installation, signing certificate, signed tool distribution policy, target database fingerprint, or protected database credential was found. The owner must provision and verify these trust prerequisites before implementation or real role assignment. A developer-controlled workstation is not an acceptable host when the developer is a local administrator or can access Docker/database write credentials.

## 2. Security objective and scope

The tool may manage only two explicitly approved real Cognito subject values, under approved aliases `controller-test` and `station-staff-test`, and only the corresponding role grants:

| Approved target alias | Allowed internal domain role | `/api/me` display label |
|---|---|---|
| `controller-test` | `RoleCode.Controller` / stored code `Controller` | `Controller` |
| `station-staff-test` | `RoleCode.StationStaff` / stored code `StationStaff` | `Station Staff` |

The actual Cognito `sub` values are environment-specific identifiers. They MUST be obtained from the trusted Cognito environment, placed only in an owner-approved protected target registry, excluded from source control and routine logs, and never confused with email addresses, usernames, JWT group claims, or the display labels above. The source of that registry and its update authority are owner decisions. Test passwords and access tokens are never requested or stored by the provisioner.

No additional roles, targets, station scopes, production assignments, public APIs, user administration, Cognito group grants, or business workflows are in scope. This design does not provide station-scope authorization evidence; T027/T051 remain blocked until an owning feature supplies an approved protected operation and controlled scope fixtures.

## 3. Proposed invocation and trust model

### 3.1 Trust model options reviewed and selected

| Option | Assessment |
|---|---|
| Verified OS/process operator identity | Selected as the operator identity source, but only on a managed non-admin Windows host. `WindowsIdentity`/the OS access token supplies the authenticated process SID and group SIDs; the tool checks membership in a dedicated OS-managed provisioner group. It never accepts `--actor`, an environment actor, or a username string. A local account or group editable by the invoking developer is not a trust source. |
| Separately trusted maintenance principal | Not selected. A Cognito or application principal could work only with a separately approved maintenance identity, validator, client and policy. None exists; Controller/Station Staff roles and Cognito groups remain prohibited as maintenance authority. |
| Isolated administrative workflow | Selected execution boundary. Run the provisioner from a controlled host under the verified OS principal; the host alone holds the scoped development DB credential. The Project Owner signs each exact approval offline. No AWS infrastructure is required, but managed host, key custody, and execution-policy prerequisites must be established. |

The selected model separates three controls: the OS group authorizes the operator to invoke the tool; the owner's signature authorizes one exact role mutation; and the tool's closed target/role policy constrains its scope. None depends on the target user already having a SmartRail role. The signed manifest is an approval artifact, not a replacement for operator authentication.

### 3.2 Authorization sequence

1. The host administrator installs the approved provisioner binary and trust policy in administrator-writable, operator-read-only locations. OS policy (for example, AppLocker/WDAC publisher/path rules) permits only the owner-approved signed binary. The operator is a standard user, not a local administrator, and is a member of the owner-approved Windows provisioner group.
2. The tool obtains the process primary-token user SID and enabled group SIDs from Windows; it rejects anonymous, service/system, impersonated, or non-member identities. The approved group SID comes from administrator-protected machine policy, not source code edits, CLI input, User Secrets, or environment variables. No `--actor` option exists.
3. The Project Owner creates one approval manifest per operation and signs its exact UTF-8 bytes offline using an owner-controlled signing certificate/private key. A detached CMS/PKCS#7 signature is proposed so the signature can be verified with standard .NET cryptography. The private key is not exported to or installed on the execution host. The operator and signer are distinct; the owner must authorize any emergency exception separately.
4. The signed manifest contains a closed, versioned schema: request UUID; action; target alias; exact Cognito `sub`; exact internal role code; exact operator SID; provisioner host identity; literal `Development` environment; exact database fingerprint; approval/change reference; issued-at and expiry UTC instants. CMS carries the signer certificate/key identifier. Signature verification uses a pinned owner certificate thumbprint installed in administrator-protected LocalMachine policy. Verify the signature over the exact bytes before parsing; reject duplicate/unknown fields, malformed encoding, and unsupported versions.
5. The tool validates the OS operator and host identities, owner signature/key, current validity window, exact operation/role/target/database match, target alias/role pairing, approval reference, and unused request UUID before any handler call. Target subjects are supplied only inside the signed manifest; there are no raw-subject, role, or actor overrides on the command line.
6. A protected durable one-use ledger atomically records and flushes the request UUID before the handler is invoked. Duplicate UUIDs are rejected. A crash after consumption but before completion requires a new owner-signed approval; the previous request is never retried. For a successful mutation, use the manifest UUID as the `RoleAssignmentAudit.Id` and include the approval reference and UUID in `EvidenceReference`, adding a second replay barrier through the existing audit primary key. The ledger is scoped to the approved host/database and is not copied to another runner.
7. The verified Windows SID, namespaced as `windows-sid:<SID>`, is the authoritative audit `ActorSubject`. The audit evidence also records the signed approval reference/UUID and signer certificate thumbprint. This is an OS principal, not a Cognito subject; the owner must approve this interpretation for the existing opaque text fields.
8. Normal API composition continues to register `DenyAllIdentityMaintenanceAuthority`. Only the dedicated tool composition may register the scoped maintenance authority. AppHost/API startup never launches the tool, and no public endpoint is added.

**Signing and verification lifecycle**: the owner keeps the non-exportable signing private key on an offline hardware-backed certificate/token or equivalent controlled offline signing device. The public certificate thumbprint is installed by a machine administrator into protected LocalMachine policy and pinned by the tool. Use a separate code-signing identity for the provisioner binary; OS policy must enforce that only the approved publisher/build is executable. Key rotation requires owner approval, a bounded transition where both pins are accepted, removal of the old pin, and a documented revocation response if a key is lost or suspected compromised. Do not put private keys, signatures that disclose target subjects, or manifests in source control. No key is generated by this design.

**One-use ledger procedure**: keep the ledger on protected local storage on the single approved provisioner host, with operator read access only and provisioner/admin write access. Acquire an exclusive process lock, check the request UUID, append its consumed state, flush it durably, then run the handler. If lock, read, append, flush, or integrity validation fails, stop before opening a write operation. On success, retain the request UUID in the existing audit event primary key as well. If the process crashes after consumption, require a new owner-signed request; do not erase or reset the ledger. A host restore or ledger loss invalidates outstanding manifests until the owner reviews audit history and reinitializes the trust state. The ledger and database audit are deliberately not a distributed transaction; consume-first behavior favors no replay over retry convenience.

**Manifest validation profile**: manifest signing covers the exact bytes received; the verifier does not normalize content before signature verification. After verification, strict deserialization rejects duplicate keys, unknown keys, missing fields, invalid enums, invalid UUIDs, invalid UTC instants, oversize payloads, and extra JSON values. One request describes one action and one assignment lifecycle event. The OS process SID, provisioner host identity, and connected database identity must equal the signed values. The approval reference is meaningful because the Project Owner signs the exact request; a CLI-supplied free-form reference cannot substitute for the signed field.

**Feasibility and boundary**: Windows can reliably report the process token SID and group membership to a process. That proves which OS account launched the process; it does not prove the process binary or local policy has not been changed. Reliability therefore depends on managed OS group membership, administrator-protected trust policy and ledger, protected host identity, enforced signed-binary execution, and the operator being a non-admin. These prerequisites are absent today. If they cannot be established, use an owner-approved isolated admin runner/host with the same independent OS identity and signing checks, plus DB write credentials unavailable to developers. Do not fall back to a developer-controlled workstation.

### 3.3 Internal contract gap

`IIdentityMaintenanceAuthority.AuthorizeAsync` currently receives only actor subject and action. It cannot itself bind an authorization decision to target subject, role, assignment ID, or evidence reference. The grant/revoke handlers do not currently pass those details to the authority. A CLI-only allowlist would therefore be enforced outside the handler and would not make the handler's authorization decision target-specific.

Before implementation, update the internal maintenance authorization request so the trusted authority can validate the exact operation context. For a grant, include target subject, role code, request UUID, and approval reference. For a revoke, resolve the active assignment and bind its exact target and role before authorization; the repository must enforce the expected assignment/subject/role in the same transactional revoke operation to avoid a read-then-write substitution race. The authority must verify the signed request and one-use ledger before returning `Allowed`. This is an internal Application contract change only; it does not add an HTTP operation or alter RBAC grants. The test-only seeder authority must remain separate and fixed to its seed manifest.

## 4. Environment, target, role, and database guards

The implementation MUST fail before any database write unless every condition below is true:

- Both runtime environment indicators are exactly `Development`; absent, `Testing`, `Production`, or conflicting values are rejected. The tool is not launched from the API or AppHost process.
- The OS process identity is authenticated, is a standard non-admin user, and is a member of the approved Windows group from the OS token. The protected group policy, pinned signing certificate, target policy, replay ledger, and provisioner installation cannot be modified by the operator. Only the approved signed provisioner binary is allowed to run under enforced host policy.
- The resolved PostgreSQL target matches the owner-approved exact development target fingerprint embedded in the signed request and administrator-protected host policy. Validate host, port, database, username, SSL mode, options/search path, and server identity; reject aliases such as `localhost`, multiple hosts, test port 5433, container/remote addresses, RDS/production, or unknown targets unless separately approved. Re-read the connected server/database identity immediately before the operation. User Secrets or environment variables may supply the tool's scoped credentials but cannot override the approved target fingerprint.
- The operator does not know the provisioner's database credential and cannot connect to the target database with write privileges. The API/service account has no role-assignment or audit-table write grant in this provisioning environment; only the protected provisioner process receives the scoped grant/revoke credential. If the developer can access the Docker socket, database superuser credential, or equivalent write path, this guard is procedural only and the environment is not eligible for controlled real-identity provisioning.
- The owner-signed request binds only `controller-test` to the exact approved Cognito subject plus `Controller`, or `station-staff-test` to the exact approved Cognito subject plus `StationStaff`. The signer confirms the subject against the controlled Cognito environment before signing. Do not print full subject values. Unsupported aliases, mismatched alias/role pairs, duplicate entries, and user-supplied raw subjects are rejected.
- Role input is a closed allowlist of internal stored codes `Controller` and `StationStaff`; it is parsed to `RoleCode.Controller` or `RoleCode.StationStaff`. `Station Staff` is an API display label and is not accepted as a stored role value. Passenger, Driver, unknown values, and arbitrary enum casts are rejected.
- The detached CMS signature verifies against the pinned owner certificate; the manifest's request UUID, action, alias/subject, internal role, environment/database fingerprint, approval reference, issued-at, and expiry are valid and exact. A free-form CLI reference alone is not evidence of authorization. Reject an expired or not-yet-valid request, signature/key mismatch, replayed request UUID, or signer/operator identity conflict.
- The operation supplies non-empty required audit evidence. The provisioner emits only redacted outcome/trace identifiers; it never logs Cognito credentials, tokens, passwords, full subject values, authorization request contents, or connection strings.

The current seed safety guard accepts only `127.0.0.1:5432/smartrail_dev` and rejects `localhost`; the proposed real-Cognito provisioning environment may use another controlled development database. Its exact target must be selected and independently approved rather than silently broadening the existing seeder's guard. The intended host must be isolated from ordinary developer DB credentials; the local Compose database, Docker socket, and `smartrail_dev` credential are not adequate controls when available to the operator.

## 5. Grant, repeat, revoke, and recovery behavior

### Grant

- Authorize the exact target/role/change request before the handler writes.
- Use `GrantRoleAssignmentHandler`; preserve domain validation and the repository's transaction that writes the assignment and `RoleAssignmentAudit` event together.
- A repeat request for an already active subject/role is an idempotent no-op and MUST NOT create a second active assignment or claim a second grant audit event. The tool reports “already active” without disclosing the subject. A new signed request is still required for each attempted operation.
- The one-use ledger consumes the approval before both successful writes and no-op results, so a request signed while a role is already active cannot later be replayed after a separate revocation.
- A grant whose audit write or transaction commit fails must leave no assignment. The failure response is safe and generic; diagnostic detail remains in protected logs without secrets or subject values.

### Revoke

- Require a distinct signed revoke approval bound to the exact target alias, internal role, active assignment ID, environment/database and change reference. Do not revoke all roles for a subject implicitly.
- Consume the request UUID before the handler call. The exact active assignment must still match the signed subject and role at mutation time; concurrent change or mismatch fails closed and leaves the assignment/audit unchanged.
- Use `RevokeRoleAssignmentHandler`; preserve the transaction that records revocation and audit. Revoked assignments are never reactivated; a later grant creates a new assignment lifecycle and grant audit event.
- A repeated or stale revoke is a safe no-op/denied result and creates no duplicate revoke audit. Report only the alias and safe outcome.
- The mapping is authoritative on the next `/api/me` read and protected authorization check. The provisioner does not revoke Cognito credentials, alter Cognito groups, or claim immediate invalidation of an already-rendered client workspace. Backend protected operations must continue to check current roles.

### Rollback

Do not delete a grant row or its audit evidence to roll back. Submit an explicitly approved revoke request for that exact assignment. If a grant transaction fails, rely on the repository transaction rollback and verify that neither assignment nor grant audit exists. If a revoke transaction fails, verify the active assignment and audit history are unchanged, then investigate and retry only with an approved request. Never repair by direct SQL.

## 6. Audit and privacy

The audit record must include the verified maintenance actor identifier, exact action, role assignment ID, UTC event time, and required signed approval/change reference. The role row retains target subject, granted/revoked actor, and timestamps. Grant/revoke state and audit must commit atomically through the repository. The tool must return safe result codes and never expose provider exceptions.

Feature 002's production audit retention/access policy remains open. Before production use, database grants/controls must prevent unauthorized modification/deletion of historical audit records and define who can query them and how long they are retained. This development design does not authorize production provisioning or amend production audit policy.

## 7. Security limitations and fallback workflow

The controls above are enforceable only when the trust host and database boundary are administered independently of the invoking developer. On a developer-controlled workstation, a local administrator can alter the executable, replace the pinned public key/policy, alter the replay ledger or system clock, add their account to the OS group, and connect to a Docker-hosted database directly. A valid owner signature does not prevent a local administrator from patching a verifier that is not protected by OS code-integrity policy. A local Compose `smartrail_dev` database whose Docker socket or write credential is available to the developer cannot technically guarantee “no direct-SQL grants.”

Therefore the process MUST NOT run on a developer-controlled/local-admin workstation or against a database for which developers retain direct role-table write access. If SmartRail cannot provide the managed Windows host, administrator-protected OS group/trust policy, enforced signed-tool execution, protected one-use ledger, and provisioner-only database credential, the alternative is an owner-approved isolated administrative runner/host with these same controls. The owner or approved operator may run the exact signed request there; the development client/API can consume the resulting PostgreSQL mapping through its normal read/authorization behavior. No AWS service is required for offline signing, but the host, credential, signing key and policy still need an independently controlled custody path.

The threat model does not protect against compromise of the owner signing key, OS administrator, host administrator, database administrator, or an authorized operator colluding with an approver. Those roles require separate governance, monitoring and incident response. Expiry checks also depend on a managed, synchronized host clock; no expiry duration is selected here.

## 8. Feature 002 baseline and approval approach

Keep the approved Feature 002 `spec.md`, `contracts/identity-permissions.openapi.yaml`, RBAC grants, role mapping behavior, and database schema unchanged during this design stage.

The smallest suitable artifact is this separately versioned companion design because it specifies a non-public operational process and introduces no new user-facing behavior, endpoint, role, permission, or data field. It makes explicit the maintenance-governance gate already identified by Feature 002. If the owner decides this process is part of Feature 002's normative implementation scope rather than operational guidance, prepare a separately reviewed versioned spec/plan/tasks amendment before implementation; do not edit the Approved baseline in place.

This document itself requires explicit owner approval before implementation. Approval covers the proposed development-only workflow only; it does not approve production operation or establish that the external trust prerequisites have been provisioned.

## 9. Required implementation files and tests after approval

No files in this section are changed by this draft.

### Expected code/document changes

- `specs/002-identity-permissions/designs/development-test-role-provisioning.md`: owner-approved version/status and final decisions.
- `specs/002-identity-permissions/plan.md` and `tasks.md`: link the approved companion design and add dependency-ordered implementation/verification tasks; preserve approved RBAC and HTTP contracts.
- `src/SmartRail.Application/Identity/Authorization/AuthorizationContracts.cs`: add a target-specific maintenance authorization request containing verified actor SID, action, target, role, assignment/request ID, and signed evidence reference.
- `src/SmartRail.Application/Identity/Maintenance/IdentityMaintenanceContracts.cs`, `GrantRoleAssignmentHandler.cs`, and `RevokeRoleAssignmentHandler.cs`: pass and enforce the exact approved request context; resolve revoke targets safely.
- `src/SmartRail.Application/Identity/Persistence/IdentityPersistenceContracts.cs` and `src/SmartRail.Infrastructure/Identity/Persistence/RoleAssignmentRepository.cs`: ensure revocation checks expected assignment, target, and role atomically.
- `tools/SmartRail.IdentityProvisioner/SmartRail.IdentityProvisioner.csproj`, `Program.cs`, `WindowsOperatorIdentity.cs`, `ProvisioningApprovalManifest.cs`, `SignedApprovalVerifier.cs`, `ProvisioningReplayLedger.cs`, `DevelopmentDatabaseGuard.cs`, and `IdentityMaintenanceAuthority.cs`: dedicated tool composition and bounded components for Windows process-token identity, protected policy/signing-certificate validation, detached CMS verification, one-use ledger, exact database fingerprint guard, redacted output, and explicit grant/revoke commands. Do not widen `tools/SmartRail.DevSeeder` into a general role editor. Add the project reference in `SmartRail.slnx` only after documenting the architecture need.
- Administrator-installed trust configuration outside the repository: approved Windows group SID, pinned owner signing-certificate thumbprint, target subject approval process, exact development database fingerprint, protected ledger path, host time policy, code-integrity policy for the signed provisioner, and a database credential unavailable to developers. Actual secret/subject values are provisioned externally and are not committed.
- Owner-controlled offline signing procedure and recovery/rotation record for the private key; no private key is added to the repository or provisioner host.
- `dev/manual-auth/Set-LocalRole.ps1` and `dev/manual-auth/README.md`: retire/replace the direct-SQL path for real Cognito provisioning; retain the synthetic local issuer workflow only if separately approved and clearly labeled.
- `tests/SmartRail.UnitTests/Identity/DevelopmentProvisioningAuthorizationTests.cs`, `tests/SmartRail.IntegrationTests/Identity/DevelopmentRoleProvisioningTests.cs`, `tests/SmartRail.IntegrationTests/Identity/DevelopmentProvisioningDatabaseGuardTests.cs`, and `tests/SmartRail.IntegrationTests/Development/AppHostAuthenticationModeTests.cs`: verify the authorization, process/config guards, exact target and roles, replay protection, audit transaction, and AppHost/CORS regressions. Existing denied/unavailable-authority and audit-history tests must continue to pass.
- `tests/SmartRail.IntegrationTests/Identity/DevelopmentCorsTests.cs`: verify approved Development origins, denied unapproved origins, and no Production expansion.

No OpenAPI, migration, schema, production API DI, production CORS, or RBAC changes are expected. Revisit only if approved requirements reveal a justified need.

### Required regression and acceptance matrix

| Scenario | Expected evidence |
|---|---|
| Authorized Controller grant | Signed approval and authorized operator accepted; handler creates exactly one active `Controller` mapping plus matching grant audit in one transaction. |
| Authorized Station Staff grant | Same as above for the Station Staff alias and internal `StationStaff` code; no station scope is implied. |
| Real `/api/me` roles | With the real Cognito access token, Controller subject returns `200` with `roles: ["Controller"]`; Station Staff subject returns `200` with `roles: ["Station Staff"]`. `subject` is correct and no scope/profile/token is returned. |
| React workspace routing | Against approved Cognito and backend configuration, Controller opens the Controller shell; Station Staff opens only the generic Station Staff shell. Record browser/environment evidence separately from backend role evidence. |
| No-role Cognito subject | Verified subject without an active internal mapping returns `200` with `roles: []`; no role workspace or protected operation is available. |
| Role revocation | Revoke exact assignment with signed approval; audit records revoke. Subsequent `/api/me` excludes the role and subsequent backend Controller-protected operation denies the identity. Client reauthentication/identity refresh is exercised; no global Cognito logout/revocation claim. |
| Unauthorized operator | Missing/invalid OS authorization or signed approval is rejected before handler persistence; no assignment or audit row is written. |
| Unauthorized subject/role | Unknown subject alias, swapped alias/role, raw subject, Passenger/Driver/unknown role, altered/expired/replayed approval, or different target/database binding is rejected with no writes. |
| OS identity verification | A real host acceptance check confirms the process-token SID and approved group membership; self-reported CLI actor, environment actor, impersonated token, disabled group membership, local-admin operator, or wrong host identity is rejected. Unit tests inject identity-provider outcomes deterministically. |
| Owner signature / trust anchor | Valid owner CMS signature is accepted; altered bytes, invalid/untrusted signer, wrong pinned thumbprint, missing signature, malformed/duplicate JSON, expired/not-yet-valid request, signer=operator, and unsupported schema are rejected before DB writes. Key rotation/revocation tests prove old pins stop authorizing new requests after the approved transition. |
| Replay protection | Concurrent duplicate request UUIDs allow at most one invocation; a consumed UUID is rejected after success, no-op, handler failure, or process restart. Ledger lock/write/flush/corruption failures stop before the handler. Successful audit uses the same UUID as its primary key. |
| Wrong environment/database | Production/Testing/missing environment, wrong host/port/database/user/server fingerprint, or environment override mismatch is rejected before mutation. |
| Direct database access | An operator without provisioner credentials cannot write identity role/audit tables directly; API DB principal is denied those table writes in the approved provisioning environment. |
| Duplicate/repeated grant | Active assignment stays unique; no duplicate active assignment or misleading additional audit event. Revoked mapping requires a separately approved new grant lifecycle. |
| Audit transaction | Inject failure between state and audit persistence; verify transaction leaves both unchanged. Query grant/revoke/regrant history and confirm actor, action, timestamps, assignment linkage, and change evidence. |
| Local/AWS AppHost mode | Default/unset and explicit `local` start the existing issuer and API with matching local issuer; `aws` starts API without local issuer and loads the explicitly configured HTTPS Cognito issuer/client/audience; unknown mode fails; non-Development AppHost fails. |
| Authentication configuration | Missing/invalid issuer or allowed client fails closed; wrong issuer/client/audience/token-use/signature/expiry remains rejected. Local mode keeps current loopback fixture behavior. |
| Development CORS | `http://localhost:5173` and `http://localhost:5174` succeed in Development; an unapproved origin is rejected; Production does not gain either development origin. |

Tests must use isolated PostgreSQL test databases for persistence. Real Cognito acceptance is a separate controlled environment run and must not be simulated by the local issuer. T027/T051 remain blocked for the station-scope cases described above.

## 10. Project-owner actions and decisions before implementation

The authorization model is selected in this draft. Before implementation, the project owner must:

1. Approve the managed-host + OS group + offline owner-signed one-use manifest model and accept its stated trusted-admin/key-compromise limitations.
2. Name the security/host administrator and provision a Windows group for approved operators. Supply and verify the actual operator SID membership outside the developer workspace; require operators to be non-admins.
3. Create the offline signing certificate using an approved algorithm/device, retain its private key outside the execution host, define recovery/rotation/revocation steps, and arrange administrator-only installation/pinning of the public certificate thumbprint. No key is generated by this task.
4. Approve manifest schema/version, detached CMS profile, request UUID handling, signature validity window/maximum lifetime, host time source, and distinct signer/operator requirement.
5. Approve the two controlled Cognito test accounts and verify their exact `sub` values through the trusted Cognito environment. Put the exact operator SID, host identity, test-subject alias/subject, role, operation, database fingerprint, approval reference, request UUID and lifetime in each signed manifest. Identifiers remain outside source control and ordinary logs.
6. Approve the exact development DB fingerprint and establish a scoped provisioner-only credential. Remove developers' direct write path to role/audit tables in that target; if that cannot be done, use an isolated admin runner/database and do not call the workflow controlled.
7. Approve the audit actor convention `windows-sid:<SID>`, evidence-reference encoding, and development audit access/retention. Production audit policy remains a separate gate.
8. Approve an externally administered replay-ledger path/ACL, durable flush behavior, backup/restore policy, and no-retry-after-consumption behavior.
9. Approve separate manifest-signing and code-signing identities, signed-binary distribution and enforcement (for example, managed AppLocker/WDAC policy), and verify operators cannot replace the binary, trust policy, signer pin or ledger.
10. Decide whether the internal maintenance-request expansion is a companion implementation detail or requires a separately versioned Feature 002 plan/tasks amendment. Do not edit the Approved spec/plan/tasks without that decision.
11. Provide the controlled Cognito app client, issuer/audience values, browser origin, and approved manual client acceptance environment. Keep T027/T051 blocked until their owning-operation contract and scope fixtures exist.

## 11. Approval-ready acceptance criteria

This draft is ready for formal owner review as a **design**, not ready for implementation until the external prerequisites above are met. The design may be approved when the owner explicitly accepts all of the following:

- Only the two named Cognito test aliases are eligible, bound by the signed request to their exact verified `sub` values and corresponding internal roles `Controller` and `StationStaff`.
- Operator identity comes only from the Windows process token and membership in the protected approved OS group; no `--actor`, editable flag, staff role, group claim, or subject possession authorizes an operation.
- Each exact grant/revoke is owner-signed offline, tied to one change reference, request UUID, target, role, operation, Development DB fingerprint, issue time and expiry; signature key pin and code-integrity policy are administrator-controlled.
- A request UUID is durably consumed before handler execution, cannot be replayed after a no-op or failure, and is reused as the audit primary key on successful grant/revoke. Failures after consumption require a new signed approval. Concurrent calls and process restart are tested.
- The provisioner alone has the database write credential; developers and the API cannot directly write role or audit tables in the approved target environment.
- The tool calls the existing grant/revoke handlers; role/audit writes remain transactional; normal API composition remains deny-all; there are no public endpoints, direct-SQL grants, schema changes or API contract changes.
- Tests prove successful Controller/Station Staff grants and revocations, `/api/me` labels, client route presentation, no-role and revoked-role denial, unauthorized operator/subject/role, wrong environment/database, invalid/expired/replayed approval, key mismatch, duplicate request concurrency, transaction failure rollback, and all AppHost/CORS/authentication regressions listed in the matrix.
- The owner accepts the explicit limitation that local administrators, database administrators, compromised signers and colluding authorized parties remain trusted; an unmanaged developer workstation is prohibited.

## 12. Recommendation

Recommend formal approval of this companion design as the selected security model, conditional on the owner accepting the required managed-host and database-isolation prerequisites. If those prerequisites cannot be provided, reject local-workstation provisioning and authorize an isolated owner-controlled administrative runner instead. Implementation requires separate owner approval after the external trust anchors and database boundary are verified. Until then, keep `DenyAllIdentityMaintenanceAuthority` in ordinary API composition, do not use the direct-SQL helper for real Cognito identities, and do not mark integration tasks complete.

## Revision history

| Version | Status | Change |
|---|---|---|
| 0.1.0 | Draft | Initial companion design; operator trust source and approval evidence open. |
| 0.2.0 | Draft | Select managed Windows process identity plus offline owner-signed one-use approval; define trust-anchor, anti-replay, database-isolation requirements and workstation limitations. |
