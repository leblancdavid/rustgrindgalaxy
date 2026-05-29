# Rust Grind Galaxy Vertical Slice

## Goal

Build a compact first playable slice that proves the game's core promise:

- movement feels good
- traversal and combat support each other
- the robot's trick loadout changes how the player moves and fights
- a mission can be entered, completed, failed, and replayed

This slice should validate feel and structure, not full content scale.

## Slice Definition

The first vertical slice is one handcrafted industrial mission with:

- one playable robot character
- one industrial level built around traversal lines
- one raider enemy
- one drone enemy
- health, damage, death, and restart flow
- material pickups
- one simple extraction or mission clear condition
- one upgrade decision between missions

## What This Slice Must Prove

### 1. Movement Is Fun On Its Own

Even without enemies, the player should enjoy moving through the level. Running, jumping, ollie timing, landing, and rail usage must already feel satisfying.

### 2. Traversal Improves Combat

The player should gain combat advantage by moving well. A clean line through the level should create safer, stronger, or faster ways to engage enemies.

### 3. One Loadout Choice Matters

The slice only needs one clear example of a module changing gameplay, but that example must be obvious and meaningful.

### 4. Mission Structure Works

The player should be able to start a mission, gather materials, survive combat, complete the objective, return to a hub flow, make one upgrade choice, and replay.

## Scope Boundaries

### In Scope

- one mission
- one biome theme: industrial refinery or corporate facility
- one player scene and controller
- one basic rail grind system
- one offensive trick
- one defensive or utility trick
- one module slot variation that changes trick behavior
- one pickup material type, with room to add more later
- one simple hub or between-mission upgrade interface
- one mission end state for success and one for failure

### Explicitly Out Of Scope

- procedural planet generation
- multiple biomes
- large loot tables
- narrative cutscene production
- multiple bosses
- large skill trees
- complex NPC hub interactions
- deep crafting taxonomy
- advanced economy balancing

## Current Repo Starting Point

The repository already has early placeholder structure for the slice:

- `scenes/player/Player.tscn`
- `scenes/enemies/Raider.tscn`
- `scenes/enemies/Drone.tscn`
- `scenes/world/levels/LevelIndustrial01.tscn`
- `scripts/player/PlayerController.cs`
- `scripts/enemies/RaiderEnemy.cs`
- `scripts/enemies/DroneEnemy.cs`
- `scripts/world/LevelIndustrial01.cs`

The current controller only supports basic left-right movement and jump. The rest of the slice can be built incrementally from this baseline.

## Slice Pillars

### Movement First

Movement quality comes before combat depth. If movement feels weak, the rest of the slice will not represent the real game.

### Readable Interactions

The player should understand what happened when a trick, pickup, hit, or rail interaction occurs. Early systems should favor clarity over density.

### Small But Complete Loop

A short mission with a real end state is more valuable than many disconnected mechanics.

## Target Player Flow

1. Launch into `LevelIndustrial01`.
2. Move through the industrial space using run, jump, and ollie.
3. Enter and exit at least a few grind rails.
4. Use one offensive trick and one defensive or utility trick.
5. Fight raider and drone enemies placed along traversal lines.
6. Collect materials during the run.
7. Reach extraction or fulfill a clear mission objective.
8. See mission results.
9. Spend materials on one upgrade or module choice.
10. Replay the mission with a changed loadout or stronger stats.

## Feature Breakdown

### Player Controller

Required:

- horizontal movement
- jump
- ollie input behavior
- improved landing feel and air control
- health and damage response
- death handling

Nice to have if cheap:

- coyote time
- jump buffering
- short landing recovery feedback
- momentum tuning variables exposed in inspector

Acceptance criteria:

- player movement feels responsive with keyboard or controller input
- jump and ollie are distinct enough to support future trick logic
- player can recover from small mistakes without constant frustration

### Rail Grind System

Required:

- rail areas or paths the player can enter consistently
- locked movement along a rail path
- clear exit behavior
- one grind-related gameplay benefit

Nice to have if cheap:

- entry assist when close to a valid rail
- sparks, audio, or camera feedback

Acceptance criteria:

- player can intentionally enter rails without unreliable collision gimmicks
- exiting rails feels predictable
- rails create meaningful fast routes through the level

### Trick System

Required:

- `A` behavior supporting ollie on ground and grab in air
- one `X` flip trick or equivalent offensive trick
- one `B` or `Y` utility, defensive, or grind trick
- trick cooldown or gating only if needed for readability

Acceptance criteria:

- at least one trick can hit enemies
- at least one trick supports traversal or defense
- the player can understand which button triggered which outcome

### Module Variation

Required:

- one module slot that changes one trick's behavior
- one upgrade or swap decision after a mission

Example minimum implementation:

