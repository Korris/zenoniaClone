# Phase 01 - Skill data model, SP / ranks, save + API, level cap 60

## Context links
- Design table: `plans/261030-1508-skill-tree-design/skill-tree-chatgpt.md`
- `Assets/Scripts/Rpg/PlayerState.cs`, `Rpg/SaveSerializer.cs`, `Rpg/Leveling.cs` (MaxLevel = 30 today!), `Rpg/RpgClasses.cs`
- `server-api/src/rpg/item-validation.js` (`stateSchema`, zod strips unknown keys), `server-api/src/rpg/rpg-constants.js` (`MAX_LEVEL = 30`)
- `Tests/dotnet/ZenoniaNet.Tests/ZenoniaNet.Tests.csproj` (pattern for a glob-compiled test project)

## Overview
Priority P1 | Status completed (2026-10-30) | Lane A | Can start NOW (no netcode-owned file).
Pure, engine-free skill table + rank/SP rules + persistence. Everything later reads from here.

## Key insights
- Rpg asmdef references UnityEngine only in SaveSerializer; keep `Rpg/Skills/*.cs` free of `UnityEngine` so a dotnet project can glob-compile them.
- API `stateSchema` is a zod object -> unknown keys are silently DROPPED on save. Must add `skillRanks` / `skillSlots` there or online loses skills.
- `SaveData.version` max is 1 in both C# and zod: add fields additively (JsonUtility defaults missing arrays to empty) -> NO version bump.
- Level cap is 30; design needs 60 (ultimates at 50). Raise in C# + API together.
- SP derived: `SpTotal(level) = level` (1 SP per level, Lv1 = 1 SP), `SpFree = level - sum(ranks)`. Only ranks saved -> no dupe exploits.

## Requirements
Functional
- `SkillDef` per skill: `Id` ("wizard.fireball"), `Class`, `Index` 0-7 (= preview/icon order R1C1..R2C4), `Name`, `Tier` (Basic <20 / Advanced 20-39 / Ultimate 40+, derived from unlock lvl), `UnlockLevel`, `Prereqs` (`SkillReq{Index, Rank}`[]), `Mp` + `MpPerRank`, `CooldownMs` (fixed), `CastLockMs`, `Targeting`, `Effects` (`SkillEffect[]`, see phase 03), `Legacy` (None/EarthBreak/IceStorm), `DamageGrowth` (default 0.12 = +12% of base per rank).
- `SkillRanks`: byte[8] per character; `CanLearn(def, level)` -> `LearnResult` enum (Ok, MaxRank, LevelTooLow, PrereqMissing, NoSp); `Learn`, `Sanitize(level)` (clamp 0..10, drop ranks whose prereqs/level fail, trim newest-tier ranks while sum > level).
- `SkillLoadout`: sbyte[8] = magazine slots 1..8 (page1 slot1 is always Dash, so 3 pages x 3 = Dash + 8) -> skill index or -1; unique; auto-fills a newly learned skill into the first empty slot.
- Save: `SaveData.skillRanks` (List<int>, 8), `SaveData.skillSlots` (List<int>, 8). Capture/Parse/Restore validate via `Sanitize`.
- Lv cap 60: `Leveling.MaxLevel = 60`, API `MAX_LEVEL = 60`.
Non-functional: each file < 200 lines; zero UnityEngine refs in `Rpg/Skills/`.

## Architecture
```
SkillTable.All (static, 40 defs, built from 5 partial class-data files)
   -> SkillRanks.CanLearn/Learn (pure)  -> PlayerState.Skills / .Loadout
   -> SaveSerializer.Capture/Parse/Restore -> save json (offline SaveStore) / API state (online)
API save: zod stateSchema validates shape + sum(ranks) <= level; zone server is the rule authority (phase 04).
```

