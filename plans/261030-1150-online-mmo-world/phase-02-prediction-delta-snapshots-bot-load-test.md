# Phase 02 - Prediction, delta snapshots, lag harness, headless bot load test

## Context links
- P1 protocol/client/server files; `HeroController.cs` (Simulate), `ComboSystem.cs`, `FrostHollowMap.Move`; LiteNetLib `SimulateLatency/SimulatePacketLoss`

## Overview
Priority P1, implemented (see reports/fullstack-developer-261030-1352-phase02-prediction-delta-load.md), 28h. Own hero predicted + reconciled; remote interpolation adaptive; per-client delta snapshots vs acked baseline;
lag/loss matrix; dotnet console bot swarm (200 bots, one process) reusing `ZenoniaProto.Net` to load-test a zone.

## Key insights
- Server sim is fixed 30 Hz; client must predict own hero in the same fixed 1/30 steps (render interpolates between predicted steps).
- Movement/dash/combo pose deterministic from input + collision; damage/crit not -> predict motion + pose only.
- 200 Unity client processes impossible on one PC -> dotnet bot client speaking protocol (no rendering), simple brain from snapshots.

## Requirements
- Input-to-motion 1 frame at RTT <= 200 ms; typical correction < 1.5 units, smoothed 100 ms.
- Adaptive interpolation delay (2 snapshots + jitter, 120-250 ms), extrapolate <= 100 ms.
- Delta: per entity field mask vs baseline snapshot client acked (Input.ackSnapshotSeq); full entry on Spawn or baseline older than 1 s.
- 200 bots: tick p99 < 33 ms, down <= 24 KB/s/client avg.

## Architecture
- `HeroSimState` (x, y, facing, dash, cooldowns, combo step/phase/timer, invuln, skill cd) Capture/Restore; owner gets it in snapshot (~20 B).
- Client: input ring (seq), restore server state, replay > lastInputSeq, visual error blend; own AttackStep/BoltFired events from server deduped by seq.
- Server per-client baseline ring (last 32 snapshots' entity states).
- Bot: `Tools/ZenoniaBots/` dotnet console: N LiteNetLib clients, brain: walk to nearest monster from snapshots, press attack, random wander; stats JSON.

## Related code files
Create: `Scripts/Net/Client/LocalHeroPredictor.cs`, `AdaptiveInterpolationDelay.cs`, `Scripts/Net/Protocol/NetDeltaSnapshot.cs`, `Scripts/HeroController.SimState.cs`,
`Scripts/Net/Server/ClientBaselineRing.cs`, `Tools/ZenoniaBots/` (csproj links Protocol + LiteNetLib source), `Tools/net-lag-matrix.ps1`, `Tools/net-load-test.ps1`
Modify: `HeroController.cs` (fixed sub-step accumulator in all modes), `Net/Client/SnapshotBuffer.cs`, `NetInputSender.cs`, `Net/Server/SnapshotBuilder.cs`, `NetLaunchArgs.cs` (`--sim-latency`, `--sim-loss`)
Ownership: P2 owns `Net/Client/*` (except chat), `Net/Server/SnapshotBuilder.cs`, `ClientBaselineRing.cs`, `Tools/ZenoniaBots/`; not `server-api/` or `NetPlayer*` (P3).

## Implementation steps
1. Fixed-step hero simulate; offline feel check (record-boss all classes).
2. SimState + owner snapshot field.
3. Predictor + reconciliation + FX dedupe.
4. Adaptive delay.
5. Delta snapshots + baselines + tests (dotnet).
6. Bot swarm tool; load test script ramps 50 -> 100 -> 200; NetStats server summary.
7. Lag matrix (0/80/150/250 ms, 0/2/5/10 % loss) on 8 Unity clients.
8. Profile + fix hot spots (likely: InterestGrid queries, per-client snapshot write, MonoBehaviour Update count).

## Todo
- [x] Fixed step (online local hero + server heroes of remote players; offline / host-own hero keep the variable-dt update, deliberately unchanged)
- [x] SimState (`OwnerSim` in every snapshot to its owner, `HeroController.SimState.cs` capture / restore)
- [x] Predictor + dedupe (`LocalHeroPredictor`, own AttackStep / BoltFired / SkillCast events skipped or adopted)
- [x] Adaptive delay (120..250 ms) + extrapolation <= 100 ms
- [x] Delta snapshots (per-entity field masks vs the acked baseline, 16-slot rings both sides) + dotnet tests (63 pass)
- [x] Bot swarm + load script (`Tests/dotnet/ZenoniaSwarm` extended instead of a new `Tools/ZenoniaBots`; `Tools/net-load-test.ps1`)
- [x] Lag matrix (`Tools/net-lag-matrix.ps1`, 8 headless Unity clients per scenario, results in the report)
- [x] Profiling fixes (server tick split into poll / grid / snapshots / flush / sim; no single hot spot, targets met with margin, no code change needed)

## Implementation notes (as built)
- Protocol v2. Input = one frame per 1/30 s step (2 B: 4 held direction bits + 5 press bits), last 5 frames repeated in every 16 B packet, 16-bit seq. Server keeps a small
  jitter buffer per player and consumes exactly one frame per hero step (`ServerInputState`): the hero walks the same steps the client predicted. A late frame freezes the
  hero and earns one catch-up step (so jitter costs no movement, a flooding client gains nothing); the queue is cut only beyond 6 frames.
- Reconciliation compares the server position / dash / cast / combo state at the acked frame (`LastInputSeq`) with what the client predicted for that frame; beyond 0.4 units or
  on a discrete mismatch the hero is restored to the server state and the newer frames are replayed (FX / resources suppressed while replaying); the visible jump fades out in ~100 ms.
- Not predicted: damage, crits, loot, shadow step / heal (wait for the SkillCast event). Warrior / wizard skills start at once (picture only).
- Own shots are launched at once; the server's BoltFired names the matching bolt (BoltEnd finds it); a predicted bolt stops cosmetically on a monster proxy.
- Delta: entry = id + 1 B field mask (+ changed fields, position as signed bytes when small); a snapshot names its baseline (= newest snapshot the client acked, <= 14 old, else full);
  new entities are always full. `--net-no-delta` forces full snapshots for comparisons.
- Test switches: `--sim-latency/--sim-jitter/--sim-loss` (client, both directions), `--no-predict`, `--predict-after S`, `--net-no-delta`, bot `--net-bot twitch`.

## Success criteria
- Load test 200 bots 10 min: tick p99 < 33 ms, no disconnects, avg down <= 24 KB/s, server RAM logged.
- Lag matrix @150 ms/5 %: correction mean < 1.5 units, no rubber-band > 8 units.
- Delta cuts idle-scene bandwidth >= 40 % vs P1.

## Risks
| Risk | L x I | Mitigation |
|---|---|---|
| Non-determinism -> constant corrections | M x M | same code, fixed step, tolerance threshold |
| Teleport skills mispredict | M x L | not predicted; wait for SkillCast |
| Server CPU at 200 (per-client serialization) | M x H | shared per-entity encoded bytes reused across clients when same baseline; worker thread for serialization later |
| Bots unrealistic | M x M | mix brains (fighters, wanderers, idlers) |

## Security
Server ignores predicted state; ack ids bounded to ring.

## Next steps
P7 PvP uses prediction; P8 uses bot swarm for capacity sizing.
