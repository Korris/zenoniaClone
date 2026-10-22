# HD Ground Tiles - Implementation Report

## Summary
Added hand-painted HD ground tiles (grass, dirt path, water, floors, cliffs/walls, portal)
alongside the existing pixel-art world, same `pixelsPerUnit`/HD-merge model as the HD hero set.
Engine merges HD over pixel-art per tile name; falls back cleanly if a sheet is missing.

## Cell map used (confirmed by reading each raw PNG, all matched the brief exactly)
- `grass.png` 1774x887 -> split 4 cols x 2 rows: row0 = 4 plain grass variants, row1 = 4
  grass+flowers variants.
- `path.png` 1254x1254 -> 3x3 dirt-on-grass auto-tile block (TL T TR / L C R / BL B BR).
- `water.png` 1086x1448 -> 3 cols x 4 rows: rows0-2 = 3x3 water-on-grass auto-tile block (banks),
  row3 col0/1 = 2 open-water animation variants, row3 col2 = wooden bridge.
- `floors.png` 1774x887 -> 4x2: row0 = sand x2 + castle floor x2, row1 = cave floor x2 + lava x2.
- `walls.png` 2126x740 -> 5x1: cliff top (grass lip), cliff wall, cliff bottom (shadow), castle
  wall, cave wall.
- `portal.png` 2172x724 -> 3x1 magic-circle frames, real alpha.

## Files changed

### New
- `tools/build-hd-tiles.py` (151 lines) -- evenly-splits each raw sheet by its known `(cols, rows)`
  grid (cells are not on an exact pixel grid) and LANCZOS-resizes every cell to 64x64. Simpler than
  `build-hd-hero.py`: no bbox/checkerboard detection needed since these are flat tile textures, not
  character silhouettes. Only `portal.png` gets alpha handling (haze threshold <=48 before AND
  after resize, premultiplied-alpha resize to avoid fringing); the other 5 sheets are opaque.
  Writes normalized sheets + `contact-sheet.png` + `README.md` to `assets/hd/tiles/normalized/`.
  Re-runnable, skips missing raw files with a warning.
- `src/graphics/sprite-cell.js` (27 lines) -- `cropSprite()`/`cell()` split out of
  sprite-registry.js so tile-sprite-builder.js can import them without a circular dependency
  (sprite-registry.js now imports `buildTiles()` from tile-sprite-builder.js).
- `src/graphics/tile-sprite-builder.js` (37 lines) -- `buildTiles(images)`: pixel `GROUND_TILES`
  cut (+ procedural portal fallback) merged with HD `HD_TILES` cut per tile name, HD winning where
  its sheet loaded (same per-name merge convention as `buildHero()`). Returns `{tiles, tilesHd}`.
- `tests/hd-tiles-config.test.js` -- 4 pure tests: every `HD_TILES.image` resolves to an
  `ASSET_FILES` key; every `HD_TILES` cell is within its sheet's real PNG bounds (read via IHDR
  bytes, no decode dependency); every ground-tile sprite name `tile-char-info.js` can reach has an
  `HD_TILES` entry; HD-only names (`flowers`/`waterAnim`) have the expected frame counts.
- `assets/hd/tiles/normalized/*.png` + `contact-sheet.png` + `README.md` (generated output).

### Modified
- `src/graphics/asset-atlas.js` (148 -> 185 lines) -- `hdGrass`/`hdPath`/`hdWater`/`hdFloors`/
  `hdWalls`/`hdPortal` in `ASSET_FILES`; `HD_TILES` table (same `{image, variants}`/`{image,
  block}` shape as `GROUND_TILES`) + `HD_TILE_CELL`/`HD_TILE_PIXELS_PER_UNIT` constants.
- `src/graphics/sprite-registry.js` (184 -> 147 lines) -- removed the old local `buildTiles()` and
  `cropSprite`/`cell` definitions (moved out); now `...buildTiles(images)` spreads `{tiles,
  tilesHd}` into the registry; kept `flowers: FLOWER_OVERLAYS.map(...)` (pixel-art overlay,
  separate from the new `tiles.flowers` HD full-tile variants).
