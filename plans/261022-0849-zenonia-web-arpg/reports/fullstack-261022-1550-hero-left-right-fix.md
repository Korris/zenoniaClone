# Hero Left/Right Facing Bug - Fix Report

## Bug
User-reported: walking left makes the hero face right, and vice versa (combat swings/poses also
mismatched vs. movement direction).

## Root cause (verified by direct pixel inspection, not guesswork)
Both the custom pixel hero sheets (`assets/custom/hero/*.png`, built from the concept art) and the
hand-painted HD hero sheets (`assets/hd/hero/normalized/*.png`, and their raw `assets/hd/hero/*.png`
sources) were authored with the `left` and `right` rows physically swapped relative to
`CHARACTER_DIRECTIONS`' assumed order (`['down','up','left','right']` -> rows 0,1,2,3). Cropped and
visually compared row2 vs row3 of both `idle.png` sheets: row index 2 (which the code read as
"left") shows the character's face/sword oriented toward screen-RIGHT; row index 3 ("right") shows
it facing screen-LEFT. Confirmed the live symptom too: in a pre-fix combo screenshot with
`player.dir = 'right'`, the sword swing animation pose faced left while the hit effect/slime (and
damage, which is computed from `dir` independently of the sprite) was correctly to the right --
movement/combat *logic* was always correct, only the *sprite row* read was backwards.

Both independently-produced art sources (concept-derived pixel sheets and separately hand-painted
HD sheets) have the identical swap, so this is a shared "row 2 = right, row 3 = left" authoring
convention that the rendering code never accounted for -- not a bug in `facingFromVector()`,
`getMoveVector()`, or any flip logic (those were all already correct).

## Fix
Added `HERO_SHEET_ROW` (`{ down:0, up:1, left:3, right:2 }`) to `src/graphics/asset-atlas.js`, with
a comment explaining why. `src/graphics/sprite-registry.js`'s `buildHeroPixel()`/`buildHeroHd()` now
look up the sheet row through this map instead of the plain `CHARACTER_DIRECTIONS` forEach index.
No art/PNGs touched (no Python re-run needed), no change to `CHARACTER_DIRECTIONS` itself (shared
with NPCs/monsters, which use an unaffected column-based layout from the CC0 pack -- verified only
the hero's row-based sheets have this swap).

## Files changed
- `src/graphics/asset-atlas.js` -- added `HERO_SHEET_ROW` constant + explanatory comment.
- `src/graphics/sprite-registry.js` -- `buildHeroPixel()`/`buildHeroHd()` use `HERO_SHEET_ROW[dir]`
  for the sheet row instead of the raw forEach index.

## Verification
- `node --test tests/*.test.js` -> 107/107 pass (no test covered this rendering detail before; it's
  DOM-only, consistent with `docs/code-standards.md`'s testable/impure split).
- `node --check` clean.
- `node tools/browser-check.mjs`: errors []. Re-ran the combo screenshot (`03-combo-1.png`): sword
  swing now correctly faces right (toward the slime/hit effect), matching `dir`. Ad-hoc screenshots
  (not a deliverable, deleted after) of pressing A (walk left) and D (walk right) confirm the hero
  now visually faces the direction it's actually moving in both cases.

## Unresolved questions
None -- fix is isolated, verified visually in both directions and in combat, all tests green.
