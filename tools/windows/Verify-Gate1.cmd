@echo off
setlocal
cd /d "%~dp0"
"data_FantasyWarfare_windows_x86_64\Simulation.Tests.exe"
if not "%ERRORLEVEL%"=="0" goto failure
if exist "gate0-smoke.log" del "gate0-smoke.log"
if exist "gate0-smoke.log" goto failure
start "" /wait "FantasyWarfare.exe" --headless --fixed-fps 20 --log-file "%CD%\gate0-smoke.log" -- --smoke
if not "%ERRORLEVEL%"=="0" goto failure
findstr /C:"PASS Godot integration:" "gate0-smoke.log" >nul
if not "%ERRORLEVEL%"=="0" goto failure
if exist "gate1-smoke.log" del "gate1-smoke.log"
if exist "gate1-smoke.log" goto failure
start "" /wait "FantasyWarfare.exe" --headless --fixed-fps 20 --log-file "%CD%\gate1-smoke.log" -- --gate1-smoke
if not "%ERRORLEVEL%"=="0" goto failure
findstr /C:"PASS Gate 1 Godot integration:" "gate1-smoke.log" >nul
if not "%ERRORLEVEL%"=="0" goto failure
if exist "corrections-smoke.log" del "corrections-smoke.log"
if exist "corrections-smoke.log" goto failure
start "" /wait "FantasyWarfare.exe" --headless --fixed-fps 20 --log-file "%CD%\corrections-smoke.log" -- --corrections-smoke
if not "%ERRORLEVEL%"=="0" goto failure
findstr /C:"PASS Corrections Godot integration:" "corrections-smoke.log" >nul
if not "%ERRORLEVEL%"=="0" goto failure
for /L %%T in (0,1,6) do (
  call :terrain %%T
  if errorlevel 1 goto failure
)
echo PASS: 55 simulation tests, both original engine suites, corrections integration and seven playable terrain trials completed.
if /I not "%~1"=="/nopause" pause
exit /b 0
:failure
echo FAIL: see console output and smoke logs; no passing result is claimed.
if /I not "%~1"=="/nopause" pause
exit /b 1

:terrain
if exist "terrain-trial-%1.log" del "terrain-trial-%1.log"
if exist "terrain-trial-%1.log" exit /b 1
start "" /wait "FantasyWarfare.exe" --headless --fixed-fps 20 --log-file "%CD%\terrain-trial-%1.log" -- --terrain-trial=%1
if not "%ERRORLEVEL%"=="0" exit /b 1
findstr /C:"PASS Playable terrain trial:" "terrain-trial-%1.log" >nul
if not "%ERRORLEVEL%"=="0" exit /b 1
findstr /C:"MEASURE playable" "terrain-trial-%1.log"
if not "%ERRORLEVEL%"=="0" exit /b 1
exit /b 0
