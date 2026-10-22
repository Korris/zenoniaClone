# Phase 09: Asset Pack Graphics (Ninja Adventure, CC0)

**Status:** Complete (2026-10-22)
**Priority:** High (user still found code-drawn sprites ugly; wanted real Zenonia-grade art)

## Decision
- Replace all code-drawn art with the CC0 "Ninja Adventure" pack by pixel-boy (itch.io). 16px chibi
  characters with facesets, 60+ monsters, bosses, tilesets (field, water, relief, nature, house,
  desert, dungeon, interior), slash FX, dialog UI. Only used files copied to `assets/ninja-adventure/`.
- PIXEL_SCALE 3 -> 4 so 16px sprites read at Zenonia size (view 240x160 world px).

## Files
- New: `graphics/asset-loader.js`, `graphics/asset-atlas.js` (manifest + cell rects), `assets/ninja-adventure/**`, CREDITS.txt
- Rewritten: `sprite-registry.js` (slices sheets), `tilemap-renderer.js` (3x3 block auto-tiling, cliff rows),
  `player-renderer.js`, `monster-renderer.js` (4-way `dir`, boss strips), `prop-renderer.js`, `effects-renderer.js`
  (FX strips by progress), `tile-char-info.js` (kinds), `prop-definitions.js`, `main.js` (async boot + loading screen)
- Deleted: hero/monster pixel-map data, prop-art-generator, dev/validate-sprites

## Mapping
- Hero = FighterRed (blonde, red headband). NPCs: OldMan, Hunter, Villager4, Caveman, Sultan, Knight.
- Monsters: Slime2, BlueBat, KappaGreen (goblin), Flam (fire slime), Cyclope (goblin warrior), Skull, SkullBlue. Boss: DemonCyclop.
- Tiles: field grass/sand variants, field orange 3x3 block = path, water-on-grass 3x3 block, relief cliff rows,
  interior floors (castle, cave), wall-simple brick, floor lava cell, procedural portal kept.

## Todo
- [x] download pack via headless browser, inspect sheets with grid overlays, pick cells
- [x] atlas + loader + registry + renderers
- [x] maps re-propped, boss renamed Demon Cyclops
- [x] screenshots verified (meadow, village, combat FX, dialog faceset); 63 tests pass

## Success criteria
- Game boots with a loading screen, no console errors, visuals match the Zenonia reference style.
