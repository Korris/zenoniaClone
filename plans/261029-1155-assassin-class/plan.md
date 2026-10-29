# Assassin class (dual daggers, purple; speed, crits, stealth)
Status: implemented; real art integrated (phase 5, 2026-10-29).
Mirrors the cleric plan (plans/261029-1121-cleric-class).

| Phase | Scope | Status |
|---|---|---|
| 1 Data | class def (+critBonus), skill-definitions-assassin.js (15), ASSASSIN_COMBO_CONFIG, gear tables (Dagger, WEAPON_CLASS_IDS), icon wiring | done |
| 2 Mechanics | monster-status(+tick), player-status (stealth, poison blade, attackSpeed, evasion), assassin-*-handlers, shadow-clone, venom-zone, combo riders, double-stab finisher | done |
| 3 Sprites/UI | asset-atlas-hd-assassin.js (ASSASSIN_ART_READY), assassin-sprite-set.js, class select x5, purple FX tint, status pips, clone render | done |
| 4 Verify | tests/assassin-*.test.js (+helpers/assassin-world.js), tools/check-assassin.mjs -> tools/screenshots/assassin/ | done |
| 5 Art integration | real sprites, two held daggers, icons (see below) | done |

Key decisions
- Skill order = icon sheet rows: SHADOW (shadowStep, backstab, shadowMastery, smokeBomb, phantomAssault), VENOM (poisonBlade, toxicMastery, venomCloud, crippleStrike, deathMark), AGILITY (evasion, quickReflexes, bladeDance, adrenaline, shadowClone). SKILL_ORDER group 5 -> PAINTED_CLASS_IDS[4].
- Crit: class `critBonus` 0.06 added to passiveMods.critChance; `critDamage` passive raises the x1.6 crit multiplier; per-hit `critBonus` arg on rollPlayerDamage (Backstab).
- Evasion: Player.evasion getter (passive + Smoke Bomb stealth) read by hit-chance.js -> only ordinary hits can MISS (skills never).
- Monster debuffs live in monster.status {slow, blind, poison, mark}; ticked in world-update. Bosses feel half of a slow.
- Handlers may return false -> tryCastSkill refunds MP/cooldown ("No target": Shadow Step, Phantom Assault, Death Mark).
- Poison Blade = N charges on basic hits (assassin-combo-riders.js). Shadow Clone is simplified: a follower that echoes every landed basic hit around itself (cloneMult of the hit), not an independent attacker.
- Finisher `stabs: 2`: two stabs 90ms apart sharing 1.2x the finisher multiplier. Slash FX = warrior slash/arc tinted purple (`tint`).
- Adrenaline reuses the cleric 'haste' kind; Blade Dance the 'radial' kind (3 pulses).
- Placeholder = old black-hair hero (assets/hd/hero) + purple wash; flip ASSASSIN_ART_READY in src/graphics/asset-atlas-hd-assassin.js.

## Phase 5 - Art integration (2026-10-29, mirrors mechanic-art-integration / warrior held sword + shield)
| Step | Result |
|---|---|
| Sheets | `python tools/build-assassin-sheets.py`: armed idle/attack1-3 (+ death = weaponless) -> normalized-hd (192px); weaponless idle/attack1-3 aligned per frame onto the armed body; weaponless walk/dash/hurt head-scale matched then brought to the armed idle scale (no armed walk/dash/hurt exist, so the BAKED walk/dash/hurt = the bare body, daggers always come from overlays). attack2/3/death scaled by first-frame body height (head matching failed there). armed + weaponless attack3 cleaned of checkerboard crumbs (tools/assassin_speck_clean.py; originals kept as raw/attack3-uncleaned.png). hero_face from idle front head. `ASSASSIN_ART_READY = true`, both side rows painted (ASSASSIN_MIRRORED empty) |
| Dagger cut | `python tools/cut-assassin-gear-overlays.py` -> assets/custom/assassin-dagger-overlays (20 PNG + manifest + contact-sheet.png), src/data/assassin-dagger-overlay-manifest-generated.js (grip = handle wrap, 22% up from the pommel) |
| Anchors | `python tools/estimate-assassin-dagger-anchors.py` (~1.5 min): armed frames: bright-violet blade mask (armed minus weaponless) -> IoU fit per dagger (tools/assassin_dagger_pair_fit.py, tier-2 curved art as template, scale pinned 0.23 +-8%, grip forced to the blade's inner end so the fit cannot flip 180 deg). Pair split main/off by facing (down: main = screen-left, up: screen-right, right: near/lower, left: far/upper). walk/dash/hurt: tools/assassin_fist_tracker.py (idle reference pose moved with the fist found by NCC). QA sheets assets/hd/assassin/qa; overrides tools/assets/assassin-held-gear-anchor-overrides.json (`main.walk.down.1`) |
| Fallback decision | WHOLE-FRAME: a frame is `{fallback}` on both hands or neither; renderer draws the armed baked frame (both baked purple daggers, tier/enhance lost, empty offhand still shows two). Per-part fallback rejected: half a pair looks broken. 7/72 armed frames; walk/dash/hurt fallback = bare body (1 frame) |
| Renderer | held-gear-specs `assassin`: parts off (offhand, optional) then main (weapon); `behind` per anchor; HELD_WEAPON_CLASSES += assassin |
| Icons | warrior-gear-icons.js buildAssassinGearIcons (weapon tilted +45, offhand -45, same art by tier) -> bag/ground/attack button; paintedSkillsassassin roster already wired (order matches SKILL_ORDER) |
| Tests/checks | tests/assassin-art.test.js; `node tools/check-assassin-art.mjs && python tools/stitch-wizard-held-staff.py tools/screenshots/assassin-art`; tools/check-assassin.mjs |

Known limits: fit daggers are slimmer than the baked purple ones (art design); fallback frames show baked daggers; walk/dash/hurt grips are NCC estimates (not fitted), a few frames may sit a few px off the fist; armed attack2/3 art has some white-sleeve frames (source art).
