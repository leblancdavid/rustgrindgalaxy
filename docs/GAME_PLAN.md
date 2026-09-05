# Rust Grind Galaxy Game Plan

## High Concept

Rust Grind Galaxy is a 2D platform action RPG where the player is a robot fugitive escaping a hostile galactic corporation. The game mixes fast traversal, movement-driven combat, mission-based progression, and gear upgrades built from scavenged materials.

The player moves through planets and space stations using an integrated skate-like movement system. Movement abilities are not separate from combat. Ollies, grinds, flips, grabs, and special tricks are the main tools for traversing levels, surviving hostile encounters, and building momentum.

## Player Fantasy

The player should feel like:
- a salvaged robot built to survive under pressure
- fast, expressive, and stylish in motion
- stronger because of mastery of movement, not only raw stats
- constantly improving through scavenging, refinement, and module upgrades
- hunted by a powerful corporation but capable of outmaneuvering it

## Core Pillars

### 1. Movement-Driven Combat

Movement is the identity of the game. Traversal mechanics should directly support combat and survival. The player should be rewarded for chaining motion cleanly instead of stopping to fight in place.

### 2. Trick-As-Equipment Progression

Tricks come from equipped robot subsystems. The player builds a loadout by selecting modules that change what each input does and how those tricks behave.

### 3. Mission-Based Scavenging Loop

The game is structured around missions. The player deploys to a planet or station, completes objectives, gathers materials and modules, then returns to a hub to refine resources and upgrade the loadout.

### 4. Planetary Variation

Each destination should feel mechanically distinct through environmental properties, material tables, enemy mixes, and traversal demands.

### 5. Boss-Driven Escape Arc

Long-term progression is driven by major bosses and corporate control points. The player is trying to survive, get stronger, and eventually break through the corporation's hold.

## Core Gameplay Loop

1. Accept a mission to a planet or station.
2. Enter the level with the current module loadout.
3. Traverse the environment using movement and trick abilities.
4. Fight enemies while collecting resources, parts, and upgrades.
5. Complete the objective and extract.
6. Return to the hub.
7. Refine materials, improve modules, and prepare for the next mission.

## Control And Trick Structure

The control scheme should stay readable and loadout-driven.

- `A`: ollie on the ground, grab trick while airborne
- `B`: secondary equipped trick
- `X`: flip trick
- `Y`: grind action or grind-specific trick
- `Ultimate`: high-impact trick that becomes available when a meter is full

The important design rule is that the input layout stays stable while the behavior changes based on equipped modules.

## Player Movement Model

### Base Movement

- move left and right
- jump and ollie
- air control
- land and preserve momentum
- take damage, die, and restart or fail the mission

### Advanced Traversal

- rail grinding
- clean rail entry and exit behavior
- chaining jumps into grinds into tricks
- momentum-preserving traversal lines

The movement model should prioritize flow and readability before adding complexity such as strict balance management or highly technical trick inputs.

## Trick Categories

### A Slot: Mobility Core

The `A` slot is always the most fundamental traversal action. On ground it is the ollie. In air it becomes a grab-style action. This slot defines the player's baseline feel.

### B Slot: Secondary Trick

The `B` slot is a flexible slot for utility, offense, or defense. It can support varied build identities without overwhelming the basic controls.

### X Slot: Flip Trick

The `X` slot is intended for aerial flip actions. These should emphasize attack, momentum conversion, or airborne positioning.

### Y Slot: Grind Trick

The `Y` slot is dedicated to grind interaction. This can cover entering rails, enhancing rail travel, or triggering effects while grinding.

### Ultimate Slot

The ultimate is a high-impact signature ability that charges over time through skilled play.

## Ultimate Meter Direction

The ultimate meter should reward active, stylish play. Good charge sources include:

- landing tricks cleanly
- chaining movement without breaking flow
- damaging enemies with trick effects
- spending time on rails successfully
- defeating enemies during combo sequences

Ultimate abilities should be dramatic and strongly tied to movement. Examples:

- magnetic overdrive that shocks nearby enemies and pulls pickups in
- a rail blitz burst that carries the player through enemies
- a heavy ollie slam that creates a large shockwave on landing
- a temporary defensive overclock with high mobility and contact damage

## Module Model

The movement rig is integrated into the robot rather than treated as a separate skateboard item. Modules should feel like robot subsystems.

Example subsystem categories:

- locomotion core
- aerial stabilizer
- flip actuator
- grind magnet assembly
- combat relay
- overdrive module

Each subsystem can provide:

- one trick behavior or input modifier
- one passive bonus
- a small set of randomized properties

## Randomized Properties

Randomization should stay controlled and readable. Modules should roll a limited set of meaningful modifiers instead of large, noisy stat pools.

Example property types:

- increased ollie height
- longer grind sustain
- bonus speed after landing a trick
- landing shock damage
- reduced airborne damage taken
- armor on rail entry
- pickup magnet radius
- arc damage chance on trick hit
- extra airtime during grab tricks

## Resource And Upgrade Loop

### Resource Types

- raw minerals gathered from planets
- tech scrap gathered from stations and defeated machines
- rare components from elites and bosses
- catalysts or energy cores for advanced upgrades

### Refinement Loop

