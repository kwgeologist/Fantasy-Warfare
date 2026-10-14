#!/usr/bin/env python3
"""Validate and ZIP an existing self-contained Windows export. No source mutations."""
import hashlib
import json
from pathlib import Path
import shutil
import struct
import subprocess
import sys
import zipfile

root = Path(__file__).resolve().parents[1]
stage = Path(sys.argv[1]).resolve()
archive = Path(sys.argv[2]).resolve()
runtime = stage / "data_FantasyWarfare_windows_x86_64"
required = [stage / "FantasyWarfare.exe", stage / "FantasyWarfare.pck"] + [
    runtime / name for name in ("FantasyWarfare.dll", "GodotSharp.dll", "Simulation.dll",
    "hostfxr.dll", "hostpolicy.dll", "coreclr.dll", "System.Private.CoreLib.dll",
    "Simulation.Tests.exe", "Simulation.Tests.dll", "Simulation.Tests.runtimeconfig.json")]
for file in required:
    if not file.is_file() or file.stat().st_size == 0:
        raise SystemExit(f"Missing/empty required package file: {file}")
for name in ("FantasyWarfare", "Simulation.Tests"):
    config = json.loads((runtime / f"{name}.runtimeconfig.json").read_text())["runtimeOptions"]
    frameworks = config.get("includedFrameworks", [])
    if frameworks != [{"name": "Microsoft.NETCore.App", "version": "8.0.32"}] or "framework" in config:
        raise SystemExit(f"{name} is not self-contained with pinned .NET 8.0.32")
exe = (stage / "FantasyWarfare.exe").read_bytes()
pe = struct.unpack_from("<I", exe, 0x3c)[0]
if exe[:2] != b"MZ" or exe[pe:pe + 4] != b"PE\0\0" or struct.unpack_from("<H", exe, pe + 4)[0] != 0x8664:
    raise SystemExit("Expected a Windows x64 PE executable")
for name in ("README-WINDOWS.txt", "Verify-Gate0.cmd", "Verify-Gate1.cmd", "Terrain-Trials.cmd"):
    text = (root / "tools/windows" / name).read_text()
    (stage / name).write_bytes(text.replace("\n", "\r\n").encode("utf-8"))
(stage / "build-manifest.json").unlink(missing_ok=True)
def digest(p):
    return hashlib.sha256(p.read_bytes()).hexdigest()
source_files = subprocess.check_output(["git", "ls-files", "-z", "src", "tests", "game/Scripts", "game/Scenes", "game/project.godot", "scenarios"], cwd=root).decode().split("\0")
manifest = {
    "package": "Fantasy-Warfare-Gate1-Windows-x64", "godot": "4.6.3", "dotnet_runtime": "8.0.32",
    "self_contained": True, "architecture": "x86_64", "milestone": "Gate 1 HUD and terrain corrections; native acceptance pending",
    "source_commit_at_packaging": subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=root).decode().strip(),
    "gameplay_source_sha256": {f: digest(root / f) for f in source_files if f},
    "files_sha256": {p.relative_to(stage).as_posix(): digest(p) for p in sorted(stage.rglob("*")) if p.is_file()},
    "native_windows_acceptance": "pending; Wine checks are not native Windows validation",
}
(stage / "build-manifest.json").write_text(json.dumps(manifest, indent=2) + "\n")
archive.parent.mkdir(parents=True, exist_ok=True)
with zipfile.ZipFile(archive, "w", zipfile.ZIP_DEFLATED, compresslevel=9) as z:
    for p in sorted(stage.rglob("*")):
        if p.is_file():
            z.write(p, "Fantasy-Warfare-Gate1-Windows-x64/" + p.relative_to(stage).as_posix())
with zipfile.ZipFile(archive) as z:
    if z.testzip() is not None:
        raise SystemExit("ZIP integrity check failed")
print(f"Validated self-contained Windows x64 package: {archive} ({archive.stat().st_size:,} bytes)")
print(f"SHA256 {digest(archive)}")
