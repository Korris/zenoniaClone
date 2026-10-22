# Oakhaven Village (new starting hub) - Implementation Report

## Summary
Built option B per orchestrator direction: a new dedicated village map (`oakhavenVillage`), now the
game's starting hub, composed after `assets/hd/map/village_reference.png` using only props already
produced by the HD prop pipeline (no fabricated/mismatched art). No core engine changes needed --
camera, player movement and collision are fully generic over map data.

## Files changed

### New
- `src/world/maps/oakhaven-village-map.js` (106 lines) -- 36x26 tiles, cliff ring, 3 houses
  (red/brown/stone-blue roofs matching the reference almost 1:1), fenced farm plot (SW), river +
  bridge (E), central plaza, forest/rock/bush scatter, 6 torches flanking path junctions, 3 NPCs,
  1 portal -> Verdant Meadow. Safe hub: `spawnZones: []`, `boss: null`.

### Modified
- `src/world/map-registry.js` -- registers `OAKHAVEN_VILLAGE_MAP`; `FIRST_MAP_ID` now
  `'oakhavenVillage'` (was `'verdantMeadow'`). `getMapDefinition()`'s existing `MAP_REGISTRY[id] ||
  MAP_REGISTRY[FIRST_MAP_ID]` fallback needed no change -- verified it still covers an unknown/old
  save `mapId` (and any pre-existing save with `mapId:'verdantMeadow'` still resolves directly,
  since that map id still exists).
- `src/world/maps/verdant-meadow-map.js` -- removed `elderRowan`/`villagerMia`/`hunterLena` from
  `npcs` (moved to the village) and their now-unused `reserve()` calls; added a portal back to
  Oakhaven Village at the west start clearing (`tx:3,ty:15` -> village `tx:16,ty:21`, `minLevel:1`).
  `playerStart` unchanged. The old 2-house/fence cluster stays as flavor (now "unowned cottages").
- `tools/browser-check.mjs` -- new game now screenshots `15-village.png` first (starting hub
  changed), then teleports the hero onto the village's south-gate portal tile and lets the running
  game loop detect + transition it (same portal-trigger path a real walk-in uses), screenshotting
  the arrival as `02-meadow.png` before the rest of the script continues (unchanged, assumes
  Verdant Meadow).
- `README.md` (5 maps, hub mentioned), `docs/system-architecture.md` (5 maps note),
  `docs/codebase-summary.md` (new map row + registry/FIRST_MAP_ID note).

### Unchanged (verified, no edit needed)
- `src/data/npc-definitions.js` / `src/data/quest-definitions.js` -- quest ids/dialog untouched per
  instruction; `roadToCave`'s `reach('darkCave')` objective and all `kill(...)` objectives are
  unaffected by which map the quest-giver NPC stands in.
- `tests/map-invariants.test.js` -- already generic over `Object.values(MAP_REGISTRY)`, so it
  covered the new map with zero edits (2 new passing subtests appeared automatically: coverage +
  reachability for `oakhavenVillage`).
- Camera (`core/camera.js`), collision (`world/tilemap.js`), player movement -- fully generic over
  map data; a new map "just works" once its tiles/props flag solidity correctly (already the
  convention via `TILE_INFO`/`PROP_DEFINITIONS`).
- Minimap colors -- all props used already have `PROP_COLORS` entries except fence/torch (same gap
  as every other existing map, not a regression).

## Reference-to-asset mapping (from `assets/hd/map/village_reference.png`, viewed directly)
Matched directly: 3 house variants (red/brown/stone-blue roofs), fence, torch (stand-in for the
reference's hanging lantern posts), pine/roundTree/smallTree/bush/stump/rock clusters, river+bridge
tile. **No art exists yet for**: well, market stall + awning, crop rows, scarecrow, wooden dock,
rowboat (checked both `assets/hd/props/` and the full pixel-art pack -- nothing matches). Per "don't
invent asset paths," these got existing-asset stand-ins instead of fabricated art, each commented
in the map file with the exact tx/ty so real HD props can be swapped in later without re-laying the
map:
- well -> `rockSmallGrey` + a flower-tile ring at the plaza (tx15,ty14)
- lantern posts -> `torch` (6, at path junctions)
- woodpile+campfire -> a 2-stump cluster (tx20/22,ty20)
- dock -> a `rockSmallGrey` cluster on the near bank (tx33,ty17)
- marketStall / farmPlot(crops) / scarecrow / rowboat -> **no prop placed**, just a `reserve()`'d
  tile + comment (tx3,ty16 / tx5-9,ty18-21 / tx7,ty19 / tx33,ty19) so scatter never covers them and
  the exact future prop position is self-documented in the file.

## Layout notes (debugged via direct math + screenshots, not guesswork)
`carvePath()`'s actual fill extents don't match the naive "width centered on x" assumption -- traced
through the helper to get each path segment's real column/row span before placing torches/props, to
avoid a torch sitting on top of a house corner or blocking a path tile (caught and fixed 2 such
overlaps before running tests: a torch over house2's footprint, and 2 torches sitting directly on
the plaza crossbar). Bridge rows were aligned to exactly match the crossbar's carved rows (15-17,
not an offset 14-16) so the river crossing has no 1-row dead gap, matching the existing
Verdant-Meadow convention (bridge rect fully encompassing the road rect).

## Verification
- `node --test tests/*.test.js` -> 107/107 pass (105 prior + 2 new generic map-invariants subtests
  for `oakhavenVillage`, zero test-file edits needed).
- `node --check` every `src/*.js` -> clean; new map file 106 lines, all files still < 200 (no layout
  helper split needed).
- `node tools/browser-check.mjs` -> `errors []`. Read `15-village.png` (new-game start: village
  square, Hero, woodpile stumps, pines, rock, fence -- matches the reference's composition style),
  `02-meadow.png` (post-walk-through-portal arrival in Verdant Meadow, old return portal visible
  next to the signpost). Ad-hoc extra screenshots (not a deliverable, deleted after) at the houses
  (Mia + flanking torches), plaza (Elder Rowan + the well-stand-in rock, quest `!` marker visible),
  farm (fenced yard + torch), and bridge (crossing + dock-stand-in rocks) all confirmed clean
  composition, correct HD scale, no clipping/floating props, portal-walk transition fires correctly
  via the real per-frame portal-detection path (not a hard `enterMap()` teleport).

## Unresolved questions
1. Four reference elements (well, market stall, crop rows + scarecrow, dock, rowboat) have no art
   in this repo yet -- stand-ins/placeholders only, per "don't fabricate." If you want real HD props
   for these, that's a new `tools/build-hd-props.py`-style pipeline run needing raw source sheets
   for each (none currently in `assets/hd/`).
2. `elderRowan`'s existing dialog line ("Welcome to Verdant Village, young one...") now reads oddly
   standing in Oakhaven Village -- left untouched per "quest ids/dialog unchanged," flag if you'd
   like a copy pass.
3. Didn't touch `docs/project-roadmap.md`/`docs/project-overview-pdr.md`'s historical "4 maps"
   references (completed-milestone records, not live specs) -- in scope was just "README (first
   map) and docs (maps list)," which I read as system-architecture/codebase-summary + README. Say
   if you want the roadmap/PDR docs touched too.
