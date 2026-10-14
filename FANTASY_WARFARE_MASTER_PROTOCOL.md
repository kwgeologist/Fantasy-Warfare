# FANTASY WARFARE
## Master Game Design, Architecture & Codex Development Protocol
**Version:** 1.0 consolidated — 2026-10-13  
**Platform:** Windows PC  
**Recommended stack:** Godot 4 .NET, C#, .NET test project  
**Status:** Design brief; implementation not yet verified  
**Scope:** Standalone tactical battles and full warfare campaigns; optional tabletop fantasy integration. Dungeon reconstruction is explicitly out of scope.

## 1. Product vision
Build a fantasy-capable military strategy and warfare simulator where tactics, intelligence, terrain, logistics, command, deception, adaptation and leadership matter more than clicking speed or artificial opponent bonuses. The game must work independently as an enjoyable standalone game and support optional use by a tabletop DM to resolve a war or a single battle involving high-level RPG characters and armies.

The defining challenge is an AI commander that develops its own plans, scouts with imperfect intelligence, recognizes changing conditions, and can revise strategy. An ambush should sometimes succeed; other times the AI may detect it, avoid it, or mount a countermaneuver. Neither the AI nor the player should know what their forces have not legitimately discovered.

### Binding product decisions
- **Windows PC first**; single-player versus computer opponents first.
- **Real-time tactical battles** with optional pause and adjustable speed. All orders can be issued while running; paused orders execute upon resumption.
- **Two selectable campaign-time modes:** continuous accelerated time with pause, or turn-based planning and advancement. Both operate over **one authoritative campaign simulation**, not two separate rule engines.
- **Scalable formations:** from small engagements to 50,000+ troop battles as a long-term validated performance goal. Wars may involve multiple armies and fronts.
- **Full-army command** at army, wing and formation levels. Heroes and spellcasters are ordered through the tactical UI; do not build third-person hero control.
- **Real-world military mechanics as the baseline** with configurable fantasy elements and tabletop house rules.
- **No mandatory historical technology/era progression.** Army capabilities follow faction, scenario and ruleset.
- **Standalone skirmish / scenario mode** and **full regional warfare / invasion campaign mode**.
- **Three views over one 3D battlefield:** elevated RTS/isometric, free camera, and overhead command map.
- **Map image import and interactive AI-assisted terrain generation** for campaign regions and extracted tactical battlefields. Users approve inferred features and can answer questions about elevation, forest density, crossings, etc.
- **Optional tabletop adapter:** user-supplied armies, commanders, creatures, spells, calendar and maps; output persistent casualties, captured locations, equipment and other battle/war outcomes.
- **Dungeon scan conversion is excluded from active design and implementation.** Do not add dungeon-specific code or dependencies.

## 2. Core gameplay principles
1. Tactical intelligence rather than resource rushing or preset unit counter formulas determines success.
2. Both player and AI can issue new orders and **pivot an entire battle plan** at any moment, subject to command delays and realistic execution.
3. AI must use only its faction's perceived information; no hidden troop locations, player orders, or invented intelligence.
4. Higher difficulty improves decision-making within fair mechanics; no secretly gifted troops, supplies, damage or visibility.
5. Terrain, formation orientation, exposure, line of sight, concealment, weather, fatigue, morale, cohesion, ammunition, and leadership must influence results.
6. Scouting, screening, feints, reserves, flank attacks, organized withdrawal, supply raids, pursuit, and encirclement are supported through genuine simulation mechanics, not one-off scripted events.
7. Victory can be rout, withdrawal, objective capture, relief, negotiated surrender, supply collapse, or destruction according to scenario.
8. Rendering, effects and camera may never affect authoritative battle results.
9. Entire game must be functional offline during ordinary battle and campaign play; optional AI processing during map import can be external, with manual/offline fallback.
10. New features must be tested before declared complete. Favor a playable, compelling tactical core over premature feature breadth.

