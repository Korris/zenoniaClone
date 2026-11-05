# Outpaint geometry (source px, 42.667 px/tile; target canvas 3072x2048 = 72x48 tiles)

Known image: `Assets/ArtLibrary/hd/map/frost-hollow/large/ground-only-chatgpt.png`, 1536x1024 at (0,0) (= old 36x24, ground only; ChatGPT redrew the cliffs, so the old rows do NOT apply).
Each ChatGPT frame is 1536x1024 (or 1024x1536 vertical), scale 1; known parts pasted in, empty parts flat colour. Overlaps serve colour matching and min-cut seams.

| Piece | Frame | Placed at (x,y) | Known part | New (px) | New tiles | Needs first |
|---|---|---|---|---|---|---|
| E1 | 1536x1024 | 1024,0 | left 512 (old) | x1536-2560, y0-1024 | c36-59, r0-23 | ground-only |
| S1 | 1536x1024 | 0,512 | top 512 (old) | x0-1536, y1024-1536 | c0-35, r24-35 | ground-only |
| SE1 | 1536x1024 | 1024,512 | top 512 (old+E1), left 512 (S1) | x1536-2560, y1024-1536 | c36-59, r24-35 | E1, S1 |
| S2 | 1536x1024 | 0,1024 | top 512 (S1) | x0-1536, y1536-2048 | c0-35, r36-47 | S1 |
| SE2 | 1536x1024 | 1024,1024 | top 512 (SE1), left 512 (S2) | x1536-2560, y1536-2048 | c36-59, r36-47 | SE1, S2 |
| FA | 1024x1536 | 2048,0 | left 512 (E1, SE1) | x2560-3072, y0-1536 | c60-71, r0-35 | SE1 |
| FB | 1024x1536 | 2048,512 | top 1024 (FA), left 512 (SE1, SE2) | x2560-3072, y1536-2048 | c60-71, r36-47 | FA, SE2 |

Order: (E1 | S1) -> (SE1 | S2) -> SE2 -> FA -> FB.

## Edge contracts the painting must continue (read off the ground-only image)
- East edge (x=1536): rows 5-11 open snow; the NE flagstone path and stairs end at a ledge (33,2-4), so E1 continues east to a ledge with a ruined watchtower; cliff bands at rows 12-13 (cols 26-34) and 14-16 (cols 32-35) continue east; the row 20-21 cliff band (cols 21-35) must END in E1 or open at the edge so the SE pocket is reachable.
- South edge (y=1024): the river leaves at cols 19-22 (S1 continues it south); the south cliff rim at row 23 cols 1-10 is closed, so S1 opens a gap at cols 2-4 where the flagstone path ends (2-3,22); cols 11-13 and 23-35 are open snow.
- Same cliff style (snow-capped grey-brown faces); no trees, crystals or props (placed later as props).

## Content per piece (for the P3 prompts)
E1 snowfield with flagstone road, ledge and ruined watchtower in the NE. S1 river continuing south, path gap, snowfield. SE1 lake shore. S2/SE2 southern cliffs and lake. FA/FB east strip with the ice-cave mouth at tiles (68-71,30-32).
