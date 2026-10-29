# Unity game feel + VFX (261029)
Status DONE_WITH_CONCERNS. Project C:\Project\zenoniaUnity, Built-in pipeline (no SRP asset), Gamma color space.
New: FeelConfig, FeelMaterials, FeelHotkeys, FxSprites, SwordTrail, ImpactFx, BloomEffect (.cs) + Resources/Shaders/{SpriteFlash,SpriteAdditive,TrailRibbon,BloomLite}.shader.
Edited: Bootstrap, CameraFollow (trauma shake), DamageNumber (pop/crit), Hud (toggle line), InputAdapter (B,F), Monster(+Visuals), SlashEffect (additive core), WarriorController.Combat, WarriorRenderer (enhanced blade glow), README.
Verify: editor was open (batch locked) -> copied project to scratchpad, ran batchmode: compile clean (0 error CS), 4 shaders supported/no messages, edit-mode smoke spawn of trail/impact/materials ok. NOT play-tested visually.
Concerns: tuning unseen in motion; hit-stop = existing GameClock.Dt freeze (global combat sim, not per-entity). Commit excludes user's pre-existing Packages/manifest.json change and .meta files.
