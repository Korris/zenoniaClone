// Oakhaven Village: hand-composed safe hub. The layout favors readable districts and clear
// pedestrian lanes over random prop scattering. Keep the 36x26 world footprint stable for saves,
// portals, and the minimap. Props are placed intentionally so tall sprites frame spaces instead
// of obscuring NPCs or creating accidental dead ends.
import {
  createGrid, fillRect, drawBorder, carvePath, placePortal, reserve, placeProp, propLine, gridToRows,
} from '../map-builder.js';

function placeCottage(grid, type, tx, ty) {
  placeProp(grid, type, tx, ty);
  reserve(grid, tx, ty - 2, 4, 1); // roof overhang row
  reserve(grid, tx, ty + 1, 4, 1); // keep doorstep open
}

// Stable IDs are quest-data keys. All NPC anchors remain on reachable ground beside a route.
const NPC_TILES = [
  { id: 'elderRowan', tx: 18, ty: 10 },
  { id: 'villagerMia', tx: 7, ty: 8 },
  { id: 'childTim', tx: 18, ty: 17 },
  { id: 'healerNina', tx: 26, ty: 13 },
  { id: 'merchantDale', tx: 5, ty: 15 },
  { id: 'farmerGuri', tx: 7, ty: 21 },
  { id: 'carpenterBrol', tx: 26, ty: 21 },
  { id: 'fishermanRoy', tx: 29, ty: 19 },
  { id: 'guardRook', tx: 18, ty: 22 },
  { id: 'hunterLena', tx: 29, ty: 5 },
];

