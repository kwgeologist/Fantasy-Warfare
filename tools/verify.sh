#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
dotnet restore game/FantasyWarfare.csproj --locked-mode
dotnet restore tests/Simulation.Tests/Simulation.Tests.csproj --locked-mode
dotnet build game/FantasyWarfare.csproj --no-restore
dotnet run --project tests/Simulation.Tests --no-restore
"${GODOT:-godot}" --headless --editor --path game --import
"${GODOT:-godot}" --headless --path game --fixed-fps 20 -- --smoke
