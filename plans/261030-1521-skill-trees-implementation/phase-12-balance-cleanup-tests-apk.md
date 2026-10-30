# Phase 12 - Balance pass, legacy cleanup, full test run, APK #2, docs

## Context links
- All previous phases; `HeroClassDef.cs` legacy fields (`SkillName`, `SkillMp`, `SkillCooldownMs`, `SkillLockMs`, `SkillIcon`), `Hud.cs`, `ClassSelectGui.cs`, `Assets/Editor/FrostSmokeTest.cs`, `DebugCapture*.cs`
- `docs/session-handoff.md`, `README.md` (Controls + classes table mention L skills)

## Overview
Priority P2 | after 08-11. Numbers, removal of the one-skill-per-class remnants, regression and release build.

## Requirements
Balance
- Spreadsheet-free: a dotnet "balance report" test prints per class/rank: damage per MP, damage per second of cooldown, ult vs basic ratio; flags outliers > 1.5x class median.
- Lv cap 60 side effects: monster EXP curve so Lv 50 ultimates are reachable (report time-to-level with current Frost Hollow EXP; tune `KillRewards`/monster EXP or leave as known gap).
- Legacy MP/CD: Ice Storm (tree 28 MP / 12 s vs today 20 / 7.7 s), Earth Break (26 / 12 s vs 29 / 8 s) - apply tree values unless the user prefers today's.
Cleanup
- Remove legacy `HeroClassDef` skill fields and every reference (grep list from 2026-10-30: `ClassSelectGui.cs:69`? (stats only), `Hud.cs:78-82`, `InventoryPanel.cs:258`, `SkillMagazine*.cs`, `FrostSmokeTest.cs:96-112`, `DebugCapture*.cs` `TryCastSkill()`), `HeroFx.ShadowTrail/ShadowStrike/Heal` if unused, `Resources/Sprites/skill-icons/*` only if no longer referenced (dash icons still are).
- README + `docs/session-handoff.md`: controls (U/I/O/L/T), skill tree, debug flags; create `docs/system-architecture.md` skill section only if the docs set is created (today docs/ has only session-handoff.md).
Tests
- dotnet: ZenoniaSkills.Tests + ZenoniaNet.Tests; server-api `npm test`; FrostSmokeTest (`CheckSkills` + existing checks); online lag matrix; recordings of all 40.
- APK #2 on device: 60 fps in a 4-player online fight with ultimates.

## Related code files
Modify: `HeroClassDef.cs`, `Hud.cs`, `InventoryPanel.cs`, `FrostSmokeTest.cs`, `DebugCapture.cs`, `DebugCapture.Boss.cs`, `HeroFx.cs`, `SkillTable.*.cs` (numbers), `README.md`, `docs/session-handoff.md`
Create: `Tests/dotnet/ZenoniaSkills.Tests/SkillBalanceReportTests.cs`

## Implementation steps
1. Balance report + tuning commits per class. 2. Legacy removal (compile after each file). 3. Full test matrix. 4. APK #2 + device run. 5. Docs.

## Todo
- [ ] balance report + tuning  - [ ] legacy fields removed  - [ ] all tests green  - [ ] APK #2 on device  - [ ] README / handoff updated

## Test matrix (whole feature)
| Level | What |
|---|---|
| Unit (dotnet) | table integrity, learn/SP/sanitize, loadout, geometry, status rules, schedule, overcharge, protocol (input index, OwnerSim cds, SelfState) |
| API (vitest) | state schema with skills, sum <= level, Lv 60, round trip |
| Integration (Unity batch) | FrostSmokeTest CheckSkills (40 casts on dummies), statuses, boss immunity, save/reload ranks |
| E2E | host + 2 clients via `net-local-test.ps1` with `--bot skills`; lag matrix; manual phone session; 40 recordings approved |

## Success criteria
All tests green; no `SkillName`/`SkillMp` references; APK 60 fps; user sign-off on balance table.

## Risks
| Risk | L x I | Mitigation |
|---|---|---|
| Removing legacy fields breaks debug tooling | M x L | grep + batch compile + smoke test |
| Lv 60 progression too slow to test ults | H x L | `--learn-all` debug flag; EXP tuning separate decision |

## Security
Final check: server rejects tampered learn/assign (smoke test sends illegal InvOps).

## Next steps
Skills in PvP (MMO plan phase 07): per-skill PvP multipliers - out of scope.

## Unresolved questions
1. Keep today's Ice Storm / Earth Break MP & cooldown, or switch to the tree values?
2. Raise monster EXP for the Lv 60 cap now, or wait for new maps?
