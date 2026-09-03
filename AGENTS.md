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

## Art & Animation Pipeline

Art generation rules and the full PixelLab learnings live in `docs/art/` (`ART_STYLE.md`, `PIXELLAB_PROMPT_RULES.md`, `PIXELLAB_MCP_GUIDE.md`). Key durable gotchas:

- **Legless characters (player, most enemies):** generate as a free image (`create_image_pro` → pick → `edit_image` → `correct_pixelart`), **never `create_character`** — it rigs the `mannequin` skeleton and template animations re-add legs. Animate with **`animate_image`** (moves our actual sprite, no skeleton).
- **PixelLab null bug:** omit unused optional fields entirely — never pass `null` (breaks `animate_image`/`animate_character`).
- **East-only art:** store only the east-facing sprite; west is `_visual.Scale.X = -1`. Generate at 64px, ship at 48px (nearest-neighbor).
- **Node rotation is overwritten:** `UpdateBoardAnimationTilt`/`ApplyTrickVisual` set `BoardSprite.Rotation`/`Position` every physics frame, and `UpdateFailedLandingVisual` sets `_visual.Rotation`. Bake static orientation into the **art** (e.g. the hoverboard's "lay flat" 90° is in the PNG, not the node).
- **Procedural animation driver = `PlayerController.Hover.cs`** (partial class), runs in `_Process` and deliberately touches **only** `_visual` `Scale`/`Position`/`Modulate`/`Texture` — never `Rotation` — so it won't fight the `_PhysicsProcess` spin/trick code.
- **Animation state priority** (per frame): dead → grind → flip → grind-rewind → jump → charge → ground idle/move. Loops ping-pong; flips+grind scrub (advance while held, rewind on release); charge follows the jump-charge ratio then tail-loops at full.
- **Runtime frame load:** frames load from `res://assets/characters/player/anim/<state>/` via `GD.Load`, guarded by `ResourceLoader.Exists`. **New PNGs only load at runtime after the Godot editor reimports them** — reload the project in-editor first, else the loader returns null and the state silently falls back to procedural.
- **Dev scratch** (candidate sheets, `preview.html`, `_*`) belongs in a `.gdignore`'d folder (e.g. `assets/characters/player/_animtest/`) so it never imports into the build.

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
