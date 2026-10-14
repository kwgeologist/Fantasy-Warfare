#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
source tools/cloud-env.sh
export XDG_DATA_HOME=/workspace/toolchains/godot-data
export XDG_CONFIG_HOME=/workspace/toolchains/godot-config
bash tools/install-windows-export-templates.sh
# Revalidate source before exporting; no gameplay or test source is rewritten.
bash tools/verify.sh
output="$PWD/builds/windows-x64"
mkdir -p "$output"
"$GODOT" --headless --path game --export-release "Windows x64" "$output/FantasyWarfare.exe"
dotnet publish tests/Simulation.Tests/Simulation.Tests.csproj -c ExportRelease -r win-x64 --self-contained true -o builds/windows-tests
# Share the game's exact runtime and Simulation.dll; do not replace its simulation assembly.
for file in Simulation.Tests.exe Simulation.Tests.dll Simulation.Tests.deps.json Simulation.Tests.runtimeconfig.json; do
  cp "builds/windows-tests/$file" "$output/data_FantasyWarfare_windows_x86_64/$file"
done
mkdir -p "$output/licenses"
curl -fsSL https://raw.githubusercontent.com/godotengine/godot/4.6.3-stable/LICENSE.txt -o "$output/licenses/GODOT-LICENSE.txt"
curl -fsSL https://raw.githubusercontent.com/godotengine/godot/4.6.3-stable/COPYRIGHT.txt -o "$output/licenses/GODOT-THIRD-PARTY-NOTICES.txt"
cp "$NUGET_PACKAGES/microsoft.netcore.app.runtime.win-x64/8.0.32/LICENSE.TXT" "$output/licenses/DOTNET-LICENSE.txt"
cp "$NUGET_PACKAGES/microsoft.netcore.app.runtime.win-x64/8.0.32/THIRD-PARTY-NOTICES.TXT" "$output/licenses/DOTNET-THIRD-PARTY-NOTICES.txt"
python3 tools/package-windows.py "$output" builds/Fantasy-Warfare-Gate0-Windows-x64.zip
