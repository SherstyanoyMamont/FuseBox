@echo off
setlocal EnableDelayedExpansion

title FuseBox Launcher
color 0A

echo ==========================================
echo           FUSEBOX LOCAL LAUNCHER
echo ==========================================
echo.

set "BACKEND_DIR=D:\Projects\FuseBox\FuseBox"
set "FRONTEND_DIR=D:\Projects\fuse-box-frontend"
set "MYSQL_CONTAINER=mysql-container"
set "BACKEND_URL=http://localhost:5133"
set "FRONTEND_URL=http://localhost:3000"

REM ============================================================
REM 1. CHECK DOCKER
REM ============================================================

echo [1/6] Checking Docker...

docker info >nul 2>&1

if errorlevel 1 (
    echo Docker is not running.
    echo Starting Docker Desktop...

    if exist "%ProgramFiles%\Docker\Docker\Docker Desktop.exe" (
        start "" "%ProgramFiles%\Docker\Docker\Docker Desktop.exe"
    ) else (
        echo ERROR: Docker Desktop.exe was not found.
        pause
        exit /b 1
    )

    echo Waiting for Docker Desktop...

    :WAIT_DOCKER
    timeout /t 3 /nobreak >nul
    docker info >nul 2>&1
    if errorlevel 1 goto WAIT_DOCKER
)

echo Docker is ready.
echo.

REM ============================================================
REM 2. CHECK PORT 3306
REM ============================================================

echo [2/6] Checking MySQL port 3306...

set "MYSQL_PID="
set "MYSQL_SERVICE="

REM ------------------------------------------------------------
REM Is Docker MySQL already running?
REM ------------------------------------------------------------

docker ps --filter "name=%MYSQL_CONTAINER%" --filter "status=running" --format "{{.Names}}" | findstr /x "%MYSQL_CONTAINER%" >nul

if not errorlevel 1 (
    echo Docker MySQL is already running.
    goto PORT_CHECK_DONE
)

REM ------------------------------------------------------------
REM Find process listening specifically on port 3306
REM ------------------------------------------------------------

for /f "tokens=5" %%P in ('netstat -ano ^| findstr /R /C:":3306 .*LISTENING"') do (
    set "MYSQL_PID=%%P"
)

if not defined MYSQL_PID (
    echo Port 3306 is free.
    goto PORT_CHECK_DONE
)

echo.
echo WARNING: Port 3306 is already occupied.
echo PID: !MYSQL_PID!
echo.

tasklist /FI "PID eq !MYSQL_PID!"
echo.

REM ------------------------------------------------------------
REM Safety check: only touch mysqld.exe
REM ------------------------------------------------------------

for /f "tokens=1" %%A in ('tasklist /FI "PID eq !MYSQL_PID!" /NH') do (
    set "PROCESS_NAME=%%A"
)

if /I not "!PROCESS_NAME!"=="mysqld.exe" (
    echo ERROR: Port 3306 is occupied by !PROCESS_NAME!.
    echo.
    echo FuseBox Launcher will NOT terminate an unknown process.
    echo PID: !MYSQL_PID!
    echo.
    pause
    exit /b 1
)

echo Local MySQL Server is using port 3306.
echo.
choice /C YN /N /M "Stop local MySQL and continue? [Y/N]: "

if errorlevel 2 (
    echo.
    echo Launch cancelled. MySQL was not stopped.
    pause
    exit /b 0
)

echo.

REM ------------------------------------------------------------
REM Find Windows Service by PID using PowerShell
REM ------------------------------------------------------------

for /f "usebackq delims=" %%S in (`powershell -NoProfile -Command "$s = Get-CimInstance Win32_Service ^| Where-Object { $_.ProcessId -eq !MYSQL_PID! }; if ($s) { $s.Name }"`) do (
    set "MYSQL_SERVICE=%%S"
)

if defined MYSQL_SERVICE (

    echo Windows MySQL service found:
    echo     !MYSQL_SERVICE!
    echo.
    echo Administrator permission may be requested.
    echo Stopping service...

    powershell -NoProfile -Command ^
        "Start-Process powershell -Verb RunAs -Wait -ArgumentList '-NoProfile','-Command','Stop-Service -Name ""!MYSQL_SERVICE!"" -Force'"

) else (

    echo WARNING: Could not determine Windows service name.
    echo Administrator permission may be requested.
    echo Force terminating mysqld.exe...

    powershell -NoProfile -Command ^
        "Start-Process taskkill -Verb RunAs -Wait -ArgumentList '/PID','!MYSQL_PID!','/F'"
)

