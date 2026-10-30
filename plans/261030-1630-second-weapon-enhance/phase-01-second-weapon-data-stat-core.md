# Phase 01 - Second-weapon data + stat core (engine-free)

## Context links
- Design: `../261030-1508-skill-tree-design/two-weapon-revision-chatgpt.md` (HYBRID)
- `Assets/Scripts/Rpg/Enhance.cs` (EnhanceStats.OffhandScale 0.5, StatKey/EnhanceStat, Effective), `Rpg/ItemDefs.cs` (O(...) offhand templates),
  `Rpg/RpgClasses.cs` (OffhandType/OffhandName), `Rpg/PlayerState.cs` (198 lines, RecomputeStats, EquipStarterGear), `Rpg/ItemFactory.cs`
  (CreateStarterItem uid `starter-offhand`, Sanitize clamps enhance for offhand), `Rpg/ItemInfo.cs` (EnhancePreview)

## Overview
P2 | gate: skill p04 merged | Adds the per-class second-weapon stat table and applies flat parts in `RecomputeStats`. No Unity types.

## Key insights
- Offhand slot + templates `shield/spellbook/robot/dagger/spirit` + starter + save + enhance roll exist. Only the stat MEANING changes.
- Today offhand +N = 50% of the main table on one base stat (shield DEF 3 at +15 = +6). Replace: `OffhandScale = 0` (base stats flat,
  never scaled) and enhance level feeds `SecondWeaponStats`. Keeping `Scale/StatKey` signatures keeps `InventoryPanel.Preview` compiling
  until phase 05 rewrites it (no cross-lane edit).
- Same stones + fail table as main weapon: shared stone economy, zero API roll changes, ledger/idempotency reused. Pity is phase 08.
- Template ids NOT renamed (saves reference them). Display names only: `Kite Shield`, `Spellbook`, `Drone`, `Off-hand Dagger`, `Spirit`.

## Curve (one shared, DRY)
Per-level weight 1 (+1..6), 2 (+7..9), 3 (+10..12), 4 (+13..15); `f(L) = cumulative / 33` -> +6 .182, +9 .364, +12 .636, +15 1.0.
Every stat = `cap * f(L)`; combos = `base + (cap - base) * f(L)`. Same "late levels matter" shape as main weapon, hard cap at +15.

| Class / item | Stat (cap at +15) | +3 | +6 | +9 | +12 | +15 |
|---|---|---|---|---|---|---|
| Warrior Kite Shield | DEF +30% | 2.7 | 5.5 | 10.9 | 19.1 | 30 |
| | Max HP +20% | 1.8 | 3.6 | 7.3 | 12.7 | 20 |
| | Damage reduction 10% | 0.9 | 1.8 | 3.6 | 6.4 | 10 |
| Wizard Spellbook | Max MP +30% | 2.7 | 5.5 | 10.9 | 19.1 | 30 |
| | Cast lock -25% | 2.3 | 4.5 | 9.1 | 15.9 | 25 |
| | MP cost -20% | 1.8 | 3.6 | 7.3 | 12.7 | 20 |
| Mechanic Drone | Robot damage +60% | 5.5 | 10.9 | 21.8 | 38.2 | 60 |
| | Robot fire rate +40% (interval / (1+x)) | 3.6 | 7.3 | 14.5 | 25.5 | 40 |
| Assassin Off-hand | Crit rate +10 pts | 0.9 | 1.8 | 3.6 | 6.4 | 10 |
| | Crit damage +50 pts (x1.6 -> x2.1) | 4.5 | 9.1 | 18.2 | 31.8 | 50 |
| Cleric Spirit | Healing power +40% | 3.6 | 7.3 | 14.5 | 25.5 | 40 |
| | Shield strength +40% | 3.6 | 7.3 | 14.5 | 25.5 | 40 |

Combos (+0 base -> +15 cap; active from +0 so every player feels them):
Earth Break >=3 hit: shield 4% -> 12% MaxHP, 3 s | Ice Storm: Chill 1.0 -> 3.0 s | Plasma Barrage: robot volley 3 shards x 55% x (0.4 -> 1.0)
x (1+RobotDmg), +350 ms | Shadow Step: next hit crit +30 -> +80 pts, 3 s, 1 charge | Heal: spirit shield 15% -> 40% of healed amount, 4 s.

## Requirements
- Functional: `SecondWeapon.For(classId, level)` -> `SecondWeaponStats`; PlayerState exposes `Second` (recomputed with stats); DEF/HP/MP %
  applied on (base + equipment) totals; CritRate added to `CritBonus`. Unequipped offhand -> all zero (combos off).
- Non-functional: deterministic, float-free rounding for flat stats (`RpgMath.Round`), every file < 200 lines.

## Architecture / files
Create
- `Assets/Scripts/Rpg/SecondWeaponStats.cs` - struct: DefPct, HpPct, DmgReduction, MpPct, CastSpeed, MpCostCut, RobotDmg, RobotRate,
  CritRate, CritDmg, HealPower, ShieldPower, Level, F (curve value). `Zero`.
