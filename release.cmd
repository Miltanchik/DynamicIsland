@echo off
chcp 65001 >nul
setlocal EnableExtensions

set "ROOT=%~dp0"
if "%ROOT:~-1%"=="\" set "ROOT=%ROOT:~0,-1%"
set "SCRIPT=%ROOT%\tools\publish_release.py"

set "PY_EXE=C:\Users\Miltan\AppData\Local\Python\bin\python.exe"
if not exist "%PY_EXE%" set "PY_EXE=python"

if "%~1"=="" goto :HELP
if /I "%~1"=="help" goto :HELP
if /I "%~1"=="--help" goto :HELP
if /I "%~1"=="/?" goto :HELP

if "%~2"=="" (
    "%PY_EXE%" "%SCRIPT%" %1
) else (
    "%PY_EXE%" "%SCRIPT%" %1 -m %2 %3 %4 %5 %6
)
exit /b %ERRORLEVEL%

:HELP
echo ============================================================================
echo   Dynamic Island Release Publisher (main/release/version.json)
echo ============================================================================
echo   release.cmd status                       - Check current version and SHA-256
echo   release.cmd patch "- Fix 1\n- Fix 2"     - Bump patch (3.2.0 -^> 3.2.1), build ^& push
echo   release.cmd minor "- New feature"        - Bump minor (3.2.0 -^> 3.3.0), build ^& push
echo   release.cmd major "- Major update"       - Bump major (3.2.0 -^> 4.0.0), build ^& push
echo   release.cmd 3.2.5 "- Custom version"     - Set explicit version, build ^& push
echo   release.cmd sync                         - Rebuild current version ^& update version.json
echo.
echo   Extra flags: --install (update local installed app), --tag (push git tag vX.Y.Z), --no-push
echo ============================================================================
"%PY_EXE%" "%SCRIPT%" status
exit /b 0