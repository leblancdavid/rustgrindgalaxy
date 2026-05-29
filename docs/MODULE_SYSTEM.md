# Rust Grind Galaxy Module System

## Purpose

Modules are the player's core progression system. They define the robot's trick behaviors, shape build identity, and connect loot progression to mineral refinement.

The module system should create three kinds of decisions:

- which module rolls are worth keeping
- which minerals are worth investing in
- how different module types synergize across a full loadout

## Module Types

There are four module types:

- `Ollie`
- `Grind`
- `Flip`
- `Grab`

Each module type maps to a core player action family.

- `Ollie`: ground launch, jump expression, landing effects
- `Grind`: rail entry, rail travel, rail exit, grind benefits
- `Flip`: airborne offense, momentum conversion, aggressive trick effects
- `Grab`: air control, defense, sustain, utility, stability

The player's build is defined by the combination of these four equipped modules.

## Core Module Structure

Each module contains:

- module type
- rarity
- a set of rolled properties
- one mineral affinity per property
- refinement progress tracked per mineral
- a derived category based on the module's active mineral profile

Modules are not handcrafted around fixed mineral identities. Minerals roll randomly on most modules. The player is expected to hunt for modules whose mineral makeup supports their preferred build.

## Rarity Structure

Rarity determines how many properties a module can have.

| Rarity | UI Color | Property Count |
| --- | --- | --- |
| Common | White | 1 |
| Uncommon | Blue | 2 |
| Rare | Yellow | 3 |
| Epic | Purple | 4 |
| Legendary | Orange | 5 |
| Unique | Gold | 6 |

### Rarity Notes

- higher rarity means more properties, not guaranteed stronger individual rolls
- more properties also means more chances for good or awkward mineral combinations
- duplicate mineral rolls are allowed on normal modules
- unique modules are a special case and do not roll duplicate minerals

## Mineral Families

The module system uses six mineral families.

| Color | Mineral Name | Identity |
| --- | --- | --- |
| Red | Cinder | impact, damage, aggression, slam force |
| Green | Verdant | repair, sustain, recovery, salvage utility |
| Blue | Azure | airtime, jump height, air control, energy flow |
| Yellow | Solar | speed, momentum, charge gain, responsiveness |
| White | Lumen | shields, armor, stability, precision |
| Black | Umbra | risk-reward, siphon, corruption, debuffs |

These names should be used in design and gameplay text. UI can still represent them with icons and color coding for readability.

## Property Roll Rules

Each rolled property is associated with exactly one mineral family.

Examples:

- `Launch Height` -> `Azure`
- `Landing Shockwave` -> `Cinder`
- `Repair On Trick Finish` -> `Verdant`
- `Combo Meter Gain` -> `Solar`
- `Armor On Rail Entry` -> `Lumen`
- `Damage Siphon` -> `Umbra`

Properties on a module are randomly rolled, including their mineral distribution. This means a module can roll repeated minerals.

Example roll patterns:

- `Cinder`
- `Azure`, `Solar`
- `Cinder`, `Cinder`, `Lumen`
- `Cinder`, `Cinder`, `Solar`, `Verdant`
- `Azure`, `Lumen`, `Lumen`, `Solar`, `Umbra`

Duplicate minerals are important because they let a module naturally lean toward a stronger category identity even before refinement investment.

## Unique Module Rule

`Unique` modules are a special rarity tier.

Rules:

- always 6 properties
- exactly one property for each mineral family
- no duplicate minerals
- always use the special six-mineral category

Recommended special category name:

- `Prismatic`

This category is reserved for modules containing all six mineral families.

## Refinement Rules

Every property is improved by refining its matching mineral.

Examples:

- improve an `Azure` property by refining `Azure`
- improve a `Cinder` property by refining `Cinder`
- improve a `Lumen` property by refining `Lumen`

Refinement should be tracked at the module level by mineral investment total.

Example:

- a module with two `Cinder` properties benefits when `Cinder` is refined
- a module with one `Solar` property and one `Azure` property can be improved through either mineral, depending on player priorities

This creates two useful behaviors:

- duplicate mineral modules reward focused investment
- mixed mineral modules reward broader investment strategies

## Category Derivation Rules

Most modules derive their category from the top three minerals with the highest total refinement investment on that module.

### Category Resolution

