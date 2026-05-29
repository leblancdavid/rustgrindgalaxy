# Level Discovery And Generation Plan

## Goal

Add a lightweight galaxy discovery loop that lets the player:

- launch probes
- discover planets, derelict ships, and stations
- keep a replayable destination catalog
- launch missions from discovered destinations
- vary rewards and difficulty by destination properties

This should extend the current mission-based slice, not replace it with a large procedural system immediately.

## Why This Approach

The current project is still centered on a single handcrafted mission scene and in-memory mission state:

- `Main -> Game -> World`
- one playable level scene: `scenes/world/levels/LevelIndustrial01.tscn`
- no save system
- no mission selection UI
- no level metadata model
- no procedural level builder

Because of that, the correct next step is to build the discovery and catalog layer first, then add procedural generation underneath it in later passes.

## Core Model

Separate the system into three layers.

### 1. Probe

Represents how the player searches for new locations.

First-pass properties:

- `quality_tier`
- `discovery_bias`
- `difficulty_bias`
- `resource_bias`

For the first pass, probes should be simple quality tiers instead of consumable crafted items.

Suggested initial tiers:

- `Basic`
- `Survey`
- `Deep Scan`

### 2. Discovery

A persistent catalog record for a found destination.

Suggested fields:

- `id`
- `seed`
- `display_name`
- `destination_type`
- `environment_theme`
- `difficulty_tier`
- `resource_profile`
- `times_visited`
- `is_unlocked`

Destination types for the first pass:

- planet
- abandoned ship
- abandoned station

Environment themes for the first pass:

- industrial
- rocky
- frozen
- derelict

### 3. Mission Run

A generated playable run derived from one discovery.

Suggested fields:

- `discovery_id`
- `run_seed`
- `gravity_scale`
- `enemy_density`
- `hazard_density`
- `material_target`
- `resource_weights`
- `palette_key`
- `level_template_id`

This lets one discovery be replayed many times while still allowing per-run variation later.

## First Playable Target

The first implementation should prove the loop with the smallest possible scope.

### Player Flow

1. Start game.
2. See a simple mission terminal menu.
3. Launch a probe using one of three quality tiers.
4. Generate a new discovery and add it to the catalog.
5. Select a discovered destination.
6. Start a mission based on that destination.
7. Play the existing level with mission modifiers.
8. Complete or fail the mission.
9. Return to the menu and keep the destination in the catalog for replay.

### First-Pass Constraints

Keep these limits for the first pass:

- reuse `LevelIndustrial01` as the only playable level scene
- do not build chunk generation yet
- do not build full save-file robustness yet beyond one simple local data file
- do not build a physical hub scene yet

## Implementation Order

## Milestone 1: Persistent Game State

Goal: stop treating `World` as the only owner of important run state.

Add:

- `scripts/game/GameState.cs`
- `scripts/game/GameData.cs`
- `scripts/game/DiscoveryRecord.cs`
- `scripts/game/MissionRunData.cs`

Responsibilities:

- hold player meta-progression state
- hold discovered destinations
- hold currently selected mission
- save and load lightweight data from `user://`
- expose methods for generating discoveries and launching missions

Recommended shape:

- `GameState` as an autoload singleton
- `GameData` as the serializable container
- `DiscoveryRecord` and `MissionRunData` as plain data objects

Acceptance criteria:

- game launches with an initialized global state
- discovered destinations persist between launches
- an active mission can be created before loading gameplay

## Milestone 2: Mission Terminal UI

Goal: put a lightweight layer in front of gameplay.

Add:

- `scenes/game/MissionTerminal.tscn`
- `scripts/ui/MissionTerminal.cs`

Responsibilities:

- show probe buttons
- show discovered destinations
- show destination summary
- allow mission launch

UI content should include:

- probe quality buttons
- catalog list
- selected destination details
- launch mission button
- basic persistent resource summary if available

Recommendation:

Use a simple menu scene or overlay, not a physical hub environment.

Acceptance criteria:

