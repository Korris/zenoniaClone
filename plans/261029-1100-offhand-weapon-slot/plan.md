# Offhand weapon slot (2026-10-29)
Status: implemented (gameplay/data/UI/save only; no art, no sprite rendering changes).

- Data: `offhand` slot appended to EQUIPMENT_SLOTS; OFFHAND_TEMPLATES (shield/dagger/robot/spirit/spellbook) + OFFHAND_BY_CLASS (warrior, assassin, mechanic, buff, wizard) in src/data/item-definitions.js. Each template has `enhanceStat` (def/atk/atk/maxHp/atk).
- Generation: generateItem/rollEquipmentDrop pick the class offhand; items carry classId + enhance. maxMp added to equipment aggregation (mpAdd) and Player.recomputeStats.
- Enhance: weapon-enhance-* generalized via isEnhanceable/enhanceStatKey (same success table, same visual bands); function names unchanged.
- UI: offhand slot at right column row 2 of Equipment view (index 5); enhance button/preview work on offhand; stat label per enhanceStat.
- Save: no version bump; missing offhand loads as null; sanitizeItem clamps enhance/classId.
- Icons: placeholder reuse of existing icons (iconGuard/itemRapier/iconHelmet/iconBoot/iconArmor). Needs painted art later.
- Tests: tests/offhand-slot.test.js; character-menu test covers 6 slots.
