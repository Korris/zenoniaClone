# Phase 05 — Weaponless warrior sheets (user-generated via ChatGPT) + normalization

## Context Links
- Raw baked sheets: assets/hd/hero/{idle, walk-v2, attack1, attack2, attack3, dash, hurt, death}.png
- tools/build-hd-hero.py (grid snap, haze threshold, head-relative scale), tools/build-hd-render.py
  (192px wrapper; `place()` = bbox-centered horizontally, bottom-aligned), tools/hero_head_scale.py
- assets/hd/style-anchor-hero-upload.png, assets/hd/walk-v2-prompt.txt (prompt format precedent)
- User memory: art via ChatGPT Plus in Chrome (chatgpt.com), one chat for the whole set

## Overview
Priority P1 for Track B. Status pending (blocked on user art). 8 sheets, one per animation, same grid as current.

## Key Insights
- **Placement trap:** build-hd-render `place()` centers the cropped bbox. Removing the sword changes bbox width
  -> body would shift sideways vs. the baked sheet, breaking anchor estimation and causing jitter if animations
  mix. Weaponless build must anchor on the **feet**: x = center of opaque columns in the bottom 20% rows, bottom-aligned.
- Scale is per-sheet, head-matched to idle (hero_head_scale). Weaponless idle becomes its own reference;
  verify resulting scale.json within +/-3% of baked scale.json per animation.
- Hands must stay in a gripping pose at the hilt position — otherwise an overlay looks pasted on.

## Requirements
- Files: `assets/hd/hero/weaponless/{idle,walk,attack1,attack2,attack3,dash,hurt,death}.png` (+ `frame-counts.json`
  copy `{"walk":4}`), same rows (down, up, left, right) and column counts: idle4 walk4 attack1 4 attack2 4 attack3 6 dash3 hurt2 death5.
- Prompt file `assets/hd/hero/weaponless/weaponless-prompt.txt` (one template + per-sheet line).
- Output `assets/hd/hero/weaponless/normalized-hd/*.png` (192px frames) + scale.json.
- Side-by-side diff sheet `assets/hd/hero/weaponless/compare-contact-sheet.png` (baked | weaponless | overlap tint).

## Prompt template (attach: the matching baked raw sheet + style-anchor-hero-upload.png)
```
Edit the attached sprite sheet. Keep EVERYTHING identical: same character (black ponytail, red headband warrior),
same anime/manga chibi style, same top-down classic view, same grid (4 rows: facing down, up, screen-left,
screen-right; {N} frames per row, same order), same poses, body positions, proportions, outline weight and colors.
ONLY change: remove the sword completely. The hands stay exactly where they are, closed in a gripping fist as if
still holding an invisible handle (fingers wrapped around an empty hilt). Do not move arms. Fill any body/cloth
area previously hidden behind the blade with matching clothing. No weapon, no slash effects, no glow, no motion
lines, no shadow, no text. Transparent background (or flat checkerboard), crisp 2D cel shading.
```
Per-sheet line: `This is the {anim} animation, {N} frames per row.` For attack sheets add:
`Fists keep the swing pose; wrist angle unchanged.` Generate one sheet per message in the same chat; reject
any result whose pose/grid drifts (check with compare sheet before accepting).

## Architecture
```
user raw sheets -> tools/build-hd-hero-weaponless.py (wraps build-hd-hero like build-hd-render,
   FRAME=192, TARGET_BODY_HEIGHT=164, place = feet-anchored) -> normalized-hd/*.png + scale.json
   -> compare-contact-sheet.png (alpha diff vs assets/hd/hero/normalized-hd)
```

## Related Code Files
- Create: `tools/build-hd-hero-weaponless.py`, `assets/hd/hero/weaponless/weaponless-prompt.txt`,
  `assets/hd/hero/weaponless/frame-counts.json`, generated sheets
- Modify: `package.json` (`"hd:weaponless": "python tools/build-hd-hero-weaponless.py"`)
- Delete: none (baked sheets stay — fallback + wizard/mechanic untouched)

## Implementation Steps
1. Write prompt file; hand to user (they generate via ChatGPT Plus, Chrome, one chat).
2. Wrapper tool: import build-hd-hero via importlib (copy build-hd-render pattern), override `place_in_frame` with
   feet anchor; walk source name `walk` (not walk-v2); skip missing sheets with warning (incremental delivery).
3. Also apply the same feet anchor when computing a "reference" placement for the baked sheet in the compare step,
   or simply report per-frame feet x/y deltas (baked normalized vs weaponless) — target |dx|,|dy| <= 3px @192.
4. Emit compare sheet; user/dev review; regenerate drifting sheets.

## Todo List
- [ ] prompt file  - [ ] user delivers 8 sheets  - [ ] wrapper tool  - [ ] scale within 3%  - [ ] compare sheet OK

## Tests
- Python self-check in tool: every output sheet = cols*192 x 4*192, no px 0<alpha<=48, each frame non-empty.
- Node: extend `tests/hd-asset-config.test.js` pattern -> new `tests/weaponless-hero-config.test.js`: atlas keys for
  weaponless sheets map to existing files when present (skip-if-missing to keep CI green before art lands).

## Success Criteria
All 8 sheets normalized; feet deltas <= 3px; no residual blade pixels (visual check on compare sheet).

## Risk Assessment
| Risk | L x I | Mitigation |
|---|---|---|
| ChatGPT redraws pose/proportions (drift) | H x H | strict prompt + compare sheet gate; accept per-sheet; feet anchoring; anchor tool tolerates small drift |
| Remnant sword fragments | M x M | compare sheet; prompt "no weapon"; manual erase allowed |
| Different head scale -> size pop between anims | M x M | head-relative scale already in pipeline; check scale.json delta |
| Art takes several iterations (blocking) | H x M | Track A unaffected; renderer per-anim fallback allows partial rollout |

## Security Considerations
None.

## Rollback
Delete weaponless folder; renderer falls back to baked sheets.

## Next Steps
P6 anchors.

## Unresolved
- Death: weapon hidden (dropped) or drawn lying beside body? Plan default: hidden from frame 2 on.
- Dash: show weapon (default yes) — confirm.

## STATUS 2026-10-29 -- WIZARD FIRST (scope change; warrior deferred, being redesigned)
Done for the wizard: raw sheets `assets/hd/wizard/weaponless/raw/*.png` (user ChatGPT edits) -> `tools/build-wizard-weaponless.py` ->
`assets/hd/wizard/weaponless/normalized-hd/*.png` + scale.json + compare-contact-sheet.png (baked | weaponless | diff).
Learned: ChatGPT redraws the body 3-15% smaller/larger and slightly shifted, so feet-anchoring is not enough. The tool keeps the baked
scale.json scale, then searches ONE body factor per sheet (0.85-0.97 found) and a per-frame shift maximizing "weaponless inside baked"
overlap (2*corr - sum(W)). Result: weaponless alpha outside baked silhouette mean 7-11% (outline drift only). Warrior: untouched, same tool pattern applies later.
