// Map 3: open desert with cactus fields, lava cracks, fire slimes and goblin warriors.
import {
  createGrid, fillRect, drawBorder, carvePath, placePortal, reserve, scatterProps, gridToRows,
} from '../map-builder.js';

function build() {
  const grid = createGrid(44, 30, 's');
  drawBorder(grid, 'C', 1);
  fillRect(grid, 12, 8, 6, 3, 'x');
  fillRect(grid, 28, 18, 7, 3, 'x');
  fillRect(grid, 20, 3, 3, 6, 'x');
  carvePath(grid, [{ x: 3, y: 15 }, { x: 14, y: 15 }, { x: 14, y: 24 }, { x: 40, y: 24 }, { x: 40, y: 6 }], 'd', 2);
  placePortal(grid, 3, 15, 's'); // back to cave
  placePortal(grid, 40, 5, 'd'); // onward to castle
  reserve(grid, 4, 13, 4, 5);
  reserve(grid, 7, 13, 1, 1); // nomad
  reserve(grid, 2, 2, 40, 1);
  scatterProps(grid, 'cactus', 22, 301, ['s']);
  scatterProps(grid, 'palm', 8, 304, ['s']);
  scatterProps(grid, 'rockSmallBrown', 8, 302, ['s']);
  scatterProps(grid, 'rockBrown', 3, 305, ['s']);
  scatterProps(grid, 'bones', 6, 303, ['s']);
  return grid;
}

const grid = build();

export const SCORCHED_DESERT_MAP = {
  id: 'scorchedDesert',
  name: 'Scorched Desert',
  rows: gridToRows(grid),
  props: grid.props,
  npcs: [{ id: 'nomadSaif', tx: 7, ty: 13 }],
  playerStart: { tx: 5, ty: 15 },
  portals: [
    { tx: 3, ty: 15, targetMap: 'darkCave', targetTx: 34, targetTy: 26, minLevel: 1 },
    { tx: 40, ty: 5, targetMap: 'ruinedCastle', targetTx: 4, targetTy: 24, minLevel: 12 },
  ],
  spawnZones: [
    { type: 'fireSlime', count: 6, area: { tx: 6, ty: 3, w: 14, h: 9 }, respawnMs: 9000 },
    { type: 'goblinWarrior', count: 5, area: { tx: 20, ty: 10, w: 14, h: 8 }, respawnMs: 11000 },
    { type: 'bat', count: 4, area: { tx: 6, ty: 20, w: 12, h: 7 }, respawnMs: 10000 },
    { type: 'fireSlime', count: 5, area: { tx: 30, ty: 3, w: 10, h: 10 }, respawnMs: 9000 },
    { type: 'goblinWarrior', count: 5, area: { tx: 22, ty: 22, w: 18, h: 5 }, respawnMs: 11000 },
  ],
  boss: null,
};
