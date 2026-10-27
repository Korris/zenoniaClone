// HD hand-painted NPC idle sheets + dialog portraits (Oakhaven villagers) -- split out of
// asset-atlas.js like asset-atlas-hd-props.js. Built by tools/build-hd-npcs.py from the raw sheets in
// assets/hd/map/Oakhaven/ into assets/hd/npcs/normalized-hd/. Same resolution model as the HD hero:
// 192px frames = 24 world units * pixelsPerUnit 8, so an HD NPC stands at hero height (the pixel-art
// pack NPCs are 16 units). Keyed by NPC id (src/data/npc-definitions.js), NOT by the pixel `sprite`
// key: sprite-registry.js builds `npcsHd[id]` and prop-renderer.js prefers it over the pixel set
// for that id, falling back per NPC when a file is missing (same boot-resilience as hero/tiles/props).
const HD_NPC_NORM_DIR = 'hd/npcs/normalized-hd/';

export const HD_NPC_IDS = Object.freeze([
  'elderRowan', 'villagerMia', 'hunterLena', 'merchantDale', 'fishermanRoy', 'carpenterBrol',
  'farmerGuri', 'guardRook', 'childTim', 'healerNina',
]);

// Sheet layout contract (must match tools/build-hd-npcs.py): 4 idle frames per row, rows =
// DOWN (front), LEFT, RIGHT. No rear pose -- renderers map 'up' to 'down'.
export const HD_NPC = Object.freeze({ frame: 192, pixelsPerUnit: 8, cols: 4, rows: ['down', 'left', 'right'] });

/** Spread into ASSET_FILES: `hdNpc<Id>` -> idle sheet, `hdNpc<Id>Face` -> 152x152 portrait. */
export const HD_NPC_ASSET_FILES = Object.freeze(Object.fromEntries(HD_NPC_IDS.flatMap((id) => [
  [sheetKey(id), `${HD_NPC_NORM_DIR}${id}.png`],
  [faceKey(id), `${HD_NPC_NORM_DIR}${id}_face.png`],
])));

export function sheetKey(id) {
  return `hdNpc${id[0].toUpperCase()}${id.slice(1)}`;
}

export function faceKey(id) {
  return `${sheetKey(id)}Face`;
}
