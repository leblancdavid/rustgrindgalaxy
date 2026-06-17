# Enemy Implementation Plan

## Overview

Add shared base classes, projectile/effect scenes, and 11+ enemy types across 4 phases. Build reusable architecture first, then layer in enemy variety.

---

## Phase 0 — Shared Architecture

### Base Class: `EnemyBase`
**New file:** `scripts/enemies/EnemyBase.cs`

Shared base for all enemies. Handles:
- `MaxHealth` / `CurrentHealth` with `TakeDamage(int amount, Node2D source)`
- `Die()` — plays death effect, queues free
- `Hurt flash` — tint red briefly on hit
- `Knockback` — optional velocity impulse on damage
- `Player detection` — helpers for distance/line-of-sight checks
- `Facing direction` — flips sprite toward player or movement direction

Exports:
- `MaxHealth` (int, default 2)
- `ContactDamage` (int, default 1)
- `DetectionRange` (float, default 0 = never aggro)
- `KnockbackResistance` (float, default 0)

### Shared Effect: `ExplosionEffect`
**New files:** `scenes/effects/ExplosionEffect.tscn`, `scripts/effects/ExplosionEffect.cs`

- AnimatedSprite2D playing explosion frames from `animations/explosions/`
- Optional Area2D for damage radius (configurable)
- Auto-queues free after animation ends
- Parameter: explosion_variant (int, which explosion set 1-10)

### Shared Effect: `SlashEffect`
**New files:** `scenes/effects/SlashEffect.tscn`, `scripts/effects/SlashEffect.cs`

- AnimatedSprite2D from `animations/slash/`
- Positioned at impact point
- Auto-queues free

### Shared Projectiles

All in new `scenes/projectiles/` and `scripts/projectiles/`:

| Scene | Script | Movement | Collision |
|---|---|---|---|
| `BulletProjectile.tscn` | `BulletProjectile.cs` | Straight line at speed | Area2D, damages player on contact, despawns |
| `BoomerangProjectile.tscn` | `BoomerangProjectile.cs` | Arc out then return to thrower | Area2D, damages player, despawns on return |
| `GrenadeProjectile.tscn` | `GrenadeProjectile.cs` | RigidBody with bounce + timer | Explodes after bounce + delay |
| `LaserBeam.tscn` | `LaserBeam.cs` | RayCast2D + Line2D, persistent duration | Continuous damage while active |
| `Shockwave.tscn` | `Shockwave.cs` | Expanding circle Area2D | Ground-level, one-pass hitbox |

Each projectile:
- Has a `Damage` property set by the spawning enemy
- Despawns after `MaxLifetime` or on hit
- Has optional `Homing` bool for tracking projectiles
- Emits `HitPlayer` signal for score tracking etc.

### `SharedEnemyState` Enum
**In `EnemyBase.cs`:**

```csharp
public enum EnemyState
{
    Idle,
    Patrol,
    Alert,
    Chase,
    Attack,
    Stagger,
    Dead,
}
```

---

## Phase 1 — Upgrade Existing Enemies

### 1a. Raider Upgrades
**Files changed:** `RaiderEnemy.cs`, `Raider.tscn`

- Inherit from `EnemyBase` instead of `CharacterBody2D`
- Add `EnemyState` machine: Idle → Patrol → Alert → Chase → Attack
- Add aggro detection: player within `DetectionRange` triggers Chase
- Add melee slash attack:
  - Enter `AttackRange` → stop, face player, telegraph (0.3s), slash
  - Slash: Area2D arc in front, active for 0.2s
  - Uses existing `Raider_1/Attack_1.png` etc. (AnimatedSprite2D)
- Patrol unchanged otherwise
- Contact damage still applies

### 1b. Drone Upgrades
**Files changed:** `DroneEnemy.cs`, `Drone.tscn`

- Inherit from `EnemyBase` instead of `Area2D`
- Add bullet attack:
  - On player detection, fire `BulletProjectile` toward player
  - 1.5s cooldown between shots
  - Drone maintains hover height but can drift toward player
- Still does contact damage

---

## Phase 2 — High-Impact New Enemies

### 2a. Laser Turret
**New files:** `scripts/enemies/LaserTurret.cs`, `scenes/enemies/LaserTurret.tscn`

- Stationary turret (StaticBody2D or CharacterBody2D with locked position)
- Rotates toward player (pivot node, limited rotation arc)
- State machine: Idle → Aiming (0.5s telegraph, red line) → Firing (0.3s laser)
- Uses `LaserBeam` scene
- Despawns when destroyed
- Can be placed on walls/ceilings

### 2b. Bomb Bot
**New files:** `scripts/enemies/BombBot.cs`, `scenes/enemies/BombBot.tscn`

- Idle until player in detection radius
- Locks on, starts beeping (increasing speed)
- Rolls toward player using CharacterBody2D with MoveAndSlide
- Explodes on contact or after 4s timer
- Uses `ExplosionEffect` scene
- Can be destroyed before detonation (1 HP)

### 2c. Rail Guard
**New files:** `scripts/enemies/RailGuard.cs`, `scenes/enemies/RailGuard.tscn`

- Spawns on a GrindRail path, moves along it
- Uses PathFollow2D on rail Path2D
- When player is grinding toward it, glows and shoots bolt forward
- Bolt is `BulletProjectile` along rail direction
- After shooting, vulnerable window (1s)
- Contact damage bumps player off rail

---

## Phase 3 — Mid-Level Variety

### 3a. Combat Drone
**New files:** `scripts/enemies/CombatDroneEnemy.cs`, `scenes/enemies/CombatDrone.tscn`

- Hovering drone with aggressive positioning AI
- Flanks player: moves to position ~45° off player angle
- Fires 3-bullet burst, then repositions
- Retreats if player gets too close

