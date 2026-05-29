# Rust Grind Galaxy Module Property Pools

## Purpose

This document defines the first-pass property catalog for modules.

Goals:

- every module type can roll from all six minerals
- every mineral has offensive, defensive, and utility expression
- the first implementation is structured and complete without being bloated
- the system is easy to turn into gameplay data later

## Generation Rules

Each dropped module is generated using these rules:

1. Determine module type.
2. Determine rarity.
3. Set property count from rarity.
4. For each property slot:
   - roll one mineral from the six-mineral pool
   - roll one variant class: `Offensive`, `Defensive`, or `Utility`
   - select the matching property from the module type + mineral + variant bucket
   - reject duplicates if the same property name already exists on the module
5. Allow duplicate minerals on a module.
6. Derive category later from refinement totals as defined in `MODULE_SYSTEM.md`.

## Rarity Recap

| Rarity | Property Count |
| --- | --- |
| Common | 1 |
| Uncommon | 2 |
| Rare | 3 |
| Epic | 4 |
| Legendary | 5 |
| Unique | 6 |

`Unique` modules remain the exception case that always include all six minerals, one per property.

## Variant Classes

### Offensive

Properties that improve damage, enemy pressure, hit behavior, burst windows, or offensive conversion.

### Defensive

Properties that improve armor, shielding, damage reduction, safety windows, sustain, or recovery.

### Utility

Properties that improve movement, control, meter flow, pickup flow, route advantage, or interaction quality.

## Mineral Damage Types

If a property deals damage or applies an offensive effect with a typed element, it uses the damage type associated with its mineral.

| Mineral | Damage Type |
| --- | --- |
| Cinder | Fire |
| Verdant | Acid |
| Azure | Cold |
| Solar | Shock |
| Lumen | Radiant |
| Umbra | Void |

Not every property needs to deal damage. Defensive and utility properties often will not. When a property does deal damage, its mineral defines the damage type.

## Property Matrix Rules

The first-pass catalog contains exactly one property for each:

- module type
- mineral
- variant class

This produces:

- 4 module types
- 6 minerals per module type
- 3 variants per mineral
- 72 total properties

## Ollie Module Property Pool

The `Ollie` module controls launch, landing, and the immediate transition between ground and air.

| Mineral | Variant | Property | Effect | Damage Type |
| --- | --- | --- | --- | --- |
| Cinder | Offensive | Landing Shockwave | Ollie landings create a short fire burst that damages nearby enemies. | Fire |
| Cinder | Defensive | Heat Shield Landing | Gain brief damage reduction after landing from an ollie. | - |
| Cinder | Utility | Breach Kick | Hard landings break weak hazards or crates in a small radius. | Fire |
| Verdant | Offensive | Spore Kickback | Landing near enemies releases an acid pulse that lightly damages and disrupts them. | Acid |
| Verdant | Defensive | Recovery Pulse | Clean ollie landings restore a small amount of health. | - |
| Verdant | Utility | Salvage Step | Successful ollie landings increase pickup pull and salvage collection briefly. | - |
| Azure | Offensive | Frost Launch Edge | The initial ollie rise lightly damages close enemies with a cold burst. | Cold |
| Azure | Defensive | Air Correction Window | Gain a brief period of improved airborne recovery after ollie launch. | - |
| Azure | Utility | Launch Height | Increases ollie height. | - |
| Solar | Offensive | Momentum Discharge | Faster ollies convert movement speed into bonus shock impact on landing. | Shock |
| Solar | Defensive | Evasive Lift | Gain a short evade window immediately after ollie takeoff. | - |
| Solar | Utility | Burst Takeoff | Grants a short speed boost when performing an ollie. | - |
| Lumen | Offensive | Precision Landing Pulse | Perfect or clean landings emit a small radiant pulse that damages nearby enemies. | Radiant |
| Lumen | Defensive | Safe Landing Plating | Reduces damage or stagger taken during the landing window. | - |
| Lumen | Utility | Stabilized Touchdown | Improves landing stability and shortens recovery after ollie landing. | - |
| Umbra | Offensive | Void Drop | Ollie landings release a void burst that deals higher damage when the player is at low health. | Void |
| Umbra | Defensive | Siphon Impact Guard | Landing on or near enemies grants a small temporary shield. | - |
| Umbra | Utility | Ghost Descent | Slightly softens landing commitment and improves repositioning after descent. | - |

