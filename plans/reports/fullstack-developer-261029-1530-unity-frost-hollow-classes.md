# Unity prototype: Frost Hollow + Wizard/Mechanic

Project: `C:\Project\zenoniaUnity` (commit `c1e8e36`, local git, no remote). Unity 6000.3.9f1, Built-in, runtime-built.

**Status:** DONE_WITH_CONCERNS
**Summary:** Frost Hollow map (painted backdrop + web tile collision), web-AI monsters (wander/chase/leash, directional attack, respawn zones, Frostfang King + HUD boss bar), Warrior/Wizard/Mechanic with class select + F1/F2/F3, ranged combos with game-feel, mechanic robot companion. Compiles clean, edit-mode smoke passes, player build runs and was screenshot-verified for all 3 classes.
**Concerns:** see end.

## What was built
- Map: `FrostHollowMap.cs` (36x24 rows copied from web `frost-hollow-map.js`; T/C/w solid, B/s walkable; 1-unit axis sweep like `tilemap.moveBox`; spawn zones; boss tile 7,5; player start 4,21). `WorldBuilder.cs` loads `Sprites/tiles/frost-hollow-ground.png` (ground.webp converted, 2304x1536, 64px/tile). F4 = collision tint (verified it lines up with the painting).
- Monsters: `MonsterDef` (frostSlime/iceGoblin/spectralWolf/frostfangKing stats from web: HP/ATK/DEF/speed/itemLevel, aggro, leash, windup/cooldown; King scale 1.65, HP 580, ATK 33, leash 120, aggro 85, stationary until aggro). `Monster.Ai.cs` = web `updateMonsterAi` (idle/wander/chase/windup/cooldown/return, wander+chase at 0.5 speed factor like web). Directional walk + attack sheets (frames 0/1 windup, 2/3 over first 240 ms cooldown), tile collision, knockback (boss not shoved), white flash. `MonsterSpawner` = per-zone respawn timers (8-11 s, random walkable tile >=60 from hero), boss never respawns. HUD boss bar (top centre).
- Classes: `HeroClassDef` (stats from web class-definitions, sheet folders, per-class mirrored-left anim list, gear parts), `ComboConfigs` (web COMBO_CONFIG_BY_CLASS values), `ComboSystem` now takes hits + chain window and `BoltDef`. `HeroRenderer` (renamed from WarriorRenderer) is data-driven: baked vs weaponless body, per-frame anchors with hidden/behind/fallback rule, flipX for mirrored rows. `HeroController*` (renamed) handles any class + tile collision. `GameSession` + `ClassSelectGui` = start screen (1/2/3 or click) and F1/F2/F3 switch.
- Ranged: `ComboProjectile` (sub-stepped, first-hit or AoE blast r26/r24, crit roll on impact, knockback away from blast), `BurstFx` (muzzle flash, arcane burst, explosion; web FX art at 4 px/unit). Feel: hit-stop (30/80 ms), trauma shake on blast + recoil, ImpactFx sparks/flash via `Monster.TakeHit`, additive glow copy on bolts/blasts.
- Mechanic robot: `RobotCompanion` (web COMPANION_OFFSETS with y flipped, 160 ms ease, bob 1.3 / 0.6 for float loop, snap >90, y-sorted, tier-1 unenhanced = 4-facing float loop else tier art). Q/E tier + R enhance apply to weapon and robot.
- Data export: `Tools/export-held-gear.mjs` now writes `held-gear.json`, `held-wizard.json`, `held-mechanic.json` (array form, JsonUtility).
- Art copied into `Assets/Resources/Sprites/`: wizard, wizard-bare, wizard-staff, mechanic, mechanic-bare, mechanic-gun, mechanic-robot, mechanic-robot-float, fx/wizard, fx/mechanic, monsters (+wolf, king), tiles/frost-hollow-ground.
- Verification helpers: `Assets/Editor/FrostSmokeTest.cs`, `Assets/Editor/PlayerBuildScript.cs`, runtime `DebugCapture.cs` (command-line flags only; inert otherwise). README updated (classes, map, controls, flags).

