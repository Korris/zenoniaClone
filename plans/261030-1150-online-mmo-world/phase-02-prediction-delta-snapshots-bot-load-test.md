# Phase 02 - Prediction, delta snapshots, lag harness, headless bot load test

## Context links
- P1 protocol/client/server files; `HeroController.cs` (Simulate), `ComboSystem.cs`, `FrostHollowMap.Move`; LiteNetLib `SimulateLatency/SimulatePacketLoss`

## Overview
Priority P1, pending, 28h. Own hero predicted + reconciled; remote interpolation adaptive; per-client delta snapshots vs acked baseline;
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
- [ ] Fixed step
- [ ] SimState
- [ ] Predictor + dedupe
- [ ] Adaptive delay
- [ ] Delta snapshots
- [ ] Bot swarm + load script
- [ ] Lag matrix
- [ ] Profiling fixes

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
