# Real Cognito users: local development role bootstrap

Real Cognito authentication and SmartRail authorization remain separate. This tool grants PostgreSQL roles to exactly two operator-supplied Cognito subjects using `GrantRoleAssignmentHandler`; cleanup uses `RevokeRoleAssignmentHandler`. Both preserve the existing transactional assignment/audit implementation. Normal API maintenance remains `DenyAllIdentityMaintenanceAuthority`. This does not implement or approve managed-host production provisioning.

## Execution approval file (outside Git)

Obtain each real user's **exact `subject` from a successful, authenticated `GET /api/me`** in their own existing Cognito session. Do not use usernames, email addresses, tokens, Cognito groups or an unverified token decoder as the identity source. This isolated user-pool tool requires two distinct canonical UUID-format `sub` strings and preserves their exact text. The underlying domain continues treating identity keys as opaque text.

Run the following PowerShell from the repository root as the approved local developer. The subject values are prompted, so they do not appear as literals in shell history or tracked files. Review that the two accounts and intended roles are the ones you approve for this local test. `ApprovedOperatorSid` authorizes the current Windows account for this file's local execution; it is not independent production governance approval. Protect the file under your user profile and keep it for cleanup. Do not commit it.

```powershell
$roleApprovalDirectory = Join-Path $env:LOCALAPPDATA 'SmartRail'
New-Item -ItemType Directory -Force -Path $roleApprovalDirectory | Out-Null
$roleApprovalFile = Join-Path $roleApprovalDirectory 'cognito-test-roles.json'
if (Test-Path $roleApprovalFile) { throw 'Approval file already exists. Review and reuse it; do not overwrite it accidentally.' }
$roleControllerSub = Read-Host 'Controller: exact verified /api/me subject'
$roleStaffSub = Read-Host 'Station Staff: exact verified /api/me subject'
$roleApprovalReference = Read-Host 'Local development-test approval reference (for example DEV-COGNITO-20261020)'
@{
    ApprovedOperatorSid = [Security.Principal.WindowsIdentity]::GetCurrent().User.Value
    ControllerSubject = $roleControllerSub
    StationStaffSubject = $roleStaffSub
    ApprovalReference = $roleApprovalReference
} | ConvertTo-Json | Set-Content -LiteralPath $roleApprovalFile -Encoding utf8
Remove-Variable roleControllerSub, roleStaffSub
```

There are exactly two target slots: Controller subject → internal `Controller`; Station Staff subject → internal `StationStaff`. Unknown JSON properties, empty/malformed/duplicate subjects, other subject/role pairs, unsupported actions and missing approval references are rejected. No freely supplied `--actor` or arbitrary role option exists. The executing CLI derives its audit actor as `windows:<actual process-user SID>` and compares that actual OS SID to the approved operator SID. Another Windows account is denied unless a separate explicit local approval file authorizes it.

## Exact grant and cleanup commands

The existing API Development connection must point to **127.0.0.1:5432/smartrail_dev**, user `smartrail_dev`, without Options/SearchPath overrides. User Secrets are loaded by the existing configuration factory; environment overrides take precedence. Passwords remain in existing secret configuration, never arguments. The tool refuses Production, test port 5433, localhost aliases, remote/RDS, unknown databases/users, and pending migrations. It never migrates, alters Cognito configuration, creates a user, or modifies JWTs. It never invokes the general railway/incident data seed.

```powershell
$roleApprovalFile = Join-Path $env:LOCALAPPDATA 'SmartRail\cognito-test-roles.json'

# Grant the approved two mappings (repeat execution is a no-op when already active).
.\dev\manual-auth\Invoke-CognitoTestRoles.ps1 -Operation Grant -ApprovalFile $roleApprovalFile -ConfirmLocalCognitoTestRoles

# Cleanup later: revoke only this operator/file approval's own bootstrap assignments.
.\dev\manual-auth\Invoke-CognitoTestRoles.ps1 -Operation Revoke -ApprovalFile $roleApprovalFile -ConfirmLocalCognitoTestRoles
```

