// Frost Hollow: the first battle zone east of Oakhaven, drawn as ONE painted backdrop
// (assets/hd/map/FrostHollow/ground.webp, see tools/build-frost-hollow-art.py) in the Zenonia 5
// field style (ChatGPT, researched from the original Zenonia 5 environment art), winter palette:
// snowy clearings, a frozen river with one wooden bridge, cliff ledges, rune pillars, two mine caves.
// GROUND_ROWS were classified from that painting tile by tile (tile-grid-preview.png), so they must
// be re-derived if the backdrop is repainted: they only drive collision and the minimap.
//   T snowy pines (solid)  C cliff / rock / ruin (solid)  w frozen river  B bridge  s snow / stone ground
import { gridToRows } from '../map-builder.js';

const GROUND_ROWS = [
  'CCCCCCCCCCCCCCCCCwwwCCCTTTTTTCCCCCCC',
  'CCCCCCCCCCCCCCCCCwwwCCCTTTTTTCCCCCCC',
  'CCCCCCCCCCsTTCTTTwwwCCCTTCCCCCCCssCC', // exit portal (33,2) on the purple circle
  'CCCCsssCCsssCCCTTwwwCCCTssCssCCCssCC',
  'TCCCsssssssssssTTTwwCCCTsssssCCsssCC',
  'TssCsssssssssssTTTwwCCCTssssssssssCC',
  'TCCCCsssssssssTTTCwwCCCTTsssssssssTT',
  'TTCCTCCssssssCCCCCwwwCCTTsCsssssssTT',
  'TTTTTsssssTssCCCCCwwwCCTTsCssssssCTT',
  'TTTCCssssCCCCCCCCCwwwCCsCCsssssssCTT',
  'TTTCCssCCCCssCCCCCwwwCCsssssssCCCCTT',
  'TTTTCsssCsssssssssCCCCCssssssssCCTTT',
  'TTCCCCsssssssssCCBBBBBsssssCCssCCCCC', // bridge row 12 crosses the river at cols 17-21
  'TTTTCCssssssCCTCCCwwwCCTsssCCssTCCCC',
  'TTTCCssssssssCTTCCwwCCTTsssCsssTCCCC',
  'TTTCCsssssssCCTTCCwwCCCTCssssssTCCCT',
  'TTTCCssssssssCCCCCwwCCCTssssssssCCCT',
  'TTTCCssCsCsssCCCCCwwCCCTssssssssssTT',
  'TCCCssCCCCTTTTTTTTwwCCCCCTssssssCCCT',
  'TCCsssCCCCTTTTTTTTwwCCCCTTsssCCCCCCT', // entrance portal (5,19) on the blue circle
  'TTssssCCCTTTTTTTTTwwCCCCCCCCCCCCCCCC',
  'TTssssCsTTTTTTTTTTwwCCCCCCCCCCCCCCCC',
  // Rows 22-23 sit under the HUD bar, so they are blocked (the painted forest runs on beneath it).
  'CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC',
  'CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC',
];

const ENTRANCE = { tx: 5, ty: 19 };
const EXIT = { tx: 33, ty: 2 };
const PLAYER_START = { tx: 4, ty: 21 }; // foot of the entrance stairs

function build() {
  const grid = {
    width: GROUND_ROWS[0].length, height: GROUND_ROWS.length,
    cells: GROUND_ROWS.map((r) => r.split('')), props: [], occupied: new Set(),
  };
  grid.cells[ENTRANCE.ty][ENTRANCE.tx] = 'P';
  grid.cells[EXIT.ty][EXIT.tx] = 'P';
  return grid;
}

const grid = build();

export const FROST_HOLLOW_MAP = {
  id: 'frostHollow',
  name: 'Frost Hollow',
  backdrop: 'hdFrostHollowGround',
  rows: gridToRows(grid),
  props: grid.props, // everything is painted into the backdrop
  npcs: [],
  playerStart: PLAYER_START,
  portals: [
    { ...ENTRANCE, targetMap: 'oakhavenVillage', targetTx: 16, targetTy: 19, minLevel: 1 },
    { ...EXIT, targetMap: 'verdantMeadow', targetTx: 4, targetTy: 15, minLevel: 2 },
  ],
  // One pack per painted clearing; the entrance circle and stairs (cols 2-6, rows 18-21) stay monster-free.
  spawnZones: [
    { type: 'frostSlime', count: 5, area: { tx: 5, ty: 4, w: 9, h: 5 }, respawnMs: 8000 },     // north-west clearing (cave, bones)
    { type: 'frostSlime', count: 3, area: { tx: 8, ty: 13, w: 5, h: 4 }, respawnMs: 9000 },    // west clearing by the stone path
    { type: 'iceGoblin', count: 4, area: { tx: 25, ty: 5, w: 8, h: 5 }, respawnMs: 10000 },    // north-east clearing
    { type: 'frostSlime', count: 2, area: { tx: 25, ty: 5, w: 8, h: 5 }, respawnMs: 8000 },
    { type: 'spectralWolf', count: 4, area: { tx: 25, ty: 14, w: 6, h: 4 }, respawnMs: 11000 }, // south-east clearing (mine cave)
  ],
  boss: { type: 'frostfangKing', tx: 7, ty: 5, localBoss: true },
};
