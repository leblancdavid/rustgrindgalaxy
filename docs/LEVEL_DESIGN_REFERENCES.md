# Level Design References

Quick reference for all level design work done so far. Use this when designing new tiles, tilesets, or levels.

---

## 1. Tile System Architecture

### Core Rules

| Property | Value |
|----------|-------|
| Tile width | **1280px** (constant, `TileWidth`) |
| Connector system | `RightGroundY` of tile N must match `LeftGroundY` of tile N+1 |
| Tile placement | `Position.X` = running offset, `Position.Y` = auto-calculated from connector mismatch |
| Scene root | `Node2D` with `LevelTile` script |
| Scene location | `scenes/world/tiles/industrial/` |
| Script paths | `scripts/world/LevelTile.cs`, `scripts/world/TileLevelGenerator.cs` |
| Generator doc | `docs/TILE_DESIGN.md` |

### Required Exports on Each Tile

| Field | Meaning |
|-------|---------|
| `LeftGroundY` | Ground Y at left edge (world-space Y offset from tile origin) |
| `RightGroundY` | Ground Y at right edge |
| `LeftRailY` | Rail height at left edge (-1.0 if no rail) |
| `RightRailY` | Rail height at right edge (-1.0 if no rail) |
| `TileWidth` | Always 1280 |

### Registration Checklist

Every tile scene must be registered in **four** places:

1. **Scene file** — `scenes/world/tiles/industrial/<TileName>.tscn`
2. **`LevelTile.cs`** — `_defaultFloorSegments` dictionary entry (walkable surface segments)
3. **`LevelTile.cs`** — `_defaultRailChains` dictionary entry for ramp tiles (generated constant-clearance rail chains; the scene itself must not contain `GrindRail` nodes)
4. **`TileLevelGenerator.cs`** — `const string` path + `_tilePool.Add()` entry with connector Y values and weight

---

## 2. Complete Tile Catalog

### Ground Tiles (23 standard height, 6 steep ramp)

| Tile Name | Left→Right Y | Symmetric? | Floor Segments | Rail |
|-----------|:---:|:---:|---|:---:|
| `FlatRunTile` | 164→164 | Yes | Single flat | Y=108 |
| `HalfPipeTile` | 164→164 | Yes | 5 segs (U-dip to 184) | Center |
| `GapJumpTile` | 164→164 | Yes | 2 segs with 160px gap | None |
| `MultiLevelTile` | 164→164 | Yes | 3 segs (mid-platform at 105) | None |
| `HighFlatTile` | 60→60 | Yes | Single flat | None |
| `MidFlatTile` | 100→100 | Yes | Single flat | None |
| `GentleRiseTile` | 164→100 | No | 3 segs | Generated chain |
| `MidRiseTile` | 100→60 | No | 3 segs | Generated chain |
| `RampSectionTile` | 164→60 | No | 3 segs (ramp 164→60) | Generated chain |
| `StairClimbTile` | 164→60 | No | 8 stair steps | Rail |
| `GentleRiseDescTile` | 100→164 | Desc | Mirror of GentleRise | Generated chain |
| `MidRiseDescTile` | 60→100 | Desc | Mirror of MidRise | Generated chain |
| `RampSectionDescTile` | 60→164 | Desc | Mirror of RampSection | Generated chain |
| `StairClimbDescTile` | 60→164 | Desc | Mirror of StairClimb | Rail |
| `SteepRampAscTile` | 260→60 | No | 5 segs | Generated chain + boost |
| `SteepRampAsc45Tile` | 360→60 | No | 5 segs | Generated chain |
| `SteepRampAsc60Tile` | 460→60 | No | 7 segs | Generated chain |
| `SteepRampDescTile` | 60→260 | Desc | Mirror of Asc | Generated chain |
| `SteepRampDesc45Tile` | 60→360 | Desc | Mirror of Asc45 | Generated chain |
| `SteepRampDesc60Tile` | 60→460 | Desc | Mirror of Asc60 | Generated chain |
| `RampGapTile` | 164→164 | No | 3 segs (ramp up, gap, ramp down) | None |
| `RailGapTile` | 164→164 | Yes | 2 segs with gap | Suspended over gap |
| `RailGapAngledTile` | 164→164 | No | 2 segs with gap | Angled over gap |

