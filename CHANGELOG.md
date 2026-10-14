# Changelog

## Gate 0 foundation — 2026-10-13

- Preserve the supplied master protocol and agent requirements.
- Initialize .NET 8 simulation and standalone assertion harness plus Godot 4.6.3 .NET project.
- Implement one formation, fixed 20 Hz clock, live movement replacement, pending paused orders, waypoint queue and deterministic scheduled inputs.
- Add shared 3D plain/ridge/woods/valley terrain, selectable troop placeholders, intent/status display, RTS/free/overhead views, orbit/pan/zoom.
- Add engine integration smoke checks, pinned dependency lockfiles, verified cloud tool installation and Windows/cloud launch documentation.
- Remain strictly within Gate 0; target-platform Windows acceptance is outstanding.

## Gate 0 Windows packaging

- Add a Windows x64 release export preset and pin the bundled .NET runtime to 8.0.32.
- Keep export restore lockfiles separate from editor/test lockfiles.
- Add reproducible verified-template export/packaging scripts, runtime/file manifests, third-party notices and a no-SDK verification command.
- Package the original tests beside the game's exact Simulation.dll and shared self-contained runtime.
- Verify the Windows export's eight tests and headless engine checks under Wine, plus rendered selection, pause/resume/movement and camera input.
- Preserve all gameplay, simulation and automated-test source. Native Windows/GPU acceptance remains pending; Gate 1 is not started.
