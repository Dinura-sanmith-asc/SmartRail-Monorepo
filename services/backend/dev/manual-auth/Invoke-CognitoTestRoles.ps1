param(
    [Parameter(Mandatory = $true)][ValidateSet('Grant', 'Revoke')][string]$Operation,
    [Parameter(Mandatory = $true)][string]$ApprovalFile,
    [Parameter(Mandatory = $true)][switch]$ConfirmLocalCognitoTestRoles
)
$ErrorActionPreference = 'Stop'
if (-not $ConfirmLocalCognitoTestRoles) { throw 'Explicit local test-role execution confirmation is required.' }
if (($env:DOTNET_ENVIRONMENT -and $env:DOTNET_ENVIRONMENT -cne 'Development') -or
    ($env:ASPNETCORE_ENVIRONMENT -and $env:ASPNETCORE_ENVIRONMENT -cne 'Development')) {
    throw 'Refusing to override a non-Development environment.'
}
$roleApprovalPath = (Resolve-Path -LiteralPath $ApprovalFile).Path
$rolePreviousLocation = Get-Location
$rolePreviousDotnet = $env:DOTNET_ENVIRONMENT
$rolePreviousAspnet = $env:ASPNETCORE_ENVIRONMENT
try {
    Set-Location -LiteralPath ([IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..')))
    $env:DOTNET_ENVIRONMENT = 'Development'
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    $roleAction = $Operation.ToLowerInvariant()
    dotnet run --project tools/SmartRail.DevSeeder -- --cognito-test-roles $roleAction --approval-file $roleApprovalPath --confirm-local-cognito-test-roles
    if ($LASTEXITCODE -ne 0) { throw 'Local Cognito test-role command did not complete. No credentials or tokens were printed.' }
}
finally {
    [Environment]::SetEnvironmentVariable('DOTNET_ENVIRONMENT', $rolePreviousDotnet, 'Process')
    [Environment]::SetEnvironmentVariable('ASPNETCORE_ENVIRONMENT', $rolePreviousAspnet, 'Process')
    Set-Location -LiteralPath $rolePreviousLocation.Path
}
