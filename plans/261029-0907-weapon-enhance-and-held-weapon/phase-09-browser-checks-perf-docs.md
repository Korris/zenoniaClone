# Phase 09 — Browser checks, perf verification, docs

## Context Links
- tools/check-weapon-icons.mjs (puppeteer-core pattern, `window.__zenonia`, Chrome path, screenshots dir)
- tools/check-combat-soak.mjs (fps/p99/GPU), tools/check-first-cast-hitch.mjs
- docs/session-handoff.md, README.md (Art direction), package.json scripts

## Overview
Priority P2. Status pending. Observable validation + docs. Server: `npm start` (port 5173).

## Requirements / Browser checks
1. `tools/check-weapon-enhance.mjs` (Track A, runnable after P3):
   - Give 40 stones (`player.materials.upgradeStone = 40`), generate rare warrior sword, open Inventory, select it.
   - Set `panel.enhanceRoll = () => 0` -> click Enhance x7; assert `item.enhance === 7`, stones 33, player.atk rise when equipped.
   - Set roll `() => 0.999` at +7 -> click; assert level 7, stones 32, flash text contains "Failed".
   - Drive to +15; assert button disabled + "Max". Kill/inject a stone ItemDrop; pickup increments count.
   - Save -> reload page -> load -> enhance and stones persisted.
   - Screenshots: bag detail at +0/+7/+15, loadout view, supply cell. `tools/screenshots/weapon-enhance/`. 0 page errors.
2. `tools/check-held-weapon.mjs` (Track B, after P7/P8):
   - Asserts registry.heldWeapon.overlays has 9 types x 2 non-empty; anchors loaded.
   - For each of 9 types at +0 and +13: force each anim (idle/walk/attack1-3/dash/hurt) x 4 dirs, screenshot crop
     around hero at actual game zoom -> grid image per type. Also +0/+7/+9/+11/+13/+15 strip for sword.
   - Pixel sanity: weapon overlay alpha present near anchor point (sample canvas) for non-hidden frames.
   - Wizard/mechanic still render baked sheets (no overlay call).
   - `tools/screenshots/held-weapon/`. 0 page errors.
3. Perf: run `node tools/check-combat-soak.mjs 60 warrior wizard` with warrior +15 bigSword equipped (add optional
   arg/env to equip); target >= 59 fps, p99 <= 17ms, 0 frames > 25ms; compare GPU% to baseline 13.6%.
   `tools/check-first-cast-hitch.mjs` still passes (first glow bake must not hitch: bake on equip/first draw ~<1ms; if not, pre-bake on equip).

## Unit test matrix (all phases)
| Area | Unit (node --test) | Integration | Browser/E2E |
|---|---|---|---|
| Enhance stats/sanitize | weapon-enhance-stats | save-serializer round trip | check-weapon-enhance |
| Method/attempt/drops | weapon-enhance-system, upgrade-stone-loot | rollDrops->applyPickup | pickup in check-weapon-enhance |
| UI | inventory-enhance-actions | character-menu existing | screenshots |
| Overlay assets | weapon-overlay-manifest | - | check-held-weapon |
| Anchors | held-weapon-anchors | - | anchor-preview.png review |
| Renderer/glow | weapon-glow-baker, held-weapon-renderer | player-renderer existing | check-held-weapon + soak |
| Icons | enhanced-item-icons | weapon-icons existing | screenshots |

## Docs
- `docs/session-handoff.md`: append section (enhance tables, stone drop rates, method isolation note, weaponless
  pipeline commands `npm run weapons:overlays`, `hd:weaponless`, `hd:anchors`, fallback flag).
- `README.md`: short feature note + commands. Art direction unchanged; note glow rule (crisp rings, no fog).
- `package.json` scripts already added in P4-P6.

## Todo List
- [ ] check-weapon-enhance.mjs  - [ ] check-held-weapon.mjs  - [ ] soak + hitch run  - [ ] docs  - [ ] user visual review

## Success Criteria
Both checks pass with 0 page errors; perf targets met; `npm test` all green; user approves screenshots (do not claim
art approval before user responds — per handoff convention).

## Risk Assessment
| Risk | L x I | Mitigation |
|---|---|---|
| Check scripts flaky on timing | M x L | waitForFunction on state, not sleeps |
| GPU regression from 2 extra draws | L x M | soak comparison; fallback flag |

## Rollback
Checks/docs only.

## Next Steps
Later: replace stone method (new method module, same contract); wizard staff / mechanic gun overlays (reuse P4-P7 pipeline).
