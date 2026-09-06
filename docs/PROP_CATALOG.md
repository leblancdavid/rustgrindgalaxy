# Prop Catalog

All props currently defined in the project, grouped by category. Intended as a reference for generating real pixel art later.

Size notation: `width × height` in pixels. Weight controls spawn probability relative to other props in the same palette.

---

## 1. Decorative Floor Props

Spawned automatically by `LevelTile.SpawnFloorProps()` along walkable floor segments. Each prop is a `Prop` node backed by a `Polygon2D` rectangle.

### 1.1 Industrial Palette (`PropPalettes.Industrial`)

27 props. Theme: factory pipes, vents, consoles, machinery.

**Background Layer** (Z -2, behind terrain):

| ID | Size | Brightness | Weight | Glow | Art Description |
|----|------|-----------|-------|------|-----------------|
| IND-B1 | 12×96 | Dark (0.18) | 5 | Top glow | Narrow exhaust stack or vent pipe with faint rim light at top |
| IND-B2 | 16×108 | Dark (0.22) | 4 | Top glow | Rusty pipe riser, slightly wider, warm cap glow |
| IND-B3 | 18×120 | Dark (0.25) | 4 | No | Structural I-beam, full-height dark silhouette |
| IND-B4 | 96×18 | Medium (0.40) | 4 | No | Horizontal support girder spanning the background |
| IND-B5 | 108×16 | Medium (0.35) | 4 | No | Distant catwalk or scaffolding ledge |
| IND-B6 | 30×66 | Dark (0.22) | 5 | No | Cooling tower or cylindrical tank silhouette |
| IND-B7 | 48×84 | Medium (0.28) | 4 | No | Large industrial tank or furnace block |
| IND-B8 | 20×84 | Medium (0.28) | 4 | No | Control panel tower or thin silo |
| IND-B9 | 10×72 | Dark (0.15) | 4 | No | Antenna mast or warning light pole |

**Default/Mid-Ground Layer** (Z 0, at player level):

| ID | Size | Brightness | Weight | Art Description |
|----|------|-----------|-------|-----------------|
| IND-M1 | 36×36 | Medium (0.40) | 7 | Metal shipping crate with riveted corners |
| IND-M2 | 48×24 | Medium (0.30) | 7 | Equipment case or floor panel |
| IND-M3 | 24×48 | Medium (0.25) | 6 | Upright locker or small cabinet |
| IND-M4 | 72×60 | Medium (0.38) | 6 | Generator or pump housing |
| IND-M5 | 84×42 | Medium (0.42) | 5 | Long industrial console or ductwork section |
| IND-M6 | 24×24 | Medium (0.35) | 5 | Small junction box or floor vent |
| IND-M7 | 18×18 | Medium (0.28) | 5 | Pipe fitting or bolt cluster |
| IND-M8 | 30×48 | Light (0.55) | 5 | Barrel or compressed gas canister |
| IND-M9 | 42×20 | Medium (0.45) | 5 | Floor plate or ramp segment |
| IND-M10 | 60×18 | Medium (0.50) | 4 | Hose loop or cable tray on the ground |
| IND-M11 | 20×42 | Light (0.60) | 4 | Fire extinguisher station or warning post |
| IND-M12 | 18×36 | Medium (0.38) | 4 | Tool rack or small transformer |
| IND-M13 | 10×10 | Medium (0.25) | 3 | Bolt head, rivet, or floor warning marker |
| IND-M14 | 12×12 | Bright yellow | 3 | Yellow hazard marker light |
| IND-M15 | 12×12 | Bright cyan | 3 | Cyan status light or coolant drip |

**Foreground Layer** (Z 4, drawn above player):

