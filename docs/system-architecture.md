# System Architecture

## Module Organization

```
src/
├── core/              # Low-level, framework-like utilities (pure)
│  ├── constants.js    # Canvas size, tile size, game states, directions
│  ├── game-loop.js    # Fixed-timestep loop wrapper (60Hz update + rAF render)
│  ├── input-manager.js # Keyboard input state tracking
│  ├── camera.js       # View following + bounds clamping
│  └── seeded-random.js # Deterministic RNG
├── graphics/          # Canvas rendering only (no state mutation)
│  ├── palette.js, pixel-painter.js, tile-art-generator.js, prop-art-generator.js
│  ├── pixel-sprite-factory.js # String pixel-map → canvas + palette remap
│  ├── sprite-cell.js  # cropSprite()/cell() -- shared image-crop-to-canvas helper
│  ├── tile-sprite-builder.js # buildTiles(): pixel GROUND_TILES merged with HD_TILES per name
│  ├── sprite-*.js, sprite-registry.js # All sprites + boot cache
│  ├── world-renderer.js # Y-sort: tilemap, props, NPCs, monsters, hero
│  ├── player-renderer.js, monster-renderer.js, prop-renderer.js # Entity render
│  ├── tilemap-renderer.js # 4-neighbor auto-tiling (cached)
│  └── effects-renderer.js # Particles, text, hit effects
├── world/             # Game world (mostly pure)
│  ├── tilemap.js      # Tile grid, collision, prop footprints
│  ├── map-builder.js, map-registry.js, map-manager.js # Map management
│  ├── maps/*.js, monster-spawner.js # 5 maps + spawners (Oakhaven Village is the safe starting hub, no spawners)
│  ├── tile-char-info.js, prop-definitions.js # Metadata
├── entities/          # Game objects (pure data + methods)
│  ├── player.js       # Hero: stats, progression, inventory/equipment, hit/heal logic
│  ├── monster.js      # Enemies: HP, AI state, knockback
│  ├── npc.js          # Friendly NPCs: sprite, position, quest marker
│  ├── projectile.js   # Fired by monsters & skills
│  ├── item-drop.js    # Potions, gold, equipment (pickup-able)
│  ├── floating-damage-number.js # Damage text feedback
│  └── visual-effect.js # Sparks, hit flash, explosions
├── systems/           # Game logic (pure)
│  ├── combo, damage, combat, quest, skill, leveling, monster-ai, loot, equipment systems
│  └── All pure functions, no DOM, no entity mutation
├── data/              # Config (frozen)
│  ├── hero-stats-config.js, monster-definitions.js # Stats & growth (incl. itemLevel)
│  ├── skill-definitions.js, npc-definitions.js # Skills & NPCs
│  ├── item-definitions.js # Equipment slots, rarities, weapon/armor templates
│  └── quest-definitions.js # 9 quests
├── game/              # High-level orchestration & control
│  ├── game-controller.js # State machine (title → playing ↔ pause/skills/dialog/quest-log → dead/win)
│  ├── game-session.js # Game session: world, player, quests, save/load, transitions
│  ├── world-state.js  # Container: player, NPCs, monsters, props, drops, effects
│  ├── player-controller.js # Input mapping: WASD, Space/J, K/L/I, E, Q, TAB, P
│  ├── npc-interaction.js # Find nearest NPC, dialog management, accept/turn-in
│  ├── quest-flow.js  # Feedback banners (accepted, progress, reward)
│  └── world-update.js # Per-frame simulation: player, NPCs, monsters, projectiles, etc.
├── ui/                # Menus & HUD (render-only)
│  ├── pixel-text.js   # Text renderer
│  ├── hud-renderer.js, minimap-renderer.js, quest-tracker-hud.js # HUD elements
│  ├── dialog-box.js, quest-log-panel.js, skill-tree-panel.js, inventory-panel.js # UI panels
│  ├── message-banner.js, menu-screens.js # Notifications & menus
├── save/              # Persistence
│  ├── save-serializer.js # State → JSON + validation
│  └── local-storage-save.js # Read/write browser localStorage
├── main.js            # Boot: canvas setup, sprite build, loop start
└── index.html         # Entry point: canvas element, script tag
```

## Frame Update Flow

