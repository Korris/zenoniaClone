# Redraw install: Willowbrook Meadows + Frost Hollow (13/11/2026)

## Done
- **Ground**: new `Tools/build-large-ground-from-redraw-quarters.py <willowbrook|frost-hollow> [--preview]`. 4 abutting quarters -> colour match to neighbours (WB gain 0.80-1.25, r0c1 was darker), low-freq step levelled across both mid edges, min-cut seam through a mirrored overlap (reuses `seam_path` of the frost painted script), x1.5 -> 2x2 chunks 2304x1536 + minimap 432x288, overwritten in place (old textures in git only). Picks highest-numbered quarter, skips `-old`.
  - WB `warps`: r1c0 pond half sat ~60 px lower than r1c1 half -> right part of r1c0 slid up so the shore meets.
- **Willowbrook bridge**: `Tools/cut-willowbrook-bridge-prop.py` cuts the old bridge from old ground (source saved `redraw/willowbrook-bridge-source-from-old-ground.png`) -> `Sprites/village/props/wbBridge.png`, PropDef `(5.5, 3.0, 5, 0)` = walk-through, 3 tiles high -> drawn flat under hero. `P("wbBridge", 23, 23)`, 14 px below old pixels. B tiles unchanged (cols 23-27, row 22).
- **Willowbrook props**: `cut-willowbrook-extra-props.py --redraw` re-cut all 21 `wbx*` from the redrawn sheet (same layout, tool took it). 7 PropDef sizes updated; fixed tool's ROWS footprints to match VillagePropDefs (was printing FootH 1 for walk-through props) and a 2% shadow haze from the non-pure key colour. 45 old-section `wb*` props (cut from first painting, no redrawn source) graded greener/more contrast: `grade-willowbrook-old-section-props-to-redraw.py`, originals kept in `redraw/old-section-props-src/`.
- **Willowbrook collision**: `build-willowbrook-large-rows.py --redraw` (rows in .cs = base, w/g re-read from painting incl. waterfall foam, + `layout/willowbrook-72x48-redraw-overrides.txt`). 57 tiles changed (narrower stream, pond moved). Checks: bridge closed -> 0 tiles reachable east of stream (no gap in water); bridge open -> 0 unreachable, with props 0 unreachable; all 11 zones free.
- **Frost Hollow collision**: `build-frost-hollow-large-rows.py --redraw`. Looked at every crossing on red overlay at 64 px/tile with tile labels: 7 stairs (west 5-6,9-11; north 32-33,3-4; mid 29-30,12-13; SW 3-4,20-21 + 5-6,23-25; NE plateau 49-50,5-6; east 44-46,17-19; SE ledge 49,38-39), lake pass 45,22-23, 2 bridges, both mine doors (decor, solid), ice cave. Redraw kept every one in place: steps open, railings solid, top/bottom connected -> **rows unchanged** (0 tiles). Flood: 2219 walkable, 0 unreachable.
- Moved `fhPineSapling` (18,29) -> (13,30): it stood on the south bridge deck (pre-existing, visible in capture).
- `docs/map-design-rules.md`: one line on the redraw scripts.
- Tests: ZenoniaSkills 297/297, ZenoniaNet 386/386.
- Build from copy OK (lock taken + released), exe `C:\temp\player-wbfh\Zenonia.exe`, 28 captures in `C:\temp\cap-wbfh`.

## Proof (docs/reference/)
`willowbrook-redraw-composite.jpg`, `willowbrook-redraw-solids-overlay.jpg`, `willowbrook-redraw-ingame-*.jpg` (6), `frost-hollow-redraw-composite.jpg`, `frost-hollow-redraw-solids-overlay.jpg`, `frost-hollow-redraw-ingame-*.jpg` (13).

## Seen in game (own eyes)
WB: spawn/west gate, bridge (solids + clean, hero standing on deck drawn over it), pond (solids + clean), centre seam, old section, east barricade, ruins zone with boars, SE zone. FH: spawn/gate portal, ice cave portal, all 7 stairs + lake pass + 2 bridges with solids, east zone, centre seam.

## Not verified
- Hero actually walking across bridge/stairs (only teleport stills + flood fill).
- Portal transitions to neighbour maps (not my files).
- Sapling move and final FH minimap: done after the build, seen only in the python composite.
- Server rebuild/online: WB rows changed (57 tiles) -> zone server must be rebuilt if it shares the map rows.
- `wbBridge.png` has no `.meta` yet (Unity editor will create it; commit it with the png).

## Known blemishes
- WB road crossing the r0/r1 seam near the west gate (cols ~5-14, row 23/24): quarters drew the road at slightly different heights; seam cut hides the line but road edge is a bit wobbly there.
- WB old-section oaks are graded, not redrawn: still softer than the wbx* trees.
- FH cliffs still jagged (as noted in art progress doc).

## Unresolved questions
- Ask ChatGPT for a redrawn sheet of the 45 old-section WB props (shed, palisade, oaks, cliffs)?
- `docs/world-map-2-art-progress.md` rows for these two maps not updated (file being edited by others).
