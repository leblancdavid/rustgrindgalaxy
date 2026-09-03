# Rust Grind Galaxy - Art Style Guide

> **Prompt rules are NOT in this doc.** For generating art with PixelLab (templates, tool choice, iteration budget) see **[`PIXELLAB_PROMPT_RULES.md`](PIXELLAB_PROMPT_RULES.md)**. This doc defines what the art must look like; that doc defines how to get it out of the generator.

## Visual Direction

**Pixel Art Style** - Retro 2D platformer with a **sci-fi industrial** overlay. The game takes place in mining facilities and industrial complexes. Robots, hoverboards, energy beams, and mineral deposits.

### Project Settings
- **Resolution**: 1280x720 (16:9), stretched from 640x360 internal
- **Render Mode**: Forward Plus with pixel-perfect texture filtering
- **Character Size**: ~48px tall (player), ~40-48px (enemies), ~24-32px (drones)
- **Texture Filter**: Nearest (no interpolation)

### Aesthetic Goals
- Industrial sci-fi atmosphere — rusted metal, glowing energy, tech panels
- **High contrast, distinct silhouettes** — readable at small sizes
- Runtime color palette system driven by mineral types (Cinder, Verdant, Azure, Solar, Lumen, Umbra)
- Single dominant style across all characters and props

## Color Palette Strategy

### Runtime Tint System — **scope: environment only**

> **Only environment props, tiles, and backgrounds use runtime tinting.** They are generated **grayscale** and tinted per-level. **Characters keep their baked identity colors** (rust body, orange visor, cyan beam) and are NOT runtime-tinted — do not desaturate the player to grayscale.

Environment assets receive color at runtime via the mineral-driven `LevelColorPalette`. See [`docs/COLOR_PALETTE_SYSTEM.md`](../COLOR_PALETTE_SYSTEM.md) for full details.

**How it works:**
```
finalColor = grayscaleBase × paletteColor
```

A white base renders as full palette color. A darker gray renders as dimmed palette color.

### Fixed Accent Colors

Certain elements should **NOT** be tinted — they use fixed accent colors:
- Energy glows and emitters
- Robot eyes/visors
- Hoverboard core glow
- Warning lights

These are generated with specific accent colors baked in.

### Grayscale Base Ramp

| Role | Grayscale Value |
|------|----------------|
| Dark shadow | `#1A1A1A` |
| Dark fill | `#2D2D2D` |
| Mid-dark | `#4A4A4A` |
| Mid | `#6E6E6E` |
| Mid-light | `#9A9A9A` |
| Light highlight | `#C8C8C8` |
| White highlight | `#E8E8E8` |

## Character Design

### Player Robot - Hovering Mech ("c13")

The player is a **rigid hovering robot** with **no legs**, riding a separate hoverboard. Final look:
- Angular rust/red armored body, orange-amber visor and chest core (fixed accents)
- No legs — the lower body ends in a **swirling cyan light-ring tractor beam** (UFO hover, not a rocket flame)
- Rides a separate **grayscale snowboard-shaped** hoverboard sprite; the beam glows down onto it
- Dynamic arms (used for balance in grind/flip poses)

### Generation Method: image pipeline, NOT `create_character`

Legless characters MUST be generated as a free **image**, not via `create_character`:
- `create_character` builds on the **`mannequin` skeleton**; every template animation then **re-adds legs** (the failure we hit). The static pose may look legless but the rig is bipedal.
- Instead: `create_image_pro` (multiple candidates, side view) → pick → `edit_image` to refine (e.g. swap flame→ring beam) → `correct_pixelart` → downscale to 48px → import.

### Side-View Platformer Projection

All characters use **side-view** (profile), **east-facing** as the only stored view:
- Dominant horizontal axis (left-right movement)
- Clear silhouette readable against backgrounds
- **West = horizontal flip in-engine** — do not store a west sprite
- Animations keep the body centered; physics/in-engine transform handles position + rotation

### Character Size Reference

