@echo off
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

REM === Delete scheduled task ===
echo [INFO] Deleting scheduled task "DockerStart"...
schtasks /Delete /TN "DockerStart" /F

REM === Remove port forwarding ===
echo [INFO] Removing port forwarding for port %PORT%...
netsh interface portproxy delete v4tov4 listenport=%PORT% listenaddress=0.0.0.0

REM === Delete firewall rule ===
echo [INFO] Deleting firewall rule "Docker TCP %PORT%"...
netsh advfirewall firewall delete rule name="Docker TCP %PORT%" >nul 2>&1

echo.
echo [DONE] Cleanup completed for port %PORT% and distribution "%DISTRO%".
pause