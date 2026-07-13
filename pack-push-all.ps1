$ErrorActionPreference = "Stop"
$base = $PSScriptRoot
$nupkgs = Join-Path $base "nupkgs"
$version = "1.0.1"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  全部 NuGet 打包 & 推送" -ForegroundColor Cyan
Write-Host "  版本: $version" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# 1. 清理旧包
if (Test-Path $nupkgs) {
    Remove-Item "$nupkgs\*.nupkg" -Force -ErrorAction SilentlyContinue
    Write-Host "[CLEAN] 已清理 nupkgs 目录" -ForegroundColor Green
}
New-Item -ItemType Directory -Path $nupkgs -Force | Out-Null

# 2. 按依赖顺序打包 src 项目
Write-Host "`n>>> 打包 src 项目 <<<" -ForegroundColor Yellow

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
        Write-Host "[FAIL] $name 打包失败！" -ForegroundColor Red
        exit 1
    }
    Write-Host "[OK] $name 打包完成" -ForegroundColor Green
}

# 3. 打包 tools 项目
Write-Host "`n>>> 打包 tools 项目 <<<" -ForegroundColor Yellow

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
        Write-Host "[FAIL] $name 打包失败！" -ForegroundColor Red
        exit 1
    }
    Write-Host "[OK] $name 打包完成" -ForegroundColor Green
}

# 4. 列出生成的包
Write-Host "`n>>> 生成的包 <<<" -ForegroundColor Yellow
Get-ChildItem "$nupkgs\*.nupkg" | ForEach-Object {
    Write-Host "  $($_.Name)" -ForegroundColor White
}

# 5. 推送到 NuGet.org
Write-Host "`n>>> 推送到 NuGet.org <<<" -ForegroundColor Yellow
$apiKey = $env:NUGET_API_KEY
if (-not $apiKey) {
    $apiKey = Read-Host "请输入 NuGet API Key (或设置环境变量 NUGET_API_KEY)"
}
if (-not $apiKey) {
    Write-Host "[SKIP] 未提供 API Key，跳过推送" -ForegroundColor Yellow
    exit 0
}

Get-ChildItem "$nupkgs\*.nupkg" | ForEach-Object {
    Write-Host "[PUSH] $($_.Name) ..." -ForegroundColor Blue
    dotnet nuget push $_.FullName --api-key $apiKey --source https://api.nuget.org/v3/index.json --skip-duplicate
    if ($LASTEXITCODE -ne 0) {
        Write-Host "[WARN] $($_.Name) 推送可能失败，继续..." -ForegroundColor Yellow
    }
}

# 6. 同时复制到本地 feed
Write-Host "`n>>> 复制到本地 feed <<<" -ForegroundColor Yellow
$localFeed = Join-Path $base "local-nuget-feed"
if (-not (Test-Path $localFeed)) {
    New-Item -ItemType Directory -Path $localFeed -Force | Out-Null
}
Get-ChildItem "$nupkgs\*.nupkg" | ForEach-Object {
    Copy-Item $_.FullName $localFeed -Force
    Write-Host "  Copied: $($_.Name)" -ForegroundColor Gray
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "  ALL DONE" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan