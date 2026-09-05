# Rust Grind Galaxy Ship System

## Status

Vision ideas. The ship's automated industry and defense loop are taking shape; the ship's own offensive system remains undecided.

## Purpose

The ship is the second upgrade avenue, alongside the player robot (see `PROGRESSION_SYSTEM.md`). It is the mobile base and the industrial backbone of the whole game loop: processing, scanning, mining, hauling, and interstellar travel all run through ship systems.

The player plays missions on foot while the ship works in the background. A large part of the game is managing that automated industry between missions.

## Class Rank System

Every upgradable ship module is ranked by class from `F` to `S+`:

`F, E, D, C, B, A, S, S+`

Higher classes unlock:

- harder sectors and systems (engines)
- richer or tougher resource sources (mining modules)
- better speed, capacity, and reliability across all modules

## Upgrade Cost

Ship modules upgrade on the same fuel economy as player systems:

- plasma (processed minerals) plus common materials at all classes
- rare materials enter as requirements at high classes (exact tier gated TBD, after bosses and rare loot are defined)
- stays a simple menu action at the ship, not a crafting game

Cost distribution is a deliberate balance tool: once the full set of buildable modules is defined, mineral costs get spread evenly across them so no module build favors one mineral pair over another, preserving universal demand for all six minerals (see the core principle in `PROGRESSION_SYSTEM.md`).

## Ship Defense And Raider Attacks

Random events and raiders attack the ship and its deployed assets while the player is out on a mission. The ship must be able to defend itself while the player is away.

### Threat Warning

The system scanner warns of a potential incoming attack. Attacks carry a threat level indicator:

| Threat | Meaning |
| --- | --- |
| Low | An attack is coming or underway, but ship/ODS defenses handle it. No player action needed. |
| Medium | The ship can likely handle it unless it is already damaged. Worth watching. |
| High | The player should return to the ship and take action. |

### The Three Options

Depending on the difficulty of the raid, when it hits the player has three options:

1. **Fight**: board the enemy ship and try to destroy its crew, likely including a boss encounter.
2. **Surrender**: pay some kind of fee to make them leave.
3. **Escape**: leave the system. The player keeps whatever resources are already loaded aboard, but forfeits every expedition and piece of mining equipment in the system; those are destroyed.

### Losing

If the ship itself is destroyed:

- all upgrades are preserved
- roughly 50% of the ship's resource stock is stolen
- the player must collect resources to repair the ship
- repair cost scales with the class rank of the ship

Deployed machines (AMES expedition ships, ORC transports, drills, collectors) can also be destroyed during system raids and must be rebuilt as replacements.

### Ship Offense

The ship's offensive system exists on the upgrade sheet, but its exact role is still undecided. Defense, escort, and raid response now have a concrete design; offense likely supports the same fight/repel moments.

## Upgradable Ship Modules

### Mineral Processing Unit

Converts stored raw minerals into plasma over time.

- processing runs continuously during gameplay, including while the player is off on missions
- upgrading improves processing speed
- first settled ship system; see the plasma section of `PROGRESSION_SYSTEM.md`

### System Scanner

Scans the current star system for sources of minerals and resources.

- upgrading improves scan speed and the likelihood of finding better, richer mineral or material sources
- source candidates include asteroid fields, barren planets, and abandoned stations or factories
- each discovered source lists a specific resource type and a rough abundance estimate
- the player then decides whether to send an expedition to retrieve the resource
- richer or harsher sources require a higher class mining ship (see AMES)
- also provides the incoming-raid warning used by the threat level system

### Engine

Enables travel to other systems and sectors.

- higher engine class is required to enter harder sectors
- the engine is the hard gate on the sector ladder (see Systems And Sectors)

### AMES - Automated Mining Expedition Ships

The ships the player sends out to discovered resource sources.

- expedition ships travel to a scanner-discovered source and bring the haul back
- upgrading improves speed, defensive capability, effectiveness, and other properties, or creates additional expedition ships
- each expedition ship has its own individual class on the `F`-`S+` ladder
- AMES class determines which sources can be worked; higher-class sources demand higher-class ships
- expedition ships can be destroyed by raids or hazards and must be rebuilt

