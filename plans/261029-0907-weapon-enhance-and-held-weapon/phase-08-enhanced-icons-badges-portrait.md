# Phase 08 — Enhanced icons, +N badges, attack button, loadout portrait

## Context Links
- src/graphics/weapon-icons.js (`itemSprite`: tier icon by itemLevel+classId, NOT templateId)
- src/ui/hud-renderer.js (`drawAttackButton`, `shadowedWeapon` WeakMap bake)
- src/ui/inventory-panel.js (drawItemIcon, drawLoadout portrait = heroes idle[0]), src/ui/character-menu.js
- src/ui/activity-notice.js, src/graphics/loot-effects.js / effects-renderer.js (ground equipment icon)

## Overview
Priority P2. Status pending. Make enhancement visible outside the hero: icons/attack button glow at +7+, "+N" badge
on slots, portrait in Equipment view holds the weapon.

## Key Insights
- Icons are tier art (itemLevel band, per class) while held overlays are per templateId -> swapping icons to overlay
  art at +7 would change the silhouette the player recognizes. Recommend: keep tier icon, apply the SAME
  `bakeWeaponGlow(icon, color, band)` (DRY; color from template's manifest glow). Works for wizard/mechanic too.
- hud-renderer's `shadowedWeapon` WeakMap keys by sprite -> passing the glow-baked canvas just works (baked once).

## Requirements
- `itemSprite(registry, item, classId, inventory)` returns glow-baked icon when `enhanceVisualBand(item) > 0`.
- "+N" badge (N>0) bottom-right of bag/equipment slots and detail icon: bagText size 12, gold `#ffd36b`, dark stroke.
- Item names in detail, pickup notice, compare use `displayItemName` (P3 covered detail; here notices/tooltips).
- Equipment-view portrait: if held-weapon assets ready, draw bare idle[0] + overlay via the P7 composite helper.
- Attack button: shows glowed icon automatically (via itemSprite); band 4 sparkles NOT drawn in HUD (YAGNI).

## Related Code Files
- Modify: `src/graphics/weapon-icons.js`, `src/ui/inventory-panel.js` (badge + portrait call; keep <200 LOC by
  putting badge drawing in `src/ui/inventory-enhance-actions.js` from P3), `src/ui/activity-notice.js` (name), `src/ui/hud-renderer.js` (only if needed)
- Create: `tests/enhanced-item-icons.test.js`

## Implementation Steps
1. itemSprite: wrap result with glow bake when banded (cache via baker).
2. Badge helper `drawEnhanceBadge(ctx, item, x, y)` in enhance-actions module; call in bag grid, equipment slots, detail.
3. Portrait: call P7 `drawHeroComposite` with idle pick (dir down, index 0) at panel coords.
4. displayItemName in notices.

## Todo List
- [ ] glow icons  - [ ] badges  - [ ] portrait  - [ ] notice names  - [ ] tests

## Tests
- itemSprite: +6 returns raw tier icon; +7 returns cached baked canvas (same object on repeat); armor unaffected.
- Existing weapon-icons.test.js, armor-icons.test.js, character-menu.test.js green.

## Success Criteria
Screenshots (P9) show +0 vs +7 vs +13 icons distinct but same silhouette; badge readable at slot size.

## Risk Assessment
| Risk | L x I | Mitigation |
|---|---|---|
| Glow ring clipped in 54px icon fit | M x L | drawItemIcon fits on baked size (includes pad) |
| Ground-drop icons (groundWeaponIcons) unaffected | L x L | drops are +0 (new items) — fine |

## Security Considerations
None.

## Rollback
Revert files; icons revert to tier art.

## Next Steps
P9.

## Unresolved
- User suggested "item icons may use enhanced art at +7": plan keeps tier icon + glow. Confirm, or switch icons
  to overlay art (per templateId) at +7.
