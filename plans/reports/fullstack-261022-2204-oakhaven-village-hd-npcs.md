# Oakhaven Village redesign + 10 HD NPCs – implementation report

Date: 2026-10-22 | Builds on `fullstack-261022-1835-oakhaven-village-relayout.md` (map re-layout, same session). Engine/renderer/collision architecture unchanged; map 36x26, ids/portals untouched.

## Phase 1 – inspection results
- Entry/render pipeline unchanged from the survey report (`scout-261022-1705-village-map-survey.md`).
- NPC assets found: `assets/hd/map/Oakhaven/` – `Elder_Rowan.png`, `Mia.png`, `Hunter Lena.png`, `Merchant Dale.png`, `Fisherman Roy.png`, `Carpenter Brol.png`, `Farmer Guri.png`, `Guard Rook.png`, `Child Tim.png`, `nina.png`.
- Measured (not assumed): all 10 are 1448x1086, 4 columns x 3 rows (front / turned-left / turned-right), RGBA with real alpha (0–255, faint halo removed by the alpha<=48 threshold). Rows are not separated by empty gaps on 7 sheets (characters touch) → grid lines snapped to emptiest gaps (hero-script method). First `nina.png` was RGB with a baked checkerboard; user replaced it mid-task with a real-alpha sheet, re-verified. No cleanup beyond haze threshold was needed on any sheet.
- Mismatch vs engine: pack NPCs are 16 units, 4 facings incl. rear; new art is 3 facings, no rear → HD NPCs drawn at hero scale (24 units), `up` → front row.

## Files changed / added
- `tools/build-hd-npcs.py` (new) + `package.json` script `hd:npcs` → `assets/hd/npcs/normalized/<id>.png` (384x288) + `<id>_face.png` (152x152) + contact sheet + README. Reuses build-hd-hero.py helpers (importlib, guarded).
- `src/graphics/asset-atlas-hd-npcs.js` (new), `asset-atlas.js` (spread files), `sprite-registry.js` (`npcsHd[id]`, HD `faces[id]`).
- `src/graphics/prop-renderer.js` (HD idle loop 260 ms/frame driven by `npc.animTimer`, left/right rows, `npcFrameUnits`, hero "Hero" label now dev-only), `world-renderer.js`, `core/constants.js` (`SHOW_DEBUG_LABELS`), `game/world-update.js` (ticks NPC animTimer), `game/game-controller.js` (nearest NPC faces hero; restores `def.dir` when hero leaves; HD portrait lookup), `ui/dialog-box.js` (HD portrait smooth-fit).
- `src/data/npc-definitions.js`: +7 dialogue-only NPCs (merchantDale, fishermanRoy, carpenterBrol, farmerGuri, guardRook, childTim, healerNina). Existing ids, quests, lines untouched.
- `src/world/maps/oakhaven-village-map.js`: `NPC_TILES` (10), yard fence posts per cottage.
- Tests: `tests/hd-npcs-config.test.js` (new). `tools/browser-check.mjs`: NPC presence/HD check, district screenshots, dialog open/close.
- Docs: README, `docs/codebase-summary.md`, `docs/system-architecture.md` (HD NPCs section), `docs/project-roadmap.md` (v0.9).

## NPC integration status
| NPC | id | tile | render | portrait |
|---|---|---|---|---|
| Elder Rowan | elderRowan (existing) | 18,14 plaza | HD animated | HD |
| Mia | villagerMia (existing) | 19,8 brown cottage | HD animated | HD |
| Hunter Lena | hunterLena (existing) | 13,23 gate edge | HD animated | HD |
| Merchant Dale | merchantDale (new) | 4,13 market spot | HD animated | HD |
| Fisherman Roy | fishermanRoy (new) | 30,19 dock landing | HD animated | HD |
| Carpenter Brol | carpenterBrol (new) | 20,21 woodpile | HD animated | HD |
| Farmer Guri | farmerGuri (new) | 8,19 inside farm | HD animated | HD |
| Guard Rook | guardRook (new) | 18,21 south gate | HD animated | HD |
| Child Tim | childTim (new) | 14,12 plaza edge | HD animated (70px body) | HD |
| Nina | healerNina (new) | 26,12 by stone cottage | HD animated | HD |
No static fallbacks needed. Pixel-art fallback remains automatic per id if a normalized file is missing. Field NPCs (minerBork, nomadSaif, knightAldric) unchanged (pixel pack).

## Map layout changes (this task)
NPCs placed by district; fence posts flanking each cottage's front yard. Paths remain the engine's 3x3 autotile dirt (no new tile variants exist) – corners are the autotile's rounded rims, not custom art.

## Verification (actually run)
- `node --check` on every changed JS; `ast.parse` on the Python script. `python tools/build-hd-npcs.py` → RESULT OK (10 sheets, feet-on-bottom validation).
- `npm test` → 116/116.
- `node tools/browser-check.mjs` → `errors []`; 10 NPCs present, all `hd:true`, on their tiles; walk-checks (plaza, farm gate, fence, market, bridge, dock, north lane, gate, portal→meadow) pass; dialog with Elder opens and closes.
- Idle jitter measured on normalized sheets: feet x-drift ≤ 2.2 px of 96 (≈2 screen px), head top 0–1 px.
- Code review (`code-reviewer-261022-2154-hd-npc-integration.md`): 8/10, no critical/high; M1–M3 applied (facing restore, dead fallback removed + test, importlib guard).

## Screenshots (`tools/screenshots/`)
15-village (gate start), 16-village-plaza, 17-village-farm, 17b-village-market, 18-village-bridge, 19-village-dock, 20-village-north, 21-village-gate, 22-village-dialog (HD portrait), 23-village-minimap-overview.

## Limitations / missing assets
- No gameplay for shop / fishing / crafting / healing / guard (no such systems; NPCs are dialogue-only, no stubs).
- Village props still without art: well, lanternPost, marketStall, cropPlot, scarecrow, woodpile, campfire, barrel, crate, rowboat (sizes in previous report); market/woodpile use HD stand-ins (torch, stumps).
- No rear pose in the NPC art: NPC facing up shows the front row.
- browser-check pages the elder dialog with E, which accepts the offered quest in that throwaway session (harmless; noted by reviewer).
- Elder Rowan's lines unchanged by instruction.
