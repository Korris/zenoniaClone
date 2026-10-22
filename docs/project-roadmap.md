# Project Roadmap

## Version 0.9 — Hand-Painted Villagers (10 HD NPCs)

**Status:** ✅ Delivered

### Changes (v0.9)
- [x] **HD NPC pipeline** `tools/build-hd-npcs.py` (`npm run hd:npcs`): 10 raw 4x3 sheets ->
  96px-frame idle sheets at hero scale + 152px dialog portraits; alpha verified per sheet (all 10
  real alpha; checkerboard keying available if a sheet ever comes back opaque).
- [x] **Engine**: `npcsHd[id]` registry + HD draw path in `prop-renderer.js` (idle loop, left/right
  rows, front row for "up"), HD portraits in the dialog box, NPCs turn to face the approaching hero,
  hero debug label hidden unless `SHOW_DEBUG_LABELS` / `window.__zenoniaDebug`.
- [x] **Village**: 7 new dialogue-only NPCs (merchantDale, fishermanRoy, carpenterBrol, farmerGuri,
  guardRook, childTim, healerNina) placed by role (market, dock, woodpile, farm, gate, plaza,
  residential); existing ids/quests untouched; yard fences by each cottage.
- [x] **Tests**: `tests/hd-npcs-config.test.js`; map-invariants cover the 10 placements; browser
  smoke test checks all 10 render HD, walks every district, opens/closes an HD dialog, screenshots
  15-23.
- [ ] Shop / fishing / crafting / healing gameplay for the new roles (no systems exist yet).

### v0.9 Success Metrics
- ✅ 115/115 unit tests; browser smoke test `errors []`; idle frames drift <= 2.2px (of 96) with
  heads pinned, feet on the bottom row.

---

## Version 0.8 — Oakhaven Village Re-layout

**Status:** ✅ Delivered (map) / ⏳ Pending (10 village props need art)

### Changes (v0.8)
- [x] **Village layout** (36x26, same map id/NPC ids/portal): north lane with the 3 cottages and
  doorstep stubs, dirt plaza at the lane crossing with a reserved 2x2 well spot, fenced farm with a
  walkable east gate + gate lane, woodpile corner, river-bank landing, plank bridge and plank dock
  (`B` tiles over water), signpost at the south gate. Stand-ins from the HD set only (stumps, torch).
- [x] **Prop pipeline prepped**: `prop-definitions.js` + `tools/build-hd-props.py` know the 10
  village props (well, lanternPost, marketStall, cropPlot, scarecrow, woodpile, campfire, barrel,
  crate, rowboat); `HD_PROPS` entries and map placements wait for `assets/hd/props/village_props.png`.
- [x] **Minimap** colours for every prop type that lacked one (fence, torch, stump, bush2, stones,
  signpost, crystal, cactus, bones).
- [x] **Tests**: `tests/oakhaven-village-layout.test.js` (plaza ring, farm gate/fence, bridge, dock,
  meadow arrival tile) + shared `tests/helpers/map-reachability.js`; browser smoke test walks the
  village with real key input (plaza, gate, fence, bridge, dock, portal) and screenshots 15-19.
- [ ] **Village props art** (blocked on an image source: no Gemini key; Figma image generation is
  paid and not approved).

### v0.8 Success Metrics
- ✅ 112/112 unit tests; browser smoke test `errors []` with all 7 village walk-checks passing.

---

## Version 0.7 — HD Hand-Painted World Art + Oakhaven Village Hub

**Status:** ✅ Delivered

### New Features (v0.7)
- [x] **HD ground tiles**: grass/flowers, dirt path, water (+3-frame open-water animation), sand/
  castle/cave/lava floors, cliff/wall, portal (real alpha) -- `tools/build-hd-tiles.py`, merged over
  the pixel-art cut per tile name.
- [x] **HD props**: all 24 prop types (pine/round/small/dead/big trees, bush x2, rocks x6, stump,
  stones, palm, cactus, bones, 3 houses, fence, signpost, pillar, torch, crystal) -- `tools/
  build-hd-props.py`, same merge-by-name model.
