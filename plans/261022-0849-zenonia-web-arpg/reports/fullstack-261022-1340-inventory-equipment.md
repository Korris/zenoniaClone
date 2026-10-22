# Inventory + equipment drops implementation

## Summary
Equipment drop/pickup/equip system: 9 weapon tiers + 12 armor pieces (4 slots x 3 tiers), 4-rarity
scaling, 12%/100% drop rolls gated by a new per-monster `itemLevel`, 20-slot bag + 5 equipment slots,
new inventory panel (B), stats fold into player.recomputeStats() alongside passives. 93/93 unit tests
pass, browser smoke test `errors []` across 3 runs (drop -> pickup -> equip -> atk increase verified).

## Files changed
New:
- `src/data/item-definitions.js` (66 lines) - EQUIPMENT_SLOTS, RARITIES (common/uncommon/rare/epic,
  weights 70/20/8/2%, mult 1.0/1.25/1.6/2.1, prefixes Fine/Superior/Legendary), WEAPON_TEMPLATES (9),
  ARMOR_TEMPLATES (12, 3 tiers x helmet/armor/gloves/boots), ITEM_TEMPLATES/TEMPLATES_BY_SLOT.
- `src/systems/equipment-system.js` (141 lines) - `generateItem(rand, itemLevel, slot?, forceRarity?)`,
  `rollRarity`, `rollEquipmentDrop(monsterDef, rand)` (12% normal / 100% boss forced rare-epic),
  `computeEquipmentModifiers`, `addToInventory`/`removeFromInventory`, `equipItem`/`unequipItem`
  (bag-swap, capacity-checked, do NOT call recomputeStats -- same convention as skill-system's
  upgradeSkill), `isUpgrade`, `sanitizeItem`, `INVENTORY_SIZE = 20`.
- `src/ui/inventory-panel.js` (193 lines), `src/ui/inventory-layout.js` (66 lines) - bag/equipment UI,
  mirrors skill-tree-panel.js/skill-tree-layout.js split.
- `tests/equipment-system.test.js` (13 tests) - determinism, rarity weight boundaries, itemLevel
  gating, drop chances (normal/boss), modifier aggregation, equip/unequip swap + overflow refusal,
  isUpgrade, sanitizeItem, save round-trip.

Edited:
- `src/data/monster-definitions.js` - added `itemLevel` (slime/bat 1, goblin 3, skeleton 4, fireSlime
  6, goblinWarrior 8, skeletonKnight 11, golemBoss 14).
- `src/entities/player.js` - `inventory`/`equipment`/`hasUnseenItem` fields; `recomputeStats()` adds
  `computeEquipmentModifiers()` on top of passives (atk/def/maxHp additive, speed second multiplier,
  equipment critChance folded into `passiveMods.critChance` so existing crit call sites need no
  changes); `toSaveData`/`applySaveData` carry inventory+equipment.
- `src/entities/item-drop.js` - `kind:'equipment'` carries an item instance (`drop.item`), new
  `pickupMsgCooldown` throttles the "Inventory full" text while standing on a drop you can't pick up.
- `src/systems/combat-system.js` - `onMonsterDeath` also rolls `rollEquipmentDrop` and pushes an
  `ItemDrop('equipment', item)` alongside the existing potion/gold drops.
- `src/game/world-update.js` - `updatePickups` handles `kind==='equipment'`: `addToInventory`, floating
  rarity-colored "+ Name" text, `hasUnseenItem=true`, or throttled "Inventory full" in red.
- `src/graphics/effects-renderer.js` - `drawDrops` renders equipment: weapon sprite 1x / armor icon
  0.6x via `registry.items[templateId]`, rarity glow pulse (ellipse, pulsing alpha) on rare/epic.
- `src/graphics/asset-atlas.js` - added 9 weapon PNGs + 4 gear icons (Helmet/Armor/Guard/Boot) to
  `ASSET_FILES`.
- `src/graphics/sprite-registry.js` - builds `registry.items[templateId]` from `ITEM_TEMPLATES`.
- `src/core/constants.js` - `GAME_STATES.INVENTORY`.
- `src/core/input-manager.js` - `inventory: ['KeyB']`, `dropItem: ['KeyX']`.
- `src/game/game-controller.js` - wires `InventoryPanel`, `S.INVENTORY` state (open on B, clears
  `hasUnseenItem`, saves on close, same pattern as SKILLS/QUEST_LOG).
