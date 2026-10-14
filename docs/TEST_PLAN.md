# Gate 0 test plan

## Automated checks

From the repository root with .NET 8.0.426 and Godot 4.6.3 .NET:

```bash
dotnet build game/FantasyWarfare.csproj
dotnet run --project tests/Simulation.Tests
"$GODOT" --headless --editor --path game --import
"$GODOT" --headless --path game --fixed-fps 20 -- --smoke
```

`bash tools/verify.sh` performs the same validation with frozen dependency restore. Eight simulation tests cover fixed clock/remainder, exact arrival/no drift, live replacement, pause/receipt timing, paused replacement plus appended waypoint, seeded scheduled-input equality across different frame partitions, target rejection, and ridge/valley heights. Failures produce nonzero exit status. The engine check invokes the real input adapter with projected coordinates and uses actual physics raycasts; it asserts selection, terrain targeting, live movement/reissue, pause freeze, pending commands/resumption, projection changes, free orbit and wheel zoom. It is bounded to 120 frames and cannot pass merely because the scene opened.

## Manual target-platform acceptance

On Windows: run the README commands and open `project.godot`. Verify plain/ridge/woods/valley visibility, select formation, order onto each terrain type, observe exact stop, replace a destination while moving, pause and replace/append commands, wait, resume and follow the queue. Use all three views and test orbit/tilt/pan/zoom, including while paused. Confirm no decorative combat controls exist. Terrain consequence mechanics are deliberately absent.

## Current evidence

Linux .NET build and the eight core tests passed during implementation. Godot .NET editor import and headless integration passed. A rendered RTS scene was inspected on a local dummy X display with Mesa llvmpipe; screenshot: `/workspace/gate0-rts.png`. Headless tests do not validate pixels. Windows runtime and Windows export were not run. See `VALIDATION.md` for final commands/results and setup diagnostics.

## Self-contained Windows package

[Windows packaging validation](WINDOWS_BUILD.md) records the subsequent export and compatibility-layer checks. `Verify-Gate0.cmd /nopause` inside the extracted package runs the unchanged eight-test harness against the **game's** Simulation.dll and shared bundled .NET runtime, followed by the exported Windows game's headless integration assertions. It requires no Godot/.NET installation and returns nonzero for either test failure or a missing current-run PASS marker. Native Windows manual acceptance is still required.
