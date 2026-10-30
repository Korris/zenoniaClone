# Phase 07 - Tests, balance pass, APK milestone, docs

## Context links
- `Assets/Editor/FrostSmokeTest.cs` (headless checks; skill p04 adds `CheckSkills`), `Tests/dotnet/ZenoniaSkills.Tests`, `Tests/dotnet/ZenoniaNet.Tests`,
  `server-api/test`, `Tools/build-from-copy.ps1` (APK), `Tools/net-local-test.ps1`, docs `./docs/*`

## Overview
P2 | after 01-06 | Close the feature: full test matrix, one balance pass with numbers, APK on phone, docs/changelog.

## Test matrix
| Level | What | Where |
|---|---|---|
| Unit (dotnet) | curve, per-class table, RecomputeStats %, crit add, unequip, combo math, BuffState NextCrit, MpCost/LockMs | `ZenoniaSkills.Tests/SecondWeaponTests.cs`, `BuffStateNextCritTests.cs` |
| Unit (vitest) | offhand enhance + parity + tamper | `server-api/test/enhance-*.test.js` |
| Integration (Unity headless) | `CheckSecondWeapon`: per class +0/+15 offhand, cast combo skill on dummies, assert shield/chill/projectile count/crit/heal shield; enhance offhand offline via EnhanceSystem with seeded rand | `FrostSmokeTest.cs` (+ new `FrostSmokeTest.SecondWeapon.cs` partial if > 200 lines) |
| Online E2E | host + 2 clients, client enhances offhand, combos visible remote, damage once, corrections within baseline | `Tools/net-local-test.ps1` |
| Device | APK, 10-monster fight with all combos | phone |

## Balance pass (targets, adjust caps in `SecondWeapon.cs` only)
- Warrior +15 shield: effective HP vs +0 <= +55% (DEF 30% + HP 20% + DR 10%). Wizard +15: casts/min +25-30%, never MP-free.
- Assassin +15: crit DPS vs +0 <= +45%. Mechanic +15: Robot Assault DPS <= +2.2x of +0. Cleric +15: heal + shield throughput <= +80%.
- Compare against main weapon +15 (+405% ATK): second weapon at +15 must stay well below main weapon value (design intent).

## Implementation steps
1. Write/extend tests; run dotnet + npm + FrostSmokeTest.
2. Balance sheet (spreadsheet-free: dotnet test printing a table) -> tweak caps.
3. APK: `Tools/build-from-copy.ps1`; install; profile 60 fps; quick 5-class play.
4. Docs: `docs/project-changelog.md` (offhand meaning change), `docs/system-architecture.md` (SecondWeapon/Hooks/Combos), `docs/development-roadmap.md`.
5. Code review (code-reviewer agent).

## Todo
- [ ] tests all green (dotnet, npm, FrostSmokeTest)
- [ ] balance table + caps tuned
- [ ] APK on phone 60 fps
- [ ] docs + changelog
- [ ] code review

## Success criteria
All suites green; APK installed and played per class; changelog entry notes "+N offhands now give specialised stats".

## Risks
| Risk | LxI | Mitigation |
|---|---|---|
| FrostSmokeTest flaky with Random crit | M x L | seed / force crit chance 0 or 1 in test |
| Mobile perf from companion glow | M x M | phase 05 static bake; toggle via FeelConfig.FxOn |

## Security
No new surface.

## Rollback
Per-phase reverts; caps-only tuning is a 1-file revert.

## Next steps
08 optional economy/PvP.
