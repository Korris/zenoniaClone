---
title: "Second-weapon enhance + two-weapon combos (hybrid design)"
description: "Each class's offhand becomes a +0..+15 specialised-stat item (shield DEF/HP/DR, spellbook MP/cast/MP cost, drone robot dmg/rate, off-hand crit, spirit heal/shield) plus 1 signature combo per class on existing skills."
status: pending
priority: P2
effort: 36h
branch: unity
tags: [items, enhance, combat, skills, ui, server-api, unity]
created: 2026-10-30
---

# Second-weapon enhance + two-weapon combos

Design (approved): [two-weapon-revision-chatgpt.md](../261030-1508-skill-tree-design/two-weapon-revision-chatgpt.md) "HYBRID" section.
Main weapon + `EnhanceConfig.AtkPct` (+15 = +405%) untouched. Offhand slot, 5 class templates, starter offhand, save field, enhance
roll, API endpoint, appearance nibble ALREADY exist -> this plan re-purposes `Slots.Offhand` instead of adding a new slot.

## Phases

| # | Phase | Effort | Depends on | Status |
|---|---|---|---|---|
| 01 | [Second-weapon data + stat core (engine-free)](phase-01-second-weapon-data-stat-core.md) | 5h | skill p04 merged | done (2026-10-30) |
| 02 | [Combat stat hooks in Skills/ (crit, DR, cast, MP, heal, shield, robot)](phase-02-combat-stat-hooks-skills.md) | 5h | 01 | pending |
| 03 | [Hero touch points (netcode-owned files, 3 lines)](phase-03-hero-netcode-touch-points.md) | 2h | 02 | pending |
| 04 | [Signature two-weapon combos](phase-04-signature-two-weapon-combos.md) | 6h | 02, 03 | pending |
| 05 | [UI (bag/loadout/enhance preview) + second-weapon glow](phase-05-ui-enhance-preview-glow.md) | 5h | 01 | pending |
| 06 | [server-api parity + online validation](phase-06-server-api-parity-online-validation.md) | 3h | 01 (04 for online run) | pending |
| 07 | [Tests, balance pass, APK milestone, docs](phase-07-tests-balance-apk-docs.md) | 4h | 01-06 | pending |
| 08 | [OPTIONAL later: fail pity, farmable stones, PvP power cap](phase-08-optional-pity-stones-pvp-cap.md) | 6h | 07 + online p07 | pending |

Parallel lanes after 01: A = 02 -> 03 -> 04 ; B = 05 ; C = 06. File ownership disjoint (see each phase "Related code files").

## Data flow
```
Item(offhand).enhance --PlayerState.RecomputeStats--> SecondWeaponStats (cached on PlayerState.Second)
   flat: DEF%/HP%/MP% folded into Def/MaxHp/MaxMp ; CritRate into CritBonus
   runtime: SecondWeaponHooks.{Incoming, CritMul, LockMs, MpCost, HealMul, ShieldMul, RobotDmg, RobotInterval}
   combos: SecondWeaponCombos (authority only) <- EarthBreak/IceStorm/SkillRun/SkillSupport call sites
enhance: UI -> [offline] EnhanceSystem.Attempt ; [online] InvOp.Enhance -> zone PlayerRpg (-> API /enhance after online p03) -> RpgState mirror
```

## Coordination gate (hard)
Starts after skill plan phase 04 (`plans/261030-1521-skill-trees-implementation/phase-04-hero-netcode-integration.md`) and the online
netcode agent are merged. Only phase 03 edits netcode-owned files: `HeroController.Combat.cs` (2 sites) and `HeroController.SimState.cs`
(1 site). `ComboSystem.cs`, `ComboProjectile*.cs`, `Net/**` are NOT touched (existing InvOp.Enhance / Appearance nibble already carry offhands).

## Backwards compatibility
SaveData shape unchanged (SaveVersion stays 1, zod schema unchanged). Existing chars already own `starter-offhand` (EquipStarterGear on
NewGame/Restore). Old +N offhands keep N; meaning changes from "50% of one base stat" to the specialised table (net buff for every class).

## Rollback
One commit set per phase; no data migration -> revert any phase, saves stay loadable. Phase 01 revert restores `OffhandScale = 0.5`.

## Success criteria (global)
dotnet `ZenoniaSkills.Tests` + `ZenoniaNet.Tests` green; `npm test` green; FrostSmokeTest `CheckSecondWeapon` green for 5 classes;
host + 2 clients: offhand enhance + all 5 combos visible, damage once; APK 60 fps on target phone.

## Unresolved questions
See phase files; top: mechanic robot companion stays passive (drone stats only feed Robot Assault + combo volley)? phases 01/05 could start
before skill p04 (file-disjoint) - allowed? spellbook art missing (icon only).
