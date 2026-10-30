# Zenonia Unity prototype (Frost Hollow, 5 classes)

Playable port of the web game's **Frost Hollow** battle map with the **Warrior, Wizard, Mechanic, Assassin and Cleric** classes, built to compare **smoothness**
(Unity, vsync-paced GPU rendering) against the web Canvas2D version in `C:\Project\zenoniaClone`.

Everything is built at runtime by `Assets/Scripts/Bootstrap.cs` -- there is no scene setup. Press Play in any scene.

## Install and run

1. Install **Unity Hub**: https://unity.com/download
2. In Unity Hub > Installs > Install Editor > pick **6000.3.9f1** (Unity 6.3). No extra modules needed for Editor play; add
   *Windows Build Support (IL2CPP)* only if you want a standalone build.
3. Unity Hub > Add > Add project from disk > select `C:\Project\zenoniaUnity`. Open it with 6000.3.9f1.
   - First open takes a few minutes: it downloads packages (2D feature set, UGUI, Input System) and imports ~15 MB of PNGs.
   - If a "Enter Safe Mode" dialog appears, a script failed to compile: open the Console, copy the first error and report it.
4. Open any scene (the default empty one is fine) and press **Play**.
5. In the Game view tick **VSync** (toolbar, top of the Game tab) and set **Display 1 / Free Aspect** or 960x640 to match the web canvas.
   For the fairest smoothness test, build (File > Build Profiles > Windows > Build And Run) or at least maximize the Game view with
   *Maximize On Play* and keep the Profiler / Stats off.

## Classes, map, monsters

A class select screen opens first: click a card or press **1-5**. **F1-F5** respawn as Warrior / Wizard / Mechanic / Assassin / Cleric at any time.

| Class | Attack (J / Space) | Art |
|---|---|---|
| Warrior | 4-hit sword combo, red crescent slash + red trail; **L Earth Break** ultimate | sword + shield overlays (10 tiers) |
| Wizard | 3 magic bolts + arcane-burst finisher (AoE r26): layered violet-blue magic missile; **L Ice Storm** ultimate | staff overlays (10 tiers) |
| Mechanic | 3 rapid shots + plasma-cannon finisher (AoE r24): plasma bolts, muzzle blast, starburst + dust impact (orange, **C** = blue) | gun overlays (10 tiers), floating robot companion |
| Assassin | 3 quick slashes + double-stab finisher (two hits 90 ms apart, 6% extra crit): purple X-slash / thrust-streak dagger FX; **L Shadow Step** (blink behind the nearest foe, guaranteed crit) | dagger overlays in BOTH hands (`off` + `main` parts, 10 tiers) |
| Cleric | 3 holy shots + bursting finisher (AoE r26): green-gold light arrows, gold pillar impact; **L Heal** (38% max HP) | holy staff overlays (10 tiers), floating spirit companion |

Signature skills on **L** use MP (HUD MP bar + cooldown line): Earth Break 29 MP / 8 s, Ice Storm 20 MP / 7.7 s, Shadow Step 9 MP / 3.9 s,
Heal 12 MP / 8.5 s (web rank-3 values). Earth Break / Ice Storm are the web ultimates (`EarthBreakUltimate.cs`, `IceStormUltimate.cs`):
charge, sweep, slam that launches non-boss monsters for 1.5 s, fissure ticks; frost waves of falling ice spears that chill. Colour identity of the
basic attacks lives in one table, `ClassPalette.cs` (warrior red, wizard violet-blue, mechanic orange/blue, assassin purple, cleric green).

Combo timings, bolt speed / range / size and damage multipliers are copied from the web `COMBO_CONFIG_BY_CLASS`
(`ComboConfigs.cs`); class stats from `class-definitions.js` (`HeroClassDef.cs`). Ranged shots use the same game-feel layer as the sword
(hit-stop on hit, trauma shake on blasts, impact sparks + white flash, additive bolt / blast glow that feeds the bloom).

**Frost Hollow** (`FrostHollowMap.cs`): the web map's painted backdrop (`Sprites/tiles/frost-hollow-ground.png`, 36 x 24 tiles of 16 units)
and its tile classification (T pines / C cliffs / w river are solid, B bridge and s snow are walkable). Hero and monsters slide along the
solid tiles (web `tilemap.moveBox`). The hero starts at the entrance stairs (tile 4,21). **F9** tints the solid tiles to compare collision with the painting.

**Monsters** (`MonsterDef.cs`, `Monster.Ai.cs`, `MonsterSpawner.cs`): frost slime, ice goblin, spectral wolf in the web's five spawn zones plus the
**Frostfang King** (HP 580, ATK 33, render scale 1.65) waiting at tile (7,5) until the hero comes within 85 units (leash 120; boss bar at the top of the HUD).
AI = web `updateMonsterAi`: idle -> wander -> chase -> windup -> strike -> cooldown -> chase | return, with directional walk and attack frames
(attack frames 0/1 during the windup, 2/3 over the first 240 ms of the cooldown). Dead monsters respawn per zone (8-11 s) at a random walkable
tile at least 60 units from the hero; the boss does not respawn.