- base module: normal ollie with small shock landing
- alternate module: lower ollie but grants armor on grind entry

Acceptance criteria:

- the player can notice the difference during the next mission attempt
- the changed behavior affects movement or combat, not just a hidden number

### Enemy Set

Required:

- raider ground threat
- drone aerial threat
- contact or attack damage
- player attacks or trick effects can defeat them

Acceptance criteria:

- raiders and drones pressure the player in different ways
- at least one encounter rewards use of movement lines instead of standing still

### Materials And Rewards

Required:

- one pickup material type placed in the level
- one mission reward summary
- one upgrade spend between missions

Acceptance criteria:

- materials are visible, collectable, and counted
- the player understands what was gained and what it can buy

### Mission Flow

Required:

- mission start
- simple objective or extraction endpoint
- success state
- failure state on death
- restart or return flow

Possible objective options:

- reach extraction point
- destroy a target count
- collect a required salvage amount

Recommendation:

Use extraction or salvage quota first. It is easiest to implement and fits the scavenger fantasy.

Acceptance criteria:

- the player can clearly win or lose
- failure recovery is fast enough to encourage replay

### Hub Or Between-Mission Screen

Required:

- simple results presentation
- one upgrade or module choice
- relaunch mission flow

Recommendation:

Use a lightweight menu or overlay first instead of a full physical hub scene.

Acceptance criteria:

- the upgrade loop is visible even if minimal
- replaying the mission is fast

## Recommended Implementation Order

1. Tighten `PlayerController.cs` movement feel.
2. Add player health, death, and restart.
3. Build first-pass rail grind interaction in `LevelIndustrial01` and player logic.
4. Add one raider and one drone behavior simple enough to support encounters.
5. Add one offensive trick and one defensive or utility trick.
6. Add one collectible material type.
7. Add mission objective and success flow.
8. Add post-mission result screen and one upgrade decision.
9. Tune the level around actual movement lines and enemy placement.

## Recommended Scene And Script Additions

These are likely useful, but should only be added when needed:

- `scripts/player/PlayerHealth.cs` or keep health in `PlayerController.cs`
- `scripts/world/GrindRail.cs`
- `scripts/world/MissionExit.cs`
- `scripts/pickups/MaterialPickup.cs`
- `scripts/ui/MissionResults.cs`
- `scripts/game/GameState.cs`

Keep the number of new files small. If a system can remain clear in one script during the slice, keep it there.

## Level Design Goals For `LevelIndustrial01`

The level should be a short industrial route with:

- a safe opening space for movement testing
- a first easy rail segment
- one low-pressure combat setup
- one traversal-combat mixed section
- one short route choice or optional pickup detour
- one extraction endpoint

The level does not need to be large. It only needs enough space to prove a movement line and one replay-driven module decision.

## Art And Presentation Standard

For the slice, presentation only needs to be readable enough to judge gameplay.

Priorities:

- clear collision and silhouettes
- readable pickups and hazards
- enemy readability
- hit feedback
- basic UI for health, materials, and mission result

Polish is welcome, but should not slow down the core loop.

## Success Metrics

The slice is successful if:

- movement feels fun before content scale exists
- rails are worth using
- at least one module choice changes how the mission is played
- raider and drone encounters feel different
- the mission can be completed in a short play session
- the player wants to immediately replay with a better route or different module

## Failure Signals

The slice needs revision if:

- movement only feels acceptable when combat is absent
- rails are gimmicks instead of meaningful routes
- tricks feel like separate attacks instead of movement-driven actions
- materials and upgrades do not affect the next run in a visible way
- the mission is too long for iteration
- the player spends more time in menus than moving

## Concrete Milestones

### Milestone 1: Playable Movement Box

Deliver:

- responsive run and jump
- ollie behavior stub
- death and restart loop
- rough industrial test space

### Milestone 2: Traversal Slice

Deliver:

- grind rails
- one simple route through the level
- one pickup type

### Milestone 3: Combat Slice

Deliver:

- raider enemy
- drone enemy
- one offensive trick
- one defensive or utility trick

### Milestone 4: Mission Loop Slice

Deliver:

- objective or extraction completion
- mission success and failure results
- material payout summary
- one upgrade or module swap

### Milestone 5: Tuning Pass

Deliver:

- cleaner pacing
- improved encounter placement
- clearer rail use cases
- stronger reason to replay

## Open Decisions

These should be answered soon because they shape implementation:

1. Should mission success require extraction, or can the player keep all materials on death?
2. Is the first module decision a module swap, a stat upgrade, or a trick unlock?
3. Should grind entry be automatic when conditions are met, or require explicit `Y` input?
4. Should the first utility trick be defensive, mobility-focused, or pickup-focused?
5. Does the slice need a visible ultimate meter now, or can that wait until after the core loop works?
