$base = $PSScriptRoot
$src = Join-Path $base "tools\EfCore.Enterprise.Templates\content\local-packages"
$nupkgs = Join-Path $base "nupkgs"

New-Item -ItemType Directory -Path $src -Force

$files = @("Domain", "Shared", "Application", "Infrastructure")
foreach ($f in $files) {
    $from = Join-Path $nupkgs "EfCore.Enterprise.$f.1.0.1.nupkg"
    Copy-Item $from $src -Force
    Write-Host "Copied: EfCore.Enterprise.$f.1.0.1.nupkg"
}
Write-Host "Done"