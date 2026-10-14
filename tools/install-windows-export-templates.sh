#!/usr/bin/env bash
set -euo pipefail
# Reuse a downloaded archive only after verifying the pinned official checksum.
export XDG_DATA_HOME="${XDG_DATA_HOME:-/workspace/toolchains/godot-data}"
template_dir="$XDG_DATA_HOME/godot/export_templates/4.6.3.stable.mono"
archive="${GODOT_TEMPLATE_ARCHIVE:-/tmp/godot-mono-templates.tpz}"
if [ ! -f "$archive" ]; then
  curl -fsSL https://github.com/godotengine/godot/releases/download/4.6.3-stable/Godot_v4.6.3-stable_mono_export_templates.tpz -o "$archive"
fi
echo "6756c4c45cc711652fab0f5fbb75459f372f1f4b6cca6bb48e8953c25369418845c8cc39fcda0756effd69d0ecaf50e5653ebb9233b8e33f3bde5d8a81635168  $archive" | sha512sum -c -
mkdir -p "$template_dir"
unzip -j -o "$archive" templates/version.txt 'templates/windows_*x86_64*' -d "$template_dir"
