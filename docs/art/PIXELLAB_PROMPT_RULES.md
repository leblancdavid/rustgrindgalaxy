# Rust Grind Galaxy - PixelLab Prompt Rules

> This doc defines **how** to generate art with PixelLab. For what the art must **look like**, see **[`ART_STYLE.md`](ART_STYLE.md)**.

## Tool Selection Matrix

| Asset Type | Primary Tool | Backup Tool |
|------------|-------------|-------------|
| Characters (humanoid/robot) | `create_character` | `create_image_pixflux` |
| Character animations | `animate_character` | `animate_image` |
| Hoverboards | `create_image_pixen` | `create_image_pixflux` |
| Props (standalone) | `create_image_pixflux` | `create_image_pixen` |
| Tiles (platformer) | `create_sidescroller_tileset` | `create_tiles_pro` |

## Character Generation

### Player Robot (Mining Robot with Hoverboard)

Use `create_character` with `mode="v3"` for highest quality.

```
Description: boxy mining robot, rectangular body, glowing cyan visor, chest energy core, industrial panel lines, robot
Size: 48
View: side
Outline: single color black outline
Shading: basic shading
Detail: medium detail
```

For accent colors (not runtime-tinted), include in description:
- "glowing cyan visor" 
- "orange amber racing stripes" (for racer variant)

### Generation Parameters

| Parameter | Value | Notes |
|-----------|-------|-------|
| `mode` | `"v3"` | Highest quality, 2-9 generations |
| `size` | `48` | Player height in pixels |
| `view` | `"side"` | Platformer profile view |
| `n_directions` | `4` | N/S/E/W for full coverage |
| `outline` | `"single color black outline"` | Consistent with art style |
| `shading` | `"basic shading"` | Clean pixel art look |
| `detail` | `"medium detail"` | Good detail without clutter |

### Idle Animation

Use `animate_character` with `template_animation_id` or custom `action_description`:

```
action_description: "subtle hover bob, energy core pulsing"
frame_count: 4
directions: ["south"]  (side-view uses south as primary)
```

### Hoverboard Sprite

Generate separately from character using `create_image_pixen`:

```
Description: glowing hoverboard, oval light board shape, bright cyan-white glow, no wheels, floating energy disc
Size: 48x16
View: side
No background: true
```

## Prompt Templates

### Character (Side-View Robot)

```
{robot_description}, side view platformer sprite, pixel art,
single color black outline, basic shading, medium detail,
crisp pixels, no anti-aliasing, 16-bit retro game asset
```

### Hoverboard (Glowing Board)

```
{glowing_board_description}, side view, pixel art,
glowing light board, energy effect, bright core,
single color black outline, crisp pixels, no anti-aliasing
```

## Grayscale + Runtime Tint Strategy

### Generating Grayscale Assets

When generating for runtime tinting:
1. Use neutral gray descriptions: "gray metal", "dark steel", "light gray panels"
2. Avoid describing specific hues — let the runtime palette handle coloring
3. Include fixed accents explicitly: "glowing cyan visor" or "orange amber stripe"

### Accent Colors (Not Tinted)

These elements keep their color through runtime tinting:
- **Cyan** (`#50B8E0` range): Player visor, energy cores
- **Orange Amber** (`#F0A030` range): Racer accents
- **Yellow** (`#F0D030` range): Warning lights

## Iteration Budget Protocol

To avoid credit burn:

1. **ONE `create_character` batch** → present results to user for selection
2. **Visual check** — user picks winner or requests changes
3. **One more batch** only if needed after feedback
4. **Two failed batches → STOP** and reassess approach

### Pre-Generation Checklist

Before each generation:
- [ ] Check `pixellab_get_balance` — if under ~120 generations, avoid pro batches
- [ ] Confirm prompt matches template
- [ ] Confirm size and view parameters
- [ ] Have clear success criteria for the generation

## Color Reduction

After accepting a sprite, run `reduce_colors`:
- Normalizes palette across animation frames
- Makes colors consistent with other assets
- Cost: 0.1 generations (very cheap)

```
pixellab_reduce_colors(
  images_base64: [<sprite_frames>],
  num_colors: 16,
  dithering: "none"
)
```

## Rejection Criteria

Regenerate if:
- Wrong view (not side-view)
- Scale is off (too big/small)
- Accent colors are wrong
- Style doesn't match other assets
- Blurry or anti-aliased edges
- Baked-in shadows in sprite

## File Naming

```
{character}_{action}_{direction}_{frame}.png

Examples:
player_idle_south_00.png
player_run_east_03.png
hoverboard_idle_south_02.png
```