### Catwalk Tiles (thin elevated walkways)

All 23 standard tile shapes have catwalk variants. Catwalk tiles use **16px collision height** vs ground tiles' **236px collision height**. Naming: `Catwalk<BaseName>`.

Catwalk variant list:
- `CatwalkFlatRunTile`, `CatwalkHalfPipeTile`, `CatwalkGapJumpTile`, `CatwalkMultiLevelTile`
- `CatwalkHighFlatTile`, `CatwalkMidFlatTile`
- `CatwalkGentleRiseTile`, `CatwalkGentleRiseDescTile`
- `CatwalkMidRiseTile`, `CatwalkMidRiseDescTile`
- `CatwalkRampSectionTile`, `CatwalkRampSectionDescTile`
- `CatwalkStairClimbTile`, `CatwalkStairClimbDescTile`
- `CatwalkSteepRampAscTile`, `CatwalkSteepRampAsc45Tile`, `CatwalkSteepRampAsc60Tile`
- `CatwalkSteepRampDescTile`, `CatwalkSteepRampDesc45Tile`, `CatwalkSteepRampDesc60Tile`
- `CatwalkRampGapTile`, `CatwalkRailGapTile`, `CatwalkRailGapAngledTile`

Connector Y values and floor segments are identical to their ground counterparts.

### Ground Height Reference

| Level | Y Value | Used By |
|-------|:-------:|---------|
| Ground | 164 | FlatRun, HalfPipe, GapJump, MultiLevel, RampSection base |
| Mid | 100 | MidFlat, MidRise base, GentleRise top |
| High | 60 | HighFlat, RampSection top, StairClimb top |
| Steep 1 | 260 | SteepRampSeries base |
| Steep 2 | 360 | SteepRamp45Series base |
| Steep 3 | 460 | SteepRamp60Series base |

### Mirror Convention

Any asymmetric tile (`LeftY ≠ RightY`) **must** have a descending (`*Desc`) variant. To create one:

1. Copy the source scene
2. Mirror all Polygon2D X coords: `newX = 1280 - oldX`
3. Mirror all CollisionPolygon2D X coords
4. Mirror node positions: `newPosX = 1280 - oldPosX`
5. Negate GrindRail rotation (n/a for ramp chain tiles — those scenes carry no rails; see `TILE_DESIGN.md#rail-chains`)
6. Swap Left/Right GroundY on root node
7. Add matching FloorSegments: `[sx, ex, sy, ey]` → `[1280-ex, 1280-sx, ey, sy]`
8. Register in all 4 places

Symmetrical tiles (`LeftY = RightY`) do not need a Desc variant.

---

## 3. Floor Segments Reference

Defined in `LevelTile.cs:479` (`_defaultFloorSegments` dictionary). Each entry is an array of `FloorSegment(startX, endX, startY, endY)`.

### Patterns

| Pattern | Structure | Tiles Using It |
|---------|-----------|---------------|
| **Flat** | Single segment `[0, 1280, Y, Y]` | FlatRun, HighFlat, MidFlat |
| **Ramp** | Flat start → interpolated ramp → flat landing | RampSection (164→60), GentleRise (164→100), MidRise (100→60) |
| **Desc ramp** | Same as ramp but reversed Y | RampSectionDesc, GentleRiseDesc, MidRiseDesc |
| **Stair** | 8 flat steps at increments | StairClimb (164→60 in 8 steps), StairClimbDesc (reverse) |
| **HalfPipe** | Flat → slope down → flat dip → slope up → flat | HalfPipe, CatwalkHalfPipe |
| **Gap** | Two disconnected segments | GapJump (560-720 gap), RailGap (400-880 gap) |
| **RampGap** | Flat → ramp up → gap → ramp down → flat | RampGap (unique 3-segment) |
| **Steep** | Flat plateau → 3-segment interpolated ramp → flat plateau | SteepRampAsc/Desc (5-7 segments) |
| **MultiLevel** | Flat → mid platform → flat | MultiLevel (164→105→164) |

### Prop Spawning Rules