- [x] **Oakhaven Village**: new starting-hub map (`oakhavenVillage`, replaces Verdant Meadow as the
  first map) composed from the HD prop set after a reference composition -- 3 cottages, farm plot,
  river+bridge, central plaza, safe (no monsters). The 3 original village NPCs moved here.
- [x] **Bugfix**: hero sprite sheets had their left/right rows swapped (user-reported, walking left
  visually faced right and vice versa) -- fixed via a row-remap table, no art/engine logic changes.
- [x] **Bugfix (2026-10-23)**: Oakhaven ground showed a square tile lattice. Root cause was the HD
  tile textures (painted cell borders, non-seamless cells, brightness-mismatched grass variants) +
  missing transitions, not the renderer. `tools/build-hd-tiles.py` now crops/heals/equalizes every
  cell, builds seamless 2x2 grass + stone macros, RGBA path/water transition masks, inner corners
  and 1-wide path pieces; the plaza `p` tile became a stone-on-grass auto-tile block. See
  `plans/reports/report-261023-0126-oakhaven-seamless-ground.md` (before/after screenshots).

### v0.7 Success Metrics
- ✅ New Game boots into Oakhaven Village; walking through its south gate reaches Verdant Meadow and
  back.
- ✅ All HD art (hero/tiles/props) renders crisp (smoothing on) alongside the still-pixel-art rest
  of the world (monsters/NPCs/UI), falling back cleanly to pixel art for any not-yet-generated sheet.
- ✅ Hero visually faces the direction it's walking/attacking, in both combat and movement.
- ✅ 107/107 unit tests pass; browser smoke test (`errors []`) covers village -> meadow -> cave ->
  castle -> desert, combo, dash, skills, inventory, death.

See `plans/261022-0849-zenonia-web-arpg/reports/` for the full implementation reports (HD render
mode, HD tiles, HD props, Oakhaven Village, hero facing fix).

---

## Version 0.6 — Inventory & Equipment Drops

**Status:** ✅ Delivered

### New Features (v0.6)
- [x] **Equipment drops**: monsters roll a 12% chance (bosses 100%, guaranteed rare/epic) to drop a
  weapon or armor piece on death, gated by a new `itemLevel` on `MonsterDefinition`.
- [x] **9 weapon tiers + 12 armor pieces**: `data/item-definitions.js` -- Sword/Sword2/Rapier/Club/
  Katana/Axe/Lance/Hammer/Big Sword (weapon slot, atk); 3 named tiers each for helmet/armor (def+hp),
  gloves (atk+crit), boots (def+speed).
- [x] **4-rarity system**: common/uncommon/rare/epic (x1.0/1.25/1.6/2.1 stat multiplier, 70/20/8/2%
  weights), named prefixes (Fine/Superior/Legendary) and colored borders/text.
- [x] **Inventory & equip/unequip**: 20-slot bag, 5 equipment slots (weapon/helmet/armor/gloves/boots),
  equipping swaps the previous item back into the bag; stats recompute on top of passives.
- [x] **New inventory UI** (B): equipment column + 5x4 bag grid, arrow/mouse navigation, info box with
  rarity-colored stat comparison vs. the currently equipped item.
- [x] **Ground rendering**: weapon sprites at 1x, armor icons at 0.6x, rarity glow pulse on rare+ drops.
- [x] **Save**: inventory + equipment persist, every item re-validated through `sanitizeItem()` on load.

