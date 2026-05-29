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
