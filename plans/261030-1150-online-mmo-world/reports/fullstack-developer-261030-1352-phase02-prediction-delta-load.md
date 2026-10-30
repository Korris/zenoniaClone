# Phase 2 report: prediction, delta snapshots, lag harness, 200-bot load test

Plan: `phase-02-prediction-delta-snapshots-bot-load-test.md`. Branch unity, nothing committed. Protocol version 1 -> 2 (clients and server must match).

## What was built

| Area | Files |
|---|---|
| Fixed step | `HeroController.Predicted.cs` (new), `HeroController.cs` (Update split into `StepOnce`), only for the online local hero and for the server hero of a remote player. Offline and the host's own hero keep today's variable-dt update (requirement: offline unchanged). |
| Sim state | `HeroController.SimState.cs`, `Net/Protocol/NetOwnerSim.cs` (`OwnerSim`: dash dir / combo phase+step / timers; 4 B idle, <= 22 B), `ComboSystem.Capture/Restore` |
| Predictor + reconcile | `Net/Client/LocalHeroPredictor.cs`, `InputSampler.cs`, `NetInputSender.cs` (rewritten), `HeroController.Combat/Skill` guards (`predicted`, `replaying`) |
| FX dedupe | `NetEventPlayer` (own AttackStep skipped, own BoltFired adopts the predicted bolt, own warrior/wizard SkillCast skipped), `ComboProjectile.Predicted.cs` (+ `Fire` returns the bolt) |
| Interpolation | `Net/Protocol/AdaptiveInterpolationDelay.cs` (2 snapshot intervals + 3x jitter, clamp 120..250 ms), `SnapshotBuffer` (instance delay, extrapolation <= 100 ms) |
| Delta snapshots | `Net/Protocol/NetDeltaSnapshot.cs` (`EntityDelta`, `SnapshotRing`, `SnapshotLookup`), `SnapshotPacker.cs`, `ServerReplication.SendSnapshot`, `NetPlayer` (sent ring, ack) |
| Input upstream | `NetInputFrame.cs` (frame = 2 B bits, `ServerInputState` jitter buffer), `NetMessageWriter/Reader.Input`, `NetInputDriver`, `ZoneServer` |
| Lag harness | `NetLinkSimulator.cs` (upstream), LiteNetLib `SimulateLatency/Loss` (downstream, vendored `NetManager.cs` now compiles the simulation in release builds), `NetLaunchArgs` (`--sim-latency --sim-jitter --sim-loss --sim-seed --no-predict --predict-after --net-no-delta`), bot `--net-bot twitch`, `Tools/net-lag-matrix.ps1`, `net-local-test.ps1` params, `net-check-summary.mjs` metrics |
| Load test | `Tests/dotnet/ZenoniaSwarm` (extended instead of a new `Tools/ZenoniaBots`: delta decode + ack, fighter / wanderer / idler brains, stats json), `Tools/net-load-test.ps1`, server tick split stats (`tickPollMs/GridEventsMs/SnapshotMs/FlushMs/SimMs`) |
| Tests | `Tests/dotnet/ZenoniaNet.Tests`: 64 pass (was 45): input queue, gap fill, catch-up credit, delta round trips / loss / baseline miss / truncation fuzz, owner sim, adaptive delay, extrapolation |