| ID | Size | Brightness | Weight | Art Description |
|----|------|-----------|-------|-----------------|
| IND-F1 | 26×92 | Bright (0.85) | 4 | Foreground support column close to camera |
| IND-F2 | 80×32 | Medium (0.50) | 4 | Foreground beam or duct crossing the view |
| IND-F3 | 52×40 | Medium (0.55) | 4 | Foreground machinery or console blocking lower view |
| IND-F4 | 40×66 | Medium (0.35) | 4 | Foreground pipe cluster or cabinet |
| IND-F5 | 66×26 | Light (0.65) | 4 | Foreground vent cover or ledge |
| IND-F6 | 34×52 | Medium (0.30) | 3 | Foreground coolant unit or vegetation tank |
| IND-F7 | 10×10 | Bright yellow (0.90) | 3 | Foreground indicator light |
| IND-F8 | 14×14 | Bright red (0.95) | 3 | Foreground alarm beacon or warning light |

### 1.2 Derelict Palette (`PropPalettes.Derelict`)

16 props. Theme: corroded, collapsed, rusted debris.

**Background Layer** (Z -2):

| ID | Size | Brightness | Weight | Glow | Art Description |
|----|------|-----------|-------|------|-----------------|
| DER-B1 | 12×90 | Medium (0.30) | 5 | Top glow | Rusted pipe or dangling cable with faint glow |
| DER-B2 | 24×108 | Medium (0.25) | 4 | No | Corroded beam or collapsed support |
| DER-B3 | 72×20 | Medium (0.35) | 4 | No | Fallen girder or debris shelf |
| DER-B4 | 12×60 | Dark (0.20) | 4 | No | Broken antenna or rebar sticking up |
| DER-B5 | 36×54 | Medium (0.40) | 4 | No | Tilted panel or half-collapsed wall section |

**Default/Mid-Ground Layer** (Z 0):

| ID | Size | Brightness | Weight | Art Description |
|----|------|-----------|-------|-----------------|
| DER-M1 | 54×28 | Medium (0.28) | 6 | Crumbled wall segment or rubble pile |
| DER-M2 | 36×30 | Medium (0.45) | 6 | Broken crate or scrap heap |
| DER-M3 | 24×24 | Medium (0.40) | 5 | Loose brick or twisted metal block |
| DER-M4 | 18×18 | Medium (0.32) | 5 | Scrap piece or broken component |
| DER-M5 | 20×42 | Medium (0.35) | 5 | Bent pipe or shattered control panel |
| DER-M6 | 12×12 | Dark (0.22) | 3 | Charred debris or bolt cluster |
| DER-M7 | 10×10 | Medium gold (0.60) | 3 | Flickering warning light or spark source |

**Foreground Layer** (Z 4):

| ID | Size | Brightness | Weight | Art Description |
|----|------|-----------|-------|-----------------|
| DER-F1 | 66×26 | Medium (0.50) | 4 | Fallen beam or collapsed railing |
| DER-F2 | 34×60 | Medium (0.55) | 4 | Broken support post close to camera |
| DER-F3 | 46×32 | Medium (0.45) | 3 | Scrap pile or machinery husk |
| DER-F4 | 20×16 | Bright gold (0.80) | 3 | Glowing fragment or slag piece |

### 1.3 Surface Palette (`PropPalettes.Surface`)

17 props. Theme: rock formations, crystal deposits, alien terrain.

**Background Layer** (Z -2):

| ID | Size | Brightness | Weight | Glow | Art Description |
|----|------|-----------|-------|------|-----------------|
| SUR-B1 | 12×84 | Medium (0.20) | 5 | Top glow | Crystal formation or rock spire with mineral glow |
| SUR-B2 | 24×84 | Medium (0.25) | 4 | No | Stone pillar or eroded column |
| SUR-B3 | 72×20 | Medium (0.35) | 4 | No | Rock ledge or cliff overhang in distance |
| SUR-B4 | 16×72 | Dark (0.20) | 4 | No | Sharp rock spire or petrified wood |
| SUR-B5 | 42×48 | Medium (0.38) | 4 | No | Distant boulder or small butte |

**Default/Mid-Ground Layer** (Z 0):

