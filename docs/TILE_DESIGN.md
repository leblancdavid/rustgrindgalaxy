# Tile Design Guide

## Overview

Levels are assembled from individual tile scenes placed side-by-side. Each tile is 1280px wide and defines:

- **Visuals**: `Polygon2D` nodes for the floor, trim, and structure
- **Physics**: `StaticBody2D` with `CollisionShape2D` or `CollisionPolygon2D` children
- **Rail**: An optional instanced `GrindRail` for rail-grind sections
- **Spawn Markers**: Empty `Node2D` containers for enemies (`Raiders`, `Drones`), hazards, and pickups

## Tile Structure

Each tile is a `Node2D` root with the `LevelTile` script attached.

### Required Exports

| Field | Description |
|---|---|
| `TileWidth` | Always `1280` |
| `LeftGroundY` | Ground Y at the tile's left edge (world-space Y offset from tile origin) |
| `RightGroundY` | Ground Y at the tile's right edge |
| `LeftRailY` | Rail height at left edge (`-1.0` if no rail) |
| `RightRailY` | Rail height at right edge (`-1.0` if no rail) |

### Connector System

Adjacent tiles must have matching connector heights. A tile's `RightGroundY` must equal the next tile's `LeftGroundY` for a seamless floor. The `TileLevelGenerator` aligns tiles by matching these values.

### FloorSegments

Every tile must have an entry in the `_defaultFloorSegments` dictionary in `LevelTile.cs`. These define the walkable surface as flat or sloped segments, which drive prop spawning and boost pad placement.

## Mirror Convention

**Any asymmetric tile MUST have a corresponding descending (`*Desc`) variant.**

Tiles that are not left-right symmetric (different `LeftGroundY` and `RightGroundY`) need a horizontally-mirrored counterpart so the level generator can create both rising and falling terrain.

| Ascending Tile | Descending Counterpart | Left→Right Y |
|---|---|---|
| `RampSectionTile` | `RampSectionDescTile` | 164→60 / 60→164 |
| `StairClimbTile` | `StairClimbDescTile` | 164→60 / 60→164 |
| `GentleRiseTile` | `GentleRiseDescTile` | 164→100 / 100→164 |
| `MidRiseTile` | `MidRiseDescTile` | 100→60 / 60→100 |

Symmetrical tiles (equal Left and Right Y) do not need a Desc variant.

### Creating a Desc Variant

1. Copy the source scene file
2. Mirror all `Polygon2D` X coordinates: `newX = 1280 - oldX`
3. Mirror all `CollisionPolygon2D` X coordinates
4. Mirror node positions: `newPosX = 1280 - oldPosX`
5. Negate `GrindRail` rotation
6. Swap `LeftGroundY` / `RightGroundY` on the root node
7. Add matching `FloorSegment` entries in `LevelTile.cs`:
   - For each segment `[sx, ex, sy, ey]` → `[1280-ex, 1280-sx, ey, sy]`
8. Register in `TileLevelGenerator.cs` with a `_tilePool.Add` entry

## Registration Checklist

Every tile scene must be registered in two places:

1. **`LevelTile.cs`** — `_defaultFloorSegments` dictionary entry
2. **`TileLevelGenerator.cs`** — path constant + `_tilePool.Add` entry with proper `LeftGroundY`/`RightGroundY` and weight

## Symmetrical Tiles (No Desc Variant Needed)

These tiles have equal `LeftGroundY` and `RightGroundY`, so mirroring produces the same layout:

- `FlatRunTile` / `CatwalkFlatRunTile`
- `HalfPipeTile` / `CatwalkHalfPipeTile`
- `GapJumpTile` / `CatwalkGapJumpTile`
- `MultiLevelTile` / `CatwalkMultiLevelTile`
- `HighFlatTile` / `CatwalkHighFlatTile`
- `MidFlatTile` / `CatwalkMidFlatTile`
