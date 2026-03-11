@echo off
setlocal

REM ── CMR Compression Toolkit – Local Launcher ──────────────────────────────
REM  Starts the ASP.NET Core web GUI on http://localhost:5000 and opens the
REM  browser once the server is ready.
REM  Requires .NET 8 SDK (https://dot.net) and ffmpeg in PATH.
REM ──────────────────────────────────────────────────────────────────────────

REM Check for .NET 8+
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo [ERROR] dotnet not found. Install the .NET 8 SDK from https://dot.net
    pause
    exit /b 1
)

REM Open browser once port 5000 is ready (polls every second, up to 30s)
start "" powershell -NoProfile -Command ^
  "$url='http://localhost:5000'; for($i=0;$i -lt 30;$i++){try{$r=(Invoke-WebRequest $url -UseBasicParsing -TimeoutSec 1 -ErrorAction Stop).StatusCode;if($r -lt 500){Start-Process $url;break}}catch{};Start-Sleep 1}"

REM Start the app
cd /d "%~dp0webgui"
dotnet run --no-launch-profile --urls http://localhost:5000

pause
