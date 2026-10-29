# Zenonia Unity prototype (warrior only)

Minimal playable port of the web game's red-haired Warrior, built to compare **smoothness**
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

## Controls

| Key | Action |
|---|---|
| WASD / Arrow keys | Move (8-direction input, 4-direction facing) |
| J / Space | Sword combo (4 hits; press during a swing to chain, the 4th is the heavy finisher) |
| K / Shift | Dash (brief invulnerability) |
| Q / E | Previous / next sword + shield tier (10 tiers) |
| R | Toggle enhanced gear art |
| B | Toggle bloom post-process (A/B) |
| F | Toggle all combat effects (A/B) |

Monsters (ice goblins + frost slimes) chase and strike. Die -> respawn at the start after 1.4 s. Killed monsters respawn after 6 s.

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
  Resources/Data/held-gear.json             sword/shield manifests + per-frame grip anchors (exported from the web data)
  Resources/Sprites/{hero,bare,sword,shield,monsters,tiles,fx}/   PNGs copied from the web project
  Scripts/                                  namespace ZenoniaProto (all < 200 LOC)
    Bootstrap.cs            RuntimeInitializeOnLoadMethod entry; builds camera, ground, hero, monsters, HUD
    GameConfig.cs / GameClock.cs / InputAdapter.cs
    SpriteSheet.cs / ProceduralSprites.cs   runtime sprite slicing + generated shadow / bar sprites
    HeldGearData.cs / WarriorRenderer.cs    paper-doll body + shield + sword at anchors (mirrors web held-weapon-renderer)
    WarriorController.cs / ComboSystem.cs   movement, dash, 4-hit combo (web COMBO_CONFIG), hurt/death/respawn
    Monster.cs / MonsterDef.cs / MonsterSpawner.cs
    DamageNumber.cs / SlashEffect.cs / CameraFollow.cs / Hud.cs / WorldBuilder.cs
    FeelConfig.cs / FeelMaterials.cs / FeelHotkeys.cs / FxSprites.cs   feel tuning, runtime materials, B/F toggles, generated FX sprites
    SwordTrail.cs / ImpactFx.cs / BloomEffect.cs                       swing ribbon, hit sparks + ring, camera bloom
  Resources/Shaders/                        SpriteFlash, SpriteAdditive, TrailRibbon, BloomLite (Built-in pipeline)
Tools/export-held-gear.mjs                  re-exports held-gear.json from the web project (node Tools/export-held-gear.mjs)
```

Menu `Zenonia > Fix Texture Import Settings` re-applies the texture settings if sprites ever look blurry or blocky.
