# Rust Grind Galaxy - PixelLab Prompt Rules

> This doc defines **how** to generate art with PixelLab. For what the art must **look like**, see **[`ART_STYLE.md`](ART_STYLE.md)**.

## Tool Selection Matrix

| Asset Type | Primary Tool | Backup Tool |
|------------|-------------|-------------|
| Characters (rigid/legless/custom silhouette) | `create_image_pro` (+ `edit_image` to refine) | `create_image_pixflux` |
| Characters (bipedal, legs ok) | `create_character` | — |
| Character **poses / motion from our sprite** | `animate_image` | `animate_character` (bipedal only) |
| VFX (beam, slash, impact) | `animate_image` (simple element) | `create_image_pixflux` |
| Board state FX (dust, sparks, wisps) | `create_image_pixen` 64px base → `animate_image` → `fx_to_alpha.ps1` | silhouette lock (not needed) |
| Hoverboards (base sprite) | `edit_image_pixen` (restyle existing deck PNG) | `create_image_pixflux` |
| Hoverboards (idle mist loop) | `animate_image` + local shape-lock | — |
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

The board is **light, not matter**: a translucent-white energy shape whose silhouette (a snowboard-deck capsule, no wheels) stays fixed while a swirly mist-of-light texture drifts inside it. Ship as **pure grayscale whites** (no black outline) so runtime `Modulate` color + alpha can tint it.

Base-still pipeline (proven):
1. `edit_image_pixen` on the existing deck PNG (48x48) — e.g.
   ```
   Recolor ONLY the interior of this exact same board shape: pure white translucent
   light energy, soft swirly mist texture inside, brighter rim, grayscale white tones
   only, keep the identical silhouette, transparent background
   ```
2. **Shape-lock locally**: AND the edit's pixels with the original alpha mask (the model bloats edges; a PowerShell `GetPixel` loop is enough at 48px). Also inpaint any stray dark pixels from neighbor averages — black specks survive edits.

Idle-loop animation (proven): `animate_image` on the locked base, `frame_count: 8`, `no_background: true`, action like "wisps of glowing mist swirl and drift slowly inside the fixed board silhouette; outline stays exactly still, only interior moves". Then shape-lock every frame to the base mask AND **replace sub-alpha pixels with the base frame's RGB** (don't leave them white — the model fades patches mid-loop). Ship `board_00..08` (frame 0 = base) to `assets/hoverboards/player/anim/idle/`; `PlayerController.Hover.cs → UpdateBoardVisual()` plays ping-pong loops selected per state (grinding → `anim/grind/`, ground-moving above `MoveSpeedThreshold` → `anim/move/`, otherwise idle/air; missing sets fall back to idle).

State-loop animations (grind/move, proven 2026-09): same `animate_image` call on the base, actions "hoverboard grinding along a rail at speed: bright energy flare pulses along the underside contact line, tiny sparks flicker off the rear underside, interior light churns quickly, outline stays fixed" and "hoverboard gliding fast across the ground: interior light streams rapidly from front to back, wisps trail off the rear edge, the underside hover glow pulses rhythmically, outline stays fixed". The model returns **opaque white/gray pixels (A=255)** — motion lives in luminance, not alpha — and it ignores "outline stays fixed" (draws trailing wisps + occasional colored specks). The shape-lock converts everything: `alpha = ramp(A*lum/255)`, floor pixels where the base is strong stay `alpha 140` so the deck never vanishes in dark churn frames; outside the silhouette everything is dropped (colored specks vanish for free via luminance). Scripts (in `.gdignore`d `assets/hoverboards/player/_animtest/`): `lock_board_frames.ps1` (mask+convert), `contact_sheet.ps1`, `sim_sheet.ps1` (preview locked frames at `BoardOpacity` over `board_glow.png` before shipping). Ship to `anim/grind/` + `anim/move/` as `board_00..08`; run headless `godot --import` so frames load without an editor restart.

### Board FX sprites (dust / sparks / wisps)

