# Color Palette System

## Goal

Give every mission level a distinct color identity based on the minerals present at its destination, without requiring separate art assets for each possible palette.

The palette is derived from a location's primary and secondary minerals. Props, tiles, and background elements share a common grayscale base and receive their final color through runtime tinting.

## How Palettes Are Determined

Each `DiscoveryRecord` has a `PrimaryMineral` and `SecondaryMineral`. When a mission launches, those two minerals produce a `LevelColorPalette`:

- Primary mineral → **primary color** (dominant architectural tones)
- Secondary mineral → **secondary color** (accents, trim, lighting)

Both primary and secondary have three luminance variants:

| Variant | Usage |
|---|---|
| Dark | Ground fills, large structural areas, deep backdrop |
| Medium | Mid-ground props, wall faces, support beams |
| Light | Highlights, trim edges, foreground detail |

## Mineral Color Reference

Each mineral maps to a hue family. These are the first-pass colors; they can be tuned without changing the system.

| Mineral | Damage Type | Light | Medium | Dark |
|---|---|---|---|---|
| Cinder | Fire | `#F07830` | `#C04718` | `#802808` |
| Verdant | Acid | `#60D060` | `#38A828` | `#186818` |
| Azure | Cold | `#50B8E0` | `#2880B0` | `#105070` |
| Solar | Shock | `#F0D040` | `#C8A018` | `#887008` |
| Lumen | Radiant | `#D0E8F8` | `#88B8D8` | `#4878A0` |
| Umbra | Void | `#B870D0` | `#8040A0` | `#482868` |

These define the **primary** colors when that mineral is the primary mineral, and the **secondary** colors when that mineral is the secondary mineral.

## Palette Data Structure

```csharp
public struct LevelColorPalette
{
    public Color PrimaryDark;
    public Color PrimaryMedium;
    public Color PrimaryLight;
    public Color SecondaryDark;
    public Color SecondaryMedium;
    public Color SecondaryLight;
}
```

A static lookup builds the palette from a primary/secondary mineral pair:

```
GetPalette(MineralType primary, MineralType secondary) → LevelColorPalette
```

Primary mineral fills `PrimaryDark/Medium/Light` and secondary mineral fills `SecondaryDark/Medium/Light` using the table above.

## Default Slot Mapping

Which palette slot each visual element should use. Marked as adjustable during implementation.

| Element | Palette Slot | Notes |
|---|---|---|
| Backdrop / sky fill | PrimaryDark | Full-screen ColorRect behind everything |
| UpperWall / cliff sides | PrimaryMedium | Secondary structural surfaces |
| MidStripe / accent lines | PrimaryLight | Horizontal accent band |
| GroundVisual (tile floor) | SecondaryDark | Large floor Polygon2D |
| GroundTrim (tile edge) | SecondaryLight | Bright edge line on walkable surface |
| GroundRise (wall risers) | SecondaryMedium | Vertical wall faces between floor levels |
| Edge lines | PrimaryLight | Thin highlight edge on floor platforms |
| Catwalk floor | SecondaryDark | Elevated walkway surface |
| Catwalk trim | SecondaryLight | Catwalk edge highlight |
| Rail support posts | SecondaryDark | Vertical support bars under rails |
| Grind rail line | PrimaryLight | Rail top surface |
| Background-layer props | PrimaryDark or PrimaryMedium | Tall distant structures |
| Default-layer props | PrimaryMedium or PrimaryLight | Mid-ground clutter |
| Foreground-layer props | Primary variants | Elements drawn above the player |
| Prop glow / lighting effects | PrimaryLight | Semi-transparent glow behind lit props |
| Hazard glow / energy effects | PrimaryLight | Active hazard warning colors |
| Extraction zone marker | PrimaryLight | Mission exit highlight |

A prop template declares a `PaletteSlot` field so the mapping can vary per prop, not just per layer.

## Tinting Approach

Two options, both viable. The first is recommended.

### Approach A: Color Multiplication (recommended)

All visual elements (props, tile Polygon2D nodes, ColorRects) use a grayscale base color. At runtime the palette color is multiplied in.

**How it works:**

```
finalColor = grayscaleBase × paletteColor
```

A white base (`#FFFFFF`) renders as the full palette color. A darker gray base (`#606060`) renders as a dimmed version of that palette color. This preserves the original brightness relationships while shifting the hue.