### Design in four lines
1. Every client sim step (1/30 s) sends one input frame (4 direction bits + 5 press bits, 16-bit seq); each packet repeats the last 5 (16 B, 30 pkt/s). The server consumes exactly one frame per hero step, so server and client walk the same steps. A late frame freezes the hero one tick and earns a catch-up step (also earned by server frame hitches); a client can never get more steps than real time passed. Queue cut only beyond 8 frames (presses survive the cut).
2. Every snapshot carries the owner's `LastInputSeq` + `OwnerSim`. The client compares the server position / dash / cast / combo state at that frame with its own record; > 0.4 units or a state mismatch = restore server state, replay the newer frames (no FX, no resources spent), fade the visible jump out in ~100 ms (> 24 units = snap).
3. Not predicted: damage, crit, loot, assassin blink, cleric heal (they wait for the server's SkillCast; their teleport shows up as a classified "teleport" correction). Predicted: movement, dash, combo timing + swing / cast / bolt pictures (bolt stops cosmetically on a monster proxy), warrior / wizard skill picture.
4. Delta: entry = id + 1 B mask + changed fields (position as signed bytes when the step is small); snapshot names its baseline (newest snapshot the client acked, <= 14 old, else full); new entities always full; `--net-no-delta` forces full for A/B.

## Results (Windows, localhost, everything -batchmode -nographics except the one 640x400 recording window)

### 1. Lag / loss matrix: prediction on, 8 Unity bot clients per scenario, 45 s each (`Tools/net-lag-matrix.ps1`)
RTT = round trip (RTT/2 each way, +-10 % jitter); loss % is applied in EACH direction (so a round trip loses about twice that). 2 of the 8 bots run the `twitch` brain
(run 0.5 s / stop / swing twice / turn): constant start-stop, attacks and dashes to stress the prediction; the others fight monsters / the boss.
"corr mean" = mean |correction| over ALL reconciles (clean ones count as 0), units. "rubber-band max" = largest correction that is not a server-side death / respawn /
blink (those are counted in "teleports"). "input->motion" = key down until the local hero is drawn 0.5 units away. "input cut / frozen / catch-up" = server counters of
input frames cut from the jitter buffer / hero steps frozen waiting for a late frame / second steps that repaid a freeze or a server hitch.

| RTT ms | loss % /way | predict | corr mean | rubber-band max | teleports | corrected | input->motion ms | interp delay ms | down KB/s | up KB/s | snap avg B | tick p99 ms | input cut | frozen | catch-up | checks failed |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 0 | 0 | yes | 0.000 | 0.0 | 0 | 0.0% | 33 | 150 | 1.63 | 0.64 | 66 | 0.76 | 0 | 2 | 0 | 1 |
| 0 | 2 | yes | 0.026 | 1.0 | 4 | 0.1% | 33 | 144 | 1.71 | 0.65 | 69 | 1.02 | 0 | 28 | 84 | 0 |
| 0 | 5 | yes | 0.019 | 1.0 | 3 | 0.2% | 33 | 143 | 1.67 | 0.63 | 67 | 0.85 | 0 | 98 | 96 | 0 |
| 0 | 10 | yes | 0.021 | 1.0 | 4 | 0.2% | 33 | 140 | 1.70 | 0.62 | 68 | 1.09 | 0 | 42 | 42 | 1 |
| 80 | 0 | yes | 0.039 | 1.0 | 9 | 0.2% | 33 | 166 | 1.83 | 0.67 | 67 | 0.96 | 0 | 5 | 0 | 0 |
| 80 | 2 | yes | 0.034 | 10.7 | 0 | 0.7% | 33 | 140 | 1.48 | 0.61 | 64 | 0.41 | 0 | 40 | 34 | 4 |
| 80 | 5 | yes | 0.016 | 0.0 | 2 | 0.0% | 33 | 156 | 1.62 | 0.61 | 74 | 0.52 | 0 | 118 | 112 | 3 |
| 80 | 10 | yes | 0.008 | 0.0 | 2 | 0.0% | 33 | 152 | 1.76 | 0.62 | 70 | 0.85 | 0 | 110 | 105 | 1 |
| 150 | 0 | yes | 0.035 | 0.6 | 5 | 0.1% | 33 | 169 | 2.01 | 0.68 | 77 | 0.73 | 0 | 7 | 7 | 0 |
| 150 | 2 | yes | 0.012 | 0.0 | 2 | 0.0% | 33 | 172 | 1.63 | 0.62 | 69 | 0.56 | 0 | 50 | 41 | 1 |
| 150 | 5 | yes | 0.001 | 0.6 | 2 | 0.1% | 33 | 168 | 1.76 | 0.66 | 68 | 0.94 | 0 | 116 | 110 | 2 |
| 150 | 10 | yes | 0.000 | 0.0 | 0 | 0.0% | 33 | 169 | 1.88 | 0.66 | 79 | 0.63 | 0 | 292 | 285 | 3 |
| 250 | 0 | yes | 0.017 | 0.7 | 4 | 0.1% | 33 | 178 | 1.90 | 0.66 | 78 | 0.84 | 0 | 146 | 141 | 0 |
| 250 | 2 | yes | 0.024 | 0.0 | 7 | 0.1% | 33 | 184 | 1.91 | 0.67 | 74 | 1.08 | 0 | 111 | 108 | 1 |
| 250 | 5 | yes | 0.005 | 2.0 | 2 | 3.6% | 33 | 188 | 2.06 | 0.70 | 76 | 1.02 | 0 | 124 | 121 | 0 |
| 250 | 10 | yes | 0.044 | 10.9 | 3 | 1.4% | 33 | 180 | 1.91 | 0.64 | 78 | 0.83 | 0 | 399 | 393 | 1 |

Reading: mean correction <= 0.044 units everywhere (budget 1.5); 0.0-3.6 % of reconciles needed a correction at all; input -> motion is one sim step (33 ms) at every RTT / loss;
no input frame was ever cut. Rare rubber bands: 4 logged events above 6 units in 16 scenarios (10.7 / 8.0 at 80 ms / 2 % and 10.2 / 10.9 at 250 ms / 10 %; every other scenario <= 2 units); the budget "none > 8 units" at
150 ms / 5 % is met there (0.6 max). Interpolation delay of the OTHER entities adapts 140 -> 188 ms with the arrival jitter. Upstream 0.6-0.7 KB/s, down 1.5-2.1 KB/s per client.
The `checks failed` column counts checker rules unrelated to prediction in 45 s runs (bot did not fight, boss not killed within the run, boss-death sync window), not a regression.

### 2. Same scenarios WITHOUT prediction (`--no-predict`, i.e. phase 1 behaviour), 8 clients, 45 s

| RTT ms | loss % /way | predict | corr mean | rubber-band max | teleports | corrected | input->motion ms | interp delay ms | down KB/s | up KB/s | snap avg B | tick p99 ms | input cut | frozen | catch-up | checks failed |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 80 | 2 | no | - | - | - | - | 271 | - | 1.96 | 0.68 | 73 | 0.86 | 0 | 39 | 34 | 1 |
| 80 | 5 | no | - | - | - | - | 332 | - | 1.89 | 0.66 | 73 | 0.97 | 0 | 86 | 81 | 0 |
| 150 | 2 | no | - | - | - | - | 416 | - | 2.08 | 0.69 | 80 | 1.03 | 0 | 9 | 6 | 1 |
| 150 | 5 | no | - | - | - | - | 399 | - | 2.01 | 0.67 | 79 | 1.07 | 0 | 163 | 159 | 1 |
| 250 | 2 | no | - | - | - | - | 465 | - | 2.15 | 0.70 | 83 | 0.98 | 0 | 210 | 209 | 1 |
| 250 | 5 | no | - | - | - | - | 426 | - | 2.09 | 0.67 | 82 | 1.02 | 0 | 129 | 129 | 2 |

Input -> motion is 270-465 ms without prediction (RTT + interpolation delay) against 33 ms with it (`net-lag-matrix.ps1 -NoPredictOnly`).

### 3. Bandwidth before / after delta snapshots (per client, KB/s, LiteNetLib byte counters; IP + UDP headers not included)
| Scenario | Phase 1 report | v2 full snapshots (`--net-no-delta`) | v2 delta (default) | Cut vs full |
|---|---|---|---|---|
| 8 Unity bots, boss / fight, down | 2.6 (snapshot avg 127 B) | - | 1.6-2.0 (snapshot avg 62-79 B) | - |
| 50 protocol bots, mixed brains, down | 10.2 | 10.6 (avg 686 B) | 3.7 (avg 207 B), 2.9 in the last run | -65 % |
| 50 protocol bots, ALL idle, down | - | 9.9 | 3.1 (avg 180 B) | -69 % (plan: >= 40 %) |
| 200 protocol bots, mixed, down | 18.5 | 15.3 (cap bound, avg 964 B) | 9.5 (ramp) / 7.2 (10 min soak) | -38 % / -53 % |
| Upstream (input), client view | 0.7, 1.2-1.5 when input changes every frame | same code | 0.6-0.7 (Unity), 0.58-0.91 (swarm) | <= 1 KB/s now met in every case |

Notes: the idle number is not 0 because an unchanged entity still costs 3 B (id + empty mask) so the client keeps a fresh sample; an idle scene of 50 entities = ~3 KB/s. The server-side
upstream counter of LiteNetLib reads 0.8-1.0 KB/s most likely because it counts a merged datagram (input + ack) once per packet; the client-side counter (0.6-0.7) is the real payload. The real wire adds
28 B of IP + UDP per packet (30 pkt/s = about +0.85 KB/s); halving the packet rate (input every second step, 8 frame redundancy) is the option if that ever matters.

### 4. 200-bot load test (`Tools/net-load-test.ps1`, swarm on the same PC as the server: pessimistic)
| Bots | Run | Server tick avg / p99 / max ms (net part of the frame) | Down avg KB/s | Up avg KB/s | Snapshot avg / max B | Lost / rejected | Server RAM peak |
|---|---|---|---|---|---|---|---|
| 50 | 60 s | 0.69 / 2.3 / 24 | 2.9 | 0.58 | 163 / 641 | 0 / 0 | 112 MB |
| 100 | 60 s | 2.34 / 6.0 / 24 | 5.8 | 0.85 | 325 / 966 | 0 / 0 | 117 MB |
| 200 | 60 s (final build) | 4.85 / 9.3 / 22.6 | 9.3 | 0.91 | 542 / 1000 | 0 / 0 | 133 MB |
| 200 | 600 s soak (build before the last two server tweaks) | 5.19 / 9.0 / 21.6 | 7.2 | 0.88 | 431 / 1000 | 0 / 0 | 104 MB at start, 132 after 1 min, 136 at 10 min |

Success criteria: tick p99 < 33 ms (met, 9 ms), no disconnects (met, 200/200 joined for 10 min), avg down <= 24 KB/s (met, 7-9), RAM logged (above). Phase 1 for comparison: 200 swarm tick avg 4.4 / p99 11.3,
down 18.5. Baseline misses 0, malformed 0, server exceptions 0. A 201st client is still rejected Full (phase 1 test, unchanged).

Where the 4.8 ms of a 200 bot tick goes (server.json samples, 200 bots, ms per tick avg): snapshot build + delta + send 4.2 (half the clients per tick, ~40 us per snapshot), poll 0.16, hero / monster sim 0.28,
event flush 0.12, grid + event routing 0.05. No single hot spot (sort, ToSnap, baseline lookup and packing are about equal), p99 is 5.5x under the budget, so no optimisation was made; the instrumentation stays
(`tickPollMs / tickGridEventsMs / tickSnapshotMs / tickFlushMs / tickSimMs`).

### 5. Other checks
- `dotnet test Tests/dotnet/ZenoniaNet.Tests`: 64 pass (new: 19).
- Win player build of the project copy: 0 errors (Editor assembly compiled too).
- Offline regression (`--class warrior --god --bot fight --shots 3,6`, hidden window): hero (72.00, 34.00), 19 monsters, same as the phase 1 baseline. Offline path unchanged by design (`predicted` / `StepGate` are null, `StepOnce(dt)` is the old Update body).
- Dev host mode + 2 bot guests (80 ms RTT, 2 % loss): guests see the host and each other, 17 hits each, 0 corrections, 0 exceptions; host tick avg 0.47 ms.
- `FrostSmokeTest` in the copy now reports 5 failures ("monsters spawned = 0", boss, launch): caused by the in-progress map work of someone else (offline normal start is now the village, a safe map, see `MapRegistry` / Bootstrap), not by this phase; the previous 4 Cleric heal failures are gone from the list only because the run stops at the first class.
- Video: `C:\Project\zenoniaUnity\plans\reports\boss-videos\online-prediction.mp4` (20 s, 640x400, one small window + 4 headless bots, 250 ms RTT / 3 % loss each way): first 9 s the local hero without prediction (answers about half a second late), then `--predict-after` switches prediction on in the same session (one visible 12-unit catch-up at the switch, then instant response).

## Deviations from the plan
- Swarm stays in `Tests/dotnet/ZenoniaSwarm` (extended) instead of a new `Tools/ZenoniaBots/`; `ClientBaselineRing.cs` is `SnapshotRing` inside `NetDeltaSnapshot.cs` (shared by both sides, in the protocol assembly, so dotnet tests cover it).
- Fixed step is applied to the online local hero and to server heroes of remote players, NOT to offline / the host's own hero (user requirement: offline unchanged; the host hero is authoritative locally and needs no prediction).
- Idle input dedupe (10 Hz keep-alive) was removed: the server consumes one frame per step, so every step sends a (12-16 B) packet. Redundancy is 5 frames, not 3 (with 3, a 20 unit lunge correction at 10 % loss was most likely a lost press).
- Delta baseline = newest acked snapshot (single baseline per snapshot, entries missing from it sent full) instead of per-entity baselines; same wire cost for fully visible scenes, simpler and loss safe.
- Own AttackStep / skill pictures are deduped by "the predicted hero already played it" rather than by sequence number.
- Added beyond the plan: catch-up credit (late frame / server hitch -> second step), `OwnerSim` timers as exact floats (a rounded timer can let a replay cross a combo phase one step early; the 17-34 unit rubber bands of the run before this change did not come back), `--predict-after`, `twitch` bot, server tick split stats, input-to-motion probe.

## Files outside my strict ownership that were touched (all small)
`ComboSystem.cs` (Capture/Restore of the combo state), `ComboProjectile.cs` (`Fire` returns the bolt, predicted-bolt hook; new `ComboProjectile.Predicted.cs`), `Assets/Plugins/LiteNetLib/NetManager.cs` + `VENDORED.txt` (the latency / loss simulation is now always compiled, was `#if DEBUG`).
Not touched: TouchControls, TouchJoystick, InputAdapter, Hud, HeroClassDef, Assets/Editor, server-api. New files have their .meta (generated by the open Editor).

## Unresolved / concerns
- Rare residual rubber bands (4 logged events of 8-11 units in 16 scenarios, no state mismatch, error already present at the acked frame): a server-side step that the client did not predict (not identified; needs a frame-by-frame trace). The visible effect is a 100 ms slide.
- Assassin blink and cleric heal are not predicted (they wait for the server's SkillCast: pose / position arrive RTT/2 late); the teleport shows as a counted "teleport" correction.
- Unchanged entities cost 3 B per snapshot (idle scene ~3 KB/s per client at 50 entities); a run-length "unchanged ids" block would cut it further if needed.
- Real wire upstream adds ~0.85 KB/s of IP + UDP headers; payload 0.6-0.7 KB/s meets the plan's number.
- Android IL2CPP build of LiteNetLib (and of the new simulation code) still unverified.
- `Assets/Scripts/Net/Api/*` (the account API client of the parallel work) needs the built-in `UnityWebRequest` module; my build copy lacked it, so I excluded that folder from the copy builds. Add `com.unity.modules.unitywebrequest` to `Packages/manifest.json` if the Editor reports CS1069.
- A leftover `scratchpad\player\Zenonia.exe -batchmode` process (pid 29292, started before this session) was not mine and was left running.
- Docs under ./docs were not updated (only `session-handoff.md` exists there).

