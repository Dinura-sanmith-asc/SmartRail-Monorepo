[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
Push-Location $repoRoot
try {
    $overrides = [Environment]::GetEnvironmentVariables('Process').Keys |
        Where-Object { $_ -like 'Authentication__Cognito__*' }
    if ($overrides) {
        throw 'Remove Authentication__Cognito__* environment overrides from this PowerShell session, then rerun. Values were not displayed.'
    }

    dotnet user-secrets set 'Authentication:Cognito:Issuer' 'http://127.0.0.1:5107' --project src/SmartRail.Api
    dotnet user-secrets set 'Authentication:Cognito:AllowedClientIds:0' 'smartrail-local-manual-client' --project src/SmartRail.Api
    dotnet user-secrets remove 'Authentication:Cognito:ExpectedAudience' --project src/SmartRail.Api | Out-Null
    foreach ($index in 1..9) {
        dotnet user-secrets remove "Authentication:Cognito:AllowedClientIds:$index" --project src/SmartRail.Api | Out-Null
    }
    Write-Host 'Development User Secrets now point to the loopback manual issuer and test client. No signing key or token was stored.'
    Write-Host 'This replaces the Development Cognito issuer/client settings; restore your normal local values when finished.'
}
finally {
    Pop-Location
}