## Grind Module Property Pool

The `Grind` module controls rail entry, sustained rail payoff, enemy contact on rails, and grind exits.

| Mineral | Variant | Property | Effect | Damage Type |
| --- | --- | --- | --- | --- |
| Cinder | Offensive | Spark Trail | Grinding deals fire damage to enemies touched along the rail. | Fire |
| Cinder | Defensive | Tempered Sparks | Gain brief contact damage reduction while actively grinding. | - |
| Cinder | Utility | Furnace Exit | Exiting a rail creates a small burst that clears weak hazards near the exit. | Fire |
| Verdant | Offensive | Corrosive Rail Wake | Grinding leaves a short acid trail that damages enemies passing through it. | Acid |
| Verdant | Defensive | Rail Repair | Regenerate a small amount of health while maintaining a grind. | - |
| Verdant | Utility | Salvage Sweep | Increase pickup radius and resource collection while grinding. | - |
| Azure | Offensive | Cryo Rail Arc | Rail contact emits small cold jolts that damage nearby airborne enemies. | Cold |
| Azure | Defensive | Coolflow Recovery | Recover control more safely when leaving a rail unexpectedly. | - |
| Azure | Utility | Glide Transfer | Improves aerial transfer and directional control when entering or leaving rails. | - |
| Solar | Offensive | Overcharge Rail | Grinding at speed builds shock damage that discharges on enemy contact. | Shock |
| Solar | Defensive | Voltage Buffer | Gain brief protection against interruption when entering a rail. | - |
| Solar | Utility | Rail Speed | Increases movement speed while grinding. | - |
| Lumen | Offensive | Radiant Edge Rail | Clean grind lines emit small radiant pulses that damage nearby enemies. | Radiant |
| Lumen | Defensive | Armor On Rail Entry | Grants brief armor or damage reduction when entering a rail. | - |
| Lumen | Utility | Mag Lock Stability | Makes grind retention and exit timing more forgiving. | - |
| Umbra | Offensive | Void Shred Line | Grinding through enemies deals void damage and briefly weakens them. | Void |
| Umbra | Defensive | Siphon Rail Guard | Enemy contact during a grind grants a small shield or life siphon effect. | - |
| Umbra | Utility | Threat Blur | Grinding lowers enemy accuracy or threat focus on the player for a short time. | - |

## Flip Module Property Pool

The `Flip` module controls airborne attack expression, burst payoff, and offensive combo pressure.

| Mineral | Variant | Property | Effect | Damage Type |
| --- | --- | --- | --- | --- |
| Cinder | Offensive | Flip Impact | Flip attacks deal increased fire damage on hit. | Fire |
| Cinder | Defensive | Ember Guard Frame | Gain a brief damage reduction window during flip startup. | - |
| Cinder | Utility | Aggro Flip Carry | Successful flip hits preserve more forward momentum. | - |
| Verdant | Offensive | Caustic Slice | Flip hits apply an acid burst that damages enemies over a short duration. | Acid |
| Verdant | Defensive | Repair On Hit | Successful flip hits restore a small amount of health. | - |
| Verdant | Utility | Scrap Shear | Enemies hit by flips have an increased chance to drop salvage or pickups. | - |
| Azure | Offensive | Arc Width | Increases the hit area or reach of flip attacks with a cold edge. | Cold |
| Azure | Defensive | Drift Guard | Improves air control during and immediately after a flip. | - |
| Azure | Utility | Rotation Control | Makes flip timing and aerial repositioning more controllable. | - |
| Solar | Offensive | Combo Charge | Successful flip hits generate additional ultimate or combo meter with shock output. | Shock |
| Solar | Defensive | Quick Reset | Shortens vulnerable recovery after completing a flip. | - |
| Solar | Utility | Speed Loop | Flip completion grants a short burst of movement speed. | - |
| Lumen | Offensive | Radiant Cutline | Flip hits emit a narrow radiant follow-through pulse. | Radiant |
| Lumen | Defensive | Guard Frame | Grants a small shield or guard window during the flip's active sequence. | - |
| Lumen | Utility | Precision Arc | Improves flip consistency and clean hit alignment. | - |
| Umbra | Offensive | Void Execution | Flip hits deal bonus void damage to low-health enemies. | Void |
| Umbra | Defensive | Siphon Veil | Successful flip hits grant a brief shield or damage siphon effect. | - |
| Umbra | Utility | Threat Cut | Flip hits reduce enemy aggression or targeting for a short time. | - |

