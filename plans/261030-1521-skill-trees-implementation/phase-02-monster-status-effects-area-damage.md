# Phase 02 - Monster status effects + area damage

## Context links
- `Assets/Scripts/Monster.cs` (`TakeHit`, knockback, boss = no knockback), `Monster.Airborne.cs` (Launch, Chill, `SpeedScale`), `Monster.Ai.cs` (uses SpeedScale)
- `Assets/Scripts/SkillDamage.cs` (`Radial` = only area helper today, authority-gated)
- `IceStormUltimate.cs:106` (`Chill`), `EarthBreakUltimate.cs:98` (`Launch`) - must keep working unchanged

## Overview
Priority P1 | Lane B | Starts once phase 01 step 1 is committed (`SkillEnums.cs`, `SkillEffect.cs` with `StatusSpec`; ~1 h), then runs parallel to 01. Status system on monsters (sim side only) + shape-based area damage used by every skill.
Replication of statuses to clients is done in phase 04 (it needs `Net/**` + `Monster.Net.cs`).

## Key insights
- Airborne/Chill already pause AI / slow movement; generalise into one `MonsterStatus` holder instead of more fields on Monster.
- Boss already ignores knockback/launch; formalise immunities in one place.
- `Monster.TakeHit` already receives `src` hero -> single hook point for mark bonus, Death Mark charges, Overcharge energy (no edit in hero combat files).
- Geometry must be pure (floats) so dotnet can test cone/line/arc edge cases.

## Requirements
Status kinds (value, duration from `StatusSpec`):
| Kind | Effect | Boss |
|---|---|---|
| Slow / Chill | move speed x (1 - value) | value halved |
| Freeze | AI + movement paused, blue tint | immune (converted to 50% slow) |
| Stun | AI + movement paused, stars FX | immune (0.3 s micro-stun max) |
| Burn / Poison | DoT: value % of caster ATK per second, ticks every 500 ms, credited to caster | yes |
| Mark | incoming damage x (1 + value) | yes |
| Knockback / Launch | existing TakeHit knock / Launch() | immune (existing) |
Refresh rule: same kind -> max(duration), max(value); DoTs from different heroes stack per source (cap 3 sources).

## Architecture
```
SkillDamage.Area(ctx, effect, center, dir)            (authority only)
  for m in Monster.All: SkillGeometry.Hits(shape, center, dir, size, m.Box) ?
     mult = effect pct x rank x buffs x conditional(m.Status)  -> m.TakeHit(...)
     m.Status.Apply(effect.Status, src)
Monster.Update -> Status.Tick(dtMs): DoT ticks -> TakeHit(quiet=true, src) ; timers
Monster.TakeHit -> dmg = SkillHitHooks.Modify(src, this, dmg)   (mark, Death Mark charge, Overcharge energy)
Monster.Ai -> Status.Disabled ? hold : speed x Status.SpeedFactor
```

## Related code files
Create
- `Assets/Scripts/Rpg/Skills/SkillGeometry.cs` (pure: `InCircle`, `InCone`, `InArc`, `InLine` (oriented rect), nearest-point-on-box tests; float args only)
- `Assets/Scripts/MonsterStatus.cs` (plain class: timers, DoT list, `Apply(StatusSpec, HeroController src, bool boss)`, `Tick`, `SpeedFactor`, `Disabled`, `IncomingMul`, `Has(StatusKind)`, `DominantCode` for replication)
- `Assets/Scripts/Monster.Status.cs` (partial: owns a `MonsterStatus`, `ApplyStatus`, tint/overlay hook for Monster.Visuals; FX only if `NetMode.RendersFx`)
- `Assets/Scripts/SkillHitHooks.cs` (static `Modify(src, target, dmg)`; phase 03 fills hero-side buff/charge calls; here: mark multiplier only)
- `Tests/dotnet/ZenoniaSkills.Tests/SkillGeometryTests.cs`, `MonsterStatusRulesTests.cs` (status refresh/immunity rules live in a pure `StatusRules.cs` under `Rpg/Skills/`)
- `Assets/Scripts/Rpg/Skills/StatusRules.cs` (pure: refresh/stack/boss conversion math)
Modify
- `Monster.cs` (`TakeHit`: 1 line hook; Update: 1 line `status.Tick`) - stays < 230 lines (already 226: move `HitFx` to `Monster.Visuals.cs` to get back under 200)
- `Monster.Airborne.cs` (`Chill` -> `status.Apply(Chill)`, `SpeedScale` -> `status.SpeedFactor`; public API unchanged)
- `Monster.Ai.cs` (skip AI while `status.Disabled`; 2 lines)
- `Monster.Visuals.cs` (tint priority: freeze blue > burn orange > poison green > chill; mark reticle child sprite)
- `SkillDamage.cs` (keep `Radial` for legacy ultimates; add `Area(...)`, `Single(...)`; split into `SkillDamage.Area.cs` if > 200)

## Implementation steps
1. `SkillGeometry` + `StatusRules` (pure) + dotnet tests.
2. `MonsterStatus` + `Monster.Status.cs`; migrate chill; keep `Launch` as is.
3. Hooks in Monster.cs / Monster.Ai.cs; move HitFx to Visuals to stay < 200 lines.
4. `SkillDamage.Area` with crit (hero crit chance + CritBonus), knockback, status, conditional bonus (`BonusVsMask`, `BonusPct`, `BehindBonusPct`).
5. FrostSmokeTest scratch check: apply each status to a dummy, assert speed/disabled/DoT damage; boss immunity.

## Todo
- [ ] SkillGeometry + tests
- [ ] StatusRules + tests
- [ ] MonsterStatus + Monster.Status.cs
- [ ] Monster hooks (TakeHit, Ai, Update), file size back < 200
- [ ] SkillDamage.Area / Single
- [ ] visuals (tints, mark reticle, stun stars) gated by RendersFx
- [ ] Ice Storm chill + Earth Break launch regression check

## Success criteria
- dotnet geometry/status tests green; Ice Storm still slows, Earth Break still launches (record `--record-ultimate wizard|warrior` unchanged look).
- DoT kill credits the caster (EXP/loot ownership via existing `Credit`).

## Risks
| Risk | L x I | Mitigation |
|---|---|---|
| Freeze/stun + monster AI windup desync (attack lands while frozen) | M x M | Disabled cancels windup -> cooldown state |
| DoT spam floods MonsterHit events online | M x M | DoT hits `quiet=true`, 500 ms ticks; numbers aggregated per tick |
| Monster.cs over 200 lines | H x L | move HitFx to Visuals first |

## Security
Authority only: `Apply`/`Tick` damage paths return early on `!NetMode.IsAuthority` (proxy monsters already ignore TakeHit).

## Next steps
Phase 03 uses `SkillDamage.Area`; phase 04 replicates `DominantCode` in MonsterSnap bits.

## Unresolved questions
1. Boss freeze/stun: convert to slow (default) or full immunity?
