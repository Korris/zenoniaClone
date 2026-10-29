# New red-haired warrior + held sword/shield overlays (2026-10-29)
Status: implemented (uncommitted). Mirrors the wizard staff paper-doll, generalized to multi-part gear.

| Step | Files | Status |
|---|---|---|
| Normalize armed/weaponless/bare to 192px, align per frame, face crop | tools/build-warrior-v2-sheets.py, tools/hero_set_align.py -> assets/hd/warrior-v2/{normalized-hd,weaponless,bare,normalized} | done |
| Cut 10 swords + 10 shields (base/+10), manifests | tools/cut-warrior-gear-overlays.py -> assets/custom/warrior-{sword,shield}-overlays, src/data/warrior-*-overlay-manifest-generated.js | done |
| Anchors (template fit, overrides, previews) | tools/estimate-warrior-held-gear-anchors.py, held_gear_fit.py, held_gear_preview.py, src/data/warrior-held-anchors-generated.js | done |
| Renderer generalized | held-gear-specs.js (new), held-weapon-registry/renderer/config, asset-atlas-held-weapon, sprite-registry (heldGear[classId]), player-renderer | done |
| Warrior baked set switched | asset-atlas.js HD_HERO_DIR -> hd/warrior-v2/ (old hd/hero kept, unused) | done |
| Skill icons | asset-atlas paintedSkillswarrior -> roster; painted-combat-assets: only carpetBomb keeps ultimate override | done |
| Tests + browser check | tests/held-gear-warrior.test.js, held-weapon-wizard.test.js (new API), tools/check-warrior-held-gear.mjs | done |

Notes: death frames draw the baked armed set (no overlays). Empty offhand => bare body (fist). Missing shield anchors => weaponless body (shield painted in).
Warrior inventory/attack-button icons unchanged (not tied to the cut art).

## Polish phase (2026-10-29) - done
- Fit rewritten as constrained IoU fit (tools/held_gear_iou_fit.py): sword region = armed - weaponless outside the body silhouette (noise-free core grown over the blade root), angle near principal axis (up view: 20-80 deg, behind body), scale band +-6% of the median, outliers in idle/walk/hurt/dash rows re-fit near row median. Manual fixes: tools/assets/warrior-held-gear-anchor-overrides.json (7 frames: hurt.down 0/1, attack2/3 left, attack3 down 2, walk right 3, attack2 right 3).
- QA: python tools/warrior_gear_qa.py -> assets/hd/warrior-v2/qa/<anim>.png ([armed | bare+overlays | diff]) + iou.json. Sword mean IoU 0.39 (old fit measured 0.18 under the same strict region), shield 0.29 (painted armed shield differs in shape from the cut art).
- Enhanced art: cutter caps body to base area (scale >= 0.94) and thins haze to 60% (tools/cut-warrior-gear-overlays.py).
- Warrior sword + Shield offhand icons (bag, ground, attack button) use the cut base overlay art per tier: src/graphics/warrior-gear-icons.js, weapon-icons.js, effects-renderer.js, sprite-registry.js; tests/warrior-gear-icons.test.js. 316 tests pass.
