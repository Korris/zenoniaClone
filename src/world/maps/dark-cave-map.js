// Map 2: winding cave with rock walls, lava pools, goblins and skeletons. Miner Bork hides here.
import {
  createGrid, fillRect, drawBorder, placePortal, reserve, scatterProps, propLine, gridToRows,
} from '../map-builder.js';

function build() {
  const grid = createGrid(40, 30, 'n');
  drawBorder(grid, 'R', 2);
  // zig-zag wall segments force the player to weave through monster lairs
  fillRect(grid, 10, 2, 2, 18, 'R');
  fillRect(grid, 20, 10, 2, 18, 'R');
  fillRect(grid, 30, 2, 2, 18, 'R');
  fillRect(grid, 14, 22, 5, 3, 'x');
  fillRect(grid, 24, 4, 4, 3, 'x');
  placePortal(grid, 4, 3, 'n'); // back to meadow
  placePortal(grid, 36, 26, 'n'); // onward to desert
  reserve(grid, 2, 4, 6, 4);
  reserve(grid, 6, 7, 1, 1); // miner
  propLine(grid, 'torch', 13, 9, 3, 2, 0);
  propLine(grid, 'torch', 23, 20, 3, 2, 0);
  scatterProps(grid, 'rockSmallGrey', 14, 201, ['n']);
  scatterProps(grid, 'rockGrey', 4, 204, ['n']);
  scatterProps(grid, 'bones', 6, 202, ['n']);
  scatterProps(grid, 'crystal', 6, 203, ['n']);
  return grid;
}

const grid = build();

export const DARK_CAVE_MAP = {
  id: 'darkCave',
  name: 'Dark Cave',
  rows: gridToRows(grid),
  props: grid.props,
  npcs: [{ id: 'minerBork', tx: 6, ty: 7 }],
  playerStart: { tx: 4, ty: 6 },
  portals: [
    { tx: 4, ty: 3, targetMap: 'verdantMeadow', targetTx: 35, targetTy: 15, minLevel: 1 },
    { tx: 36, ty: 26, targetMap: 'scorchedDesert', targetTx: 4, targetTy: 15, minLevel: 7 },
  ],
  spawnZones: [
    { type: 'goblin', count: 5, area: { tx: 3, ty: 10, w: 6, h: 16 }, respawnMs: 9000 },
    { type: 'skeleton', count: 4, area: { tx: 13, ty: 3, w: 6, h: 16 }, respawnMs: 11000 },
    { type: 'goblin', count: 5, area: { tx: 23, ty: 10, w: 6, h: 16 }, respawnMs: 9000 },
    { type: 'bat', count: 3, area: { tx: 13, ty: 26, w: 16, h: 2 }, respawnMs: 10000 },
    { type: 'skeleton', count: 5, area: { tx: 33, ty: 4, w: 5, h: 18 }, respawnMs: 11000 },
  ],
  boss: null,
};
