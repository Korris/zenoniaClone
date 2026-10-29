# Phase 01 — Enhance level data model + ATK bonus

## Context Links
- src/systems/equipment-system.js (generateItem, sanitizeItem, computeEquipmentModifiers, isUpgrade)
- src/ui/inventory-layout.js (describeItemStats, compareToEquipped)
- src/save/save-serializer.js (items always pass through sanitizeItem)
- tests/equipment-system.test.js, tests/save-serializer.test.js

## Overview
Priority P1 (blocks Track A + P7/P8). Status completed. Add `enhance` (0..15) to weapon instances and a
derived ATK bonus. Pure logic, no UI.

## Key Insights
- Weapon base ATK spans ~4 .. ~270 (base 4-15 x rarity 1-2.1 x itemLevel mult up to 8.5). A flat
  bonus would be meaningless late game -> percent of base ATK with a per-level floor.
- Never mutate `item.stats.atk`: keeps sanitize trivial, lets formula change later without save migration.
- Item names are generic ("Fine Sword"); `+N` must be presentation-only (`displayItemName`), never stored in `name`.

## Requirements
- Functional: `item.enhance` int 0..15 only on `slot === 'weapon'`; armor ignores it.
- Effective ATK = base + `enhanceAtkBonus(baseAtk, level)`; used by player stats, stat lines, compare, isUpgrade.
- Non-functional: save-compatible (missing/garbage => 0, clamp 0..15); pure, unit-testable.

## Architecture
Proposed ATK table (cumulative % of base ATK, MU-style accelerating bands):

| Level | +1..+6 | +7..+9 | +10..+12 | +13..+15 |
|---|---|---|---|---|
| per level | +5% | +8% | +12% | +18% |
| cumulative at band end | 30% | 54% | 90% | 144% |

`bonus = max(level, round(baseAtk * ENHANCE_ATK_PCT[level]))` (floor of +1 ATK per level so weak weapons still grow).
Example: base 40 -> +7: 40*0.38=15; +15: 58.

New files:
- `src/data/weapon-enhance-config.js` — `MAX_ENHANCE = 15`, `ENHANCE_ATK_PCT` (16 entries, index=level),
  `ENHANCE_VISUAL_BANDS = [{min:7,...},{min:9},{min:11},{min:13}]`, `ENHANCED_ART_MIN_LEVEL = 7`.
- `src/systems/weapon-enhance-stats.js` — `enhanceLevel(item)`, `enhanceAtkBonus(baseAtk, level)`,
  `effectiveItemStats(item)` (returns new object), `displayItemName(item)` (`"Fine Sword +7"`),
  `enhanceVisualBand(item)` (0 none, 1..4 for +7/+9/+11/+13 thresholds).

Data flow: raw save -> `sanitizeItem` (clamp enhance) -> `computeEquipmentModifiers` uses `effectiveItemStats` -> `player.atk`.

## Related Code Files
- Create: `src/data/weapon-enhance-config.js`, `src/systems/weapon-enhance-stats.js`, `tests/weapon-enhance-stats.test.js`
- Modify: `src/systems/equipment-system.js` (generateItem sets `enhance: 0` for weapons; sanitizeItem clamps;
  computeEquipmentModifiers + isUpgrade use effectiveItemStats), `src/ui/inventory-layout.js`
  (describeItemStats -> `ATK 52 (+12)`; compareToEquipped on effective stats)
- Delete: none

## Implementation Steps
1. Write config module with tables; freeze objects.
2. Write stats module (pure helpers). `enhanceLevel` = `Number.isInteger(item?.enhance) ? clamp : 0`, 0 for non-weapons.
3. generateItem: add `enhance: 0` only when slot is weapon (do not consume extra rand() calls).
4. sanitizeItem: `if (weapon) item.enhance = clamp(floor(raw.enhance)||0, 0, MAX_ENHANCE)`.
5. computeEquipmentModifiers / isUpgrade: iterate `effectiveItemStats(item)`.
6. inventory-layout: stat line shows bonus part; compare uses effective values both sides.
7. Run `npm test`.

## Todo List
- [x] config module
- [x] stats helpers
- [x] equipment-system wiring
- [x] inventory-layout lines
- [x] unit tests

## Tests (tests/weapon-enhance-stats.test.js + additions)
- bonus table: level 0 -> 0; monotonic increase; floor >= level; base 40 at +7/+15 exact values.
- sanitizeItem: missing -> 0, 99 -> 15, -3 -> 0, "7" string -> 0, armor with enhance -> field absent.
- computeEquipmentModifiers: +10 weapon raises atkAdd by expected bonus.
- save round-trip keeps enhance (serializeSave -> validateSave).
- displayItemName: +0 unchanged, +7 suffix.
- Regression: existing equipment-system / save-serializer tests unchanged and green (rand call count unchanged).

## Success Criteria
All tests green; old save fixture loads with enhance 0; `player.atk` reflects bonus after recomputeStats.

## Risk Assessment
| Risk | L x I | Mitigation |
|---|---|---|
| Extra rand() in generateItem shifts deterministic tests | M x M | Assign constant 0, no rand |
| Stat double-count if someone writes bonus into stats | L x H | Single source: effectiveItemStats; test asserts stats.atk unchanged after enhance |
| Balance (+144% too strong) | M x L | Table in config; tweak without migration |

## Security Considerations
Save JSON untrusted: clamp + integer check; never trust `name` suffix.

## Rollback
Revert files; stored `enhance` field silently ignored by old sanitizeItem (dropped on next save).

## Next Steps
P2 (method + stones), P7 uses `enhanceVisualBand`.

## Unresolved
- Final ATK % table OK? (proposal above)