```
GameLoop.tick()
  ↓
GameController.update(dt)
  ↓ (if PLAYING)
  updateWorld(world, input, dt, banner)
    ├─ updatePlayer(world, input, dt, banner)
    │   ├─ Movement (WASD), Attack (Space/J), Skills (K/L/I), Interact (E)
    │   └─ Quest log (Q), Skill panel (TAB)
    ├─ updateNpcs(world) → check for E-key interact, manage dialog state
    ├─ updateMonsters(world, dt)
    │   ├─ updateMonsterAi() → move intent
    │   ├─ Tilemap collision sweep (props block)
    │   ├─ Attack execution, questSystem.recordKill() on death
    ├─ resolveProjectileHits(world)
    ├─ [updateLists: projectiles, drops, effects, particles, text]
    ├─ updatePickups(world) → apply potions/gold
    ├─ questSystem.recordMapEnter() on portal transition
    ├─ Spawner.update() → spawn new monsters
    ├─ world.removeDead()
    └─ events[] (portal, levelUp, bossDefeated, playerDied, questProgress, questReward)

GameController.handleEvent(event)
  └─ State transitions (paused → skills, playing → dead, etc.)

GameController.render(ctx)
  ↓
renderWorld(ctx, world, registry)
  ├─ Tilemap (all tiles + edges via auto-tiling)
  ├─ Y-sorted props, NPCs (with name tags), monsters, hero
  └─ Particles & floating text
drawHud(ctx, world, registry)
  ├─ Bottom bar: portrait, LV, HP/MP (gold frame), EXP, skill slots
  ├─ Top-left minimap (cached per map)
  ├─ Top-right quest tracker (active quest title + progress)
  └─ Dialog box (if DIALOG state) or Quest log (if QUEST_LOG state)
banner.render(ctx) → "LEVEL UP!" overlay
SkillPanel.render(ctx) → skill tree (if SKILLS state)
Menu.render(ctx) → pause/death/victory menu
```

## Key Data Shapes

### MapDefinition
```js
{
  id: "verdant-meadow",
  name: "Verdant Meadow",
  recommendedLevel: 1,
  tilemap: { rows: ["ggqqgg", "gg..gg", ...] },
  portals: [{ x, y, destMapId, destX, destY }],
  spawners: [{ x, y, monsterType, maxCount, respawnDelayMs }]
}
```

### MonsterDefinition
```js
{
  name: "Slime", sprite: "slime", hp: 30, atk: 6, def: 1, speed: 30, exp: 12,
  aggroRange: 70, attackRange: 12, attackWindupMs: 350, attackCooldownMs: 1200,
  behavior: "melee", projectileSpeed?: 90 (ranged only),
  drops: { hpPotion: 0.15, mpPotion: 0.1 }, gold: [1, 3],
  itemLevel: 1, // gates which equipment templates can drop + scales rolled stats (equipment-system.js)
}
```

### SkillDefinition
15 nodes: 3 branches (`sword`/`body`/`spirit`) x 5 nodes (tiers 1-3), maxLevel 5 each.
```js
{
  id: "slashWave", name: "Slash Wave", type: "active", branch: "sword", tier: 1,
  maxLevel: 5, requiredLevel: [2, 4, 7, 10, 14], requires: [{ skill: "powerStrike", level: 2 }],
  mpCost: [6, 7, 8, 9, 10], cooldownMs: [2500, ...], damageMult: [1.4, ...], range: [90, ...],
  icon: "iconCut", // key into ASSET_FILES -> a real 24x24 PNG, used directly as the icon
  effect: { kind: "projectile", speed: 160, spriteKey: "arc", w: 14, h: 14, pierceFromLevel: 3, label: "Slash Wave!", textColor: "#e0a0ff" },
}
// Passives (type: "passive") carry `passive: { statName: [v1..v5] }` instead of `effect`;
// computePassiveModifiers() in skill-system.js sums these into one stat object (hpMult, mpMult,
// defAdd, speedMult, comboMult, critChance, regenMult, mpRegenMult), cached on player.passiveMods.
// effect.kind drives skill-cast-system.js's generic per-kind handler: projectile/radial/strike/dash/heal/buff.
```
Hotbar: `player.hotbar` is a 3-slot array of active skill ids (or null), bound via K/L/I in the
skill tree panel or auto-bound to the first empty slot when an active skill is first learned.

