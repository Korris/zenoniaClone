# Phase 03: Zone server, credit participants on the wanted kill

## Context
- `World/KillRewards.cs` `Resolve`: builds `Winners` (hit it, alive, in zone, has `Owner`), pays EXP through `PartyKillShare.Pay`, and for a boss calls `Owner.RaiseBossKilled()` per winner.
- `World/PartyKillShare.cs` `PayParty`: the sharing set = top winner + party members alive in the zone who hit it or stand within `PartyExp.InRange` (400 u) of `m.Feet`.
- `World/PlayerRpg.cs` (187 lines): events `BossKilled`, `QuestChanged`, and `Touch()`.
- `Net/Server/NetPlayer.cs` `OnBossKilled`: `RpgDirty = true; LastRpgSentMs = -10000f` pushes RpgState at once. `NetEventBus.Toast` sends owner-only text.
- `Net/Server/ZonePersistence.cs` `Attach`: `p.Rpg.BossKilled += () => RequestSave(p)`.
- The boss gate is per hero (`NetPlayer.Bosses`) and is not touched here. The boss still respawns 30 min after death online, so a missed kill can be retried the same day.

## Overview
Priority P2. Authority only: guarded by `!NetMode.IsOffline && NetMode.IsAuthority`. This covers the dedicated server and the dev host; the offline game never claims.

## Requirements
- On a boss death, if `m.Type == WantedBoss.Today(nowUtcMs).BossType`, every participant claims once with today's dayKey. Both values are read once per kill, so a kill at the reset second belongs to exactly one day.
- Participants = `Winners` ∪ the sharing set of each winner's party, computed by the **same** code as the EXP share (DRY). Each hero is credited at most once.
- An already-claimed hero gets the owner toast "Wanted reward already claimed today" and no state change.
- A successful claim: the RpgState push happens now, plus `RequestSave` (an immediate write, like a boss kill). Log one line per claim: character id, boss, dayKey, reward. Add the NetStats counter `wantedClaims`.
- Launch arg `--wanted-boss <monsterType>` (server/host) sets `WantedBoss.Override`, with a warning in the log. It is for e2e tests and live ops; an unknown type is an error and gets ignored.

## Architecture
```
KillRewards.Resolve (boss) -> WantedBossCredit.Pay(zone, m, Winners)
   -> PartyKillShare.CollectParticipants(zone, m, Winners, into)   // shared with PayParty
   -> for each h: h.Owner.ClaimWanted(def, dayKey)                  // PlayerRpg -> PlayerState.ClaimWanted
        -> PlayerRpg.WantedClaimed(ActionResult) event + Touch()
NetPlayer:        WantedClaimed ok -> RpgDirty, LastRpgSentMs=-10000; fail -> Toast(reason)
ZonePersistence:  WantedClaimed ok -> RequestSave(p)
```

## Related code files
- Create: `Assets/Scripts/World/WantedBossCredit.cs` (~50 lines).
- Modify: `Assets/Scripts/World/KillRewards.cs`: one line after the `RaiseBossKilled` loop.
- Modify: `Assets/Scripts/World/PartyKillShare.cs`: extract the member loop of `PayParty` into `AddSharing(...)` and add a public `CollectParticipants`. It stays under 100 lines.
- Modify: `Assets/Scripts/World/PlayerRpg.cs`: `event Action<ActionResult> WantedClaimed` and `ClaimWanted(def, day)` (~8 lines, about 195 total). If it goes over 200 lines, move the quest helpers into `PlayerRpg.Quests.cs` (partial).
- Modify: `Assets/Scripts/Net/Server/NetPlayer.cs` (~6 lines) and `Assets/Scripts/Net/Server/ZonePersistence.cs` (1 line in `Attach`; the file is 374 lines, so add nothing else there).
- Modify: `Assets/Scripts/Net/NetLaunchArgs.cs` (`--wanted-boss`).
- No protocol file changes. No `Skills/` or SkillTable changes.

## Implementation steps
1. Refactor `PartyKillShare`: pull the "who shares" loop into a helper that both `PayParty` and `CollectParticipants` call. Behaviour stays the same; check with the existing party e2e (`DebugCapture.NetParty`) in phase 05.
2. Write `WantedBossCredit.Pay`:
   - guard the run mode, read `now = DateTimeOffset.UtcNow`, `def = WantedBoss.Today(now)`, and return if `def.BossType != m.Type`;
   - collect the participants into a static reused list (no allocations, like `KillRewards`) and claim for each.
3. Add `PlayerRpg.ClaimWanted` and the event.
4. Subscribe in NetPlayer (push now / toast) and ZonePersistence (save now).
5. Parse `--wanted-boss` in `NetLaunchArgs` and apply it at server start.
6. Compile check: read the latest compile in `%LOCALAPPDATA%\Unity\Editor\Editor.log` if Unity is open. Otherwise build through `Tools/build-from-copy.ps1`. **Never batch-build the open project.**

## Todo
- [ ] PartyKillShare helper extraction (same behaviour)
- [ ] WantedBossCredit.cs
- [ ] PlayerRpg event + method
- [ ] NetPlayer / ZonePersistence wiring
- [ ] `--wanted-boss` arg
- [ ] build from copy: 0 errors

## Success criteria
- Build from copy has 0 compile errors. ZenoniaNet.Tests and ZenoniaSkills.Tests are still green.
- Dev host with `--wanted-boss frostfangKing` (phase 05 bot): the killer and a non-hitting party member in range each get the reward exactly once, and a second kill (after a zone restart) gives only the normal drops.

## Risks
| Risk | L x I | Mitigation |
|---|---|---|
| The refactor changes the EXP share | L x H | Pure extraction with the loop unchanged. The party e2e prints shares; compare them before and after. |
| A hero dead at the kill gets nothing, which party players feel is unfair | M x M | Same rule as EXP (consistent, explainable). Listed as an open question; it can be relaxed later by counting dead party members within range. |
| The zone clock is skewed | L x M | The day comes from the server clock only. Use NTP on the VPS; the clamp in the API stops rollbacks. |
| Several zone processes later | L x L | Each character is held by one session, so there is no cross-zone duplicate. The rotation is deterministic. |
| `--wanted-boss` left on in production | L x M | Warning in the log on every start. Not set in the `deploy/` compose. |

## Security
The client cannot trigger a claim: only the authority's kill resolution reaches `ClaimWanted`. There is no client message to process.

## Next
Phase 04 shows the result; phase 05 runs it end to end.
