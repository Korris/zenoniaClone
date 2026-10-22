# Code Review: seamless HD ground tiles (Oakhaven grid removal)

Scope: tools/hd_tile_texture_utils.py, tools/build-hd-tiles.py, src/graphics/{asset-atlas,tile-sprite-builder,tile-variant-picker,tile-block-renderer,tilemap-renderer}.js, src/world/tile-char-info.js, tests/hd-tiles-config.test.js.
`npm test`: 120/120 pass. Generated sheets probed numerically (scratch python): orientation, seams, alpha bands.

Score: 8.5/10 -- no correctness defects found in the render path; two small texture-build imperfections and one test gap.

## Critical
None.

## High
None.

## Medium

1. tools/hd_tile_texture_utils.py:67,80-81 -- `_heal_axis` triangular weight is asymmetric. `np.arange(-f, f)` yields 2f samples: w[0]=0 but w[-1]=1/f (0.09 for f=11), so every healed strip ends with a ~9% blend step one pixel past `mid+f` on the right/bottom side. Verified numerically. Not visible at today's settings, but it is a hard step of exactly the kind the whole build is trying to remove.
   Fix (3 lines):
   ```python
   w = 1 - np.abs(np.arange(-f, f + 1)) / f          # 2f+1 samples, 0 at both ends
   shape[axis] = 2 * f + 1
   dst_slice[axis] = slice(mid - f, mid + f + 1)
   clean = np.take(rolled, np.arange(src - f, src + f + 1), axis=axis, mode='wrap')
   ```
   Rest of the index math is correct: single-cell sheet (seams=[0]) -> cell=n, src=0, wrap-take reads the cell centre; 2-cell sheet -> src reads the centre of the *other* cell; never straddles a seam.

2. tests/hd-tiles-config.test.js -- piece selection in `drawBlockTile` (openKey -> shape vs 3x3 cell, `isCenter`) is untested because it is inline with ctx calls. Cheap pure test: extract
   ```js
   export function blockPieceKey(top, bottom, left, right) {
     return (top ? 'T' : '') + (bottom ? 'B' : '') + (left ? 'L' : '') + (right ? 'R' : '');
   }
   ```
   and assert `Object.keys(PATH_SHAPES)` equals exactly the keys where at least one opposite pair is both closed (`['', 'B', 'L', 'LR', 'R', 'T', 'TB']`), plus a no-duplicate check in `cellsOf(def)` (block ∪ center ∪ inner ∪ shapes must not overlap -- an off-by-one in PATH_SHAPES/PATH_INNER against the Python SHAPE_CELLS would otherwise pass the bounds test silently).

## Low

3. tools/build-hd-tiles.py:161 / hd_tile_texture_utils.py:86 -- `heal_seams` runs on straight (non-premultiplied) RGBA after `premultiplied_resize` zeroed RGB where alpha=0. Blending a strip with different alpha mixes black RGB into partial-alpha fringe pixels. Measured on path.png T cell: fringe RGB [137,148,40] (green-yellow, from the painted grass) -- benign today, but a larger FEATHER or a stricter `material_alpha` would turn it into a dark halo along the road edge. Fix: premultiply before `heal_seams`, un-premultiply after (helpers already exist in `premultiplied_resize`).

4. hd_tile_texture_utils.py:104-106 -- `material_alpha(soft=0.25)`: yellow-green highlight pixels (G/R ~1.1) stay ~50% opaque, so the grass band of the T cell keeps a ~10% mean alpha (rows 0-5 mean 24-28/255, RGB [196,219,48]) = faint yellow flecks tinting the base grass on every north road edge. L/R/B bands are clean (0-6). The `extra_shapes` opening filter removes these flecks for the synthesized pieces but the painted 3x3 edge cells keep them. Option: apply the same MinFilter(5)/MaxFilter(5) opening to the 3x3 edge-cell alphas, or raise `soft` slightly for the path sheet only.

5. src/graphics/tile-block-renderer.js:34 -- `blockCell` returns the centre column when BOTH left and right are closed (and likewise rows). In the pixel-art fallback (no `Shapes`) a 1-wide lane therefore still draws the plain centre -- acknowledged in the comment, pre-existing behaviour, not a regression.

## Verified OK (asked for explicitly)

- tile-block-renderer.js: piece selection correct (`'TL'` open -> `[2][2]` BR corner; `'TBL'` -> right-edge piece; shapes only for keys where an opposite pair is closed). Inner-corner condition `same(dx,0) && same(0,dy) && !same(dx,dy)` correct; map border (`null` -> same) never produces wedges. `masked()` resets composite op to `source-over` before returning, all state is on the private scratch context, main ctx untouched; scratch resized only when material size changes (fill and base are both 64px in HD, so no thrash). Per-tile allocs: one short string + one 2-tuple -- negligible.
- Pixel fallback: `Shapes`/`Fill`/`Center`/`Inner` all optional (`&&` / `if (!inner) return`); `plazaStone` pixel block alias exists in GROUND_TILES; nothing else in src/ indexes `tiles.plazaStone` or treats `p` as a variants tile (grep). Partial HD load is safe: `buildFromDefs` skips the whole entry when the owning sheet is missing, so `plazaStoneFill` can never exist without the HD block masks.
- tile-sprite-builder.js:64 -- `fill` reads from `images[def.fill.image]` with its own guard; `variantFrames` propagates `macro` onto the array (non-enumerable not needed; `pickVariant` reads `frames.macro`).
- asset-atlas.js: `plazaStone.fill` macro [2,2] with 4 variants in MACRO_ORDER; test `allDefs()` bounds-checks the fill against plaza.png.
- Python orientation (measured on the generated path.png): T cell alpha rows top->bottom [24,7,44,162,254,255..] = grass at top; L cell cols [2,6,63,212,248..] = grass at left; TL wedge TL-quadrant 205 / BR-quadrant 0; `'TB'` shape col means [0,2,59,235,253,238,72,4] = vertical pipe; `'T'` cap dirt rows 0-4 only, grass L/R; island core ~206 (soft but present). Inner-corner flips map to [TL,TR,BL,BR] as the renderer expects.
- Macro seams: grass quadrant boundary diff 9.8-10.8 vs interior neighbour diff 9.6 (seamless); path centre 2.5; plaza 4-9.
- tile-char-info.js: `d`/`p` mutual family, `w` unchanged; `sameEdgeFamily(ch, null)` still true.
- tilemap-renderer.js: `imageSmoothingEnabled` set once and restored; portal ground accepts a block neighbour with `same = () => true` -> open centre (fill macro for plaza).

## Unresolved questions
- Is the island (`''`) shape ever used by a map? If not, the soft core (alpha ~0.8) is moot; if yes, consider `SHAPE_GAIN` 2.0 for that key only.
