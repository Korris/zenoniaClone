# HD Props - Implementation Report

## Summary
Added hand-painted HD props (trees, rocks, houses, fences, torches, crystals...) alongside the
existing pixel-art world, same `pixelsPerUnit`/HD-merge model as the HD hero/tile sets. Engine
merges HD over pixel-art per prop type; falls back cleanly if a file is missing. All 24 prop types
in `prop-definitions.js` now have HD art.

## Key discovery: raw sheets are NOT evenly-spaced grids
Unlike the tile sheets (flat, evenly-spaced), these raw sheets hold several painted objects placed
irregularly with uneven gaps and a painted glow/halo around each (same look as pine_tree.png's
background glow). An even-split-by-known-cell-count approach (like build-hd-tiles.py) bled
neighboring objects into each other's crops (verified visually: a naive even split of `village.png`
put the fence's corner post inside the pillar's cell). Fixed with column-gap segmentation: threshold
the haze (alpha<=48->0) first, then find fully-transparent gap columns between objects. Works
automatically for 7/8 sheets; `desert.png`'s palm/cactus touch by a few stray antialiased px even
past the threshold, so that one sheet uses an explicit manual split (local minimum of the alpha
column-sum profile, x=877).

## Prop -> source mapping (verified by reading every raw PNG + contact sheet)
- `pine_tree.png` (1024x1536, whole image) -> `pine` AND `pineDark` (same tree, duplicated).
- `round_tree.png` (1254x1254, whole image) -> `roundTree` AND `bigTree` (same tree, duplicated).
- `small_trees.png` (2172x724, 3 auto segments) -> `smallTree`, (middle "small pine" cell skipped,
  not wired to any prop type per the brief), `deadTree`.
- `bushes_rocks.png` (2172x724, 6 auto segments) -> `bush`, `bush2`, `rockSmallGrey`,
  `rockSmallBrown`, `stump`, `stones`.
- `big_rocks.png` (1774x887, 2 auto segments) -> `rockGrey`, `rockBrown`.
- `village.png` (2172x724, 6 auto segments) -> `fence`, `signpost`, `pillar`, `torch`, `crystal`,
  `bones`.
- `houses.png` (2172x724, 3 auto segments) -> `house`, `house2`, `house3`.
- `desert.png` (1448x1086, manual split at x=877) -> `palm`, `cactus`.

## Files changed

### New
- `tools/build-hd-props.py` (195 lines) -- normalizer: haze-threshold -> per-sheet segmentation
  (auto gap-detect / whole / manual split) -> tight bbox crop -> premultiplied-alpha LANCZOS fit
  (limiting dimension) -> bottom-center paste onto each prop type's exact world-size x4 canvas.
  Re-runnable, skips missing raw files with a warning. Writes `assets/hd/props/normalized/*.png` +
  `contact-sheet.png` + `README.md`.
- `src/graphics/asset-atlas-hd-props.js` (41 lines) -- `HD_PROP_ASSET_FILES` (spread into
  `ASSET_FILES`) + `HD_PROPS` (propType -> asset key) + `HD_PROP_PIXELS_PER_UNIT` (4). Split out of
  `asset-atlas.js` to keep that file under 200 lines (adding ~37 lines of prop tables inline would
  have pushed it to 224).