### SaveObject
```js
{
  version: 1, savedAt: 1234567890,
  player: {
    level, exp, skillPoints, skills: {slashWave: 2, ...}, hotbar: ['slashWave', null, null], gold, hp, mp,
    inventory: [ItemInstance, ...], equipment: { weapon: ItemInstance|null, helmet, armor, gloves, boots },
  },
  quests: { active: {questId: {progress: 5}}, completed: ['slimeTrouble', ...] },
  mapId: "ruined-castle", x: 480, y: 320, bossDefeated: false
}
```

### ItemInstance (equipment-system.js `generateItem`)
```js
{ uid: "sword2-a1b2c3", templateId: "sword2", slot: "weapon", name: "Fine Short Sword",
  rarity: "uncommon", itemLevel: 6, stats: { atk: 8 } }
// stats = round(template.baseStats[stat] * RARITIES[rarity].mult * (1 + 0.08 * (itemLevel - 1)))
// weapon templates carry {atk}; helmet/armor carry {def, maxHp}; gloves carry {atk, critChance};
// boots carry {def, speedMult}. save-serializer.js runs every item through sanitizeItem() on load.
```

## Asset Pipeline

**Ninja Adventure Pack:** CC0 asset pack by pixel-boy (https://pixel-boy.itch.io/ninja-adventure-asset-pack). Loading via asset-loader.js; manifest in asset-atlas.js. NPCs/monsters: 64×112px (walk, attack). Boss: 50×50px strips. Tiles: auto-tiling via tilemap-renderer.js.

**Custom Hero Sprites (v0.4):** AI concept PNG → `build-hero-sprites.py` → 8 animations (idle/walk/attack1-3/dash/hurt/death) + FX strips at 24×24px, 32-color palette. Validator: `validate_sprites.py`. Deployed in `assets/custom/hero/`.

**Animation Selection:** (player-renderer.js heroFrame) Picks animation by state: dead→death via deathTimer, dash, combo step→attack1/2/3 by phaseProgress, hurt, walk, idle via idleTimer.

## HD Assets

Hand-painted, Zenonia-4-style replacement art that renders alongside the pixel-art world (hero
first; other actors stay pixel-art for now). All gameplay/world units are unchanged -- world px,
16-unit tiles, 24-unit hero frame -- an HD sprite just has more source pixels per world unit.

**Resolution-independent sprites:** every sprite canvas carries a `pixelsPerUnit` property (set by
`cropSprite()` in sprite-cell.js, default 1 for the existing pixel-art pack). An HD sprite sets
it to `HD_HERO.pixelsPerUnit` (4): a 96px-wide HD hero frame and a 24px-wide pixel hero frame both
represent the SAME 24 world units (`unitsFor(sprite) = sprite.width / sprite.pixelsPerUnit`, in
asset-atlas.js, covered by `tests/hd-asset-config.test.js`). Renderers must therefore always pass
an explicit **world-unit** destination size to `drawImage` (never rely on the source's natural
pixel size) and toggle `ctx.imageSmoothingEnabled = sprite.pixelsPerUnit > 1` around that one draw,
restoring `false` immediately after (the rest of the world renders pixel-art crisp, smoothing
disabled). See player-renderer.js's `drawPlayer`, menu-screens.js's title hero, hud-renderer.js's
`drawPortrait`, inventory-panel.js's `drawPortrait`.

**Pipeline:** `tools/build-hd-hero.py` (`npm run hd:build`) turns raw ChatGPT sheets in
`assets/hd/hero/*.png` (idle/walk/attack1-3/dash/hurt/death, 4 rows = DOWN/UP/LEFT/RIGHT x N cols,
plus a standalone `hero_face.png` portrait) into normalized 96×96 sheets + a 152×152 face in
`assets/hd/hero/normalized/`. Key differences from the pixel-art pipeline
(`build-hero-sprites.py`): soft (non-binary) alpha is preserved, no outline, no palette
quantization, and the scale is computed PER SHEET (from that sheet's own row0/col0 frame) since raw
sheets arrive at different native resolutions. Some sheets come back with real alpha; others are
opaque with a painted two-tone checkerboard standing in for transparency -- `build-hd-hero.py`
detects this and color-keys it instead (sample the two tile colors via k-means, keep only the
color-matching region connected to the sheet's outer edge, drop tiny leftover specks, soften the
cut edge by ~1px). Only files that exist in `assets/hd/hero/` are processed; missing ones are
skipped with a warning, so the set can grow one animation at a time.

**Boot resilience:** `ASSET_FILES` in asset-atlas.js only lists HD keys for sheets that actually
exist (avoids pointless 404s); `loadAllImages()` (asset-loader.js) uses `Promise.allSettled` so ANY
missing/failed asset is logged and dropped rather than aborting boot. `sprite-registry.js`'s
`buildHero()` merges the HD set over the pixel set per-animation: an animation whose HD sheet
hasn't been generated/registered yet just falls back to the 24px pixel frames.

**Adding a new HD sheet:** drop the raw ChatGPT PNG in `assets/hd/hero/<anim>.png` (or
`hero_face.png` for the portrait), run `npm run hd:build`, then add the corresponding
`hdHero<Anim>` key to `ASSET_FILES` in asset-atlas.js (path `hd/hero/normalized/<anim>.png`) --
`sprite-registry.js`/`player-renderer.js` pick it up automatically, no other code changes needed.

### HD ground tiles

Same resolution-independence model as the hero, applied to the world tiles: hand-painted sheets in
`assets/hd/tiles/*.png` (grass, dirt-on-grass path, water-on-grass, floors, cliff/wall, portal),
normalized by `tools/build-hd-tiles.py` (`npm run hd:tiles`) into 64px-per-cell sheets in
`assets/hd/tiles/normalized/` (pixelsPerUnit 4, matching a 16-world-unit tile exactly like
`HD_HERO`/24-unit hero frame). Unlike the hero pipeline, raw tile sheets are flat, evenly-spaced
grids (no body/bbox detection, no checkerboard-keying) -- each sheet is split evenly by its known
`(cols, rows)`. Only `portal.png` carries real alpha from the source (haze-thresholded before AND
after resize, premultiplied to avoid fringing).

**Seamless build (why the ground has no visible grid):** the raw sheets are *illustrations of a
grid* -- every cell has a dark panel border painted in, adjacent cells are unrelated paintings and
sibling variants differ in brightness -- so tiled 1:1 they read as a conspicuous square lattice
(the renderer itself draws flat tiles with no seam; verified with a flat-colour probe). The build
fixes this at the texture level (`tools/hd_tile_texture_utils.py`): each cell is cropped inside
its painted border, LANCZOS-resized to 64x64 and **seam-healed** (a feathered strip of clean
interior texture blended over every boundary it repeats across, including the wrap edge).
Grass: the 4 plain variants are brightness-matched and healed into ONE 2x2 **macro** texture
whose quadrants are the variants; flower tiles are the SAME quadrants plus the painted flowers
lifted off the raw flower cells. Path/water blocks become **RGBA** transition masks (grass pixels
transparent via a hue mask) so the seamless base grass drawn under them shows through instead of a
second, non-matching painted grass. `plaza.png` is a 2x2 seamless stone macro cut from the castle
floor cells.

`HD_TILES` in asset-atlas.js maps each tile name to `{image, variants}` or `{image, block}` cells
at 64px/cell -- the same shape as the pixel-art `GROUND_TILES`, so `src/graphics/
tile-sprite-builder.js`'s `buildTiles()` can merge HD over pixel per tile name (HD wins where its
sheet loaded; same lenient-boot convention as the hero set). HD entries may add seamless extras:
`macro: [cols, rows]` (variants are quadrants of one texture; `tile-variant-picker.js` picks
`(ty % rows) * cols + (tx % cols)` so neighbours continue each other -- untagged pixel-art sets keep
the pseudo-random scatter), and for blocks `center` (macro variants for open centers), `inner`
(4 grass wedge masks drawn over concave corners, from the diagonal neighbours), `shapes` (the 7
pieces a 3x3 block lacks: 1-wide pipes, dead-end caps, island -- keyed by open sides) and `fill`
(a macro material drawn THROUGH the block cells' alpha: the Oakhaven plaza `p` tile is
`plazaStone` = stone fill through the dirt path's silhouette, so stone-on-grass gets the same
organic edges and inner corners as the road; dirt and plaza are mutual edge family so a road meets
the plaza with a butt joint). `tile-block-renderer.js` does the per-tile composition with one
reused 64px scratch canvas (`destination-in` masking); the pixel-art fallback never has these
extras and draws the plain 3x3 block as before. Two HD-only names have no pixel-art equivalent:
`flowers` and `waterAnim` (3 open-water variants the HD renderer cycles through on the block's
center cell, ~450ms/frame, while the bank edges stay static). `registry.tilesHd` (true once any
HD tile sheet loaded) tells `tilemap-renderer.js` to toggle `ctx.imageSmoothingEnabled` once per
`drawTilemap()` call (not per tile) and to take the HD-only draw paths above; every tile draw
passes an explicit `TILE_SIZE` destination size regardless.

The portal tile is the one spot where real alpha mattered: `game-controller.js` clears the canvas
to solid black every frame (the old fully-opaque procedural portal tile never needed a backdrop),
so the HD portal frame's transparent corners need ground under them: `tilemap-renderer.js` lays
the neighbouring ground tile (a plain variants tile, or a block tile's open center -- e.g. the dirt
road the Oakhaven gate portal sits on) under the frame, and only falls back to a dark backdrop
fill (`PORTAL_BACKDROP`) at map edges, or the magic circle would float on a black square.

**Adding a new HD tile sheet:** drop the raw PNG in `assets/hd/tiles/<name>.png`, add its
`(cols, rows, margin)` to `GRIDS` plus a builder in `tools/build-hd-tiles.py` (the generic
helpers in `hd_tile_texture_utils.py` cover crop/heal/macro/alpha), run `npm run hd:tiles`, then
add the `hd<Name>` key to `ASSET_FILES` and the tile's cell(s) to `HD_TILES` in asset-atlas.js --
no other code changes needed (`tile-sprite-builder.js` picks up any `HD_TILES` entry whose image
loaded).

### HD props

Same resolution-independence model as the hero/tiles, applied to world props (trees, rocks,
houses, fences, torches...): hand-painted raw sheets in `assets/hd/props/*.png`, normalized by
`tools/build-hd-props.py` (`npm run hd:props`) into one PNG per prop type in
`assets/hd/props/normalized/` (pixelsPerUnit 4, canvas size = that prop's world `width`/`height` x4,
matching `src/world/prop-definitions.js`).

Unlike the flat, evenly-spaced tile sheets, a raw prop sheet holds several unrelated painted
objects (e.g. `village.png` has a fence, signpost, pillar, torch, crystal and a bone pile) placed
irregularly -- NOT on an exact pixel grid, and often with a faint painted glow/halo around each
object (same look as a single pine tree's background glow). The build script: (1) thresholds that
haze (alpha <= 48 -> fully transparent) for the whole sheet; (2) splits it into per-object segments
-- most sheets split automatically by finding fully-transparent gap columns between objects, one
manual split handles the one sheet (`desert.png`) where two objects touch by a few stray
antialiased px even past the haze threshold; (3) tight-crops each segment to its real alpha bbox,
then LANCZOS-resizes (premultiplied alpha, to avoid edge fringing) so the limiting dimension exactly
fills that prop's target canvas, pasted bottom-center (the object's base sits on the canvas's bottom
edge, same convention as a pixel-art prop sprite that's taller than its footprint). Two raw sheets
hold only one object each but feed two prop types with identical art (`pine_tree.png` -> `pine` AND
`pineDark`; `round_tree.png` -> `roundTree` AND `bigTree`).

`HD_PROPS` in `src/graphics/asset-atlas-hd-props.js` (re-exported from asset-atlas.js) maps each
prop type to its normalized-file `ASSET_FILES` key -- same shape/merge convention as `HD_TILES`:
`src/graphics/prop-sprite-builder.js`'s `buildProps()` merges it over the pixel-art `PROP_RECTS` cut
per prop type, HD winning where its file loaded. `prop-renderer.js`'s `drawProp()` always passes an
explicit `(def.width, def.height)` world-unit dest size and toggles `ctx.imageSmoothingEnabled` from
the picked canvas's `pixelsPerUnit`, restoring `false` immediately after -- identical convention to
the hero/tile HD draws.

`fence` and `signpost` got a height bump (16 -> 24 world units, footprint unchanged at 1 tile) to
match the already-established "taller than its footprint" convention used by `pillar`/`torch`/
`crystal`: a full wooden fence post painted at the correct aspect ratio reads as a tiny squashed
stub if forced into a flat 16x16 box.

**Adding a new HD prop sheet:** drop the raw PNG in `assets/hd/props/<name>.png`, add it to
`SHEETS` in `tools/build-hd-props.py` (segmentation mode + the prop type(s) its segments map to,
left to right) and its target world size to `PROP_TARGETS` (must match `prop-definitions.js`), run
`npm run hd:props`, then add the prop's `hdProp<Name>` key to `HD_PROP_ASSET_FILES` and `HD_PROPS`
in `asset-atlas-hd-props.js` -- no other code changes needed (`prop-sprite-builder.js` picks up any
`HD_PROPS` entry whose file loaded).

**Pending village set:** `prop-definitions.js` and `build-hd-props.py` already define 10 Oakhaven
Village props with no art yet (well 32x48, lanternPost 16x32, marketStall 48x48, cropPlot 32x16,
scarecrow 16x32, woodpile 32x32, campfire 16x16, barrel 16x16, crate 16x16, rowboat 32x16 -- world
units, canvas = x4). Painting `assets/hd/props/village_props.png` (objects left-to-right in that
order, transparent background, >= 2 empty columns between objects), running `npm run hd:props`, then
adding the `HD_PROP_ASSET_FILES`/`HD_PROPS` keys and `placeProp()` calls at the spots commented in
`oakhaven-village-map.js` is all that's left. `tests/hd-props-config.test.js` fails loudly if an
`HD_PROPS` entry is added before its file exists.

### HD NPCs (Oakhaven villagers)

Same contract as the HD hero, applied to the 10 village NPCs: raw hand-painted sheets
(`assets/hd/map/Oakhaven/<Name>.png`, 4 columns x 3 rows: idle frames across, rows = front /
turned-left / turned-right, no rear pose) are normalized by `tools/build-hd-npcs.py`
(`npm run hd:npcs`) into `assets/hd/npcs/normalized/<npcId>.png` (384x288 = 4 x 3 frames of 96px,
24 world units x pixelsPerUnit 4, feet on the bottom row, body ~82px so villagers stand at hero
height; the child is 70px) plus `<npcId>_face.png` (152x152 head-and-shoulders crop for the dialog
box). The script reuses build-hd-hero.py's helpers (haze threshold / checkerboard keying, gap-snapped
grid lines, premultiplied LANCZOS downscale, feet pivot) through importlib so the two pipelines
cannot drift.

Engine side: `asset-atlas-hd-npcs.js` lists the ids + file keys; `sprite-registry.js` builds
`registry.npcsHd[id] = { down|left|right: { idle[4] } }` and drops the HD portrait into
`registry.faces[id]` (stamped `pixelsPerUnit` so `dialog-box.js` smooth-fits it instead of 3x
pixel-scaling). `prop-renderer.js`'s `drawNpc()` prefers `npcsHd[npc.id]` (24-unit dest size,
smoothing on, idle frame = `npc.animTimer / 260ms`, ticked every frame in world-update.js and never
reset, so the loop never restarts while standing; `up` maps to the front row) and falls back to the
pixel-pack `npcs[def.sprite]` at 16 units for any id without a loaded sheet (the 3 field NPCs, or a
missing file). `game-controller.js` turns the nearest NPC towards the hero each frame
(`faceTowards`, dominant axis) so the left/right rows are used when approached from the side.
Collision stays the 1-tile footprint `map-manager.js` always gave NPCs; y-sort uses the feet `y`
like every other entity. The hero's floating "Hero" label is now a dev overlay
(`SHOW_DEBUG_LABELS` in constants.js, or `window.__zenoniaDebug = true` at runtime).

