# Phase 04 - Hero + netcode integration (the only phase touching netcode-owned files)

## Context links
- Gate: online-netcode agent's work on `HeroController*.cs`, `ComboSystem.cs`, `ComboProjectile*.cs`, `Net/**` merged to `unity`. Re-read those files first; line refs below are from 2026-10-30.
- `HeroController.Skill.cs` (TryCastSkill/TickSkill/CastPose), `HeroController.cs:159` (`Pressed(GameKey.Skill)`), `HeroController.Net.cs` (`PlaySkillFx`), `.SimState.cs` (OwnerSim), `.Predicted.cs` (`PredictsSkill`)
- `Net/Protocol/NetInputFrame.cs` (`InputBits`, AllMask 511 of 16 bits), `NetOwnerSim.cs` (9-field presence mask), `NetEventTypes.cs` (`SkillCast` Step = class id today), `NetMessageWriter.cs` (`SelfStateMsg.SkillCdLeft`), `NetOpcodes.cs` (`InvOp`, `NetProtocol.Version = 2`), `NetSnapshotTypes.cs` (`MonsterBits` 8/16 used, 32/64/128 free)
- `Net/Client/InputSampler.cs:24`, `Net/Server/NetInputDriver.cs:38`, `Net/Client/LocalHeroPredictor.cs:62`, `Net/Client/NetEventPlayer.cs:80`, `Net/NetEventBus.cs:77`, `Net/Server/ServerReplication.cs:196`, `Net/Server/ZoneServer.cs:267`

## Overview
Priority P1 | after 03 + netcode merge | single owner, single branch `skills/p04-integration`. Goal: minimal, surgical edits; all logic stays in `Skills/`.

## Key insights
- Input frame has 7 spare bits: `Skill` bit (64) + 3-bit skill index at bits 9-11 -> 8 skills, wire stays 2 B. One skill press per 1/30 s step is enough.
- Per-skill cooldowns must be in `OwnerSim` or replay after correction refuses/duplicates casts. Presence mask makes idle cost 0; mask grows 9 -> 19 bits (3 B).
- `SkillCast.Step` currently carries class id (redundant: the hero knows its class) -> reuse as skill index. No new event fields; `Other` = target id / heal amount; X/Y = aim/landing.
- Learn/assign requests reuse the inventory request path (`InvAction` + `RpgState` reply): only 2 new `InvOp` values, no new opcode.
- Monster status replication: 3 free `MonsterBits` -> 3-bit "dominant status" code (0 none, 1 freeze, 2 stun, 3 burn, 4 poison, 5 mark). No wire size change.

## Touch points (exhaustive; target line budget)
| File | Change | ~Lines |
|---|---|---|
| `HeroController.Skill.cs` | rewrite to delegate: `HeroSkills skills`; `TryCastSkill(int index)`; keep `TryCastSkill()` = magazine primary (debug/tests); `SkillCooldownLeftMs` -> primary; `TickSkill` -> `skills.Tick` + forced-move step; MP spend skipped when `replaying` | rewrite (<120) |
| `HeroController.cs` | `Pressed(GameKey.Skill)` -> `SkillInput.PressedIndex(Pressed)` loop; `AtkStat` x `skills.Buffs.AtkMul`; `combo.Update(dtMs * skills.Buffs.AtkSpeedMul, ...)`; `ResetSkillState` unchanged call | 4 |
| `HeroController.SkillBody.cs` (new partial) | implements `ISkillBody` (TryBlink w/ RectBlocked, BeginForcedMove, AddInvuln, Heal, Feet, Facing ...) | new <90 |
| `HeroController.ClassSkills.cs` | delete `ShadowStep()` / `Heal()` + consts; keep `HealPulse` (cleric finisher) | -60 |
| `HeroController.Net.cs` | `PlaySkillFx(Vector2 at, ushort arg)` -> `PlaySkillFx(int index, Vector2 at, int facing, ushort arg)` => `SkillRun.PlayRemote(...)`; `ProxySetSelf` reads cooldown array | 10 |
| `HeroController.SimState.cs` | Capture/Restore `CastSkill`, `Cd[8]`, `AtkSpeedLeft`, forced-move timer; `castTotal` from SkillDef lock | 8 |
| `HeroController.Predicted.cs` | `PredictsSkill` -> `skills.Predicts(index)` (derived rule, phase 03) | 3 |
| `HeroController.Combat.cs` | incoming damage: `dmg = skills.Buffs.Absorb(dmg)` before HP loss | 1 |
| `Net/Protocol/NetInputFrame.cs` | `SkillIndexShift = 9`, `SkillIndexMask = 7 << 9`, `AllMask = 4095`; helpers `WithSkill(bits, i)`, `SkillIndex(bits)` | 6 |
| `Net/Protocol/NetOwnerSim.cs` | + `CastSkill` byte, `Cd0..Cd7`, `AtkSpeedLeft`, `MoveLeft` timers in the presence mask (3-byte mask) | 25 |
| `Net/Protocol/NetEventTypes.cs` | doc: `SkillCast.Step` = skill index; `HitFlags.Status = 8` (hit applied a status) | 2 |
| `Net/Protocol/NetMessageWriter.cs` / `NetMessageReader.cs` | `SelfStateMsg.SkillCdLeft` float -> `ushort[8] SkillCds` + `byte Energy` (mechanic Overcharge) | 8 |
| `Net/Protocol/NetOpcodes.cs` | `InvOp.SkillLearn = 6, SkillAssign = 7`; `NetProtocol.Version = 3` | 2 |
| `Net/Protocol/NetSnapshotTypes.cs` | `MonsterBits.StatusShift = 5` (3 bits) | 1 |
| `Net/NetEventBus.cs` | `SkillCast(h, index, at, arg)` | 3 |
| `Net/Client/NetEventPlayer.cs` | pass `e.Step` index; skip own predicted skill per index | 2 |
| `Net/Client/InputSampler.cs`, `Net/Client/LocalHeroPredictor.cs`, `Net/Server/NetInputDriver.cs` | map `GameKey.Skill1..Skill8` <-> skill bit + index | 3+3+3 |
| `Net/Server/ServerReplication.cs`, `Net/Server/ZoneServer.cs` | SelfState fills cooldown array; dirty check compares array | 4+2 |
| `Monster.Net.cs` (not netcode-listed) | pack/unpack status code; client timers for visuals | 8 |
| `World/PlayerRpg.cs` (not netcode) | `ApplyInvAction`: `SkillLearn` (Slot = index) / `SkillAssign` (Slot = index, ItemUid = slot number) -> SkillRanks/SkillLoadout, `Sanitize` | 15 |
| `InputAdapter.cs` (not netcode) | GameKey `Skill1..Skill8`, `SkillSlot1..3` (U/I/O = visible page), `SkillTree` (T); L = slot 2 alias | 10 |
Never touched: `ComboSystem.cs`, `ComboProjectile*.cs`.