## 3. Architecture overview
Separate presentation, simulation, rules, AI, assets/import pipeline, campaign and persistence. Favor pure C# libraries for deterministic core logic, with Godot as the rendering/input/UI integration layer.

```text
FantasyWarfare/
  AGENTS.md
  README.md
  CHANGELOG.md
  docs/
    MASTER_PROTOCOL.md
    ARCHITECTURE.md
    GAME_DESIGN.md
    AI_DESIGN.md
    DATA_CONTRACTS.md
    ROADMAP.md
    TEST_PLAN.md
  game/
    project.godot
    Scenes/
    Scripts/              # Godot adapters, rendering, UI, cameras
    Assets/
    Data/
  src/
    Simulation/           # Fixed tick, state, seeded RNG, events
    Terrain/              # Terrain model, navigation, LOS
    Armies/               # Armies, hierarchy, formations, troops
    Orders/               # Command delivery, standing orders
    Combat/               # Melee, ranged, cohesion, morale, fatigue
    Intelligence/         # Faction-specific perception and reports
    TacticalAI/           # Hierarchical planning and execution
    Campaign/             # Shared strategic model, time policies
    Rules/                # Historical baseline and fantasy extensions
    MapImport/            # Future import, interpretation, user review
    Persistence/          # Save/load and replay
    Tabletop/             # Optional import/export adapters
  tests/
    Simulation.Tests/
    AI.Tests/
    Integration.Tests/
  scenarios/
    tutorials/
    regressions/
    benchmarks/
  tools/
```

Structure may change to meet actual .NET/Godot conventions, but modular boundaries are mandatory. The battle simulation must be testable without opening Godot or rendering frames.

### Core state model
- Authoritative `BattleState` and `CampaignState`; no duplicate army roster when a tactical battle is launched from a campaign.
- `FactionPerceptionState` separate from actual state, with observed location, observation time, confidence and stale intelligence.
- Explicit `Order` and `CommandDelivery` entities; issuing an order is distinct from its being received and executed.
- Versioned IDs for factions, commanders, armies, formations, terrain features, rulesets, battles and campaigns.
- Fixed-timestep simulation clock, deterministic scheduling and seeded pseudorandomness; record seed and state for replay.
- Data-driven `UnitDefinition`, `RulesetDefinition`, `SpellDefinition`, `ScenarioDefinition` and terrain types. Use validated JSON or other documented versioned serialization.
- Events and tactical-battle-result application must be idempotent; duplicate event processing cannot duplicate casualties.

## 4. Tactical warfare simulation
### Units and hierarchy
`Army -> Wing/Division -> Formation -> TroopStrength` (configurable levels; avoid mandatory historical formation sizes). Start with light/heavy infantry, spearmen, archers and light/heavy cavalry. Later add crossbowmen, mounted archers, engineers, siege units, specialized monsters, flying units, spellcasters and heroes.

Formations have size, frontage/depth, facing, position, strength, equipment, attack/defense capabilities, visibility, morale, discipline, fatigue, cohesion, command status, ammunition, experience and commander. Do not pathfind or run sophisticated AI for each rendered soldier. Use formation-level simulation and batch visual representation, then benchmark scale rather than asserting it.

### Commands
Start with select, move, attack, hold, withdraw, reposition, and pause. Expand to defend, face target, wheel, pivot (physical), change frontage, scout, screen, ambush, hold fire, fire at will, pursue, rally, controlled feigned retreat, charge, conditional triggers and group/subcommander orders. An **operational strategic pivot** is separate from a physical formation pivot. Formations should obey appropriate delay and cohesion mechanics.

