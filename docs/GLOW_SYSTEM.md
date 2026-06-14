# Glow System

Per-object border glow for interactable props. Renders a soft luminous outline that follows the object's shape, with configurable color, thickness, and corner rounding.

## Goal

Make interactable props visually distinct from background decor. The glow provides a subtle "this is important" signal without requiring particle effects or animations.

## Core File

`scripts/world/RectGlow.cs` — static factory class. Creates `Polygon2D` nodes with a shader material that computes the glow procedurally.

## API

### GlowParams

All three factory methods accept an optional `GlowParams` override:

| Field | Default | Description |
|---|---|---|
| `Color` | `Colors.White` | Glow tint. Passed to shader as `glow_color`. |
| `BorderThickness` | `3f` | Width of the glow border in pixels. Controls how far the glow extends past the object edge. |
| `CornerRadius` | `3f` | Rounding radius applied to the glow polygon's corners via `smoothstep`. |
| `PeakAlpha` | `0.35f` | Maximum alpha of the glow, reached at the object edge. |

Defaults match the original hardcoded behavior. Passing `null` or omitting the parameter uses defaults.

### Factory Methods

#### `CreateGlow(float width, float height, int zIndex, GlowParams? paramOverride = null)`

Rectangular glow. The glow polygon is a rectangle of the given dimensions, centered at the parent node's position. The shader computes distance from the nearest edge and fades alpha linearly from transparent (polygon edge) to peak (object edge).

```csharp
var glow = RectGlow.CreateGlow(48f, 10f, ZIndex + 1);
parent.AddChild(glow);
```

**Callers:** GrindRail, GrindBoost, BoostPad, LaunchPad, ExtractionZone, RespawnBeacon.

#### `CreateCircleGlow(float radius, int zIndex, GlowParams? paramOverride = null)`

Circular glow. The glow polygon is a 32-segment circle with radius `radius + border_thickness`. The shader computes distance from center and fades alpha based on `R - dist`.

```csharp
var glow = RectGlow.CreateCircleGlow(6f, ZIndex + 1);
parent.AddChild(glow);
```

**Intended for:** MineralPickup (uses `CircleShape2D` collision).

#### `CreateAlphaGlow(Texture2D objectTexture, float padding, int zIndex, GlowParams? paramOverride = null)`

Shape-following glow. The glow polygon is a rectangle encompassing the object with `padding` on each side. The shader samples the object texture's alpha channel to find the nearest opaque pixel, then fades alpha based on distance to that pixel. Includes an `insideMask` so no glow renders inside the object itself.

```csharp
var tex = GD.Load<Texture2D>("res://assets/shock_hazard.png");
var glow = RectGlow.CreateAlphaGlow(tex, 3f, ZIndex + 1);
parent.AddChild(glow);
```

**Intended for:** ShockHazard (zigzag lightning shape), and any future irregular-shape props.

### Backward Compatibility

The original signature `CreateGlow(float width, float height, int zIndex)` is preserved and delegates to the overload with `paramOverride = null`. All 8 existing callers work without changes.

## Shader Details

All three shaders share the same structure:

1. Compute edge distance for the shape (rect / circle / alpha-sampled)
2. Map distance to `t ∈ [0, 1]` where `t = 0` is the polygon edge (transparent) and `t = 1` is the object edge (peak alpha)
3. Apply `cornerFade` via `smoothstep(0.0, corner_radius, max(xDist, yDist))` to round the glow polygon's corners
4. Apply baked grain noise: 32×32 grid, `fract(sin(...) * 43758.5453)`, ±6% amplitude
5. Output `vec4(glow_color.rgb, alpha * grain * glow_color.a)`

### Rect Shader

`edgeDist = min(xDist, yDist)` — standard rectangular distance field. Corner rounding fades the glow near the polygon's corners, preventing sharp 90° protrusions.

### Circle Shader

`edgeDist = (radius + border_thickness) - length(pos - center)` — radial distance. No corner fading needed (circle has no corners).

### Alpha Shader

