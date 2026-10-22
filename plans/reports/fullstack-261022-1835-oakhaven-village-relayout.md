# Oakhaven Village re-layout – implementation report

Date: 2026-10-22 | Plan: `plans/reports/scout-261022-1705-village-map-survey.md` (approved) | Map stays 36x26, ids/portals/NPC ids unchanged. No engine/renderer/collision changes.

## Files changed
- `src/world/maps/oakhaven-village-map.js` – rewritten layout (131 lines): north lane + doorstep stubs for 3 cottages, spine to south gate, crossbar to plank bridge, dirt plaza (5x5) with reserved 2x2 well spot, farm fence ring with 2-tile east gate + gate lane, woodpile corner (stump x2 + torch stand-ins), river-bank landing + plank dock (`B` tiles), signpost at gate. All no-art spots `reserve()`d and commented with exact tx/ty.
- `src/world/prop-definitions.js` – 10 defs for village props without art (not in HD_PROPS yet). `cropPlot` walkable.
- `tools/build-hd-props.py` – `PROP_TARGETS`, `SHEETS['village_props.png']` (auto split, 10 outputs in order), `CELL_NOTES`. Missing sheet → WARN, exit 0.
- `src/graphics/asset-atlas-hd-props.js` – comment: how to wire the set once PNGs exist.
- `src/ui/minimap-renderer.js` – PROP_COLORS for fence, torch, stump, bush2, stones, signpost, crystal, cactus, bones.
- `tests/helpers/map-reachability.js` (new), `tests/map-invariants.test.js` (uses helper; new generic portal-arrival test for every map), `tests/oakhaven-village-layout.test.js` (new: plaza ring, farm gate/fence, bridge, dock).
- `tools/browser-check.mjs` – 7 real-input walk-checks in the village + screenshots `16-village-plaza.png`, `17-village-farm.png`, `18-village-bridge.png`, `19-village-dock.png`; fails the run if any check is off.
- Docs: `docs/codebase-summary.md`, `docs/system-architecture.md` (pending village set), `docs/project-roadmap.md` (v0.8).

## Verification
- `node --check` clean on all changed JS; `ast.parse` ok on the Python script.
- `npm test` → 112/112.
- `npm run check:browser` → `errors []`; walk-checks: plazaUp ty14, plazaRight stops at 17 (elder blocks 18), farmGate reaches tx7, fence stops at 11, bridge reaches east bank tx34, dock stops at tx32 (water at 33), portal → verdantMeadow.
- Code review (`code-reviewer-261022-1821-oakhaven-village-relayout.md`): 8/10, 0 critical/high. Applied: cropPlot walkable, portal-arrival test generalized. Declined: minimap colours for the 10 art-less props (add with the art).

## Design decisions
- Plaza paved with castleFloor `f` was tried → looked like a castle slab; reverted to dirt `d`.
- No pixel-art stand-ins (user rule). HD stand-ins only: stump (woodpile), torch (lantern/campfire).
- Dock = bridge tiles over water, no art needed.

## Assets still missing (raw sheet `assets/hd/props/village_props.png`)
RGBA, transparent background, objects left→right in this order, ≥2 fully transparent columns between objects, any resolution (script fits by the limiting dimension; draw each at the listed aspect).

| # | prop | world w x h | canvas px | aspect | spot in map (anchor tx,ty) |
|---|---|---|---|---|---|
| 1 | well | 32x48 | 128x192 | 2:3 | (15,15) plaza centre |
| 2 | lanternPost | 16x32 | 64x128 | 1:2 | replace torches (13,13) (19,17) (11,19) (30,22) |
| 3 | marketStall | 48x48 | 192x192 | 1:1 | (3,16) west of crossbar |
| 4 | cropPlot | 32x16 | 128x64 | 2:1 | (4,20) (6,20) (8,20) (4,22) (6,22) (8,22) |
| 5 | scarecrow | 16x32 | 64x128 | 1:2 | (9,19) |
| 6 | woodpile | 32x32 | 128x128 | 1:1 | (21,20) replaces stumps |
| 7 | campfire | 16x16 | 64x64 | 1:1 | (24,21) replaces torch |
| 8 | barrel | 16x16 | 64x64 | 1:1 | (23,19) |
| 9 | crate | 16x16 | 64x64 | 1:1 | beside the stall / dock |
| 10 | rowboat | 32x16 | 128x64 | 2:1 | (32,23) on water |

Wire-up after painting: `npm run hd:props` → add `hdProp*` keys to `HD_PROP_ASSET_FILES` + `HD_PROPS` → `placeProp()` at the spots above (remove the stand-ins) → `npm test` + `npm run check:browser`.

## Unresolved questions
1. Image source for the sheet (no Gemini key; Figma generation is paid, not approved).
2. Elder Rowan still says "Verdant Village" (dialog untouched by instruction).
