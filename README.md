# Fantasy Warfare — Gate 1 tactical foundation

Windows-first Godot 4 .NET + deterministic/headless C# tactical simulation.
[Master protocol](FANTASY_WARFARE_MASTER_PROTOCOL.md) and [AGENTS.md](AGENTS.md) govern scope.
Gate 0 passed native Windows user acceptance. This Gate 1 build is ready for native Windows testing; later gameplay gates are not authorized.

## Download and play — no tools to install

[Download the Gate 1 Windows x64 executable ZIP](https://github.com/kwgeologist/Fantasy-Warfare/blob/main/packages/Fantasy-Warfare-Gate1-Windows-x64.zip?raw=true).
Extract **all** files and double-click **FantasyWarfare.exe**. Keep its `.pck` and `data_FantasyWarfare_windows_x86_64` folder alongside it. Godot 4.6.3 .NET and .NET 8.0.32 runtime are bundled; no Godot editor, SDK, separate runtime or VC++ Redistributable install is required.
Windows 10/11 x64 and an OpenGL 3.3 graphics driver are required. The portable build is unsigned. Run `Verify-Gate1.cmd` for the complete no-SDK automated verification; `/nopause` is available for automation.

The prior [Gate 0 package](https://github.com/kwgeologist/Fantasy-Warfare/blob/main/packages/Fantasy-Warfare-Gate0-Windows-x64.zip?raw=true) is retained. GitHub **Code → Download ZIP** downloads repository source, rather than just the runnable package.

## Working controls

- Click selects a friendly; Shift-click toggles more; rectangle drag selects; Shift-drag adds; Tab selects all surviving friendlies.
- Right-click terrain: coordinated group move with offsets and a common pace cap. Shift-right-click: append waypoints. Right-click a currently visible red formation: attack.
- Space: pause/resume, with orders queued while paused. H: hold. X then right-click: controlled withdrawal. R: safe rally. Routing formations refuse move/attack until rallied.
- 1 RTS / 2 free orbit / 3 overhead; WASD pans, wheel zooms, middle drag orbits/tilts in free mode. `-`/`=` changes speed from 0.5x to 4x. F1 hides/shows HUD. F9 restarts.
- Yellow disk: selection. Orange marker/line: current or pending destination/path. White pointer: facing. Yellow tracers: resolved ranged volleys. HUD tracks identities, strength, ammunition, morale/cohesion/fatigue, facing, orders and command failures.

## Implemented foundation

Six formations per side, including infantry, spearmen, archers and cavalry. One fixed 20 Hz simulation owns all state. Authored roads/open terrain/woods/slopes affect movement and fatigue/cohesion; rock blocks movement. A deterministic grid search respects conservative formation footprints. Ridges and forest depth obstruct sight; contact and firing reveal enemies under the same rules for both factions.

Melee and ranged attacks resolve casualties with seeded randomness, weapon/armor/facing/terrain/range effects, and finite ammunition. Fatigue, casualty/flank/routing pressure, controlled withdrawal, rout, safe rally and army-collapse victory work. Defeated-battle commands are rejected with a restart instruction.

The first enemy commander receives copied own-state and legitimately observed enemy contacts only. It scouts public terrain, evaluates support/threats, avoids frontal spear attacks with cavalry, withdraws/screens threatened archers, respects ammunition and changes plans when observations change. It receives no troop, resource, visibility or damage bonuses. Full belief/plan telemetry stays out of the active player HUD.

## Build and test

Windows development needs **.NET SDK 8.0.426** and **Godot 4.6.3 .NET** plus matching .NET export templates. From the repository root:

```powershell
dotnet restore game/FantasyWarfare.csproj --locked-mode
dotnet build game/FantasyWarfare.csproj --no-restore
dotnet run --project tests/Simulation.Tests
```

Open `game/project.godot` and press F5. Use the `Windows x64` release preset to export; distribute its entire output folder.

Cloud/Linux:

```bash
cd /workspace/Fantasy-Warfare
source tools/cloud-env.sh
bash tools/verify.sh
bash tools/export-windows.sh
"$GODOT" --path game --audio-driver Dummy   # needs a graphical display
```

`tools/install-cloud.sh` prepares the pinned development tools when needed. Managed-sandbox .NET IPC may require the supported escalated execution context. The test project is a real console assertion harness, run by `dotnet run`, not `dotnet test`.

## Evidence and practical limits

44 simulation checks include all eight unchanged Gate 0 regressions. Reproducible scenarios exercise actual combat, fixed-seed/frame independence, hidden-state invariance and a commander changing from Exploit@1 to AvoidSpears@81 upon discovery of reserve 2. Original and Gate 1 Godot integration checks exercise real scene/input raycasts, group selection, pause/orders, fog picking and all cameras. See [Gate 1 validation](docs/GATE1_VALIDATION.md) for exact results, diagnosed failures and Windows package checks.

This is a playable foundation, not a historically calibrated combat model or advanced adaptive-AI claim. Navigation uses a static 2m grid/circular footprints and simple local sidestepping; dense moving traffic can congest. Terrain/congestion can separate a group despite its pace cap. Shots are formation salvos with immediate casualty resolution and visual tracers, not ballistic projectiles. Facing changes are instantaneous. Faction contact sharing is immediate; spotting uses center eye height and forest depth, with no probabilistic detection/weather/night rules. Last-known contacts expire after 10 seconds and cannot be targeted as current enemies.

Placeholder art and aggregate troops; no large-army benchmark claim. No save/load, campaign, magic, siege, multiplayer, AI map reconstruction or dungeons. Gate 1 native Windows/GPU acceptance remains pending even when Wine checks pass. Next: test the delivered Windows build and address navigation/control/balance regressions before expanding scope.

[Architecture](docs/ARCHITECTURE.md) · [Contracts](docs/DATA_CONTRACTS.md) · [AI boundary](docs/AI_DESIGN.md) · [Tests](docs/TEST_PLAN.md) · [Roadmap](docs/ROADMAP.md) · [Changelog](CHANGELOG.md)
