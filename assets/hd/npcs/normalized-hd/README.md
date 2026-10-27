# HD NPC sheets (normalized)

Generated from the hand-painted sheets in `assets/hd/map/Oakhaven/*.png` by `tools/build-hd-npcs.py`
(`npm run hd:npcs`). Same contract as the HD hero: 192x192 frames = 24 world units x pixelsPerUnit 8,
feet on the bottom row, body ~164px tall (child 140px).

| npcId | source | sheet |
|---|---|---|
| elderRowan | `Elder_Rowan.png` | 768x576 |
| villagerMia | `Mia.png` | 768x576 |
| hunterLena | `Hunter Lena.png` | 768x576 |
| merchantDale | `Merchant Dale.png` | 768x576 |
| fishermanRoy | `Fisherman Roy.png` | 768x576 |
| carpenterBrol | `Carpenter Brol.png` | 768x576 |
| farmerGuri | `Farmer Guri.png` | 768x576 |
| guardRook | `Guard Rook.png` | 768x576 |
| childTim | `Child Tim.png` | 768x576 |
| healerNina | `nina.png` | 768x576 |

- Layout: 4 idle frames per row; rows `0 = DOWN (front)`, `1 = LEFT`, `2 = RIGHT`. No rear pose:
  the engine draws the front row when an NPC faces up.
- `<npcId>_face.png` (152x152): dialog portrait, head+shoulders of the front frame, centered.
- Wired through `src/graphics/asset-atlas-hd-npcs.js`; a missing file falls back to the pixel-art
  NPC set (`NPC_SHEETS`).
