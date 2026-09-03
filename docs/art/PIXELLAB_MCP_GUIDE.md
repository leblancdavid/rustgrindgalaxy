# Rust Grind Galaxy - PixelLab MCP Guide

> Adapted from KBTV project. For prompt templates and generation rules, see **[`PIXELLAB_PROMPT_RULES.md`](PIXELLAB_PROMPT_RULES.md)**.

## MCP Server Connection

PixelLab MCP server is connected via opencode. The following tools are available:
- `pixellab_*` — all PixelLab generation tools

## Available Tools

### Image Generation

| Tool | Use Case | Cost |
|------|----------|------|
| `pixellab_create_image_pixflux` | Props, general assets | 1 gen |
| `pixellab_create_image_pixen` | Tiny sprites (≤32px) | 1 gen |
| `pixellab_create_image_pro` | Multiple candidates, best quality | 20-40 gen |
| `pixellab_edit_image` | Edit existing PNG | 20-40 gen |
| `pixellab_edit_image_pixen` | Quick edit, tiny sprites | 1 gen |
| `pixellab_animate_image` | Animate loose sprites | 1 gen |

### Characters

> **Only for bipedal/legged characters.** The rig is a humanoid `mannequin`; it re-adds legs and will not preserve legless/custom silhouettes. For our cast use the **image pipeline** + **`animate_image`** (see above / `PIXELLAB_PROMPT_RULES.md`).

| Tool | Use Case | Cost |
|------|----------|------|
| `pixellab_create_character` | **Bipedal** character with directions | 1-9 gen (v3) |
| `pixellab_animate_character` | Animate **bipedal** character (template/custom) | ~1-4 gen/dir |
| `pixellab_create_character_state` | Bipedal character variant | 20-40 gen |

### Objects

| Tool | Use Case | Cost |
|------|----------|------|
| `pixellab_create_1_direction_object` | Single direction object | 20-40 gen |
| `pixellab_create_8_direction_object` | 8-angle object | 20-40 gen |
| `pixellab_animate_object` | Animate existing object | ~1-4 gen/dir |

### Tilesets

| Tool | Use Case | Cost |
|------|----------|------|
| `pixellab_create_sidescroller_tileset` | Platformer tiles | 2-3 gen |
| `pixellab_create_tiles_pro` | Terrain tiles | 1-4 gen |
| `pixellab_create_topdown_tileset` | Top-down Wang tiles | 3-4 gen |
| `pixellab_create_building_kit` | Walls/floors | varies |

### Utility (0.1 gen each)

| Tool | Use Case |
|------|----------|
| `pixellab_reduce_colors` | Quantize to palette |
| `pixellab_correct_pixelart` | Fix stray pixels |
| `pixellab_unzoom_image` | Downscale upscaled art |
| `pixellab_get_balance` | Check credits |

## Quick Reference

### Generate a legless character (our cast) — image pipeline
```
pixellab_create_image_pro(
  description: "upright hovering robot, side view facing right, clearly NO legs, body ends in a glowing light-ring beam, pixel art",
  width: 64, height: 64, no_background: true
)
# pick a candidate → edit_image to refine → correct_pixelart → downscale to 48px
```
`create_character` (below) is **bipedal-only** — its `mannequin` skeleton re-adds legs in animations.

### Animate FROM our base sprite (keeps it legless) — `animate_image`
```
pixellab_animate_image(
  first_frame_url: "https://api.pixellab.ai/mcp/images/<job>/download",
  action: "the robot launches upward, beam flares, body stays legless and rigid, no legs",
  frame_count: 8,
  no_background: true
)
```
`animate_character` only suits bipedal characters and requires a registered `character_id` (our free-image sprites have none). See `PIXELLAB_PROMPT_RULES.md`.

### Reduce Colors
```
pixellab_reduce_colors(
  images_base64: [<png_base64>],
  num_colors: 16
)
```

## Generation Costs

| Operation | Generations |
|-----------|-------------|
| Character creation (v3) | 2-9 |
| Character animation (v3) | ~1/direction |
| Image generation (pixflux/pixen) | 1 |
| Image generation (pro) | 20-40 |
| Color reduction | 0.1 |

## Getting Help

```
pixellab_agent_help(question: "How do I generate a character with specific accent colors?")
```

## Reporting Issues

```
pixellab_agent_feedback(
  tool_name: "create_character",
  feedback_type: "bug",
  message: "Description of the issue"
)
```