- `src/graphics/tilemap-renderer.js` (62 -> 97 lines) -- every `drawImage` now passes an explicit
  `TILE_SIZE` destination size; `ctx.imageSmoothingEnabled` toggled once per `drawTilemap()` call
  from `registry.tilesHd` (not per tile), restored to `false` at the end; `'flowers'` kind draws
  `tiles.flowers` directly when HD (no overlay); `'block'` kind cycles the water block's open
  center cell (`row===1 && col===1`) through `tiles.waterAnim` every 450ms while edges stay static;
  `'animated'` (portal) fills a dark `PORTAL_BACKDROP` square before compositing when HD (see bug
  below).
- `tools/browser-check.mjs` -- added a detour right after `02-meadow.png`: `enterMap('darkCave')`
  -> `12-hd-cave.png`, `enterMap('ruinedCastle')` -> `13-hd-castle.png`, then back to
  `verdantMeadow` before the rest of the script (which assumes that map).
- `package.json` -- `hd:tiles` script.
- `README.md`, `docs/system-architecture.md` ("HD Assets" -> new "HD ground tiles" subsection),
  `docs/code-standards.md` ("Adding an HD Ground Tile"), `docs/codebase-summary.md`.

## Bug found and fixed mid-implementation
**Portal rendered as a solid black square.** `game-controller.js` clears the whole canvas to black
every frame (`ctx.fillRect` with `fillStyle='#000'`) -- the old procedural portal tile was always
fully opaque so this never mattered. The new HD `portal.png` has real (correct, verified via numpy)
alpha outside the magic circle, so its transparent corners let the black clear color show through
as a hard-edged square. Fixed by filling a `PORTAL_BACKDROP` ('#2b2b38', matching the old
procedural tile's base color) under the HD frame only when `tilesHd` is true, restoring the
intended dark-void vignette look. Verified via a cropped close-up of `12-hd-cave.png`.

## Verification
- `python tools/build-hd-tiles.py` -> all 6 sheets at the exact spec'd sizes (grass 256x128, path
  192x192, water 192x256, floors 256x128, walls 320x64, portal 192x64), `RESULT: OK`. Read
  `contact-sheet.png`: clean cuts, no seams within cells.
- `node --test tests/*.test.js` -> 101/101 pass (97 prior + 4 new in `hd-tiles-config.test.js`).
- `node tools/browser-check.mjs tools/screenshots` -> `errors []`. Read `02-meadow.png` (verified
  by cropping+zooming: grass/path/cliff/water/bridge all crisp painted HD, correct auto-tiling, no
  seams; water sparkle animation confirmed differs between two captures 460ms apart), `12-hd-cave.png`
  (cave floor/wall HD, portal backdrop fix confirmed via crop), `13-hd-castle.png` (castle floor/wall
  HD, portal HD, hero still pixel-art-adjacent HD hero renders fine alongside).
- `node --check` on every `src/*.js` file -> clean; largest touched file `asset-atlas.js` at 185
  lines, all under 200.
- Debug scripts used during investigation (`tools/debug-hd*.mjs`, `tools/screenshots/debug-*.png`)
  were deleted after use; not part of the deliverable.

## Unresolved questions
- `tools/screenshots/09-death.png` and `hd-tiles-contact.png` are stale leftovers from earlier
  sessions (different naming scheme / unclear origin, not written by this task) -- left in place
  since `tools/screenshots/` isn't a tracked deliverable directory and deleting unrelated files
  felt out of scope; flag if you'd like them removed.
- The portal backdrop fill is a flat square (`PORTAL_BACKDROP`) rather than a soft circular
  vignette matching the glow's silhouette -- same visual convention as the old procedural tile
  (which was also a flat square), so kept for consistency, but say if a soft-edged backdrop is
  wanted instead.
