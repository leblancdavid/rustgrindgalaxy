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

### Runtime Tint System (Primary Approach)

Assets are generated as **grayscale** and receive color at runtime via the mineral-driven `LevelColorPalette`. See [`docs/COLOR_PALETTE_SYSTEM.md`](../COLOR_PALETTE_SYSTEM.md) for full details.

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

### Player Robot - Boxy Mining Robot

The player is a **boxy mining robot** with a hoverboard. Key features:
- Boxy/rectangular body (mining robot aesthetic)
- No legs — floats on a separate hoverboard sprite
- Energy beam connects body to board
- Glowing visor and chest energy core as fixed accents
- Panel lines and industrial details

### Side-View Platformer Projection

All characters use **side-view** (profile) projection:
- Dominant horizontal axis (left-right movement)
- Clear silhouette readable against backgrounds
- Distinct front/back/profile views

### Character Size Reference

| Character | Height | Notes |
|-----------|--------|-------|
| Player | ~48px | Main character, boxy robot |
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

- **Frame Rate**: 8-12 FPS for retro feel
- **Idle animations**: Subtle hover bob for hoverboard, energy pulse for robot
- **Run animations**: 4-6 frames
- **Attack animations**: 3-5 frames with clear wind-up and impact

## Asset Structure

```
assets/
├── characters/
│   ├── player/
│   │   ├── idle/
│   │   ├── run/
│   │   └── attack/
│   ├── raider/
│   └── drone/
├── hoverboards/
│   └── player/
│       ├── idle/
│       └── run/
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

## Rejection Criteria

Reject and regenerate if:
- **Wrong projection** — not side-view
- **Wrong scale** — too big or too small relative to other assets
- **Saturated colors** — colors that don't work with runtime tinting
- **Blurry edges** — anti-aliased or fuzzy pixel art
- **Inconsistent style** — doesn't match other assets
- **Baked-in shadows** — ground shadows or drop shadows in sprite
