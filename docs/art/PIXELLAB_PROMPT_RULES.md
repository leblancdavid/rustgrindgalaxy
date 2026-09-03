# Rust Grind Galaxy - PixelLab Prompt Rules

> This doc defines **how** to generate art with PixelLab. For what the art must **look like**, see **[`ART_STYLE.md`](ART_STYLE.md)**.

## Tool Selection Matrix

| Asset Type | Primary Tool | Backup Tool |
|------------|-------------|-------------|
| Characters (rigid/legless/custom silhouette) | `create_image_pro` (+ `edit_image` to refine) | `create_image_pixflux` |
| Characters (bipedal, legs ok) | `create_character` | — |
| Character **poses / motion from our sprite** | `animate_image` | `animate_character` (bipedal only) |
| VFX (beam, slash, impact) | `animate_image` (simple element) | `create_image_pixflux` |
| Hoverboards | `create_image_pixflux` | `create_image_pixen` |
| Props (standalone) | `create_image_pixflux` | `create_image_pixen` |
| Tiles (platformer) | `create_sidescroller_tileset` | `create_tiles_pro` |

> **Legless / non-humanoid rule:** do **NOT** use `create_character` — it rigs the `mannequin` skeleton and every later animation re-adds legs. Generate the body as a **free image** (`create_image_pro` → pick → `edit_image` → `correct_pixelart`) and animate it with **`animate_image`**, which moves our actual sprite and cannot invent limbs.


## Character Generation (legless hovering robot)

Generate the body as a **free image**, not a rigged character. Proven pipeline:

1. **`create_image_pro`** — 16 candidates, transparent, side view. Emphasize the silhouette:
   ```
   upright hovering robot, side view facing right, clearly NO legs — sleek angular
   dark metal body whose lower half ends in a glowing downward cone of light with
   concentric swirling rings like a UFO tractor beam, glowing visor and chest core,
   arms at sides, clean silhouette, filling canvas, pixel art
   ```
   Keep the description **movement/appearance only** — never say "run", "stand", "feet", "legs" (the model adds limbs).
2. **Pick** a candidate (reviewed from a contact sheet).
3. **`edit_image`** to fix details (e.g. `description: "Replace ONLY the rocket flame underneath with a hovering tractor beam: soft cone of light with concentric glowing rings. No fire, no legs."`) — preserves the body, swaps the base.
4. **`correct_pixelart`** (strength ~0.12) to tidy edges; optionally **`reduce_colors`** to lock the palette.
5. **Downscale to 48px** (nearest-neighbor) to match the sprite grid, add a `.import`, and mirror for west in-engine.

`create_character`/`animate_character` are only for **bipedal** characters — they impose the mannequin skeleton and re-add legs. Do not use for our cast.

## Animating From Our Base Sprite

Use **`animate_image`** with the finalized sprite as `first_frame_url` (the PixelLab download URL works — it's public/UUID-keyed). It moves our exact image; no skeleton, so **no legs can appear**.

```
animate_image(
  first_frame_url: "https://api.pixellab.ai/mcp/images/<job>/download",
  action: "the whole robot launches straight upward and stretches tall, energy beam flares beneath, body stays legless and rigid, no legs",
  frame_count: 8,
  no_background: true
)
```

- `animate_image` returns **frame_count + 1** frames; **index 0 is the input unchanged**, then the generated motion.
- Fetch a specific frame: `get_image(job_id, index=N)` or the `.../download?index=N` URL.
- Keep `action` **movement-only** (a verb/pose), avoid environmental nouns.

Per-motion notes (our proven set):
- **jump** — flares the beam + rises; good, body stays rigid
- **grind** — low crouch, arms out for balance (rotation/bob stays in-engine)
- **front flip** — tuck/crouch forward pose; **in-engine supplies the spin**
- **back flip** — lean-back/arch pose; **in-engine supplies the spin**
- **whole-body locomotion** (bob, squash, stretch, facing, spin, ripples) = **procedural in-engine**, not AI frames

> **CRITICAL GOTCHA (all PixelLab tools):** pass only the fields you use; **omit unused optionals entirely — never send `null`.** E.g. `animate_image` rejects `action: null` with a Pydantic `input_type=NoneType` error; `animate_character` rejects a `null` sibling with "provide either …". This was an MCP-layer null bug — always supply real values.

Concurrency cap is **8 jobs at once** — a 9th returns `need 1 job slots but only 0 available (8/8 used)`; re-queue after some finish.


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

1. **ONE `create_image_pro` batch** → present results to user for selection
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

East-only (west is an in-engine flip), one folder per action:

```
assets/characters/<character>/anim/<action>/<frame_NN>.png
assets/characters/player/player_east.png          # base body (48px)
assets/characters/player/anim/jump/jump_00.png    # index 0 == base
assets/characters/player/anim/grind/grind_00.png
assets/characters/player/anim/backflip/flip_back_00.png
assets/characters/player/anim/frontflip/flip_front_00.png
assets/hoverboards/player/hoverboard_snowboard.png
```