- `src/ui/hud-renderer.js` - hint box widened, "Q Quests   B Bag" line, blinking red "NEW" tag while
  `player.hasUnseenItem`.
- `src/save/save-serializer.js` - `validateSave` sanitizes `inventory`/`equipment` through
  `sanitizeItem()` (unknown templateId/slot-mismatch/unknown rarity dropped, stats clamped).
- `tools/browser-check.mjs` - new steps: force-generate an epic weapon drop at the hero's feet,
  wait for pickup, open bag (B) -> `09-inventory.png`, Enter to equip -> `10-equipped.png` (confirms
  `player.atk` increased), close, then death pose renumbered `11-death.png`.
- `docs/codebase-summary.md`, `docs/system-architecture.md` (new "Equipment & Inventory" section +
  ItemInstance shape + SaveObject/MonsterDefinition updates), `docs/code-standards.md` ("Adding a New
  Equipment Item"), `docs/project-roadmap.md` (v0.6 section + backlog tweaks), `README.md` (B bag
  control, equipment feature line, test list) updated.

## How it works
- **Drop roll**: `combat-system.js onMonsterDeath` -> `rollEquipmentDrop(monster.def)`. Normal: 12%
  chance via `rand() >= 0.12`. Boss (`behavior==='boss'`): always drops, forced rarity rare (50%) or
  epic (50%). `itemLevel` on the def both gates which templates are eligible
  (`TEMPLATES_BY_SLOT[slot].filter(t => t.minItemLevel <= itemLevel)`, falls back to the weakest tier
  if nothing qualifies) and scales stats: `round(base * rarityMult * (1 + 0.08*(itemLevel-1)))`.
- **Pickup**: same `world.drops` list as potions/gold; `updatePickups` special-cases
  `kind==='equipment'` to call `addToInventory` instead of `applyPickup`. Bag-full keeps the drop alive
  and throttles the warning text via `drop.pickupMsgCooldown` (1s) so standing on it doesn't spam text.
- **Equip/unequip**: `equipItem`/`unequipItem` only mutate `player.inventory`/`player.equipment` (bag
  swap, capacity pre-checked so nothing is ever lost); the UI caller (`inventory-panel.js`) calls
  `player.recomputeStats()` afterward -- deliberately the same pattern as `skill-system.js`'s
  `upgradeSkill` + `skill-tree-panel.js`, so equipment-system.js stays trivially unit-testable with
  plain mock players (no need for a full `Player` instance in most tests).
- **Stats**: `recomputeStats()` now also calls `computeEquipmentModifiers()` and applies
  atk/def/maxHp additively, speed as a second multiplier, and folds equipment crit straight into
  `passiveMods.critChance` -- `player-damage.js`/`combat-system.js`'s existing
  `player.passiveMods?.critChance` reads pick it up with zero changes to those files.
- **UI** (B / Esc / Tab, `S.INVENTORY`): `inventory-panel.js` mirrors `skill-tree-panel.js`'s
  layout/panel split. Left: faceset + 5 equipment slots (abbreviated WPN/HELM/ARMR/GLV/BOOT labels to
  avoid overlap at the 52px slot size). Right: 5x4 bag grid (64px slots, `gridSlotPosition`). Selection
  state `{area:'equipment'|'bag', index}`; arrows cross between areas at matching row/col; Enter
  equips (bag) or unequips (equipment slot); X drops (destroys) the selected bag item; mouse click
  selects, double-click (350ms window, tracked via `performance.now()`) activates. Info box shows
  absolute stats (`describeItemStats`) and, for bag selections, green/red per-stat deltas vs. the
  currently-equipped item in that slot (`compareToEquipped`, drawn manually via `ctx.fillText` +
  `measureText` since `drawText` only supports one color per call).
- **Rendering**: ground drops use `registry.items[templateId]` (weapon PNGs/gear icons from the CC0
  Ninja Adventure pack, no code-drawn sprites -- same source as every other asset in the game).
  Weapon sprites draw at 1x, armor icons at 0.6x; rare/epic drops get a pulsing colored ellipse under
  them. Inventory/equipment slot icons reuse the same sprites at larger scale (3x weapon / 2x gear,
  0.8x in the smaller 52px equipment column).