## Quest System

**Core** (quest-system.js): Pure state machine (acceptQuest, recordKill, turnInQuest, questStatus).
**UI**: Dialog, tracker (top-right), log (Q), feedback banners.
**Save**: `{active: {questId: {progress}}, completed: [...]}`

## Equipment & Inventory

**Drops**: `combat-system.js`'s `onMonsterDeath` calls `rollEquipmentDrop(monster.def)` (equipment-system.js)
alongside the existing potion/gold `rollDrops`. Normal monsters have a 12% chance; bosses always drop a
guaranteed rare-or-epic item. `itemLevel` on the `MonsterDefinition` gates which weapon/armor templates
can be picked (`TEMPLATES_BY_SLOT` in `data/item-definitions.js`, filtered by `minItemLevel`) and scales
the rolled stats. Dropped items spawn as `new ItemDrop(x, y, 'equipment', item)` -- rendered via
`registry.items[item.templateId]` (weapon sprite at 1x, armor icon at 0.6x, rarity glow pulse on rare+).

**Pickup**: `world-update.js`'s `updatePickups` calls `addToInventory(player, drop.item)`; on success the
drop is consumed and `player.hasUnseenItem = true` (drives the HUD's blinking "NEW" tag next to "B Bag"
until the bag is opened). When the bag is full the drop stays on the ground and shows a throttled
"Inventory full" message.