- Props spawn within floor segment bounds, with a half-width margin
- Segments steeper than 30 degrees are skipped for prop spawning
- Prop exclusion radius: 60px around interactives/respawns
- 4-8 decorative props per tile, weighted random from active prop palette
- 1 interactive prop per tile at 30% chance (BoostPad / LaunchPad / GrindBoost)
- Loot props spawn at 50% chance, 1-3 per tile

---

## 4. Visual Node Naming Convention (Palette Mapping)

`LevelTile.ApplyVisualPalette()` uses `Polygon2D.Name` string matching to assign colors:

| Node Name Contains | Palette Slot | Example |
|--------------------|:-------------|---------|
| `"Rise"` (wall rises) | `SecondaryMedium` | Vertical wall faces between heights |
| `"Edge"` | `PrimaryLight` | Thin highlight edge on platforms |
| `"Trim"` | `SecondaryLight` | Bright edge line on walkable surface |
| `"UpperPlatformVisual"` | `SecondaryDark` | Elevated platform surfaces |
| `"Visual"` (catch-all floor) | `SecondaryDark` | Main ground visual fill |
| Other/unknown | Skipped | Not colored |

---

## 5. Color Palette System

6 minerals × 3 brightness variants = 18 palette colors. Derived from mission's primary + secondary mineral.

| Mineral | Light | Medium | Dark |
|---------|-------|--------|------|
| Cinder (red) | `#F07830` | `#C04718` | `#802808` |
| Verdant (green) | `#60D060` | `#38A828` | `#186818` |
| Azure (blue) | `#50B8E0` | `#2880B0` | `#105070` |
| Solar (yellow) | `#F0D040` | `#C8A018` | `#887008` |
| Lumen (white) | `#D0E8F8` | `#88B8D8` | `#4878A0` |
| Umbra (purple) | `#B870D0` | `#8040A0` | `#482868` |

`LevelColorPalette` struct: `PrimaryDark/Medium/Light` + `SecondaryDark/Medium/Light`.

Full design: `docs/COLOR_PALETTE_SYSTEM.md`.

---

## 6. Prop Palettes

Three complete prop palettes defined in `MissionLevel.cs` as grayscale `PropTemplate` lists:

| Palette | Theme | Total Props | Background | Default | Foreground |
|---------|-------|:-----------:|:----------:|:-------:|:----------:|
| **Industrial** | Factory pipes, vents, consoles | 27 | 9 | 15 | 8 |
| **Derelict** | Corroded, rubble, scrap | 16 | 5 | 7 | 4 |
| **Surface** | Rock, crystal, alien terrain | 17 | 5 | 8 | 4 |

Each prop template: Width, Height, Grayscale Color, Weight, Layer, PaletteSlot, Glow params.

Full reference: `docs/PROP_CATALOG.md`.

---

## 7. Level Types

### TileLevelIndustrial (Procedural — Main Level)
- **Scene:** `scenes/world/levels/TileLevelIndustrial.tscn`
- **Script:** `scripts/world/TileLevelIndustrial.cs`
- **Generator:** `TileLevelGenerator.cs` — streaming procedural placer
- **Pool:** All 46 tile types registered (23 ground + 23 catwalk), weight=1.0 each
- **Defaults:** MinLevelTiles=15, TilesAheadOfPlayer=5, BeaconInterval=5
- **Palette:** Industrial, overridden by mission minerals
- **Entry:** `ProcGenTest.tscn` (current game boot)

### LevelSurface01 (Handcrafted)
- **Scene:** `scenes/world/levels/LevelSurface01.tscn`
- **Width:** 320px, viewport 320×180
- **Prop palette:** Surface (rock/crystal)
- **Content:** Main ground + 3 platforms, 2 rails, 3 raiders, 2 drones, 3 hazards, 6 pickups
- **Background:** Sky, Horizon, DustBand (ColorRects tinted by palette)

### LevelDerelict01 (Handcrafted)
- **Scene:** `scenes/world/levels/LevelDerelict01.tscn`
- **Width:** 320px, viewport 320×180
- **Prop palette:** Derelict (rusted/corroded)
- **Content:** Main ground + 3 platforms, 2 rails, 3 raiders, 3 drones, 5 hazards, 6 pickups
- **Background:** Backdrop, HullBand, FogBand (purple theme)

