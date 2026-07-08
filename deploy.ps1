$base = $PSScriptRoot
$src = Join-Path $base "tools\EfCore.Enterprise.Templates\content\local-packages"
$nupkgs = Join-Path $base "nupkgs"

if (Test-Path $src) { Remove-Item $src -Recurse -Force }
New-Item -ItemType Directory -Path $src -Force

$files = @("Domain", "Shared", "Application", "Infrastructure")
foreach ($f in $files) {
    $from = Join-Path $nupkgs "EfCore.Enterprise.$f.1.0.1.nupkg"
    Copy-Item $from $src -Force
    Write-Host "Copied: $f"
}

Set-Location $base
dotnet pack tools\EfCore.Enterprise.Templates\EfCore.Enterprise.Templates.csproj -c Release -o nupkgs

git add -A
git commit -m "fix: nuget.config 指向 ./local-packages"
git push origin master
git tag -d v1.0.1
git tag v1.0.1
git push origin :v1.0.1
git push origin v1.0.1

Write-Host "ALL DONE"