REM ------------------------------------------------------------
REM Wait for port 3306
REM ------------------------------------------------------------

echo.
echo Waiting for port 3306 to become free...

set /a PORT_WAIT=0

:WAIT_PORT_3306

netstat -ano | findstr /R /C:":3306 .*LISTENING" >nul

if not errorlevel 1 (

    set /a PORT_WAIT+=1

    if !PORT_WAIT! GEQ 20 (
        echo.
        echo ERROR: Port 3306 is still occupied.
        echo.
        netstat -ano | findstr ":3306"
        echo.
        pause
        exit /b 1
    )

    timeout /t 1 /nobreak >nul
    goto WAIT_PORT_3306
)

echo Port 3306 is now free.

:PORT_CHECK_DONE

echo Port check complete.
echo.

REM ============================================================
REM 3. START MYSQL CONTAINER
REM ============================================================

echo [3/6] Starting MySQL...

docker ps --filter "name=%MYSQL_CONTAINER%" --filter "status=running" --format "{{.Names}}" | findstr /x "%MYSQL_CONTAINER%" >nul

if errorlevel 1 (
    docker start %MYSQL_CONTAINER%

    if errorlevel 1 (
        echo ERROR: Could not start %MYSQL_CONTAINER%.
        pause
        exit /b 1
    )
) else (
    echo MySQL container is already running.
)

echo Waiting for MySQL...

:WAIT_MYSQL
docker exec %MYSQL_CONTAINER% mysqladmin ping -umyuser -pmypassword --silent >nul 2>&1

if errorlevel 1 (
    timeout /t 2 /nobreak >nul
    goto WAIT_MYSQL
)

echo MySQL is ready.
echo.

REM ============================================================
REM 4. START BACKEND
REM ============================================================

echo [4/6] Starting ASP.NET backend...

powershell -NoProfile -Command ^
    "try { Invoke-WebRequest -UseBasicParsing '%BACKEND_URL%' -TimeoutSec 2 | Out-Null; exit 0 } catch { if ($_.Exception.Response) { exit 0 } else { exit 1 } }"

if errorlevel 1 (
    start "FuseBox Backend" cmd /k "cd /d %BACKEND_DIR% && dotnet run"
) else (
    echo Backend appears to be already running.
)

echo Waiting for backend...

:WAIT_BACKEND
powershell -NoProfile -Command ^
    "try { Invoke-WebRequest -UseBasicParsing '%BACKEND_URL%/swagger/index.html' -TimeoutSec 2 | Out-Null; exit 0 } catch { exit 1 }"

if errorlevel 1 (
    timeout /t 2 /nobreak >nul
    goto WAIT_BACKEND
)

echo Backend is ready.
echo.

REM ============================================================
REM 5. START FRONTEND
REM ============================================================

echo [5/6] Starting Next.js frontend...

powershell -NoProfile -Command ^
    "try { Invoke-WebRequest -UseBasicParsing '%FRONTEND_URL%' -TimeoutSec 2 | Out-Null; exit 0 } catch { exit 1 }"

if errorlevel 1 (
    start "FuseBox Frontend" cmd /k "cd /d %FRONTEND_DIR% && npm run dev"
) else (
    echo Frontend appears to be already running.
)

echo Waiting for frontend...

:WAIT_FRONTEND
powershell -NoProfile -Command ^
    "try { Invoke-WebRequest -UseBasicParsing '%FRONTEND_URL%' -TimeoutSec 2 | Out-Null; exit 0 } catch { exit 1 }"

if errorlevel 1 (
    timeout /t 2 /nobreak >nul
    goto WAIT_FRONTEND
)

echo Frontend is ready.
echo.

REM ============================================================
REM 6. OPEN WEBSITE
REM ============================================================

echo [6/6] Opening FuseBox...

start "" "%FRONTEND_URL%"

echo.
echo ==========================================
echo          FUSEBOX IS RUNNING!
echo ==========================================
echo.
echo MySQL:   localhost:3306
echo Backend: %BACKEND_URL%
echo Frontend:%FRONTEND_URL%
echo.
echo You can close this launcher window.
echo Backend and Frontend have their own windows.
echo.

timeout /t 5 /nobreak >nul
exit