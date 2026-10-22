# Phase 07: Art Overhaul (Zenonia Chronobreak look)

**Status:** Complete (2026-10-22)
**Priority:** High (user rejected v0.1 placeholder sprites; references: `zenonia1a.jpg`, `zenonia1b.jpg`, `zenonia1c.png`)

## Context
- User feedback: sprites too few colors / too small; must look like Zenonia 1 remaster.
- Constraint kept: no external assets, everything generated in code.

## Requirements
- Hero 20x26 blonde spiky hair, big shaded sword, 4 dirs x 4 walk frames. NPC cast via palette remaps.
- Monsters redrawn with 3-4 tones + soft outline; golem 32x24.
- Ground tiles procedural with shading; auto-tiled edges (dirt rim, water foam, cliff lip).
- Multi-tile props (pine 32x48, house 64x64, fence, bush, rock, stump, mushroom, signpost, cactus, pillar, bones) y-sorted with actors; solid footprints block movement.
- HUD: gold-framed bar + portrait + minimap + name tags.

## Files
- New: `graphics/pixel-painter.js`, `graphics/tile-art-generator.js`, `graphics/prop-art-generator.js`, `graphics/prop-renderer.js`, `world/prop-definitions.js`, `ui/minimap-renderer.js`, `dev/validate-sprites.mjs`
- Rewritten: `sprites/hero-sprite-data.js`, `sprites/monster-sprite-data.js`, `sprites/effect-sprite-data.js`, `sprite-registry.js`, `tilemap-renderer.js`, `world-renderer.js`, `tile-char-info.js`, `tilemap.js`, `map-builder.js`, `maps/*.js`, `ui/hud-renderer.js`
- Deleted: `sprites/tile-sprite-data.js`

## Todo
- [x] pixel-painter helpers (ellipse, triangle, outline, speckle)
- [x] procedural ground tiles + edge overlays
- [x] procedural props + footprints + y-sort rendering
- [x] hero/NPC/monster/effect pixel maps with per-sprite palettes
- [x] maps rebuilt: cliff borders, river + bridge, village, forest
- [x] HUD v2 + minimap
- [x] width/palette validator + screenshot verification in headless browser

## Success criteria
- `node dev/validate-sprites.mjs` prints SPRITES OK; all 4 maps reachable (portals, boss, NPCs); screenshots match reference style.