1. Sum total refined investment per mineral on the module.
2. Sort minerals by total investment.
3. Take the top three minerals.
4. Resolve the displayed category from those top three.

### Duplicate Handling

Duplicate minerals do not create separate category entries. They increase the strength of that mineral's claim on the category.

Example:

- property minerals: `Cinder`, `Cinder`, `Solar`, `Verdant`
- if `Cinder` receives the most refinement, it counts once as a dominant category mineral, not twice
- resulting category could be `Cinder-Solar-Verdant Matrix`

### Modules With More Than Three Minerals

Epic and Legendary modules may contain four or five minerals. Their category still uses only the top three by refinement investment.

Lower-priority minerals still matter for property improvement. They are simply not part of the category label.

### Tie Breakers

If two minerals have the same total refinement investment, resolve order by:

1. higher number of properties using that mineral
2. higher total base property roll magnitude
3. fixed mineral order for stability: `Cinder`, `Verdant`, `Azure`, `Solar`, `Lumen`, `Umbra`

This prevents category labels from changing unpredictably.

## Category Naming Structure

To keep the system readable and scalable, use a consistent naming pattern.

### One Dominant Mineral

- `Pure Cinder`
- `Pure Verdant`
- `Pure Azure`
- `Pure Solar`
- `Pure Lumen`
- `Pure Umbra`

### Two Dominant Minerals

- `[Mineral]-[Mineral] Alloy`

Examples:

- `Cinder-Azure Alloy`
- `Solar-Lumen Alloy`
- `Verdant-Umbra Alloy`

### Three Dominant Minerals

- `[Mineral]-[Mineral]-[Mineral] Matrix`

Examples:

- `Cinder-Solar-Lumen Matrix`
- `Azure-Verdant-Umbra Matrix`
- `Cinder-Azure-Solar Matrix`

### All Six Minerals

- `Prismatic`

Reserved for unique modules only.

## Full Category List

### Pure Categories

- `Pure Cinder`
- `Pure Verdant`
- `Pure Azure`
- `Pure Solar`
- `Pure Lumen`
- `Pure Umbra`

### Dual Categories

- `Cinder-Verdant Alloy`
- `Cinder-Azure Alloy`
- `Cinder-Solar Alloy`
- `Cinder-Lumen Alloy`
- `Cinder-Umbra Alloy`
- `Verdant-Azure Alloy`
- `Verdant-Solar Alloy`
- `Verdant-Lumen Alloy`
- `Verdant-Umbra Alloy`
- `Azure-Solar Alloy`
- `Azure-Lumen Alloy`
- `Azure-Umbra Alloy`
- `Solar-Lumen Alloy`
- `Solar-Umbra Alloy`
- `Lumen-Umbra Alloy`

### Tri Categories

- `Cinder-Verdant-Azure Matrix`
- `Cinder-Verdant-Solar Matrix`
- `Cinder-Verdant-Lumen Matrix`
- `Cinder-Verdant-Umbra Matrix`
- `Cinder-Azure-Solar Matrix`
- `Cinder-Azure-Lumen Matrix`
- `Cinder-Azure-Umbra Matrix`
- `Cinder-Solar-Lumen Matrix`
- `Cinder-Solar-Umbra Matrix`
- `Cinder-Lumen-Umbra Matrix`
- `Verdant-Azure-Solar Matrix`
- `Verdant-Azure-Lumen Matrix`
- `Verdant-Azure-Umbra Matrix`
- `Verdant-Solar-Lumen Matrix`
- `Verdant-Solar-Umbra Matrix`
- `Verdant-Lumen-Umbra Matrix`
- `Azure-Solar-Lumen Matrix`
- `Azure-Solar-Umbra Matrix`
- `Azure-Lumen-Umbra Matrix`
- `Solar-Lumen-Umbra Matrix`

### Special Category

- `Prismatic`

## Category Meaning

Categories should have gameplay identity, not just naming identity.

- `Pure`: high specialization, strongest narrow identity
- `Alloy`: two-mineral hybrid synergy
- `Matrix`: flexible three-mineral blend
- `Prismatic`: broad all-mineral identity with unique design space

This supports build tradeoffs:

- pure modules are easier to optimize around one mineral plan
- alloy modules support focused hybrid builds
- matrix modules support flexible cross-system builds
- prismatic modules support rare all-rounder or special-effect designs