Per-state FX sprites parented to `BoardSprite` (driver: `PlayerController.BoardFx.cs`): gray dust trails behind when ground-moving, cyan-white sparks at the rail contact while grinding, white wind wisps when airborne. Recipe (proven 2026-09): each set = `create_image_pixen` base on a **64x64 transparent canvas** (`outline: lineless`, description bakes the layout: dust "densest at the right edge dissipating left", sparks "burst ejecting from a contact point at the top right toward the lower left", wisps "arcs flowing leftward") → `animate_image` (8 frames) → `fx_to_alpha.ps1` converts opaque white/gray output to pure white + alpha (ramp `v-95 → 255`, unlike board frames these have **no silhouette**, so the whole alpha comes from luminance). **Loop-hygiene check:** inspect per-frame non-transparent pixel counts first — the spark set had near-empty dead frames mid-loop (34/462 px) that read as a flicker gap; deleted and renumbered them (ship fewer frames if needed). FX stay grayscale so runtime `SelfModulate` colors (`SparkFxColor` etc.) control the look; `SelfModulate.A` divides out the board's `BoardOpacity` since children multiply parent modulate. Offsets are board-local px (validated with `fx_placement_preview.ps1` before shipping); children inherit flip/tilt/bob/spin and all use `ShowBehindParent=true`. Grind sparks do **not** use a board offset — they sit fixed at the board's contact anchor (board-local center + `SparkContactLiftPixels`; each grind trick will eventually supply its own contact point through the emitter's `contactLocal` parameter; flare-core pivot corrected via `SparkFlashPivot` export — the pivot is the flash point in texture px relative to sprite center (measured ~+14,-15 of a 64px canvas); it must be exact because rotation and growth both pivot around it, and a wrong value makes the core swing whenever angle/scale change), full opacity while grinding. The spark runtime is a `SparkBurstFx` scheduler with two emitters (a second, smaller one at `SparkSmallScaleBias`; both fire from the exact contact point by default — the scatter knobs `SparkBurstJitterPixels`, `SparkSecondaryTrailPixels`, `SparkSmallJitterBias` ship at 0): each burst rolls a spray angle centered straight-back (`SparkRotateBackDegrees` 45) ± `SparkRotateMaxDegrees` 35 — 15° below the board through straight-back to 15° above it, never forward (art rest ≈45° between straight-down and straight-back), length (2-6 frames) and max size, then plays with position and angle frozen while scale **grows** `SparkGrowFrom`→max (ease-out, capped by `SparkScaleCap`); random invisible gaps (`SparkPauseChance`, `SparkPauseSecondsMin/Max`) separate bursts. The burst's size envelope (0.69→1.33) is sampled from `railRatio` at roll time, so faster grinds erupt bigger.

Light look (no gray in art): all board frames ship as **pure white RGB with variable alpha** — the swirl's gray values were converted to transparency (lum 185–255 → alpha 80–255, then re-remapped so the body sits near opaque and only wisps go transparent). This keeps tinting (white × color) clean and lets the board read as white swirling light over any background. `BoardSprite` uses Linear texture filtering (project default is Nearest). `BoardOpacity` export (currently 0.6, trialed up from 0.4) scales the whole board+glow. Glow: `assets/hoverboards/player/board_glow.png` — a **pre-baked Gaussian** (CPU separable blur of the binarized silhouette at 4× resolution, 192px covering the same 48 world units, peak-normalized alpha) drawn by a `BoardGlow` child `Sprite2D` of `BoardSprite` with an additive `CanvasItemMaterial` (base local scale 1/4). A dilation-sum fragment shader was the first attempt and removed — it produced angular fades (center-scaled dilations + texture-edge clipping). Exports: `BoardGlowScale` (size multiplier), `BoardGlowStrength` (additive brightness), `BoardGlowColor` (future tinting sets this + board modulate together). Being a child, the glow inherits tilt/bob/spin/flip for free. The reusable recipe for glowing other assets is in [`docs/GLOW_SYSTEM.md`](../GLOW_SYSTEM.md) §Soft Emission Glow. See `PlayerController.Hover.cs → UpdateBoardVisual()`.

Gotchas learned:
- `create_image_pixflux` rejects canvases below **32x32 total area** — `48x16` failed; use `48x48` (or bigger) with the shape drawn inside.
- The board is generated **long-axis vertical** (a "snowboard"), then **rotated 90° into the PNG** so it lays flat. Do **not** rotate the `Sprite2D` node — `UpdateBoardAnimationTilt`/`ApplyTrickVisual` overwrite `BoardSprite.Rotation` every physics frame (see `AGENTS.md` → Art & Animation Pipeline).
- `edit_image_pixen` may leave **black specks** and grow the silhouette 1-2px — always shape-lock + despeck before using as an animation seed.

## Prompt Templates

### Character (Side-View Robot)

```
{robot_description}, side view platformer sprite, pixel art,
single color black outline, basic shading, medium detail,
crisp pixels, no anti-aliasing, 16-bit retro game asset
```

### Hoverboard (Energy Board)

```
{energy_board_description}, side view, pixel art,
translucent white light, swirly mist texture inside, no outline,
grayscale whites only, crisp pixels, transparent background
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
assets/hoverboards/player/hoverboard_snowboard.png  # white energy base, laid flat (baked 90°), shape-locked
assets/hoverboards/player/anim/idle/board_00.png     # mist-swirl idle loop (00 == base)
assets/hoverboards/player/anim/grind/board_00.png    # grind churn loop (00 == base)
assets/hoverboards/player/anim/move/board_00.png     # ground-glide flow-streak loop (00 == base)
assets/hoverboards/player/fx/dust/boardfx_00.png     # ground-move dust trail (64px, white+alpha, tinted by DustFxColor)
assets/hoverboards/player/fx/sparks/boardfx_00.png   # grind spark burst (7 usable frames after dead-frame cull)
assets/hoverboards/player/fx/wisps/boardfx_00.png    # airborne wind streaks
```

