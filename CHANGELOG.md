# Changelog

## Gate 1 corrections — 2026-10-14

- Replace large persistent panels with a compact bottom command/status bar, selected-only scrollable details and collapsible help; retain all controls/cameras.
- Align cell colours and triangular collision/render heights with simulation terrain.
- Apply forward terrain costs to clear-path routing and smoothing; add grade/class speed penalties and uphill fatigue.
- Add measured playable-map open/forest/road/uphill trials, visible Windows trial launcher and frame-independent arrival diagnostics.
- Preserve all 44 prior tests and both original engine suites; add 11 regressions and HUD/render/travel integration.
- Deliver updated self-contained Windows package. Gate 1 stays open for native HUD/terrain acceptance; no Gate 2 or troop model changes.

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

## Gate 1 tactical warfare foundation

- Record user-confirmed native Windows Gate 0 acceptance and authorize Gate 1 scope.
- Extend the shared simulation with multiple identified formations, four unit/weapon classes, ID selection, group orders and versioned scenario data.
- Add terrain speed/LOS, footprint-aware static pathfinding and local formation contact handling.
- Add seeded simultaneous melee/ranged casualties, ammo, fatigue/cohesion/morale, flank/rout pressure, controlled withdrawal, rally and defeat victory.
- Add a knowledge-snapshot-only commander, observation aging/fire/contact discovery, support/threat assessment, spear avoidance, archer screening and actual reserve-discovery replanning.
- Preserve all eight Gate 0 tests; add 36 tactical tests, both original and tactical engine integration checks, and rendered input evidence.
- Update self-contained Windows packaging and verification while retaining the prior Gate 0 downloadable ZIP.
- Document failed checks/corrections and practical limits; native Gate 1 Windows acceptance is pending.
