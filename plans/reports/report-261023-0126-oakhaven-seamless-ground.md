# Oakhaven ground grid: root cause + fix (2026-10-23)

Screenshots (before / after / side-by-side, same hero positions, same zoom):
`plans/reports/screenshots/261023-oakhaven-seamless-ground/` -- `plaza`, `path-south`,
`path-west`, `north-grass`, `river`.

## Step 1 -- Investigation

| Suspect | Finding | Verdict |
|---|---|---|
| Debug grid overlay | No grid/strokeRect code in world/tile rendering (only HUD/skill panel frames). | not a cause |
| Renderer (scale-4 transform + smoothing, 16px dest) | Flat-colour 64px tile drawn 6x6 through the same drawImage path: one single RGB value across every cell boundary (row + column probe). Camera offsets are integer (`renderX` rounds). | not a cause |
| Texture: grass cells | Normalized cell edges 15-25 luminance darker than interiors (e.g. r0c0 inner 116 vs top/bottom 96). Raw sheet has a ~12px dark band at every panel boundary (cols 887-898). 4 variants had mean luminance 113/116/125/125 -> checkerboard. Cells are separate paintings, no edge continuity. | **primary cause** |
| Texture: path block | Raw grid strokes at the 1/3 boundaries (cols 417/418, 834/835). Edge cells carry their own painted grass (a different green than the grass tiles) -> visible rim line between road and lawn. Old build stretched a 40px crop to 64px for the center (blurry). | cause |
| Texture: stone floor (plaza) | Cells not seamless (bottom row 120 vs inner 133), drawn as plain `variants` with no transition to grass -> hard stepped square outline. | cause |
| Autotile coverage | 3x3 block only: no inner (concave) corners, and a 1-wide path tile with path above+below selected the plain CENTER cell (square dirt block). | cause |
| Variant picker | `(tx*7+ty*13)%4` scatter can't be seamless with independent paintings. | contributing |

## Step 2 -- Fix (texture pipeline + autotile, no map/gameplay changes)

**Build (`tools/build-hd-tiles.py` + new `tools/hd_tile_texture_utils.py`)**
- Crop inside each cell's painted border (`margin` per sheet), LANCZOS to 64px.
- Seam-heal every cell on the axes it repeats along (feathered strip of clean interior texture blended over each boundary incl. the wrap edge).
- Grass: brightness-match the 4 variants, heal them into ONE 2x2 seamless macro; variants = quadrants. Flower tiles = same quadrants + flowers lifted off the raw flower cells (hue mask), so `q` tiles never introduce a seam.
- Path/water blocks -> RGBA: grass pixels transparent (hue mask), so the seamless base grass shows through the painted transition instead of a second non-matching grass. Path center -> 2x2 macro; 4 inner-corner grass wedges; 7 extra pieces (1-wide pipes, 4 caps, island) synthesized from the edge alphas.
- `plaza.png`: 2x2 seamless stone macro (castle floor cells + mirrored), drawn THROUGH the path block's silhouette -> stone-on-grass transitions with the same organic edges/inner corners as the road.
- Floors (sand/castle/cave/lava) -> seamless 2x1 macros; cliffs healed horizontally; water anim frames brightness-matched to the center.

**Renderer**
- `tile-variant-picker.js`: `macro`-tagged sets pick by tile position (`(ty%rows)*cols + tx%cols`); pixel-art sets unchanged.
- `tile-block-renderer.js` (split out of tilemap-renderer.js): base grass -> 3x3 piece or `shapes[openSides]` -> `fill` through mask / `center` macro -> `inner` wedges from diagonal neighbours. One reused 64px scratch canvas, `destination-in` masking.
- `tilemap-renderer.js`: portal ground now also accepts a block neighbour (gate portal sat on a black square because its neighbours are road tiles).
- `tile-char-info.js`: `p` plaza = block kind (sprite `plazaStone`, base grass); `d`<->`p` mutual family (road meets plaza with a butt joint, no grass rim between).
- Pixel-art fallback: `GROUND_TILES.plazaStone` aliases the dirt block (previously undefined -> would throw); extras are HD-only and simply absent.

Not done, by request: no coordinate-only map redesign, no prop scattering, no blur, no static concept background. Map size, movement, collision, NPC IDs, quests, portals, HUD untouched.

## Step 3 -- Verification

- `npm test`: 122/122 pass (6 new pure tests: macro/variant mapping, blockCell, blockPieceKey covers exactly the 7 missing shapes, shapes/inner piece sets, no duplicate cells per def, nested `fill` bounds).
- Code review (`plans/reports/code-reviewer-261023-0126-seamless-hd-ground-tiles.md`): 8.5/10, no critical/high. Applied: heal weight made symmetric (`arange(-f, f+1)`, removed a ~9% step at strip ends), morphological opening on the painted edge alphas (removes flower flecks in the grass band), `blockPieceKey` extracted + tested, duplicate-cell test. Not applied (low, documented): premultiplying around `heal_seams` (fringe is benign at current feather) and the pre-existing pixel-fallback center-column behaviour.
- Real gameplay screenshots (headless Chrome, `http://127.0.0.1:5173`, new game, hero teleported to fixed tiles): grass lattice gone; plaza has organic stone-on-grass edge with rounded inner corners; road edges blend into the same grass; 1-wide feeder lanes read as narrow trails instead of square blocks; gate portal sits on dirt. Water bank seams reduced (banks' grass now transparent). See screenshot folder above.
- `npm run check:browser`: boot/NPC/combat/skills/inventory/death checks pass; 2 pre-existing failures remain ("dialog did not open near the elder", village walk-check) -- the script's coordinates predate the V4/V5 layout (well at (15,15) now blocks the "still empty well spot" walk; elder moved). Collision only reads tile `solid` + prop footprints, neither touched here.

## Files

- new: `tools/hd_tile_texture_utils.py`, `src/graphics/tile-variant-picker.js`, `src/graphics/tile-block-renderer.js`, `assets/hd/tiles/normalized/plaza.png`
- changed: `tools/build-hd-tiles.py`, `assets/hd/tiles/normalized/*` (regenerated; `path.png` now 320x256), `src/graphics/asset-atlas.js`, `src/graphics/tile-sprite-builder.js`, `src/graphics/tilemap-renderer.js`, `src/world/tile-char-info.js`, `tests/hd-tiles-config.test.js`, docs (`system-architecture.md`, `code-standards.md`, `codebase-summary.md`, `project-roadmap.md`)

## Known limits / follow-ups

- Water block has no inner-corner wedges (Oakhaven river has no concave corners; add `inner` to `waterBlock` if a map needs it).
- Dirt<->stone joint is a straight butt joint (no painted dirt-to-stone edge).
- Stone macro uses mirrored copies of 2 painted cells; a 4-cell raw sheet would give more variety.
- `tools/browser-check.mjs` village walk-checks should be re-targeted to the current layout.

## Unresolved questions

- None blocking. Is the thinner look of 1-wide feeder trails acceptable, or should those lanes be widened to 2 tiles in the map?
