# Mechanic art integration (held gun + robot companion + icons)
Status: done (2026-10-29). Mirrors plans/261029-1121-cleric-class phase 5.

| Step | Result |
|---|---|
| Weaponless sheets | `python tools/build-mechanic-sheets.py` -> assets/hd/mechanic/weaponless/normalized-hd (idle walk attack1 attack3 dash hurt aligned per frame onto the baked armed set; dash/hurt resampled to armed source size). attack2 (wrong poses) + death skipped = baked armed frames (per-anim fallback, no `bare` entry) |
| Gun / robot cut | `python tools/cut-mechanic-gear-overlays.py [gun|robot|float]` (reuses warrior cutter + cleric `cut_float(cfg)`; width-based scale, template-matched +10 registration). Manifests src/data/mechanic-{gun,robot}-overlay-manifest-generated.js, mechanic-robot-float-manifest-generated.js; contact sheets assets/custom/mechanic-{gun,robot}-overlays/contact-sheet.png |
| Gun anchors | `python tools/estimate-mechanic-gun-anchors.py` (~20 s). IoU fit, any angle (0 = barrel right), negative `sx` = art mirrored about the grip (left facing). Mirrored anims (attack3/dash/hurt left) fitted on flipped right frames. QA sheets assets/hd/mechanic/qa; poor frames => `{fallback:true}` (armed baked frame). Overrides: tools/assets/mechanic-held-gear-anchor-overrides.json |
| Held gear | held-gear-specs `mechanic` (slot weapon), HELD_WEAPON_CLASSES += mechanic, sprite-registry heldGear.mechanic |
| Companion | src/entities/spirit-follow.js -> companion-follow.js, cleric-spirit-renderer.js -> companion-renderer.js (+ companion-specs.js); cleric + mechanic share it, `registry.companions[classId]`; visible only with offhand and alive |
| Icons | warrior-gear-icons.js also builds mechanic gun (tilted up-right) + robot icons; weapon-icons.js resolves them (bag/ground/attack button). paintedSkillsmechanic -> skill-icons-mechanic-roster.png; carpetBomb ultimate override only if roster missing |
| Tests/checks | tests/mechanic-art.test.js; `node tools/check-mechanic-art.mjs && python tools/stitch-wizard-held-staff.py tools/screenshots/mechanic-art` |

Known limits: 26/92 gun frames fall back to the baked gun (mostly attack3 beam frames, some walk/down); robot tiers 2-10 / +7 use static front art + code bob (only tier-1 plain has the float loop).