### v0.6 Success Metrics
- ✅ Killing monsters occasionally drops equipment; bosses always drop rare/epic gear.
- ✅ Picking up an item adds it to the bag (or shows "Inventory full" if there's no room).
- ✅ Equipping a weapon/armor piece visibly changes `player.atk`/`def`/`maxHp`/`speed`/crit chance.
- ✅ Bag UI navigable by keyboard and mouse; B/Esc/Tab close; X drops a selected item.
- ✅ Save/load round-trips inventory and equipped gear.
- ✅ 93/93 unit tests pass; browser smoke test (`errors []`) covers a full drop -> pickup -> equip flow.

---

## Version 0.5 — Zenonia Skill Tree

**Status:** ✅ Delivered

### New Features (v0.5)
- [x] **15-node skill tree**: 3 branches (Sword/Body/Spirit) x 3 tiers, 5 levels each, gated by
  hero level + prerequisite nodes (e.g. Blade Storm needs Whirlwind 3 + Heavy Blow 1).
- [x] **3-slot hotbar** (K/L/I): active skills bind dynamically (auto-bound when first learned,
  rebindable from the skill tree panel); replaces the old fixed skill1/2/3 -> skillId mapping.
- [x] **8 new skills**: Power Strike, Heavy Blow, Blade Storm (sword); Iron Skin, Swift Feet,
  Second Wind (body); Focus, Healing Light, Battle Cry, Spirit Bolt, Crit Eye (spirit).
- [x] **Passive stat aggregation**: `computePassiveModifiers()` sums every learned passive into
  hpMult/mpMult/defAdd/speedMult/comboMult/critChance/regenMult/mpRegenMult, cached on `player.passiveMods`.
- [x] **Combat additions**: attack buffs (Battle Cry), combo damage bonus (Power Strike), crits
  (Crit Eye, x1.6 damage, yellow text).
- [x] **Data-driven skill casting**: `effect.kind` (projectile/radial/strike/dash/heal/buff) picks
  one shared handler per kind in `skill-cast-system.js`.
- [x] **Real skill icons**: 24x24 PNGs from the Ninja Adventure icon pack (replaces procedural icons).
- [x] **New skill tree UI**: 3-column x 3-tier node grid with prerequisite connector lines, node
  states (locked/available/learned/maxed), info box, hotbar binding.

---

## Version 0.2 — Art Overhaul + Quest System

**Status:** ✅ Delivered

### New Features (v0.2)
- [x] **Art Pipeline v2**: Procedural tiles with 4-neighbor auto-tiling, 11 multi-tile props (pine, bush, house, cactus, etc.)
- [x] **NPC System**: 6 friendly NPCs across maps with dialogue and quest integration.
- [x] **Quest System**: 9 quests with kill/reach objectives, prerequisites, level gates, rewards. Quest log (Q), HUD tracker, dialog system.
- [x] **Enhanced HUD v2**: Gold-framed bottom bar (portrait, LV, HP/MP, EXP, skills), top-left minimap (cached), top-right quest tracker.
- [x] **Sprite Variants**: Hero + 6 NPC palette remaps from shared body template.
- [x] **Controller States**: Added DIALOG and QUEST_LOG states, new input actions (E for interact, Q for quest log, TAB for skills).
- [x] **Save System Update**: Now persists quest state (active quests + completed list).
- [x] **Tests**: 53 passing tests including quest-system.test.js and sprite-animation.test.js.

### v0.2 Success Metrics
- ✅ All 6 NPCs placed with dialogue lines and quest associations.
- ✅ 9 quests fully functional with prerequisites, level gates, kill/reach objectives.
- ✅ Procedural art generation for tiles, edges, and props.
- ✅ Quest log UI shows active/completed quests with progress tracking.
- ✅ Save/load preserves quest state across sessions.

---

## Version 0.3 — Asset Pack Integration

**Status:** ✅ Delivered

### What Changed (v0.3)
- [x] **Graphics layer refactor**: Switched from code-drawn pixel maps to CC0 "Ninja Adventure" asset pack (PNG tilesets, character sheets, monsters, boss, FX).
- [x] **Asset pipeline**: Async image loading (asset-loader.js), sprite registry slicing (sprite-registry.js), asset manifest (asset-atlas.js).
- [x] **Updated sprite system**: Character sheets 64×112px (columns=facing, rows=animation), monster sheets 64×64px, boss horizontal strips (50×50px).
- [x] **Prop system refactor**: Props now reference asset pack tilesets; footprints defined in prop-definitions.js. 23 props available (trees, bushes, rocks, houses, torches, etc.).
- [x] **Tilemap rendering**: Auto-tiling from asset-defined tiles; 4-neighbor edge detection via tile-char-info.js; procedural portal animation retained.
- [x] **Removed**: hero-sprite-data.js, monster-sprite-data.js, prop-art-generator.js, dev/validate-sprites.mjs (no longer needed).
- [x] **Tests**: All 63 tests pass; no spritesheet validation needed (assets external).

### v0.3 Success Metrics
- ✅ Game loads asset pack on boot with progress bar.
- ✅ All sprites render correctly from PNG source (hero, 6 NPCs, 7 monsters, boss, 23 props, effects, UI).
- ✅ PIXEL_SCALE = 4 (240×160 world px view, 960×640 screen px).
- ✅ Tilemap auto-tiling works with asset-defined tiles.
- ✅ Assets folder required in deployment; see deployment-guide.md.
- ✅ License: CC0 Ninja Adventure credited in assets/ninja-adventure/CREDITS.txt.

---

## Version 0.4 — Custom Hero Pipeline & Combat Feel

**Status:** ✅ Delivered

### What's New (v0.4)
- [x] **Custom hero sprite generation**: AI concept PNG → Python/Pillow pipeline → 24×24px animations (idle/walk/attack1-3/dash/hurt/death).
- [x] **Sprite validation**: Automated QA checks sizes, RGBA alpha, frame counts, feet pivot, color palette (≤32 colors).
- [x] **FX strips**: New slash (small/big), dash trail, hit spark effects integrated into custom hero set.
- [x] **Animation selection**: State machine in player-renderer.js picks animation by game state (dead/dash/combo/hurt/walk/idle).
- [x] **Combo feel**: Lunge forward per hit (player-controller.lungeForward), finisher spinning arc + screen shake, hitSpark on every impact.
- [x] **Dash & death**: Animated dash trails, death animation driven in DEAD state via deathTimer.
- [x] **Browser check**: Puppeteer smoke test validates boot, combo chains, dash, death, console errors (63 tests pass).
- [x] **npm scripts**: `sprites:build`, `sprites:validate`, `check:browser`.

### v0.4 Success Metrics
- ✅ Hero renders 24×24 from custom set; animation state correctly selected.
- ✅ All 8 hero animations play smoothly; feet pivot on bottom row.
- ✅ FX strips load and display on hits/dash; color palette ≤32 colors.
- ✅ Combo feels responsive: chain window 400ms, finisher spins, shake on impact.
- ✅ Dash animates with trail effect; death animation smooth.
- ✅ Validator runs in CI; smoke test boots and validates game state.

---

## Version 0.1 — MVP (Previous Release)

**Status:** ✅ Delivered (implementation phases 01-06)

### Features Delivered
- [x] Game loop (60Hz fixed timestep, render on RAF)
- [x] Camera & viewport (follow player, clamp to map)
- [x] Tilemap (4 solid/walkable tile types, collision sweep)
- [x] 4 maps (Verdant Meadow → Dark Cave → Scorched Desert → Ruined Castle)
- [x] Portal travel (level-gated transitions between maps)
- [x] Player movement (WASD) & facing direction (8-way sprite facing)
- [x] **3-hit combo system** (400ms chain window, finisher scales with level)
- [x] Melee & ranged monsters (7 types: slime, bat, goblin, skeleton, fire slime, goblin warrior, skeleton knight)
- [x] Monster AI (chase on aggro, melee/ranged attacks, wander when idle)
- [x] Damage formula (ATK × mult - DEF, minimum 1)
- [x] Knockback on hit (visual feedback)
- [x] Leveling (EXP curve, stat growth, death penalty 10%)
- [x] **Skill tree** (4 skills: Slash Wave, Whirlwind, Dash Strike, Vitality; 5 levels each)
- [x] Skill casting (MP cost, cooldown, damage scaling, range/radius/dash distance)
- [x] Boss fight (Ancient Golem, 1500 HP, ranged projectiles)
- [x] Loot drops (HP/MP potions, gold; probability per monster)
- [x] **Save/Load** (localStorage, auto-save, manual save on pause)
- [x] HUD (HP/MP bars, combo counter, skill cooldowns, gold display)
- [x] Menus (title, pause, death, victory)
- [x] Visual feedback (hit flash, floating damage numbers, particle effects)
- [x] Unit tests (pure modules: leveling, combat, save serialization)

### Non-Goals (Out of Scope for v0.1)
- ❌ Backend server (all state in browser)
- ❌ Audio (silent experience)
- ❌ Mobile / touch controls (keyboard only)
- ❌ Equipment system (stats from level + skills only)
- ❌ NPC dialogue or story
- ❌ Animations (uses sprite frames, not tweens)

---

## Future Ideas (v0.4+)

### Backlog — Not Planned Yet

#### Gameplay Enhancements
- **Equipment shop**: Buy/sell weapons & armor with gold (drops already implemented in v0.6).
- **Accessories** (rings/amulets): a 6th/7th equipment slot with unique bonuses beyond the current 5.
- **More monsters**: Fire elementals, ice golems, flying enemies.
- **More maps**: Desert temple, crystal caves, sky fortress (5+ new zones).
- **Boss variants**: Multiple final bosses depending on route.
- **Difficulty modes**: Hard mode (monster stats +50%, no autosave).

#### Economy & Progression
- **Gold shop**: Buy potions, permanent stat boosts, or cosmetics.
- **Crafting**: Combine drops to create items.
- **Achievements**: Unlock cosmetics (hero skins, dyes, effects).
- **New Game+**: Replay with higher monster levels, new challenges.

#### Player Quality of Life
- **Minimap**: Fog of war, visited areas.
- **Consumables in the bag**: potions currently auto-apply on pickup; carrying & manually using them
  from the inventory (v0.6 added the bag UI, currently equipment-only) is a natural follow-up.
- **Stat page**: View detailed hero stats, skill breakdowns.
- **Remappable controls**: Customize hotkeys.
- **Gamepad support**: Xbox/Switch controller input.

#### Technical & Presentation
- **Audio**: Background music + sound effects (SFX library or chiptune composer).
- **Leaderboards**: Fastest boss kill, highest level reached (requires backend).
- **Replays**: Record & playback combat sequences.
- **Shaders**: Post-processing (CRT effect, bloom, color grading).
- **Mobile/touch**: Joystick UI + gesture controls (requires redesign).

#### Content
- **Storyline**: NPCs with dialogue trees, world lore, ending cinematics.
- **Side quests**: Optional challenges, rewards.
- **PvP arena**: Deathmatch with other players (requires backend + matchmaking).

---

## Development Philosophy

### YAGNI First
Features only when they solve a real problem or enhance core gameplay. No speculative code.

### Keep It Simple
- Small files, clear module boundaries.
- Pure logic + rendering separation.
- No external dependencies.

### Don't Repeat Yourself
- Shared palette for all colors.
- Config-driven monster stats, maps, skills.
- Reusable systems (animation, sprite factory, etc.).

---

## Metrics & Success (v0.1)

✅ **Completion Checklist**
- [ ] Play from Verdant Meadow → Ruined Castle → defeat boss → victory screen.
- [ ] 3-hit combo chains with 400ms window between hits.
- [ ] Level up triggers on boss defeated and at set EXP thresholds.
- [ ] Skill tree shows 4 skills, 5 levels each, correctly gated by hero level.
- [ ] Save game persists hero level, EXP, skills, position, map, and `bossDefeated` flag.
- [ ] Load game restores hero to saved position & state.
- [ ] All 4 maps accessible via portals (level-gated).
- [ ] Monster spawning, AI, and loot drops work as intended.
- [ ] No console errors on boot → play → save → load → quit.
- [ ] Tests pass: `npm test`.

---

## Timeline (Reference)

- **Phase 01** (4h): Scaffold, core loop, input, camera, sprite factory. ✅
- **Phase 02** (5h): Tilemap, 4 maps, portals, collision. ✅
- **Phase 03** (6h): Player, combo, hit detection, effects. ✅
- **Phase 04** (6h): Monsters, AI, spawner, drops, leveling. ✅
- **Phase 05** (6h): Skills, HUD, save, death, boss win. ✅
- **Phase 06** (3h): Tests & polish. ✅

**Total:** 30 hours.