Samples the object texture in a `(2 * ceil(border_thickness) + 4)²` grid around each fragment. Finds the nearest opaque pixel (`alpha > 0.5`) and uses `length(offset)` as the distance. Falls back to rectangular edge distance if no opaque pixel is found within range. Applies `insideMask = 1 - step(0.5, selfAlpha)` to suppress glow inside the object.

## Z-Index Convention

Glow renders on top of the object. Standard pattern:

```csharp
_glowPoly = RectGlow.CreateGlow(w, h, ZIndex + 1);
```

The object's `Polygon2D` gets `ZIndex = 0` (or explicit), glow gets `ZIndex + 1`.

## Color Integration

Glow color defaults to white. To match the object's palette color:

```csharp
public void ApplyPalette(LevelColorPalette palette, PaletteSlot slot = PaletteSlot.PrimaryLight)
{
    if (_poly != null)
        _poly.Color = palette.Resolve(slot);

    if (_glowPoly != null && _glowPoly.Material is ShaderMaterial mat)
        mat.SetShaderParameter("glow_color", _poly.Color);
}
```

This is how `GrindRail` syncs glow color to its palette slot. Other props can follow the same pattern.

## Current Caller Reference

| Prop | Method | Glow Size | Notes |
|---|---|---|---|
| GrindRail | `CreateGlow(Width+6, Height+6, Z+1)` | 102×16 | Color synced via `ApplyPalette` |
| GrindBoost | `CreateGlow(PadWidth+6, PadHeight+6, Z+1)` | 54×16 | Default white |
| BoostPad | `CreateGlow(PadWidth+6, PadHeight+6, Z+1)` | 54×16 | Default white |
| LaunchPad | `CreateGlow(PadWidth+6, PadHeight+6, Z+1)` | 54×16 | Default white |
| ExtractionZone | `CreateGlow(34, 26, Z+1)` | 34×26 | Default white |
| RespawnBeacon | `CreateGlow(26, 34, Z+1)` | 26×34 | Default white |
| MineralPickup | `CreateGlow(18, 18, Z+1)` | 18×18 | Square mismatch with crystal shape — candidate for `CreateCircleGlow` |
| ShockHazard | `CreateGlow(24, 16, Z+1)` | 24×16 | Rectangle mismatch with zigzag shape — candidate for `CreateAlphaGlow` |

## Migration Candidates

When final art arrives:

- **MineralPickup** → switch to `CreateCircleGlow(6f, ZIndex + 1)` to match `CircleShape2D` collision
- **ShockHazard** → switch to `CreateAlphaGlow(texture, 3f, ZIndex + 1)` to follow the zigzag lightning contour
- Other props with chevron decorations (GrindBoost, BoostPad, LaunchPad) may benefit from `CreateAlphaGlow` if the chevrons become prominent in final art

## Design History

### Corner Rounding

Several approaches were tried for rounding the glow polygon's 90° corners:

1. **Euclidean/min blend** (`mix(length(vec2(xDist, yDist)), min(xDist, yDist), cornerBlend)`) — produced a visible seam where the two distance metrics transitioned
2. **Additive offset** (`min(xDist, yDist) + smoothstep(...) * 1.5`) — made corners brighter instead of fading them
3. **Multiplicative fade** (`smoothstep(0.0, corner_radius, max(xDist, yDist))`) — correct approach. Fades alpha to 0 near the polygon's corners, creating a rounded contour without seam artifacts

The multiplicative fade is the current implementation. The `corner_radius` uniform controls how aggressively corners are rounded.

### Border Profile

Originally used a center-bright gradient (brightest in the middle of the border). Switched to a one-sided ramp (transparent at polygon edge, peak at object edge, hard cutoff inside) to prevent white blob saturation on large objects.

### Grain Noise

Animated noise was considered but rejected for phase 1. The current implementation uses baked ±6% amplitude noise on a 32×32 hash grid, which is sufficient for a subtle shimmer effect without per-frame cost.

### 1×1 White Texture

The `Polygon2D` uses a 1×1 white pixel texture to ensure Godot passes UV data to the shader. Without a texture, UV interpolation can be unreliable.
