FANTASY WARFARE — GATE 0 WINDOWS x64

RUN
1. Extract the ENTIRE ZIP to a writable local folder (not inside the ZIP viewer).
2. Double-click FantasyWarfare.exe. No Godot, .NET SDK or separate .NET Runtime install is required.
3. Keep FantasyWarfare.pck and data_FantasyWarfare_windows_x86_64 beside the executable.
Close the window to exit. No installer or administrator privileges are needed.

REQUIREMENTS AND LIMITATIONS
Windows 10/11, 64-bit x86 processor; graphics driver supporting OpenGL 3.3.
Hardware-specific graphics drivers and Windows system DLLs are supplied by Windows/the hardware vendor, not this ZIP.
The package includes Godot 4.6.3 .NET release engine, Microsoft .NET 8.0.32 runtime, the game/simulation DLLs, and the original test harness.
No separate Visual C++ Redistributable is required by the bundled native binaries: they import Windows system/UCRT libraries.
Offline play; no account, API, online AI, or external game assets required.
Unsigned test build: Windows may show an unknown-publisher/SmartScreen notice. This is not a signed installer.
Windows ARM64/native ARM builds and 32-bit Windows are not validated or included.

CONTROLS
Left click blue formation: select; left click empty terrain: deselect.
Right click terrain: replace move order, including during movement.
Shift + right click: append waypoint.
Space: tactical pause/resume. Paused orders execute only after resumption.
1: elevated RTS; 2: free orbit; 3: overhead orthographic.
WASD: pan; wheel: zoom; middle drag: orbit/tilt in Free view.
Yellow disk: selection; orange marker: current/latest pending destination.

WHAT WORKS / WHAT IS ABSENT
Same Gate 0 scene and mechanics as the tested project: plain/ridge/woods/valley,
one selectable formation with 24 troop placeholders, live orders, waypoints,
pause and deterministic 20 Hz simulation, and three cameras.
Woods are traversable visuals; movement is straight and terrain does not yet change speed or LOS.
No tactical combat, enemies/AI, morale, multi-formation play, campaign, magic, multiplayer or map importer.
No 50,000-troop performance claim; no save/load yet. Terrain label clipping is a known placeholder-art limitation.

OPTIONAL AUTOMATED CHECK
Double-click Verify-Gate0.cmd. It executes eight independent simulation tests
against this game's Simulation.dll, then runs this game's headless engine integration test.
Requires a writable extracted folder to create gate0-smoke.log. For automation use:
  Verify-Gate0.cmd /nopause
PASS must be printed and exit code must be 0. No SDK or Godot editor is used.
The game itself requires no CMD file: double-click FantasyWarfare.exe to play.

VALIDATION STATUS
Source tests: 8/8 PASS on Linux.
Exported Windows test executable: 8/8 PASS under Wine using this game's runtime and Simulation.dll.
Exported Windows engine integration: PASS under Wine (selection, terrain picking,
live move/reissue, paused order queue/resume, three cameras, free orbit and zoom).
Rendered Windows executable and real mouse/keyboard checks: exercised under Wine/Mesa.
These compatibility-layer checks do not establish native Windows/GPU acceptance.
Please test controls, cameras, terrain and movement on your Windows machine before Gate 1.

WINDOWS ACCEPTANCE
Select/move; wait for an exact stop; replace a move while running; pause, replace
and append orders, wait, then resume. Confirm the formation did not move while paused.
Switch 1/2/3 and test pan/zoom/orbit. Move across plain, ridge, woods and valley.
Report Windows version, GPU/driver and any failing step. Normal engine log is in:
  %APPDATA%\Godot\app_userdata\Fantasy Warfare — Gate 0\logs\godot.log
See build-manifest.json for source and packaged file hashes; licenses/ for notices.
