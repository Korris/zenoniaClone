# Codebase File Summary

| File | Purpose |
|------|---------|
| **core/constants.js** | Canvas dims, pixel scale, tile size, game state enums, direction vectors. |
| **core/game-loop.js** | Fixed-timestep wrapper: 60Hz update, render on RAF, handles tab-switch frame clamping. |
| **core/input-manager.js** | Keyboard state tracking; queues key presses for single-frame detection. |
| **core/camera.js** | Viewport following entity, clamps to tilemap bounds. |
| **core/seeded-random.js** | Seeded PRNG for deterministic monster spawns & drops. |
| **graphics/tile-art-generator.js** | Procedural portal animation frames (pixel-art fallback; HD portal uses real painted frames via HD_TILES). |
| **graphics/pixel-painter.js** | 1px drawing helpers (outline, filled rect, line). |
| **graphics/pixel-sprite-factory.js** | Canvas sprite cropping; tinted sprites for hit flash. |
| **graphics/sprite-cell.js** | `cropSprite()`/`cell()`: shared image-crop-to-canvas helper (stamps `pixelsPerUnit`), used by sprite-registry.js and tile-sprite-builder.js. |
| **graphics/tile-sprite-builder.js** | `buildTiles()`: pixel-art `GROUND_TILES` cut merged with HD `HD_TILES` cut per tile name (HD wins where its sheet loaded); returns `{tiles, tilesHd}`. |
| **graphics/sprite-animation.js** | Frame-based animation controller (timer, loop, frame index). |
| **graphics/asset-atlas.js** | Manifest: ASSET_FILES (pack + custom hero paths, + HD hero/tile/prop paths for sheets that exist), HERO_ANIMATIONS (8 state keys), HERO_FRAME (24px), FX_STRIPS, GROUND_TILES/HD_TILES (ground tile cell maps, pixel + HD), PROP_RECTS (pixel prop cells), FLOWER_OVERLAYS. HD_HERO/HD_HERO_ANIMATIONS (hand-painted 96px set, pixelsPerUnit=4) + `unitsFor()` helper -- see system-architecture.md "HD assets". Re-exports HD_PROPS/HD_PROP_PIXELS_PER_UNIT from asset-atlas-hd-props.js. |
| **graphics/asset-atlas-hd-props.js** | HD_PROPS (prop type -> normalized PNG key) + its ASSET_FILES fragment, split out of asset-atlas.js to stay under 200 lines. |
| **graphics/prop-sprite-builder.js** | `buildProps()`: pixel-art `PROP_RECTS` cut merged with HD `HD_PROPS` cut per prop type (HD wins where its normalized file loaded) -- same merge-by-name pattern as tile-sprite-builder.js. |
| **graphics/asset-loader.js** | Loads PNG images via Promise.allSettled (a missing/not-yet-generated file, e.g. an HD sheet, logs a warning and is dropped from the map instead of failing boot). |
| **graphics/sprite-registry.js** | Slices loaded images into canvases; exports HERO_FRAME_SIZE (24px, world units) + heroFrameUnits. `registry.hero[dir][anim]` is the HD set where that sheet loaded, else falls back to the 24px pixel set per-animation; `registry.tiles`/`registry.tilesHd` come from tile-sprite-builder.js; `registry.props` comes from prop-sprite-builder.js. NPCs/monsters remain 16px. |
| **graphics/sprites/effect-sprite-data.js** | Procedural markers (quest !/?), palette for procedural portal. Skill icons are real 24x24 PNGs (asset-atlas.js), not procedural. |
| **graphics/world-renderer.js** | Y-sort + render tilemap, props, NPCs, monsters, hero, effects. Camera offset. |
| **graphics/player-renderer.js** | Draw hero sprite (facing, animation frame, hit flash tint). Dest size is always HERO_FRAME_SIZE world units regardless of source resolution; `imageSmoothingEnabled` toggles on only when the picked frame's `pixelsPerUnit > 1` (HD). |
| **graphics/monster-renderer.js** | Draw monsters with AI state (idle/chase/attack anim). |
| **graphics/tilemap-renderer.js** | Visible-tile loop: variants/flowers/cliff/portal draws; block tiles delegate to tile-block-renderer.js. HD tiles (registry.tilesHd): toggles smoothing once per call, draws grass+flower macro quadrants (no overlay), and lays the neighbouring ground (incl. a road's open center) under the real-alpha HD portal frame. |
| **graphics/tile-block-renderer.js** | One auto-tile block tile (dirt path, water, plaza stone): seamless base grass, 3x3 piece from the 4 edge neighbours (or an HD extra `shapes` piece for 1-wide pipes/caps/islands), `center` macro for open road centers, `fill` material drawn through the block's alpha (plaza stone), `inner` grass wedges on concave corners from the diagonal neighbours, water center animation (~450ms). Uses one reused scratch canvas for masking. |
| **graphics/tile-variant-picker.js** | `pickVariant(frames, tx, ty)`: position-based quadrant pick for `macro`-tagged seamless sets, pseudo-random scatter otherwise. |
| **graphics/prop-renderer.js** | Draw props, NPCs, quest markers (!/?), name tags. Props draw at an explicit `def.width`/`def.height` world-unit dest size, smoothing toggled on only when the picked canvas is HD (`pixelsPerUnit > 1`). |
| **graphics/effects-renderer.js** | Render particles, floating text, hit effects. |
| **world/tilemap.js** | Tile grid; solid & prop footprints; pixel-perfect collision sweep (AABB). |
| **world/map-builder.js** | Compile map def (tiles, portals, spawners, props) → Tilemap. Helpers: placeProp, scatterProps, propLine, forestBorder. |
| **world/map-registry.js** | Registry of 5 maps by ID; `FIRST_MAP_ID` = `oakhavenVillage`; helper to fetch (falls back to `FIRST_MAP_ID` for an unknown/old save `mapId`). |
| **world/map-manager.js** | Portal transitions, level gates, find portal under player. |
| **world/maps/oakhaven-village-map.js** | Map 0 (starting hub, v6): safe 36x24 village drawn as ONE painted backdrop (`backdrop: 'hdOakhavenGround'` -> `assets/hd/map/oakhaven-v6/ground.webp`). `GROUND_ROWS` were classified from that painting (T forest, g grass, d road/market/farm, p plaza, w river, B bridge/dock) and only drive collision + minimap. 5 HD buildings (elder manor, Mia's cottage, hunter lodge, healer cottage, carpenter workshop), plaza well, market stall, farm, dock + rowboat, 10 NPCs, portal (16,21) -> Verdant Meadow. Layout guarded by `tests/oakhaven-village-layout.test.js`. |
| **tools/build-oakhaven-village-art.py** | Oakhaven v6 pipeline (`npm run hd:village`): raw ChatGPT sheets in `assets/hd/map/oakhaven-v6/` -> `ground.webp` backdrop (4 px/unit) + 20 normalized props (halo alpha cut, blob labeling, reading-order mapping, premultiplied resize to `PROP_TILES`, which must match `prop-definitions.js`). |
| **world/maps/verdant-meadow-map.js** | Map 1: slimes, bats, props; portal west -> Oakhaven Village, east -> Dark Cave. |
| **world/maps/dark-cave-map.js** | Map 2: goblins, skeletons, Miner Bork, cave props. |
| **world/maps/scorched-desert-map.js** | Map 3: fire slimes, warriors, Nomad Saif, desert props. |
| **world/maps/ruined-castle-map.js** | Map 4: skeleton knights, Sir Aldric, boss, castle props. |
| **world/monster-spawner.js** | Spawn pool manager; respawn on timer & count limits. |
| **world/tile-char-info.js** | Tile metadata: solid, walkable. Auto-tiling edges & sameEdgeFamily. |
| **world/prop-definitions.js** | Metadata: 24 props (pine, tree, bush, rock, house, cactus, torch, crystal, etc). Width, height, footprint, solid -- fence/signpost use a taller 24-unit height (1.5 tiles) than their 1-tile footprint, same "taller than footprint" convention as pillar/torch/crystal, so their HD art (a full wooden post) isn't squashed into a flat 16x16 box. |
| **entities/player.js** | Hero: level, EXP, skills, hotbar (3 slots), buffs (atkMult timer), HP/MP, passiveMods cache (passives + equipment crit folded in), inventory (bag array) + equipment (5 slots), combo state, damage/heal logic. |
| **entities/monster.js** | Enemy: HP, AI state, aggro timer, knockback, attack cooldown. |
| **entities/npc.js** | Friendly NPCs: sprite variant, position, quest marker (!/?). |
| **entities/projectile.js** | Fired by monsters & skills; travels, collides, explodes on wall. |
| **entities/item-drop.js** | Potion, gold, or equipment item on ground (`kind:'equipment'` carries an item instance); despawns after 30s or pickup. |
| **entities/floating-damage-number.js** | Damage text that rises & fades. |
| **entities/visual-effect.js** | Spark, hit flash, or explosion animation. |
| **entities/skill-pulse.js** | Generic repeating timer for multi-hit skills (Blade Storm's 3 pulses). |
| **systems/combo-attack-system.js** | 3-hit state machine: windups → active → recover; chain window & finisher. |
| **systems/damage-formula.js** | Damage calc: (ATK × mult - DEF); hitbox overlap detection. |
| **systems/player-damage.js** | heroAttack() (applies Battle Cry buff), rollPlayerDamage() (Crit Eye roll, x1.6). Shared by combat-system & skill-cast-system. |
| **systems/combat-system.js** | Resolve combo/radial/strike hits; comboMult (Power Strike), crit coloring; knockback & damage numbers. |
| **systems/quest-system.js** | Pure state machine: acceptQuest, recordKill, recordMapEnter, turnInQuest, npcMarker, progressText. |
| **systems/skill-system.js** | canUpgradeSkill (level + prerequisite gating), computePassiveModifiers (sums all learned passives into one stat object), bindSkillToSlot/upgradeSkill (hotbar auto-bind). |
| **systems/skill-cast-system.js** | Data-driven by `effect.kind` (projectile/radial/strike/dash/heal/buff); one handler per kind shared by every skill using it. |
| **systems/leveling-system.js** | EXP curves, level-up stat scaling, death penalty (10% EXP loss). |
| **systems/monster-ai-system.js** | Behavior: idle → wander → chase (aggro) → attack (ranged/melee). |
| **systems/loot-system.js** | Apply drop: heal HP/MP (potion) or add gold; validate drops vs defs. |
| **systems/equipment-system.js** | generateItem/rollEquipmentDrop (rarity + itemLevel gated template pool), computeEquipmentModifiers, equipItem/unequipItem (bag swap), addToInventory/removeFromInventory, isUpgrade, sanitizeItem (save validation). |
| **data/hero-stats-config.js** | Base stats, growth per level, combo timings, HP/MP regen rates. |
| **data/monster-definitions.js** | Stat blocks for 8 enemy types (incl. `itemLevel` for equipment drop scaling). |
| **data/item-definitions.js** | EQUIPMENT_SLOTS, RARITIES (common/uncommon/rare/epic), 9 weapon + 12 armor templates (helmet/armor/gloves/boots x 3 tiers), grouped by slot for drop rolls. |
| **data/class-definitions.js** | Playable classes (warrior, wizard): base stats + growth (fed to `statsForLevel(level, classId)`), the class's 3 skill branches, attack style ('melee' / 'ranged'). `classForBranch()` maps a skill to its class. Old saves default to warrior. |
| **data/skill-definitions.js** | Skill trees, 15 nodes per class (3 branches x 2/2/1 tiers). Warrior tree here; wizard tree in `skill-definitions-wizard.js` (Fire / Frost / Arcane, extra effect params `explodeRadius`/`explodeFx`, radial `fx`/`fxDiameter`); both merged into `SKILL_DEFINITIONS` / `SKILL_ORDER`. |
| **data/npc-definitions.js** | 6 NPCs: name, sprite variant, idle chatter lines, quest list. |
| **data/quest-definitions.js** | 9 quests: kill/reach objectives, prerequisites, level gates, rewards, dialogs. |
| **game/game-controller.js** | State machine (title, playing, paused, skills, inventory, dialog, quest-log, dead, win). |
| **game/game-session.js** | Game session: world, player, quests, save/load (auto + manual), transitions. |
| **game/world-state.js** | Container: player, NPCs, monsters, props, drops, effects, tilemap, camera. |
| **game/player-controller.js** | Input: WASD, Space/J (attack), K/L/I (skills), E (interact), Q (quest log), TAB (skills panel). |
| **game/npc-interaction.js** | Find nearest NPC, open dialog, apply accept/turn-in outcome. |
| **game/quest-flow.js** | Feedback banners: quest accepted, progress, turn-in, reward. |
| **game/world-update.js** | Per-frame simulation: player, NPCs, monsters, projectiles, props, drops (potion/gold/equipment pickups), effects. |
| **ui/pixel-text.js** | Text rendering using pixel-art sprite glyph sheet. |
| **ui/hud-renderer.js** | Gold-framed bottom bar: portrait (HD faceset when `registry.faces.heroIsHd`, smoothing toggled only for that draw), LV, HP, MP, EXP, 3 hotbar slots (player.hotbar, K/L/I), hint box (TAB Skills / Q Quests / B Bag) with "NEW" blink on unseen items. |
| **ui/minimap-renderer.js** | Top-left minimap, cached per map, shows player + NPCs + monsters. |
| **ui/quest-tracker-hud.js** | Top-right: active quest title, objective, progress. |
| **ui/dialog-box.js** | NPC dialog rendering: multi-line, choice buttons (Accept/Turn-In). |
| **ui/quest-log-panel.js** | Quest log UI (Q): list active/completed, show objectives, rewards. |
| **ui/skill-tree-panel.js** | Skill tree UI: 3-column x 3-tier node grid, prerequisite connectors, Enter upgrade, K/L/I hotbar bind. |
| **ui/skill-tree-layout.js** | Pure layout (node positions, nav index) + node-state/formatting helpers for the skill tree panel. |
| **ui/inventory-panel.js** | Bag/equipment UI (B): 5 equipment slots + 5x4 bag grid, arrow nav across both areas, Enter equip/unequip, X drop, mouse click/double-click, info box with rarity-colored stat deltas vs. equipped. |
| **ui/inventory-layout.js** | Pure layout (equipment/grid slot positions) + stat formatting/comparison helpers for the inventory panel. |
| **ui/message-banner.js** | Floating notification (e.g., "LEVEL UP!", "QUEST ACCEPTED!"). |
| **ui/menu-screens.js** | Title, pause, death, victory menus; input handling. |
| **ui/class-select-screen.js** | New Game hero selection: one card per class (animated HD idle, tagline, attack style, stat bars); Left/Right/click select, Enter confirm, Esc back. |
| **systems/projectile-hit-system.js** | Projectile impacts for both sides (moved from combat-system.js), AoE bursts for projectiles with `explodeRadius`, and `fireComboBolt()` for the wizard's ranged J combo. |
| **save/save-serializer.js** | Serialize state (player incl. inventory/equipment, quests, map, position) → JSON; validate on load (sanitizeItem() for every item). |
| **save/local-storage-save.js** | Read/write browser localStorage with version checking. |
| **tools/build-hero-sprites.py** | AI → custom hero sprite pipeline: loads concept PNG, generates 8 animations (idle/walk/attack1-3/dash/hurt/death) + 4 FX strips (slash_small/big, dash_trail, hit_spark) at 24×24px, 32-color palette, feet pivot on bottom row. |
| **tools/validate_sprites.py** | QA: verifies sprite sizes, RGBA binary alpha, non-empty frames, feet pivot, ≤32 colors. Run: `npm run sprites:validate`. |
| **tools/build-hd-hero.py** | HD (hand-painted, Zenonia 4 style) hero pipeline: normalizes raw ChatGPT sheets in `assets/hd/hero/*.png` into 96×96 frames in `assets/hd/hero/normalized/`. Per-sheet scale (from each sheet's own row0/col0 frame), soft alpha kept (haze threshold) or checkerboard color-keyed (for sheets painted opaque instead of using real alpha), no outline/quantization. Also builds the 152×152 `hero_face.png` portrait. Run: `npm run hd:build`. See `assets/hd/hero/normalized/README.md`. |
| **tools/build-hd-tiles.py** | HD ground-tile pipeline: splits raw sheets in `assets/hd/tiles/*.png` (grass, path, water, floors, walls, portal) by their known grid, crops inside each cell's painted panel border, LANCZOS-resizes to 64×64 and seam-heals so nothing tiles with a visible lattice. Grass -> one 2x2 seamless macro (+ flower decals on the same quadrants); path/water -> RGBA transition masks (grass transparent) + path center macro, inner-corner wedges and 7 extra shapes; `plaza.png` -> 2x2 stone macro. Writes `assets/hd/tiles/normalized/` (pixelsPerUnit 4). Run: `npm run hd:tiles`. See `assets/hd/tiles/normalized/README.md`. |
| **tools/hd_tile_texture_utils.py** | Importable helpers for build-hd-tiles.py: `split_cells` (margin crop), `equalize_means`, `heal_seams` (feathered interior strip over every boundary incl. wrap), `macro_from_cells`, `material_alpha` (hue-based grass-vs-dirt/water mask), `flower_decals`, `inner_corner_masks`, premultiplied resize. |
| **tools/build-hd-props.py** | HD prop pipeline: raw sheets in `assets/hd/props/*.png` hold several objects placed irregularly (not an exact grid) -- haze-thresholds the glow/halo, splits each sheet into per-object segments (empty-column gaps, or a manual split where two objects touch), tight-crops, and LANCZOS-fits (premultiplied) each object bottom-center into its prop type's exact world-size x4 canvas in `assets/hd/props/normalized/`. Run: `npm run hd:props`. See `assets/hd/props/normalized/README.md`. |
| **tools/build-hd-npcs.py** | HD villager pipeline: raw 4x3 sheets (`assets/hd/map/Oakhaven/*.png`, rows front/left/right, 4 idle frames) -> `assets/hd/npcs/normalized/<id>.png` (384x288, 96px frames = 24 units x4, feet on the bottom row, body ~82px / child 70px) + `<id>_face.png` (152x152 dialog portrait). Reuses build-hd-hero.py's alpha/grid/resize helpers via importlib. Run: `npm run hd:npcs`. |
| **graphics/asset-atlas-hd-npcs.js** | `HD_NPC_IDS`, `HD_NPC` frame contract, `HD_NPC_ASSET_FILES` (spread into ASSET_FILES), `sheetKey`/`faceKey`. Keyed by NPC id; sprite-registry.js builds `npcsHd[id]` + `faces[id]`, prop-renderer.js prefers them over the pixel set per id. |
| **tools/browser-check.mjs** | Smoke test: puppeteer-core headless Chrome, boots game, walks the village with real key input (plaza, farm gate + fence, bridge, dock, south-gate portal -> meadow; screenshots 15-19), chains combo, dashes, opens skill tree, drops/picks up/equips an item, dies, checks console. |
| **main.js** | Boot: canvas setup, loading screen, image load, sprite build, input wiring, loop start. |
| **index.html** | HTML entry point: canvas element, script import. |
