# Fantasy Warfare — Gate 0

Windows-first Godot 4 .NET / C# tactical foundation. The supplied
[FANTASY_WARFARE_MASTER_PROTOCOL.md](FANTASY_WARFARE_MASTER_PROTOCOL.md) and
[AGENTS.md](AGENTS.md) are authoritative. Gate 0 only is implemented.

## Windows player package (no tools to install)

Download [Fantasy-Warfare-Gate0-Windows-x64.zip](https://github.com/kwgeologist/Fantasy-Warfare/blob/main/packages/Fantasy-Warfare-Gate0-Windows-x64.zip?raw=true), extract the entire ZIP, and double-click **FantasyWarfare.exe**. Keep the `.pck` and `data_FantasyWarfare_windows_x86_64` folder alongside it. This contains the engine and .NET runtime; Godot and the .NET SDK are only needed to develop/rebuild the project.

Windows 10/11 x64 with an OpenGL 3.3 graphics driver is required. See the included `README-WINDOWS.txt` and [Windows build notes](docs/WINDOWS_BUILD.md). The exact exported Windows executable passed core/integration and rendered input checks under Wine; native Windows/GPU acceptance remains pending. No new gameplay is included.

## Windows developer launch

1. Install **.NET SDK 8.0.426** and **Godot 4.6.3 .NET** (the standard Godot download cannot run C#).
2. From this repository root, run:

```powershell
dotnet restore game/FantasyWarfare.csproj --locked-mode
dotnet build game/FantasyWarfare.csproj --no-restore
dotnet run --project tests/Simulation.Tests
```

3. Open `game/project.godot` in Godot .NET, let import finish, build C# if prompted, and press **F6** on `Scenes/Battle.tscn` or **F5** to run the project.
   CLI alternative, using your .NET Godot executable:

```powershell
& 'C:\Tools\Godot\Godot_v4.6.3-stable_mono_win64.exe' --editor --path game --import
& 'C:\Tools\Godot\Godot_v4.6.3-stable_mono_win64.exe' --path game
```

Replace the example executable path with your installed executable. Windows launch instructions are provided; Windows execution has not been validated in this Linux environment.

## Cloud / Linux

```bash
cd /workspace/Fantasy-Warfare
bash tools/install-cloud.sh       # pinned, checksum-verified tooling + validation
source tools/cloud-env.sh         # repeat in each new shell
bash tools/verify.sh
"$GODOT" --path game --audio-driver Dummy  # requires a graphical display
```

The verification script imports the editor and runs a **real engine headless integration check**, in addition to the independent simulation harness. In this managed sandbox .NET named mutex/IPC operations require an escalated execution context. The cloud has standard Godot too; always use `$GODOT` after sourcing the helper to select the .NET edition. No credentials or online service are needed during gameplay.

## Controls and implemented behavior

- Left click the blue formation to select it; click elsewhere to deselect.
- Right click terrain to replace its current move order, including while moving.
- Shift + right click appends a waypoint. An ordinary right click replaces the active route.
- Space pauses/resumes. Paused commands are pending until the next simulation tick after resume; paused wall time is discarded.
- 1: elevated RTS view. 2: free orbit view. 3: overhead orthographic view.
- WASD pans the camera, mouse wheel zooms, middle drag orbits/tilts in free mode.
- Yellow highlight shows selection; orange marker shows the current/latest pending destination. The HUD reports state, position, tick and order counts.

One authoritative formation with 24 placeholder troop visuals moves across a 100 m square 3D height-field battlefield with a plain, ridge, woods and valley. Its 20 Hz simulation is independent of cameras and rendering. Movement is 4 m/s in horizontal simulation coordinates; it snaps to its destination without overshoot.

## Scope and validation limits

This is a foundation, not combat gameplay. No enemy AI, combat, morale, terrain speed/LOS effects, obstacle avoidance, campaign, magic, multiplayer, map import, or dungeon modules are implemented. Woods are traversable visual terrain; orders follow straight paths. Formation footprint is visual and does not enforce collision. Seed is retained for reproducible scenario identity; Gate 0 has no random mechanic. Cross-platform bitwise replay and large-army performance are not claimed. A self-contained Windows x64 player package is now provided; native Windows acceptance remains pending.

See [test plan](docs/TEST_PLAN.md), [architecture](docs/ARCHITECTURE.md),
[data contracts](docs/DATA_CONTRACTS.md), [roadmap](docs/ROADMAP.md), and
[changelog](CHANGELOG.md). The custom console harness intentionally runs with `dotnet run`; `dotnet test` is not its runner.
