# HD monster sheets (normalized)

Generated from the ChatGPT sheets in `assets/hd/monsters/raw/*.png` by `tools/build-hd-monsters.py`
(`npm run hd:monsters`). Same frame contract as the HD hero/NPCs: 96x96 frames = 24 world units x
pixelsPerUnit 4, feet on the bottom row. Layout: 4 columns = walk/hover cycle frames, rows = front
(DOWN), LEFT, RIGHT (no rear pose: the renderer shows the front row for UP).

| type | body height | sheet |
|------|-------------|-------|
| frostSlime | 52px | 384x288 |
| iceGoblin | 76px | 384x288 |
| spectralWolf | 70px | 384x288 |

Sliced by `src/graphics/sprite-registry.js` into `registry.monstersHd[type]` (config in
`src/graphics/asset-atlas-hd-monsters.js`).
