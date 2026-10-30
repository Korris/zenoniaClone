# Phase 03 - Skill executor runtime + composable effects

## Context links
- Phase 01 (`SkillDef`, `SkillEffect`), phase 02 (`SkillDamage.Area`, `MonsterStatus`)
- Patterns to copy: `EarthBreakUltimate.cs` / `IceStormUltimate.cs` (timeline MonoBehaviour; damage on authority, art gated by `NetMode.RendersFx`),
  `HeroController.ClassSkills.cs` (Shadow Step blink + collision check via `FrostHollowMap.RectBlocked`), `VfxSprites.cs` / `VfxAnim.cs`, `ClassSfx.cs`, `GameSfx.cs`
- `World/ZoneHeroes.cs` (`All`, `NearestAlive`) for ally targeting

## Overview
Priority P1 | Lane A | after 01 + 02 | NO netcode-owned file is edited here: hero access goes through a small interface `ISkillBody`
that `HeroController` implements in phase 04. Deliverable compiles standalone; runtime is verified at the end of phase 04.

## Key insights
- 38 skills reduce to 9 effect kinds; each skill = 1-3 effects with time offsets. No per-skill classes (except 2 legacy adapters).
- One `SkillRun` per cast drives the effects by elapsed ms. Same object on offline / server / host (`authority=true`) and on clients
  replaying a `SkillCast` event (`authority=false`, visuals only). Random positions (rain) use a seed both sides derive from the cast event.
- Prediction rule (derived, not hand-set): a skill is **predicted** by the owning client iff no effect moves the hero and targeting is
  Facing/Self/AroundSelf. Motion / NearestEnemy / LowestAlly skills wait for the server's `SkillCast` (same as Shadow Step / Heal today).
- Hit-stop / shake / camera punch only for the local hero and only via `FeelConfig` (hit-stop already offline-only).

## Effect record (`SkillEffect`, flat; unused fields default)
`Kind` {Strike, Projectile, Motion, Buff, Heal, Summon, Rain, Zone} | `AtMs` | `Hits`, `IntervalMs` | `Shape`, `Range`, `Radius`, `Length`, `Width`, `AngleDeg` (tiles/deg)
| `Pct` (% ATK, dmg or heal) | `Status` (`StatusSpec{Kind, Value, Ms, Charges}`) | `Buff` (`BuffSpec{Kind, Value, Ms}`) | `Ally` (targets heroes)
| `Count`, `SpreadDeg`, `Speed`, `Pierce`, `Homing` (projectile) | `DurationMs`, `Motion` {Dash, Blink, Leap}, `MoveTiles`
| `BonusVsMask`, `BonusPct`, `BehindBonusPct` | `Vfx` (row/sub-set key), `Sfx` (clip key)

