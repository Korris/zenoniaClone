# Phase 1 steps 9-17 report

## Done
- 9 Run modes: `NetLaunchArgs` (--mode/--port/--host|--connect/--name/--class/--max-players/--dev-key/--run-seconds/--net-bot/--net-stats/--net-test-version), Bootstrap branches. New `NetMode.NoViews` (server or -nographics) so unattended bot clients run the real client path with view-less entities.
- 10 `ZoneServer` (listener, key, Full/Version/BadToken/AlreadyOnline rejects, 120 pkt/s limit, 5 s hello timeout), `NetPlayer`, `NetInputDriver`, `DevLoginStub`, `ServerHeroFactory`.
- 11 `InterestGrid`+`InterestSet` (cell 64, see 2 / drop 3, dotnet-tested), `ServerReplication` (spawn/despawn diff, nearest-first capped snapshot with cursors, AOI/owner/zone-wide event routing), `NetEventBus`, shared NetId registry with 10 s quarantine.
- 12-13 Client: `NetClient`, `SnapshotBuffer` (150 ms render delay, teleport snap, dotnet-tested; no extrapolation, holds last state), `ProxySpawner`, `NetEventPlayer`, `ClientRpgMirror`; proxies: `HeroController.Net` (pose/step/progress animation through the existing Render), `Monster.Net`, `LootDrop` proxy (owner dim while priority runs), `NameTags`.
- 14 `NetInputSender` (30 Hz, last-3 redundancy, press counters, idle frames deduped to 10 Hz keep-alive, zero input with bag open) + InvAction round trip (Equip/Unequip/Use/Discard/Sort/Enhance via small hooks in InventoryPanel*).
- 15 `ConnectScreenGui` (name/address/class, Join, Host (dev), Offline; button under class select), `GameSession.Online` (runtime switch offline<->host/client, connection-lost handling).
- 16 `Tools/net-local-test.ps1`, `Tools/net-check-summary.mjs`, `Tools/build-from-copy.ps1`, `NetStats`, `--record-net`, `--net-bot fight|boss|wander` (BFS-navigating bot reusing DebugCapture.Think), `Tests/dotnet/ZenoniaSwarm` (N protocol clients in one process), editor `NetLogicCheck`.
- 17 Android LAN smoke: SKIPPED (no device); LiteNetLib IL2CPP still unverified.
- Requested addition: other players' damage numbers small (70%), white, 50% alpha (own = as before); hero hit numbers of others small/faded, no shake; host mode same. Offline unchanged.

## Results (localhost, Windows, all processes -batchmode -nographics)
- 8 Unity bot clients, 70 s (all 8 classes cycle): ALL CHECKS PASSED. Boss killed, 8/8 saw BossDied (lag 9-33 ms), 8/8 EXP, 0 exceptions, 0 kicks. Server tick avg 0.13 / p99 0.59 / max 18.8 ms. Snapshot avg 127 B, max 193 B. Per client down 2.6 KB/s, up 0.7 KB/s. Snapshots 15.0 Hz at every client.
- 50-client swarm (dotnet, all near entrance): tick avg 0.75 / p99 5.7 ms; down 10.2 KB/s, up 1.5 KB/s; snapshot max 655 B (50 heroes).
- 200-client swarm + 201st: 200 joined, 201st rejected Full, wrong version rejected, wrong key rejected; tick avg 4.4 / p99 11.3 ms; down 18.5 KB/s; snapshot max 1000 B (cap), 0 exceptions.
- Host mode: host + 2 guests: guests see host + each other, host sees guests with FX/names/boss bar HP dropping.
- Offline regression: `--class warrior --god --bot fight --shots 3,6` hero (72,34), 19 monsters (same as baseline). FrostSmokeTest on copy: only the 4 pre-existing Cleric heal failures; new NetLogicCheck all pass. `dotnet test`: 45 pass.
- Video: `C:\Project\zenoniaUnity\plans\reports\boss-videos\online-3-players.mp4` (14 s, 6 players: Cam + 5 bots, recorded from one 640x400 client window, with audio).

## Deviations / findings
- Snapshot cap 1100 -> 1000 B: LiteNetLib rejects unreliable packets > 1023 B (found by the 200-swarm: TooBigPacketException). Constant, checker and plan updated.
- Upstream bandwidth: ~0.7 KB/s for the real client, but 1.2-1.5 KB/s when input changes every frame (swarm / attack-spamming) vs plan <= 1 KB/s. Checker threshold 1.5 (use `--strict-up` for 1.0).
- No client-side prediction (P2): own hero is shown 150 ms + RTT behind, like the others.
- Remote cleric finisher heal sparkle / hit camera shake for others not replayed.
- Tests/dotnet csproj files were git-ignored by the root `*.csproj` rule (the earlier xunit csproj was never tracked); added `Tests/dotnet/.gitignore` negation.
- Shared/other-owned files touched: InventoryPanel.cs / InventoryPanel.Bag.cs (online hooks, pause only offline), MonsterDef.cs (All/ByIndex), Assets/Editor/FrostSmokeTest.cs (2 calls). HeroRenderer.cs NOT touched. ResourceTextureImportSettings.cs shows modified: not mine.
- New files have .meta copied from the build copy. Docs under ./docs not updated. Boss respawn stays 30 min (from steps 1-8; plan text says 15 +-2).

## Unresolved
- Android IL2CPP/LAN smoke.
- Decide upstream budget (1 vs 1.5 KB/s) or add input deltas in P2.

**Status:** DONE_WITH_CONCERNS
