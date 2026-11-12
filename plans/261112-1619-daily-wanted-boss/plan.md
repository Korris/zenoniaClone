---
title: "Daily Wanted Boss (Boss Truy Na)"
description: "One existing boss per day is wanted; every hero that takes part in its kill gets one fixed reward per character per day, online only."
status: pending
priority: P2
effort: 11h
branch: unity
tags: [online, event, boss, zone-server, server-api, ui]
created: 2026-11-12
---

# Daily Wanted Boss

Source: `docs/online-events-ideas.md` item #2. No new maps, monsters, combat, art or opcodes.

## Decisions (short)
- **Boss of the day**: pure C# `Rpg/WantedBoss.cs`. `dayKey = floor((utcMs + 2h) / 1 day)`, so the day turns at **05:00 Vietnam time** (22:00 UTC). `boss = Rotation[dayKey % 3]`, rotation = Frostfang King (Frost Hollow, Lv3), Glacier Warden (Frostfang Den, Lv20), Mirefang Hydra (Sunken Reliquary, Lv35): the only 3 bosses (`MapSpawnTable.BossFor`). No shared state is needed. The zone decides, and the client uses the same function only to show the board.
- **Claim storage**: `PlayerState.WantedDay` (int dayKey, 0 = never) saved in `SaveData.wantedDay`, like `quests`. The reward and the flag change in the **same** state, so they are always saved together: a crash loses both or neither. One session lock per character means only one zone can write it. Retries and replays go through the existing rev/AdoptOwnWrite logic. The API **must list the field** in `stateSchema` (zod strips unknown keys). It also keeps `max(old,new)`, so a stale or old zone build can never reopen a claim.
- **Took part** = same set that gets boss EXP: heroes in `Monster.DamageBy` who are alive and in the zone at the kill, plus their party members who are alive in the zone within `PartyExp` range (400 u). No damage threshold in v1.
- **Reward**: fixed per boss (gold + Upgrade Stones + sized potions). No gear, so a full bag cannot block it. See phase 01 table.
- **Client**: no wire change. `wantedDay` rides inside the existing RpgState `SaveJson`. The client watches it (like `QuestTracker.Observe`) and shows a chat line, a notification and a board (Guard Rook in Oakhaven, Leila in Sunspire).
- **Online only**: offline saves are local files and the clock is the player's, so the claim would be cheatable. The goal is to bring players back online. Offline keeps the field at 0 and shows nothing.

## Phases
| # | Phase | Owner files | Effort | Status |
|---|---|---|---|---|
| 01 | [Shared rules: rotation, reward table, claim in PlayerState](phase-01-shared-wanted-rules.md) | `Assets/Scripts/Rpg/*` | 2h | pending |
| 02 | [server-api: persist + clamp wantedDay, tests](phase-02-server-api-wanted-day.md) | `server-api/*` | 1.5h | pending |
| 03 | [Zone server: credit participants on the wanted kill](phase-03-zone-server-kill-credit.md) | `World/*`, `Net/Server/*`, `NetLaunchArgs.cs` | 2.5h | pending |
| 04 | [Client: tracker, chat line, NPC board](phase-04-client-board-and-announce.md) | `Net/Client/ClientRpgMirror.cs`, `TownNpc.cs`, new UI files | 2.5h | pending |
| 05 | [Verification, e2e, docs](phase-05-verification-e2e-docs.md) | `Tools/*`, `DebugCapture.NetWanted.cs`, docs | 2.5h | pending |

## Dependencies
- 01 must come before 03 and 04. 02 can run in parallel with 01 (no shared files). 03 must come before 04 (client text matches the server rule). 05 comes last.
- **Deploy order: server-api (02) first, then the zone, then the client.** A zone on an old API would have `wantedDay` stripped, so every relog would allow a new claim.
- Ship the zone and client together (same build). NetProtocol.Version does not change because the bytes are the same.

## Data flow
Boss dies -> `KillRewards.Resolve` -> `WantedBossCredit.Pay` (wanted boss today?) -> each participant `PlayerRpg.ClaimWanted` -> `PlayerState.ClaimWanted` (adds gold, stones, potions, sets WantedDay) -> NetPlayer: RpgState pushed now + `RequestSave` -> server-api `save` (schema keeps the field, clamp max) -> Mongo `characters.state.wantedDay`. Client: RpgState -> `ClientRpgMirror` -> `WantedBossTracker.Observe` -> chat + notification. NPC board reads `WantedBoss.Today` + `state.WantedDay`.

## Rollback
Each phase reverts on its own. Reverting 03 stops new claims, and the stored `wantedDay` stays harmless. Reverting 02 after 03 has shipped is **not allowed** (double claims); revert 03 first.

## Unresolved questions (recommended default in brackets: work can start on these)
1. Reward sizes? [150 G / 3 stones / 3+3 Small potions; 600 G / 5 / 3+3 Medium; 1000 G / 8 / 3+3 Large]
2. Reset hour? [05:00 UTC+7. Midnight is the busiest hour; one constant changes it]
3. Rotation list? [King, Warden, Hydra in order, 3-day cycle. Each level band sees its boss every 3rd day. Change it with a data array if that feels too sparse]
4. Offline gets it? [No, online only]
5. Damage threshold against tap-and-leave? [None in v1. Being alive in the zone at the kill already filters out most leechers]
6. Limit per account instead of per character (5 heroes = 5 claims)? [Per character as specified. The maximum is 5000 G/account/day on Hydra days]
7. Should the reward need a minimum hero level (a Lv3 alt on King day)? [No: King rewards are small]