| Character | Height | Notes |
|-----------|--------|-------|
| Player | ~48px | Boxy→ sleek hovering racer mech (c13); east-facing, west = flip |
| Raider | ~44px | Humanoid enemy |
| Drone | ~28px | Flying enemy, smaller |

## Prop Visual Style

### Side-View Platformer Rules

1. **Profile view dominant** — characters and props seen from the side
2. **Clear silhouette** — readable shapes, no ambiguous forms
3. **Consistent scale** — same pixel weight across all assets
4. **No smooth gradients** — pixel art, hard edges, dithered shadows only if needed
5. **Single outline color** — black outline on silhouette
6. **Fill the canvas** — subject occupies 90-100% of sprite box

### Industrial Sci-Fi Aesthetic

- Rusted metal textures (grayscale values)
- Panel lines and tech details
- Glowing energy elements (fixed accent colors)
- Industrial shapes — boxes, pipes, vents

## Animation Guidelines

Hybrid approach — **in-engine procedural motion + PixelLab pose/VFX frames**:

- **Frame Rate**: 8-12 FPS for retro feel
- **Whole-body transforms stay in-engine** (never AI-redraw the body — it drifts / adds legs): hover bob, facing flip, jump/fall stretch, landing squash, air spin, grind tilt/bob, damage flash
- **Beam / effects**: ring ripple overlay in-engine; `animate_image` (from the base sprite) for beam flares and pose motion — it animates *our image*, no skeleton, so legs can't appear
- **Jump**: AI frames = beam flare + rise (body rigid)
- **Grind**: AI pose = low crouch, arms out for balance (rotation/bob in-engine)
- **Front flip**: AI pose = tuck/crouch forward (the spin is done in-engine)
- **Back flip**: AI pose = lean back / arch (the spin is done in-engine)


## Asset Structure

```
assets/
├── characters/
│   ├── player/
│   │   ├── player_east.png          # 48px legless body + ring beam (west = flip)
│   │   ├── beam_ring.png            # grayscale hover-ring for the swirl overlay
│   │   └── anim/
│   │       ├── idle/    idle_00..08.png      # body still, beam rotates (loop)
│   │       ├── move/    move_00..06.png      # forward lean + brighter beam (intro once, then tail-loop)
│   │       ├── charge/  charge_00..06.png     # crouch + beam gathers (ratio build, tail-loop at full)
│   │       ├── jump/    jump_00..08.png
│   │       ├── grind/   grind_00..NN.png
│   │       ├── backflip/ flip_back_00..NN.png
│   │       └── frontflip/ flip_front_00..NN.png
│   ├── raider/          # TODO regenerate legless to match
│   └── drone/
├── hoverboards/
│   └── player/
│       └── hoverboard_snowboard.png # grayscale board, laid flat
└── props/
```

## Godot 4.x Implementation Notes

### project.godot Settings
```toml
[display]
window/size/viewport_width=1280
window/size/viewport_height=720
window/stretch/mode="viewport"

[rendering]
textures/canvas_textures/default_texture_filter=0
```

### Sprite Import
- PNG files with alpha channel
- Use `Import > Texture > Filter > Nearest` for pixel-perfect
- y_sort_enabled for proper draw order
- **New PNGs must be reimported by the Godot editor before `GD.Load`/`ResourceLoader.Exists` succeed at runtime** — drop frames into `res://`, reload the project in-editor, then run. (Runtime anim loaders guard on `Exists` and silently skip un-imported frames.)
- Keep dev scratch (candidate sheets, previews) in a `.gdignore`'d folder so it never imports into the build.

## Rejection Criteria

Reject and regenerate if:
- **Wrong projection** — not side-view
- **Wrong scale** — too big or too small relative to other assets
- **Saturated colors** — *(environment props/tiles only)* hues that break runtime tinting; characters intentionally keep baked accents
- **Blurry edges** — anti-aliased or fuzzy pixel art
- **Inconsistent style** — doesn't match other assets
- **Baked-in shadows** — ground shadows or drop shadows in sprite
