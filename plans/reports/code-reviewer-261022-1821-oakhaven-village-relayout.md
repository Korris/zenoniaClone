# Code Review: Oakhaven Village re-layout

Scope: `src/world/maps/oakhaven-village-map.js`, `src/world/prop-definitions.js`, `src/graphics/asset-atlas-hd-props.js`, `src/ui/minimap-renderer.js`, `tools/build-hd-props.py`, `tests/helpers/map-reachability.js`, `tests/map-invariants.test.js`, `tests/oakhaven-village-layout.test.js`, `tools/browser-check.mjs`.

Verification run:
- `npm test` -> 112/112 pass.
- `node --check` on all 8 changed JS files -> clean.
- `python tools/build-hd-props.py assets/hd/props <scratch>` -> exit 0, `WARN skipping village_props.png ... not found` + `WARN: no art produced for ...10 types`, contact sheet + README written. Missing sheet does NOT break the run.
- Numeric grid dump (scratch node script): 103 props, 0 footprint overlaps, 0 props on `w`/`B`/`C`/`P`, NPC tiles `d`/`g`/`g`, start (16,21)=`d`, portal (16,23)=`P`. Tile solidity: only `w`,`C` solid; `q`,`B`,`d`,`P` walkable.
- Screenshots 15-19 fresh (18:34-18:35), village + bridge look right.

Overall: 8/10. Layout is correct against `carvePath` fill semantics (comments on lines 25-28 match the actual col/row ranges), reserved spots are honored by scatter, hero cannot be trapped. Findings below are minor.

## Critical
None.

## High
None.

## Medium

1. `src/ui/minimap-renderer.js:12-18` -- PROP_COLORS has NO entries for the 10 new types (well, lanternPost, marketStall, cropPlot, scarecrow, woodpile, campfire, barrel, crate, rowboat). Change list says additions were made; they are not in the file. Minimap silently `continue`s on missing color, so when art lands the props vanish from the minimap with no test failure. Fix:
   ```js
   well: '#9aa8a8', lanternPost: '#ffb040', marketStall: '#d85040', cropPlot: '#7aa030', scarecrow: '#c8a060',
   woodpile: '#8a5a3a', campfire: '#ff7020', barrel: '#8a5a3a', crate: '#c89860', rowboat: '#a07040',
   ```

2. `src/world/prop-definitions.js:25` + `oakhaven-village-map.js:59` -- `cropPlot: def(2,1,2,1)` is solid; six spots (4/6/8, 20) and (4/6/8, 22) fill cols 4-9 of rows 20 and 22 completely. Gate enters at (9,21); row 19 (scarecrow row) becomes unreachable once the art lands, and the farm interior collapses to a single corridor. The def is the art contract being fixed now, so decide now: make crops walkable like `stones` (`cropPlot: def(2, 1, 2, 1, false)`) or drop the (8,20) plot so (9,20)->(9,19) stays open.

3. `tests/oakhaven-village-layout.test.js:52-57` -- portal-arrival check is one-directional and village-specific; village->meadow target (4,15) is unverified by any test. Generalize once in `tests/map-invariants.test.js` loop (DRY, covers all 4 portals on all maps):
   ```js
   for (const p of map.portals) {
     const target = MAP_REGISTRY[p.targetMap];
     assert.ok(target, `${map.id} portal -> unknown map ${p.targetMap}`);
     assert.ok(reachableFrom(target).has(`${p.targetTx},${p.targetTy}`), `arrival ${p.targetTx},${p.targetTy} blocked`);
     assert.notEqual(target.rows[p.targetTy][p.targetTx], 'P', 'arrival on a portal tile');
   }
   ```
   then delete the village-specific test (cache `reachableFrom` per map if runtime matters; currently ~ms).

## Low

4. `tools/build-hd-props.py:250-255` (`write_readme`) -- emits the `### village_props.png` section even though the sheet was skipped; README header says "Generated from the raw sheets in assets/hd/props/*.png", so it now documents a file that does not exist. Also `assets/hd/props/normalized/README.md` on disk is stale vs. script output (diff = that section) -- self-heals on next `npm run hd:props`. Suggest: `heading = f'### {name}' + ('' if name in results_by_sheet else ' (not painted yet)')` or keep as-is and accept.

5. `oakhaven-village-map.js:72` -- barrel spot (23,19) is currently under the sprite of stump (23,20) (stump is 2 tiles tall -> visual rows 19-20). Only safe because the stumps are removed in the same swap; add that to the comment or move barrel to (25,19).

6. `tests/oakhaven-village-layout.test.js:10` -- `reachableFrom(MAP)` runs at module load; a throw there aborts the file with zero named tests instead of one failing test. Wrap in a lazy getter or `before()`.

7. `oakhaven-village-map.js:17` -- comment "roof overflow row (visual only, not solid)" -- the reserved row is the sprite's top row (house is 3 rows tall, footprint 2), reserved so scatter props don't get drawn behind the roof. Comment wording slightly misleading; "top roof row: keep clear so no scatter prop hides behind it" is the actual reason.

## Checks that passed (no action)
- carvePath ranges: north lane cols 4-30 rows 9-11; spine cols 15-17 rows 9-24; crossbar cols 6-32 rows 14-16, river/bridge overwrite cols 31-33 correctly, east stub col 34 rows 14-16 reserved so forestBorder/scatter can't block the bank.
- Fences: rows 18/23 cols 3-10 solid, col 3 rows 19-22 solid, col 10 solid except gate (20,21); gate lane cols 11-14 rows 20-21 reaches spine. Torch (11,19) placed inside the reserved area intentionally (placeProp ignores `reserve` -- fine, explicit).
- NPCs: elder (18,14) on plaza `d`, Mia (19,8) beside house2 doorstep, Lena (13,23) with 4 open neighbours; no prop sprite covers any NPC/portal/start (map-invariants).
- Arrival (16,21) is `d`, two tiles above `P` (16,23) -> no bounce; `loadMap` falls back via `findWalkableNear` anyway.
- Dock cols 31-32 rows 20-21 end in `w` at col 33; rowboat reserve (32-33,23) on water.
- East bank strip col 34: only 1x1 scatter can land there (2-wide footprints hit `C` at col 35); dead-end at worst, no trap.
- `well`/`marketStall` spots inside `reserve` ring -> scatter cannot take them; plaza ring around well verified by test.
- PROP_TARGETS entries match prop-definitions sizes for all 10 new types (manually cross-checked).
- `hd-props-config.test.js` only asserts types *used on maps* have art -> new defs without HD_PROPS entries are safe.
- `tests/helpers/map-reachability.js` mirrors `map-manager.buildProps` + NPC blocking; faithful extraction.
- browser-check thresholds have adequate margin (80 px/s, 700-1200 ms, blockers at known cols).

## Unresolved questions
1. Was the PROP_COLORS edit lost (file shows none of the 10 types), or was it intentionally deferred until art exists? If deferred, the change list is wrong.
2. Should crops be walkable (farm as decoration) or solid (farm as obstacle course)? Drives finding 2.