- `Assets/Scripts/Rpg/SecondWeapon.cs` - `Curve[16]`, per-class caps table (keyed by `RpgClasses` id), combo base/cap constants
  (`ComboEarthShieldPct`, `ComboChillMs`, `ComboVolleyMul`, `ComboNextCritPts`, `ComboHealShieldPct`) + `Combo(base, cap, level)`; `For(classId, level)`.
- `Assets/Scripts/Rpg/PlayerState.Bag.cs` - move bag/equip/consumable/pickup methods out (PlayerState is 198 lines); class becomes `partial`.
Modify
- `Rpg/PlayerState.cs` - `partial`; `public SecondWeaponStats Second`; RecomputeStats: `Second = SecondWeapon.For(ClassId, EnhanceStats.Level(offhand))`,
  `MaxHp = Round((b.MaxHp+hpAdd)*(1+Second.HpPct))` etc., `CritBonus += Second.CritRate`.
- `Rpg/Enhance.cs` - `OffhandScale = 0` + doc (offhand enhance feeds SecondWeapon, base stats flat).
- `Rpg/ItemDefs.cs`, `Rpg/RpgClasses.cs` - display names only.
- `Rpg/ItemInfo.cs` - `EnhancePreview` offhand branch -> `SecondWeaponText.Preview(item)`; create `Rpg/SecondWeaponText.cs`
  (labels "Crit +3.6% -> +4.5%", one line per stat, used by phase 05 too).
- `Assets/Scripts/DevStartConfig.cs` - `offhandPlus` (-1 = keep) applied in `Apply(p)` for test captures.

## Implementation steps
1. Write `SecondWeapon.cs` + `SecondWeaponStats.cs` (table above verbatim).
2. Split PlayerState (pure move, no logic change) -> compile + run existing dotnet tests (must stay green before step 3).
3. RecomputeStats integration; `OffhandScale = 0`.
4. Names; `SecondWeaponText`; ItemInfo branch; DevStartConfig field.
5. Tests in `Tests/dotnet/ZenoniaSkills.Tests/SecondWeaponTests.cs` (it already compiles `Rpg/**`).

## Todo
- [x] SecondWeapon table + stats struct
- [x] PlayerState partial split (no behaviour change)
- [x] RecomputeStats + OffhandScale 0
- [x] names, SecondWeaponText, ItemInfo, DevStartConfig
- [x] SecondWeaponTests

## Success criteria
- Tests: curve monotonic, f(0)=0, f(15)=1; each class only its own keys non-zero; Lv60 warrior +15 shield -> Def = round(raw*1.3);
  unequip offhand -> Second == Zero and stats revert; crit add for assassin; old save with `starter-offhand` enhance 7 loads and maps to +7 row.
- `dotnet test Tests/dotnet/ZenoniaSkills.Tests` green; Unity compiles (batch `-quit` compile).

## Risks
| Risk | LxI | Mitigation |
|---|---|---|
| HP/MP % on totals inflates potion heal (30% of max) | M x L | accepted; potions scale already with gear |
| Percent DEF stacking with future armor | L x M | caps hard at +15; phase 07 balance pass |
| Split breaks reflection/serialization | L x L | PlayerState not serialized directly (SaveSerializer copies fields) |

## Security
Stats derived only from sanitized items (`ItemFactory.Sanitize` clamps enhance 0..15); nothing new is read from save JSON.

## Rollback
Revert commit: `OffhandScale` back to 0.5, `Second` gone; saves unaffected.

## Next steps
02 (hooks read `Second`), 05 (UI uses `SecondWeaponText`), 06 (JS parity of `OFFHAND_SCALE`).

## Unresolved questions
- Percent vs flat for shield DEF/HP (percent keeps value across levels; flat is easier to read).
- Rename stored `name` of old offhands ("Shield" -> "Kite Shield") in Sanitize, or leave?

## Implementation notes (2026-10-30)
- Added `SecondWeaponStats.Equipped` (combos active at +0 only when worn; `Zero` = unequipped). Combo parameters are methods (`SecondWeapon.ComboEarthShieldPct(level)`, `ComboChillSeconds`, `ComboVolleyMul`, `ComboNextCritPts`, `ComboHealShieldPct`).
- Resolved: shield DEF/HP as percentages; stored offhand names not renamed (new drops/starters use "Kite Shield"/"Drone").
- `PlayerState.Bag.cs` holds bag/equip/starter/consumables. `ItemInfo.EnhancePreview` offhand branch uses `SecondWeaponText`; weapon branch ATK only.
- `dev-start.json` accepts `offhandPlus`. server-api `OFFHAND_SCALE` still 0.5 -> phase 06 (parity).
- Verified: dotnet ZenoniaSkills.Tests 75/75; Windows player build from copy succeeded.
