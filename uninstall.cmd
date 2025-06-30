@echo off
setlocal enabledelayedexpansion
REM === Docker Cleanup Script ===

REM === Check for required arguments ===
if "%~1"=="" (
    echo [ERROR] You must provide the port number.
    echo Usage: docker_cleanup.cmd PORT DISTRIBUTION
    exit /b 1
)

if "%~2"=="" (
    echo [ERROR] You must provide the WSL distribution name.
    echo Usage: docker_cleanup.cmd PORT DISTRIBUTION
    exit /b 1
)

set "PORT=%~1"
set "DISTRO=%~2"

echo [INFO] Starting cleanup for port %PORT% and distribution "%DISTRO%"...

REM === Unregister WSL distribution ===
echo [INFO] Unregistering WSL distribution "%DISTRO%"...
wsl --unregister %DISTRO%

REM === Remove Docker CLI folder ===
echo [INFO] Deleting folder C:\sw\DockerCLI...
rd /s /q "C:\sw\DockerCLI"

REM === Remove .wslconfig from user ===
echo [INFO] Removing port forwarding in .wslconfig...


set "infile=%USERPROFILE%\.wslconfig"
set "tempfile=%infile%.tmp"

if exist "%infile%" (
    >"%tempfile%" (
        for /f "usebackq delims=" %%A in ("%infile%") do (
            set "line=%%A"
            setlocal enabledelayedexpansion
            if /i not "!line!"=="localhostForwarding=true" echo !line!
            endlocal
        )
    )
    move /Y "%tempfile%" "%infile%" >nul
) else (
    echo [INFO] No .wslconfig file found. Skipping.
)

echo.
echo [DONE] Cleanup completed for port %PORT% and distribution "%DISTRO%".
pause