function build() {
  const grid = createGrid(36, 26, 'g');
  drawBorder(grid, 'C', 1);

  // Main routes use one-tile lanes and stepped turns. The broad central space is painted below
  // as a rounded stone plaza, rather than crossing full-width rectangular bands of dirt.
  carvePath(grid, [{ x: 17, y: 23 }, { x: 17, y: 20 }, { x: 16, y: 18 }, { x: 16, y: 11 }, { x: 17, y: 9 }, { x: 17, y: 7 }], 'd', 2);
  carvePath(grid, [{ x: 13, y: 14 }, { x: 8, y: 14 }, { x: 5, y: 15 }], 'd', 2);
  carvePath(grid, [{ x: 20, y: 14 }, { x: 26, y: 14 }, { x: 29, y: 15 }], 'd', 2);
  carvePath(grid, [{ x: 10, y: 19 }, { x: 10, y: 21 }, { x: 12, y: 22 }], 'd', 1);
  carvePath(grid, [{ x: 28, y: 15 }, { x: 28, y: 19 }, { x: 30, y: 20 }], 'd', 1);
  // Short feeder paths connect each doorstep to the main lanes, so the village reads as one
  // walkable settlement rather than isolated islands of props on grass.
  carvePath(grid, [{ x: 6, y: 9 }, { x: 6, y: 12 }, { x: 8, y: 14 }], 'd', 1);
  carvePath(grid, [{ x: 16, y: 8 }, { x: 16, y: 11 }], 'd', 1);
  carvePath(grid, [{ x: 24, y: 8 }, { x: 24, y: 12 }, { x: 26, y: 14 }], 'd', 1);
  carvePath(grid, [{ x: 23, y: 19 }, { x: 24, y: 20 }, { x: 26, y: 21 }], 'd', 1);
  // Short connecting pads keep doorways and the bridge approaches comfortably walkable.
  fillRect(grid, 5, 8, 2, 1, 'd');
  fillRect(grid, 15, 7, 3, 1, 'd');
  fillRect(grid, 24, 8, 2, 1, 'd');
  fillRect(grid, 28, 14, 2, 2, 'd');

  // River / bridge / dock. Keep the bridge aligned with the cross-lane and the dock reachable.
  fillRect(grid, 31, 1, 3, 24, 'w');
  fillRect(grid, 31, 14, 3, 2, 'B');
  fillRect(grid, 34, 14, 1, 2, 'd');
  fillRect(grid, 31, 20, 2, 2, 'B');
  reserve(grid, 34, 13, 1, 5);
  reserve(grid, 32, 23, 2, 1); // future rowboat landing

  // Residential north: three distinct roof colors, open front yards, and small garden boundaries.
  placeCottage(grid, 'house', 4, 6);
  // The elder's larger blue-roof residence is the main northern landmark.
  placeProp(grid, 'elderHouse', 14, 8);
  reserve(grid, 14, 5, 7, 2); // protect the wider manor roof and wing overhang
  placeCottage(grid, 'house3', 22, 6);
  // A fourth cottage reuses the existing house sprite as the carpenter's workshop; it is
  // deliberately separated from the plaza so the work yard has a clear landmark.
  placeCottage(grid, 'house2', 21, 18);
  // Small healer's cottage on the east side of the plaza. Reuse a coherent existing house sprite
  // instead of a placeholder icon, and leave a doorstep facing the path.
  placeCottage(grid, 'house', 23, 11);
  propLine(grid, 'fence', 3, 7, 2, 1, 0);
  placeProp(grid, 'fence', 8, 7);
  // Short fence posts frame the elder's front approach without blocking the doorway.
  placeProp(grid, 'fence', 12, 7);
  placeProp(grid, 'fence', 18, 7);
  propLine(grid, 'fence', 21, 7, 2, 1, 0);
  placeProp(grid, 'bush2', 8, 7);
  placeProp(grid, 'bush', 11, 8);
  placeProp(grid, 'bush2', 20, 8);
  placeProp(grid, 'bush', 10, 7);
  placeProp(grid, 'bush2', 19, 7);
  placeProp(grid, 'bush', 27, 8);
  placeProp(grid, 'bush', 3, 5);
  placeProp(grid, 'bush2', 9, 5);
  placeProp(grid, 'bush', 19, 5);
  placeProp(grid, 'bush2', 26, 5);
  fillRect(grid, 2, 9, 2, 1, 'q');
  fillRect(grid, 9, 9, 2, 1, 'q');
  fillRect(grid, 20, 9, 2, 1, 'q');
  fillRect(grid, 23, 10, 1, 2, 'q');
  fillRect(grid, 2, 11, 1, 2, 'q');
  fillRect(grid, 9, 10, 2, 1, 'q');
  fillRect(grid, 22, 11, 1, 2, 'q');
  fillRect(grid, 27, 12, 1, 2, 'q');
  fillRect(grid, 2, 4, 2, 1, 'q');
  fillRect(grid, 8, 4, 2, 1, 'q');
  fillRect(grid, 11, 9, 1, 2, 'q');
  fillRect(grid, 19, 9, 1, 2, 'q');
  fillRect(grid, 28, 10, 1, 2, 'q');
  fillRect(grid, 2, 16, 1, 2, 'q');
  fillRect(grid, 11, 17, 1, 2, 'q');
  fillRect(grid, 22, 19, 1, 2, 'q');
  fillRect(grid, 28, 21, 1, 2, 'q');
  fillRect(grid, 3, 3, 2, 1, 'q');
  fillRect(grid, 7, 3, 2, 1, 'q');
  fillRect(grid, 13, 4, 2, 1, 'q');
  fillRect(grid, 18, 4, 2, 1, 'q');
  fillRect(grid, 23, 4, 2, 1, 'q');
  fillRect(grid, 27, 8, 1, 2, 'q');
  fillRect(grid, 2, 14, 1, 2, 'q');
  fillRect(grid, 10, 18, 1, 2, 'q');
  fillRect(grid, 19, 21, 1, 2, 'q');
  fillRect(grid, 29, 15, 1, 2, 'q');
  fillRect(grid, 11, 12, 1, 2, 'q');
  fillRect(grid, 21, 12, 1, 2, 'q');
  fillRect(grid, 11, 17, 1, 2, 'q');
  fillRect(grid, 21, 17, 1, 2, 'q');
  fillRect(grid, 28, 7, 1, 2, 'q');
  fillRect(grid, 29, 18, 1, 1, 'q');

  // Central plaza: stepped, softly irregular outline instead of a 7x7 square. Its outer edge
  // transitions directly into grass auto-tiles, while the well and two small planted corners form
  // a readable landmark without filling the entire walkable area with dirt.
  // Stone paving uses a dedicated plaza tile semantic and a stepped silhouette; the stone joints
  // are intentional masonry rather than the repeated dirt grid visible on the old path tiles.
  fillRect(grid, 16, 11, 3, 1, 'p');
  fillRect(grid, 14, 12, 6, 1, 'p');
  fillRect(grid, 13, 13, 8, 1, 'p');
  fillRect(grid, 12, 14, 10, 3, 'p');
  fillRect(grid, 13, 17, 8, 1, 'p');
  fillRect(grid, 14, 18, 6, 1, 'p');
  fillRect(grid, 16, 19, 3, 1, 'p');
  reserve(grid, 11, 12, 12, 7);
  placeProp(grid, 'well', 15, 15);
  placeProp(grid, 'lanternPost', 12, 13);
  placeProp(grid, 'lanternPost', 21, 17);
  placeProp(grid, 'bush2', 12, 17);
  placeProp(grid, 'bush', 21, 12);
  placeProp(grid, 'bush2', 10, 11);
  placeProp(grid, 'bush', 10, 17);
  placeProp(grid, 'bush2', 22, 18);
  placeProp(grid, 'bush', 12, 20);
  placeProp(grid, 'bush2', 20, 20);
  // Small flower patches soften the plaza's grass edge; keep the actual lanes clear.
  fillRect(grid, 10, 12, 1, 2, 'q');
  fillRect(grid, 22, 16, 1, 2, 'q');
  fillRect(grid, 13, 11, 2, 1, 'q');
  fillRect(grid, 20, 11, 2, 1, 'q');
  fillRect(grid, 12, 18, 2, 1, 'q');
  fillRect(grid, 20, 18, 2, 1, 'q');

  // Market west: a compact stall with a clear customer-facing lane and grouped storage props.
  reserve(grid, 3, 13, 4, 3);
  placeProp(grid, 'marketStall', 2, 11);
  placeProp(grid, 'marketStall', 2, 14);
  placeProp(grid, 'marketStall', 6, 14);
  placeProp(grid, 'barrel', 8, 15);
  placeProp(grid, 'crate', 9, 15);
  placeProp(grid, 'roundTree', 10, 12);
  placeProp(grid, 'bush2', 9, 16);

  // Farm southwest: fenced enclosure with a wide two-tile gate on the east side.
  propLine(grid, 'fence', 3, 18, 8, 1, 0);
  propLine(grid, 'fence', 3, 23, 8, 1, 0);
  propLine(grid, 'fence', 3, 19, 4, 0, 1);
  placeProp(grid, 'fence', 10, 19);
  placeProp(grid, 'fence', 10, 22);
  fillRect(grid, 4, 19, 6, 4, 'q');
  reserve(grid, 4, 19, 6, 4);
  // Crop rows and a small scarecrow turn the enclosure into a readable farm landmark.
  placeProp(grid, 'cropPlot', 4, 20);
  placeProp(grid, 'cropPlot', 6, 20);
  placeProp(grid, 'cropPlot', 4, 22);
  placeProp(grid, 'cropPlot', 6, 22);
  placeProp(grid, 'cropPlot', 8, 20);
  placeProp(grid, 'cropPlot', 8, 22);
  placeProp(grid, 'scarecrow', 8, 21);
  placeProp(grid, 'lanternPost', 11, 19);

  // Carpenter's yard southeast: a reused cottage acts as the workshop; stumps, timber and crates form its yard.
  reserve(grid, 21, 18, 4, 2);
  reserve(grid, 21, 21, 5, 2);
  placeProp(grid, 'woodpile', 21, 22);
  placeProp(grid, 'barrel', 25, 22);
  placeProp(grid, 'crate', 26, 22);
  placeProp(grid, 'campfire', 24, 24);
  placeProp(grid, 'rockGrey', 22, 24);
  placeProp(grid, 'rockSmallBrown', 26, 23);

  // Fishing bank and river approach. The NPC stands on the bank, never on water.
  placeProp(grid, 'lanternPost', 30, 22);
  placeProp(grid, 'rockSmallGrey', 28, 23);
  placeProp(grid, 'stones', 30, 18);
  placeProp(grid, 'rowboat', 32, 23);
  // One continuous decorative bridge overlay unifies the repeated walkable bridge tiles.
  placeProp(grid, 'bridgeArch', 31, 15);

  // Natural framing: clustered tree lines along the west and outer edges, plus small accents near
  // residential gardens. These are hand-authored instead of pseudo-randomly scattered.
  placeProp(grid, 'bigTree', 1, 4);
  placeProp(grid, 'pine', 8, 4);
  placeProp(grid, 'pineDark', 12, 5);
  placeProp(grid, 'pine', 22, 4);
  placeProp(grid, 'roundTree', 1, 17);
  placeProp(grid, 'smallTree', 1, 16);
  placeProp(grid, 'pine', 1, 20);
  placeProp(grid, 'roundTree', 2, 24);
  placeProp(grid, 'pineDark', 12, 24);
  placeProp(grid, 'smallTree', 29, 11);
  placeProp(grid, 'bush2', 22, 12);
  placeProp(grid, 'bush', 27, 17);
  placeProp(grid, 'stones', 27, 23);
  // More deliberate framing clusters: canopy along the north/west edges and reeds/rocks on the
  // riverbank. Keep them off the path centerline and NPC anchors.
  placeProp(grid, 'pineDark', 5, 3);
  placeProp(grid, 'pine', 8, 3);
  placeProp(grid, 'roundTree', 10, 3);
  placeProp(grid, 'pineDark', 17, 3);
  placeProp(grid, 'pine', 20, 3);
  placeProp(grid, 'roundTree', 24, 3);
  placeProp(grid, 'smallTree', 28, 3);
  placeProp(grid, 'bigTree', 2, 11);
  placeProp(grid, 'smallTree', 2, 15);
  placeProp(grid, 'bush2', 29, 9);
  placeProp(grid, 'bush', 29, 13);
  placeProp(grid, 'rockSmallGrey', 29, 17);
  placeProp(grid, 'bush2', 29, 22);
  placeProp(grid, 'bush', 29, 10);
  placeProp(grid, 'stones', 30, 12);
  placeProp(grid, 'bush2', 29, 13);
  placeProp(grid, 'rockSmallBrown', 29, 16);

  // Reserve the NPC footprints and player spawn so props never visually cover them.
  for (const n of NPC_TILES) reserve(grid, n.tx, n.ty, 1, 1);
  reserve(grid, 16, 21, 1, 1);
  placePortal(grid, 16, 23, 'd');
  // Village entrance framing: the portal remains reachable, while the posts and fence wings read
  // as a gate without introducing a new opaque background asset.
  placeProp(grid, 'pillar', 14, 24);
  placeProp(grid, 'pillar', 20, 24);
  propLine(grid, 'fence', 9, 24, 5, 1, 0);
  propLine(grid, 'fence', 21, 24, 5, 1, 0);
  placeProp(grid, 'signpost', 22, 22);

  return grid;
}

const grid = build();

export const OAKHAVEN_VILLAGE_MAP = {
  id: 'oakhavenVillage',
  name: 'Oakhaven Village',
  rows: gridToRows(grid),
  props: grid.props,
  npcs: NPC_TILES,
  playerStart: { tx: 16, ty: 21 },
  portals: [
    { tx: 16, ty: 23, targetMap: 'verdantMeadow', targetTx: 4, targetTy: 15, minLevel: 1 },
  ],
  spawnZones: [],
  boss: null,
};