- player can generate discoveries without entering the world scene
- player can inspect destinations and launch one
- returning from a mission lands back in the terminal

## Milestone 3: Discovery Generation

Goal: generate destinations with consistent identity and useful variation.

Add:

- `scripts/game/DiscoveryGenerator.cs`
- `scripts/game/ProbeTier.cs`
- `scripts/game/DestinationType.cs`
- `scripts/game/EnvironmentTheme.cs`

Generator inputs:

- probe tier
- RNG seed

Generator outputs:

- destination type
- theme
- difficulty tier
- resource profile
- display name
- base mission properties

Suggested first-pass rules:

- better probe tiers slightly increase rare themes and higher-value resource profiles
- better probe tiers can increase difficulty range
- all discoveries remain permanently replayable

Example generated names:

- Ashfall Extraction Moon
- Kestrel Drift Station
- Orphan Rig Delta
- Frostwake Cargo Hull

Acceptance criteria:

- probe launches create distinct discovery records
- discoveries have readable names and properties
- discoveries feel different even before procgen exists

## Milestone 4: Mission Parameter Injection Into World

Goal: make the current world scene respond to mission data instead of fixed values only.

Modify:

- `scripts/world/World.cs`
- `scripts/ui/Hud.cs`
- `scripts/world/LevelIndustrial01.cs`

New responsibility split:

- `GameState` owns selected mission data before the run starts
- `World` reads active mission data on `_Ready()`
- `World` applies mission parameters to the current level and HUD

First-pass mission parameters to support:

- mission title
- material target
- gravity modifier
- mineral distribution
- enemy density scalar
- palette/theme label

First-pass implementation options:

1. adjust `MissionMaterialTarget`
2. tint background or accent colors by theme
3. reposition, enable, or disable a few enemy and pickup nodes based on seed
4. scale player or world gravity from mission data

Acceptance criteria:

- launching different discoveries changes mission setup
- HUD reflects destination identity and mission objective
- mission replay still works cleanly

## Milestone 5: Mission Results And Return Flow

Goal: replace reload-only behavior with loop-aware flow.

Add:

- `scripts/ui/MissionResults.cs`
- optional `scenes/ui/MissionResults.tscn`

Modify:

- `scripts/world/World.cs`
- `scripts/Main.cs`

Responsibilities:

- report success or failure
- transfer mission rewards to persistent game data
- return player to mission terminal
- preserve catalog and progression

Acceptance criteria:

- mission completion no longer only reloads the current scene
- rewards are saved to persistent state
- player returns to the terminal and can relaunch missions

## Milestone 6: Environment Profiles

Goal: make destination types mechanically readable.

Add:

- `scripts/game/EnvironmentProfile.cs`
- `scripts/game/EnvironmentCatalog.cs`

Each profile should define:

- display name
- palette key
- gravity range
- resource weighting
- enemy density range
- hazard density range
- supported level templates

Suggested first profiles:

- `IndustrialRefinery`
- `RockyMoon`
- `FrozenMoon`
- `DerelictShip`
- `AbandonedStation`

Use these profiles to avoid scattered hardcoded switches across `World.cs`.

## Milestone 7: Chunk-Based Level Assembly

Goal: begin real procedural level building without losing control of traversal quality.

Add:

- `scripts/world/LevelGenerator.cs`
- `scripts/world/LevelChunkMarker.cs`
- `scenes/world/chunks/...`

Recommended chunk categories:

- `Start`
- `Traversal`
- `Combat`
- `Branch`
- `Reward`
- `Extraction`

Recommended generator strategy:

1. pick a chunk sequence from simple rules
2. instantiate chunk scenes in order
3. align chunks via connection markers
4. place enemy and pickup spawn points from mission parameters
5. validate that a path from start to extraction exists

This is preferred over raw tile-level generation because it preserves authored platforming quality.

Acceptance criteria:

- levels assemble from multiple authored chunks
- generated layouts remain traversable
- environment type changes chunk pool selection or decoration

## Milestone 8: Tile And Theme Variation

