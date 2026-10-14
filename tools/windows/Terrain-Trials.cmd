@echo off
setlocal
cd /d "%~dp0"
echo Reproducible terrain movement on the playable battlefield
echo 0 Infantry open: 12m
echo 1 Infantry forest: 12m
echo 2 Cavalry open: 12m
echo 3 Cavalry forest: 12m
echo 4 Infantry level: 8m displacement
echo 5 Infantry uphill: 8m displacement
echo 6 Infantry road: 12m
set /p trial="Choose 0-6: "
for %%T in (0 1 2 3 4 5 6) do if "%trial%"=="%%T" goto launch
exit /b 1
:launch
start "" "FantasyWarfare.exe" -- --terrain-demo=%trial%
echo Press Space in the game to start. The game pauses and reports the measured result on arrival.
echo F9 repeats the same trial. These isolated trials disable enemies to remove traffic/combat interference.
exit /b 0