### Physics and combat rules
- Terrain and slopes affect speed, visibility, fatigue, formation integrity, and defensive positioning.
- Forest density affects concealment, ranged visibility, movement, and cavalry suitability.
- Rivers consider fordability, width, depth, current, bridges and equipment.
- Weapons, armor, range, angle of attack, flank and rear exposure, cohesion, ammo, fatigue and morale all affect combat.
- Casualty resolution and morale/rout rules must be reproducible and measurable.
- Planned retreat and genuine rout must be distinct, with chances of disorder depending on training and pressure.
- Weather and time of day eventually affect visibility and movement.
- Siege mechanics eventually include walls, towers, breaches, trebuchets, engineers, supply and blockade.

### Cameras and input
Three views on shared 3D terrain: isometric/elevated RTS, free orbit/zoom/tilt, overhead map. Mouse and hotkey selection, formation multi-select, waypoint orders, queued commands, clear intent/target indicators, formation status display, tactical pause, and adjustable speed. Direct orders must be possible without pausing. Presentation is never authoritative for game simulation.

## 5. Adaptive tactical AI — highest-priority differentiator
Use a layered system, not a single monolithic AI or online LLM dependency:
- **Army commander:** evaluates objectives, plans courses of action, holds reserves, interprets reconnaissance, allocates formations, judges withdrawals, re-plans when battlefield evidence changes.
- **Subordinate commander:** follows mission intent, controls local formations, reacts to flank threats, requests help and maintains cohesion without excessive micro orders.
- **Tactical evaluator:** examines own forces, observed enemy forces, terrain, uncertainty, morale, fatigue, available routes, threats and positional advantage.
- **Perception and intelligence:** must expose only authorized observations to AI planning interfaces. Last-known enemy positions are not guaranteed current.

A practical initial implementation may combine utility scoring, GOAP/HTN-like plan selection, behavior/state machines for local maneuvers, and bounded replanning; refine with evidence from tests. Do not claim 'adaptive' merely because AI changes attack targets. Avoid indecisive oscillation by using commitment windows, hysteresis and plan-change costs while allowing emergency changes.

Commander profiles may vary in aggression, caution, initiative, battlefield experience, reconnaissance discipline, and deception recognition. Different commanders can make different **plausible** decisions using the same information. Don't intentionally make the AI foolish for superficial personality.

The player and AI have analogous action sets and comparable command/visibility constraints. Difficulty settings may alter search depth, planning sophistication, reaction and coordination within documented fair rules, not hidden buffs.

### AI telemetry and analysis
Log what each AI commander believed, what plan was selected, observations that changed the plan, issued commands, and outcomes. After battle, reveal an after-action timeline (including previously concealed opponent reasoning only after the battle, never during it). Add AI-versus-AI and headless automated scenario tests, with reproducible random seeds.

## 6. Full campaign / warfare simulation
Support invasions, multi-army operations, territory, settlements, garrisons, strategic routes, recruitment, reinforcement, replacement, fatigue, supply depots, foraging, transports, blockades, sieges, diplomacy, occupation, strategic objectives, espionage, scouting, false/stale intelligence, and surrender. An invasion involving 50,000 soldiers may consist of multiple field armies and battles across several regions rather than one giant tactical scene.

Strategic AI should protect supply, defend chokepoints, choose invasion corridors, identify offensive/defensive opportunities, allocate garrisons, coordinate armies and adapt to losses, with imperfect information. Never depend on tactical hidden data unavailable at campaign level.

### Dual campaign time
**Mode A — continuous accelerated:** running world clock with variable speed, orders at any time, optional pause, configured auto-pause for major events.

**Mode B — turn-based:** orders submitted in a planning phase; advance by configurable daily or weekly intervals, resolving all factions against one shared timestamped simulation timeline and interrupting for battles/events. Resume the unprocessed remainder after interruption.

One `CampaignSimulation` and event queue; interchangeable time controllers only determine how far/when shared simulation advances. Campaign results should match across modes given the same orders, timing, seeds and rules. No faction gets sequential-turn first-mover advantage. Calendar abstraction supports fantasy calendars/months and does not alter simulation clock math. Mode selected at campaign creation; mid-campaign switching optional later.

