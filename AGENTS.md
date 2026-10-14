# Codex Project Instructions — Fantasy Warfare

Read `FANTASY_WARFARE_MASTER_PROTOCOL.md` (or `docs/MASTER_PROTOCOL.md` if the repo relocates it) before writing code. It is the authoritative product/design specification. Where implementation detail is unspecified, make a conservative, documented decision that protects the tactical simulation.

## Mission
Build a Windows-first, standalone fantasy warfare game supporting real-time pausable tactical battles and eventually full military campaigns and tabletop RPG interoperability. The defining advantage is fair, adaptive, non-omniscient tactical/strategic AI. **Currently implement Gate 0 only.**

## Binding constraints
- Godot 4 .NET + C#, independent deterministic/headless C# simulation library.
- No AI cheating with hidden knowledge, free troops, resource buffs or combat advantages.
- Fixed simulation tick; simulation state independent of rendering/cameras; seeded randomness and repeatable tests.
- Full army/formation command, with live orders and optional pause. Three views of the same 3D battlefield.
- Large armies are a future measurable target; don't promise 50,000 troop performance without benchmarks.
- Campaign uses one shared simulation with continuous or turn-based clock policies (later milestone).
- No compulsory tech tree, no third-person hero gameplay.
- Military realism first; fantasy and house rules configurable extensions.
- Map importer eventually allows interactive AI terrain generation with user correction; do not build that in Gate 0.
- Dungeon generation/reconstruction is OUT OF SCOPE; do not create dungeon modules or scaffolding.
- Don't bundle copyrighted maps, D&D texts or proprietary setting assets. The engine must be setting-neutral.
- Keep this project separate from all other projects and repositories.

## Working rules
1. Inspect repository and available tooling before editing.
2. Implement one small milestone at a time and maintain a runnable project.
3. Add automated tests for real core mechanics; never fake green tests.
4. Report exact commands executed, passes, failures and what couldn't be tested.
5. No placeholder UI that implies missing mechanics work.
6. Version data contracts; avoid duplicated authoritative unit/army state.
7. Keep AI planners restricted to faction-specific observed state when developed.
8. Prefer placeholders over polishing art before tactical controls work.
9. Update README, architecture notes, roadmap, changelog and test plan when appropriate.
10. End each task with concrete status, run instructions, known limitations and the smallest next useful increment.

## Immediate work (Gate 0)
Initialize the project and implement a minimal 3D plain/ridge/woods/valley scene, switchable RTS/free/overhead cameras, one selectable formation and move orders, orders during play, tactical pause/queue-and-resume, headless fixed-tick C# simulation and tests, plus clear launch instructions. Do not attempt later features yet.
