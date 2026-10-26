// Map 1: meadow fields east of Oakhaven Village. River with a bridge, pine forest, slimes and
// bats. The village cottages/NPCs that used to live here moved to oakhaven-village-map.js; the
// old house/fence cluster stays as flavor (now unowned cottages) and the west start clearing
// gained a portal back to the village.
import {
  createGrid, fillRect, drawBorder, scatter, carvePath, placePortal, reserve, placeProp, scatterProps,
  propLine, forestBorder, gridToRows,
} from '../map-builder.js';

function build() {
  const grid = createGrid(40, 30, 'g');
  drawBorder(grid, 'C', 1);
  // dirt road west -> east, crossing the river on a wooden bridge
  carvePath(grid, [{ x: 3, y: 15 }, { x: 36, y: 15 }], 'd', 2);
  carvePath(grid, [{ x: 9, y: 10 }, { x: 9, y: 15 }], 'd', 2);
  fillRect(grid, 24, 2, 3, 26, 'w');
  fillRect(grid, 24, 14, 3, 3, 'B');
  reserve(grid, 2, 2, 36, 1); // keep the cliff lip clean
  // old cottages: two unowned houses behind a fence line with a gap for the road (NPCs who used
  // to live here moved to Oakhaven Village)
  placeProp(grid, 'house', 4, 6);
  placeProp(grid, 'house2', 12, 6);
  propLine(grid, 'fence', 3, 9, 6, 1, 0);
  propLine(grid, 'fence', 11, 9, 7, 1, 0);
  placeProp(grid, 'signpost', 6, 13);
  // safe start clearing (west) + portal back to Oakhaven Village
  reserve(grid, 2, 13, 5, 5);
  placePortal(grid, 3, 15, 'd');
  placePortal(grid, 37, 15, 'd');
  scatter(grid, 'q', 70, 103, ['g']);
  forestBorder(grid, 'pine', 5, 111, ['g', 'q']);
  scatterProps(grid, 'pine', 10, 112, ['g', 'q']);
  scatterProps(grid, 'roundTree', 5, 117, ['g', 'q']);
  scatterProps(grid, 'smallTree', 6, 118, ['g', 'q']);
  scatterProps(grid, 'bush', 10, 113, ['g', 'q']);
  scatterProps(grid, 'bush2', 6, 119, ['g', 'q']);
  scatterProps(grid, 'rockSmallGrey', 6, 114, ['g', 'q']);
  scatterProps(grid, 'stump', 4, 115, ['g', 'q']);
  scatterProps(grid, 'stones', 8, 116, ['g', 'q']);
  return grid;
}

const grid = build();

export const VERDANT_MEADOW_MAP = {
  id: 'verdantMeadow',
  name: 'Verdant Meadow',
  rows: gridToRows(grid),
  props: grid.props,
  npcs: [],
  playerStart: { tx: 4, ty: 15 },
  portals: [
    { tx: 3, ty: 15, targetMap: 'oakhavenVillage', targetTx: 16, targetTy: 19, minLevel: 1 },
    { tx: 37, ty: 15, targetMap: 'darkCave', targetTx: 4, targetTy: 4, minLevel: 3 },
  ],
  spawnZones: [
    { type: 'slime', count: 6, area: { tx: 14, ty: 3, w: 9, h: 9 }, respawnMs: 8000 },
    { type: 'slime', count: 4, area: { tx: 28, ty: 4, w: 8, h: 8 }, respawnMs: 8000 },
    { type: 'bat', count: 4, area: { tx: 6, ty: 20, w: 14, h: 7 }, respawnMs: 10000 },
    { type: 'slime', count: 3, area: { tx: 28, ty: 20, w: 8, h: 6 }, respawnMs: 8000 },
    { type: 'bat', count: 2, area: { tx: 30, ty: 10, w: 6, h: 4 }, respawnMs: 10000 },
  ],
  boss: null,
};
