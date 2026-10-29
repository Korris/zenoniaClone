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

### Oakhaven Village (v6)
Built separately by `tools/build-oakhaven-village-art.py` (`npm run hd:village`) from the ChatGPT sheets in `assets/hd/map/oakhaven-v6/`: elderHouse, cottageRed, hunterLodge, healerCottage, carpenterWorkshop, well, lanternPost, marketStall, scarecrow, woodpile, campfire, barrel, crate, rowboat, villageFence, villageOak, villagePine, villageBush, villageFlowerBush, villageSignpost.
