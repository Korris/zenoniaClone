# Phase 04 - Signature two-weapon combos (existing skills, no new art)

## Context links
- `EarthBreakUltimate.cs:100` (main slam `List<Monster> hit = SkillDamage.Radial(...)`), `IceStormUltimate.cs:108` (per wave hits),
  `Skills/SkillRun.cs` (Begin/Fire/Destroy; steps from `ctx.Def.Effects`), `Skills/SkillSupport.cs` (Heal/HealOne), `Skills/SkillBolt.cs`
  (Launch(ctx, effect)), `Monster.Status.cs` (`Chill(ms)`, StatusRules boss handling), skill tables: Warrior 2 Earth Break, Wizard 2 Ice Storm,
  Mechanic 1 Plasma Barrage (moved from slot 2 when Rocket Barrage replaced Homing Rockets), Assassin 1 Shadow Step, Cleric 0 Heal.
- Numbers: phase 01 table (`SecondWeapon.Combo*`).

## Overview
P2 | after 02 + 03 | One file `Skills/SecondWeaponCombos.cs` owns all 5 combos; call sites are 1-2 lines each.

## Key insights
- `SkillDamage.Radial` returns an empty list off-authority -> legacy combos (Earth Break, Ice Storm) are authority-only for free.
- Ice Storm applies NO slow today (only knockback) -> combo = Chill on hit, base 1 s at +0 rising to 3 s (bosses: StatusRules already soften).
- Plasma Barrage volley must render on remote clients too: append an extra Projectile effect to the run's step list (SkillRun builds steps for
  authority AND view replay) -> damage only on authority, art everywhere, no new event. Volley count fixed (3) so viewers need no level.
- Shadow Step bonus must hit the NEXT attack, not its own strike -> grant on run end (SkillRun destroy), not at cast.
- Combos always on from +0 (base value), only magnitude scales -> no "is it unlocked" state to replicate.

## Requirements
| Combo | Trigger (authority) | Effect |
|---|---|---|
| Warrior Earth Break | main slam hit count >= 3 | `HeroBuffs.For(h).Add(Shield, MaxHp * Combo(4%,12%), 3000)` |
| Wizard Ice Storm | each monster hit by a wave | `m.Chill(Combo(1000,3000))` |
| Mechanic Plasma Barrage | cast (schedule) | extra Projectile: count 3, spread 20, Pct 55 x Combo(0.4,1.0) x RobotDmgMul, AtMs 350 |
| Assassin Shadow Step | run finished | `Add(NextCritPct, Combo(30,80), 3000, charges 1)` |
| Cleric Heal | each HealOne amount > 0 | target `Add(Shield, amount * Combo(15%,40%) * ShieldMul, 4000)` |
Unequipped offhand -> no combo (Second.Level == -1 sentinel / Zero).

## Architecture / files
Create `Assets/Scripts/Skills/SecondWeaponCombos.cs` (<120 lines): `OnEarthBreakSlam(hero, hits)`, `OnIceStormHits(hero, hits)`,
`ExtraEffects(ctx)` (returns null or 1-element array), `OnRunEnd(ctx)`, `OnHealed(ctx, target, amount)`.
Modify
- `EarthBreakUltimate.cs` after line 100: `SecondWeaponCombos.OnEarthBreakSlam(hero, hit.Count)` (1 line).
- `IceStormUltimate.cs` after line 108: `SecondWeaponCombos.OnIceStormHits(hero, hit)` (1 line).
- `Skills/SkillRun.cs` - `effects = Concat(ctx.Def.Effects, SecondWeaponCombos.ExtraEffects(ctx))` used by Build + Fire; on last step
  `if (ctx.Authority) SecondWeaponCombos.OnRunEnd(ctx)` (~5 lines).
- `Skills/SkillSupport.cs` - after HealOne: `if (ctx.Authority) SecondWeaponCombos.OnHealed(ctx, to[i], amount)` (1 line).
- `Skills/HeroCrit.cs` (phase 02) - already consumes NextCritPct.
Feedback (no new art): reuse `ShockRing`/`DamageNumber` text ("Guard!", "Crit ready") + existing shield buff visual if any; else tint flash.

## Data flow
```
legacy ult (authority) -> Radial hits -> SecondWeaponCombos -> HeroBuffs / Monster.Chill -> replicated by existing HeroState/MonsterBits
data skill -> SkillRun.Begin(ctx) -> steps(Def.Effects + Extra) -> SkillEffects.Fire (authority dmg, view art) -> run end -> buff
```

## Implementation steps
1. SecondWeaponCombos with combo math from `SecondWeapon` (engine-free numbers already unit-tested in phase 01).
2. Call sites (5 files, table above).
3. Offline per class at +0 / +15 (DevStartConfig.offhandPlus): verify each trigger; record capture (`DebugCapture.Skills`).
4. Host + client: remote sees Plasma volley; shields/chill only applied once (server).

## Todo
- [ ] SecondWeaponCombos
- [ ] EarthBreak, IceStorm, SkillRun, SkillSupport call sites
- [ ] offline 5-class check +0/+15
- [ ] host/client check

## Success criteria
- FrostSmokeTest `CheckSecondWeapon` (phase 07): per class, scripted dummies: EB on 3 dummies -> ShieldLeft == round(MaxHp*0.12) at +15;
  Ice Storm hit -> Chill ~3000 ms; Plasma -> 9 projectiles; Shadow Step then melee -> crit chance raised once; Heal -> target shield > 0.
- Earth Break on 2 dummies -> no shield.

## Risks
| Risk | LxI | Mitigation |
|---|---|---|
| Shield stacking (EB shield + cleric shield) -> "stronger value wins" rule in BuffState | M x L | accepted (refresh rule, no sum) |
| Plasma extra effect changes step indices used by skill p04 prediction / PlayRemote | M x M | append at END, original indices unchanged |
| Chill on bosses too strong | L x M | StatusRules already scale boss CC |
| Shadow Step bonus wasted by DoT tick | L x L | DoT excluded in HeroCrit |

## Security
All combo effects authority-gated; clients only render.

## Rollback
Revert commit; call sites + 1 file; no data.

## Next steps
05 (in parallel), 07 balance.

## Unresolved questions
- Plasma volley origin: hero muzzle (KISS, chosen) vs robot companion shoulder position (needs offset field in SkillEffect)?
- Earth Break threshold 3 counts dummies/bosses equally - OK?
