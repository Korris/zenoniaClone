---
title: "Zenonia-style Web ARPG"
description: "Browser 2D top-down chibi action RPG: combo hack-and-slash, leveling, upgradable skills, 4 maps via portals. Vanilla Canvas + ES modules."
status: completed
priority: P2
effort: 30h (+12h art/quest)
branch: none
tags: [game, canvas, javascript, arpg, pixel-art]
created: 2026-10-22
---

# Zenonia Web ARPG - Implementation Plan

## Stack (fixed)
Vanilla HTML5 Canvas, JS ES modules, no deps/bundler. Code-defined pixel-art (string maps -> offscreen canvas, 3x scale, smoothing off). localStorage save. `node --test` for pure logic. Logical res 960x640, tile 16px (48px on screen). All files <200 lines, kebab-case.

## Architecture Rules
- `src/systems/*`, `src/data/*`, `src/world/tilemap.js` logic = **pure, no DOM** (testable in Node).
- Rendering only in `src/graphics/*`, `src/ui/*`, entity `draw(ctx, camera)` methods.
- Single shared `game-state.js` object passed around; no globals besides it.
- Fixed timestep update (60Hz, dt in seconds), render with rAF.

## Phases
| # | Phase | Effort | Status |
|---|-------|--------|--------|
| 01 | [Scaffold, core loop, input, camera, sprite factory](phase-01-scaffold-core-loop-and-sprite-factory.md) | 4h | done |
| 02 | [Tilemap, 4 maps, map manager, portals, collision](phase-02-tilemap-maps-portals-collision.md) | 5h | done |
| 03 | [Player, hero sprites, combo, hit detection, effects](phase-03-player-combo-hit-detection.md) | 6h | done |
| 04 | [Monsters, AI, spawner, drops, leveling](phase-04-monsters-ai-drops-leveling.md) | 6h | done |
| 05 | [Skills, skill panel, HUD, pause, save, death, boss win](phase-05-skills-hud-save-death-win.md) | 6h | done |
| 06 | [Unit tests + polish](phase-06-tests-and-polish.md) | 3h | done |
| 07 | [Art overhaul, Zenonia Chronobreak look](phase-07-art-overhaul-zenonia-style.md) | 8h | done |
| 08 | [Quest system, NPCs, dialog](phase-08-quest-system-npcs-dialog.md) | 4h | done |
| 09 | [Asset pack graphics (Ninja Adventure CC0)](phase-09-asset-pack-graphics.md) | 4h | done |
| 10 | [Custom hero sprite pipeline + combo animation](phase-10-custom-hero-sprite-pipeline.md) | 3h | done |
| 11 | HD render mode (hand-painted hero, Zenonia 4 style) -- [report](reports/fullstack-261022-1430-hd-render-mode.md) | - | done |
| 12 | HD ground tiles (grass/path/water/floors/walls/portal) -- [report](reports/fullstack-261022-1455-hd-tiles.md) | - | done |
| 13 | HD props (trees/rocks/houses/fences/torches...) -- [report](reports/fullstack-261022-1520-hd-props.md) | - | done |
| 14 | Oakhaven Village: new starting-hub map (replaces Verdant Meadow as map 1) -- [report](reports/fullstack-261022-1540-oakhaven-village.md) | - | done |
| 15 | Bugfix: hero left/right facing sprites were swapped -- [report](reports/fullstack-261022-1550-hero-left-right-fix.md) | - | done |

## Dependencies
01 -> 02 -> 03 -> 04 -> 05 -> 06 -> 07 -> 08 -> 09 -> 10 (sequential). Phase 09 superseded phase 07's code-drawn art. Tests for leveling/combo can be written alongside 03/04 (pure modules).
Phases 11-15 are later, ad-hoc follow-up work (post "done definition") -- each HD phase stacks on
the previous one's `pixelsPerUnit`/merge-by-name model (hero -> tiles -> props); phase 14 (Oakhaven
Village) only needed map data, no engine changes; phase 15 was a user-reported bug caught and fixed
the same session.

## Current Status (as of 2026-10-22 15:50)
All 15 phases done. Latest state: game now boots into **Oakhaven Village** (new safe hub, was
Verdant Meadow), world art is hand-painted HD (hero/tiles/props) layered over the CC0 pixel pack,
hero facing bug fixed. 107/107 tests pass. See `reports/` (sorted by filename timestamp) for the
full session-by-session detail; nothing in-flight / no open implementation work right now.

## Key Data Shapes (detailed in phase files)
- MapDef: phase-02. MonsterDef: phase-04. SkillDef: phase-05. HeroStatsConfig: phase-03.
- Save payload: phase-05.

## Run
`npx serve .` or `python -m http.server 8000` -> open http://localhost:3000 / :8000.
Tests: `npm test` (`node --test tests/*.test.js`, 63 pass).

## Done Definition
Play from Verdant Meadow to Ruined Castle boss kill; combo 3-hit works with 400ms window; level up grants SP; 4 skills upgradable to Lv5; save/load restores state; all tests pass.

## Unresolved Questions
- Gold usage: no shop specified -> gold is score-only (quests also reward gold). Confirm.
- Audio: not in spec -> skipped.
- Mobile touch controls: not in spec -> skipped.
