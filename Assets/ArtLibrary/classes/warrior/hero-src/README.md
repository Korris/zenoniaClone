# Hero sprite set (custom)

Generated from the ChatGPT concept sheet `assets/source/character-concept.png` by `tools/build-hero-sprites.py`
and checked by `tools/validate_sprites.py`. Frames are converted with controlled processing
(background keyed to true alpha, holes filled, one global scale, shared 32-color palette), not
hand-pixelled; wide sword poses may clip 1-3 px at the frame edge.

## Character sheets (24x24 px per frame)

| File | Frames/row | Size | Suggested FPS | Loop |
|------|-----------:|------|--------------:|------|
| idle.png | 4 | 96x96 | 5 | yes |
| walk.png | 6 | 144x96 | 10 | yes |
| attack1.png | 4 | 96x96 | 16 | no (combo hit 1) |
| attack2.png | 4 | 96x96 | 16 | no (combo hit 2) |
| attack3.png | 6 | 144x96 | 14 | no (finisher) |
| dash.png | 3 | 72x96 | 18 | yes while dashing |
| hurt.png | 2 | 48x96 | 10 | no |
| death.png | 5 | 120x96 | 8 | hold last frame |

- Rows: `0 = DOWN`, `1 = UP`, `2 = LEFT`, `3 = RIGHT`. Columns run left to right in time order.
- Pivot: feet on the bottom row (y = 23), body centered horizontally (x = 12). Draw a frame at
  `(entity.x - 12, entity.y - 24)` where `entity.y` is the feet position.
- Palette: 32 opaque colors shared by all character frames; alpha is binary (0 or 255).

## Effect strips (single row, facing RIGHT)

| File | Frames | Frame | Size | Use |
|------|-------:|-------|------|-----|
| slash_small.png | 6 | 32x32 | 192x32 | combo hits 1-2 |
| slash_big.png | 8 | 48x48 | 384x48 | finisher / Slash Wave projectile |
| dash_trail.png | 4 | 32x32 | 128x32 | spawned behind the hero while dashing |
| hit_spark.png | 5 | 16x16 | 80x16 | on every landed hit |

Rotate the strip for other directions (`right = 0`, `down = +90deg`, `left = 180deg`, `up = -90deg`).

## Engine integration (HTML5 Canvas)

- `src/graphics/asset-atlas.js` -> `HERO_ANIMATIONS` + `FX_STRIPS` list the files; `sprite-registry.js` slices
  them into `registry.hero[dir][animation][frame]` and `registry.effects.{slash,arc,dashTrail,hitSpark}`.
- `src/graphics/player-renderer.js` picks the animation from game state: dead -> death,
  dash -> dash, combo step 1/2/3 -> attack1/2/3 (frame = swing progress), hurt flash -> hurt,
  moving -> walk, otherwise idle.
- Rebuild after changing the concept sheet: `python tools/build-hero-sprites.py assets/source/character-concept.png assets/custom/hero`
  then `python tools/validate_sprites.py assets/custom/hero`.
