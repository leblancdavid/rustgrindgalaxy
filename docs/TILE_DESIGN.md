# Tile Design Guide

## Overview

Levels are assembled from individual tile scenes placed side-by-side. Each tile is 1280px wide and defines:

- **Visuals**: `Polygon2D` nodes for the floor, trim, and structure
- **Physics**: `StaticBody2D` with `CollisionShape2D` or `CollisionPolygon2D` children
- **Rail**: An optional instanced `GrindRail` for rail-grind sections. Ramp tiles instead generate their rails at runtime (see Rail Chains below)
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

### Rail Chains

Ramp tiles do NOT contain hand-placed `GrindRail` nodes. If the tile name has an entry in `_defaultRailChains` (`LevelTile.cs`), `TileLevelGenerator` calls `LevelTile.BuildRailChains()` at placement time, which generates one `GrindRail` per covered segment from the floor polyline via `RailChainGeometry.cs`:

- Every rail sits at a constant **perpendicular** clearance (`36` default, per-chain `Clearance` field in `_defaultRailChains`) from its slope. This replaced the old hand-baked rails, whose offset was *vertical*: a fixed vertical gap `h` shrinks the perpendicular gap to `h·cos θ` (the 45° segments rode at only ~20 px perpendicular while flats sat at 56), which is what made ramp rails look glued to the floor.
- Segment-to-segment joints are the miter intersection of the two offset lines: `vertex + (clearance / cos(Δθ/2))` along the bisector of the segments' up-normals — i.e. each rail end sits `clearance · tan(Δθ/2)` from the floor vertex. Chains are therefore continuous at every angle change (flat→20°→45°→60°...).
- **Valley corners** (slope steepening, e.g. flat→20°→45°): the joint falls slightly *before* the floor vertex, so the rail begins rising early — purely visual, clearance is never violated. **Shoulder corners** (slope flattening, e.g. 45°→20°): the shallower rail miters ~8 px early over the steep floor and locally dips to ~26-27 px above it instead of 36. This is inherent to exact-constant clearance (the alternatives are a step discontinuity at the joint or violating the steeper segment's clearance). It never touches the floor.
- Prev/Next links are set programmatically (`GrindRail.SetChainLinks`), so rail-to-rail transitions use the fast linked path in `TryFindConnectingRail`.
- Asc/Desc mirrors and Catwalk variants share one key: the math derives everything from `FloorSegments`, so mirroring the segments mirrors the chain automatically.
- Tuning `Clearance` must keep every link positive: at 36 the shortest is the 20° transition stub (~49 px); at 56 it was ~41 px. The invariant that matters — perpendicular == clearance on every rail — was validated by a throwaway console harness that called the compiled `RailChainGeometry.Build` and sampled min clearance across each rail's full x-domain, including shoulder overhangs. Re-run a similar check if adding sharper angle changes.

Covered: `RampSection(Desc)`, `GentleRise(Desc)`, `MidRise(Desc)`, `SteepRampAsc/Desc`, `SteepRampAsc/Desc45`, `SteepRampAsc/Desc60` (+ Catwalk variants). Flat rails (FlatRun, HalfPipe, HighFlat, MidFlat, MultiLevel), the StairClimb diagonal rail, and the gap-spanning RailGap rails stay hand-placed in their scenes.

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
5. Negate `GrindRail` rotation (n/a for ramp chain tiles — those scenes carry no rails; see Rail Chains)
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