## Composition of the 40 skills (rank-1 values from the design)
| Class | # | Skill | Effects |
|---|---|---|---|
| Warrior | 0 | Power Slash | Strike Arc r2.5 120deg 170% |
| | 1 | Whirlwind | Strike Circle r2.5, Hits 3 / 250 ms, 100% |
| | 2 | Earth Break | **Legacy** EarthBreakUltimate (rank -> mult) |
| | 3 | Flame Cleave | Strike Arc r3 140deg 300% + Burn |
| | 4 | Charging Strike | Motion Dash 5 tiles 250 ms + Strike Line 5x2 300% (at end) |
| | 5 | Blade Cyclone | Zone attached to caster r2.5, 5 hits x90% over 2.5 s (caster can walk) |
| | 6 | Inferno Crash | Motion Leap to facing point 4 tiles + Strike Circle r3 520% + Burn |
| | 7 | Berserker Awakening | Buff AtkPct 25 + AtkSpeedPct 15, 12 s |
| Wizard | 0 | Arcane Bolt | Projectile 7 tiles 160% |
| | 1 | Fireball | Projectile -> on impact Strike Circle r1.5 230% + Burn 15%/s 4 s |
| | 2 | Ice Storm | **Legacy** IceStormUltimate |
| | 3 | Arcane Lance | Projectile Pierce 8 tiles fast 320% |
| | 4 | Mana Shield | Buff Shield 250% ATK 8 s |
| | 5 | Frost Nova | Strike Circle self r3 280% + Freeze 2 s |
| | 6 | Elemental Burst | Strike Circle at range 4 r2.5 400%, BonusVs Burn/Freeze +25% |
| | 7 | Astral Cataclysm | Rain 8 x120% within r4 at range 5, 200 ms apart |
| Assassin | 0 | Shadow Strike | Strike Arc r1.8 Hits 2 / 120 ms 95% |
| | 1 | Shadow Step | Motion Blink behind NearestEnemy r6 + Strike Single 260%, BehindBonus 30% |
| | 2 | Blade Dance | Strike Circle r2 Hits 4 / 150 ms 85% |
| | 3 | Phantom Rush | Motion Dash x3 (2 tiles each, 180 ms) each + Strike Line 2x1.5 120% |
| | 4 | Venom Daggers | Strike Cone r2.5 60deg 120% + Poison 25%/s 10 s |
| | 5 | Shadow Clone | Summon clone 8 s, strike nearest r1.8 every 600 ms at 50% |
| | 6 | Death Mark | Status Mark on NearestEnemy r6: next 3 hits +50% (Charges 3, 10 s) |
| | 7 | Nightfall Execution | Strike Circle r3 Hits 8 / 120 ms 125% |
| Cleric | 0 | Heal | Heal LowestAlly r6 (self incl.) 220% ATK |
| | 1 | Spirit Blessing | Buff Ally r4 AtkPct 15, 12 s |
| | 2 | Holy Ray | Strike Line 7x1.5 260% (beam) |
| | 3 | Divine Light | Strike Circle at range 3 r2 300% + Heal Ally same circle 100% |
| | 4 | Sanctuary | Zone r3 8 s, Heal Ally 35%/s |
| | 5 | Guardian Spirit | Buff Ally r4 Shield 200% ATK 8 s |
| | 6 | Purification | Strike Circle self r3 350% (+ cleanse = no-op until heroes get debuffs) |
| | 7 | Divine Ascension | Heal Ally r5 450% + Strike Circle r5 550% |
| Mechanic | 0 | Power Shot | Projectile Pierce 7 tiles 150% |
| | 1 | Homing Rockets | Projectile Count 3 Homing (acquire r6) 75% each, small blast r1 |
| | 2 | Plasma Barrage | Projectile Count 6 Spread 60deg range 6, 55% each |
| | 3 | Target Lock | Status Mark +15% taken, up to 5 nearest within r7, 8 s |
| | 4 | Rocket Punch | Projectile Pierce wide (Line 4x2) 280% + Knockback 2 tiles |
| | 5 | Robot Assault | Summon robot drone 8 s, pulse r6 every 1 s at 65% |
| | 6 | EMP Burst | Strike Circle self r3 350% + Stun 1.5 s |
| | 7 | Orbital Cannon | Rain 6 x140% within r4 at range 7, 250 ms apart |

## Architecture
```
HeroSkills.TryCast(i)  checks: learned, cd, mp, not casting/dashing/dead      (phase 04 calls it from HeroController.Skill.cs)
  -> SkillTargeting.Resolve(def, body) -> SkillContext{body, def, rank, origin, dir, aim, target, seed, authority, view}
  -> cd/mp/castLock set; SkillRun.Start(ctx)  (or SkillLegacy for Earth Break / Ice Storm)
SkillRun (MonoBehaviour, per cast) Update: SkillSchedule.Due(elapsed) -> SkillEffects.Fire(ctx, effect, hitIndex)
  Strike     -> SkillDamage.Area / Single                         (authority)   + SkillVfx impact row (view)
  Projectile -> SkillBolt (own MonoBehaviour: move, sweep, pierce/homing, on-impact chained Strike)
  Motion     -> body.TryBlink(dest) / body.BeginForcedMove(dir, units, ms)   (authority + predicting owner)
  Buff/Heal  -> HeroBuffs.Add on targets (self / allies in radius / lowest ally)
  Summon     -> SkillMinion (follows caster, pulse attack)          Rain -> seeded points, Strike each
  Zone       -> SkillGroundZone (fixed or attached, pulse dmg/heal)
Monster.TakeHit -> SkillHitHooks.Modify: mark charges, src HeroBuffs.NextHitsPct, Overcharge energy (phase 11)
```
`ISkillBody` (implemented by HeroController in phase 04): `Feet`, `Facing`, `SetFacing`, `NetId`, `Atk`, `CritChance`, `Palette`, `Class`,
`TryBlink(Vector2)`, `BeginForcedMove(Vector2 dir, float units, float ms)`, `AddInvuln(ms)`, `Heal(float)`, `Buffs`, `IsLocal`, `Transform`.

