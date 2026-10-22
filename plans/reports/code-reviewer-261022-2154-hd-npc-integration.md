# Code Review: HD NPC integration + Oakhaven NPC placement

**Scope:** tools/build-hd-npcs.py, src/graphics/{asset-atlas-hd-npcs,asset-atlas,sprite-registry,prop-renderer,world-renderer}.js, src/core/constants.js, src/game/{world-update,game-controller}.js, src/ui/dialog-box.js, src/data/npc-definitions.js, src/world/maps/oakhaven-village-map.js, tests/hd-npcs-config.test.js, tools/browser-check.mjs, package.json.
**Baseline:** npm test 115/115, browser-check errors [], 10/10 NPCs hd:true. Screenshots 21/22 inspected: scale, y-sort, portrait, marker all correct.

**Score: 8/10** -- no Critical/High. Clean fallback design; the real issues are a sticky facing side-effect and a dead fallback branch.

## Critical
None.

## High
None.

## Medium

### M1. NPC facing is never restored after the hero walks away
`src/game/game-controller.js:67` -- `faceTowards` now runs every frame while in TALK_RANGE, and nothing resets `npc.dir` when `nearbyNpc` becomes null. Pixel-art NPCs on other maps (minerBork/nomadSaif/knightAldric) stay with their back to the camera (`dir='up'`) indefinitely after the hero passes above them; `def.dir` is effectively used only until first approach. On Oakhaven the `up->down` remap hides it. (No per-frame flip-flop: along a diagonal approach `|dx|-|dy|` is constant, so the facing snaps once at the crossing -- acceptable.)
```js
// game-controller.js updatePlaying()
const prev = this.nearbyNpc;
this.nearbyNpc = findNearbyNpc(session.world);
if (prev && prev !== this.nearbyNpc) prev.dir = prev.def.dir || 'down';
if (this.nearbyNpc) this.nearbyNpc.faceTowards(session.player.x, session.player.y);
```
(`openNpcDialog` still calls `faceTowards` itself -- the per-frame call is now redundant there; harmless.)

### M2. Pixel fallback `|| registry.hero` cannot work
`src/graphics/prop-renderer.js:38` -- `characterFrame(registry.npcs[sprite] || registry.hero, dir)` returns `facing.idle`; for the hero set that is an **array** (HD/pixel hero idle frames), so `drawImage(array)` throws and kills the render loop. Only reachable if an NPC's `sprite` key is not in `NPC_SHEETS` (today all six resolve), so it is a latent trap, not a live bug. Either drop the fallback or make it a real one:
```js
const set = registry.npcs[npc.def.sprite];
if (!set) return null; // and skip draw in drawNpc
```
Plus a cheap test (see T1) so a typo in a new definition fails at `npm test`, not at runtime.

### M3. Python helper coupling has no early failure
`tools/build-hd-npcs.py:27-29` -- `importlib` of `build-hd-hero.py` works (guarded by `__main__`, module-level only parses argv/constants), but the NPC script depends on 8 private names (`FRAME, FACE_SIZE, TARGET_BODY_HEIGHT, normalize_alpha, grid_lines, tight_bbox, downscale, place_in_frame`). A rename in the hero script surfaces as an `AttributeError` mid-run, after some PNGs are already written. Minimal guard:
```python
REQUIRED = ('FRAME', 'FACE_SIZE', 'TARGET_BODY_HEIGHT', 'normalize_alpha', 'grid_lines', 'tight_bbox', 'downscale', 'place_in_frame')
missing = [n for n in REQUIRED if not hasattr(hero, n)]
if missing: sys.exit(f'build-hd-hero.py no longer exports: {missing}')
```
Proper fix (later, if a third HD script appears): move shared helpers to `tools/hd_sheet_utils.py` and import normally. Not worth it for two consumers.

## Low

### L1. browser-check accepts `slimeTrouble` as a side-effect
`tools/browser-check.mjs:80-83` -- paging with `KeyE` through the elder's offer selects `choices[0]` = Accept on the last page. Deterministic, but later checks (meadow markers, quest log) run with an active quest. Use `Escape` on the final page (`Esc` = decline per dialog-box.js) or note it in the comment.

### L2. Per-frame tight-bbox centering causes 1-2 px horizontal idle jitter
`tools/build-hd-npcs.py:85` via `hero.place_in_frame` -- each frame is centered on its own bbox, so an arm swing shifts the whole body (elderRowan left row xmin 12/12/13/13; merchantDale right row 21/21/21/21 ok). 0.25-0.5 world units; same behaviour as the hero pipeline, so consistent. Fix only if it reads as wobble: compute one x-offset per row from frame 0 and reuse it.

### L3. Idle rows are near-static on several sheets
farmerGuri/childTim/healerNina have identical bboxes across all 4 frames; the 260 ms loop will look frozen for them. Art issue, not code -- noting so no one hunts a renderer bug.

## Verified non-issues (from the prompt's checklist)
- **y-sort / footprint:** `npc.y = (ty+1)*16-2`, footprint tile `floor((y-2)/16) = ty`; sort key is feet-y like the hero; the 24-unit sprite overhangs 4 px per side onto neighbour tiles (cosmetic, same as the hero). Screenshot 22 confirms hero-in-front ordering.
- **`npc.animTimer` undefined:** `Npc` ctor seeds it; `world.npcs` is only ever built by `loadMap` (save/continue goes through `loadMap` too, no raw NPC restore). Not reachable.
- **Dialog portrait as plain `HTMLImageElement`:** `(portrait.pixelsPerUnit || 1) > 1` is false for pixel facesets -> `drawScaledSprite` path unchanged. `faces[id]` cannot collide with `hero/boss/heroIsHd` or the sprite keys.
- **Other maps' NPCs:** `npcsHd` has no entry for miner/nomad/knight ids -> `npcFrameUnits` = 16, pixel frame path unchanged; browser-check cave/castle/desert screenshots still pass.
- **Map placement:** NPC tiles reserved; `map-invariants` covers "not covered by prop sprite" + reachability for all 10.
- **Sheets:** all 120 frames have body top at 14-30 px (82 px / 70 px child), feet on the bottom row, no clipping warnings.

## Tests -- gaps
- **T1** (add to `tests/hd-npcs-config.test.js` or `map-invariants`): every `NPC_DEFINITIONS[*].sprite` is a key of `NPC_SHEETS` -- guards M2 for all 13 NPCs, not just the HD ten.
- **T2** (optional): `HD_NPC.rows` must be a subset of `CHARACTER_DIRECTIONS` and never contain `'up'` while `HD_NPC_FACING` is private -- documents the "no rear pose" contract in code.

## Positive
- Per-id HD override with per-file fallback mirrors the hero/tiles/props pattern; nothing stubbed.
- `pixelsPerUnit` stamping lets dialog/prop/NPC renderers decide smoothing locally -- no global flags.
- Python validator (`feet on bottom row`, empty frames) and the IHDR-size test make sheet drift fail fast.
- Quest-giver ids untouched; dialogue-only NPCs explicitly not stubbed with shop/craft hooks (YAGNI respected).

## Unresolved questions
1. Should NPCs return to `def.dir` immediately when the hero leaves range (M1), or is "keeps looking where the hero went" the intended feel? If intended, only the pixel-art 'up' case on other maps needs the reset.
2. Is the browser-check meant to accept the elder's quest (L1), e.g. so later meadow checks exercise quest markers?