### Campaign-to-battle transaction
On encounter, freeze relevant campaign evolution at an exact event time; create tactical battle with **same authoritative participating units** plus terrain, supplies, fatigue, morale, weather and deployment. After battle apply a uniquely identified outcome **exactly once**: casualties, wounds, prisoners, equipment and supply losses, retreat, reinforcement state, morale, commander outcomes and territory changes. Resume campaign clock/turn with updated state. Standalone battles bypass the campaign layer entirely.

## 7. Rules, heroes and fantasy
Baseline is plausible premodern real-world tactics and physically coherent movement/combat, not mandatory historical technology progression. All stats and mechanics must be configurable where appropriate.

Ruleset configurations:
1. **Realistic/no magic:** conventional warfare.
2. **Fantasy:** spellcasters, unusual creatures, flying units, magical terrain, enchanted equipment and morale effects integrated with the physical battle engine.
3. **Custom house rules:** editable combat modifiers, unit statistics, spell behavior, commander attributes and scenario constraints.

Data-driven magic eventually supports casting time, range, area, duration, interruption, resource limitations, counters, saves/resistances and terrain alteration. Magic does not ignore intelligence/visibility principles; enemies discover spell effects based on observation. Hero leaders may attach to formations or be commanded as battlefield entities, but **no third-person action/RPG control is part of the approved project**.

Support optional tabletop campaign adapters to ingest user-authored rosters, character stats, heroes, maps and campaign calendar; export survivors, dead, captured, treasure/equipment, and territorial consequences. Keep commercial/public distribution legally setting-neutral; do not bundle unlicensed AD&D/TSR/Wizards or fan-cartography content. User-provided maps are external inputs subject to their rights and permissions.

## 8. Two-scale mapping and interactive 3D terrain
### Campaign terrain
Regional geography, regions, settlement positions, roads, rivers, forests, mountains, political boundaries, routes, supply corridors and travel restrictions.

### Tactical terrain
A selected local part of a regional map, with ridges, slopes, stands of trees, streams, bridges, walls, choke points, visibility and navigability appropriate for tactical combat.

Maintain coordinate mapping from **source image -> calibrated region coordinates -> generated tactical local coordinates**. Fantasy maps may use arbitrary/custom coordinate systems, not necessarily Earth CRS. Preserve source map attribution and calibration.

### AI-assisted user map import (future module)
- Input raster maps and suitable extracted PDF pages, e.g. a user-provided illustrated Pomarj regional map.
- Interpret title/labels/legend and recognize symbols for forests, hills, mountains, roads, rivers, settlements, coasts, marshes, fortifications.
- Extract **editable intermediate vector/region layers**; do not jump straight from pixels to a locked mesh.
- Infer plausible topography and local detail **with uncertainty annotations**. Stylized fantasy maps do not yield reliable actual elevations automatically.
- Present correction tools and source overlays; user decisions always override estimates.
- Generate editable 3D campaign landscape and extract localized tactical battlefields.
- Preserve source image, parsed features, confidence and provenance (map evidence, user input, defaults or AI inference).
- Do not rely on online processing for ordinary game play; optional online/offline inference backends are distinct from manual editing.

### Interactive terrain-build modes
1. **Automatic:** infer and propose features, flag uncertain ones for review.
2. **Interactive (preferred default for imported maps):** ask *targeted high-impact* questions during build, including elevation/relative relief, slope, forest/undergrowth density, river fordability/width/depth, road quality, passes, cliffs, fortification height and materials, and seasonal condition. Never require a long fixed questionnaire.
3. **Manual:** edit and paint heights, forests, waterways, roads and structures directly.

Allow users to interrupt generation, revise a value, and regenerate only the affected region while preserving approved geometry and edits. Provide undo/redo. Forest density must affect visibility and movement; elevation must affect movement and LOS; road/river decisions must affect navigation and supply. Test functional terrain consequences, not just prettier art. Preview and approvals required for ambiguous features. Offer campaign-region fidelity versus plausible standalone tactical generation as distinct objectives.

