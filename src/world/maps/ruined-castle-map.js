// Map 4: final castle. Stone halls with pillars, skeleton knights, and the Golem boss.
import {
  createGrid, fillRect, drawBorder, placePortal, reserve, scatterProps, propLine, gridToRows,
} from '../map-builder.js';

function build() {
  const grid = createGrid(36, 28, 'f');
  drawBorder(grid, '#', 2);
  // Two long halls separated by inner walls with doorways
  fillRect(grid, 2, 13, 14, 2, '#');
  fillRect(grid, 20, 13, 14, 2, '#');
  fillRect(grid, 17, 2, 2, 7, '#');
  fillRect(grid, 17, 18, 2, 8, '#');
  // Throne room walls (boss arena) with a single southern doorway
  fillRect(grid, 22, 2, 12, 1, '#');
  fillRect(grid, 22, 2, 1, 9, '#');
  fillRect(grid, 22, 10, 5, 1, '#');
  fillRect(grid, 29, 10, 5, 1, '#');
  fillRect(grid, 6, 20, 3, 2, 'x');
  fillRect(grid, 26, 20, 3, 2, 'x');
  placePortal(grid, 3, 24, 'f');
  reserve(grid, 3, 22, 5, 4);
  reserve(grid, 6, 22, 1, 1); // ghost knight
  reserve(grid, 27, 4, 4, 4); // boss spot
  propLine(grid, 'pillar', 4, 5, 4, 3, 0);
  propLine(grid, 'pillar', 4, 19, 4, 3, 0);
  propLine(grid, 'pillar', 24, 17, 4, 3, 0);
  scatterProps(grid, 'bones', 10, 401, ['f']);
  scatterProps(grid, 'torch', 6, 402, ['f']);
  return grid;
}

const grid = build();

export const RUINED_CASTLE_MAP = {
  id: 'ruinedCastle',
  name: 'Ruined Castle',
  rows: gridToRows(grid),
  props: grid.props,
  npcs: [{ id: 'knightAldric', tx: 6, ty: 22 }],
  playerStart: { tx: 5, ty: 24 },
  portals: [
    { tx: 3, ty: 24, targetMap: 'scorchedDesert', targetTx: 38, targetTy: 6, minLevel: 1 },
  ],
  spawnZones: [
    { type: 'skeletonKnight', count: 5, area: { tx: 3, ty: 16, w: 12, h: 8 }, respawnMs: 12000 },
    { type: 'skeleton', count: 4, area: { tx: 20, ty: 16, w: 12, h: 8 }, respawnMs: 11000 },
    { type: 'skeletonKnight', count: 5, area: { tx: 3, ty: 3, w: 12, h: 8 }, respawnMs: 12000 },
    { type: 'goblinWarrior', count: 3, area: { tx: 20, ty: 11, w: 12, h: 1 }, respawnMs: 12000 },
  ],
  boss: { type: 'golemBoss', tx: 28, ty: 6 },
};
