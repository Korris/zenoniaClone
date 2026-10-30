# Phase 1 steps 1-8 report

## Per step
1. LiteNetLib: latest tag is v1.2.0 (plan's "1.3.x" does not exist). Library source + LICENSE.txt + upstream asmdef in `Assets/Plugins/LiteNetLib` (+VENDORED.txt). Compiles, Win player builds. TODO: Android IL2CPP build not verified.
2. `Assets/Scripts/Net/Protocol` (asmdef `ZenoniaProto.Net`, noEngineReferences): NetOpcodes, NetQuantize, NetInputFrame (InputRedundancy, ServerInputState), NetSnapshotTypes (+SnapshotPacker, cap 1100 B, round-robin cursors), NetEventTypes, NetSpawnTypes, NetMessageWriter/Reader (Try* never throw). Tests: `Tests/dotnet/ZenoniaNet.Tests` (xunit, compiles sources by glob): `dotnet test` = 34 pass.
3. `World/Zone`, `ZoneHeroes` (NetId, NearestAlive, AnyWithin), `LocalPlayer`. Monster.All/Boss/Kills, LootDrop.All forward to Zone.Main. HeroController.Instance / GameSession.Hero forward to LocalPlayer. `DebugGod` static replaced by per-hero `God` (DebugCapture callers updated).
4. Monster: sticky nearest-alive target (AcquireTarget), `ActiveRadius=320` dormancy, Strike(target).
5. Attribution: TakeHit(..., src), SkillDamage.Radial(..., src), ComboProjectile.Fire(..., src), Earth Break / Ice Storm / Shadow Step pass src. Damage table `Monster.DamageBy` (overkill capped). `World/KillRewards` = EXP split + loot owner (online only) + boss. `LootOwnership` (3 s, +2 s party), LootDrop ownerId/freeAt, pickup by any hero with a PlayerRpg.
6. `PlayerRpg` (events, ApplyInvAction), `RpgSession` = offline adapter (NotifyKill/NotifyRespawn replaced by PlayerRpg events), `BossRespawnTimer` (30 min, online only; offline keeps save flag), `MonsterSpawner.SpawnBoss`.
7. `HeroFx` (static cosmetic half), `NetMode` (Current/IsAuthority/IsHeadless/RendersFx), RendersFx guards on SlashEffect, SwordTrail, ImpactFx, BurstFx, DamageNumber, GameSfx, CameraFollow trauma/punch, ScreenFlash, UltimateFx, ShockRing; ultimates skip art when headless. GameClock: hit-stop/pause offline only, fixed 1/30 Dt on server, advances Zone clock.
8. `HeroController.InitHeadless`, Render/gear no-op without view, Monster/LootDrop/ComboProjectile skip renderers when headless. Minimal Bootstrap branch (`--mode server`: no camera/FX/audio/HUD) + `Net/HeadlessBench.cs` (`--headless-bench N --bench-seconds S [--bench-brawl]`).

## Results
- Compile (merged-assembly check) clean; Win player build Succeeded, 0 errors.
- Headless tick (per frame, 1 frame = 1 tick, uncapped batchmode -nographics, 19 monsters, heroes spread):
  - 200 idle: avg 0.066-0.070 ms, p99 0.175-0.202 ms, max 3.8-6.1 ms
  - 50 idle: avg 0.026 ms, p99 0.085 ms; 0 heroes: 0.010 ms
  - brawl 200 (heroes attack, kills/loot/EXP run headless): avg 0.155 ms, p99 0.518 ms, no exceptions
- FrostSmokeTest (editor, on project copy): new ZoneLogicCheck (boss timer, loot priority, EXP split, 2-hero targeting, sticky aggro + retarget on death, dormancy, attribution, dead contributor, loot owner, boss timer online vs offline) all ok. Only failures = 4 Cleric heal checks, which fail identically on HEAD baseline (pre-existing).
- Offline player vs HEAD baseline: `--class warrior --god --bot fight --shots 3,6` identical (hero (72,34), same hp, 19 monsters). `--record-party`: "recorded boss fight 720 frames, boss down", 0 exceptions, frames show damage numbers, EXP, level-up, loot, pickups; compared visually with baseline build (similar; RNG differs).

## Behaviour notes
- Party capture: monsters now target the nearest hero (party members too), not only the player. Offline player EXP/loot unchanged (party members have no PlayerRpg; offline loot has no owner).
- Dormant monsters (>320 from every living hero) freeze: only visible effect is far-away monsters not wandering.
- Added `HeroController.Retire()` called on class switch so the old hero cannot loot/aggro for the rest of the frame.
- New files got .meta copied from the copy project's generated ones. Repo line endings mixed (autocrlf); new files LF.
- Did not touch the coordinator's HeroClassDef / sword art / held-gear files (final sync re-copied the repo, which includes them).

## Deferred / unresolved
- Android IL2CPP verification of LiteNetLib.
- Per-hero camera trauma is still global (any hero hurt/hit shakes the local camera, as before).
- Docs under ./docs not updated.
