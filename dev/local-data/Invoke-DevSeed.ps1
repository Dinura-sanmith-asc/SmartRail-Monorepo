param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('Check', 'Migrate', 'Seed', 'StationDisplayT030')]
    [string]$Action
)

$ErrorActionPreference = 'Stop'
$seedRepoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$seedPreviousLocation = Get-Location
$seedPreviousDotnet = $env:DOTNET_ENVIRONMENT
$seedPreviousAspnet = $env:ASPNETCORE_ENVIRONMENT
if (($seedPreviousDotnet -and $seedPreviousDotnet -cne 'Development') -or
    ($seedPreviousAspnet -and $seedPreviousAspnet -cne 'Development')) {
    throw 'Refusing to override a non-Development environment.'
}
try {
    Set-Location -LiteralPath $seedRepoRoot
    $env:DOTNET_ENVIRONMENT = 'Development'
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    # Reads configuration and validates the exact target without connecting to PostgreSQL.
    dotnet run --project tools/SmartRail.DevSeeder -- --check
    if ($LASTEXITCODE -ne 0) { throw 'Local target validation failed. No migration or seed was attempted.' }
    if ($Action -eq 'Migrate') {
        dotnet ef database update 20261020031244_DriverOperations --project src/SmartRail.Infrastructure --startup-project src/SmartRail.Api --context SmartRailDbContext
        if ($LASTEXITCODE -ne 0) { throw 'Reviewed local migration did not complete.' }
    }
    elseif ($Action -eq 'Seed') {
        dotnet run --project tools/SmartRail.DevSeeder -- --confirm-local-development --enable-development-sample-categories
        if ($LASTEXITCODE -ne 0) { throw 'Local seed did not complete. Review README; successful earlier steps are resumable.' }
    }
    elseif ($Action -eq 'StationDisplayT030') {
        dotnet run --project tools/SmartRail.DevSeeder -- --station-display-t030 --confirm-local-development
        if ($LASTEXITCODE -ne 0) { throw 'T030 local Station Display seed did not complete.' }
    }
}
finally {
    [Environment]::SetEnvironmentVariable('DOTNET_ENVIRONMENT', $seedPreviousDotnet, 'Process')
    [Environment]::SetEnvironmentVariable('ASPNETCORE_ENVIRONMENT', $seedPreviousAspnet, 'Process')
    Set-Location -LiteralPath $seedPreviousLocation.Path
}
