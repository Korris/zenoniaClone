# Phase 03 — Enhance UI in Inventory / Equipment panels

## Context Links
- src/ui/inventory-panel.js (exactly 200 LOC; already draws a disabled 'Enhance' button at BAG_ACTIONS.secondary)
- src/ui/inventory-layout.js (BAG_ACTIONS), src/ui/bag-widgets.js (bagButton, bagText, bagWrappedText)

## Overview
Priority P2. Status completed. Make the existing greyed "Enhance" button work for weapons, add an Enhance button
on the Equipment (loadout) view for the equipped weapon, add Upgrade Stone supply cell.

## Key Insights
- inventory-panel.js is at the 200-line limit -> extract enhance UI into its own module before adding.
- Button already reserved at `BAG_ACTIONS.secondary` (766,528,146x32) — reuse; no new layout for bag view.
- Loadout view: `unequip` sits at 748..908; add `BAG_ACTIONS.loadoutEnhance = {x:540,y:528,w:150,h:32}`
  (inside left panel 320..696).
- Panel has no world/RNG; expose `panel.enhanceRoll = Math.random` (overridable by checks/tests).

## Requirements
- Bag view, weapon selected: detail shows `+N -> +N+1`, `Success 70%`, `ATK 52 -> 58`, `Cost: 1 Upgrade Stone (have 4)`.
  Button enabled (gold) when canPay && < +15 && weapon; otherwise 45% alpha (current look) + reason on click.
- Loadout view: weapon slot selected -> loadoutEnhance button with same behavior on equipped weapon.
- Result flash: `Success! Sword +8` (gold) / `Failed... stone lost (+7 kept)` (red-ish) / `Max +15`.
- Supply cell `upgradeStone` (supplyOrder = coin, hpPotion, mpPotion, upgradeStone; cellCount 23 -> 24, still 2 pages); details text
  "Used to enhance weapons. Success drops at higher +." No 'Use' button.
- Item name in detail panel uses `displayItemName`.
- No confirm dialog (failure never destroys/downgrades) — YAGNI.

## Architecture
New `src/ui/inventory-enhance-actions.js`:
- `enhancePreview(player, item, method)` -> pure view-model `{ can, reason, from, to, chance, atkFrom, atkTo, costLabel, owned }` (testable).
- `drawEnhancePreview(ctx, preview, x, y)` — 3-4 text lines in detail panel area (y 403 region, replacing set blurb for weapons).
- `runEnhance(panel, player, item)` -> calls `attemptEnhance(..., { roll: panel.enhanceRoll })`, flashes message.
inventory-panel.js: click routing for `secondary` / `loadoutEnhance`, keyboard: none new (YAGNI); delegates to module.
Move `drawSupplyDetails`/`drawSupplyIcon` into `src/ui/inventory-supply-cells.js` to get headroom (<200 LOC).

## Related Code Files
- Create: `src/ui/inventory-enhance-actions.js`, `src/ui/inventory-supply-cells.js`, `tests/inventory-enhance-actions.test.js`
- Modify: `src/ui/inventory-panel.js`, `src/ui/inventory-layout.js` (BAG_ACTIONS.loadoutEnhance)

## Implementation Steps
1. Extract supply drawing to inventory-supply-cells.js (pure move; SUPPLY_NAMES gains upgradeStone).
2. Add enhance-actions module (preview + draw + run).
3. Wire clicks: bag mode `secondary` -> runEnhance(selected weapon); equipment mode `loadoutEnhance` when slot weapon.
4. Replace greyed button draw with state-dependent tone/alpha.
5. Use displayItemName in detail title.
6. `npm test`; manual run `npm start`.

## Todo List
- [x] extract supply cells  - [x] preview model  - [x] click wiring  - [x] stone supply cell  - [x] tests

## Tests
- enhancePreview: armor -> can=false reason 'Weapons only'; 0 stones -> 'Need Upgrade Stone'; +15 -> 'Max'; normal values match P1/P2 tables.
- runEnhance with forced roll updates item and flash text; equipped path recomputes player.atk.
- Existing character-menu / potion-storage tests still pass (supply order change: assert by key, not index).

## Success Criteria
Clicking Enhance with stones visibly changes name to +N and ATK line; P9 screenshots at +0/+7/+15.

## Risk Assessment
| Risk | L x I | Mitigation |
|---|---|---|
| supplyOrder grows to 4 -> page math/cellCount shifts bag indices | M x M | cellSelection already offset-based; test navigation to last bag cell |
| Sort button sorts supplies by count -> stone order changes | L x L | acceptable, keyed by kind |
| Button overlap in loadout view | L x L | coordinates chosen inside left panel; screenshot check |

## Security Considerations
None beyond P2 validations (UI calls only attemptEnhance).

## Rollback
Revert UI files; logic from P1/P2 remains harmless.

## Next Steps
P8 adds +N badges and glow icons.

## Unresolved
- Want a keyboard shortcut for Enhance (e.g. `E`)? Default: mouse/touch only.
