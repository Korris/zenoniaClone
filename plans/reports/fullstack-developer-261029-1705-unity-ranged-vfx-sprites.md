# Unity: painted ranged VFX sprites (cleric / wizard / mechanic)

Project `C:\Project\zenoniaUnity`, branch `unity`, commit `5ecd790` (pushed to origin/unity).

**Status:** DONE_WITH_CONCERNS
**Summary:** Ranged basic attacks use the hand-painted sheets: cast at staff tip/barrel during windup, 4-frame projectile loop rotated to travel, impact row at the hit (cleric pillar upright), finisher 1.35x. Compile clean, smoke PASS, player builds, recordings reviewed.

## Built
- `Tools/slice-vfx-sheets.py`: gutter-based slicer (specks <12px dropped, alpha>10 projections), trims frames, per-row pivots (projectile = white-hot head, wizard = orb centre; cast = centre, mechanic = muzzle ring; impact = centre, cleric bottom-centre). Writes `Resources/Sprites/fx/vfx/<set>/{cast,proj,impact}_<n>.png` and `Resources/Data/vfx-<set>.json` (sets: cleric, wizard, mechanic-orange, mechanic-blue). Existing `ResourceTextureImportSettings` already covers the path (uncompressed, clamp, trilinear + mips; verified in the copy's .meta).
- `VfxSprites` (loader, `UnitsPerPx` 0.058 = projectile ~20 units, hero ~18), `VfxAnim` (pooled cast/impact player: normal alpha + faint additive copy `FeelConfig.PaintedGlowAlpha` 0.22, real-time so it plays through hit-stop), `ComboProjectile.Painted.cs` (loop + impact), `HeroController.Combat.cs` (`UpdateRangedCastFx` at windup start; procedural `FallbackCastFx` only if art missing).
- Finisher: 1.35x cast/projectile/impact + embers + existing shake/hit-stop. Normal hits: sprite + monster-hit sparks only.
- `ImpactFx`/`Monster.TakeHit` got `quiet` flag: with a painted impact the white flash quad and shock ring are skipped (they washed out the sprite and stacked 3 rings on a finisher). Monster sprite white flash unchanged.
- Mechanic C toggle swaps orange/blue sets. Old rigs (Arcane/Streak/HolySmite/MechanicFx) kept as fallback only.
- `DebugCapture.Combo.cs`: `--record-combo wizard|cleric|mechanic [--mech-blue] [--zoom N] [--dummies N] [--dummy-dist N]` 60 fps frame dump.
- Bloom threshold unchanged (1.25): base sprites are normal-blend, only the 0.22 additive copy feeds bloom; no washout seen on snow.
- Smoke test: new `CheckVfxSets` (4 sets x 3x4 frames, pivots in range).

## Verification (copy `...\scratchpad\unity-classes5\proj`)
Batchmode: zero error CS, `FrostSmokeTest` PASS (74 ok, all 5 classes + VFX sets), Windows player build succeeded, 4 combo recordings (450 frames each) inspected.

## Media (`plans/reports/unity-classes5-shots/`)
`ranged-vfx-sprites.png` (cast / projectile / impact / finisher rows: cleric, wizard, mechanic orange, mechanic blue), `wizard-combo.mp4`, `cleric-combo.mp4`, `mechanic-combo.mp4`, `mechanic-blue-combo.mp4`.

## Concerns
- Videos include the debug HUD; scene is random (monsters chase), so not every shot shows a clean flight.
- Large white blobs around hit monsters on finishers are the existing white hit-flash (esp. the white-furred Frostfang King), not the new sprites.
- Cast anim is 26 fps (~150 ms) and starts at windup (50-70 ms), so the muzzle blast frames land just after the shot leaves.
- Sprite size is a single constant (`VfxSprites.UnitsPerPx`); reference FX are larger relative to hero, brief asked 0.6-0.8 hero height so kept ~1.1x including tail.
- Unity Editor on the real project will import the new PNGs on next focus (metas are committed). No code-reviewer run, no unit tests beyond edit-mode smoke.
- Warrior/assassin melee untouched (their vfx sheets are only committed as sources under `Assets/ArtLibrary/vfx`).
