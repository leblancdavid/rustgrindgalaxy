# AGENTS.md - Rust Grind Galaxy Project Guidelines

This document provides project-specific guidance for AI agents working in this repository.

## Project Context

- Engine: Godot 4.x
- Language: C#
- Current goal: build a 2D sci-fi platformer vertical slice
- Working title: Rust Grind Galaxy
- Reference project structure: `D:\Dev\Games\kbtv`

## Current Product Direction

Build toward a compact first playable slice with:
- one industrial test level
- one player character
- one raider enemy
- one drone enemy
- basic melee combat
- pickups, health, death, and restart flow

Keep scope tight. Prefer making the first level and movement/combat feel good before adding progression or narrative systems.

## Repository Expectations

Target structure for implementation:
- `scenes/` for Godot scenes
- `scripts/` for C# gameplay code
- `assets/` for curated game-ready assets
- `docs/` for design and implementation notes

The current repository started as a mostly asset-only shell. Do not assume gameplay systems already exist.

## Coding Guidelines

- Prefer small, direct changes over abstract frameworks.
- Keep gameplay code easy to read and easy to delete during iteration.
- Use `PascalCase` for C# types, methods, properties, scene names, and filenames.
- Keep one primary gameplay concept per script unless the file is very small.
- Prefer scene and script pairs where practical, such as `Player.tscn` and `PlayerController.cs`.
- Avoid premature data architecture in phase 1. Use simple exported values before building config pipelines.

## Godot Guidelines

- Avoid editing generated Godot cache files unless required.
- Prefer normal Godot project structure over custom bootstrapping unless there is a clear need.
- When adding scenes, keep ownership and node structure obvious.
- Use deterministic scene names and script paths so they are easy to find with search tools.

## Physics & Node Lookup Patterns

- For code-created `Area2D` nodes, `BodyEntered`/`GetOverlappingBodies()` can fail silently. Use `PhysicsDirectSpaceState2D.IntersectShape()` with a `PhysicsShapeQueryParameters2D` instead — it queries the physics engine's spatial hash directly and is guaranteed to detect overlap if the geometries intersect.
- When looking up `World` via `player.GetParentOrNull<World>()`, the player's parent may be `ProcGenTest` (test scene) instead of `World`. Use `world?.CollectMineral(...)` (null-conditional) to silently skip optional World-only operations while still running required cleanup like `QueueFree()` or `PlayShatter()`. See `MineralPickup.cs:32-34` for the established pattern.
- Shadows (`Shadow.cs` + `ShadowSilhouette.cs`): every object entity attaches a shadow via `Shadow.Attach(parent)` (index 0 so it draws behind sibling art); effects/projectiles stay shadow-free. Shadows auto-detect the parent's first textured `Sprite2D`, else first `Polygon2D` body visual (shader-material glows, `Glow`/`Beam`/`Fuse` names are excluded); anything else falls back to the soft ellipse. Silhouettes are baked black, cropped to the tight alpha bbox (sprite art has transparent padding — the flip mirrors about the bottommost *opaque* row so feet stay welded), and vertically flipped; squash and sun shear are runtime-only (`Transform2D` basis, `WorldSun` shear set per level seed). Ground clips it, it does not gate it: shadows draw wherever the ray hits and are **clipped per-pixel to opaque ground art** by `GroundClip` — it registers the direct `Polygon2D` children of every `LevelTile` (world-space, lazy bake) as ground fill, and each shadow sprite runs an even-odd edge-test shader against per-instance uniforms (canvas_item has no world matrix; Shadow hands it the CPU-composed linear map incl. owner + slope rotation). Catwalk strips keep a partial shadow welded to the strip; nothing hangs over gaps or past platform edges. New tile shapes need no shadow code, but ground art must stay a direct `Polygon2D` of the tile root. Pads/beacons/props are placed flush or sunk into the floor, so a failed or out-of-range direct ray retries from `EmbeddedRetryLift` above the origin and accepts only surfaces within `EmbeddedMaxSunk` (a false accept over a thin platform is still caught by the probe).

## Art & Animation Pipeline

Art generation rules and the full PixelLab learnings live in `docs/art/` (`ART_STYLE.md`, `PIXELLAB_PROMPT_RULES.md`, `PIXELLAB_MCP_GUIDE.md`). Key durable gotchas:

