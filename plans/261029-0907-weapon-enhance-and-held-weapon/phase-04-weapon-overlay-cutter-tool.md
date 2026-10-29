# Phase 04 — Weapon overlay cutter (18 PNGs + manifest with grip)

## Context Links
- Source: assets/custom/weapon-overlay-sheet-raw.png (1024x1536 RGBA, untracked — commit with this phase)
- Patterns: tools/split-earth-break.py (alpha gutters, scrap removal, frames.json, contact sheet),
  tools/build-monster-attacks.py, tools/sprite_sheet_cutter.py
- Art rules: AGENTS.md Art direction (discard alpha<=48 haze)

## Overview
Priority P1 for Track B. Status implemented (pending user contact-sheet approval). Python tool slicing rows 1-3 into 9 [base, enhanced] pairs.

## Key Insights (measured)
- With alpha>=48: row bands y 28-343, 394-694, 746-1113, row4 1127-1510 (discard). Alpha histogram: ~315k px
  in 1..47 = glow haze to drop.
- Column gutters exist but one is only 2px (859-860, axe-enhanced edge) -> split **per row band** on that
  band's own column profile, then fall back to connected components + nearest-cell assignment.
- Enhanced variants have disconnected flame/lightning flecks -> do NOT keep only the largest component; keep
  all components >= 12 px whose centroid falls in the cell.
- Order per row (left->right, pairs): r1 sword, sword2, rapier; r2 club, katana, axe; r3 lance, hammer, bigSword.
- Katana (base + enhanced) leans ~10deg: straighten with angle measured on the BASE mask (PCA of opaque px),
  apply the same angle to its enhanced twin (glow would bias PCA).

## Requirements
- Output `assets/custom/weapon-overlays/<type>-<base|enhanced>.png`, tight-cropped + 4px pad, max height 256
  (downscale LANCZOS, keep both members of a pair at the same scale factor so enhanced overlays base).
- Grip point per PNG: default = handle center, located as midpoint between pommel bottom and the guard
  (widest row in lower 45% for swords); per-type override table in the tool for long hafts
  (club/axe/lance/hammer: fraction of length from bottom, e.g. lance 0.30, hammer 0.22, axe 0.25, club 0.20).
- Emit `assets/custom/weapon-overlays/manifest.json` (tool/debug) AND generated
  `src/data/weapon-overlay-manifest-generated.js` (runtime cannot fetch JSON; repo loads data as ES modules):
  `export const WEAPON_OVERLAYS = { sword: { base:{file,w,h,gripX,gripY}, enhanced:{...}, glow:'#5cc8ff', length: 1.0 }, ... }`
  `glow` = dominant hue of enhanced art (blue/red/orange/purple), computed from saturated px; `length` = relative
  world length (bigSword 1.25, sword2 0.8 ...) from base heights normalized to sword.
- `contact-sheet.png` with grip crosshair drawn, for human review.

## Architecture
```
raw sheet --alpha<48->0--> row bands --per-band column gutters/components--> 18 cells
  -> (katana) rotate by base PCA angle -> trim+pad -> pair-consistent downscale
  -> grip detect (+ overrides) -> PNGs + manifest.json + *-generated.js + contact-sheet.png
```

## Related Code Files
- Create: `tools/cut-weapon-overlays.py` (<200 LOC; split helpers into `tools/weapon_overlay_geometry.py` if needed —
  existing python helper modules use snake_case for importability), `assets/custom/weapon-overlays/*`,
  `src/data/weapon-overlay-manifest-generated.js`, `tests/weapon-overlay-manifest.test.js`
- Modify: `package.json` scripts: `"weapons:overlays": "python tools/cut-weapon-overlays.py"`

## Implementation Steps
1. Load RGBA, zero alpha<48. Find row bands from row alpha profile; keep first 3.
2. Per band: column profile gutters -> 6 cells; if count != 6, connected components assigned by centroid to 6 equal-width bins.
3. Drop components < 12px; crop cell bbox.
4. PCA angle on base masks; rotate pair if |angle| > 2deg (expected: katana only; log every angle).
5. Pair scale: s = min(1, 256 / max(h_base, h_enh)); resize both.
6. Grip detection + override table; sanity: grip inside opaque px.
7. Dominant glow hue (enhanced px with S>0.5, V>0.6) -> hex.
8. Write outputs; print summary table.

## Todo List
- [x] tool  - [ ] outputs committed (not committed, per instruction)  - [x] generated module  - [x] manifest test  - [ ] contact sheet reviewed (user)

## Tests (tests/weapon-overlay-manifest.test.js, node)
- 9 types == WEAPON_TEMPLATES keys; each has base+enhanced with w,h>0, grip within bounds, glow hex, length in [0.6,1.4].
- Files exist on disk (fs.existsSync) and PNG header width/height match manifest (read IHDR bytes; no deps).

## Success Criteria
18 PNGs, no haze (no px with 0<alpha<48), no clipped tips (bbox not touching cell edge in source), katana vertical
within 2deg, contact sheet approved by user.

## Risk Assessment
| Risk | L x I | Mitigation |
|---|---|---|
| Flame flecks from neighbour bleed into a cell | M x M | per-band gutters + centroid assignment; contact sheet review |
| Grip heuristic wrong for hafted weapons | H x M | explicit override table, visual crosshair |
| Enhanced art larger than base -> overlay jumps when crossing +7 | M x M | grip-aligned, same pair scale; enhanced gets same `length` |
| Art style (enhanced pieces are ornate/"Western") | M x H | user already supplied sheet; flag in review against AGENTS.md; glow code stays anime-crisp |

## Security Considerations
Local tool only; run python with explicit paths.

## Rollback
Delete output folder + generated module; nothing at runtime depends on it until P7.

## Next Steps
P6 (uses manifest for anchor previews), P7.

## Unresolved
- Does the user want row-4 extras kept anywhere (e.g. future rarities)? Plan: ignore.
