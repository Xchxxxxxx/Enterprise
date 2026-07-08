# Local NuGet source registration script
# After running this, any local project can reference EfCore.Enterprise via NuGet

$ErrorActionPreference = "Stop"

$scriptRoot = $PSScriptRoot
$localFeed = Join-Path $scriptRoot "local-nuget-feed"
$sourceName = "EfCore.Enterprise Local"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  EfCore.Enterprise Local NuGet Setup" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

if (-not (Test-Path $localFeed)) {
    New-Item -ItemType Directory -Path $localFeed -Force | Out-Null
    Write-Host "[OK] Created feed dir: $localFeed" -ForegroundColor Green
}

$nupkgSource = Join-Path $scriptRoot "tools\nupkg"
if (Test-Path $nupkgSource) {
    Copy-Item "$nupkgSource\*.nupkg" $localFeed -Force
    Write-Host "[OK] Copied NuGet packages to feed" -ForegroundColor Green
}

$existingSource = dotnet nuget list source 2>&1 | Select-String -Pattern $sourceName -SimpleMatch

if ($existingSource) {
    Write-Host "[OK] Source '$sourceName' already registered" -ForegroundColor Green
} else {
    dotnet nuget add source $localFeed --name $sourceName
    Write-Host "[OK] Registered NuGet source: $sourceName" -ForegroundColor Green
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  Setup complete! Any project can now:" -ForegroundColor Green
Write-Host ""
Write-Host "  dotnet add package EfCore.Enterprise.Domain"
Write-Host "  dotnet add package EfCore.Enterprise.Shared"
Write-Host "  dotnet add package EfCore.Enterprise.Infrastructure"
Write-Host "  dotnet add package EfCore.Enterprise.Application"
Write-Host "  dotnet new install EfCore.Enterprise.Templates"
Write-Host "  dotnet tool install -g EfCore.Enterprise.Cli"
Write-Host "========================================" -ForegroundColor Green