- **Legless characters (player, most enemies):** generate as a free image (`create_image_pro` → pick → `edit_image` → `correct_pixelart`), **never `create_character`** — it rigs the `mannequin` skeleton and template animations re-add legs. Animate with **`animate_image`** (moves our actual sprite, no skeleton).
- **PixelLab null bug:** omit unused optional fields entirely — never pass `null` (breaks `animate_image`/`animate_character`).
- **East-only art:** store only the east-facing sprite; west is `_visual.Scale.X = -1`. Generate at 64px, ship at 48px (nearest-neighbor).
- **Node rotation is overwritten:** `UpdateBoardAnimationTilt`/`ApplyTrickVisual` set `BoardSprite.Rotation`/`Position` every physics frame, and `UpdateFailedLandingVisual` sets `_visual.Rotation`. Bake static orientation into the **art** (e.g. the hoverboard's "lay flat" 90° is in the PNG, not the node).
- **Procedural animation driver = `PlayerController.Hover.cs`** (partial class), runs in `_Process` and deliberately touches **only** `_visual` `Scale`/`Position`/`Modulate`/`Texture` (plus `_boardVisual.Texture` for the board's per-state loops: grind > ground-move > idle) — never `Rotation` — so it won't fight the `_PhysicsProcess` spin/trick code.
- **Board FX layer = `PlayerController.BoardFx.cs`** (partial): wind wisps (air) and sparks (grind) are runtime-created `Sprite2D`. Wind rides the **body, not the board**: child of `VisualContainer` at index 0 (draws behind the robot), so it inherits the air spin (flip direction) but not the board's tilt/bob/trick swirl; `ApplyWind` trails the sprite on the side the player is leaving (`Position.X = _facing * WispFxOffset.X`), mirrors `Scale.X` with facing so streaks sweep away from the body, flips `Scale.Y` while falling (art flows down-left), alpha follows `Velocity.Length()` with `WispMinStrength` floor at apex, color is `WispFxColor.Lerp(levelPalette.Resolve(WispFxTintSlot), WispFxTintStrength)` (slot defaults to `PrimaryLight`, same palette hook as dust), no `BoardOpacity` compensation (not under the board's modulate). Sparks are children of `BoardSprite` with `ShowBehindParent`; they inherit the board's flip/tilt/bob/spin, animate only `Texture`/`Position`/`Rotation`/`Scale`/`SelfModulate`, and fade via speed-scaled targets. Dust (ground-move) is a separate world-space system, see below. Sparks are fixed at the board's contact anchor — board-local center + `SparkContactLiftPixels` (no wiggle/slide; each grind trick will eventually supply its own contact point, passed through the emitter's `contactLocal` parameter). Spark logic is a `SparkBurstFx` class with **two emitters** (main + smaller via `SparkSmallScaleBias`; both fire from the exact contact point by default — `SparkBurstJitterPixels`/`SparkSecondaryTrailPixels`/`SparkSmallJitterBias` all 0, knobs kept if scatter is wanted): each burst rolls a spray angle centered straight-back (`SparkRotateBackDegrees` 45) ± `SparkRotateMaxDegrees` 35 — i.e. 15° below the board through straight-back to 15° above it, never forward, art rest ≈45° — length (2-6 frames) and max size, plays with position/angle frozen while scale **grows** `SparkGrowFrom`→max over the burst (capped by `SparkScaleCap`, pivoting on `SparkFlashPivot` so the flare core stays welded to the contact), with random invisible gaps (`SparkPauseChance`, `SparkPauseSecondsMin/Max`) between bursts. Frames from `res://assets/hoverboards/player/fx/<set>/boardfx_NN.png`; missing set = that FX hidden. **Dust is the deliberate exception to "inherits board motion":** a pool of `DustFxPoolSize` `DustPuffFx` sprites (`BoardDust00+`) emitted at random intervals (mean `DustFxIntervalFast`..`DustFxIntervalSlow` shrinks with `moveRatio`); dust lives on its own **`DustPuffs` Node2D** (child of the player root, moved to first sibling) — deliberately OUTSIDE the board/container chain so tilt/flip/bob/spin can never touch it; each puff captures its **world** ground anchor at spawn (`ToGlobal` of the board-bottom-welded point, board `GlobalScale.X` baked into the puff's size), then per frame re-pins `GlobalPosition = anchor + drift*t − bottom-weld in world units` with `Rotation = 0`; per-puff size/fps jitter (`DustFxScaleJitter`/`DustFpsJitter`), expands `1 -> DustFxGrowTo` and fades `1 -> DustFxFadeFloor` over `DustFxLifetime`, drifts back+up (`DustFxDriftBack`/`DustFxDriftLift`), and a >`DustTeleportCutoffPx` player position step (respawn/teleport) clears the pool. Puff color is rolled at spawn as `DustFxColor.Lerp(levelPalette.Resolve(DustFxTintSlot), DustFxTintStrength)`; `TileLevelGenerator.Initialize` feeds the palette to the player via `SetLevelPalette(...)` (guarded on `Brightness > 0`), so no-palette scenes keep the plain tint. A ground-jump launch (`TryReleaseJump`'s onFloor branch, not rails) calls `EmitJumpPuff()` for one extra puff at `DustFxJumpScaleBias` on the takeoff spot.
- **Animation state priority** (per frame): dead → grind → flip → grind-rewind → jump → charge → ground idle/move. Loops ping-pong; flips+grind scrub (advance while held, rewind on release); charge follows the jump-charge ratio then tail-loops at full.
- **Runtime frame load:** frames load from `res://assets/characters/player/anim/<state>/` via `GD.Load`, guarded by `ResourceLoader.Exists`. **New PNGs only load at runtime after the Godot editor reimports them** — run `godot --headless --import` from the CLI (or reload in-editor) first, else the loader returns null and the state silently falls back to procedural.
- **Dev scratch** (candidate sheets, `preview.html`, `_*`) belongs in a folder named `_staging/` under `assets/` — every such folder is `.gdignore`'d (never imported by Godot) AND gitignored via `**/_staging/` (never tracked in the repo), and is safe to delete at any time. Reusable pipeline scripts (`sheet.ps1`, `dl_candidates.ps1`, `resize_nearest.ps1`, `make_glow.ps1`) live in `tools/` and ARE tracked.

## Refactoring And Safety

- Inspect existing usage before changing public method names or scene paths.
- Do not remove user-created assets or folders unless cleanup is explicitly part of the task.
- When cleanup is requested, keep only assets that clearly support the current vertical slice.
- Do not invent compatibility layers unless there is a real consumer that needs them.

## Working Style

- Search first, then edit.
- Prefer minimal file creation when a simple structure will do.
- Leave concise comments only where behavior would otherwise be hard to parse.
- If a choice affects long-term project direction, make the smallest reversible decision first.
