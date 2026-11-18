# RO Soft 8-direction hero, default outfit (19/11/2026)

User approved the 5-class concept (`docs/reference/ro-soft-8dir-5-classes-sheet.png`). Decisions 04:05:
hair and weapon BAKED into the body (hair layer later), first pass = idle + walk + 1 attack per direction, menu merged.

## Art (per class, ChatGPT Tay, chat https://chatgpt.com/c/6afe0787-01c8-83ec-b422-ad676e70b9f6)
- idle: 1 frame per direction (have it: `ArtLibrary/classes/<c>/ro-soft-8dir/cut/idle-<dir>.png`), breathing in code.
- walk: 4 frames x 8 directions; attack: 3-4 frames x 8 directions (wind-up, hit, follow). Draw in strips that include the approved idle frame as reference; left directions drawn, not mirrored.
- Sheet format: rows = 8 directions (down, down-right, right, up-right, up, up-left, left, down-left), cell 256, feet on a fixed baseline, figure height = idle height.
- Install: `Assets/Resources/Sprites/classes/<c>/body/soft/{idle,walk,attack}.png` (+ .meta like other sheets).

## Code (findings: Explore report in this session, summary)
- New partial `HeroRenderer.Soft.cs`: when the class has soft sheets (and flag on), Init loads them, keeps shadow/lean/body/dash trail, skips parts/head/hand. `Show`, `FrameCount`, `HasAnim` branch first. PPU = bodyHeightPx / 20.5, pivot at feet row.
- Anim mapping: every combo/attackN/melee*/cast/skill pose/spin -> attack (by progress); dash -> walk; hurt -> idle (+tint); death -> idle fade.
- Facing: sim stays 4-dir (`HeroController.facing`, hitboxes, RangedAimRules, LocalHeroPredictor bits). Add view-only `dir8`: local hero from `ReadMove()` when moving and not attacking; while attacking use dir8 only if it maps to the current cardinal facing, else the cardinal.
- Proxies: Option A, no protocol change: dir8 from interpolated position delta while Moving/Dash, keep last otherwise, snap to cardinal when facing changes. No server rebuild.
- Callers to check: HeroPortraitView (bag, armory, training hall), HeroPreviewStage, HeroSelectPreview, ShadowCloneFx (indexes 4-row Assassin sheet directly), HeroPortraitLayers (HUD portraits from hair art), FrostSmokeTest.
- Muzzle/cast-tip tables (HeroFx, RangedAimRules) are measured on the old art: leave as is for v1, note visual offset.

## Order
1. Warrior art (walk, attack) -> install -> code path -> build from copy -> capture 8 directions in game.
2. Other 4 classes art + install.
3. Hair layer, more anims (dash, hurt, death, combo variety) later.

## Unresolved
- Diagonal swing drawn diagonally while the hitbox stays cardinal: accepted for v1?
- Re-measure muzzle tables later?
