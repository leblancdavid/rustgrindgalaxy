# Glow System

Two glow families live here: **`RectGlow` border glow** (below) for interactable props, and the **baked-Gaussian sprite glow** (see §Soft Emission Glow) for objects that emit light themselves.

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
| ShockHazard | `CreateGlow(24, 16, Z+1)` | 24×16 | Rectangle mismatch with zigzag shape — candidate for `CreateAlphaGlow` |
| MineralPickup / LootPickup / LootProp | Baked Gaussian (below) | 4× per-variant textures | `LootVisuals.AttachGlow`, `ShowBehindParent=true`, mineral tint inherited from the sprite's `Modulate` |

## Migration Candidates

When final art arrives:

- **ShockHazard** → switch to `CreateAlphaGlow(texture, 3f, ZIndex + 1)` to follow the zigzag lightning contour
- Other props with chevron decorations (GrindBoost, BoostPad, LaunchPad) may benefit from `CreateAlphaGlow` if the chevrons become prominent in final art

Loot props (2026-09): MineralPickup, LootPickup, and LootProp all switched from `RectGlow`/polygon halos to per-variant baked-Gaussian glow sprites (see next section), generated with `tools/make_glow.ps1`.

## Soft Emission Glow (Baked Gaussian Sprite)

`RectGlow` is an attention outline: a bright border ring with the object's interior suppressed. When the object **is** the light — energy boards, lamps, mist auras — use the baked-Gaussian pattern instead: a pre-blurred silhouette texture drawn additively by a child sprite. No shader code at runtime.

### When to use which

| Need | Pattern |
|---|---|
| "This prop is interactable" outline ring | `RectGlow.CreateGlow` / `CreateCircleGlow` / `CreateAlphaGlow` |
| Object glows/emits light, soft halo all around | Baked Gaussian sprite (this section) |
| Whole-scene bloom | `WorldEnvironment` glow layer — deliberate, not currently used |

### Recipe (once per asset, CPU-side)

1. **Binarize the source mask.** Art that ships with variable alpha (the board's swirl wisps) must be thresholded to a solid silhouette first (`alpha > 102` for the board), or the glow inherits the texture's mottling.
2. **Work at 4× resolution** (nearest upscale, same world area): gives the Gaussian sub-texel smoothness and room for the halo to fade without clipping. Board: 48px frames → 192px glow canvas.
3. **Separable Gaussian blur on the alpha channel.** σ in output px ≈ desired halo reach ÷ 2.5 (board: σ=9 ≈ 2–3 world-px past the edge), kernel radius 3σ, edges clamp (no wrap).
4. **Peak-normalize** the blurred alpha to 255 — the blur eats ~1.5% of the peak at σ=9.
5. **Save as pure-white RGB + blurred alpha**, e.g. `assets/props/minerals/glow/mineral_00.png`. The generator is now a reusable script: `tools/make_glow.ps1 -SrcDir <art dir> -OutDir <art dir>\glow -Factor 4 -Sigma 9` (binarize alpha > 128, nearest 4× upscale, separable Gaussian σ=9 on output px, peak-normalize). **The output canvas gets `-Pad` transparent output px around the silhouette (default 3σ)** — without it the halo clips at the texture edge and reads as a box. Glow filenames mirror the art they belong to. The glow `Sprite2D` stays centered at scale 0.25: symmetric padding keeps the silhouette aligned with the art and the halo simply extends past the art's bounds.

### Runtime wiring (per instance)

- `Sprite2D` as a **child of the glowing node** — it inherits position/rotation/scale/bob/flip automatically, so zero transform-mirroring code (see the AGENTS.md "node rotation is overwritten" gotcha). Children draw after their parent, giving "on top of" placement.
- `CanvasItemMaterial` with `BlendMode = Add` for true light emission (dark background + alpha-blended white reads as gray; additive reads as glow).
- **Local scale = source-world-px / asset-px**: the 192px board glow uses `0.25` since the parent is already 0.75-scaled. Keep a `BaseScale` const and wrap it in an exported size multiplier.
- Strength and tint via **`SelfModulate`** (`A = strength × color.A`). Note the modulate chain flows down from the parent: a parent opacity knob (like `BoardOpacity`) dims the glow too — desirable for a unified light object.
- Guard with `ResourceLoader.Exists(BoardGlowTexPath)` and skip silently, matching `LoadFrames` behavior: **an un-imported PNG means no glow and no error** — reload the editor when a newly added glow asset doesn't show up.
- Texture filtering: the halo is a smooth alpha ramp, so the sprite needs Linear filtering (either set it on the parent and inherit, or set it on the glow node; project default is Nearest).

### Worked example

`PlayerController.Hover.cs`: glow created in the `_animInit` block, per-frame `SelfModulate`/`Scale` push in `UpdateBoardVisual()`. Exports: `BoardGlowScale` (size multiplier), `BoardGlowStrength` (brightness), `BoardGlowColor` (tint — keep this and the tinted object's modulate driven by the same future palette value).

`LootVisuals.AttachGlow()` (set-static version): the glow `Sprite2D` is a child of the loot art sprite with local `Scale = 0.25` (base is 4× texture, so it sits exactly on the art), additive material, `ShowBehindParent = true` (halo around the silhouette, crisp pixel detail on top), `SelfModulate.A = 0.6`. Tinting is free: the child inherits the parent sprite's `Modulate` (mineral color for ores/crystals/patches, warm brown for crates, near-white for scrap). The parent's own scale (`PickupVisualScale` on pickups, `PropVisualScale` on props) automatically sizes the halo.

### Anti-patterns learned

- **Dilation-sum fragment shader** (average of N hard-silhouette samples progressively zoomed about center): the fade follows a center-scaled copy of the shape, which reads angular on elongated sprites, and clips flat where the halo outruns the texture padding. Replaced by the baked Gaussian.
- **Shader blur of the board sprite** for softness: only smooths interior gradients, hard alpha edges stay hard.
- **`create_1_direction_object` / PixelLab for the glow**: glow textures are derived from our own art; no generation needed.

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
