# Phase 01 - Net core in one zone: login stub, many players, AOI, snapshots/events, view-only remotes

## Context links
- `README.md`, `docs/session-handoff.md`
- Sim: `Assets/Scripts/HeroController*.cs`, `Monster*.cs`, `MonsterSpawner.cs`, `ComboProjectile.cs`, `SkillDamage.cs`, `LootDrop.cs`, `RpgSession.cs`, `GameClock.cs`
- Boot: `Bootstrap.cs`, `GameSession.cs`, `DebugCapture*.cs` (`DriveByAi`, `Think`, `RecordFrames`), `InputAdapter.BotHeld/BotPressed`
- RPG pure asmdef `Assets/Scripts/Rpg/*`; LiteNetLib https://github.com/RevenantX/LiteNetLib (MIT, pin 1.3.x)

## Overview
Priority P1, pending, 64h. LiteNetLib + run modes (offline / zone server / client; `host` = zone server that also renders and plays locally, dev only).
Refactor sim from "one hero + statics" to a `Zone` with N heroes. Login stub (name + class, no persistence). Server tick 30 Hz, AOI grid,
quantized snapshots 15 Hz, AOI-filtered events, view-only remote heroes/monsters/loot, dormant monster AI, shared loot with owner priority,
boss respawn timer. Multi-process localhost harness. Persistence = P3, multiple zones = P4.

## Key insights (from code)
- Single-hero statics: `HeroController.Instance` (Monster.Step target, MonsterSpawner avoid, LootDrop pickup, Hud), `GameSession.Hero` (RpgSession, Hud, RpgHud,
  TouchControls, camera), `RpgSession.Instance/Player` static hooks (`NotifyKill`, `Pickup`, `NotifyRespawn`, `SyncGear`).
- World statics: `Monster.All/Boss/Kills`, `MonsterSpawner.instance`, `LootDrop.All`, `FrostHollowMap` (static map; OK until P4 MapDef).
- `Monster.Think(hero)` single target; `Monster.TakeHit`, `SkillDamage.Radial`, `ComboProjectile.Strike` have no attacker -> no kill credit / damage table.
- Gameplay + FX interleaved (`HeroController.Combat.MeleeStrike`, `Monster.TakeHit` spawns DamageNumber/ImpactFx).
- `GameClock.HitStop` freezes the whole sim; `GameClock.Paused` freezes it while bag open -> invalid on a shared server.
- `HeroController.Init` needs a `HeroRenderer` (cls from renderer); `Monster.Init` loads sprite sheets -> server must run heroes/monsters without views (CPU/RAM at 200 players).
- `RpgSession.Attach` destroys the boss if the save has `bossDefeated` -> replaced by world boss respawn timer online.
- Frost Hollow = 576x384 units (36x24 tiles of 16); camera view ~240x160 units -> AOI cell 64 units (9x6 cells), radius 2 cells.

## Requirements
Functional
- Args: `--mode offline|server|host|client`, `--port 7777`, `--connect host:port`, `--name`, `--class`, `--max-players 200`, `--tick 30`, `--snap-div 2`,
  `--dev-key <secret>` (login stub gate), `--run-seconds N`, `--net-bot fight|wander`, `--net-stats <json>`, `--headless-views off`.
- Connect screen (IMGUI): name, address, class, [Join] [Host (dev)] [Offline].
- All players on the zone share monsters, spawns, boss; see each other within AOI.
- Loot: visible to everyone in AOI; `owner` = top damage dealer of the kill, owner-only pickup for 3 s (`LootPriorityMs`), then anyone; despawn 60 s.
- EXP: split by damage share among heroes that hit the monster (alive, same zone).
- Boss: Frostfang King respawns `BossRespawnMs = 15 min +- 2 min` after death (server timer); no per-save boss flag online.
- Monster AI dormant when no hero within `ActiveRadius = 320` units (skip Step; return home instantly when leash state? -> freeze in place, resume on wake).
- Inventory actions round-trip to server (no local mutation online).
- Reject when full / bad version / bad key.
Non-functional
- 50 players: server tick p99 < 20 ms (Windows batchmode), down <= 20 KB/s/client, up <= 1 KB/s/client, snapshot packet <= 1000 B (LiteNetLib max unreliable = 1023 B; was 1100).
- Offline feel unchanged (hit-stop, pause, saves).

