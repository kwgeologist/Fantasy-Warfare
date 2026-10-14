#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
dotnet restore game/FantasyWarfare.csproj --locked-mode
dotnet restore tests/Simulation.Tests/Simulation.Tests.csproj --locked-mode
dotnet build game/FantasyWarfare.csproj --no-restore
dotnet run --project tests/Simulation.Tests --no-restore
"${GODOT:-godot}" --headless --editor --path game --import
"${GODOT:-godot}" --headless --path game --fixed-fps 20 -- --smoke
"${GODOT:-godot}" --headless --path game --fixed-fps 20 -- --gate1-smoke
"${GODOT:-godot}" --headless --path game --fixed-fps 20 -- --corrections-smoke
for trial in {0..6}; do
  "${GODOT:-godot}" --headless --path game --fixed-fps 20 -- --terrain-trial="$trial"
done