| ID | Size | Brightness | Weight | Art Description |
|----|------|-----------|-------|-----------------|
| SUR-M1 | 30×28 | Medium (0.45) | 7 | Loose stone or gravel pile |
| SUR-M2 | 48×20 | Medium (0.35) | 7 | Flat slab or bedrock outcrop |
| SUR-M3 | 60×30 | Medium (0.42) | 6 | Large rock formation |
| SUR-M4 | 24×20 | Medium (0.40) | 5 | Broken rock fragment |
| SUR-M5 | 18×16 | Medium (0.32) | 5 | Small stone or mineral chunk |
| SUR-M6 | 20×30 | Medium (0.28) | 5 | Upright stone or stalagmite stub |
| SUR-M7 | 12×12 | Medium (0.50) | 3 | Light-colored mineral deposit |
| SUR-M8 | 10×10 | Bright gold (0.60) | 3 | Glinting ore fragment or crystal shard |

**Foreground Layer** (Z 4):

| ID | Size | Brightness | Weight | Art Description |
|----|------|-----------|-------|-----------------|
| SUR-F1 | 52×32 | Medium (0.35) | 5 | Foreground rock shelf or mossy stone |
| SUR-F2 | 40×26 | Medium (0.50) | 4 | Flat rock close to camera |
| SUR-F3 | 30×46 | Medium (0.40) | 4 | Foreground standing stone or crystal |
| SUR-F4 | 20×16 | Bright gold (0.70) | 3 | Bright mineral fragment nearby |

---

## 2. Tile Structural Visuals

Defined per-tile in `.tscn` scene files. These form the core level architecture.

| Name | Typical Size | Layer | Description |
|------|-------------|-------|-------------|
| **GroundVisual** | 1280 × 236 | Z 0 | Main walkable terrain fill. Polygon2D under the surface line. |
| **GroundTrim** | 1280 × 3 | Z 0 | Thin bright strip along the walkable surface edge. |
| **Catwalk floor** | varies | Z 1 | Elevated walkway surface fill (catwalk tile variants). |
| **Catwalk trim** | varies | Z 1 | Bright edge line on catwalk surface. |
| **Rail supports** | 6 × (height) | Z -1 | Vertical support bars under each GrindRail endpoint. Spawned by `LevelTile.SpawnRailSupports()`. |
| **Backdrop** | fills view | Z -3 | Full-screen background ColorRect. Theme-tinted. |
| **UpperWall** | fills width × 26 | Z -2 | Ceiling or cliff top strip, below ground surface height. |
| **MidStripe** | fills width × 4 | Z -1 | Horizontal accent band. Mid-level of viewport. Used as ambient color cue. |

---

## 3. Interactive Gameplay Objects

These are standalone scene objects, not part of the prop palette system.

### BoostPad (`BoostPad.cs`)

| Property | Value |
|----------|-------|
| Size | 48 × 10 |
| Collision | RectangleShape2D, Area2D trigger |
| Visual | Green `Polygon2D` body + two white triangle chevrons pointing right |
| Color | Hardcoded green `(0.2, 0.7, 0.2)` with white chevrons |
| Function | Speed boost on floor contact (2x speed, 1.5s duration) |

### LaunchPad (`LaunchPad.cs`)

| Property | Value |
|----------|-------|
| Size | 48 × 10 |
| Collision | RectangleShape2D, Area2D trigger |
| Visual | Orange `Polygon2D` body + two white up-chevrons |
| Color | Hardcoded orange `(0.9, 0.5, 0.1)` with white chevrons |
| Function | Vertical launch on floor contact (700 px/s upward) |

### GrindBoost (`GrindBoost.cs`)

| Property | Value |
|----------|-------|
| Size | 48 × 10 |
| Collision | RectangleShape2D, Area2D trigger |
| Visual | Green `Polygon2D` body + two white chevrons, Z 2 |
| Color | Hardcoded green `(0.2, 0.8, 0.3)` with white chevrons |
| Function | Rail-mounted speed boost (2x, 1.5s, requires grinding) |

### ShockHazard (`ShockHazard.cs`)

