# Gate 1 test plan

`dotnet run --project tests/Simulation.Tests` is the real assertion runner, with nonzero exit for any failed assertion. It executes 55 tests: all 44 preserved Gate 0/Gate 1 checks plus 11 corrections regressions. `dotnet test` is not the runner. `bash tools/verify.sh` performs frozen restores, build, all tests, editor import, both original engine suites, corrections HUD/render integration and seven playable movement trials.

## Preserved Gate 0 coverage

Original fixed clock/remainder, arrival/no drift, live destination replacement, pause/order timing, paused replacement/waypoints, seeded frame partition, target rejection, ridge/valley checks remain in `Program.cs`. No assertions were deleted or relaxed. Gate 1 checks are called separately from `Gate1Tests.cs`. The original Godot click/move/reissue/pause/camera assertions remain executable with `--smoke`.

## Tactical checks

Six-per-side identity/four classes; ownership-safe click/toggle/rectangle selection; group offsets/pace and pause receipt; atomic errors; road/open/forest/cavalry speeds; slopes; detour footprint safety; narrow gap rejection; ridge/forest LOS; concealed attack rejection; simultaneous melee and morale; ranged ammo/casualties; range/LOS rejection; ammo exhaustion; firing discovery; spear/front/rear power; fatigue recovery; forest cohesion; withdrawal vs rout; routing contagion and safe rally; unsafe rally rejection; planner API/no-live-state boundary; hidden-state decision invariance; spear avoidance; archer protection/screens; unexpected reserve discovery and received changed order; stale contact integrity; complete seeded/frame-independent combat/event/decision traces; forest melee contact; flank morale; real default skirmish combat; versioned scenario roster/weapons; invalid data; commander ammo limit; victory and rejected post-victory orders.

## Engine checks

```bash
"$GODOT" --headless --path game --fixed-fps 20 -- --smoke
"$GODOT" --headless --path game --fixed-fps 20 -- --gate1-smoke
```

The tactical check uses actual projected coordinates, physics raycasts and input adapters. It asserts 12 formations, click/shift/drag box selection, offset group movement/reissue, paused queue/resume, hold/withdraw/rally and select-all, hidden enemy mesh/picking suppression, and RTS/free/overhead/orbit/zoom. It is bounded to 180 frames, while the original regression is bounded to 120. It cannot pass merely because a window exists.

## Windows package and manual acceptance

Extract the executable ZIP and run `Verify-Gate1.cmd /nopause`: the game's exact Simulation.dll and shared bundled runtime are tested; both engine suites run using the exported Windows binary, with fresh-log PASS markers and exit-code checks. Manifest hashes and ZIP CRC are checked during packaging, then hashes checked after fresh extraction. Wine execution is compatibility-layer validation; native Windows/GPU acceptance remains pending for Gate 1.

On Windows: verify all selection modes, group offsets, live replacements, paused queues, all camera modes and speed controls. Move onto roads/woods/slopes, around rock and into congested passages. Attack only visible enemies; watch volleys, ammunition, casualties, fatigue/cohesion/morale, withdrawal/rout/rally and victory. Confirm concealed enemies cannot be selected/attacked. Restart for repeatability. Report Windows version, GPU/driver, failing step and engine/smoke logs. Do not begin the next gameplay gate until these controls and mechanics have user acceptance.

See [validation evidence and diagnosed failures](GATE1_VALIDATION.md). Tests prove implemented mechanics/scenarios, not historical calibration, general AI superiority or large-army performance.

## HUD and terrain correction acceptance

Run `--corrections-smoke` for actual mesh per-cell colour/collision agreement, compact edge HUD, selected-only details/collapse/help, button focus/actions, selection/order/pause/camera behavior. Run `--terrain-trial=0` through `6` with fixed-fps 20 for measured playable-map runs; compare exact arrival tick, distance and fatigue to shared-library reference results. `Terrain-Trials.cmd` exposes visible paused-start fixtures for native acceptance; keep orders unchanged during comparisons. Long-render-frame diagnostics are tested separately. See [correction results](GATE1_CORRECTIONS.md) for coordinates, times, failures and pending Windows acceptance.