## Module Type Synergy Direction

Minerals roll randomly, but each module type should still have natural synergy tendencies.

### Ollie Module Synergies

Best natural fits:

- `Azure`: launch height, hang time, air steering
- `Solar`: momentum burst, charge gain, fast recovery
- `Cinder`: landing impact, shockwave, aggressive launch

Secondary fits:

- `Lumen`: safer landings, brief armor on launch
- `Verdant`: sustain on successful landings
- `Umbra`: high-risk impact conversion

### Grind Module Synergies

Best natural fits:

- `Solar`: rail speed, combo gain, meter gain
- `Lumen`: stability, armor, safe entry or exit
- `Umbra`: debuff trails, siphon on rail contact, risky power gain

Secondary fits:

- `Azure`: aerial transfer and rail control
- `Cinder`: rail sparks, contact damage, offensive exits
- `Verdant`: repair or salvage gain during grind chains

### Flip Module Synergies

Best natural fits:

- `Cinder`: burst damage, slam force, offensive arc hits
- `Azure`: aerial rotation control, repositioning, attack range shaping
- `Umbra`: execute effects, siphon, dangerous high-payoff tricks

Secondary fits:

- `Solar`: combo acceleration and speed conversion
- `Lumen`: safer commit window or damage mitigation mid-flip
- `Verdant`: sustain from successful offensive play

### Grab Module Synergies

Best natural fits:

- `Verdant`: repair, sustain, recovery utility
- `Lumen`: shielding, stability, defense
- `Azure`: hang time, air control, precision movement

Secondary fits:

- `Solar`: momentum retention and quick release benefits
- `Umbra`: trade life for power or siphon gains
- `Cinder`: aggressive release bursts or impact finishers

## Example Module Concepts

### Example 1: Common Ollie Module

- Rarity: `Common`
- Property count: 1
- Property: `Launch Height`
- Mineral: `Azure`
- Category: `Pure Azure`

This is a straightforward focused module with a clean identity.

### Example 2: Rare Grind Module

- Rarity: `Rare`
- Property count: 3
- Properties:
  - `Rail Speed` -> `Solar`
  - `Armor On Rail Entry` -> `Lumen`
  - `Contact Spark Damage` -> `Cinder`
- Category after refinement: `Solar-Lumen-Cinder Matrix`

This is a mixed module supporting speed, safety, and offense.

### Example 3: Epic Flip Module With Duplicate Minerals

- Rarity: `Epic`
- Property count: 4
- Properties:
  - `Aerial Slash Width` -> `Cinder`
  - `Impact Damage` -> `Cinder`
  - `Air Drift Control` -> `Azure`
  - `Combo Gain` -> `Solar`
- Category after investment: `Cinder-Azure-Solar Matrix`

The duplicate `Cinder` rolls push this module toward aggressive specialization.

### Example 4: Unique Grab Module

- Rarity: `Unique`
- Property count: 6
- One property for each mineral family
- Category: `Prismatic`

This is a special all-spectrum module that should feel distinct from normal drop logic.

## UI Notes

Rarity colors and mineral colors overlap in some cases, so they should not rely on color alone.

UI should distinguish these clearly:

- rarity using frame treatment, border style, iconography, or badge shape
- minerals using icons, small color markers, and property tags

This is especially important because `Common`, `Uncommon`, and `Rare` rarity colors overlap with `Lumen`, `Azure`, and `Solar` mineral colors.

## Design Intent

The module system should encourage players to do all of the following:

- chase better module rolls
- commit to favorite minerals
- discover hybrid builds naturally
- value some modules because of mineral alignment, not just rarity
- replay missions to gather refinement materials for specific builds

The best module is not always the highest rarity. A lower-rarity module with strong mineral alignment may outperform a broader but unfocused roll.

## Open Questions For Implementation

1. Should category labels update immediately whenever refinement changes, or only at specific upgrade checkpoints?
2. Should pure, alloy, matrix, and prismatic categories have hidden gameplay bonuses beyond their rolled properties?
3. Should unique modules be handcrafted named drops, or can they still roll randomized property values within their fixed mineral structure?
4. What is the exact property pool for each module type at the first playable slice?
5. How visible should per-property mineral scaling be in the UI?
