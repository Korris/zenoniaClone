# Phase 10: Custom Hero Sprite Pipeline + Combo Animation

**Status:** Complete (2026-10-22)
**Priority:** High (user: combo "đứng một chỗ chém", wanted ChatGPT-made sprites wired in)

## Input
- `assets/source/character-concept.png`: ChatGPT concept sheet (painted background, labels, frames not on a grid).
- Spec (user prompt): 24x24 frames, rows down/up/left/right, idle 4 / walk 6 / attack1 4 / attack2 4 / attack3 6 / dash 3 / hurt 2 / death 5; FX strips slash_small 6x32, slash_big 8x48, dash_trail 4x32, hit_spark 5x16; RGBA true alpha; <=32 colors; validator; contact sheet; README.

## Pipeline (`tools/build-hero-sprites.py`, Python/Pillow/numpy)
- Block rectangles per animation -> foreground mask -> grid lines snapped to the emptiest gaps.
- Per cell: background keyed by color distance (characters) or brightness/whiteness (FX), holes filled by flood fill, tight bbox.
- One global scale (idle body = 20 px) so proportions and pivot match across animations; feet on row 23.
- Shared median-cut palette (32 colors), binary alpha; faint FX tail frames synthesized from the previous frame.
- Output: `assets/custom/hero/*.png`, `contact-sheet.png`, `README.md`. `tools/validate_sprites.py` -> RESULT: OK.
- Limitation (stated): frames are controlled conversions of the concept poses, not hand-pixelled; wide sword poses clip 1-3 px.

## Engine
- `asset-atlas.js`: HERO_ANIMATIONS + FX_STRIPS now point at the custom set; loader root = `assets/`.
- `sprite-registry.js`: `buildHero` -> `hero[dir][anim][]`, `HERO_FRAME_SIZE = 24`.
- `player-renderer.js`: state -> animation (death / dash / attack1-3 by `combo.phaseProgress` / hurt / walk / idle).
- Combo feel: `stepPx` lunge per hit (6/8/16), finisher spinning big arc + shake, hit sparks on every hit, dash trail every 45 ms.
- `tools/browser-check.mjs` (puppeteer-core + local Chrome): boot, combo chain, dash, death screenshots, console errors.

## Todo
- [x] builder + validator + contact sheet + README
- [x] atlas/registry/renderer integration, death animation driven in DEAD state
- [x] smoke test passes with 0 errors; 63 unit tests pass