Goal: vary visual identity within shared layout structures.

Add later:

- theme-specific tile palettes
- decoration sets
- background variants
- hazard prop sets

Strategy:

- keep chunk structure reusable
- swap visual sets by `EnvironmentTheme`
- keep collision and traversal layout stable where possible

This is where the project can support your idea of reusing tiles and section layouts with different styling and colors.

## File-By-File First Pass

These are the concrete changes to make first.

### New Files

- `scripts/game/GameState.cs`
- `scripts/game/GameData.cs`
- `scripts/game/DiscoveryRecord.cs`
- `scripts/game/MissionRunData.cs`
- `scripts/game/DiscoveryGenerator.cs`
- `scripts/game/ProbeTier.cs`
- `scripts/game/DestinationType.cs`
- `scripts/game/EnvironmentTheme.cs`
- `scripts/ui/MissionTerminal.cs`
- `scenes/game/MissionTerminal.tscn`

### Existing Files To Change

- `project.godot`
  - register `GameState` as autoload

- `scripts/Main.cs`
  - load mission terminal first instead of entering the world immediately

- `scenes/game/Game.tscn`
  - may become a scene container or may be replaced if unnecessary

- `scripts/world/World.cs`
  - read active mission data from `GameState`
  - stop assuming all important state is local-only
  - report mission result back to `GameState`

- `scripts/ui/Hud.cs`
  - show destination and mission metadata

- `scripts/world/LevelIndustrial01.cs`
  - accept mission parameters and perform simple per-run adjustments

## Data Guidance

Keep the first pass simple and code-first.

Recommended serialization format:

- JSON file under `user://savegame.json`

Recommended first-pass saved data:

- discovered destinations
- total recovered materials
- mission count stats

Do not save everything in the first pass.

Avoid adding:

- version migration system
- large content databases
- editor-authored resource catalogs

Those can come later if the discovery loop proves fun.

## Gameplay Rules For First Pass

Recommended initial behavior:

- discoveries are permanent once found
- discoveries are always replayable
- one mission scene supports all discoveries initially
- difficulty affects enemy pressure and material target first
- environment theme affects palette, label, and reward profile first
- probe quality affects discovery rarity and challenge range

This keeps the early system readable and testable.

## Risks And Controls

### Risk: Adding too much architecture too early

Control:

- keep state objects plain
- avoid deep inheritance
- avoid a full procedural framework in milestone 1

### Risk: Procgen creates weak traversal

Control:

- start with handcrafted level modifiers
- move to chunk assembly before tile-level generation

### Risk: `World.cs` becomes overloaded

Control:

- move persistent and discovery logic to `GameState`
- keep `World` focused on the active mission run

### Risk: Too many destination types before enough content exists

Control:

- support only a small initial set
- let types share the same level template with different modifiers

## Recommended Immediate Build Slice

Build this exact slice next:

1. Add `GameState` autoload and persistent JSON save.
2. Add `MissionTerminal` menu scene.
3. Add probe buttons for `Basic`, `Survey`, and `Deep Scan`.
4. Generate persistent discoveries for planet, station, and ship types.
5. Launch missions from catalog entries.
6. Reuse `LevelIndustrial01` for all missions.
7. Vary material target, gravity, theme label, and enemy or pickup density by mission data.
8. Return to the terminal after mission success or failure.

If this slice works, the project will have the correct foundation for:

- replayable destination discovery
- environment-based mission identity
- future chunk-based procedural generation
- later tile and palette swapping

## Out Of Scope For This Pass

Do not include these in the next implementation step unless they become necessary:

- full procedural tile generation
- consumable probe crafting economy
- large biome library
- advanced mission chains
- complex hub NPC interactions
- multiple handcrafted full-size levels

## Success Criteria

This plan is successful when:

- the player can discover and retain destinations
- destinations feel different before full procgen exists
- replay is routed through a catalog instead of immediate scene reload only
- mission state is cleanly separated from persistent meta-state
- the codebase is ready for chunk-based generation later