## Maps

Two maps, both 36x24 tiles of 16 units: **Oakhaven Village** (safe start hub: painted backdrop, HD props with footprint collision, 10 villagers, village music, no monsters)
and **Frost Hollow** (the battle map). `Maps/IGameMap` + `Maps/TileMapBase` hold the shared collision; `MapRegistry.Current` is the map the sim runs
(`FrostHollowMap.RectBlocked / Move / IsSolidTile` forward to it). The south gate of the village and the blue circle at the Frost Hollow entrance are portals:
walking onto one fades out, swaps the map (`Maps/MapTransfer`) and fades in with the same hero. New offline characters start in the village; a save
resumes on the map it was saved on. Offline only for now: online (host / client / server) always plays Frost Hollow with no portals or villagers.
Launch arg `--map frost-hollow|oakhaven-village` picks the start map (capture / bot runs default to Frost Hollow). Village art: `Resources/Sprites/village/`
(ground + portal in `tiles/`, props, NPC idle sheets + faces), copied from the web `Assets/hd/`.

## Controls

| Key | Action |
|---|---|
| 1-5 (or click) | Pick Warrior / Wizard / Mechanic / Assassin / Cleric on the start screen |
| F1-F5 | Respawn as Warrior / Wizard / Mechanic / Assassin / Cleric |
| L | Class skill: Earth Break / Ice Storm / Shadow Step / Heal |
| C | Mechanic only: FX colour orange / blue |
| WASD / Arrow keys | Move (8-direction input, 4-direction facing) |
| J / Space | Class combo (4 steps; press during a step to chain, the 4th is the finisher) |
| K / Shift | Dash (brief invulnerability) |
| Q / E | Previous / next weapon tier (10 tiers: sword + shield / staff / gun + robot / two daggers / staff + spirit; Q / E / R also change the companions) |
| R | Toggle enhanced gear art |
| B | Toggle bloom post-process (A/B) |
| F | Toggle all combat effects (A/B) |
| F9 | Show / hide the collision tint |
| E / Enter | Talk to Elder Thomas (near the spawn) / advance the dialogue / accept the quest; Enter otherwise focuses the chat (Enter sends, Esc leaves the field) |
| Esc | Close the dialogue / quest window |

Die -> respawn at the entrance after 1.4 s.

**In-game windows** (`WindowsHost.cs`, art `Sprites/ui/win-*`): top-centre toast banner (`NotificationToast`, `RpgHud.Toast`), NPC dialogue strip with typewriter
(`NpcDialogueBox`), quest offer window (`QuestOfferWindow`), local-only chat (`ChatWindow` / `ChatHub`, `IChatTransport` is the network seam; collapsed by default on phones).
Elder Thomas (`ElderNpc`) offers "Ancient Crystal Survey" (defeat 5 Frost Slimes, reward 150 G + 3 stones + 2 HP potions; `QuestTracker`, in memory, offline only).
While a window owns the keyboard `UiWindows.BlocksGameplay` makes `InputAdapter` drop the hero's keys.

## Game feel / VFX layer

All tuning lives in `Assets/Scripts/FeelConfig.cs` (hit-stop 45/80 ms, shake trauma, flash, trail, sparks, bloom threshold/intensity).
The HUD shows `[B] Bloom` and `[F] Effects` state under the controls. **F off** = plain hits (25/60 ms stop, red tint, no shake / white flash /
sword ribbon / sparks / shock ring / additive slash core / blade glow / damage-number pop). **B off** = no bloom.

- Hit-stop: `GameClock.Dt` is 0 for the stop, so hero + monsters freeze while camera, damage numbers, trail and sparks keep animating.
- Shake: trauma model (offset = trauma^2), camera offset only. White flash: `ZenoniaProto/SpriteFlash` shader (`_FlashAmount`).
- Sword ribbon: shared crescent mesh + `TrailRibbon` shader (white core to orange to red). Sparks: pooled code-built ParticleSystem
  (stretched additive) + flash quad + shock ring (crit / finisher). Slash and enhanced blade get an additive copy.
- Bloom: Built-in pipeline `OnRenderImage` (`BloomEffect` + `BloomLite` shader), camera runs HDR; additive FX are pushed above 1.0 so only they glow.
- Shaders live in `Assets/Resources/Shaders/` (loaded by name, so they are included in builds; missing shader falls back to a built-in one).

**Painted ranged FX** (wizard / cleric / mechanic): hand-painted sprite sheets in `Assets/ArtLibrary/vfx/` are sliced by `Tools/slice-vfx-sheets.py`
(transparent-gutter detection, per-row pivots) into `Resources/Sprites/fx/vfx/<set>/{cast,proj,impact}_<n>.png` + `Resources/Data/vfx-<set>.json`.
`VfxSprites` loads a set, `VfxAnim` plays cast (at staff tip / barrel, windup) and impact rows, `ComboProjectile.Painted.cs` loops the 4-frame projectile
rotated to travel (pivot on the bright head). Base sprite = normal alpha blend, plus a faint additive copy (`FeelConfig.PaintedGlowAlpha`) for bloom. Finisher = 1.35x.
Mechanic C swaps `mechanic-orange` / `mechanic-blue`. Missing art falls back to the procedural rigs. Re-run the slicer after changing a sheet.
Video: `--record-combo wizard|cleric|mechanic [--mech-blue] [--zoom N]` (60 fps frame dump, like `--record-ultimate`).