## Data flow
```
touch/keys -> InputAdapter GameKey.SkillN (index) -> [offline] HeroController.Pressed -> HeroSkills.TryCast
                                                 -> [client] InputSampler bits(Skill|idx<<9) -> UDP -> ServerInputState -> NetInputDriver.Pressed(SkillN)
server: HeroSkills.TryCast -> SkillRun(authority) -> Monster.TakeHit -> MonsterHit events; NetEventBus.SkillCast(idx, at, arg) -> AOI clients
client: own predicted skill: local SkillRun(view only) at press; else SkillCast event -> SkillRun.PlayRemote(view only)
owner reconciliation: OwnerSim{CastSkill, Cd[8], MoveLeft, AtkSpeedLeft} restore + replay
learn: UI -> [offline] PlayerRpg.ApplyInvAction ; [online] InvAction(SkillLearn) -> ServerInventoryActions -> PlayerRpg -> RpgState(save json incl. ranks) -> ClientRpgMirror
```

## Implementation steps
1. Rebase on merged netcode; re-verify every touch point still exists; update this table if moved.
2. Protocol constants + OwnerSim + SelfState + InvOp; extend `Tests/dotnet/ZenoniaNet.Tests` (`InputTests` skill index round trip, `MessageRoundTripTests` OwnerSim with cds, SelfState array). Bump Version 3.
3. HeroController partials (table order). Keep the legacy `TryCastSkill()` so `DebugCapture*` and `FrostSmokeTest` keep compiling.
4. Input mapping (InputAdapter, sampler, driver, predictor).
5. PlayerRpg learn/assign; ServerInventoryActions needs no change (generic) - verify.
6. Monster.Net status code.
7. FrostSmokeTest `CheckSkills`: per class, Lv60 save with all ranks 1, cast every skill on 3 dummies headless (offline mode): MP spent, cd set, damage dealt or buff/heal applied; illegal learn refused.
8. Online check: `Tools/net-local-test.ps1` host + 2 clients, `--bot fight` + a new `--bot skills` (cycles skill presses) -> no desync storms (`NetStats` corrections/min within the netcode baseline).

## Todo
- [x] rebase + re-verify touch list
- [x] protocol (version 3) + dotnet net tests (70 pass)
- [x] HeroController partials (ISkillBody stays the `HeroSkillBody` adapter, no new partial)
- [x] input mapping (Skill1..8 keys, U/I/O + L slot keys, sampler, predictor, driver)
- [x] learn / assign / respec via InvOp (3 new ops)
- [x] monster status replication (3-bit status code + mark bit, chillLeft bridge removed)
- [x] FrostSmokeTest CheckSkills (`Assets/Editor/SkillHeroCheck.cs`, all 5 classes, Lv 60, 8 skills each)
- [x] online run: dedicated server + 5 skills bots (0 and 50 ms one-way latency / 2% loss), all checks pass
- [ ] full `Tools/net-lag-matrix.ps1` matrix (only the 100 ms RTT / 2% loss cell was run)