- **Save**: `player.toSaveData()` serializes inventory/equipment as-is (matches the existing
  skills/hotbar convention: raw data out, sanitize on the way back in);
  `save-serializer.js validateSave` runs every item through `sanitizeItem` (template/slot/rarity
  checked, stats clamped 0-999, itemLevel clamped 1-30) -- untrusted JSON never reaches `player.equipment`.

## Tests
- `node --test tests/*.test.js` -> 93/93 pass (80 pre-existing + 13 new in
  `tests/equipment-system.test.js`).
- `node tools/browser-check.mjs tools/screenshots` -> `errors []`, run 3x. Confirmed each run:
  `pickup {"inventory":1,"hasUnseenItem":true,...}`, `equip {"beforeAtk":60,"afterAtk":78|86,...}` --
  delta matches the stat formula exactly (e.g. epic Katana itemLevel 6: `round(9*2.1*1.4)=26`,
  60->86 observed).
- `node --check` on every `src/**/*.js`: clean. `wc -l`: every file < 200 lines (inventory-panel.js
  largest of the new files at 193).
- Screenshots reviewed: `09-inventory.png` (all 5 equipment slots + 5x4 grid render without overlap
  after shrinking the equipment-column slot to 52px/56px gap; item stats + delta shown in info box),
  `10-equipped.png` (epic weapon border in magenta `#e0a0ff`, "Equipped Legendary Short Sword" footer
  message, bag count 0/20). Fixed one layout bug mid-implementation: initial equipment-column labels
  (drawn below each 64px box with a 72px gap) were covered by the next box down, and the 5th (Boots)
  slot was clipped by the info panel -- fixed by shrinking the equipment slot box (64->52px),
  tightening the gap (72->56px), moving labels inside the box bottom (abbreviated WPN/HELM/ARMR/
  GLV/BOOT), and re-tuning EQUIP_TOP so all 5 fit above INFO_BOX_Y.

## Usage
- Open bag: B (closes with B/Esc/Tab, same as skill tree's Tab/Esc). Arrows navigate; Left from bag
  column 0 jumps to the equipment column at the matching row (clamped); Right from an equipment slot
  jumps to the bag's matching row, column 0.
- Enter: equip the selected bag item, or unequip the selected equipment slot (back into the bag, if
  there's room). X: permanently drops (deletes) the selected bag item. Mouse: click selects,
  double-click (within 350ms) performs the same equip/unequip action as Enter.
- New item checklist is in `docs/code-standards.md` -> "Adding a New Equipment Item".

## Unresolved / judgment calls
- `isUpgrade(item, equipped)` exists and is unit-tested per spec but isn't wired into the UI as a
  visible badge (the green/red stat-delta line already conveys the same information more precisely;
  adding a redundant "UPGRADE!" tag felt like YAGNI). Easy to wire in later if wanted.
- "X drop" permanently deletes the item (no confirmation, no world drop spawned) per the spec's wording
  ("X = drop (delete) selected bag item") -- if the intent was a tossable ground item instead, that's
  a one-line change (`world.drops.push(new ItemDrop(...))` instead of just `removeFromInventory`).
- Potions/gold still auto-apply on pickup (unchanged); only equipment goes into the bag. Consumables-
  in-the-bag is noted as a natural v0.7+ follow-up in the roadmap, not implemented here (YAGNI --
  wasn't asked for).
- Observed one pre-existing flaky assertion in `browser-check.mjs`'s `doubleTapDash` step
  (`dashing:false` on 2 of 3 runs) -- this is unrelated to this task (same double-tap timing logic in
  `player-controller.js` was untouched; prior skill-tree report also hit cold-start timing flakes) and
  `errors []` was clean on every run regardless.
- Weapon/armor tier `minItemLevel` assignments and the 8%-per-level stat scaling constant are my
  balance judgment calls within the spec's structural constraints (exact numbers weren't specified
  beyond baseAtk values and the four monster-tier examples) -- not separately playtested for balance.

## Key paths
- `D:\zenonia\src\data\item-definitions.js`, `D:\zenonia\src\systems\equipment-system.js`
- `D:\zenonia\src\ui\inventory-panel.js`, `D:\zenonia\src\ui\inventory-layout.js`
- `D:\zenonia\tests\equipment-system.test.js`
- `D:\zenonia\tools\screenshots\09-inventory.png`, `D:\zenonia\tools\screenshots\10-equipped.png`