## 9. Scenario editor and tabletop workflow
Standalone battle setup must support editable terrain, factions, formation rosters, unit stats, commanders, hidden starting deployments, objectives, weather, rules and victory conditions. A user should be able to start a battle with no campaign file.

Eventually support creating a regional war from an imported or authored map; placing armies, towns, fortifications and supplies; assigning campaign goals; selecting a calendar; and initiating war. Integrate results back into persistent campaign and export human-readable and machine-readable reports to support a tabletop DM.

Keep content-neutral schemas so a user can privately create campaigns in Greyhawk, Mystara, Forgotten Realms or their own setting. Do not ship copyrighted maps or proprietary setting datasets.

## 10. Implementation performance, reliability and persistence
- Fixed simulation timestep, independent frame rate, seeded RNG and reproducible tests.
- Core simulation testable headlessly without the graphics engine.
- Save game includes time mode, calendar, RNG, army/formation state, fog of war, events, queues, orders, decision states and active battle/turn interruption where applicable.
- Typed, validated, versioned save/scenario schemas with migration plans.
- Graceful failure when data is invalid or partial.
- Explicit performance benchmarks; profile the simulation and render separately. 50,000 is a long-term measurable target, not a v0.1 promise.
- Avoid premature optimization that hides different combat rules across sizes; aggregation choices must be benchmarked against reference results.
- Use structured logs, scenario tests, integration tests and regression checks.
- Offline game operation and no hidden recurring API fees.

## 11. Phased roadmap and gates
**Gate 0 — foundation (first Codex assignment):** Repository, Godot .NET project, headless C# simulation/test projects, basic formation model, simple movement orders, minimal 3D terrain with plain/ridge/forest/valley, three working cameras, test harness and documentation. It must compile, open and provide visible working interaction. *No campaign, magic, multiplayer or AI-generated terrain at Gate 0.*

**Gate 1 — credible tactical prototype:** 5–8 formations per side, basic melee/ranged combat, morale/fatigue, terrain effects, pause + live orders, faction-specific visibility, basic opposing commander that can change plan, victory conditions, reproducible integration tests. Prove that changing strategy is mechanically possible and meaningful.

**Gate 2 — genuinely challenging AI:** Better scouting, deception, reserves, flank threats, tactical plan generation/revision, personality, conditional orders, telemetry and after-action analysis. Iterate from gameplay testing rather than from claims about intelligence. Require scenario benchmarks against naive and fixed-script opponents.

**Gate 3 — tactical breadth and scale:** Sieges and improved formations, larger battles, optimization and profiling, editor usability, fantasy magic/hero rules, varying troop definitions, robust save/replay.

**Gate 4 — war campaign:** Shared strategic engine, multiple armies, two time controllers, logistics, strategic AI, territory and campaign-to-battle transactions.

**Gate 5 — cartography/import:** AI-assisted map reading, interactive terrain wizard, regional-to-local mapping, local regeneration, full terrain/scenario editing and tabletop import/export polish. Minimal manually-authored maps must be supported much earlier so campaign work can progress without AI import.

Priorities may be reprioritized after testing, but **do not implement dungeon reconstruction**, and do not add grand-campaign scope before playable tactical AI is demonstrated.

## 12. Gate 0 explicit acceptance criteria
1. Godot .NET project can be opened and started on Windows using documented steps.
2. C# simulation project compiles and runs independently of rendering.
3. A terrain scene shows a navigable plain, ridge, woods, and valley.
4. Player can switch among strategic isometric, free 3D, and overhead command views; all render one consistent scene.
5. At least one formation is visible and selectable; right-click or clearly documented command issues movement.
6. Formation moves as intended under a fixed simulation tick and stops at destination.
7. While the simulation is running, the player can issue another movement order and observe an updated destination.
8. Pause freezes simulation; orders can be queued/updated while paused and execute only on resume.
9. Same seed and same scheduled inputs produce equivalent simulation outcomes regardless of rendering frame rate.
10. Automated tests exist for formation movement, time progression, pause semantics and deterministic update.
11. README describes launch/build/test commands, implemented features and limitations.
12. No nonworking decorative combat controls or unverified 'complete' claims.