## What to compare with the web version

- **FPS / max** readout (top-left): same format as the web (`FPS 60 · max 17ms`, refreshed every 0.5 s). "max" is the worst frame in
  the window; a stable, low max is what feels smooth.
- Walk in circles through the monster pack and fight: hit-stop, knockback, camera follow (smooth-damp), HD sprite scaling.
- Sprite quality: Unity uses trilinear mipmapped sprites; the web uses a pre-built downscale chain.
- Scale: 1 Unity unit = 1 web world px; the camera shows 160 units vertically (web: 960x640 canvas at 4x => 240x160 world px).

## Project layout

```
Assets/
  Editor/ResourceTextureImportSettings.cs   forces texture import settings (no hand-written .meta files)
  Resources/Data/held-{gear,wizard,mechanic,assassin,cleric}.json   overlay manifests + per-frame grip anchors (+ robot) exported from the web data
  Resources/Sprites/{hero,bare,sword,shield,wizard,wizard-bare,wizard-staff,mechanic,mechanic-bare,mechanic-gun,mechanic-robot,mechanic-robot-float,monsters,tiles,fx}/   PNGs copied from the web project
  Scripts/                                  namespace ZenoniaProto (all < 200 LOC)
    Bootstrap.cs            RuntimeInitializeOnLoadMethod entry; builds camera, ground, monsters, HUD, session
    GameSession.cs / ClassSelectGui.cs      class select screen, hero spawn + F1/F2/F3 switch
    HeroClassDef.cs / ComboConfigs.cs       class stats, sheet folders, gear parts, combo chains (web values)
    FrostHollowMap.cs / WorldBuilder.cs     Frost Hollow tile grid, collision sweep, spawn zones, painted ground
    ComboProjectile.cs / BurstFx.cs         ranged bolts / bullets / blasts + pooled FX sprites
    HeroCompanion.cs / GearOverlays.cs      shared companion (mechanic robot, cleric spirit: follow / bob / float loop), overlay sprite cache
    DebugCapture.cs                         command-line verification flags (see below)
    GameConfig.cs / GameClock.cs / InputAdapter.cs
    SpriteSheet.cs / ProceduralSprites.cs   runtime sprite slicing + generated shadow / bar sprites
    HeldGearData.cs / HeroRenderer.cs       paper-doll body + overlays at per-frame anchors (mirrors web held-weapon-renderer)
    HeroController*.cs / ComboSystem.cs     movement, dash, class combo, hurt/death/respawn
    Monster*.cs / MonsterDef.cs / MonsterSpawner.cs
    DamageNumber.cs / SlashEffect.cs / CameraFollow.cs / Hud.cs / WorldBuilder.cs
    FeelConfig.cs / FeelMaterials.cs / FeelHotkeys.cs / FxSprites.cs   feel tuning, runtime materials, B/F toggles, generated FX sprites
    SwordTrail.cs / ImpactFx.cs / BloomEffect.cs                       swing ribbon, hit sparks + ring, camera bloom
  Resources/Shaders/                        SpriteFlash, SpriteAdditive, TrailRibbon, BloomLite (Built-in pipeline)
Tools/export-held-gear.mjs                  re-exports the three held-*.json files from the web project (node Tools/export-held-gear.mjs)
Assets/Editor/FrostSmokeTest.cs             headless smoke test (-executeMethod ZenoniaProto.EditorTools.FrostSmokeTest.Run)
Assets/Editor/PlayerBuildScript.cs          batchmode Windows player build (-executeMethod ...PlayerBuildScript.BuildWindows -buildOut <exe>)
```

Menu `Zenonia > Fix Texture Import Settings` re-applies the texture settings if sprites ever look blurry or blocky.

## Verification flags (player build)

`DebugCapture` is inert unless a flag is present: `--class warrior|wizard|mechanic|assassin|cleric`, `--tp tx,ty`, `--face down|up|left|right`, `--tier N`,
`--enhanced`, `--bot fight|attack|walk`, `--zoom N`, `--solids`, `--shots 3,5,8 --shot-path <prefix>` (PNG at those seconds, then quit), `--skill-at 1.5,9` (press L), `--dummies N --dummy-dist D` (slime targets), `--god`,
`--nobloom`, `--mech-blue`, `--perf --run-seconds N` (logs frames slower than 16 ms), `--record-ultimate warrior|wizard` (60 fps PNG sequence for ffmpeg).
Example: `Zenonia.exe -screen-width 1280 -screen-height 720 --class wizard --tp 27,10 --bot fight --shots 4,6 --shot-path C:	mp\wiz`.
