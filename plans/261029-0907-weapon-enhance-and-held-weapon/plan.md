---
title: "Weapon enhancement (+0..+15) and visibly held weapon"
description: "MU-style stone enhancement for weapons plus warrior paper-doll weapon overlay with enhance-band visuals"
status: pending
priority: P2
effort: 30h
branch: main
tags: [gameplay, items, ui, graphics, art-pipeline]
created: 2026-10-29
---

# Weapon enhance + held weapon

Two tracks. Track A (gameplay, pure code) ships independently of Track B (art + renderer).
Track B is asset-gated: no weaponless sheet / anchors for an animation -> baked art (zero regression).

## Key decisions
- `item.enhance` 0..15 on weapon instances; missing => 0; base `item.stats` never mutated, bonus derived.
- Upgrade method (stone cost + success table) isolated in `weapon-upgrade-method-stone.js`; swap later.
- Fail = stone consumed, level kept. RNG injected (`roll` param), like `world.hitRoll`.
- Held weapon: **WIZARD first** (scope change 2026-10-29: warrior is being redesigned later; do NOT build warrior held-weapon; warrior/mechanic keep baked art). Staff overlay by `equipmentTier(itemLevel)` (10 tiers).
- Overlay chosen by `templateId` (sword..bigSword); +0..6 base art, +7..15 enhanced art.
- Glow layers baked once per (sprite, band) -- no per-frame shadowBlur, no foggy halos.
- Icons/attack button keep tier art; at +7+ get the same baked glow (no art swap).

## Phases
| # | Phase | Track | Effort | Blocked by | Status |
|---|-------|-------|--------|-----------|--------|
| 1 | [Enhance data + stats](phase-01-enhance-data-and-stats.md) | A | 3h | - | completed |
| 2 | [Upgrade stone, drops, method](phase-02-upgrade-stone-drops-and-method.md) | A | 4h | 1 | completed |
| 3 | [Enhance UI in inventory](phase-03-enhance-ui-inventory.md) | A | 4h | 2 | completed |
| 4 | [Weapon overlay cutter tool](phase-04-weapon-overlay-cutter-tool.md) | B | 3h | - | implemented (review pending) |
| 5 | [Weaponless hero sheets](phase-05-weaponless-hero-sheets.md) | B | 4h + user art | - | wizard done (warrior deferred) |
| 6 | [Hand anchor estimation](phase-06-hand-anchor-estimation.md) | B | 4h | 4, 5 | wizard done (warrior deferred) |
| 7 | [Held-weapon renderer + glow bake](phase-07-held-weapon-renderer-and-glow.md) | B | 5h | 1, 4, 6 | wizard done (warrior deferred) |
| 8 | [Icons, badges, portrait](phase-08-enhanced-icons-badges-portrait.md) | A+B | 2h | 3, 7 | pending |
| 9 | [Browser checks, perf, docs](phase-09-browser-checks-perf-docs.md) | - | 1h+ | 8 | pending |

Parallel: {1->2->3} || {4, 5 (user art)} -> 6 -> 7 -> 8 -> 9. Phases 4 and 5 touch only tools/assets.

## Data flow (summary)
Monster death -> `rollDrops` -> ItemDrop(upgradeStone) -> pickup -> `player.materials.upgradeStone`
-> Inventory "Enhance" -> `attemptEnhance(player, item, {method, roll})` -> `item.enhance++` | stone lost
-> `recomputeStats` (effectiveItemStats) -> save (`sanitizeItem` clamps enhance, `materials` validated)
-> renderer: `enhanceVisualBand(item)` -> overlay art (base/enhanced) + baked glow -> drawn at frame anchor.

## File ownership (no overlaps between parallel phases)
- P1: `equipment-system.js`, `inventory-layout.js`, new `weapon-enhance-*`
- P2: `player.js`, `save-serializer.js`, `loot-system.js`, `item-drop.js`, `monster-definitions.js`, `world-update.js`, `potion-renderer.js`
- P3: `inventory-panel.js` + new `ui/inventory-enhance-*.js`
- P4/P5/P6: `tools/*.py`, `assets/custom/weapon-overlays/`, `assets/hd/hero/weaponless/`, generated `src/data/*-generated.js`
- P7: `player-renderer.js`, `sprite-registry.js`, `asset-atlas.js`, new `held-weapon-*.js`, `weapon-glow-baker.js`
- P8: `weapon-icons.js`, `hud-renderer.js`, `activity-notice.js`, `inventory-panel.js` + `inventory-enhance-actions.js` (sequential after P3)

## Success (overall)
- `npm test` green (197 existing + new); new browser checks pass with 0 page errors.
- Combat soak (tools/check-combat-soak.mjs) with +15 warrior: >=59 fps, p99 <=17ms (baseline 59.9/16.8).
- Old saves load unchanged (enhance 0, stones 0).

## Unresolved questions
See bottom of each phase; consolidated list in the planner's final report.
