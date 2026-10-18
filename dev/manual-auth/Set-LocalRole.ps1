[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet('Grant', 'Revoke')]
    [string] $Operation,

    [Parameter(Mandatory)]
    [ValidateSet('Passenger', 'Driver', 'StationStaff', 'Controller')]
    [string] $Role
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$subjects = @{
    Passenger = 'smartrail-manual-passenger'
    Driver = 'smartrail-manual-driver'
    StationStaff = 'smartrail-manual-station-staff'
    Controller = 'smartrail-manual-controller'
}
$subject = $subjects[$Role]
$actor = 'local-manual-test-maintainer'
$evidence = 'local manual authentication testing'
$sqlSubject = $subject.Replace("'", "''")
$sqlRole = $Role.Replace("'", "''")
$sqlActor = $actor.Replace("'", "''")
$sqlEvidence = $evidence.Replace("'", "''")
Push-Location $repoRoot
try {
    $context = (docker context show).Trim()
    $endpoint = (docker context inspect $context --format '{{(index .Endpoints "docker").Host}}').Trim()
    if ($endpoint -notmatch '^(npipe://|unix://|windows://)') {
        throw 'Docker context is not a local named-pipe or Unix-socket endpoint; refusing to modify any database.'
    }
    $services = @(docker compose ps --status running --services)
    if ($LASTEXITCODE -ne 0 -or 'postgres' -notin $services) {
        throw 'The local Compose postgres service is not running; no database changes were made.'
    }

    if ($Operation -eq 'Grant') {
        $sql = @'
DO $$ BEGIN
  IF current_database() <> 'smartrail_dev' OR current_user <> 'smartrail_dev' THEN
    RAISE EXCEPTION 'Refusing role grant outside local smartrail_dev';
  END IF;
END $$;
WITH new_assignment AS (
  INSERT INTO identity_role_assignment
    (id, cognito_subject, role_code, granted_at_utc, granted_by_subject, revoked_at_utc, revoked_by_subject)
  VALUES (gen_random_uuid(), '__SQL_SUBJECT__', '__SQL_ROLE__', now(), '__SQL_ACTOR__', NULL, NULL)
  ON CONFLICT (cognito_subject, role_code)
    WHERE revoked_at_utc IS NULL AND revoked_by_subject IS NULL
    DO NOTHING
  RETURNING id, granted_at_utc, granted_by_subject
)
INSERT INTO identity_role_assignment_audit
  (id, role_assignment_id, action, actor_subject, occurred_at_utc, evidence_reference)
SELECT gen_random_uuid(), id, 'Granted', granted_by_subject, granted_at_utc, '__SQL_EVIDENCE__'
FROM new_assignment;
SELECT 'grant complete or already active' AS result;
'@
    }
    else {
        $sql = @'
DO $$ BEGIN
  IF current_database() <> 'smartrail_dev' OR current_user <> 'smartrail_dev' THEN
    RAISE EXCEPTION 'Refusing role revocation outside local smartrail_dev';
  END IF;
END $$;
WITH revoked AS (
  UPDATE identity_role_assignment
  SET revoked_at_utc = now(), revoked_by_subject = '__SQL_ACTOR__'
  WHERE id = (
    SELECT id FROM identity_role_assignment
    WHERE cognito_subject = '__SQL_SUBJECT__' AND role_code = '__SQL_ROLE__'
      AND revoked_at_utc IS NULL AND revoked_by_subject IS NULL
    LIMIT 1
  )
  RETURNING id, revoked_at_utc, revoked_by_subject
)
INSERT INTO identity_role_assignment_audit
  (id, role_assignment_id, action, actor_subject, occurred_at_utc, evidence_reference)
SELECT gen_random_uuid(), id, 'Revoked', revoked_by_subject, revoked_at_utc, '__SQL_EVIDENCE__'
FROM revoked;
SELECT 'revoke complete or no active grant existed' AS result;
'@
    }

    $sql = $sql.Replace('__SQL_SUBJECT__', $sqlSubject).Replace('__SQL_ROLE__', $sqlRole).
        Replace('__SQL_ACTOR__', $sqlActor).Replace('__SQL_EVIDENCE__', $sqlEvidence)
    docker compose exec -T postgres psql -X -v ON_ERROR_STOP=1 -U smartrail_dev -d smartrail_dev -c $sql
    if ($LASTEXITCODE -ne 0) { throw 'Local role mapping operation failed. No remote database was contacted.' }

    $verifySql = "SELECT role_code, CASE WHEN revoked_at_utc IS NULL THEN 'active' ELSE 'revoked' END AS state FROM identity_role_assignment WHERE cognito_subject = '$sqlSubject' ORDER BY role_code, granted_at_utc;"
    docker compose exec -T postgres psql -X -v ON_ERROR_STOP=1 -U smartrail_dev -d smartrail_dev -c $verifySql
    if ($LASTEXITCODE -ne 0) { throw 'The local role mapping changed, but its verification query failed.' }
}
finally {
    Pop-Location
}
