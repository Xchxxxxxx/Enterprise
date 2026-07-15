$ErrorActionPreference = "Stop"
$base = $PSScriptRoot
$nupkgs = Join-Path $base "nupkgs"
$version = "1.0.1"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  NuGet Pack & Push" -ForegroundColor Cyan
Write-Host "  Version: $version" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# 1. Clean old packages
if (Test-Path $nupkgs) {
    Remove-Item "$nupkgs\*.nupkg" -Force -ErrorAction SilentlyContinue
    Write-Host "[CLEAN] Cleaned nupkgs directory" -ForegroundColor Green
}
New-Item -ItemType Directory -Path $nupkgs -Force | Out-Null

# 2. Pack src projects in dependency order
Write-Host "`n>>> Packing src projects <<<" -ForegroundColor Yellow

$srcProjects = @(
    "src\02-Shared\EfCore.Enterprise.Shared.csproj",
    "src\01-Domain\EfCore.Enterprise.Domain.csproj",
    "src\03-Infrastructure\EfCore.Enterprise.Infrastructure.csproj",
    "src\04-Application\EfCore.Enterprise.Application.csproj"
)

foreach ($proj in $srcProjects) {
    $name = [System.IO.Path]::GetFileNameWithoutExtension($proj)
    Write-Host "`n[PACK] $name ..." -ForegroundColor Blue
    dotnet pack $proj -c Release -o $nupkgs --no-restore
    if ($LASTEXITCODE -ne 0) {
        Write-Host "[FAIL] $name pack failed!" -ForegroundColor Red
        exit 1
    }
    Write-Host "[OK] $name packed" -ForegroundColor Green
}

# 3. Pack tool projects
Write-Host "`n>>> Packing tool projects <<<" -ForegroundColor Yellow

$toolProjects = @(
    "tools\EfCore.Enterprise\EfCore.Enterprise.csproj",
    "tools\EfCore.Enterprise.Cli\EfCore.Enterprise.Cli.csproj",
    "tools\EfCore.Enterprise.Templates\EfCore.Enterprise.Templates.csproj"
)

foreach ($proj in $toolProjects) {
    $name = [System.IO.Path]::GetFileNameWithoutExtension($proj)
    Write-Host "`n[PACK] $name ..." -ForegroundColor Blue
    dotnet pack $proj -c Release -o $nupkgs
    if ($LASTEXITCODE -ne 0) {
        Write-Host "[FAIL] $name pack failed!" -ForegroundColor Red
        exit 1
    }
    Write-Host "[OK] $name packed" -ForegroundColor Green
}

# 4. List generated packages
Write-Host "`n>>> Generated packages <<<" -ForegroundColor Yellow
Get-ChildItem "$nupkgs\*.nupkg" | ForEach-Object {
    Write-Host "  $($_.Name)" -ForegroundColor White
}

# 5. Push to NuGet.org
Write-Host "`n>>> Pushing to NuGet.org <<<" -ForegroundColor Yellow
$apiKey = $env:NUGET_API_KEY
if (-not $apiKey) {
    $apiKey = Read-Host "Enter NuGet API Key (or set env NUGET_API_KEY)"
}
if (-not $apiKey) {
    Write-Host "[SKIP] No API Key, skipping push" -ForegroundColor Yellow
    exit 0
}

Get-ChildItem "$nupkgs\*.nupkg" | ForEach-Object {
    Write-Host "[PUSH] $($_.Name) ..." -ForegroundColor Blue
    dotnet nuget push $_.FullName --api-key $apiKey --source https://api.nuget.org/v3/index.json --skip-duplicate
    if ($LASTEXITCODE -ne 0) {
        Write-Host "[WARN] $($_.Name) push may have failed, continuing..." -ForegroundColor Yellow
    } else {
        Write-Host "[OK] $($_.Name) pushed" -ForegroundColor Green
    }
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "  All done!" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan