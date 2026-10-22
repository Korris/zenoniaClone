# Code Review: Asset-Pack Graphics Switch (pass 3)

Scope: main.js, graphics/{asset-loader,asset-atlas,sprite-registry,tilemap/player/monster/prop/effects-renderer,tile-art-generator}.js, world/{tile-char-info,prop-definitions}.js, entities/{monster,visual-effect}.js, game/world-update.js, systems/{combat,skill-cast}-system.js, ui/{dialog-box,hud-renderer,minimap-renderer}.js, core/constants.js. ~880 LOC graphics. Tests: 63 pass.

Verified against real PNG headers: every GROUND_TILES / FLOWER_OVERLAYS / PROP_RECTS cell rect is inside its sheet; all 5 FX_STRIPS frame counts match width/w exactly (slash 130/26=5, arc 228/38=6, circular 378/54=7, flame 200/25=8, spark 270/27=10). Boss strips do NOT match (see H2).

**Score: 7.5/10** - clean data-driven atlas, correct flip/scale math, no per-frame hot spots; loses points for untested asset manifest, silent frame-count desync, and procedural-era leftovers.

## Critical
None.

## High

### H1. One missing/corrupt image bricks boot, incl. assets never drawn - `src/graphics/asset-loader.js:23`
`Promise.all` rejects on the first `onerror`; `main.js:40` dumps a stack trace. Three of the 40 files (`dialogBox`, `facesetBox`, `fxFlame`) are loaded but never read, so a renamed UI PNG kills the game for nothing. Fail-fast is acceptable for a static bundled pack (a placeholder canvas would break `buildStrip`'s width guard anyway) - but it must be caught in CI, not by the player.
Fix (both, small):
1. Use `Promise.allSettled`, collect every failed path, throw one `Error('Missing assets:\n' + paths.join('\n'))` so a rename shows all breakage at once.
2. Add `tests/asset-atlas.test.js` (node, no DOM): for each `ASSET_FILES` entry assert `fs.existsSync`, read IHDR (bytes 16-23) and assert every `GROUND_TILES`/`PROP_RECTS`/`FLOWER_OVERLAYS` rect is in-bounds and every `FX_STRIPS`/`BOSS_STRIPS` `frames * w <= width`. This is the only thing that would have caught H2.

### H2. Boss strip frame counts are wrong; only the width guard hides it - `src/graphics/sprite-registry.js:99,108`
Requested idle 8 / walk 8 / hit 4; real images are 250/50=5, 300/50=6, 150/50=3. `buildStrip` silently truncates, so it renders, but the numbers in code are false and any future edit to `w`/`frameW` would produce garbage frames with no error.
Fix (KISS): drop the count param entirely - `buildStrip(img, w, h)` with `count = Math.floor(img.width / w)` - and delete `frames` from `FX_STRIPS` / the literal 8/8/4. One source of truth (the PNG), guard becomes unnecessary.

## Medium

### M1. Monster hit-flash snaps animation to frame 0 and duplicates the player tint cache - `sprite-registry.js:46`, `monster-renderer.js:66`
`hitFlash[dir]` is a tint of `frames[dir][0]`; while walking, every hit pops the sprite to frame 0 for 100 ms. `player-renderer.js:7-17` already has a lazy `WeakMap` tint cache that handles any frame. 28 tinted canvases are also built eagerly at boot for this.
Fix: export `tintedFrame(frame, color)` from player-renderer, use it in `drawMonster` (`tintedFrame(frame, HIT_FLASH_COLOR)`) and delete `hitFlash` from `buildMonster`. Also delete `boss.hitFlash` (`sprite-registry.js:109`) - `drawBoss` uses the `hit` strip, never reads it.

### M2. Monster projectile visual ~2x its hitbox - `combat-system.js:112`, `effects-renderer.js:36-37`
`spark` frame 27x35 at scale 0.4 draws ~11x14 world px over a 6x6 hitbox; shots that visibly graze the hero miss. Scale math itself is fine (`drawRotated` centers on `sprite.width/2`). Also `pickFrame(entry, 0.4)` freezes every projectile on one strip frame.
Fix: scale 0.25 for monster shots (or hitbox 10x10), and pick frame from lifetime: `pickFrame(entry, 1 - p.lifeMs / p.maxLifeMs)` (add `maxLifeMs` to Projectile) if animated projectiles are wanted; otherwise leave frozen - low value.

### M3. Procedural-era dead code (YAGNI) - safe deletes
- `src/graphics/sprites/effect-sprite-data.js:11-38` `SWORD_ROWS`, `SLASH_ARC_ROWS`, `POTION_ROWS`, `COIN_ROWS`, `SPARK_ROWS`, `PORTRAIT_ROWS` - zero importers.
- `src/graphics/pixel-painter.js:21-70` `ellipse`, `triangle`, `speckle`, `outline`, `sideShade` - only `makeCanvas/px/rect` used (portal).
- `src/graphics/pixel-sprite-factory.js:36-50` `createFrames`, `composeSprite` - unused; `PALETTE` default (`palette.js`) never hit since both callers pass `EFFECT_PALETTE` -> delete `palette.js`, make `palette` required.
- `src/graphics/sprite-animation.js` - no `src/` importer; only `tests/sprite-animation.test.js` keeps it alive. Delete both or wire it in.
- `asset-atlas.js:23,31,80` + `sprite-registry.js:117` `dialogBox`, `facesetBox`, `fxFlame`, `darkGrass`, `registry.ui` - loaded/sliced, never drawn (3 wasted requests on boot; see H1).
- `visual-effect.js:52` `SparkParticle.spriteKey = 'spark'` - `drawParticles` uses `fillRect`; field unused.
- `data/monster-definitions.js` `sprite:` field (`'golemBoss'` etc.) - renderer keys `registry.monsters[monster.type]`; field never read.

### M4. `EDGE_FAMILY` has dead/redundant entries - `src/world/tile-char-info.js:26`
`sameEdgeFamily(ch, …)` is only called with `ch` of kind `block` or `cliff` (`tilemap-renderer.js:49,52-53`). `B` is `variants` -> its entry never used. `C: 'C'` and `R: 'R'` equal the `|| ch` fallback. Collapse to `{ d: 'dBP', w: 'wB' }` and the comment stays true.

## Low

- `tilemap-renderer.js:40` allocates a `same` closure per visible tile per frame (~170/frame) though only `block`/`cliff` need it. Move the `const same = …` into those two branches, or make it a module-level `function same(tilemap, ch, tx, ty, dx, dy)`.
- `prop-definitions.js:4` and `minimap-renderer.js:48-49` hard-code `16`; use `TILE_SIZE`. `PROP_DEFINITIONS` width/height duplicate `PROP_RECTS` w/h (all 25 currently consistent). `drawProp` could use `canvas.width/height` and drop width/height from the DOM-free defs unless tests need them.
- `monster.js` constructor never initialises `attackCount`; `combat-system.js:101` papers over it with `|| 0`. Add `this.attackCount = 0`.
- `tilemap-renderer.js:54` four-way ternary for cliff row selection is hard to read; a 3-line `if` chain (`!above -> top`, `!below && !info.top -> bottom`, else wall) is clearer.
- `effects-renderer.js:26` drop blink `lifeMs < 3000` and `world-update.js:49` `age < 250` are magic numbers; name them.

## Verified OK (no action)
- **Raw `Image` in registry** (`coin`, `potionHp`, `potionMp`, `faces.*`): `drawImage`, `.width/.height` (natural size after load), `drawScaledSprite`, `drawRotated` all accept `HTMLImageElement`. Fine.
- **Boss flip** (`monster-renderer.js:27-30`): `translate(drawX+w,0); scale(-1,1); drawImage(frame,0,drawY)` mirrors exactly onto `[drawX, drawX+w]`. Idle strip is a front-facing symmetric sprite, so the flip (and the no-flip for `up`/`down`) is visually harmless.
- **PIXEL_SCALE 4**: camera clamp uses derived `VIEW_WIDTH/HEIGHT`; name tags, floating text, world transform all multiply by `PIXEL_SCALE`; minimap is 1 px/tile and independent; mouse clicks are scaled to canvas px and only consumed by screen-space UI. HUD now hides the bottom 26 world px (was 35); `CAMERA_TARGET_Y_OFFSET = 8` still keeps the hero above it. No hard-coded `3`/`48` leftovers.
- **FX keys**: every `spriteKey` emitted (`slash`, `arc`, `circular`, `spark`, `coin`, `potionHp`, `potionMp`) and every marker id (`available`, `turnIn`, `talk`) exists in the registry; renderers null-check anyway.
- **`main.js` innerHTML error path**: message is internal (asset path / stack), no user-controlled input - not an XSS vector.
- **Dialog faceset**: 38x38 at 3x = 114 inside the 122 frame (`dialog-box.js:81-82`); comment is accurate.

## Recommended actions
1. H1.2 asset-manifest node test (also makes H2 impossible to regress), then H1.1 `allSettled` error aggregation.
2. H2 derive strip frame count from image width; delete `frames` fields.
3. M1 share the tint cache; delete `hitFlash` (monster + boss).
4. M3/M4 dead-code deletes in one `chore:` commit.
5. M2 + Low items opportunistically.

## Unresolved questions
- Is `fxFlame` reserved for a planned fire-slime attack? If yes keep it; otherwise it is the only unused strip.
- Are `DialogBoxFaceset.png` / `FacesetBox.png` meant to replace the hand-drawn `drawPanel` frames in `dialog-box.js`? If not, drop them from `ASSET_FILES`.