## Implementation notes (2026-10-30)
Protocol version **3**. Deviations from the table above:
- Input frame: `InputBits.Skill` (64) + 3-bit index at bits 9-11 (`AllMask` 4095), still 2 B. Cut frames merge presses with `InputBits.Merge` (indexes are never OR-ed). New file `Net/Protocol/InputBits.cs`.
- `OwnerSim` keeps its 3-byte idle cost (8-bit base mask) and gets an optional extension block (flag bit 7 of byte 0): cast skill index, 8 cooldowns, attack-speed buff (left + percent). `SkillCd` field removed. `MoveLeft` was NOT added: forced motion (Dash / Leap / Blink) is server-confirmed and the owner sees it as snapshot corrections (see report).
- `SelfStateMsg.SkillCdLeft` -> `ushort[8] SkillCds` (energy byte for the mechanic left to phase 11).
- `SkillCast` event: Step = (rank << 3) | index, Other = salt (low 16 bits of the casting input step: server, owner and every client seed rain drops alike; the rain seed no longer uses the position), X/Y/Facing as before. `NetEventPlayer` skips the owner's own event only for skills `SkillPrediction.Predicts` (no Motion, not NearestEnemy / LowestAlly).
- `InvOp.SkillLearn = 6` (Slot = index), `SkillAssign = 7` (Slot = skill or 255 = clear, ItemUid = magazine slot), `SkillRespec = 8` (three ops, not two: respec needed its own). `PlayerRpg.ApplyInvAction` validates with the same rules as the offline window.
- `MonsterBits`: bits 4-6 = dominant status code (0 none, 1 freeze, 2 stun, 3 burn, 4 poison, 5 slow / chill), bit 7 = marked (mark charges are not replicated: nothing on the client uses them). `MonsterStatus.ShowReplicated` replaces `ForceChill`.
- Hero: `TryCastSkill(int)` + `TryCastSkill()` (primary = first magazine slot), `AtkStat` includes `HeroBuffs.AtkMul`, combo speed x `AtkSpeedMul`, `TakeDamage` absorbs through the shield, `AddInvuln`. `ShadowStep` / `Heal` / `skillArg` deleted; `HealPulse` kept.
- Keys: `GameKey.Skill1..8` (no physical key), `SlotKey1..3` = U / I+L / O = the visible magazine slots of the current page (Dash or a skill), `GameKey.Skill` stays as the primary-skill alias for bots. `I` no longer opens the bag (B only). Magazine taps press `Skill1+index`.
- SFX: whirlwind / blade-cyclone / blade-dance / sanctuary clips renamed `-loop`, played by `SkillSfx.Loop` for the run / zone length; earth-break-impact / ice-storm-impact are played from the legacy ultimates.
- Test tooling: `--dev-level N` (server), `--net-bot skills` (learns through InvOp, then cycles the skill keys), `net-local-test.ps1 -Bot skills`, `stateDiff<bit>` telemetry in the predictor, `--skill-ui` steps `cast:I`, `face:D`, `mp`.

## Success criteria
- Offline: all 40 skills cast via keys and `TryCastSkill(i)`; existing combo/dash/ultimate recordings unchanged.
- Online (host + clients, 100 ms RTT, 2% loss): casting causes <= baseline +10% reconciliation corrections; remote players see every cast; damage only once (server).
- `dotnet test` (Net + Skills) green; `npm test` (server-api) green.

## Risks
| Risk | L x I | Mitigation |
|---|---|---|
| Netcode agent reshapes the files | H x M | gate + step 1 re-verification; keep logic out of their files |
| Prediction misfires on non-predicted motion skills (rubber-band) | M x M | same pattern as current Shadow Step; MoveLeft in OwnerSim for replay |
| Old clients after Version bump | L x L | handshake already rejects mismatched Version |
| Atk-speed buff desync in combo timing | M x L | AtkSpeedLeft in OwnerSim |

## Rollback
Revert the p04 merge commit: legacy `HeroClassDef.Skill*` fields are still present (removed only in phase 12), so the old L skill path returns intact.

## Security
- Server validates: index 0-7, learned rank > 0, cd, MP, not dead/casting; learn/assign through `SkillRanks`/`SkillLoadout` rules only.
- Clients never send positions/targets; repeated skill bits beyond 1 per frame are impossible by encoding.

## Next steps
Phase 05 binds the magazine to `GameKey.SkillN`; phase 07 tunes the wizard end to end.

## Unresolved questions
1. Keyboard: U/I/O for the 3 visible magazine slots + L alias of slot 2 + T for the tree window - OK?
