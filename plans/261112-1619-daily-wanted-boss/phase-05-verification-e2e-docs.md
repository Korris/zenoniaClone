# Phase 05: Verification, end-to-end run, docs

## Context
- Unit tests: `dotnet test Tests/dotnet/ZenoniaSkills.Tests` (compiles `Rpg/**`) and `dotnet test Tests/dotnet/ZenoniaNet.Tests` (Protocol, Maps, BossRespawnTimer). Neither covers visuals.
- `server-api`: `npm test` (vitest, mongodb-memory-server). Run `npm ci` once on a new machine.
- E2E patterns:
  - `Tools/net-persist-test.ps1` (real mongod + real server-api + headless zone + scripted clients `DebugCapture.NetPersist.cs`, PASS/FAIL lines, numbers read from the API);
  - `Tools/play-online-local.ps1` (by hand);
  - the online fight bot `DebugCapture.NetNav.cs` already walks to the boss;
  - the party bots are in `DebugCapture.NetParty.cs`.
- Build **only** with `Tools/build-from-copy.ps1 -CopyDir C:\temp\zu-copy -OutExe C:\temp\player-wanted\Zenonia.exe`. Never batch-build the open project.
- Dev cheats (`--dev-level` ...) are ignored when `--api-url` is set (by design), so a real-API run needs a strong character through a test fixture.

## Overview
Priority P1 before merge. The automation is new but small.

## Requirements / test matrix
| Layer | What | How |
|---|---|---|
| Unit C# | day math, rotation, override, claim once, caps, save round trip | `WantedBossTests.cs` (phase 01) |
| Unit JS | schema keeps the field, clamp, trade/ops keep it, default 0 | `wanted-day.test.js` (phase 02) |
| Regression | all existing suites | both `dotnet test` + `npm test` |
| E2E dev host (no API) | killer + non-hitting party member in range both claim; a member out of range does not; second kill the same day: no reward | bots below, `--wanted-boss frostfangKing --dev-level 30` |
| E2E real API | claim persisted (`GET /characters/:id` shows `state.wantedDay` = today, gold/stones increased by the exact reward); relog then a kill after a zone restart (the boss respawns at zone start) gives no second reward; a graceful zone restart right after the claim keeps the reward and the flag together | `Tools/wanted-boss-test.ps1` |
| Manual + eyes | board text, chat line, toast on web and mobile HUD | `play-online-local.ps1` + screenshots |

## Related code files
- Create: `Assets/Scripts/DebugCapture.NetWanted.cs` (~150 lines). It holds the bot roles `wanted-kill` (walk to the boss with the NetNav helpers, fight, wait for RpgState `WantedDay > 0`, print PASS/FAIL + gold/stone deltas) and `wanted-near` (join the killer's party, stand within 300 u, never attack). Keep it a separate partial and do not grow `DebugCapture.NetPersist.cs`.
- Modify: the `DebugCapture.cs` role switch (1 line per role).
- Create: `Tools/wanted-boss-test.ps1` (modeled on `net-persist-test.ps1`: same mongod/API/zone start code, made-up secrets, PASS/FAIL count). Factor the shared start code only if it is copied verbatim; otherwise accept the duplication in a test script.
- Create: `server-api/scripts/dev-boost-character.mjs` (test fixture). It sets `state.level` on one character by name and **refuses** unless `MONGO_URI` points at 127.0.0.1/localhost. It is never wired into the app.
- Docs to update:
  - `README.md` (Maps/online section: the wanted boss rule; also fix "quests stay offline", which is stale since QuestAccept/TurnIn run on the zone);
  - `docs/deploy.md` (server-api before zone);
  - `docs/pending-tasks.md` (status, open questions);
  - `docs/online-events-ideas.md` (#2 marked as in progress or done).

## Implementation steps
1. Run the three unit suites; all green.
2. Build from a copy.
3. Dev-host run: server `--mode host` or server without `--api-url`, with `--wanted-boss frostfangKing`. Run 3 bots: killer, near party member, a far party member parked at the entrance.
4. Real-API run with `Tools/wanted-boss-test.ps1`:
   - register and create through the real flow;
   - boost the level with the fixture while offline;
   - join, kill, check the API;
   - relog, restart the zone, kill again, and check the API values have not changed except the normal drops (assert stones/gold delta is less than the reward size).
5. Manual: `play-online-local.ps1`. Talk to Guard Rook (Oakhaven) and Leila (Sunspire), take screenshots and **look at them**. Save them under `docs/reference/` with meaningful names.
6. Update the docs. Note in pending-tasks that `dev-start.json` cheats must still be off before release (unchanged by this feature).

## Todo
- [ ] unit suites green (C# x2, vitest)
- [ ] build from copy 0 errors
- [ ] DebugCapture.NetWanted.cs + roles
- [ ] dev-host e2e PASS
- [ ] wanted-boss-test.ps1 + boost fixture, real-API e2e PASS
- [ ] party regression: EXP shares are the same as before the refactor (NetParty bot log)
- [ ] screenshots viewed
- [ ] docs updated

## Success criteria
- All PASS lines and 0 FAIL in both e2e scripts. The API value is checked, not the bots' word.
- The screenshots show the board and the claim line readable on both HUD layouts.
- The report says plainly what was not seen in game (for example a real 05:00 rollover: covered only by unit tests).

## Risks
| Risk | L x I | Mitigation |
|---|---|---|
| The bot cannot kill the King in time (Lv30 not enough, boss leash) | M x M | Raise the boost level in the fixture; the timeout prints FAIL with the boss HP. |
| The boost fixture is used against production | L x H | It refuses any non-localhost URI and lives in `scripts/`, outside `src/`. |
| Ports collide with other local runs | L x L | Ports that differ from net-persist (7797/3198/27198). |
| Unity open on the project during the build | M x H | Build from a copy only (CLAUDE.md). |

## Rollback
The test-only files can stay. The feature itself is rolled back in the order 04 -> 03 (02 stays).
