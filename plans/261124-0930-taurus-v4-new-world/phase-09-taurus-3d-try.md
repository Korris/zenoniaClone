# Phase 09 - Taurus 3D try (Willowbrook Village + Clover Meadows)

Status: TRY done 24/11/2026 (code + shots), not committed, user has not looked. Shrine NOT converted. Exe: `D:\temp\player-taurus-3d\Zenonia.exe` (3D is default in test builds, `--map2d` = 2.5D).

## Findings: how the old Willowbrook 3D (Map3D) is built

- Gate: `Map3D.Maps` (list of ids) + test build + not `--map2d` (or `--map3d off`) + `View25D.IsMap25D(id)`. Release builds: off. `Map3D.Active` is read in `WorldBuilder.Rebuild`, `MapScenery`, `View25DProfile.For` (-> `Map3DFeel`: tilt 38 / FOV 32), camera control (360 deg yaw), `ClickMove`, FX toolkit.
- Visual only. The 2D tile grid, rows, solid tiles, spawns, portals, server and every position are unchanged; units stay billboards on the z = 0 plane (1 tile = 1 m = 16 units, up = -z). So no server rebuild, no quest / collision change, online unaffected (3D only where `NetMode.RendersFx`).
- Terrain (`Map3DHeightField` + `Map3DTerrain`): 0.5 m vertex grid, map + 30 m apron, chunks 32 m. Walkable tiles (any char except 'T') stay at height 0; 'T' tiles rise as a forest bank (distance-to-open field + Perlin), ground past the edge becomes hills, water bed sinks from the splat water channel, edge portals get a flat dirt corridor. Surface sits 1.5 cm under z = 0.
- Ground look: 4-layer splat (R dirt, G forest floor, B paving, A water) in `Resources/Map3D/<key>-splat.bytes` (8 texels / tile). Willowbrook's was read from the painted 2.5D ground (`Tools/build-map-3d-splat.py`); layer textures `ground-grass / dirt / paving / rock` + tints in `Map3DAssets.Terrain` (shared by all maps).
- Props (`Map3DProps.Classify` by type-name substring): trees / bushes / rocks / logs / stumps / fences / signpost / bridge / shed / reeds become generated meshes (`Map3DTreeBuilder`, `Map3DRockBuilder`, `Map3DWoodBuilder`...) with ChatGPT-painted leaf / bark / rock / wood atlases, merged per cell (`Map3DBatcher`). Anything unnamed stays a sprite (standing = alpha-cutout + real shadows, flat decor lifted 2 cm).
- Light: `Map3DLighting` (sun fixed in world space, soft shadows by graphics level: High realtime, Medium baked, Low none; fog, trilight ambient). Water sheet, grass tufts, border forest, impostors (off), 3D warp gates for every map exit (`Map3DPortals`).
- Colour per map is NOT split yet: one grass / dirt / leaf palette (Willowbrook green).

## What was done (cheapest path, same renderer)

- `Map3D.Maps` + `Map3DAssets.Key`: village and meadows added. `View25DProfile.For`: Map3D check before the Taurus profile.
- `Tools/build-map-3d-splat.py`: new rows mode for `tau-willowbrook-village` / `tau-clover-meadows` (d dirt, p paving, w/B water, T/t forest, domain-warped + rounded so tile corners are not a staircase). Output `Assets/Resources/Map3D/tau-*-splat.bytes`.
- `Map3DHeightField`: Taurus 't' forest mass (5+ of 8 neighbours solid) rises as a bank like 'T'; lone trunks stay flat.
- `Map3DProps`: bridge may run north-south (Taurus bridges cross an east-west stream; Willowbrook's was east-west only), 'planter' no longer classed as an oak, `stones` -> rock cluster, elderwood / great oak -> ancient oak mesh.

## Todo

- [x] Survey old 3D pipeline
- [x] Map3D enabled for village + meadows, splats from rows, bank rule, bridge rotation, prop fixes
- [x] Build from copy, shots (`docs/reference/taurus-v4-art/ingame-3d/`), 2.5D still works (`--map2d`, 0 Map3D log lines)
- [x] Tests: ZenoniaSkills 778 ok, ZenoniaNet 625 + 2 FrostHollow fails (pre-existing); vitest not touched
- [ ] OPEN ART (ChatGPT design chat, not code): Taurus golden oak / elderwood leaf atlas + bark (3D trees are generic green Willowbrook oaks, identity lost vs 2.5D), Taurus grass / dirt / plaza / forest-floor / ancient-roots tileable swatches for the 3D terrain (reuse `Assets/ArtLibrary/maps/taurus/ground/*`, add `tau-` textures + per-map terrain material), reeds / flower / mushroom 3D cards
- [ ] OPEN 3D MODELS: village buildings (guild hall, forge, windmill, card tent, stalls, gate towers + arch, walls) and shrine entrance, great oak planter ring, lamp posts, well, benches, barrels, crates are still sprites that skew when the camera yaws; needs 3D meshes (or ChatGPT turnaround art + mesh builders like `Map3DWoodBuilder`)
- [ ] OPEN TECH: village gate shows Map3D warp ring inside the sprite gate arch (decide: ring only on the shrine door, vortex + column only at the village gate, or hide); tree scale from sprite width (3D crowns bigger than 2.5D); per-zone ground colour in Clover (mushroom / bramble / ancient zones all one green); `tauQuestTouch.png` prop art missing (placeholder square, pre-existing); Shrine dungeon (dark interior, needs ceiling / wall meshes, Map3D has none); FPS 46-56 High at 1280x720 (check Medium on laptop)
- [ ] User decision: make 3D default for Taurus in release, or keep 2.5D as the shipping look until buildings + trees are 3D

## Colour & Lighting V2.0 / V2.1 (24/11/2026)

Spec `docs/reference/taurus-v4-art/palette/taurus-palette-spec-chatgpt.txt`; review of our shots `taurus-palette-v2-feedback-chatgpt-1.txt` (V2.1: no asset redrawn). 3D: `Map3DPalette.cs` (Taurus-only values), `Map3DAssets` / `Map3DLighting` / `Map3DGrass`, shaders `Map3DTerrain` (+`_PavingTint/_PavingContrast`) and `Map3DWater` (+`_Calm`). 2.5D: `View25DProfile.TaurusField` light + `Tools/build-tau-25d-ground.py` PALETTE (regenerates ground chunks + borders; swatches in ArtLibrary untouched). Shots before: `palette/ingame-v2/`, after V2.1: `palette/ingame-v2b/`.
- [ ] Not re-verified by ChatGPT after V2.1 (one feedback round only). Open: 2.5D golden oak highlight -5-8 % saturation (needs sprite grade), grove light fog, AO / projected 2.5D tree shadow, global saturation +4-6 / contrast +8 (no grading pass in 3D), 3D crown form still crumbly vs 2.5D.

## RO3 look loop (24/11/2026, 4 rounds with the design chat)

Sharpness V1.0 + Camera V3.1 applied, then rounds r1-r4 (`docs/reference/ro3-compare/rounds/`): scores village 6.5 / 7.4 / 7.9 / 8.2, field 5.8 / 6.9 / 7.5 / 7.9, sharpness 8.5 / 8.9 / 9.0 / 9.0, zoom-out 5.5 / 6.8 / 8.0 / 8.5; final "ĐẠT, chốt khóa môi trường Taurus V1.0". Rules: map-design-rules item 25. Open: Guild lanterns, forge hybrid 3D, crowd FPS profiling.