## How verified (all on a COPY, real project untouched by batchmode)
Copy: `C:\Users\GIGA\AppData\Local\Temp\claude\C--Project-zenoniaClone\1d7439af-3382-431f-a9f3-64668a535d26\scratchpad\unity-frost\proj` (helper scripts `sync.sh`, `run-unity.sh`, `shot.sh` alongside).
- Batchmode compile: zero `error CS`, no shader errors (logs `compile3.log`, `pbuild4.log` in that dir).
- `-executeMethod ZenoniaProto.EditorTools.FrostSmokeTest.Run` -> `SMOKE RESULT: PASS` (`smoke4.log`): start/boss/zone tiles walkable, river+cliff solid, hero can step onto the bridge and cross it, banks block; per class: world built, 19 monsters (18 + boss), hero spawned, gear JSON loaded (10 tiers), 512 frames drawn (all anims/dirs/frames, tier 0 and 9, base/enhanced), overlays present, robot created.
- Windows player build succeeded (`pbuild4.log`), ran at 1280x720 with the debug flags, 144 FPS, no exceptions in player logs.

## Screenshots (copied to `C:\Project\zenoniaClone\plans\reports\unity-frost-shots\`)
- `select_1.png` class select screen
- `warrior-atk-m.png`, `wizard-atk-m.png`, `mechanic-atk-m.png` combat montages (slash arcs, magic bolt + arcane burst, bullets + plasma blast, goblin/slime/wolf)
- `mi-m.png` mechanic gun + robot in 4 facings; `t7-m.png` tier 7 enhanced wizard staff + mechanic gun/robot
- `solids_1.png` collision tint vs painting (bridge)
- Full set (incl. first-pass fight shots): `...\scratchpad\unity-frost\shots\`

## Fixes made from looking at screenshots
- HUD text unreadable on snow -> dark backing panel; boss bar text clipped -> taller bar.
- Arcane burst/explosion blew out to pure white -> additive core of blasts reduced to 22% alpha (still blooms; centre remains bright by design).
- Test bot stuck behind cliffs -> line-of-sight + skip-boss targeting (test tooling only).

## What the user should do
Switch to the Unity Editor, let it recompile and import the new PNGs (first import a few minutes, ~50 MB of art; `.meta` files are generated and committed in the next save), press Play. Pick a class with 1/2/3 or click; F1/F2/F3 re-spawn as another class; F4 shows collision. Try Q/E/R on each class (mechanic also changes the robot), walk the stone path to the bridge, then fight the northwest clearing (Frostfang King at the west cave).

## Concerns / unresolved
- Monster chase/wander speed uses the web 0.5 factor (slimes 15 u/s, goblins 22, wolves 31, King 18), so they are slow vs hero 80; this is web parity, change `ChaseSpeedFactor` in `Monster.Ai.cs` if it should feel tougher than web.
- King hits hard (ATK 33 vs hero 80-100 HP): bot runs with a melee hero died quickly; real play needs dash/kiting. No levelling, loot, EXP or potions were ported (not requested).
- Wizard/mechanic bolts ignore cliffs/trees (web parity, projectiles have no tile check). Melee warrior hits likewise ignore walls.
- Hero/boss spawn collisions: boss hitbox 22x14 at the post tile; not seen stuck, but monsters pushed into walls by knockback just stop (no unstuck).
- The `Packages/manifest.json` change (Input System package removed), `.vsconfig`, `packages-lock.json` and ProjectSettings assets were already uncommitted before this task and were left out of my commit.
- IL2CPP not used (Mono standalone build). Select-screen shot at t=2s showed a layout offset at window start; later frames are fine (window resize at launch).
- `git add` warned about LF->CRLF conversion on committed .cs files (autocrlf), harmless.
- Not run: `code-reviewer` agent, formal unit tests for `ComboSystem` (smoke + player runs only).

## Files
New: `FrostHollowMap.cs, HeroClassDef.cs, ComboConfigs.cs, GameSession.cs, ClassSelectGui.cs, ComboProjectile.cs, BurstFx.cs, RobotCompanion.cs, GearOverlays.cs, Monster.Ai.cs, DebugCapture.cs` (Assets/Scripts); `Assets/Editor/FrostSmokeTest.cs, PlayerBuildScript.cs`; `Assets/Resources/Data/held-wizard.json, held-mechanic.json`.
Changed: `Bootstrap, GameConfig, WorldBuilder, MonsterDef, Monster, Monster.Visuals, MonsterSpawner, ComboSystem, HeldGearData, InputAdapter, Hud, FeelConfig`, `HeroRenderer/HeroController*` (renamed from Warrior*), `Tools/export-held-gear.mjs`, `README.md`.