## 13. Gate 1 acceptance scenarios (future)
- Units behind ridge/woods cannot be targeted as known enemies without correct reconnaissance/visibility.
- First arrow fire can reveal concealed archers under documented spotting rules.
- Cavalry assesses a frontal assault on steady spearmen as unfavorable without explicit suicide orders.
- Movement through dense woods changes movement/cohesion and ranged visibility.
- A planned withdrawal does not automatically count as a rout; a low-discipline unit may lose cohesion.
- A reserve formation responds to a legitimate observed flank threat.
- AI updates operational strategy when its initial plan becomes untenable; record what observation triggered revision.
- Human player can completely replace initial ambush orders during battle.
- No AI path reads actual enemy state that its perception model does not expose.
- Test outcomes and AI decisions are reproducible under controlled seed and input ordering.

## 14. Long-term campaign and map acceptance scenarios
- Standalone battle starts without campaign.
- A multi-army campaign can run in either continuous or turn-based mode with one common simulation.
- Simultaneous movement, supply and AI actions resolve under timestamped event rules.
- With equivalent order timing and seed, campaign outcomes match across time modes.
- A battle pauses campaign correctly and applies results exactly once.
- Save/load works during campaigns, battle interruptions and unfinished turns.
- Imported map features are editable before 3D terrain generation.
- User can set ridge elevation or forest density during generation; edits change gameplay as expected.
- User modifications persist through localized regeneration.
- Regional maps can produce local tactical battlegrounds without inventing claimed source-map precision.
- Tabletop battle and war results export as versioned JSON plus readable summary.

## 15. Development protocol for Codex
1. First inspect repo and tooling; verify actual installed Godot/.NET versions rather than assuming local environment.
2. Create/update `AGENTS.md` and maintain docs of architecture, contracts, current status, test plan and roadmap.
3. Deliver **small, runnable, testable increments**. One milestone at a time.
4. Before implementation, explain the specific milestone's dependency impact and which files will be changed.
5. Ensure project compiles and core tests pass after each substantive change; distinguish tests run from tests unavailable in environment.
6. Never report functionality as implemented unless code is present and demonstrably exercised.
7. Prefer simple visual placeholders initially, with legible formations and terrain. Polished assets come later.
8. Prevent regression across UI, simulations, scenes, saved formats and tests.
9. Commit logical work to version control where repository permissions allow; don't claim commits if not possible.
10. Avoid adding third-party dependencies without specific benefit and license review. Never require paid APIs for core play.
11. Do not change approved requirements silently; propose tradeoffs and document deviations.
12. Keep campaign/map/rules adapters modular, not embedded inside battle scene nodes.
13. Preserve player overrides, imported map source provenance, and pending orders across relevant saves.
14. At each handoff, provide exact run/build/test instructions, files changed, tests passed/failed/not run, screenshots if available, known limitations, and proposed next milestone.
15. Make no modifications to any unrelated project or repository, including O&G Workbench.

## 16. First instruction to execute now
Initialize **FantasyWarfare**, implement Gate 0 only, and create a functioning minimal Windows-oriented Godot C# prototype with a headless formation simulation and tests. Provide working camera switching, one controllable formation, move/reissue while running, pause and issue while paused, simple navigable terrain, and project docs. Show what runs and what doesn't. **Do not** implement campaign, magic, sophisticated AI, map-import AI, dungeons, multiplayer, or a giant speculative framework during this assignment. Structure interfaces to permit those systems later without overengineering them today.
