# Phase 02 - Combat stat hooks in Skills/ (non-netcode files)

## Context links
- `Assets/Scripts/SkillDamage.cs` (Radial: crit x1.6 inline), `SkillDamage.Area.cs:26,85` (`CritMul = 1.6f`, crit roll),
  `Skills/SkillSupport.cs:32-60` (Buff: Shield = % caster ATK; Heal/HealOne), `Skills/SkillMinion.cs` (mechanic drone pulse: Pct, IntervalMs),
  `Skills/HeroBuffs.cs` (Absorb), `Skills/HeroSkills.cs` (created by skill p04: TryCast MP + cast lock), `Rpg/Skills/SkillEnums.cs` (BuffKind), `Rpg/Skills/BuffState.cs`

## Overview
P2 | after 01 | All runtime use of `PlayerState.Second` goes through ONE static: `SecondWeaponHooks`. Authority-only for damage/heal/shield;
lock + MP cost run on both owner client and server (prediction parity).

## Key insights
- Crit x1.6 is duplicated in 4 places; ranged combo bolts (`ComboProjectile`) are off-limits and assassin never uses them -> crit damage
  only needs melee (`HeroController.Combat`, phase 03) + skill damage paths. Crit RATE already flows everywhere via `CritBonus`.
- Phase 04 `NextCritPct` buff needs the same roll site -> one `HeroCrit.Roll` helper serves both stat and combo (DRY).
- Robot companion (`HeroCompanion`) is view-only; the only robot that deals damage today is the Robot Assault drone (`SkillMinion`, pulse).
- Cast lock + MP cost must use the same formula on owner client and server, else OwnerSim replay mismatches -> pure function on `PlayerState`.

## Requirements
- `HeroCrit.Roll(HeroController h, float chance, bool damageOverTime)` -> `(bool crit, float mul)`: mul = 1.6 + Second.CritDmg; consumes
  NextCritPct charge (not for DoT).
- `SecondWeaponHooks`: `Incoming(hero, dmg)` = round(dmg*(1-DmgReduction)) then `HeroBuffs.Absorb`; `LockMs(PlayerState, SkillDef)`,
  `MpCost(PlayerState, SkillDef)` (min 1); `HealMul(hero)`; `ShieldMul(hero)`; `RobotDmgMul(hero)`; `RobotIntervalMs(hero, baseMs)` (min 100).

## Architecture / files
Create
- `Assets/Scripts/Skills/SecondWeaponHooks.cs` (<90 lines) - reads `hero.Rpg.Second` (null-safe -> neutral values).
- `Assets/Scripts/Skills/HeroCrit.cs` (<50 lines).
Modify (skill-system files, not netcode-owned)
- `SkillDamage.cs` Radial crit -> `HeroCrit.Roll(src, critChance, false)`.
- `SkillDamage.Area.cs:85-86` -> `HeroCrit.Roll(ctx.Hero, ctx.CritChance, false)`; drop `CritMul` const.
- `Skills/SkillSupport.cs` - Heal amount `*= HealMul(caster)`; Buff Shield value `*= ShieldMul(caster)`.
- `Skills/SkillMinion.cs` - pulse damage `*= RobotDmgMul`; strike interval `RobotIntervalMs(fx.IntervalMs)` (mechanic only).
- `Skills/HeroSkills.cs` (p04) - MP spend `SecondWeaponHooks.MpCost`, cast lock `SecondWeaponHooks.LockMs` (incl. legacy Ice Storm/Earth Break locks).
- `Rpg/Skills/SkillEnums.cs` - `BuffKind.NextCritPct` (append at end; enum order serialized nowhere - verify grep).
- `Rpg/Skills/BuffState.cs` - `ConsumeNextCrit()` returns bonus pts (0 if none), spends 1 charge.

## Data flow
```
hit (authority) -> HeroCrit.Roll(chance = base + CritBonus[+Second.CritRate] + NextCritPct) -> mul 1.6 + CritDmg -> Monster.TakeHit
monster -> hero dmg -> [p03 line] SecondWeaponHooks.Incoming -> DR -> HeroBuffs.Absorb -> hp
press skill -> HeroSkills.TryCast -> MpCost / LockMs (owner client + server, same inputs) -> SkillExecutor.Cast
```

## Implementation steps
1. BuffKind + BuffState.ConsumeNextCrit + dotnet test (`BuffStateNextCritTests`).
2. HeroCrit + SecondWeaponHooks.
3. Replace crit sites in SkillDamage*.cs; heal/shield/minion multipliers; HeroSkills MP/lock.
4. Unity compile; offline smoke: assassin +15 crit numbers (DamageNumber crit) ~2.1x; wizard +15 lock visibly shorter.

## Todo
- [ ] BuffKind.NextCritPct + BuffState + test
- [ ] HeroCrit, SecondWeaponHooks
- [ ] SkillDamage / SkillDamage.Area crit sites
- [ ] SkillSupport heal/shield, SkillMinion robot, HeroSkills MP/lock
- [ ] compile + offline smoke

## Success criteria
- Unit: MpCost(+15 spellbook, 28 MP) = 22; LockMs(650) = 488; RobotIntervalMs(1000,+15) = 714; NextCrit consumed once, not by DoT.
- No behaviour change for a +0 offhand except combos (phase 04): recorded combo/skill captures identical at +0.

## Risks
| Risk | LxI | Mitigation |
|---|---|---|
| Skill p04 renamed/merged HeroSkills | M x M | step 0: re-read p04 output; hook names stay, call site moves |
| Lock/MP mismatch client vs server -> rubber-band | M x M | pure function of mirrored PlayerState; owner mirror gets equipment via RpgState |
| `Random.value` crit on server vs client view | L x L | crits only on authority already |

## Security
Server computes everything from its own PlayerState; client sends only button bits.

## Rollback
Revert commit; hooks are additive call wrappers, removing them restores x1.6 + raw costs.

## Next steps
03 wires the 3 netcode-owned sites; 04 uses `HeroCrit` + `BuffKind.NextCritPct`.

## Unresolved questions
- Should cleric `HealPulse` (combo finisher, netcode-owned `HeroController.ClassSkills.cs`) also scale with Healing power? Default: no.
