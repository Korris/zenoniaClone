# HD NPC sheets (normalized)

Generated from the hand-painted sheets in `assets/hd/map/Oakhaven/*.png` by `tools/build-hd-npcs.py`
(`npm run hd:npcs`). Same contract as the HD hero: 96x96 frames = 24 world units x pixelsPerUnit 4,
feet on the bottom row, body ~82px tall (child 70px).

| npcId | source | sheet |
|---|---|---|
| elderRowan | `Elder_Rowan.png` | 384x288 |
| villagerMia | `Mia.png` | 384x288 |
| hunterLena | `Hunter Lena.png` | 384x288 |
| merchantDale | `Merchant Dale.png` | 384x288 |
| fishermanRoy | `Fisherman Roy.png` | 384x288 |
| carpenterBrol | `Carpenter Brol.png` | 384x288 |
| farmerGuri | `Farmer Guri.png` | 384x288 |
| guardRook | `Guard Rook.png` | 384x288 |
| childTim | `Child Tim.png` | 384x288 |
| healerNina | `nina.png` | 384x288 |

- Layout: 4 idle frames per row; rows `0 = DOWN (front)`, `1 = LEFT`, `2 = RIGHT`. No rear pose:
  the engine draws the front row when an NPC faces up.
- `<npcId>_face.png` (152x152): dialog portrait, head+shoulders of the front frame, centered.
- Wired through `src/graphics/asset-atlas-hd-npcs.js`; a missing file falls back to the pixel-art
  NPC set (`NPC_SHEETS`).
