# Rust Grind Galaxy Progression System

## Status

Vision ideas, partially settled. The decisions below are agreed direction but still revisitable; open questions are flagged at the end.

## Purpose

This document covers long-term player progression: how the player character and the ship get stronger, and how all six mineral families stay relevant to every build.

It complements `MODULE_SYSTEM.md`, which covers loot-driven module power. Module drops and refinement are one axis of progression. This document defines the systems that surround them.

## Core Principle: Every Mineral Needs A Purpose In Every Build

A player will often assemble a module and equipment loadout that only leans on one to three mineral families. The remaining families would feel like dead weight unless they serve those players too.

Design rule:

- every mineral must have a use that is independent of whether it appears on the player's equipped modules
- module refinement is not enough on its own; minerals need universal sinks that all players share

The player system upgrades defined below are the primary answer to this rule. The ship's industry (see `SHIP_SYSTEM.md`) adds more sinks on top.

## Two Upgrade Avenues

The player can invest in two things:

1. The player robot itself
2. The ship

This document focuses on the player robot. The ship is fully specified in its own document, `SHIP_SYSTEM.md`; its converter role is summarized below because the plasma pipeline spans both avenues.

## No XP, No Levels

Traditional RPG levels and XP do not fit this game. The player does not gain levels from killing enemies or completing missions.

Instead, the player upgrades by processing raw minerals into `Plasma`, then injecting that plasma into the player's own systems. Progression is literally fueled by what the player mines and where they mine it.

## Plasma And The Ship Converter

Raw minerals cannot be used directly. They must be processed first.

- the ship carries a `Plasma Converter` unit
- the player stores raw mineral stock on the ship
- the converter turns stored minerals into plasma over time, in the background
- the resulting plasma is the shared ingredient for all system upgrades

This makes the ship the processing heart of the loop:

1. Gather raw minerals on missions.
2. Return to the ship; minerals go into stock.
3. The converter slowly processes stock into plasma over time.
4. Spend plasma (plus common materials) on upgrades at the upgrade station.

Ship upgrades can increase converter processing speed, which makes the ship upgrade avenue directly accelerate player progression instead of competing with it.

## The Three Player Systems

The player has three main systems:

| System | Work Name | Upgrades |
| --- | --- | --- |
| Offensive | offense system | overall damage output |
| Defensive | defense system | overall toughness, damage taken from enemies |
| Core | core / utility system | module upgrade capacity and content gating |

### Offensive System

Straightforward: leveling this system increases the player's overall damage output across all tricks and attacks, regardless of module build.

### Defensive System

Leveling this system increases overall toughness, reducing how much damage the player takes from enemies.

### Core System

The core system is the structural one. It has two jobs:

1. Module upgrade capacity. Modules can only be upgraded up to a limit set by the core. Raising that limit requires upgrading the core system.
2. Content gate. Core level gates access to higher difficulty levels, planets, and star systems.

This makes the core the pacing backbone of the whole progression arc: it throttles both build depth (module limits) and world access (difficulty gating).

## Mineral Pairings Per System

Each system is upgraded using a combination of two processed minerals.

Current direction:

| System | Mineral Pair | Colors |
| --- | --- | --- |
| Offensive | `Cinder` + `Solar` | red + yellow |
| Defensive | `Verdant` + `Lumen` | green + white |
| Core | `Azure` + `Umbra` | blue + purple |

The pairs are disjoint and cover all six minerals, so all six have permanent universal demand no matter which modules the player is running. That satisfies the core principle above with a single mechanism.

The different-minerals-per-system rule also forces travel: a player pushing all three systems must visit Cinder/Solar, Verdant/Lumen, and Azure/Umbra worlds.

## Upgrade Injectors

Spending plasma on a system requires building an upgrade injector: plasma plus a common material.

