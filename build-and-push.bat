@echo off
cd /d d:\自建\项目\ef

echo ========================================
echo   Building Solution...
echo ========================================
dotnet build EfCore.Enterprise.sln -c Release --no-incremental

echo.
echo ========================================
echo   Packing NuGet Packages...
echo ========================================

if exist nupkgs\*.nupkg del /q nupkgs\*.nupkg

dotnet pack src\02-Shared\EfCore.Enterprise.Shared.csproj -c Release -o nupkgs --no-restore
dotnet pack src\01-Domain\EfCore.Enterprise.Domain.csproj -c Release -o nupkgs --no-restore
dotnet pack src\03-Infrastructure\EfCore.Enterprise.Infrastructure.csproj -c Release -o nupkgs --no-restore
dotnet pack src\04-Application\EfCore.Enterprise.Application.csproj -c Release -o nupkgs --no-restore

echo.
echo ========================================
echo   Generated Packages:
echo ========================================
dir nupkgs\*.nupkg

echo.
echo ========================================
echo   Copying to ClipBoard...
echo ========================================
if exist "D:\自建\项目\app\Backend\ClipBoard\local-packages" (
    copy /Y nupkgs\EfCore.Enterprise.*.nupkg "D:\自建\项目\app\Backend\ClipBoard\local-packages\"
    echo [OK] Packages copied to ClipBoard local-packages
) else (
    echo [WARN] ClipBoard local-packages not found, skipping copy
)

echo.
echo ========================================
echo   Git Commit & Push Tag...
echo ========================================
git add .
git commit -m "feat: Mediator scoped + TrackAll query tracking"
git tag -f v1.0.1
git push origin v1.0.1 --force

echo.
echo ========================================
echo   DONE!
echo ========================================
pause