**Prop flow:**

1. `PropTemplate.Color` is changed from hardcoded hues to grayscale values (e.g., `RGB(0.9, 0.9, 0.9)` for light, `RGB(0.5, 0.5, 0.5)` for medium, `RGB(0.2, 0.2, 0.2)` for dark).
2. `Prop` gains a `PaletteSlot` field (e.g., `PrimaryMedium`).
3. When the palette is applied, `Prop.PropColor = grayscaleBase × palette[slot]`.
4. `Polygon2D.Color` already supports this directly — no shader needed.

**Tile scene flow:**

1. Each tile's `Polygon2D` nodes (GroundVisual, GroundTrim, etc.) get a grayscale color in the `.tscn` file.
2. During `ApplyMission`, the level iterates its tiles and applies palette multiplication to each visual node based on its slot assignment.
3. Alternatively, tiles can store a small dictionary mapping node names to palette slots.

**Trade-offs:**

- Requires editing all tile `.tscn` files and prop palette colors to grayscale.
- Gives per-element control — ground, trim, backdrop, and every prop can use different slots.
- No shaders, no rendering pipeline changes.
- Integrates naturally with the existing `Polygon2D.Color` and `Prop` code.

### Approach B: Multiply Blend Layer

Add a full-screen `ColorRect` with blend mode `Multiply` as a child of the level root, covering the game view.

**How it works:**

1. The `ColorRect` sits above all level elements with `SelfModulate` set to a blend color.
2. The blend color is derived from the palette (e.g., `PrimaryMedium` at ~50% opacity).
3. Elements are lit/colored normally underneath; the overlay tints everything uniformly.

**Trade-offs:**

- Zero changes to existing tile scenes, props, or code.
- Cannot tint individual elements differently — ground, props, backdrop all get the same tint.
- Darkens elements when multiplying (a white element stays its color, but dark elements get crushed).
- Useful as a quick lighting mood pass, but insufficient for the per-slot control described in this document.

## Integration Points

### `EnvironmentProfile.PaletteKey`

Currently a string label. After this system, the palette is computed directly from the mission's `PrimaryMineral` and `SecondaryMineral`, so `PaletteKey` becomes informational only or is removed.

### `World.cs`

The active `MissionRunData` already carries `PrimaryMineral` and `SecondaryMineral`. After instantiating the level, `World` resolves the palette via `GetPalette()` and passes it to the level.

### `MissionLevel.ApplyMission()`

Each level implementation (e.g., `TileLevelIndustrial`) receives the palette and distributes it:

1. Applies backdrop/wall/stripe colors by their assigned palette slots.
2. Iterates all active tiles and applies palette to their `Polygon2D` visual nodes.
3. Applies the palette reference to the `TileLevelGenerator` so newly spawned tiles also receive it.

### `Prop.cs` / `PropTemplate`

- `PropTemplate` adds a `PaletteSlot` field.
- `Prop` stores a reference to the active `LevelColorPalette` (or the palette is applied once via `PropColor`).
- During `Initialize()` and `UpdateVisual()`, the final color is computed as `grayscaleBase × palette[slot]`.

### `TileLevelGenerator`

When `SpawnFloorProps()` creates a prop, it passes the active palette. The prop uses its template's `PaletteSlot` to pick the correct color.

## Lighting And Glow Props

Props with `IsLighting = true` use palette colors differently:

- The glow `Polygon2D` uses `SecondaryMedium` as its base tint rather than a grayscale value.
- This keeps hazard and accent lighting tied to the secondary mineral's hue.

This can be refined later (e.g., a separate glow palette slot), but the first pass uses `SecondaryMedium` for all glow effects.

## Future Considerations

- **Mission modifiers** could manipulate the palette — `LowVisibility` could desaturate all colors, `SignalInterference` could shift hues.
- **Pixel-art sprites** (if added later) can use `Sprite2D.SelfModulate` to receive the same palette treatment.
- **Palette animation** could slowly shift colors during a mission for environmental storytelling (e.g., a reactor core melting down shifts from Azure to Cinder tones).
- **Per-tile overrides** could let specific tiles declare their own slot assignments through exported node references, though the first pass keeps a simple name-based mapping.
