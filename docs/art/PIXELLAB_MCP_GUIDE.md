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

| Tool | Use Case | Cost |
|------|----------|------|
| `pixellab_create_character` | Full character with directions | 1-9 gen (v3) |
| `pixellab_animate_character` | Animate existing character | ~1-4 gen/dir |
| `pixellab_create_character_state` | Character variant | 20-40 gen |

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

### Generate Character (v3 mode)
```
pixellab_create_character(
  description: "boxy mining robot, side view...",
  mode: "v3",
  size: 48,
  view: "side",
  n_directions: 4
)
```

### Animate Character
```
pixellab_animate_character(
  character_id: "<uuid>",
  action_description: "idle hover bob",
  directions: ["south"],
  frame_count: 4
)
```

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
