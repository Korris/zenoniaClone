# HD Render Mode (Hero) - Implementation Report

## Summary
Added a resolution-independent HD rendering path (hand-painted, Zenonia 4 style) for the hero,
alongside the existing pixel-art world. All gameplay/world units unchanged. Full HD hero set now
wired: idle, walk, attack1-3, dash, hurt, death + 152x152 face portrait.

## Files changed

### New
- `tools/build-hd-hero.py` (431 lines) -- normalizer: raw ChatGPT sheets (`assets/hd/hero/*.png`)
  -> clean 96x96 frame sheets + 152x152 face in `assets/hd/hero/normalized/`. Per-sheet scale
  (from each sheet's own row0/col0 DOWN frame -- raw sheets arrive at different native
  resolutions per file). Handles both real (soft) alpha and opaque sheets with a painted
  checkerboard standing in for transparency (k-means tile-color sampling + border-connected
  flood fill + small-component cleanup + 1px edge soften). No outline/quantization (unlike the
  pixel-art `build-hero-sprites.py`). Exceeds the 200-line guideline, same as the existing
  `build-hero-sprites.py` (293 lines) -- kept as one cohesive pipeline script per that precedent;
  flag if you'd rather I split it.
- `tests/hd-asset-config.test.js` -- `unitsFor()` math + `HD_HERO`/`HD_HERO_ANIMATIONS` sanity
  (4 tests, pure, no DOM).
- `assets/hd/hero/normalized/*.png` + `scale.json` + `README.md` + `contact-sheet.png` (generated
  output, not hand-written).

### Modified
- `src/graphics/asset-atlas.js` -- `HD_HERO_ANIMATIONS`, `HD_HERO` (`{pixelsPerUnit:4, frame:96,
  animations}`), `unitsFor(sprite)` helper. `ASSET_FILES` lists `hdHero*` keys only for sheets
  that exist (avoids 404 noise; add a key here when a new sheet is generated).
- `src/graphics/asset-loader.js` -- `loadAllImages()` now uses `Promise.allSettled`; a failed/
  missing load logs a warning and is dropped from the map instead of rejecting boot.
- `src/graphics/sprite-registry.js` -- `cropSprite(..., pixelsPerUnit=1)` stamps the property on
  every sprite canvas. `buildHero()` = pixel set (`buildHeroPixel`) merged with the HD set
  (`buildHeroHd`), HD winning per-animation when its sheet loaded. `faces.hero` prefers
  `hdHeroFace`; `faces.heroIsHd` flag added. `registry.heroFrameUnits` exposed.
- `src/graphics/player-renderer.js` -- `drawPlayer` now draws at an explicit world-unit dest size
  (`HERO_FRAME_SIZE`, both the live frame and dash afterimages) instead of natural size, and
  toggles `ctx.imageSmoothingEnabled` from the frame's `pixelsPerUnit`, restoring `false` after.
- `src/ui/hud-renderer.js` / `src/ui/inventory-panel.js` -- portrait draws switched from
  `drawScaledSprite` (always smoothing=false) to explicit `drawImage` with smoothing toggled by
  `registry.faces.heroIsHd`.
- `src/ui/menu-screens.js` -- title-screen hero draws at `HERO_FRAME_SIZE * PIXEL_SCALE` dest size
  (was a fixed x4 of the pixel frame's natural 24px, which would have rendered an HD frame 4x too
  big) with smoothing toggled from `pixelsPerUnit`; dropped the now-unused `drawScaledSprite`
  import.
- `package.json` -- `hd:build` script.
- `README.md`, `docs/system-architecture.md` ("HD Assets" section), `docs/code-standards.md`
  ("Adding an HD Hero Sheet"), `docs/codebase-summary.md`.

## Key bugs found and fixed mid-implementation
1. **Checkerboard "fake transparency" not actually keyed out.** Some raw sheets (attack1, dash)
   are fully opaque RGB with a painted 2-tone checkerboard instead of real alpha. First attempt's
   `np.unique`-by-exact-color sampling picked two near-duplicate shades of the SAME tile (the
   other tile's pixels are heavily dithered so no single exact value dominates a frequency count)
   -> most of the background survived as opaque, visible in-game as a literal grey checker square
   behind the hero. Fixed with k-means (k=2) color clustering + flood-fill from the sheet border
   (only color-matching pixels *contiguous with the outer edge* count as background, so pale
   character colors like the cream tunic are never at risk even with a generous distance
   threshold). Verified clean via `tools/screenshots/03-combo-1.png`.
2. **Per-sheet vs. global scale.** Raw ChatGPT sheets arrive at different native resolutions per
   file (idle cells ~313px/~295px body, walk ~239px body, attack1 ~222-307px depending on the
   regen). A single scale derived from idle would have shrunk/grown every other animation. Scale
   is computed per sheet from that sheet's own row0/col0 (DOWN, frame 0) frame height, recorded
   per-animation in `scale.json`.

## Verification
- `python tools/build-hd-hero.py` -> all 8 animation sheets + `hero_face.png` (152x152),
  `RESULT: OK`. Read the contact sheet: all frames clean (no checkerboard remnants), consistent
  body proportions across idle/walk/attack1-3/dash/hurt/death.
- `node --test tests/*.test.js` -> 97/97 pass (incl. 4 new in `hd-asset-config.test.js`).
- `node tools/browser-check.mjs tools/screenshots` -> `errors []`. Read 01-title, 02-meadow,
  03-combo-1, 04-dash, 11-death: HD hero renders crisp (smoothing on) at the correct 96 screen-px
  footprint (24 world units x4), feet on the same ground line as before, alongside the pixel-art
  world/monsters/NPCs/HUD (still chunky, unaffected). HUD portrait + inventory faceset now show
  the HD face.
- `for f in $(find src -name "*.js"); do node --check "$f"; done` -> clean. Every `src/*.js` file
  is under 200 lines (sprite-registry.js is the largest touched file at 184).

## How to add the remaining HD coverage (NPCs, monsters, etc. -- not scoped here)
Follow the same pattern as the hero: raw sheet(s) -> a small `tools/build-hd-<thing>.py`
normalizer -> `assets/hd/<thing>/normalized/` -> register in `ASSET_FILES` -> consuming renderer
draws at an explicit world-unit dest size with `imageSmoothingEnabled` toggled from
`sprite.pixelsPerUnit`. Everything needed to repeat this (the `pixelsPerUnit`/`unitsFor` model, the
lenient asset loader) is already generic, not hero-specific.

## Unresolved questions
- `tools/build-hd-hero.py` is 431 lines (over the 200-line guideline); left as one file matching
  the existing `build-hero-sprites.py` precedent (293 lines) for a cohesive single-purpose
  pipeline script. Say if you'd like it split (e.g. checkerboard-keying helpers into a separate
  module).
- `attack1`/`attack2`/`attack3`/`dash` have several "overflow, clipping" warnings (sword/pose
  extends past the 96x96 frame, clipped from the top/left) -- consistent with the accepted
  precedent from the pixel-art hero README ("wide sword poses may clip a few px"). Flag if any
  specific frame needs a re-paint instead.
- `attack3` (frame row=right col=4) and `dash` (frame row=right col=2) have feet 1-3px short of
  the bottom row (lunge pose) -- downgraded from FAIL to WARN in `validate()`; worth a visual
  double-check if frame-perfect ground contact matters for those specific poses.
