FANTASY WARFARE — GATE 1 TACTICAL FOUNDATION — WINDOWS x64

EXTRACT AND RUN
Extract ALL files to a writable local folder and double-click FantasyWarfare.exe.
Keep FantasyWarfare.pck and data_FantasyWarfare_windows_x86_64 beside it.
Godot 4.6.3 .NET engine and Microsoft .NET 8.0.32 runtime are included.
No Godot editor, .NET SDK, separate .NET Runtime or Visual C++ Redistributable install is required.
Windows 10/11 x64 with an OpenGL 3.3 graphics driver is required; hardware drivers are not bundled.
Portable unsigned build; Windows may show an unknown-publisher/SmartScreen notice.
No installer/admin privileges, account, paid API, or online connection is needed to play.

CONTROLS
Click a blue formation to select; Shift-click toggles an additional formation.
Drag a rectangle to multi-select; Shift-drag adds. Tab selects all surviving friendlies.
Right-click terrain for offset-preserving group movement; Shift-right-click appends waypoints.
Right-click a CURRENTLY VISIBLE red formation to attack. Hidden/stale enemies cannot be targeted.
Space pauses/resumes; paused orders execute at fixed ticks after resume.
H holds; X then right-click chooses a controlled withdrawal destination; R rallies survivors if safe.
1 RTS; 2 free orbit; 3 overhead. WASD pan, wheel zoom, middle drag orbit/tilt in free mode.
- / = changes simulation speed (0.5x, 1x, 2x, 4x). F1 hides/shows the HUD; F9 restarts.
Yellow disk = selection; orange marker/line = issued destination/path.
White line on formation = facing. Yellow tracers = actual ranged volleys.
HUD shows ID, class, surviving strength, ammo, M morale, C cohesion, F fatigue, facing and current orders.
Routing formations refuse movement/attack until a safe rally; an atomic group order is rejected if any selected footprint/destination is invalid.

FEATURES
Six formations per side: infantry/swords, spearmen, archers/bows and cavalry/lances.
Roads/open/forest/slopes change movement; rock is impassable; conservative formation footprints route around obstacles.
Ridges and woods block line of sight; factions share legitimately observed contacts.
Melee/ranged attacks, ammunition, casualties, facing, terrain and range modifiers are seeded and repeatable.
Fatigue/cohesion/morale, flank pressure, routing panic, withdrawal, rally and army-collapse victory work.
The enemy commander scouts public terrain, evaluates observed strength, avoids spear fronts,
screens/withdraws threatened archers, respects ammunition, and revises plans on discovery.
No omniscient enemy roster, resource buffs or damage bonuses are supplied to it.

VERIFY WITHOUT DEVELOPMENT TOOLS
Double-click Verify-Gate1.cmd. For automation: Verify-Gate1.cmd /nopause.
Runs 44 real simulation tests (including all 8 Gate 0 regressions), the original engine control regression,
and Gate 1 engine input/visibility tests using this game's exact Simulation.dll/runtime.
PASS and exit code 0 are required. Fresh gate0-smoke.log/gate1-smoke.log files are created.

REPRODUCIBLE EVIDENCE
The surprise scenario reveals enemy spear reserve 2 through actual movement/spotting;
AI changes Exploit at tick 1 to AvoidSpears at tick 81 and executes a withdrawal order.
Tests also exercise the default skirmish until it generates real casualties.
Fixed-seed combat results, events and commander decisions match different frame partitions.

LIMITATIONS / TESTING
Exported Windows binaries and the extracted package are tested under Wine in the build environment.
Gate 0 passed native Windows user acceptance; this Gate 1 build still needs native Windows/GPU acceptance.
Placeholder art; no animation or individual soldier physics. Troops are formation-level aggregates.
Navigation uses a static 2m grid and conservative circular footprints. Local sidestepping may congest
at moving formations/bottlenecks; it is not a full crowd-navigation system. Group pace is capped at
its slowest type, but terrain and congestion can separate formations; no rigid synchronized maneuver.
Combat is an initial configurable abstraction, not validated historical casualty/ballistics modeling.
Terrain is authored/static; no destructible terrain, save/load, campaign, magic, siege, multiplayer,
AI map reconstruction or dungeon modules. No large-army performance claim.
LOS uses center eye height, forest depth and short contact spotting; no weather/night/detection uncertainty.
Contacts age out after 10 seconds. AI is a bounded rule/utility baseline with commitment windows,
not a claim of advanced adaptive intelligence. All stats use the same rules on both sides.

TEST ON WINDOWS BEFORE THE NEXT MILESTONE
Check selection/drag/groups; live orders; pause with replacement/queued orders; all cameras.
Move across roads/forest/slopes and around rock. Issue visible-enemy attacks; watch casualties,
ammo, fatigue, morale, routing and rally. Restart to repeat. Report Windows/GPU/driver and failing steps.
Normal log: %APPDATA%\Godot\app_userdata\Fantasy Warfare — Gate 1\logs\godot.log
See build-manifest.json for source/file hashes and licenses/ for notices.