1. Collect raw resources during missions.
2. Return to hub after extraction.
3. Refine resources into usable upgrade materials.
4. Spend refined materials on improving modules and tricks.

### Upgrade Goals

Upgrades should improve one or more of the following:

- traversal quality
- trick power
- survivability
- loadout specialization

### Player And Ship Systems (Vision)

Long-term progression is captured in `PROGRESSION_SYSTEM.md` and `SHIP_SYSTEM.md`. Current direction:

- no XP or character levels; the player upgrades three internal systems (offense, defense, core) by injecting plasma processed from mineral pairs
- raw minerals must be converted to plasma by the ship's converter; ship upgrades can speed up processing
- the core system provides the module upgrade capacity pool and gates access to higher difficulty content
- every mineral must have a use independent of module builds, so all six stay valuable to every player
- upgrade cost stays deliberately simple: plasma plus one common material per system, no rare components, this is not a crafting game
- the ship is the industrial backbone: class-ranked modules (`F` to `S+`) for processing, scanning, engines, automated mining (expedition ships, planetary drills, orbital collectors), and probes
- star systems carry mineral mixes with a richness rating, enemy difficulty scales with abundance, and sectors are the difficulty tiers gated by engine class; pushing the sector ladder with the ship is the meta game loop (see `SHIP_SYSTEM.md`)
- raiders can attack the ship and its automated assets while the player is on a mission; the scanner feeds a low/medium/high threat indicator, and the player chooses to fight, surrender, or escape (see `SHIP_SYSTEM.md`)

## Mission Structure

The game should use a mission-based format rather than a continuous world for the early product.

Benefits:

- easier to scope and tune
- easier to create strong traversal routes
- cleaner progression pacing
- clearer material and reward structure
- simpler boss gating

Early mission type candidates:

- salvage run
- target elimination
- data theft and extraction
- refinery sabotage
- boss pursuit

## World Structure

The galaxy is controlled by a corporation that exploits planets, refineries, and orbital infrastructure. The player is a rogue robot asset trying to survive corporate pursuit, gather strength, and eventually escape.

Destinations should include both natural planets and manufactured corporate stations. Each mission location should combine traversal identity, enemy pressure, and material rewards.

## Planet And Station Property Framework

Rather than building fully procedural planets first, the initial direction should be handcrafted missions with randomized or selected mission properties layered on top.

Useful property axes:

- gravity level
- rail density
- hazard density
- atmosphere or air drag
- temperature or elemental threat
- enemy faction mix
- material abundance
- storm or interference conditions
- magnetism or rail behavior modifiers

Examples:

- low gravity: easier airtime, harder landing control
- high magnetism: stronger rail interactions and altered routing
- corrosive atmosphere: passive pressure without protection
- unstable industrial platforms: timing-heavy traversal
- signal storms: more drone pressure and targeting disruption

## Materials By Region

Material rewards should reinforce location identity. Players should quickly learn where to go for specific upgrade needs.

Example material families:

- ferrous scrap
- crystal lattice
- volatile gas cells
- ceramic plating
- magnetic alloys
- corrupted cores

## Enemy Structure

### Raider Role

Raiders are mobile humanoid scavengers or corporate mercenaries that apply ground pressure and punish predictable approaches.

### Drone Role

Drones provide ranged harassment, aerial denial, and pressure from awkward angles.

### Future Role Space

Additional enemy roles can include:

- heavy security units
- shielded support enemies
- environmental hazard machines
- biome-specific corrupted constructs

Encounters should test movement decisions, not just damage output. Enemies should challenge the player's line choice, timing, landing safety, and rail usage.

## Boss Structure

Bosses are major corporate enforcers, experimental war machines, or regional control points. They function as progression gates and major milestones in the escape arc.

Each boss should test a different player skill set, such as:

- momentum maintenance
- aerial control
- grind usage
- reaction timing
- trick build specialization

## First Vertical Slice Scope

The first playable slice should prove the core feel, not the full game.

The slice should include:

- one industrial mission level
- one player robot with satisfying move and jump feel
- ollie and rail grinding
- one offensive trick and one defensive or utility trick
- one module-driven trick slot variation
- one raider enemy
- one drone enemy
- collectible materials
- a simple refine and upgrade loop
- death, mission failure, and restart flow

## Development Priorities

1. Tighten movement, ollie, jump, landing, and momentum feel.
2. Add reliable rail grind detection and traversal.
3. Add one offensive trick and one defensive or utility trick.
4. Add a simple module slot that changes trick behavior.
5. Add collectible materials and a basic end-of-mission reward flow.
6. Add a first-pass refine and upgrade screen or station.
7. Add mission modifiers to vary the industrial test mission.
8. Expand enemy behavior around movement-driven combat.

## Open Questions For Next Pass

1. Should missions use strict timers, soft extraction pressure, or no major time pressure?
2. Should modules be mostly found loot, mostly crafted upgrades, or a hybrid of both?
3. How forgiving should rail grinding be in the first playable slice?
4. Should the hub be a physical scene or a simpler menu-based upgrade layer at first?
5. What are the first three mission destinations and what unique materials should each provide?
6. What non-module sinks guarantee every mineral stays useful for every build (see `PROGRESSION_SYSTEM.md`)?
