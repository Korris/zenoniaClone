# Phase 01: Shared rules (rotation, reward table, claim in PlayerState)

## Context
- `Assets/Scripts/MapSpawnTable.cs` `BossFor`: only 3 bosses. `frostfangKing` on `frostHollow` (ItemLevel 3), `glacierWarden` on `frostfangDen` (20), `mirefangHydra` on `sunkenReliquary` (35). Online a boss respawns 30 min after its death (`World/BossRespawnTimer.cs`, `Zone.cs:92`).
- `Rpg/LootTable.cs`: boss gold drops are 20-50 / 133-273 / 233-473, each plus 2 stones. Potion prices are 15/60/180 (`PotionCatalog`).
- Pattern to copy: `Rpg/PlayerState.Quests.cs` (reward through `ApplyPickup`, "Success has no text") and `SaveData.quests` (saved, reaches the client in RpgState).
- `ZenoniaSkills.Tests` compiles `Assets/Scripts/Rpg/**`, so everything here is unit-testable without Unity.

## Overview
Priority P2. Pure C# with no Unity types. Offline behaviour does not change (nothing calls the claim offline).

## Requirements
- `WantedBoss.DayKey(long utcMs)` = `floor((utcMs + ResetShiftMs) / 86_400_000)` with `ResetShiftMs = +7h - 5h = 2h` (day turns at 05:00 UTC+7).
- `WantedBoss.For(int dayKey)` returns the rotation entry `Rotation[dayKey % Rotation.Length]`.
- `WantedBoss.Override` (string monster type, null by default): set only by a zone launch arg, for tests.
- `WantedBoss.Today(long utcMs)` returns the override entry if one is set, otherwise the rotation entry.
- `WantedBoss.NextResetUtcMs(long utcMs)` is for display only.
- Reward table (data, one line per boss):

| BossType | Map id | Boss Lv | Gold | Stones | HP pots | MP pots | Potion size |
|---|---|---|---|---|---|---|---|
| frostfangKing | frostHollow | 3 | 150 | 3 | 3 | 3 | 0 Small |
| glacierWarden | frostfangDen | 20 | 600 | 5 | 3 | 3 | 1 Medium |
| mirefangHydra | sunkenReliquary | 35 | 1000 | 8 | 3 | 3 | 2 Large |

  Gold is about 2-3x the boss's own drop, once per character per day. Most of the value is stones, which enhance burns even on a failed attempt and which trade cannot move (trade moves only bag items and gold). Potions get used up. So the reward pushes value into sinks rather than into raw gold.
- `PlayerState.WantedDay` (int). `ClaimWanted(int dayKey, WantedBossDef def)`:
  - Returns `Fail("Already claimed today")` when `WantedDay >= dayKey`, and changes nothing.
  - Otherwise it adds the gold (`ApplyPickup` Coin), the stones (`ApplyPickup` UpgradeStone, already capped at MaxStones) and the sized potions (`HpPotions`/`HpPotionsMedium`/`HpPotionsLarge`, same for MP), sets `WantedDay = dayKey` and returns Success with the reward text ("+600 G, +5 Upgrade Stones, +3 HP / +3 MP Medium Potions").
- `SaveData.wantedDay` (int, missing in old saves = 0). `SaveSerializer.Capture` / `Restore` copy it. SaveVersion does not change, because a missing field already reads as 0.

## Architecture
`WantedBoss` (static, data + math) -> `WantedBossDef` (BossType, BossName, MapId, BossLevel, Gold, Stones, HpPotions, MpPotions, PotionSize). The display map name comes from `MapRegistry.Get(MapId).Name` on the client (Unity side), not from here.

## Related code files
- Create: `Assets/Scripts/Rpg/WantedBoss.cs` (~90 lines: def class, table, rotation, day math).
- Create: `Assets/Scripts/Rpg/PlayerState.Wanted.cs` (~40 lines, partial).
- Modify: `Assets/Scripts/Rpg/SaveSerializer.cs` (field in SaveData, 1 line in Capture, 1 in Restore; 132 lines now, stays under 140).
- Create: `Tests/dotnet/ZenoniaSkills.Tests/WantedBossTests.cs`.
- Unity `.meta` files for the new `.cs` files are generated when Unity opens the project. Do not hand-write GUIDs. Commit the generated ones.

## Implementation steps
1. Write `WantedBoss.cs`: constants `ResetShiftMs`, `DayMs`, the `Rotation` array (same order as the table) and `Find(bossType)`.
2. Write `PlayerState.Wanted.cs` with `WantedDay` and `ClaimWanted`, plus a `RewardText(def)` helper that the client board also uses (DRY).
3. Add `wantedDay` to SaveData, Capture and Restore.
4. Tests:
   - DayKey at 21:59:59.999Z vs 22:00:00Z (key +1).
   - The rotation covers all 3 entries over 3 consecutive days.
   - Override wins over the rotation.
   - Claim adds the exact amounts; a second claim the same day fails and changes nothing; the next day succeeds.
   - Stones cap at 9999.
   - SaveSerializer round trip keeps `wantedDay`; an old JSON without the field gives 0.
5. `dotnet test Tests/dotnet/ZenoniaSkills.Tests`.

## Todo
- [ ] WantedBoss.cs
- [ ] PlayerState.Wanted.cs
- [ ] SaveData / SaveSerializer field
- [ ] WantedBossTests.cs green, full ZenoniaSkills suite green

## Success criteria
- `dotnet test Tests/dotnet/ZenoniaSkills.Tests` passes, including the new tests.
- No skill number or SkillTable change. `git diff` touches no `Skills/` file.

## Risks
| Risk | L x I | Mitigation |
|---|---|---|
| Day math off by the timezone shift | M x H | Boundary tests with fixed UTC instants. A single constant holds the shift. |
| Hidden potion cap or int overflow on gold | L x M | Gold is int with no cap in C#; the API caps at MAX_INT (2e9). Reward totals are tiny. |
| JsonUtility default for a missing int | L x L | The round-trip test covers an old JSON without the field. |

## Security
Pure data. The claim is only reachable from the authority (phase 03).

## Next
Phase 03 and 04 consume `WantedBoss` and `PlayerState.ClaimWanted`.