### PMD - Planetary Mining Drill

A drill constructed on the ship and deployed to a planet the ship is orbiting.

- when the player finds a mineral source during a mission, they can call down a drill to set up a mining operation at that spot
- the drill collects resources at a rate set by drill class times the richness of the patch
- when the patch is depleted, the player must find another patch on a future mission and call down a new drill
- depletion pacing is a playtest target: it should feel rewarding, never like a chore
- this makes active exploration feed the passive industry: playing missions well literally seeds future automation

### ORC - Orbital Resource Collector

Retrieves mined minerals from a planet that has an active drill and makes them available to transport ships.

- transport ships do interplanetary travel to haul resources to the player
- enables a player who is off in another system to keep collecting minerals automatically
- upgrading improves collection speed, transport ship speed, and transport defensive capability
- transports moving between orbit and the player are natural targets for raids; escorts and ODS coverage reduce that risk
- transports can be destroyed and must be rebuilt

### ODS - Orbital Defense System

A buildable defense module that protects the assets in the current system: transport ships, expedition ships, and mining equipment.

- far more effective at system defense than the ship's own guns, so a defended system stops needing constant player attention
- the counterweight to the High-threat "return and fight" pressure: strong ODS coverage pushes threats down toward Low/Medium
- class-ranked like everything else; placement and coverage rules to be defined

### Probes

Discovers other star systems.

- the player builds and launches probes to search for new possible systems
- probes have a probability of finding nothing or being destroyed, and the loss is meant to sting enough to create tension
- launching a higher class probe increases the probability of finding something
- higher difficulty sectors require higher class probes and rarer build materials
- probes can be tuned with specific plasma to increase the chance of finding a planet rich in a specific mineral type

## Systems And Sectors

Two nested layers organize the galaxy:

- a star system has a combination of mineral types and resources, plus a richness rating; enemy difficulty is proportional to resource abundance, so richer systems are harder
- a sector is a difficulty tier; the sector ladder is the spine of long-term progression

Moving to the next sector requires upgrading and building higher class ship systems. The reward is higher resource counts and better gear in general.

## The Meta Loop

The ship systems together form the loop that drives the whole game:

1. Launch probes to discover neighboring star systems.
2. Enter a system (engine class gates the sector).
3. Scan the system for resource sources (system scanner).
4. Play missions on the best destinations; find patches and call down drills (PMD).
5. Send expeditions to system-level sources (AMES).
6. ORCs and transport ships haul background production to the player.
7. Convert raw minerals to plasma (processing unit).
8. Upgrade player systems, ship modules, and probe stock.
9. Push to the next sector.

Risk runs underneath the loop the whole time: richer systems draw harder enemies and more raids, and the threat level decides whether the player trusts their defenses or heads back to fight, pay, or run.

## Reconciliation With Existing Documents

`LEVEL_DISCOVERY_PLAN.md` implements the first-pass discovery slice with simple probe tiers (`Basic`, `Survey`, `Deep Scan`). Those are an implementation simplification of the `F`-`S+` class probes described here; the implementation plan is unchanged until the vision systems are actually built.

## Open Questions

1. What exactly is the ship's offensive system for, beyond raid defense support?
2. Does the ship itself carry a class rank for repair-cost scaling, or is that the combined class of its modules?
3. Surrender fee economy: what is paid, how much, and how does it scale with raid difficulty?
4. Raid trigger rules: what causes attacks (richness, time in system, stock size, sector tier)?
5. Fleet size caps for AMES ships, ORC transports, drills, and ODS units per system.
6. ODS placement model: one per system, a buildable network, or mobile units?
7. Deferred by design: per-module plasma pairings (set after the full module list exists, balanced so all minerals stay in demand) and rare-material class thresholds (set after bosses and rare drops are defined).

## Related Documents

- `PROGRESSION_SYSTEM.md`: player systems, plasma pipeline, injector costs
- `LEVEL_DISCOVERY_PLAN.md`: current implementation-stage discovery loop
- `GAME_PLAN.md`: overall game plan and core loop