## Grab Module Property Pool

The `Grab` module controls hang time, air safety, utility sustain, and release effects.

| Mineral | Variant | Property | Effect | Damage Type |
| --- | --- | --- | --- | --- |
| Cinder | Offensive | Impact Release | Releasing a grab into landing causes a small fire burst around the player. | Fire |
| Cinder | Defensive | Heated Brace | Gain brief resistance during grab release and landing. | - |
| Cinder | Utility | Burnthrough Drop | Grab releases destroy weak obstacles or hazards on landing. | Fire |
| Verdant | Offensive | Acid Vent Release | Grab release emits a small acid pulse that damages nearby enemies. | Acid |
| Verdant | Defensive | Repair Grip | Restore a small amount of health while holding or cleanly releasing a grab. | - |
| Verdant | Utility | Scavenger Hold | Grabs improve pickup attraction and salvage collection briefly. | - |
| Azure | Offensive | Cold Wake Release | Grab release emits a small cold burst that damages and lightly slows enemies. | Cold |
| Azure | Defensive | Softfall Field | Reduces descent risk and improves safe aerial correction during a grab. | - |
| Azure | Utility | Hang Time | Slows fall speed while the grab is active. | - |
| Solar | Offensive | Shock Snap Release | Releasing a grab at speed emits a short shock burst on landing. | Shock |
| Solar | Defensive | Momentum Cushion | Retains control and reduces punishment when transitioning out of a grab. | - |
| Solar | Utility | Momentum Preserve | Preserves more horizontal speed when releasing a grab. | - |
| Lumen | Offensive | Radiant Flare Release | Clean grab release emits a small radiant pulse on landing. | Radiant |
| Lumen | Defensive | Shield Hold | Grants brief shielding or damage reduction while holding a grab. | - |
| Lumen | Utility | Stable Float | Improves aerial steadiness and precision while grabbing. | - |
| Umbra | Offensive | Void Drop Field | Grab release creates a small void burst that deals bonus damage in risky close range. | Void |
| Umbra | Defensive | Dark Siphon Hold | Holding or releasing a grab near enemies grants a small shield or siphon effect. | - |
| Umbra | Utility | Threat Fade | Grabs briefly reduce enemy awareness or pressure after release. | - |

## Slice-Ready Implementation Notes

For the first playable slice, not all 72 properties need to be active immediately. The important thing is to define the complete catalog now so the system has a stable structure.

Recommended first activation priority:

- `Azure`, `Solar`, `Cinder`, and `Lumen` properties first
- `Verdant` and `Umbra` next, once health, shield, and enemy response loops are stable

This is an implementation recommendation, not a design restriction. All six minerals remain part of the roll table design.

## Future Weighting Notes

Mineral probabilities should remain equal by default.

Later, mission context can adjust roll weighting based on:

- planet biome
- station type
- faction presence
- hazard profile
- boss territory

Examples:

- volcanic or refinery missions may bias `Cinder`
- frozen or high-altitude zones may bias `Azure`
- holy or corporate shield-heavy zones may bias `Lumen`
- corrupted or abandoned sectors may bias `Umbra`

## Implementation Notes

The simplest future data shape for each property is:

- `PropertyId`
- `DisplayName`
- `ModuleType`
- `Mineral`
- `Variant`
- `Description`
- `DamageType`
- `SliceReady`

This document is the design source for that data.
