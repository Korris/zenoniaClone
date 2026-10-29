# Cleric class (support/healer, formerly "buff")
Status: implemented (placeholder wizard sprites). Art arrives in parallel under assets/hd/cleric/**.

| Phase | File | Status |
|---|---|---|
| 1 Data: class, 15 skills, combo, gear tables | phase-01-data.md | done |
| 2 Mechanics: shield, haste, grace, HoT zone, purify, holy blasts | phase-02-mechanics.md | done |
| 3 Sprites/UI: placeholder sheets, class select x4, icon atlas wiring | phase-03-sprites-ui.md | done |
| 4 Tests + browser check | phase-04-verify.md | done |

Key decisions
- Skill order MUST match icon sheet rows: HOLY (holyBolt, smite, divineJudgment, consecrate, heavenlyRay), RESTORATION (heal, regeneration, healingCircle, purify, resurrectionGrace), BLESSING (blessingOfMight, sacredShield, hasteHymn, spiritBond, angelsDescent).
- Self-only support (game is single-player); "allies" = the hero.
- resurrectionGrace = passive: lethal hit leaves hero at X% max HP + 1.5s invulnerability, per-level cooldown (120s..80s).
- Sprite source is data-driven: `CLERIC_ART_READY` in src/graphics/asset-atlas-hd-cleric.js (false => tinted wizard placeholder).

## Phase 5: Art integration (2026-10-29) - done
Real GREEN cleric art in: `CLERIC_ART_READY = true`; icons (assets/custom/skill-icons-cleric-roster.png) already wired via painted-combat-assets.
- Sheets: `python tools/build-cleric-sheets.py` -> assets/hd/cleric/normalized-hd (armed = baked set), weaponless/normalized-hd (aligned per frame onto armed, no death), normalized/hero_face.png. Sprites cross cell lines, so the wrapped splitter regroups connected pieces per frame (cuts merged neighbours at the emptiest column, drops crumbs). Side rows: row 2 faces screen-left, row 3 screen-right (verified, CLERIC_MIRRORED stays empty).
- Staff/spirit art: `python tools/cut-cleric-gear-overlays.py [staff|spirit|float]` (reuses cut-warrior-gear-overlays.py via importlib; that file got cfg hooks: names/dir/grip/tool). Staff grip = 34% up the shaft; spirit grip = body centre. Manifests src/data/cleric-{staff,spirit}-overlay-manifest-generated.js, cleric-spirit-float-manifest-generated.js. Contact sheets: assets/custom/cleric-{staff,spirit}-overlays/contact-sheet.png.
- Held staff: held-gear-specs.js `cleric` (weapon slot, bare = weaponless sheets), HELD_WEAPON_CLASSES += cleric. Anchors: `python tools/estimate-cleric-staff-anchors.py` (~3 min; IoU fit of the armed staff head outside the weaponless silhouette; angle constrained: idle/walk +-30deg, others around body->head direction; overrides tools/assets/cleric-held-gear-anchor-overrides.json; QA sheets assets/hd/cleric/qa). Frames the overlay cannot match (QA IoU < 0.35 or staff mostly hidden) get `{fallback:true}` -> held-weapon-renderer returns null => armed baked frame (per-frame fallback). Death: baked armed frames.
- Spirit companion (render-only): src/entities/spirit-follow.js (target offset by facing, exp ease 160 ms, snap > 90 units, bob) + src/graphics/cleric-spirit-renderer.js (art by offhand tier/band, glow via bakeStaffGlow, drawGear reuse) + world-renderer y-sorted layer. Tier-1 unenhanced = float loop (4 facings x 4 frames); other tiers / +7 and up = static front art + code bob (documented limitation). Hidden with empty offhand / dead. Spirit Bond sparkle: skipped (YAGNI).
- FX: HOLY_FX_TINT (#c9f26a) in src/data/cleric-fx-config.js applied to combo orbs (Projectile.tint, burst, flash), Holy Bolt / Smite / holyBlast / Angel's Descent bursts and the heal sparkle via tinted-sprite-cache.
- Tests: tests/cleric-art.test.js. Browser: node tools/check-cleric-art.mjs && python tools/stitch-wizard-held-staff.py tools/screenshots/cleric-art -> tools/screenshots/cleric-art/.
