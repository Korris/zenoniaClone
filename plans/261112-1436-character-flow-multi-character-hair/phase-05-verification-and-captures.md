# Phase 05: verification and captures

## Context
- `Assets/Scripts/DebugCapture.NetPersist.cs` (`--login-shots DIR`, 662 lines: do not grow), `DebugCapture.HeroSelect.cs` (`--hero-select-test`).
- `Tools/build-from-copy.ps1` (never batch-build the project Unity has open).

## Test matrix
| Level | What | How |
|-------|------|-----|
| Unit | skills/net logic unaffected | `dotnet test Tests/dotnet/ZenoniaSkills.Tests`, `dotnet test Tests/dotnet/ZenoniaNet.Tests` |
| Unit | `CharacterLookPrefs.Parse/Format`, `RandomName` passes `NameProblem` | add to ZenoniaNet.Tests only if that project already links client source files; else covered by capture |
| Integration | server-api untouched | `cd server-api && npx vitest run` (sanity, must stay green) |
| E2E capture | each page | `--login-shots DIR --account <fresh>`: login, empty list, choose class, customize (layered + full-head class), list with 1-2 heroes, delete confirm |
| E2E capture | hero select offline unchanged | `--hero-select-test --shot-path P` |
| Manual | 2 clients + zone + server-api | create, enter, other client sees style; colour local only; delete clears prefs |

## Files
Create:
- `Assets/Scripts/DebugCapture.CharacterFlow.cs` (partial: new shots for list/customize; called from `LoginShots`)
Modify:
- `Assets/Scripts/DebugCapture.NetPersist.cs` (one call line into the new partial)
- `Assets/Scripts/DebugCapture.HeroSelect.cs` (only if the online label change touches its path)

## Steps
1. Run both dotnet suites + vitest.
2. Build via `Tools/build-from-copy.ps1`; run captures from the copy build.
3. Open every PNG and compare to mockups (CLAUDE.md: report only what was seen; state what was not seen in game).
4. Update `docs/pending-tasks.md` (deferred phases 06/07 + open questions) and changelog via docs-manager.

## Success criteria
All suites green; captures of every page exist and were viewed; manual 2-client check done or explicitly listed as not done.

## Rollback
Revert phase commits in reverse (04, 03, 01); phase 02 art can stay.
