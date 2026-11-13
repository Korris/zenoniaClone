# Cliff Harpy ranged attack (monster projectile) — 2026-11-13

Status: DONE_WITH_CONCERNS. Works offline (seen in game). Online path coded + protocol test, NOT run against a server. Nothing committed, nothing deployed.

## How it works

- `MonsterDef.Ranged` (`MonsterRangedProfile`, null = melee). Only `CliffHarpy` sets it: Range 100, speed 130 u/s, MaxTravel 150, hit square 6, cream tint. Atk / HP / timings unchanged; no other monster touched.
- Chase: ranged monster enters Windup when target <= Range AND line of sight clear (no solid tile between); else keeps walking closer like melee. No kiting: point blank it still shoots.
- Windup / Cooldown states and attack frames reused. Windup end -> `Monster.Strike` -> `FireRangedShot`: aims at hero's position at that instant (feet + 4), angle rounded to wire step (1.4 deg) so server and clients fly the same line.
- `MonsterProjectile`: straight flight in 2-unit sub-steps; ends on solid tile, first living hero whose 10x8 body box it touches, or MaxTravel. Authority calls `hero.TakeDamage(CombatMath.Damage(def.Atk, 1, DefStat))` (same formula as melee; existing HeroHit event, invuln / dash / god rules apply). Client: picture only.
- Online: new event `NetEventType.MonsterShot = 19` (monster id, start x/y, angle), AOI-filtered. Client `NetEventPlayer` -> `Monster.PlayRangedShot`. `NetProtocol.Version` 18 -> 19 (old client cannot parse a batch holding the new event).
- Cleanup on map change: `MonsterProjectile.ClearAll()` in `MapTransfer.ClearEntities` (offline) and `ProxySpawner.ClearForTransfer` (client).

## Projectile look (placeholder)

Procedural quill generated in code (`MonsterProjectile.Quill()`): 20x7 px white feather shape with dark rim, 10x3.5 units, tinted `0xF2E3B0`, rotated to flight direction, soft ground shadow. No ChatGPT art, no existing fx sprite used (Sprites/fx has only portal + wanted warnings).
Replace later: drop `Assets/Resources/Sprites/fx/monster-shots/cliff-harpy-feather.png` (painted flying to screen-right, 8 px per unit); code picks it up automatically, untinted.

## Files

New:
- `Assets/Scripts/Rpg/MonsterRangedRules.cs` (profile + engine-free rules)
- `Assets/Scripts/Monster.Ranged.cs`
- `Assets/Scripts/MonsterProjectile.cs`
- `Tests/dotnet/ZenoniaSkills.Tests/MonsterRangedRulesTests.cs` (8 tests)

Edited:
- `Assets/Scripts/MonsterDef.cs` (Ranged field, CliffHarpy profile, comment)
- `Assets/Scripts/Monster.Ai.cs` (Chase check), `Assets/Scripts/Monster.cs` (Strike: 1 line)
- `Assets/Scripts/HeroController.cs` (public `Box`)
- `Assets/Scripts/Net/Protocol/NetEventTypes.cs`, `NetOpcodes.cs` (Version 19), `Assets/Scripts/Net/NetEventBus.cs`, `Assets/Scripts/Net/Client/NetEventPlayer.cs`, `ProxySpawner.cs`
- `Assets/Scripts/Maps/MapTransfer.cs`
- `Tests/dotnet/ZenoniaNet.Tests/MessageRoundTripTests.cs` (MonsterShot round trip, version 19)

New .cs files have no `.meta` yet (the open Unity editor will generate them; add them to the commit).

## Tests

- `dotnet test Tests/dotnet/ZenoniaSkills.Tests`: 305/305 (297 + 8 new)
- `dotnet test Tests/dotnet/ZenoniaNet.Tests`: 387/387 (existing event round-trip test extended)
- `Tools/build-from-copy.ps1`: Succeeded, errors 0.

## Seen in game (offline, warrior, `--map sunbreakFoothills --tp 28,14 --god`, 30 shots at 0.1 s)

- `plans/reports/ranged-monster-261113-shots/shot_7.png` (full frame), `zoom-feather.png` (3x crop of two consecutive frames).
- Saw: harpies stop at a distance from the hero (not walking into melee like the lizards next to them), play attack pose, cream feather with shadow in flight from harpy toward hero, further along in the next frame.
- Feather is small and pale on the bright grass: readable but subtle. Placeholder quality.
- Build exe and capture folders in C:\temp deleted; lock file removed.

## Not verified

- Online (server + client): MonsterShot event replay, picture vs server hit timing. Client picture plays at render time (~150 ms behind), local hero is predicted, so a hit may land slightly before the feather visibly arrives.
- Damage number on a non-god hero, death by feather; feather stopping at a wall in game (unit-tested only).
- Old Frost Hollow zones: MapSpawnTable has cliffHarpy only in Sunbreak; none found in Frost Hollow tables, not checked in game.
- Harpy behind a stream / on a ledge: no line of sight -> walks into the edge and waits (same as melee monsters). Seen once at tp 31,16 (harpy on a rock ledge, no shots).

## Shipping note

Protocol (v19, new event) + monster table changed: client, zone server, server-api ship together; Linux server must be rebuilt. Nothing deployed.

## Open questions

1. Water tiles block feathers (they are solid tiles). OK, or should shots cross streams?
2. Range 100 / speed 130 / same Atk as melee: fine, or lower damage since it now hits from range?
3. No shot sound and no impact picture yet (only the normal hurt number). Wanted?
4. Feather size / contrast OK until real art?