| System | Plasma Pair | Common Material |
| --- | --- | --- |
| Offensive | `Cinder` + `Solar` | CPU chips |
| Defensive | `Verdant` + `Lumen` | tech scrap |
| Core | `Azure` + `Umbra` | catalysts |

Rules of thumb:

- player system upgrades never require rare components; rares stay for endgame or special content
- this is a simple one-recipe-per-upgrade gate, not a crafting game; injector crafting should be a menu action, not a minigame or deep recipe web

## Continuous Injection

Systems do not have discrete ranks. They grow from a continuous injected amount: each injector adds to the system's running total, and the stat effect scales smoothly with the amount injected. Displayed "levels" are just readable markers on top of that continuous value.

## Core Capacity Model

Core level provides a total upgrade capacity pool, divided across the player's module slots.

Example:

- core capacity: `1000`
- module slots: `5`
- per-module upgrade limit: `200`

So raising the core raises every module's ceiling at once, and the core is the only thing that unlocks deeper module investment.

Note: the current module doc defines four module types (`Ollie`, `Grind`, `Flip`, `Grab`). The five-slot direction (ollie, grind, plus three other tricks) needs to be reconciled with that; see open questions.

## Mineral Distribution Per Planet

Each planet has a mineral profile:

- one primary mineral that is abundant
- one secondary mineral that is noticeably less abundant
- occasionally one scarce mineral that can be found in small amounts

This maps directly onto the existing `PrimaryMineral` / `SecondaryMineral` pair on discovery records (see `LEVEL_DISCOVERY_PLAN.md` and `COLOR_PALETTE_SYSTEM.md`); the scarce third mineral is a new addition to that model.

## When Upgrades Happen

Upgrades happen between missions, on the ship. After extraction the player:

1. takes inventory of gathered materials and processed plasma
2. uses the upgrade station, a slot on the ship
3. picks which system(s) to invest in

No upgrading mid-mission. The ship is the calm, planning half of the loop.

## Relationship To The Module System

Modules already consume refined minerals through the per-property refinement loop in `MODULE_SYSTEM.md`. That loop is build-specific: it matters most when a mineral appears on your equipped modules.

The three player systems are the build-agnostic layer:

- module refinement: mineral spend that depends on your loot
- system injection: mineral spend that every player needs at every stage

Together they ensure no mineral is ever worthless.

## Ship Upgrades

Designed in `SHIP_SYSTEM.md`. Headline points that touch this document:

- the ship is the second upgrade avenue: every ship module is class-ranked `F` to `S+`
- the plasma converter is one of several ship modules; processing speed is its upgrade axis
- ship module costs follow the same economy: plasma plus common materials, with rare materials gating high classes
- engines gate sector travel, probes discover systems, and the ship's industry runs while the player is on missions

## Open Questions

1. Module slot count: the player vision says five trick modules (ollie, grind, + three others), but `MODULE_SYSTEM.md` currently defines four types (`Ollie`, `Grind`, `Flip`, `Grab`). Is the fifth slot the ultimate, a new module type, or does the split change?
2. Does the core capacity pool limit refinement investment per module, a separate module upgrade level, or both?
3. Where do CPU chips come from (drop source, which enemy or activity)?
4. What are the plasma conversion rates and raw-mineral-to-plasma ratios?
5. Should scarce third minerals have their own sink, or are they just a bonus find?
6. Does continuous injection mean injectors are single-use additive items, or one large stockpile spent directly?
7. Exact UI for the ship upgrade station: separate screens for converter, systems, and modules?

## Related Documents

- `SHIP_SYSTEM.md`: ship modules, class ranks, sectors, and the automated industry
- `MODULE_SYSTEM.md`: module rolls, refinement, and category derivation
- `MODULE_PROPERTY_POOLS.md`: per-mineral property catalog
- `GAME_PLAN.md`: overall game plan and core loop
- `COLOR_PALETTE_SYSTEM.md` and `LEVEL_DISCOVERY_PLAN.md`: per-mineral planet identity and discovery
