# Phase 04 - Zones, portals, zone handoff, Oakhaven hub, multi-zone process

## Context links
- `FrostHollowMap.cs`, `WorldBuilder.cs`, `MonsterSpawner.cs`, `GameMusic` (oakhaven-village.mp3 ready); web ref `C:\Project\zenoniaClone` (Oakhaven data, read only); P1 `Zone`; P3 tickets/registry

## Overview
Priority P1, pending, 30h. `MapDef` abstraction (tiles, solids, spawn zones, boss, portals, save point, music), Oakhaven village (no combat), portals/warps,
zone registry in API, handoff between processes (reconnect with ticket) and inside one process (move hero between Zone objects, no reconnect). One process hosts N zones.

## Key insights
- Custom tile collision (no Unity physics) -> several zones coexist in one process if all world state is per-Zone; remaining statics to remove: `FrostHollowMap` (-> `zone.Map`), `Monster.All/Boss` compat getters (-> `monster.Zone`), `LootDrop.All`, spawner.
- Client renders one zone at a time; zone change = unload world root, build new map, keep UI.
- Handoff must save before leaving (no dupes): source saves (version n), issues target ticket, despawns; target redeems.

## Requirements
- `--zones frost-hollow,oakhaven` per process; each zone registers `{zoneId, map, host, port, players, maxPlayers}` heartbeat 5 s.
- Portal = rect on map + target map + target spawn; touch -> server-side check -> transfer.
- Cross-process: S2C `ZoneTransfer{host, port, ticket, mapId}` (op 16); client reconnects; total < 2 s on LAN.
- Same-process: S2C `ZoneChanged{zoneId, mapId, heroNetId}`; client rebuilds map, no reconnect.
- Save point per map (respawn on death); `@warp`-style admin command (dev only).

## Related code files
Create: `Scripts/World/MapDef.cs`, `Scripts/World/OakhavenMap.cs`, `Scripts/World/PortalDef.cs`, `Scripts/World/ZoneHost.cs` (N zones in one process), `Scripts/Net/Server/ZoneTransferService.cs`, `Scripts/Net/Server/ZoneRegistryHeartbeat.cs`, `Scripts/Net/Client/ZoneLoader.cs`, `server-api/src/routes/zone-registry-routes.js`
Modify: `FrostHollowMap.cs` (-> MapDef instance), `WorldBuilder.cs` (build per map, rebuild on change), `Bootstrap.cs`, `MonsterSpawner.cs`, `Monster*.cs`, `LootDrop.cs`, `HeroController.cs` (`Zone` ref, map collision via zone), `Zone.cs`, `ZoneServer.cs` (route peers to zones), `Net/Protocol/NetOpcodes.cs`
Ownership: P4 owns `Scripts/World/*`, map files, ZoneServer routing; coordinate with P2 none (P2 done before or no overlap except ZoneServer -> P4 after P2 merge).

## Implementation steps
1. MapDef refactor with Frost Hollow only; F9 collision screenshots + smoke unchanged.
2. Remove compat statics: entities carry `Zone`; all queries per zone.
3. ZoneHost: several zones ticking in one process (one Update loop iterates zones); AOI/event bus per zone.
4. Oakhaven map (backdrop, tiles, collision, NPC placeholders, music), save point.
5. Portals + same-process transfer.
6. Zone registry + cross-process transfer (save -> ticket -> ZoneTransfer -> redeem).
7. Client ZoneLoader (loading overlay, rebuild map, clear proxies/pools).
8. Harness: bots walking portal loop between two processes for 30 min.

## Todo
- [ ] MapDef refactor
- [ ] Per-zone entity state
- [ ] ZoneHost multi-zone
- [ ] Oakhaven map
- [ ] Portals + in-process transfer
- [ ] Registry + cross-process transfer
- [ ] Client ZoneLoader
- [ ] Portal soak test

## Success criteria
- Portal soak: 50 bots x 30 min across 2 processes: 0 lost/duplicated characters, item counts conserved (checker sums inventories before/after).
- Two zones in one process isolated (no cross AOI/events).

## Risks
| Risk | L x I | Mitigation |
|---|---|---|
| Static removal regressions | H x M | step-by-step, smoke + videos per step |
| Dupe/loss during handoff | M x H | save-then-ticket ordering, version check, soak checker |
| Client memory on zone switch (Android) | M x M | unload map textures/pools, Resources.UnloadUnusedAssets |
| Hot zone overload (everyone in town) | M x H | maxPlayers per zone; P8 channels (zone instances "ch1/ch2") if needed |

## Security
Portal use validated server side (hero inside rect, alive, cooldown); tickets single-use, bound to character + target zone.

## Next steps
P5 chat global/whisper uses world bus; P6 party; P8 distributes zones across processes/hosts.
