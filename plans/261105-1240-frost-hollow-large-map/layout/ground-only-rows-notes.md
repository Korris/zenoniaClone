# Old section rows re-derived from the ChatGPT ground-only image

Files: `frost-hollow-ground-only-36x24-rows.txt` (C cliff/cave/border, w river, B bridge, s walkable), script `classify-old-section-from-ground-only.py` (colour classifier + hand fixes in `old-section-overrides.py`), overlay `../visuals/frost-hollow-ground-only-rows-overlay*.png` (viewed; matches the painting to about one tile). Old rows kept in `frost-hollow-36x24-old-rows.txt` for diffing. Trees are gone from the image (no T); the old forest became cliff or snow.

Fixes by eye: border row 0 and col 0 solid; bridge deck row 12 cols 17-21 (B); west stairs (5-6, 9-11), east stairs (29-30, 12-13), NE stairs (32-33, 2-4) walkable; (1,12) water false positive; enclosed cliff-top island (13-15,14-15) solid.

## Old coordinates on the new rows
| Coordinate | Tile | Reachable from start |
|---|---|---|
| start (4,21), village gate (2,21) | s, s | yes |
| King (7,5), capture (11,6) | s, s | yes |
| old Den portal (33,2), stairs (32-33,3-4) | s | yes |
| Zone frostSlime x5 (5,4,9,5) | 44/45 free | all |
| Zone frostSlime x3 (8,13,5,4) | 18/20 free | all |
| Zones iceGoblin x4 + frostSlime x2 (25,5,8,5) | 40/40 | all |
| Zone spectralWolf x4 (25,14,6,4) | 24/24 | all |

None lands on a cliff, no fix needed. 467 of 507 walkable tiles are reachable from start; the 40 unreachable are south pockets (rows 21-23: cols 11-16, 23-35) behind the south cliff rims, to be joined by S1/SE1/E1.

Open issue: the start nook (1-9, 21-22) is closed to the south in this image. **S1 must open a gap at cols 2-4** (where the flagstone path ends) or the village gate area has no way out except north (it is not a dead end today: the nook connects north via the stone path).

Glacier Warden is the Den boss, not on this map; the Frost Hollow -> Den link is the portal, moved to the east cave mouth (71,30-32) in P6.

Consequences: (a) props cannot be cut by diffing against the original painting in the old section; pines, crystals, rune stones, bones, fences, stumps and rocks come from a new snowy prop sheet placed by hand near their old positions. (b) P6 replaces the old rows of FrostHollowLayout.cs with these rows, not the old ones.