## Related code files (all new, `Assets/Scripts/Skills/`, namespace `ZenoniaProto`)
- `ISkillBody.cs`, `SkillContext.cs`, `SkillTargeting.cs`, `HeroSkills.cs` (cds[8], cast state, Capture/Restore helpers for OwnerSim), `HeroBuffs.cs` (AtkMul, AtkSpeedMul, shield absorb, NextHits charges)
- `SkillRun.cs`, `SkillEffects.cs` (dispatch switch), `SkillMotion.cs`, `SkillSupport.cs` (buff/heal/ally pick), `SkillBolt.cs`, `SkillMinion.cs`, `SkillRain.cs`, `SkillGroundZone.cs`, `SkillLegacy.cs`
- `SkillVfx.cs` (loads `Resources/Data/skills/<class>-<skill>.json` + sprites via a new `VfxSprites.Load(dataPath, spriteFolder)` overload; plays rows `cast`/`loop`/`impact`; fallback = tinted procedural `BurstFx`/`ShockRing` so code lands before art)
- `SkillSfx.cs` (convention `Audio/sfx/skills/<class>/<skill>-{cast,impact,loop}`; silently skips missing clips; `GameSfx.Play`)
- `Rpg/Skills/SkillSchedule.cs` (pure: which effect/hit is due at t; tested in dotnet) + `SkillScheduleTests.cs`
Modify (not netcode-owned): `EarthBreakUltimate.cs`, `IceStormUltimate.cs` (add `Cast(owner, float multScale)` overload; existing `Cast(owner)` = scale 1; visuals untouched),
`VfxSprites.cs` (+`Load` overload), `SkillDamage.cs` already from phase 02.