The wrapper refuses existing non-Development environment settings, temporarily sets Development when absent, and restores the shell on exit. Each assignment/audit transaction is atomic; the two users are not one global transaction. If the second command step fails, fix availability and rerun; no duplicate active mapping or grant audit is created for the already-active first user. An explicit grant after cleanup creates a new grant lifecycle rather than reactivating the revoked row.

Audit records include the OS actor and `local-cognito-test-bootstrap:<ApprovalReference>`. Existing matching active grants are preserved unchanged. Cleanup refuses grants created outside the same OS actor and approval reference; it never broadly revokes all roles or touches unrelated identities. No station-scope grant is made by this role-only tool. Station Staff still needs separately authorized station assignments for scoped incident actions; a workspace opening alone does not prove station authorization.

## Verify real sessions in the existing Operations Web

Keep the currently working **real Cognito** mode, issuer, client ID, audience, CORS and backend URL unchanged. Do not run `Configure-ManualAuth.ps1` for this verification: that separate script selects the local test issuer.

1. Before granting, sign in as Controller through the existing React Operations Web login. In browser Developer Tools → Network, inspect its successful backend `GET /api/me` response and copy only `subject` into the Controller approval slot. Sign out.
2. Sign in as Station Staff and obtain its verified `/api/me.subject` the same way. Use the Station Staff slot. Do not copy Authorization headers, passwords or bearer tokens into the approval file or chat.
3. Run the grant command above, then reload the Controller's authenticated application session. Inspect the new `/api/me` request: expect `200` with `roles: ["Controller"]` if it has no unrelated pre-existing active roles. Confirm the Controller workspace opens.
4. Sign out and log in as Station Staff. Inspect `/api/me`: expect `200` with `roles: ["Station Staff"]`; confirm the Station Staff workspace opens. Tokens do not need changed group claims: every call reads current PostgreSQL mappings. If unrelated active roles already exist, the approved union behavior returns those too; this tool does not remove them to force an exact array.
5. Missing/invalid/expired credentials must still return 401 on `/api/me`. After cleanup, issue a fresh `/api/me` request using the same still-valid session: the revoked role must disappear on the next lookup (other unrelated roles remain).

If the React app caches identity state, trigger its reload/re-fetch or sign out/in. If backend labels are correct but a workspace does not open, inspect client routing/role handling separately; no React source is present in this backend repository. Never weaken JWT validation or add a UI-only permission bypass to make the screen open.

The tool prints role labels and completion status only, without subjects, credentials, tokens, connection strings or raw provider exceptions. Real subject IDs, interactive logins and actual workspace verification must be supplied/performed at execution; automated synthetic fixtures are not real Cognito-session evidence.

## Verification evidence

Implementation verification on 2026-10-20: full solution build passed with isolated output (0 errors; existing AppHost MessagePack NU1902 advisory remains visible). The normal-output full solution test run passed **187 unit + 153 integration tests**, 0 failed and 0 skipped. This includes 12 new Cognito-bootstrap cases plus the existing environment/database guards, default-deny composition, JWT, local issuer, API contract and authorization regressions. Real PostgreSQL tests used disposable `smartrail_it_*` databases on loopback port 5433, not `smartrail_dev`.

PowerShell syntax validation and `git diff --check` passed. The read-only configuration check confirmed the approved `127.0.0.1:5432/smartrail_dev` target; the existing Development secret still identifies an HTTPS Cognito issuer, and no authentication or secret setting was changed.

After the operator supplied the two real subjects, the approved local bootstrap granted both mappings on 2026-10-20. A read-only database check confirmed exactly one active assignment and one matching grant audit for each intended role, with the actual Windows actor recorded. Repeating the grant command returned already-active/no-change for both. The execution approval file is stored outside the repository under `%LOCALAPPDATA%\SmartRail\cognito-test-roles.json`; real subject IDs are not included in tracked artifacts. The initial run exposed an unnecessarily narrow SID syntax restriction; it was corrected to accept valid Windows SID authorities such as `S-1-12` while retaining exact actual-operator matching. All 13 focused bootstrap tests passed after that correction (the earlier complete solution run passed 187 unit and 153 integration tests).

Real authenticated Cognito `/api/me` responses and React workspace opening still require the existing interactive user sessions and have **not** been claimed as verified. Refresh those sessions using the browser steps above; database provisioning evidence is separate from real HTTP/client evidence.