### 3b. Grenadier
**New files:** `scripts/enemies/Grenadier.cs`, `scenes/enemies/Grenadier.tscn`

- Hovers high, fires grenades that bounce then explode
- Uses `GrenadeProjectile`
- Shows ground arc indicator where grenade will land
- Vulnerable while reloading (2s window)

### 3c. Boomerang Raider
**New files:** `scripts/enemies/BoomerangRaider.cs`, `scenes/enemies/BoomerangRaider.tscn`

- Ground patrol like raider but ranged
- Throws boomerang at player range
- Uses `BoomerangProjectile`
- Vulnerable during throw recovery

---

## Phase 4 — Specialized Threats

### 4a. Mine Layer
**New files:** `scripts/enemies/MineLayer.cs`, `scenes/enemies/MineLayer.tscn`, `scripts/projectiles/Mine.cs`

- Slow ground enemy, drops mines periodically
- Mines: small Area2D, arms after 0.5s, explodes on player touch
- Mine Layer has shield (absorbs 1 hit, recharges 3s)
- Uses `ExplosionEffect` for mine detonation

### 4b. Shock Drone
**New files:** `scripts/enemies/ShockDrone.cs`, `scenes/enemies/ShockDrone.tscn`

- Hovering drone, telegraphs with glow + crackle (1s)
- Fires ground-level shockwave ring
- Uses `Shockwave` scene
- Vulnerable after attack (1.5s)

### 4c. Suicide Drone
**New files:** `scripts/enemies/SuicideDrone.cs`, `scenes/enemies/SuicideDrone.tscn`

- Dormant hover, activates when player in range
- Locks on with increasing beep/pulse
- Dives in straight line at player position
- Explodes on contact/terrain hit
- Uses `ExplosionEffect`

---

## Tile Spawn Marker Convention

Each tile scene (`LevelTile`) already has spawn containers:
- `Raiders` (Node2D) — spawns raider-type enemies
- `Drones` (Node2D) — spawns drone-type enemies

Expand to include:
- `Hazards` (Node2D) — for Bomb Bots, Mine Layers, etc.
- `Turrets` (Node2D) — for Laser Turrets
- `RailEnemies` (Node2D inside rail scenes) — for Rail Guards

A new `scripts/enemies/EnemySpawner.cs` reads these markers and instantiates enemies on level load:

```csharp
// Each child node in a spawn container gets a matching enemy scene instantiated
foreach (Node2D marker in GetNode("Raiders").GetChildren())
{
    var raider = RaiderScene.Instantiate<RaiderEnemy>();
    raider.GlobalPosition = marker.GlobalPosition;
    AddChild(raider);
}
```

---

## File Change Summary

### New Files (14 scripts + 14 scenes = ~28 files)

```
scripts/enemies/EnemyBase.cs          — shared base class
scripts/effects/ExplosionEffect.cs    — explosion animation
scripts/effects/SlashEffect.cs        — slash animation
scripts/projectiles/BulletProjectile.cs
scripts/projectiles/BoomerangProjectile.cs
scripts/projectiles/GrenadeProjectile.cs
scripts/projectiles/LaserBeam.cs
scripts/projectiles/Shockwave.cs
scripts/projectiles/Mine.cs           — mine layer's mine
scripts/enemies/LaserTurret.cs
scripts/enemies/BombBot.cs
scripts/enemies/RailGuard.cs
scripts/enemies/CombatDroneEnemy.cs
scripts/enemies/Grenadier.cs
scripts/enemies/BoomerangRaider.cs
scripts/enemies/MineLayer.cs
scripts/enemies/ShockDrone.cs
scripts/enemies/SuicideDrone.cs
scripts/enemies/EnemySpawner.cs
scenes/enemies/LaserTurret.tscn
scenes/enemies/BombBot.tscn
scenes/enemies/RailGuard.tscn
scenes/enemies/CombatDrone.tscn
scenes/enemies/Grenadier.tscn
scenes/enemies/BoomerangRaider.tscn
scenes/enemies/MineLayer.tscn
scenes/enemies/ShockDrone.tscn
scenes/enemies/SuicideDrone.tscn
scenes/effects/ExplosionEffect.tscn
scenes/effects/SlashEffect.tscn
scenes/projectiles/BulletProjectile.tscn
scenes/projectiles/BoomerangProjectile.tscn
scenes/projectiles/GrenadeProjectile.tscn
scenes/projectiles/LaserBeam.tscn
scenes/projectiles/Shockwave.tscn
scenes/projectiles/Mine.tscn
```

### Modified Files

```
scripts/enemies/RaiderEnemy.cs    — inherit EnemyBase, add state machine + melee
scripts/enemies/DroneEnemy.cs     — inherit EnemyBase, add bullet attack
scripts/world/LevelTile.cs        — add new spawn container support
scripts/world/TileLevelGenerator.cs — place spawn markers in tile generation
```

---

## Key Design Decisions

1. **State machines via enum + switch** — not a framework. Simple and readable for the slice.
2. **Projectiles are pooled** — `BulletProjectile` and friends use `QueueFree()` on despawn. If performance becomes an issue, add `ObjectPool` later.
3. **EnemySpawner reads marker nodes** — level designers place empty `Marker2D` nodes in spawn containers on each tile scene. The spawner reads them at runtime and instantiates the correct enemy type.
4. **EnemyBase vs Node composition** — base class approach chosen for simplicity over component-based. Health, damage, knockback, and state are core enough that inheritance is cleaner.
5. **GrindRail integration** — Rail Guard gets a special `SpawnOnRail(RailPath2D)` method since it needs path-following. Other enemies ignore rails entirely.