**Equip/unequip**: `equipItem`/`unequipItem` (equipment-system.js) mutate `player.inventory`/`player.equipment`
only -- same convention as `skill-system.js`'s `upgradeSkill`: the UI caller (`inventory-panel.js`) calls
`player.recomputeStats()` afterward. `recomputeStats()` sums `computeEquipmentModifiers()` on top of
`computePassiveModifiers()`: `atk`/`def`/`maxHp` get additive bonuses, `speed` gets a second multiplier,
and equipment `critChance` is folded straight into `player.passiveMods.critChance` so the existing crit-roll
call sites (`player-damage.js`, `combat-system.js`) don't need to know equipment exists.

**UI** (`inventory-panel.js` + `inventory-layout.js`, state `INVENTORY`, key `B`): mirrors the skill tree
panel's layout/panel split. Left column: hero faceset + 5 equipment slots; right: 5x4 bag grid (`INVENTORY_SIZE
= 20`). Arrows navigate across both areas, Enter equips (from bag) or unequips (from a slot), X drops the
selected bag item, mouse click selects and double-click equips. Info box shows the selected item's stats and,
for bag items, a green/red delta line vs. the currently equipped item in that slot.

**Save**: `player.toSaveData()` serializes `inventory`/`equipment` as-is; `save-serializer.js`'s `validateSave`
runs every item through `sanitizeItem()` (unknown `templateId`/slot mismatch/unknown rarity -> dropped, stats
clamped) -- never trusts raw client JSON, same policy as skills/hotbar.

## Pixel Rendering Pipeline

1. **String pixel-map** (e.g., `["KSH", "ssh", ...]`) — one character per pixel.
2. **Palette lookup** (palette.js) — each character → CSS color (or null = transparent).
3. **Offscreen canvas** (createSprite) — draw pixels at 1:1 scale.
4. **3× scale on render** — `ctx.drawImage(sprite, x, y, w*3, h*3)` + `imageSmoothingEnabled = false` → chunky pixel look.
5. **FlipX / Remap** (for variants, e.g., facing left) — swap character map before palette lookup.

## Coordinate System

| Unit | Definition | Notes |
|------|-----------|-------|
| **World px** | Game logic unit (pixels in tilemap grid) | 1 world px = 3 screen px (via PIXEL_SCALE) |
| **Tile** | 16 world px × 16 world px | Maps stored as 1 char = 1 tile |
| **Entity position** | `(x, y)` = center-x, feet-y (bottom) | Hitbox computed from here |
| **Camera** | Follows player center, clamped to map bounds | Render offset applied in world-renderer.js |
| **Hitbox** | `{w, h}` stored in config; center from `(x, y)` | e.g., player 10px wide, 8px tall |

## Testable (Pure) vs. DOM Layers

**Testable (Node --test)**: tilemap.js, systems/*.js, data/*.js, save-serializer.js, entities/*.js, quest-system.js.
**DOM/Browser Only**: graphics/*.js, ui/*.js, game/{game-controller, game-session, game-loop, input-manager}.js, main.js.
