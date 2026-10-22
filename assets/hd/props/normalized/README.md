# HD prop sheets (normalized)

Generated from the raw sheets in `assets/hd/props/*.png` by `tools/build-hd-props.py`
(`npm run hd:props`). Each raw sheet holds several hand-painted objects placed irregularly
(not an exact grid); the build script removes a faint glow/halo (alpha <= 48 -> transparent),
splits the sheet into per-object segments (empty-column gaps, or a manual split where two
objects touch by a few stray px), tight-crops each object, and fits it bottom-center into
its prop type's HD canvas (world size x4, matching `src/world/prop-definitions.js`).

## Prop -> source mapping

### pine_tree.png
- whole sheet = 1 pine tree, reused as both `pine` and `pineDark`

### round_tree.png
- whole sheet = 1 round tree, reused as both `roundTree` and `bigTree`

### small_trees.png
- small leafy tree -> smallTree
- small pine (unused)
- dead tree -> deadTree

### bushes_rocks.png
- bush
- flowering bush -> bush2
- grey rock -> rockSmallGrey
- brown rock -> rockSmallBrown
- stump
- stones (non-solid)

### big_rocks.png
- grey boulders -> rockGrey
- brown boulders -> rockBrown

### village.png
- fence
- signpost
- pillar
- torch
- crystal
- bones (non-solid)

### houses.png
- red roof -> house
- brown roof -> house2
- stone roof -> house3

### desert.png
- palm
- cactus

### village_props.png
- well (roofed stone well, 2x3 tiles)
- lanternPost (wooden post + hanging lantern, 1x2)
- marketStall (striped-awning stall with goods, 3x3)
- cropPlot (one row of vegetables, 2x1)
- scarecrow (1x2)
- woodpile (log shelter, 2x2)
- campfire (1x1)
- barrel (1x1)
- crate (1x1)
- rowboat (2x1, moored on water)

## Rebuild

```
python tools/build-hd-props.py
# or: npm run hd:props
```

Missing raw files are skipped with a warning -- the set can grow one sheet at a time.