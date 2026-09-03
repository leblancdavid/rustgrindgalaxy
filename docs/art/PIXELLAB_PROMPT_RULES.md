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
- **idle** — looping (ping-pong): body still + rigid, only the beam/rings rotate/swirl
- **move** — looping: leans forward, beam burns brighter/longer
- **charge** (hold jump) — crouches, beam gathers/brightens; builds by charge ratio 0→1, then tail-loops the last frames while held, hands off to jump on release
- **jump** — flares the beam + rises; body stays rigid
- **grind** — low crouch, arms out for balance (rotation/bob stays in-engine)
- **front flip** — tuck/crouch forward pose; **in-engine supplies the spin**
- **back flip** — lean-back/arch pose; **in-engine supplies the spin**
- **whole-body locomotion** (bob, squash, stretch, facing, spin, ripples) = **procedural in-engine**, not AI frames

Playback model: **idle/move/jump loop** (ping-pong avoids seams); **flips + grind scrub** — advance to the held pose while the input is held, **rewind** to neutral on release; **charge** builds by the jump-charge ratio, then tail-loops the last frames while fully held, and hands off to jump on release. See `PlayerController.Hover.cs`.


> **CRITICAL GOTCHA (all PixelLab tools):** pass only the fields you use; **omit unused optionals entirely — never send `null`.** E.g. `animate_image` rejects `action: null` with a Pydantic `input_type=NoneType` error; `animate_character` rejects a `null` sibling with "provide either …". This was an MCP-layer null bug — always supply real values.

Concurrency cap is **8 jobs at once** — a 9th returns `need 1 job slots but only 0 available (8/8 used)`; re-queue after some finish.


### Hoverboard Sprite

Separate **grayscale** sprite (runtime-tinted like other env art) — a **snowboard deck** (no wheels), not a glowing disc:

```
create_image_pixflux(
  description: "grayscale snowboard deck shape, flat elongated board, straight-on side view, no bindings, transparent background, pixel art",
  width: 48, height: 48, no_background: true
)
```

Gotchas learned:
- `create_image_pixflux` rejects canvases below **32x32 total area** — `48x16` failed; use `48x48` (or bigger) with the shape drawn inside.
- The board is generated **long-axis vertical** (a "snowboard"), then **rotated 90° into the PNG** so it lays flat. Do **not** rotate the `Sprite2D` node — `UpdateBoardAnimationTilt`/`ApplyTrickVisual` overwrite `BoardSprite.Rotation` every physics frame (see `AGENTS.md` → Art & Animation Pipeline).

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

> **Environment only.** Grayscale + runtime tint applies to **props, tiles, and backgrounds**. Characters keep **baked** identity colors and are not tinted — ignore this section for characters and describe their real colors.

### Generating Grayscale Assets (props/tiles)

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

## Asset Import & Runtime-Load Workflow

End-to-end for each new sprite / animation:

1. Generate at **64px** (transparent). Review via a contact sheet or a looping `preview.html`.
2. `correct_pixelart` (strength ~0.1) to drop stray corner pixels.
3. **Downscale to 48px**, nearest-neighbor, to match the sprite grid.
4. Write a `.import` per PNG (standard `texture`/`CompressedTexture2D` params, unique `uid://…`) or let the **editor auto-import** on focus.
5. **Reload the project in the Godot editor** so new PNGs import — until then `GD.Load<Texture2D>(res://…)` and `ResourceLoader.Exists` return **null**. `PlayerController.Hover.cs → LoadFrames()` guards on `Exists`, so un-imported frames make that animation silently fall back to procedural.
6. Keep staging (candidate sheets, `_*.png`, `preview.html`) in a `.gdignore` folder (e.g. `assets/characters/player/_animtest/`) so it never enters the build.

## File Naming

East-only (west is an in-engine flip), one folder per action:

```
assets/characters/<character>/anim/<action>/<frame_NN>.png
assets/characters/player/player_east.png            # base body (48px)
assets/characters/player/anim/idle/idle_00.png      # index 0 == base
assets/characters/player/anim/move/move_00.png
assets/characters/player/anim/charge/charge_00.png
assets/characters/player/anim/jump/jump_00.png
assets/characters/player/anim/grind/grind_00.png
assets/characters/player/anim/backflip/flip_back_00.png
assets/characters/player/anim/frontflip/flip_front_00.png
assets/hoverboards/player/hoverboard_snowboard.png  # grayscale, laid flat (baked 90°)
```