### MovementTest (Sandbox)
- **Scene:** `scenes/world/MovementTest.tscn`
- **Width:** 1280×360
- **Purpose:** Movement feel testing with instructions overlay
- **Content:** Main ground, 3 stair-step platforms, sloped ramps, bridge, landing, 3 rails

### ProcGenTest (Generator Test Harness)
- **Scene:** `scenes/world/ProcGenTest.tscn`
- **Entry point:** `scripts/Main.cs` loads this scene
- **Config:** `cycle_all_tiles_before_repeat=true`, `min_level_tiles=32`
- **Background:** Space parallax, randomized mineral palette

---

## 8. World Object Scenes

| Object | Script | Scene | Function |
|--------|--------|-------|----------|
| GrindRail | `scripts/world/GrindRail.cs` | `scenes/world/GrindRail.tscn` | Rail-grind zone |
| GrindBoost | `scripts/world/GrindBoost.cs` | (code-created) | Rail speed boost (2x, 1.5s) |
| BoostPad | `scripts/world/BoostPad.cs` | (code-created) | Floor speed boost (2x, 1.5s) |
| LaunchPad | `scripts/world/LaunchPad.cs` | (code-created) | Vertical launch (700px/s) |
| ShockHazard | `scripts/world/ShockHazard.cs` | `scenes/world/ShockHazard.tscn` | Contact damage (1 dmg) |
| MineralPickup | `scripts/world/MineralPickup.cs` | `scenes/world/MineralPickup.tscn` | Collect mineral (6 types) |
| ExtractionZone | `scripts/world/ExtractionZone.cs` | `scenes/world/ExtractionZone.tscn` | Mission exit |
| RespawnBeacon | `scripts/world/RespawnBeacon.cs` | `scenes/world/RespawnBeacon.tscn` | Checkpoint |
| Prop | `scripts/world/Prop.cs` | (code-created) | Decorative |
| LootProp | `scripts/world/LootProp.cs` | (code-created) | Breakable loot |
| RectGlow | `scripts/world/RectGlow.cs` | (code-created) | Glow effect |

---

## 9. PixelLab Account Status

| Resource | Count | Details |
|----------|:-----:|---------|
| Generations remaining | 13/40 | Trial subscription |
| Top-down tilesets | 0 | None created yet |
| Sidescroller tilesets | 0 | None created yet |
| Isometric tiles | 0 | None created yet |
| Tiles Pro | 0 | None created yet |
| Objects | 0 | None created yet |
| Characters | 2 | "Vern Radio Host" (80px, 8-dir, low top-down), "Office Worker" (48px, 4-dir, high top-down) |

No tile/tileset assets have been generated via PixelLab yet. The current system uses runtime Polygon2D procedurally generated shapes with grayscale colors and palette tinting.

---

## 10. Guidelines for Future Tile Design

### When Adding a New Tile

1. **Scene file** → `scenes/world/tiles/industrial/<Name>Tile.tscn`
2. **Floor segments** → add entry in `LevelTile.cs` `_defaultFloorSegments`
3. **Register in pool** → add path constant + `_tilePool.Add()` in `TileLevelGenerator.cs` `LoadTilePool()`
4. **If asymmetric** → create Desc variant with mirrored geometry

### When Adding a New Theme/Tileset

- Create new scene folder: `scenes/world/tiles/<theme>/`
- Create new level scene: `scenes/world/levels/TileLevel<Theme>.tscn`
- Register new tile paths in a new or extended `TileLevelGenerator`
- Define new prop palette in `MissionLevel.cs` `PropPalettes`

### PixelLab Tileset Creation Notes

- Our current art is procedural Polygon2D (no sprite textures yet)
- PixelLab tilesets should match: sidescroller perspective, 16×16 or 32×32 tile size
- For connected tilesets, create a base tileset first, then use its base tile ID for chaining
- See TILE_DESIGN.md for tile specs that pixel art sprites would need to match

### Key Heights Reference

```
Tile:       1280px wide
Ground:     164px (default floor)
Mid:        100px
High:        60px
Steep bases: 260, 360, 460px
Viewport:   640×360 (or 320×180 for handcrafted levels)
```