## Related code files
Create (all `Assets/Scripts/Rpg/Skills/`, namespace `ZenoniaProto.Rpg`):
- `SkillEnums.cs` (SkillTier, SkillTargeting {Facing, NearestEnemy, Self, LowestAlly, AroundSelf}, AreaShape {Single, Circle, Cone, Arc, Line}, EffectKind, StatusKind {Slow, Freeze, Stun, Burn, Poison, Mark, Knockback, Launch, Chill}, BuffKind {AtkPct, AtkSpeedPct, Shield, NextHitsPct}, LegacySkill)
- `SkillDef.cs` (+ `SkillReq`, `MpAt(rank)`, `DamageMul(effect, rank)`, `TierOf(level)`)
- `SkillEffect.cs` (flat effect record + `StatusSpec`, `BuffSpec`; fields listed in phase 03)
- `SkillTable.cs` (registry: `All`, `ForClass(classId)`, `Get(classId, index)`, `ById`) + `SkillTable.Warrior.cs`, `.Wizard.cs`, `.Assassin.cs`, `.Cleric.cs`, `.Mechanic.cs` (partial; data only; ~60-90 lines each)
- `SkillRanks.cs`, `SkillLoadout.cs`, `SkillText.cs` (tooltip lines per rank: "MP 18 -> 20", "Damage 230% -> 258%")
- `Tests/dotnet/ZenoniaSkills.Tests/ZenoniaSkills.Tests.csproj` (`<Compile Include="..\..\..\Assets\Scripts\Rpg\Skills\**\*.cs" />`), `SkillRanksTests.cs`, `SkillTableTests.cs`, `SkillLoadoutTests.cs`
Modify:
- `Rpg/PlayerState.cs` (+`Skills`, `Loadout`, `SkillPointsFree`; ~6 lines)
- `Rpg/SaveSerializer.cs` (2 fields + capture/parse/restore; ~12 lines; if > 200 lines split `SaveSerializer.Skills.cs` partial)
- `Rpg/Leveling.cs` (MaxLevel 60)
- `server-api/src/rpg/rpg-constants.js` (MAX_LEVEL 60, SKILL_COUNT 8, SKILL_MAX_RANK 10), `server-api/src/rpg/item-validation.js` (`skillRanks`, `skillSlots` + superRefine sum <= level), `server-api/test/*` (schema tests), `Assets/Scripts/Net/Api/ApiDtos.cs` NOT needed (state = SaveData).

## Implementation steps
1. Write enums, `SkillReq`, `SkillEffect`, `SkillDef` (no logic beyond MpAt/DamageMul).
2. Transcribe the 40 rows of the design table into the 5 partial data files (rank-1 values; area in tiles; MP/rank from design "MP +1..5": basic +1, advanced +2..3, ultimate +5).
   Ice Storm / Earth Break rows: `Legacy = IceStorm/EarthBreak`, effects empty; MP/CD from the tree (28/12 s, 26/12 s) - flag in balance phase vs today's 20/7.7 s, 29/8 s.
3. `SkillRanks` + `SkillLoadout` + `SkillText`.
4. PlayerState wiring; SaveSerializer fields; old saves (no arrays) -> all ranks 0, loadout empty.
5. Leveling.MaxLevel 60; API constants + schema; run `npm test` in server-api.
6. dotnet tests (see matrix); `dotnet test Tests/dotnet/ZenoniaSkills.Tests`.
7. Unity compile check (batchmode `-quit` compile or editor).

## Todo
- [x] enums / SkillDef / SkillEffect
- [x] 5 class data files (40 skills)
- [x] SkillRanks / SkillLoadout / SkillText
- [x] PlayerState + SaveSerializer
- [x] Lv cap 60 (C# + API)
- [x] API zod schema + tests
- [x] ZenoniaSkills.Tests project + tests green

## Test matrix (dotnet, pure)
| Test | Asserts |
|---|---|
| table integrity | 8 per class, indices 0-7 unique, prereqs point to lower-unlock skills of same class, no cycles, every prereq rank <= 10, unlock 1..60 |
| learn rules | Lv1 can learn skill 0 only; prereq rank gate; no SP; max rank 10; ult needs Lv50 + both prereqs at 5 |
| SP math | free = level - sum; Lv60 cannot max all (80 > 60) |
| sanitize | tampered save (rank 11, missing prereq, sum > level) trimmed deterministically |
| loadout | unique, auto-fill, unlearned index rejected |
| scaling | DamageMul(rank 1) = base, rank 10 = base*(1+9*growth); MpAt monotonic |

## Success criteria
- dotnet tests pass; server-api tests pass; Unity compiles; an old save loads with 0 ranks and Lv clamp unchanged.
- Online save round trip keeps `skillRanks` (API test posts state with ranks and reads them back).

## Risks
| Risk | L x I | Mitigation |
|---|---|---|
| API drops new fields silently | H x H | schema change + round-trip test in this phase |
| Lv 60 breaks EXP/stat balance | M x M | curve untouched (50*L^1.5); monster EXP tuning listed in phase 12 |
| Design numbers transcription errors | M x M | SkillTableTests assert a few known rows (Ice Storm 28 MP, Orbital Lv50) |

## Security
- Ranks never trusted from the client: zone server recomputes via `SkillRanks.Sanitize` on load; API superRefine rejects sum > level.

## Next steps
Phase 03 consumes `SkillTable`; phase 05 consumes `SkillRanks`/`SkillLoadout`/`SkillText`.

## Unresolved questions
1. Rank-up level gate? Default: none beyond unlock level (SP is the only limiter). Alternative: rank r needs `unlock + 2*(r-1)`.
2. New character auto-learns skill 0 rank 1 (spends the Lv1 SP) so L is never empty? Default: yes.
3. Respec: free reset button, gold cost, or none? Default: free reset while < Lv 20, else 1000 G (phase 05 only if approved).
