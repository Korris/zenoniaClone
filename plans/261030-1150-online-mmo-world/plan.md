---
title: "Online MMO world (RO-style shared maps, authoritative Unity zone servers)"
description: "Turn the single-player prototype into a persistent shared online world: login/char API (Node+Mongo), headless Unity zone servers, AOI, portals, social, PvP maps, deploy."
status: pending
priority: P1
effort: 220h
branch: unity
tags: [networking, mmo, litenetlib, aoi, zone-server, nodejs, mongodb, unity]
created: 2026-10-30
---

# Online MMO world

Locked: persistent shared world like Ragnarok Online (no rooms/lobbies). Maps = zones (Frost Hollow, Oakhaven, more later); everyone on a map shares
monsters, field spawns, boss respawn timers, loot (owner priority then free). Topology: Node+Mongo login/character API (JWT) + authoritative headless
Unity zone servers (same project, `-batchmode -nographics`), one process hosts 1..N zones, portals hand off. LiteNetLib UDP + custom binary protocol;
clients send input only (fed via `HeroController.AiHeld/AiPressed`); remotes view-only interpolated; own hero predicted (P2). Offline single-player stays.

## Phases

| # | Phase | Effort | Depends on | Status |
|---|---|---|---|---|
| 1 | [Net core, one zone, AOI, snapshots/events](phase-01-net-core-single-zone-aoi-snapshot-sync.md) | 64h | - | pending |
| 2 | [Prediction, delta snapshots, lag + headless bot load test](phase-02-prediction-delta-snapshots-bot-load-test.md) | 28h | 1 | pending |
| 3 | [Account/character API, persistence, server-side inventory/enhance/loot](phase-03-account-character-api-persistence.md) | 36h | 1 | pending |
| 4 | [Zones, portals, zone handoff, Oakhaven hub, multi-zone process](phase-04-zones-portals-handoff-oakhaven.md) | 30h | 3 | pending |
| 5 | [Readability, nameplates, chat](phase-05-readability-nameplates-chat.md) | 18h | 1 (chat bus after 3) | pending |
| 6 | [Party + EXP share, then guild, trade, vendor](phase-06-party-guild-trade-vendor.md) | 24h | 3, 5 | pending |
| 7 | [PvP maps](phase-07-pvp-maps.md) | 12h | 2, 4 | pending |
| 8 | [Deploy/ops: Linux build, Docker, VPS, scaling, monitoring, anti-cheat](phase-08-deploy-ops-scaling-monitoring-anticheat.md) | 18h | 4 | pending |

Parallel lanes after P1: {P2, P3, P5-readability}; file ownership listed per phase.

## Topology

```
client --HTTPS--> api (Node: auth + characters + zone registry + world bus WS) --> MongoDB
client --UDP--> zone process (Unity headless; zones e.g. frost-hollow, oakhaven) --HTTPS/WS (service key)--> api
zone sim 30 Hz | snapshots 15 Hz per client, AOI-filtered, delta (P2) | events reliable-ordered, AOI-filtered | input 30 Hz
```

## Cross-phase rules
- Offline mode = today's path; `FrostSmokeTest` + `--record-party` video stay green every phase.
- World state lives in a `Zone` object (monsters, loot, heroes, spawner, AOI grid, event bus), not statics; P1 runs one zone, P4 several per process.
- Cosmetic code never runs on headless; gameplay code never runs on client proxies.
- Protocol in pure asmdef `ZenoniaProto.Net` (dotnet-testable, reused by the dotnet bot client in P2).
- Server never trusts client state; only input + requests validated server side.

## Global success criteria
- P1: 50 players (bots) on Frost Hollow, server tick p99 < 20 ms, <= 20 KB/s down per client.
- P2: 200 bots on one zone, tick p99 < 33 ms, <= 24 KB/s down per client.
- Persistence: kill server mid-session -> lose <= 60 s of progress, never items from trades/enhance (immediate saves).

## Rollback
Branch per phase (`net/p1-core` ...), merge to `unity` when green; `--mode` default offline.

## Questions (decided 2026-10-30 unless noted)
1. DECIDED: loot owner = top damage, 3 s owner priority (+2 s party), then free.
2. DECIDED: EXP split by damage share; party members nearby share.
3. DECIDED: -10% EXP, respawn at the map save point.
4. DECIDED: Frostfang King respawns after 30 min.
5. DECIDED: 5 characters per account (one per class).
6. ~~Account model~~ DECIDED 2026-10-30: registered accounts only (username + email + password, JWT + refresh token remembered on the device), no guest login; social login maybe later. See phase 03.
7. Remote heroes on Android at 50+ visible: OK to cap full-detail heroes (nearest 30) and draw rest simplified?
8. VPS provider/region and budget; expected CCU at launch.
9. DECIDED: zone server rolls field loot in C#, API persists; enhance rolled by the API.