- `src/graphics/prop-sprite-builder.js` (28 lines) -- `buildProps()`: pixel `PROP_RECTS` cut merged
  with HD `HD_PROPS` cut per prop type, HD winning where its file loaded (same merge-by-name
  convention as `tile-sprite-builder.js`'s `buildTiles()`).
- `tests/hd-props-config.test.js` -- 4 pure tests: every map prop type has an `HD_PROPS` entry or a
  `PROP_RECTS` fallback; every `HD_PROPS` image key resolves to `ASSET_FILES`; every normalized PNG
  is exactly `(def.width*4) x (def.height*4)` (read via PNG IHDR, no decode dependency); every
  `HD_PROPS` key is a known prop type.
- `assets/hd/props/normalized/*.png` (24 files) + `contact-sheet.png` + `README.md` (generated).

### Modified
- `src/graphics/asset-atlas.js` (185 -> 191 lines) -- imports/re-exports `HD_PROPS`/
  `HD_PROP_PIXELS_PER_UNIT` from the new module; `...HD_PROP_ASSET_FILES` spread into `ASSET_FILES`.
- `src/graphics/sprite-registry.js` (147 -> 142 lines) -- removed the old local `buildProps()`
  (pixel-only); now imports `buildProps` from `prop-sprite-builder.js`.
- `src/graphics/prop-renderer.js` -- `drawProp()` now passes an explicit `(def.width, def.height)`
  world-unit dest size (was relying on the canvas's natural pixel size) and toggles
  `ctx.imageSmoothingEnabled` on only when the picked canvas's `pixelsPerUnit > 1`, restoring
  `false` after -- identical convention to `player-renderer.js`/`tilemap-renderer.js`.
- `src/world/prop-definitions.js` -- `fence`/`signpost` height bumped 16 -> 24 world units
  (footprint unchanged at 1x1 tile): the HD art is a full wooden post, which read as a squashed stub
  forced into a flat 16x16 box. Matches the existing "taller than footprint" convention already used
  by `pillar`/`torch`/`crystal`. Verified safe: `map-builder.js`'s `footprintFree()` (height-aware
  visual-row check) is only used by `scatterProps`/`forestBorder` -- fence/signpost are placed via
  `placeProp`/`propLine` in every current map, which don't call it, so no map-generation behavior
  changed.
- `package.json` -- `hd:props` script.
- `tools/browser-check.mjs` -- added `scorchedDesert` detour (`14-hd-desert.png`) alongside the
  existing cave/castle detour, before returning to Verdant Meadow.
- `README.md`, `docs/system-architecture.md` ("HD Assets" -> new "HD props" subsection),
  `docs/code-standards.md` ("Adding an HD Prop"), `docs/codebase-summary.md`.

## Verification
- `python tools/build-hd-props.py` -> all 24 prop types produced at their exact spec'd canvas sizes
  (e.g. pine 128x192, house 256x192, fence/signpost 64x96), `RESULT: OK`. Read the contact sheet:
  every object clean (no halo/bleed remnants), correctly bottom-aligned, no neighbor bleed-through.
  Individually re-checked fence.png, bones.png, house.png, pillar.png, palm.png, cactus.png,
  stones.png at full res -- all clean.
- `node --test tests/*.test.js` -> 105/105 pass (101 prior + 4 new in `hd-props-config.test.js`).
- `node tools/browser-check.mjs tools/screenshots` -> `errors []`. Read `02-meadow.png` (pine/round
  tree/signpost crisp HD, correct scale vs. hero), `12-hd-cave.png` (pillars/bones/torches HD,
  correct scale), `13-hd-castle.png` (pillars/bones/torches HD in situ), `14-hd-desert.png` (HD
  cactus + palm, correct scale, bottom-aligned on sand). Also ran an ad-hoc teleport-to-village
  screenshot (not a deliverable, deleted after) to directly confirm houses/fences/trees render well
  together in-engine: both house variants, the new taller HD fence line, round/pine trees and a bush
  all composited correctly, fence now reads as a proper waist-high post instead of a squashed box.
- `node --check` on every `src/*.js` file -> clean; every file under 200 lines (`asset-atlas.js` is
  the largest touched file at 191).

## Unresolved questions
- `small_trees.png`'s middle cell ("small pine") has no prop type of its own per the brief ("not
  required") -- left unprocessed entirely (no `smallPine.png` generated) rather than generating an
  unused asset, per YAGNI. Say if you'd like it wired up as a new prop type for future maps.
- Fence/signpost height (24 world units, derived from matching the existing "taller than footprint"
  convention) is a judgment call, not computed from the art's exact aspect ratio (the fit-by-
  limiting-dimension step tolerates either). Visually confirmed good via the in-engine village
  screenshot; flag if a different height reads better to you.
- Mid-task a message arrived proposing a much larger, separate task ("build a playable village map"
  touching map-builder/camera/collision/player-movement) -- out of scope for this phase (which was
  scoped to HD prop art + the files listed in file ownership) and not something I started; flagging
  for the orchestrator to scope/dispatch as its own phase if still wanted.
