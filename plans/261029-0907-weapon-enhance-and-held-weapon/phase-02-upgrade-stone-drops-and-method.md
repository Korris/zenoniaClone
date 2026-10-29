# Phase 02 — Upgrade stone material, drops, isolated upgrade method

## Context Links
- src/systems/loot-system.js (rollDrops, applyPickup), src/entities/item-drop.js (DROP_KINDS)
- src/data/monster-definitions.js (`base()` drops defaults, bosses), src/game/world-update.js (lootLabel)
- src/entities/player.js (potions, toSaveData/applySaveData), src/save/save-serializer.js
- src/graphics/potion-renderer.js (`drawPotion` used by effects-renderer, loot-effects, activity-notice, inventory)

## Overview
Priority P1. Status completed. Stackable "Upgrade Stone" consumable, a drop source, and `attemptEnhance`
with the cost/success roll behind a swappable method module.

## Key Insights
- Supplies are not inventory items (potions live in `player.potions` + supply cells). Stones follow the same
  pattern -> no bag slot cost, no INVENTORY_SIZE impact.
- Store under new `player.materials = { upgradeStone }` (not `potions`: usePotion/HUD keys iterate potions).
- `drawPotion(kind)` is the single drop/notice/supply icon entry point (4 call sites) -> delegate
  `upgradeStone` from inside it = 1 edit, DRY.
- rollDrops consumes rand() in order; append the stone roll LAST so existing seeded tests keep their sequence.

## Requirements
- Drop: default chance 5% per kill (`drops.upgradeStone: 0.05` in `base()`); local boss (Frostfang) and final
  boss guaranteed 2 (`drops.upgradeStoneCount`). Kind `upgradeStone`, value = count.
- Pickup: `+1 Upgrade Stone`, sets hasUnseenItem; label color `#9fd8ff`.
- Method contract (temporary, replaceable):
  ```js
  // weapon-upgrade-method-stone.js
  export const STONE_UPGRADE_METHOD = {
    id: 'upgradeStone',
    successChance(level) {},       // chance to go level -> level+1
    canPay(player, level) {},      // bool
    pay(player, level) {},         // consumes 1 stone
    costLabel(level) {},           // '1 Upgrade Stone' for UI
  };
  ```
- Proposed success table (target level -> chance):
  +1..+3 100% | +4 90 | +5 85 | +6 80 | +7 70 | +8 65 | +9 60 | +10 50 | +11 45 | +12 40 | +13 30 | +14 25 | +15 20.
  Expected stones to +15 ~ 36 (sum 1/p).
- `attemptEnhance(player, item, { method = STONE_UPGRADE_METHOD, roll = Math.random })` returns
  `{ ok, success, level, reason }`; reasons: `not-weapon`, `max-level`, `no-cost`. Fail = cost paid, level kept.
- Item must be owned (bag or equipped) — check uid in inventory or equipment.weapon.

## Architecture
```
weapon-enhance-system.attemptEnhance --uses--> method.{canPay,pay,successChance}
        |                                         (only file knowing about stones)
        +--> item.enhance += success ? 1 : 0 ; if equipped -> player.recomputeStats()
```
Enhance core knows nothing about stones; P3 UI reads `method.costLabel` + `successChance`.

## Related Code Files
- Create: `src/systems/weapon-upgrade-method-stone.js`, `src/systems/weapon-enhance-system.js`,
  `src/graphics/upgrade-stone-renderer.js` (code-drawn chibi crystal: faceted cyan/violet gem, clean dark
  outline, 1 highlight, no glow haze — mirrors potion-renderer style), `tests/weapon-enhance-system.test.js`,
  `tests/upgrade-stone-loot.test.js`
- Modify: `src/entities/player.js` (materials init/save/apply), `src/save/save-serializer.js` (validate
  `materials.upgradeStone` int 0..9999), `src/systems/loot-system.js` (rollDrops append, applyPickup case),
  `src/entities/item-drop.js` (DROP_KINDS.upgradeStone), `src/data/monster-definitions.js` (default + boss counts),
  `src/game/world-update.js` (lootLabel), `src/graphics/potion-renderer.js` (delegate kind)

## Implementation Steps
1. Method module with SUCCESS_BY_TARGET table (index = target level).
2. Enhance system: validate weapon/max/ownership -> canPay -> pay -> `roll() < chance` -> mutate -> recompute if equipped.
3. Player: `this.materials = { upgradeStone: 0 }`; toSaveData/applySaveData (`?? 0`).
4. Save validation: `materials: { upgradeStone: clampInt(p.materials?.upgradeStone, 0, 9999) }`.
5. Drops: `if (rand() < (def.drops.upgradeStone || 0)) drops.push({kind:'upgradeStone', value: def.drops.upgradeStoneCount || 1})`
   placed after mpPotion roll; bosses set chance 1.
6. applyPickup: `player.materials.upgradeStone = min(9999, +value)`.
7. Stone renderer + potion-renderer delegate; lootLabel entry.
8. `npm test`.

## Todo List
- [x] method module  - [x] enhance system  - [x] player/save materials
- [x] drops + pickup + label  - [x] stone renderer  - [x] tests

## Tests
- attemptEnhance with roll ()=>0 succeeds, ()=>0.999 fails at +4 (90%): stone consumed, level kept.
- No stones -> `no-cost`, no mutation. +15 -> `max-level`, no stone consumed. Armor -> `not-weapon`.
- Equipped weapon success -> player.atk increases (recompute called).
- Unowned item uid -> rejected.
- Success table monotonic non-increasing, values in (0,1].
- rollDrops: seeded rand reproduces old drops for first entries; stone appears when rand < 0.05; boss always.
- applyPickup increments; save round-trip materials; garbage materials -> 0; legacy save (no field) -> 0.

## Success Criteria
Unit tests green; picking a stone in-game increments count (verified in P9 browser check).

## Risk Assessment
| Risk | L x I | Mitigation |
|---|---|---|
| Existing rollDrops tests use fixed sequences of rand values | M x M | Append roll last; update fixture only if it asserts exhaustion |
| Stone economy too slow/fast | M x L | Chance in monster defs; ~36 stones to +15 ~ 720 kills at 5% — revisit |
| Future method swap leaks stone logic elsewhere | L x M | Only method module + supply UI reference `upgradeStone` |

## Security Considerations
Clamp counts from save; no negative stones; enhance only on owned items.

## Rollback
Revert; saved `materials` ignored by old validateSave (stones lost on next save — acceptable for temp method).

## Next Steps
P3 UI.

## Unresolved
- Drop rate 5% / boss 2 acceptable? Should stones also be buyable from an NPC later (out of scope)?
