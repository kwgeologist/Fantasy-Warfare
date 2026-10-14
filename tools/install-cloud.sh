#!/usr/bin/env bash
set -euo pipefail
cd /workspace/Fantasy-Warfare
mkdir -p /workspace/toolchains
setup_tmp=$(mktemp -d /tmp/fantasy-warfare-setup.XXXXXX)
trap 'rm -rf "$setup_tmp"' EXIT
if ! /workspace/toolchains/dotnet/dotnet --version 2>/dev/null | rg -qx '8.0.426'; then
  curl -fsSL https://builds.dotnet.microsoft.com/dotnet/Sdk/8.0.426/dotnet-sdk-8.0.426-linux-x64.tar.gz -o "$setup_tmp/sdk.tar.gz"
  echo "8141d01bdc53f604aed3bb661b604707a64209f8a947cd81bf95b4f518951593d77864069191f54ea5e84362a409da6b0f69074bf893b283d7e0cf93e2bf20a8  $setup_tmp/sdk.tar.gz" | sha512sum -c -
  mkdir -p /workspace/toolchains/dotnet
  tar -xzf "$setup_tmp/sdk.tar.gz" -C /workspace/toolchains/dotnet
fi
godot_binary=/workspace/toolchains/Godot_v4.6.3-stable_mono_linux_x86_64/Godot_v4.6.3-stable_mono_linux.x86_64
if [ ! -x "$godot_binary" ]; then
  curl -fsSL https://github.com/godotengine/godot/releases/download/4.6.3-stable/Godot_v4.6.3-stable_mono_linux_x86_64.zip -o "$setup_tmp/godot.zip"
  echo "962c4f5fd3a704de36c9dedcb2f5f6c973065ac8ed8fd6e12c7ebf7e48c3852d774bcef2ccf76b50b206c0c564ceea54315e2943e76e397529923e7de680508e  $setup_tmp/godot.zip" | sha512sum -c -
  unzip -q "$setup_tmp/godot.zip" -d /workspace/toolchains
fi
source tools/cloud-env.sh
bash tools/verify.sh