## Implementation steps
1. `ISkillBody`, `SkillContext`, `SkillTargeting` (nearest enemy within r, lowest-HP% ally within r, facing aim point clamped by `FrostHollowMap` solids).
2. `HeroSkills` + `HeroBuffs` (pure-ish C#, no MonoBehaviour); `SkillSchedule` + tests.
3. `SkillRun` + `SkillEffects` + Strike path; then Projectile (`SkillBolt`), Motion, Support, Rain, Zone, Minion, Legacy.
4. Rain seed: `seed = hash(casterNetId, skillIndex, quantized aim x/y)` -> `System.Random(seed)`; server and clients get identical points.
5. Concurrency caps: max 3 live `SkillRun`/hero, 2 minions/hero, 4 zones/hero (oldest ends).
6. `SkillVfx` fallback art + `SkillSfx`; preload per class on spawn (like `EarthBreakUltimate.Preload`).
7. Compile check; dotnet tests.

## Todo
- [x] ISkillBody / context / targeting
- [x] HeroSkills (= pure `SkillCooldowns`) / HeroBuffs / SkillSchedule (+tests)
- [x] SkillRun + 8 effect kinds + legacy adapters
- [x] SkillVfx (fallback) + SkillSfx
- [x] caps + preload
- [x] compile + tests green

## Success criteria
Compiles; `SkillScheduleTests` green; every `SkillTable` row maps to supported effect kinds (table test: no unsupported kind/shape combo).

## Risks
| Risk | L x I | Mitigation |
|---|---|---|
| Client projectile visuals diverge from server hits | M x L | clients draw bolts visual-only to first proxy contact/range; damage numbers come from MonsterHit events |
| Server CPU with many casts | L x M | caps per hero; area queries over zone monster list only (tens); no allocations in hot paths (reused lists) |
| Forced motion through walls | M x H | step-wise `FrostHollowMap.Move` sweep, Blink requires free rect (copy Shadow Step check) |

## Security
Targets/aim computed by the authority only; client input = skill index. Damage/heal/status code returns early when `!ctx.Authority`.

## Next steps
Phase 04 implements `ISkillBody` on HeroController and wires input/net.

## Unresolved questions
1. Party restriction for ally skills: until phase 6 (party) of the MMO plan lands, "ally" = any hero in radius. OK?
2. Can heroes move while channeling Blade Cyclone / Sanctuary? Default: Cyclone yes (attached zone, short cast lock), Sanctuary fixed on ground.

## Implementation notes (2026-10-30)
Files: `Assets/Scripts/Skills/` = `ISkillBody`, `HeroSkillBody` (adapter over HeroController public API), `SkillContext` (+`SkillAim`: targeting, wall clamp), `SkillExecutor` (entry `Cast(hero, index, rankOverride, salt)`; legacy Earth Break / Ice Storm adapters live here, no separate SkillLegacy file), `SkillRun`, `SkillEffects` (dispatch, Strike, Rain, Motion), `SkillSupport` (Buff/Heal), `SkillBolt`, `SkillMinion`, `SkillGroundZone`, `SkillMotion`, `HeroBuffs`, `SkillCaps`, `SkillVfx` + `SkillFxAnim`, `SkillSfx`.
Pure (`Rpg/Skills/`): `SkillSchedule`, `SkillScatter` (rain seed / points / fan), `BuffState`, `SkillCooldowns` (the "HeroSkills" cd[8] + Capture/Restore). Tests: `ZenoniaSkills.Tests/SkillScheduleTests.cs` (65 total green, includes a table test: every skill row maps to supported effects).
Other edits: `SkillEffect.Attached` (Blade Cyclone follows the caster), `SkillHitHooks.Modify(src, target, dmg, damageOverTime)` (mark mult + Death Mark charges only for the marker + `NextHitsPct`; DoT never spends charges), `Monster.TakeHit` passes `quietFx`, `EarthBreak/IceStormUltimate.Cast(owner, damageScale)` overloads, `PlayerState.NewGame` starts at full HP/MP (starter gear raised maxima: Cleric started 85/97).
Smoke: `FrostSmokeTest` forces Frost Hollow through `MapRegistry.TestStartMap`; `StatusLogicCheck` no longer clobbers `Zone.Boss` / leaves destroyed dummies in the monster list; new `SkillExecutorCheck` (8 skills per class cast, around-self strike hurts, shield lands). Smoke PASS (all classes, monsters present).
Art: `SkillVfx` follows phase 06 (`Data/skills/<class>-<skill>.json`, `Sprites/skills/<class>/<skill>/{cast,loop,impact}_<n>.png`); missing set / row = tinted ShockRing (cast / impact) or glow orb (projectile) fallback. Verified on Frost Hollow dummies with the sliced wizard art (reports/phase-03-wizard-skills-sheet-*.png).
Decisions: Line strike right after a Dash/Leap sweeps the travelled path (anchored at the motion start); Single strike centres on the locked target; Zone `Attached` for Cyclone, Sanctuary fixed; Summon = mechanic -> area pulse, others -> chase + single strike; projectiles ignore walls (as ComboProjectile); sounds by convention, silent when missing.

### Hooks phase 04 must add (no netcode file touched here)
1. `HeroController.Skill.cs`: replace `TryCastSkill` body by `HeroSkills` logic: learned (`Rpg.Skills[i] > 0`), `SkillCooldowns.Ready(i)`, MP `def.MpAt(rank)`, not casting/dashing/dead, then `SkillExecutor.Cast(this, i)` (returns false = refused target, no cost); start cd `def.CooldownMs`, MP, `castLeft = def.CastLockMs`. Replaying clients call `SkillExecutor.Cast` with `NetMode` non-authority (it already skips damage / heal / motion / buffs) and pass `salt` = the event's tick so rain matches.
2. `HeroController` combat: `AtkStat` x `HeroBuffs.AtkMul(this)`, attack speed x `HeroBuffs.AtkSpeedMul(this)`, `TakeDamage`: `dmg = HeroBuffs.Absorb(this, dmg)` before HP loss (Mana Shield / Guardian Spirit shields).
3. `HeroSkillBody.AddInvuln` is a no-op: expose `invuln` (public method) so Shadow Step's 300 ms i-frames work; `SkillMotion` uses `Teleport` per frame: on a predicting client motion skills must wait for the server (`PredictsSkill` false for Motion / NearestEnemy / LowestAlly).
4. Net: SkillCast event = (caster NetId, skill index, rank, salt); NearestEnemy targets are re-resolved on each client from its proxies (small drift accepted, damage is server side). Replicate `HeroBuffs` shield / buffs (HP bar overlay) if wanted.
5. Spawn: call `SkillExecutor.Preload(classId)` next to `EarthBreakUltimate.Preload()`.
6. Mana Shield / buff auras: only an impact flash today (no persistent aura); add a follow loop in phase 07 if the art needs it.
