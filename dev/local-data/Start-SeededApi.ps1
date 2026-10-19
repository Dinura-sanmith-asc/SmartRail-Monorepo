param([Parameter(Mandatory = $true)][switch]$EnableDevelopmentSampleCategory)

$ErrorActionPreference = 'Stop'
if (-not $EnableDevelopmentSampleCategory) { throw 'Explicit development sample-category opt-in is required.' }
if (($env:DOTNET_ENVIRONMENT -and $env:DOTNET_ENVIRONMENT -cne 'Development') -or
    ($env:ASPNETCORE_ENVIRONMENT -and $env:ASPNETCORE_ENVIRONMENT -cne 'Development')) {
    throw 'Refusing to override a non-Development environment.'
}
if (Get-ChildItem Env: | Where-Object { $_.Name -like 'IncidentManagement__CategoryCatalog__*' }) {
    throw 'Remove current-session category overrides first; they will not be overwritten.'
}
$seedPreviousLocation = Get-Location
$seedPreviousDotnet = $env:DOTNET_ENVIRONMENT
$seedPreviousAspnet = $env:ASPNETCORE_ENVIRONMENT
try {
    Set-Location -LiteralPath ([IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..')))
    $env:DOTNET_ENVIRONMENT = 'Development'
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    dotnet run --project tools/SmartRail.DevSeeder -- --check
    if ($LASTEXITCODE -ne 0) { throw 'Exact local development database configuration is required.' }
    # Process-local only: never writes a category catalogue to appsettings or User Secrets.
    $env:IncidentManagement__CategoryCatalog__Codes__0 = 'DEV-SAMPLE-OPERATIONAL'
    dotnet run --project src/SmartRail.Api --launch-profile http
    if ($LASTEXITCODE -ne 0) { throw 'Development API did not complete normally.' }
}
finally {
    Remove-Item Env:\IncidentManagement__CategoryCatalog__Codes__0 -ErrorAction SilentlyContinue
    [Environment]::SetEnvironmentVariable('DOTNET_ENVIRONMENT', $seedPreviousDotnet, 'Process')
    [Environment]::SetEnvironmentVariable('ASPNETCORE_ENVIRONMENT', $seedPreviousAspnet, 'Process')
    Set-Location -LiteralPath $seedPreviousLocation.Path
}
