# Gate 0 validation record

Environment: Linux x86-64, .NET SDK 8.0.426, Godot 4.6.3 .NET. Scope: Gate 0 only.

## Final executed commands and results

From `/workspace/Fantasy-Warfare`:

- `bash tools/install-cloud.sh` — PASS, exit 0. Idempotent reuse of verified toolchains; invokes the frozen restore/build/core-test/editor-import/engine-test workflow below.
- `dotnet restore game/FantasyWarfare.csproj --locked-mode` — PASS.
- `dotnet restore tests/Simulation.Tests/Simulation.Tests.csproj --locked-mode` — PASS.
- `dotnet build game/FantasyWarfare.csproj --no-restore` — PASS, 0 warnings, 0 errors.
- `dotnet run --project tests/Simulation.Tests --no-restore` — PASS, 8 tests executed, 8 passed, 0 failed.
- `"$GODOT" --headless --editor --path game --import` — PASS, exit 0.
- `"$GODOT" --headless --path game --fixed-fps 20 -- --smoke` — PASS, exit 0: formation selection raycast, terrain move raycast, live movement/reissue, paused queue/freeze/resume, three camera modes, free orbit and wheel zoom.
- `dotnet build game/FantasyWarfare.sln --no-restore` — PASS, 0 warnings, 0 errors; validates the Godot editor's solution build.
- `DISPLAY=:99 "$GODOT" --path game --audio-driver Dummy` — rendered with Mesa llvmpipe on a local dummy Xorg display.
- `python3 /tmp/gate0-input.py` — real XTest mouse/keyboard events selected the formation, paused, issued a right-click move, resumed, and switched Free/Overhead/RTS views; exit 0. Captured screenshots were visually inspected.
- `compare -metric AE /workspace/gate0-paused.png /workspace/gate0-paused-later.png null:` — 0 changed pixels, exit 0. Both show tick 114, position (-20.0, 0.0), one pending order. Resumed capture shows tick 138, position (-22.0, 4.4), no pending order.

Screenshots retained outside checkout: `/workspace/gate0-rts.png`, `gate0-paused.png`, `gate0-paused-later.png`, `gate0-resumed.png`, `gate0-free.png`, `gate0-overhead.png`.

## Diagnosed and resolved failures

- Standard preinstalled Godot lacks C# support; installed the matching .NET edition and verified its release SHA-512 checksum.
- Initial unpinned .NET installer hit a 403 fallback version endpoint. Installed SDK 8.0.426 from Microsoft's official release URL and verified SHA-512 against official release metadata. Final retained SDK files were extracted from the verified archive.
- Managed shell sandbox blocks .NET named mutex/IPC initialization. Build/test/engine operations succeeded in the supported escalated execution context. No package, TLS, or checksum verification was disabled.
- Initial generated C# UI strings had newline syntax errors; corrected to verbatim strings. Final build has no errors.
- Initial engine test assumed one physics callback meant a rendered-frame simulation update. Corrected the test to run after presentation-frame updates, with a bounded wait for the next resume tick. Core assertions were retained.
- Initial graphical launch found no sound device; final visual check uses Godot's supported Dummy audio driver. The dummy display cannot change VSync; that renderer warning does not affect the simulation.

## Unrun and deliberately absent

Windows editor/runtime/export acceptance is NOT RUN. Interactive checks were Linux only; no Windows-ready executable package is claimed. No scale benchmark or cross-platform bitwise determinism claim. No combat, terrain-effect, AI, campaign, magic, map-import or multiplayer check, because those mechanics are outside Gate 0.

Original supplied `AGENTS.md` and `FANTASY_WARFARE_MASTER_PROTOCOL.md` were verified byte-for-byte unchanged. The initial checkout contained no source files or commits. Dependencies/generated output are ignored; source, tests, docs and lockfiles constitute the local Gate 0 change.

Cloud configuration draft save confirmed `install_script` and `start_skill` persistence. It does not publish the environment or verify fresh-task restoration. Review/save environment settings and publish through the product when desired. Local commits have not been pushed; fresh-task restoration of local-only commits is not independently verified.
