# Phase 06 — Per-frame hand anchors (estimate + override + preview)

## Context Links
- Baked frames: assets/hd/hero/normalized-hd/*.png ; weaponless: assets/hd/hero/weaponless/normalized-hd/*.png (P5)
- Overlay manifest: src/data/weapon-overlay-manifest-generated.js (P4)
- Frame lookup: src/graphics/sprite-registry.js buildHeroHd (rows down/up + HD_HERO_SIDE_ROWS left=2,right=3)

## Overview
Priority P1 Track B. Status pending. Produce anchors for 32 frames x 4 directions = 128 entries.

## Key Insights
- The baked sheets already show where the sword is in every frame -> estimate anchors by diffing baked vs
  weaponless instead of hand-authoring 128 entries.
- Baked frames were bbox-centered, weaponless feet-anchored -> align first: search integer shift (+/-16px) minimizing
  alpha mismatch outside the candidate weapon region (body-only score).
- Interactive browser editor = YAGNI: overrides JSON + preview contact sheet is enough for 128 entries.

## Requirements
Anchor schema (frame pixel space, 192x192, origin top-left of frame):
```js
// src/data/warrior-weapon-anchors-generated.js
export const WARRIOR_WEAPON_ANCHORS = {
  idle: { down: [ { x, y, angle, len, behind, hidden? }, ... ], up: [...], left: [...], right: [...] },
  ...
};
```
- `x,y` = hand grip point; `angle` deg, 0 = blade straight up, clockwise positive; `len` = baked blade length px
  (drives foreshortening scale vs. reference length); `behind` = draw before body; `hidden` = skip draw.
- Overrides: `tools/assets/weapon-anchor-overrides.json` `{ "attack3.down.2": { "angle": 140, "behind": false } }` merged last.
- Preview: `assets/hd/hero/weaponless/anchor-preview.png` — every frame with base sword overlay composited using the
  same math as runtime (grip align, rotate, scale, behind/front).

## Algorithm (tools/estimate-weapon-anchors.py)
1. For each anim/dir/frame: baked B, weaponless W (alpha). Find shift (dx,dy) aligning B to W (min |B-W| on W-opaque px).
2. Weapon mask = B_alpha>128 AND W_alpha<48 (after shift); keep largest component + attached components; ignore < 40px.
3. If mask < 150px -> `hidden: true` (e.g. weapon fully occluded/sheathed; review in preview).
4. PCA of mask -> axis; endpoints = extreme projections. Grip end = endpoint nearer W's hand: nearest W-opaque px
   to the axis line within 10px; grip point = projection of that contact onto axis.
5. angle from grip->tip vector; len = |tip - grip|.
6. behind: in overlap region (B and W opaque, around grip/blade line) compare colors; if B ~= W there (body painted
   over blade) -> behind = true; per-direction majority smoothing (up-facing mostly behind).
7. Temporal smoothing: none for attacks (fast swings real); idle/walk clamp angle jitter <= 4deg to median.
8. Merge overrides; write generated JS + JSON copy + preview; print low-confidence list (mask small, angle jump >60deg).

## Related Code Files
- Create: `tools/estimate-weapon-anchors.py` (+ `tools/weapon_anchor_math.py` if >200 LOC),
  `tools/assets/weapon-anchor-overrides.json`, `src/data/warrior-weapon-anchors-generated.js`,
  `src/graphics/held-weapon-anchor-math.js` (pure JS: anchor -> transform {tx,ty,rot,scale}; shared with tests),
  `tests/held-weapon-anchors.test.js`
- Modify: `package.json` (`"hd:anchors": "python tools/estimate-weapon-anchors.py"`)

## Implementation Steps
1. Write math helper JS first (single source of the transform; python preview mirrors it — document formula in both).
2. Estimator steps above; run; inspect preview; add overrides; rerun until clean.
3. Commit generated module.

## Todo List
- [ ] JS anchor math  - [ ] estimator  - [ ] overrides pass  - [ ] preview approved  - [ ] tests

## Tests (tests/held-weapon-anchors.test.js)
- Coverage: every anim in HD_HERO_ANIMATIONS x 4 dirs has entries == frame count (idle4 walk4 a1 4 a2 4 a3 6 dash3 hurt2 death5).
- Bounds: x,y in [0,192]; angle finite; len > 0 unless hidden.
- Math: anchor angle 0 + grip -> tip above grip; mirrored? (no mirroring for warrior side rows) documented.
- Smoothness: idle/walk consecutive angle delta <= 25deg.

## Success Criteria
Preview shows sword in hand for all 128 frames; low-confidence list empty after overrides.

## Risk Assessment
| Risk | L x I | Mitigation |
|---|---|---|
| Weaponless pose drift makes diff mask noisy | H x M | shift alignment, component filter, overrides |
| Baked slash/motion-line pixels counted as weapon | M x M | mask limited to near-axis component; overrides |
| Foreshortened down/up thrusts (short len) | M x L | scale = clamp(len/refLen, 0.55, 1.1) |
| Weapons longer than sword clip through head | M x L | per-type `length` from manifest; check in P9 screenshots for bigSword/lance |

## Security Considerations
None.

## Rollback
Remove generated anchors; renderer treats missing anchors as "use baked sheet".

## Next Steps
P7 runtime.

## Unresolved
- Lance/hammer/axe held at sword grip look right in attack swings? May need per-type grip offset along axis (manifest gripY handles most).

## STATUS 2026-10-29 -- WIZARD FIRST (scope change; warrior deferred)
Done for the wizard: `tools/estimate-wizard-staff-anchors.py` (+ `tools/wizard_staff_preview.py`, overrides `tools/assets/wizard-staff-anchor-overrides.json`)
-> `src/data/wizard-staff-anchors-generated.js` (WIZARD_STAFF_ANCHORS[anim][dir][i] = {x,y,angle,s,behind,hidden}, displayed-frame coords; attack1/2 left
= mirrored right row) + `assets/hd/wizard/weaponless/anchors.json` + `anchor-preview-<anim>.png`.
Deviation from plan: PCA/diff-mask was unreliable (staff shaft is cut by body dilation, spell VFX baked in attack3). Final method = template fit: synthetic
staff silhouette (shaft+head) matched by cv2.matchTemplate over angle/length, scoring staff-candidate pixels (baked minus body, opaque, gold-weighted) minus pixels outside the baked silhouette.
Walk is NOT fitted (baked walk hides/flips the staff): carried like idle (idle median grip/angle) shifted by weaponless upper-body centroid bob. Death hidden. Fill rule: frames with no visible staff reuse nearest frame, behind body.
Known weak spots: dash/hurt/attack frames where baked hides the staff behind the body (estimate may be coarse); use the overrides JSON to fix.
