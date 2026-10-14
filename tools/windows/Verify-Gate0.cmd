@echo off
setlocal
cd /d "%~dp0"
"data_FantasyWarfare_windows_x86_64\Simulation.Tests.exe"
if errorlevel 1 goto failure
if exist "gate0-smoke.log" del "gate0-smoke.log"
start "" /wait "FantasyWarfare.exe" --headless --fixed-fps 20 --log-file "%CD%\gate0-smoke.log" -- --smoke
if errorlevel 1 goto failure
findstr /C:"PASS Godot integration:" "gate0-smoke.log" >nul
if errorlevel 1 goto failure
echo PASS: simulation suite and Gate 0 control regression completed.
if /I not "%~1"=="/nopause" pause
exit /b 0
:failure
echo FAIL: see console output and gate0-smoke.log. No passing result is claimed.
if /I not "%~1"=="/nopause" pause
exit /b 1