## Architecture
Run modes
| Mode | Sim | Views/FX | Input |
|---|---|---|---|
| offline | local zone | yes | InputAdapter (today) |
| server | authoritative zone, 30 Hz fixed | none (no HeroRenderer, no sprites, no FX/UI/audio) | network per hero |
| host (dev) | server + renders, local hero via InputAdapter | yes | local + network |
| client | proxies only | yes | InputAdapter -> NetInputSender |
`NetMode.Current`, `IsAuthority`, `IsHeadless`, `RendersFx`.

Zone object (new `Zone` class; `Zone.Main` in P1)
- Owns: `Heroes`, `Monsters` (+ `Boss`, `Kills`), `Loot`, `MonsterSpawner` timers, `BossTimer`, `InterestGrid`, `NetEventBus`, `NetEntityRegistry`.
- Compat: `Monster.All` -> `Zone.Main.Monsters`, `Monster.Boss` -> `Zone.Main.Boss`, `LootDrop.All` -> `Zone.Main.Loot` (getters kept to limit edits; P4 swaps to entity's own zone).
- `HeroRegistry` in zone: `All`, `NearestAlive(pos, maxDist)`. `Local` (client/offline UI hero) is a separate static `LocalPlayer.Hero`; `HeroController.Instance` and `GameSession.Hero` forward to it.

Per-player RPG
- `PlayerRpg` (new, non-static): owns `PlayerState`; `GainExp`, `Pickup`, `UsePotion`, `Revive`, `ApplyInvAction`; raises `Changed`.
- `RpgSession` = offline adapter (SaveStore load/save/autosave, toasts) around one `PlayerRpg`. Server: one per `NetPlayer`, saves nowhere in P1.
- `Monster.damageBy` (Dictionary heroNetId -> dmg) -> on death: EXP split, loot owner = max.

Data flow
```
client InputAdapter -> NetInputSender (30 Hz: seq, held bits, press counters, facing; last 3 frames per packet) -> server
server NetPlayer.InputDriver -> hero.AiHeld/AiPressed (counter delta -> one Pressed() per tick; stale >250 ms = release)
Zone.Tick (30 Hz): heroes, active monsters, bolts, loot, spawner, boss timer -> gameplay hooks -> NetEventBus.Emit(event, pos, ownerOnly?)
every snap-div ticks per client: InterestGrid query (cells within r=2 of hero, leave at r=3 hysteresis) -> diff -> Spawn/Despawn (reliable)
  -> Snapshot (unreliable, prioritized by distance, cap 1100 B, overflow entities sent round-robin next snapshot)
events: per tick per client, filter by event pos within client's interest (or ownerOnly) -> EventBatch (reliable ordered)
client SnapshotBuffer (serverTimeMs, render at latest-150 ms) -> proxies; NetEventPlayer plays events at render time
```

### Protocol (`ZenoniaProto.Net`, LiteNetLib NetDataWriter/Reader, little endian)
`byte opcode` first. `ProtocolVersion = 1`. Position `short` = round(units*8). `uint serverTimeMs`. Ids: `ushort NetId` (per zone), `ushort PlayerId`.
Channels: ch0 unreliable (Input, Snapshot), ch1 reliable ordered (Spawn/Despawn/Events/RpgState), ch2 reliable ordered (chat, P5).

Client -> server
| Op | Name | Delivery | Fields |
|---|---|---|---|
| 1 | Hello | reliable | ushort protocolVersion, string name(<=16), byte heroClass, string token (P1 dev-key; P3 zone ticket) |
| 2 | Input | ch0 | uint ackSnapshotSeq, byte count(1..3), per frame: uint seq, byte heldBits(up,down,left,right,attack,dash,skill), byte attackCnt, dashCnt, skillCnt, hpPotCnt, mpPotCnt, byte facing(255 none) |
| 3 | InvAction | ch1 | uint reqId, byte op(Equip,Unequip,Use,Discard,Sort,Enhance), string itemUid, byte slot |
| 4 | Revive | ch1 | (none) - P1 auto-respawn timer, kept for later save-point choice |

Server -> client
| Op | Name | Delivery | Fields |
|---|---|---|---|
| 10 | Welcome | ch1 | ushort playerId, ushort heroNetId, byte zoneId, byte mapId, uint serverTimeMs, byte tickRate, byte snapDiv |
| 11 | Reject | then disconnect | byte reason(Full, Version, BadToken, AlreadyOnline), string text |
| 12 | Spawn | ch1 | ushort id, byte kind(Hero,Monster,Loot), byte type(class / MonsterDef index / LootKind), short x, short y, byte facing, + kind payload |
|  |  |  | Hero: ushort playerId, string name, byte level, Appearance{byte weaponTier, offTier, enhanceBand, flags(mechBlue)} |
|  |  |  | Monster: ushort hp, ushort maxHp, byte state | Loot: int amount, ushort itemDefId, byte rarity, byte enhance, ushort ownerPlayerId, uint freeAtMs |
| 13 | Despawn | ch1 | ushort id, byte cause(LeftAoi, Died, Removed, PickedUp, Logout) |
| 14 | Snapshot | ch0 | uint snapSeq, uint serverTimeMs, uint lastInputSeq, byte heroN, heroes[], byte monsterN, monsters[] |
| 15 | SelfState | ch1 (on change, <= 4/s) | ushort hp, maxHp, mp, maxMp, uint exp, byte level, float skillCdLeft |
| 17 | RpgState | ch1 owner | uint reqId(0 push), byte result, string text, string saveJson (`SaveSerializer.Capture`) |
| 20 | EventBatch | ch1 | uint serverTimeMs, byte n, events[] |

Snapshot entries: Hero 12 B = ushort id, short x, short y, byte facing\|flags(dead,dash,moving,invuln,hurt), byte pose(Idle,Walk,Dash,Attack,Hurt,Death,Cast), byte step, byte progress, byte hpPct.
Monster 10 B = ushort id, short x, short y, byte facing\|state, byte attackIdx\|airborne\|chilled, byte hpPct, byte deathProgress. (Exact HP via MonsterHit events; own HP via SelfState.)
Budget @ 40 heroes + 25 monsters visible = 730 B x 15 Hz = 11 KB/s + events ~4 KB/s.

Events (type byte; each carries implicit time of batch)
| T | Event | Payload |
|---|---|---|
| 1 | AttackStep | ushort hero, byte step, byte facing |
| 2 | BoltFired | ushort boltId, ushort hero, byte step, short x, y, byte angle, byte flags |
| 3 | BoltEnd | ushort boltId, short x, y, byte exploded |
| 4 | MonsterHit | ushort monster, ushort srcHero, ushort dmg, ushort hpLeft, byte flags(crit,finisher,quiet), sbyte knockX, knockY |
| 5 | HeroHit | ushort hero, ushort dmg, ushort srcMonster |
| 6 | SkillCast | ushort hero, byte skill, short x, y, byte facing, ushort targetId |
| 7 | HeroDied / 8 HeroRespawned | ushort hero (+ short x, y) |
| 9 | ExpGain (owner) | uint exp, byte levelsGained |
| 10 | LevelUp | ushort hero, byte level (AOI: others see the ring FX) |
| 11 | PotionUsed | ushort hero, byte hpOrMp |
| 12 | AppearanceChanged | ushort hero, Appearance |
| 13 | Toast (owner) | string text, uint rgba |
| 14 | BossSpawned / 15 BossDied | ushort id (zone-wide, not AOI filtered) |

Rates: sim 30 Hz fixed (`GameClock` fixed Dt 1/30 on server; `targetFrameRate=30`, vSync 0), snapshots 15 Hz, events every tick, input 30 Hz, LiteNetLib update 15 ms, timeout 8 s, ping 1 s.

## Related code files
Create (under `Assets/`)
- `Plugins/LiteNetLib/**` + asmdef
- `Scripts/Net/Protocol/` (asmdef `ZenoniaProto.Net`, no UnityEngine): `NetOpcodes.cs`, `NetQuantize.cs`, `NetInputFrame.cs`, `NetSnapshotTypes.cs`, `NetEventTypes.cs`, `NetSpawnTypes.cs`, `NetMessageWriter.cs`, `NetMessageReader.cs`
- `Scripts/World/Zone.cs`, `ZoneHeroes.cs`, `InterestGrid.cs`, `BossRespawnTimer.cs`, `LootOwnership.cs`, `PlayerRpg.cs`, `LocalPlayer.cs`
- `Scripts/Net/NetMode.cs`, `NetLaunchArgs.cs`, `NetEntityRegistry.cs`, `NetStats.cs`
- `Scripts/Net/Server/ZoneServer.cs`, `NetPlayer.cs`, `NetInputDriver.cs`, `ClientInterest.cs` (visible set + diff), `SnapshotBuilder.cs`, `NetEventBus.cs`, `ServerInventoryActions.cs`, `DevLoginStub.cs`
- `Scripts/Net/Client/NetClient.cs`, `ConnectScreenGui.cs`, `NetInputSender.cs`, `SnapshotBuffer.cs`, `ProxySpawner.cs`, `NetEventPlayer.cs`, `ClientRpgMirror.cs`
- `Scripts/HeroFx.cs` (cosmetic half of attack/bolt/skill/level-up), `Scripts/DebugCapture.Net.cs` (`--net-bot`, `--record-net`)
- `Tools/build-from-copy.ps1`, `Tools/net-local-test.ps1`, `Tools/net-check-summary.mjs`, `Tests/dotnet/ZenoniaNet.Tests/`

Modify
- `Bootstrap.cs` (mode branch; server skips camera/HUD/music/sfx/bloom/touch/FX inits), `GameSession.cs` (`SpawnNetHero`, `SpawnProxyHero`, LocalPlayer), `GameClock.cs` (fixed Dt server; HitStop/Paused only offline)
- `HeroController.cs`, `.Combat.cs`, `.Skill.cs`, `.ClassSkills.cs`, `.Animation.cs` (Init without view on headless; proxy mode; `Owner` PlayerRpg; events; FX via HeroFx; per-hero God)
- `Monster.cs`, `.Ai.cs`, `.Visuals.cs`, `.Airborne.cs` (zone lists, target = nearest alive w/ sticky aggro, dormancy, damage table, headless no sprites, proxy apply)
- `MonsterSpawner.cs` (zone-owned, avoid all heroes, boss via BossRespawnTimer), `SkillDamage.cs` (+src), `ComboProjectile.cs` (+src, boltId, cosmetic mode), `EarthBreakUltimate.cs`, `IceStormUltimate.cs` (cosmetic flag, +src)
- `LootDrop.cs` (zone list, owner/freeAt, authority pickup, client cosmetic), `RpgSession.cs` (wrap PlayerRpg, offline only), `InventoryPanel*.cs` (online -> InvAction)
- `Hud.cs`, `RpgHud.cs`, `TouchControls.cs`, `CameraFollow.cs` (LocalPlayer), `DebugCapture.cs` (net flags in arg gate)
- `Assets/Editor/FrostSmokeTest.cs` (multi-hero checks)

## Implementation steps (each step compiles; offline smoke + `--record-party` green after 3-8)
1. Vendor LiteNetLib + asmdef; verify Win + Android IL2CPP build.
2. Protocol asmdef + dotnet xunit tests (round-trip all messages, quantize bounds, input redundancy/counters, snapshot size caps).
3. `Zone` + `ZoneHeroes` + `LocalPlayer`: move Monster/Loot/Spawner statics behind zone, keep compat getters; per-hero `God`.
4. Monster targeting (nearest alive within aggro, sticky while alive + in leash) and dormancy (`ActiveRadius`); `Strike(target)`.
5. Attribution: `TakeHit(..., HeroController src)`, `SkillDamage.Radial(..., src)`, `ComboProjectile.Fire(..., src)`; damage table; EXP split; loot owner + `freeAt`.
6. `PlayerRpg` extraction; `RpgSession` offline adapter; `BossRespawnTimer` (online) vs save flag (offline).
7. Cosmetic split (`HeroFx`, `NetMode.RendersFx` guards: SlashEffect, SwordTrail, ImpactFx, BurstFx, DamageNumber, GameSfx, CameraFollow trauma, ScreenFlash, UltimateFx); `GameClock` rules.
8. Headless entities: `HeroController.InitHeadless(def, state)` (no HeroRenderer, Render no-op), `Monster` skips sprite/renderers when headless; measure 200 idle heroes tick time.
9. NetMode/args/Bootstrap branch.
10. Server: `ZoneServer` (listener, ConnectionRequest checks max/version/token, per-peer packet rate limit), `NetPlayer`, `NetInputDriver`, spawn at entrance + jitter, logout = despawn.
11. `InterestGrid` (cell 64, entity -> cell index updated on move) + `ClientInterest` (visible set, hysteresis, Spawn/Despawn diff) + `SnapshotBuilder` (priority by distance, byte cap, round-robin overflow) + `NetEventBus` (AOI filter by event pos, ownerOnly, zone-wide).
12. Client: `NetClient`, `ProxySpawner`, `SnapshotBuffer` (ring 32, clock offset smoothing, render delay 150 ms, lerp pos/progress, snap facing/pose, extrapolate <= 100 ms), `NetEventPlayer`, `ClientRpgMirror`, `SelfState` -> HUD.
13. Proxy modes: hero (`InitProxy`, Render from pose/step/progress, walk/idle local timers, appearance), monster (no Step), loot (cosmetic, owner tint for priority window).
14. `NetInputSender`; bag open -> zero input; InvAction round-trip.
15. Connect screen + host (dev) mode.
16. Harness + NetStats + summary checker; `--record-net` video from one client.
17. Android client on LAN smoke.

## Test matrix
| Level | What | How |
|---|---|---|
| Unit dotnet | protocol, quantize, input counters, snapshot cap, InterestGrid diff/hysteresis (pure logic in Protocol asmdef or linked file) | `dotnet test Tests/dotnet` |
| Editor | RpgLogicCheck; PlayerRpg EXP split, loot owner/priority, boss timer | existing editor checks + new cases |
| Integration | FrostSmokeTest: 2+ sim heroes, retarget on death, dormancy, attribution | batch from project copy |
| E2E localhost | 1 server + 8 Unity clients `--net-bot fight --run-seconds 120`: boss killed, all clients in AOI saw BossDied +-200 ms, 0 exceptions, budgets | `Tools/net-local-test.ps1` + `net-check-summary.mjs` |
| Scale smoke | 50 bots (Unity clients `-batchmode -nographics` 10 per machine run, or wait for P2 dotnet bots) - P1 gate uses 50 headless Unity clients | same script `-Clients 50 -Headless` |
| Visual | `--record-net` client video vs offline `--record-party` | manual |
| Device | Android client on LAN | manual |

Harness: `build-from-copy.ps1` robocopies project (excl Temp/Logs) to `..\zenoniaUnity-build`, runs `Unity.exe -batchmode -projectPath <copy> -executeMethod ZenoniaProto.EditorTools.PlayerBuildScript.BuildWindows -buildOut Build\Zenonia.exe`.
`net-local-test.ps1 -Clients N [-Headless]`: server `Zenonia.exe -batchmode -nographics --mode server --dev-key k --run-seconds 140 --net-stats server.json`,
clients `--mode client --connect 127.0.0.1:7777 --dev-key k --class <i%5> --net-bot fight --run-seconds 120 --net-stats c<i>.json`.
`--net-bot` reuses `DebugCapture.Think` against proxy monsters, feeding `InputAdapter.BotHeld/BotPressed` -> exercises the real NetInputSender path.

## Todo
- [x] LiteNetLib v1.2.0 + asmdef (Win compiled + built; Android IL2CPP build verification TODO)
- [x] Protocol asmdef + dotnet tests (34 pass)
- [x] Zone object + LocalPlayer + compat getters
- [x] Monster targeting + dormancy
- [x] Attribution, damage table, EXP split, loot owner/priority
- [x] PlayerRpg + boss respawn timer
- [x] Cosmetic split + clock rules
- [x] Headless heroes/monsters (no views)
- [x] NetMode/args/Bootstrap (+ -nographics bot clients: NetMode.NoViews)
- [x] ZoneServer, NetPlayer, InputDriver, dev login stub
- [x] InterestGrid/InterestSet (dotnet tests), ServerReplication (spawn diff, snapshot, events), NetEventBus
- [x] Client: NetClient, ProxySpawner, SnapshotBuffer (dotnet tests), EventPlayer, RpgMirror (no extrapolation: holds last state)
- [x] Proxy hero/monster/loot
- [x] InputSender (dedupes idle frames, 10 Hz keep-alive) + InvAction round-trip
- [x] Connect screen + host mode
- [x] Harness (Tools/net-local-test.ps1), NetStats, checker, record-net, dotnet swarm (Tests/dotnet/ZenoniaSwarm)
- [ ] Android LAN smoke (skipped: no device in this run; LiteNetLib IL2CPP still unverified)

## Success criteria
- 8-client E2E passes checker; 50-client scale smoke: server tick p99 < 20 ms, down <= 20 KB/s/client, no snapshot > 1100 B.
- 201st player (with `--max-players 200`) rejected Full; version mismatch rejected.
- Offline smoke + party video unchanged vs baseline.

## Risks
| Risk | L x I | Mitigation |
|---|---|---|
| Bandwidth: small map -> everyone in AOI at 50-200 players | H x H | per-packet byte cap + distance priority + round-robin; hpPct not hp; deltas in P2; heroes beyond 40 nearest at 1/3 rate |
| Unity batchmode perf (GameObject per entity, Update overhead) | M x H | headless no views/FX, dormant monsters, 30 Hz; profile 200 heroes in step 8; fallback: plain C# entity update loop called from one MonoBehaviour |
| Hidden FX/gameplay coupling (ultimates, Shadow Step, Heal) | H x M | grep every TakeHit/Radial/Spawn; SkillCast per skill; per-class record-net videos |
| Offline feel regression from refactor | M x H | offline path keeps hit-stop/pause/saves; baseline videos |
| Cheating (speed, damage, dupes, packet spam) | M x H | input only; press counters clamped (<= 4/frame); server-side cooldowns already in sim; InvAction validates uid/ownership; rate limit 120 pkt/s -> kick; malformed -> disconnect |
| Android UDP/NAT | M x H | client-server only (no P2P), server public IP (P8), keepalive ping 1 s, reconnect flow P4 |
| Event/snapshot timing mismatch | M x L | events time-stamped, played at render time |
| NetId reuse races | L x M | free ids after 10 s quarantine |

## Security
Dev-key login stub (constant-time compare) only until P3; names sanitized (<=16 printable); reader bounds checks, malformed -> disconnect, never throw into sim;
online client never writes SaveStore (`SaveStore.Enabled = false`).

## Rollback
Branch `net/p1-core`; steps 3-8 (refactor, offline-only effect) land first and revert independently; network code additive; `--mode` default offline.

## Next steps
P2 prediction + deltas + dotnet bot swarm; P3 real login/characters/persistence (Hello.token = zone ticket); P4 zones/portals.