| Property | Value |
|----------|-------|
| Collision | Area2D, trigger |
| Visual | Single `Polygon2D` child node named "Visual" |
| Color | Switched by palette key in `SetTheme()`: industrial=yellow, rocky=orange, frozen=cyan, derelict=purple |
| Function | Deals 1 damage on body contact (any layer) |

### MineralPickup (`MineralPickup.cs`)

| Property | Value |
|----------|-------|
| Collision | Area2D, trigger |
| Visual | Single `Polygon2D` child node named "Visual" |
| Color | Determined by `MineralType` via `GetMineralColor()` (Cinder=red, Verdant=green, Azure=blue, Solar=yellow, Lumen=white, Umbra=purple) |
| Function | Adds mineral to `World.CollectedMinerals`, despawns |

### RespawnBeacon (`RespawnBeacon.cs`)

| Property | Value |
|----------|-------|
| Visual | Two Polygon2D nodes: "Visual" (body) + "Beam" (vertical effect) + "Label" |
| Color idle | Gray `(0.35, 0.35, 0.38)` |
| Color active | Blue `(0.3, 0.7, 1.0)` with cyan beam `(0.5, 0.8, 1.0, 0.6)` |
| Function | Sets respawn point when player passes it. Beam plays on activate. |

### ExtractionZone (`ExtractionZone.cs`)

| Property | Value |
|----------|-------|
| Visual | Single `Polygon2D` child node named "Visual" |
| Color inactive | Dark gray `(0.29, 0.30, 0.36, 0.55)` |
| Color active | Bright green `(0.32, 0.96, 0.70, 0.90)` |
| Function | Completes mission on body entry when active. Active state toggled by mineral target. |

### GrindRail (`GrindRail.cs`)

| Property | Value |
|----------|-------|
| Visual | `Line2D` child node |
| Dimensions | Variable width (exported), 10px height collision, Z 1 |
| Color | Rail line color set via editor in each tile scene; procedural chains are tinted by `ApplyVisualPalette` |
| Creation | Hand-placed in flat/stair/gap tile scenes; ramp chain tiles generate them at placement time via `LevelTile.BuildRailChains()` (see `TILE_DESIGN.md#rail-chains`) |
| Function | Rail-grind interaction zone. Player snaps to rail when overlapping within distance threshold. |

---

## 4. Palette Conversion Status

Props that need their hardcoded colors converted to grayscale + palette slot for the color palette system:

| Object | Current Colors | Needs Conversion | Priority |
|--------|---------------|-----------------|----------|
| Decorative props (Industrial palette) | Hardcoded per entry | Yes — all 27 entries | High |
| Decorative props (Derelict palette) | Hardcoded per entry | Yes — all 16 entries | High |
| Decorative props (Surface palette) | Hardcoded per entry | Yes — all 17 entries | High |
| Tile GroundVisual | Hardcoded per tile scene | Yes — 28 tile scenes | High |
| Tile GroundTrim | Hardcoded per tile scene | Yes — 28 tile scenes | High |
| Backdrop ColorRect | Switched by palette key | Partial — port to palette slot | Medium |
| UpperWall ColorRect | Switched by palette key | Partial — port to palette slot | Medium |
| MidStripe ColorRect | Switched by palette key | Partial — port to palette slot | Medium |
| Rail supports (`RailSupportColor`) | Hardcoded gray | Yes | Medium |
| ShockHazard colors | Switched by palette key | Yes — port to use palette slot | Medium |
| BoostPad body color | Hardcoded green | Yes — port to use palette slot | Low |
| LaunchPad body color | Hardcoded orange | Yes — port to use palette slot | Low |
| GrindBoost body color | Hardcoded green | Yes — port to use palette slot | Low |
| ExtractionZone active/inactive | Hardcoded green/gray | Yes — port to use palette slot | Low |
| RespawnBeacon colors | Hardcoded gray/blue | No — checkpoint identity should remain distinct | None |
| MineralPickup colors | Mineral-type mapping | No — mineral identity must remain readable | None |
| GrindRail line color | Editor-set per tile | Yes — port to use palette slot | Low |
