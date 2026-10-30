# Phase 08 - OPTIONAL later: enhance fail pity, farmable stones, PvP power cap

## Context links
- ChatGPT warning in `../261030-1508-skill-tree-design/two-weapon-revision-chatgpt.md` (+405% with fail chance = pay-to-win risk)
- `Rpg/Enhance.cs` (`IUpgradeMethod`, `StoneUpgradeMethod.SuccessByTarget`), `Rpg/LootTable.cs`, `server-api/src/rpg/enhance-logic.js`,
  `item-validation.js` (zod strips unknown keys), online `phase-07-pvp-maps.md` (`MapDef.NormalizeStats`)

## Overview
P3 | NOT core | do only after 07 and real play data. Each sub-item is independent (A/B/C), ship separately.

## A. Fail pity (both weapons)
- `Item.failStreak` (int, 0..10): +1 on fail, reset on success; chance = base + 5 pts x streak (cap 100%). Engine-free in `StoneUpgradeMethod`.
- Save shape change -> add `failStreak` to zod `itemSchema` (default 0) + `attemptEnhance` mirror + tamper rule (streak may only rise via API).
  Old saves default 0 -> no migration step. Keep `SaveVersion` 1 (additive field).
- Tests: C# + JS parity table for streak 0..10.

## B. Farmable stones
- Raise `LootTable` stone weight for elites/boss (+ guaranteed 1-3 from boss), later daily quest reward (quest system exists: QuestTracker).
  Tune with drop telemetry; one constant file each side.

## C. PvP power cap
- On PvP maps (`MapDef.NormalizeStats`, online p07): effective enhance = min(level, 10) for main AND second weapon, applied in
  `EnhanceStats.Level`-consumers via `PlayerState.EnhanceCap` (set by zone on map enter). Server-only effect; display shows "(capped +10)".

## Requirements / success criteria
- A: 1000-roll simulation: expected stones to +15 drops >= 30%; parity C#/JS exact.
- B: median stones/hour at Lv30 map measured before/after.
- C: PvP duel +15 vs +10 damage ratio == 1.0.

## Risks
| Risk | LxI | Mitigation |
|---|---|---|
| Schema change desync API vs Unity | M x H | additive field, defaults, parity tests first |
| Economy inflation from pity + farmable together | M x M | ship A or B alone, measure |

## Security
Streak stored server-side (API authoritative); clients cannot reset or raise it; tamper check extended.

## Rollback
Feature flags (constants) per sub-item; additive save field is harmless if unused.

## Unresolved questions
- Pity per item or per character? (per item proposed)
- PvP cap level 10 or full normalization?
