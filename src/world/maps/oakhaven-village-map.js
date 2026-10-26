// Oakhaven Village (v6): safe starting hub drawn as ONE painted backdrop
// (assets/hd/map/oakhaven-v6/ground.webp, see tools/build-oakhaven-village-art.py) with HD buildings
// and props y-sorted on top. GROUND_ROWS were classified from that painting tile by tile, so they
// must be re-derived if the backdrop is repainted: they only drive collision and the minimap.
//   T forest canopy (solid)  g grass  d dirt road / market / farm soil  p plaza  w river  B bridge/dock
import { placeProp, reserve, gridToRows } from '../map-builder.js';

const GROUND_ROWS = [
  'TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTwwwTTT',
  'TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTwwwTTT',
  'TTTggTgggggTgggggggggTgggggggTwwwTTT',
  'TTggggggggggggggggggggggggggggTwwTTT',
  'TTggggggggggggggddgggggggggggggwwTTT',
  'TTgggdgggggggggggdgggggggggdggTwwTgT',
  'TTTggdggggggggggddgggggggggdgggwwTgT',
  'TTggddddggggggggddgggggggggdgggwwTgT',
  'TTgddddddgggggggpppggggdgggdggTwwwgT',
  'TTgdddddddgggggpppppgggdgggdggTwwTgT',
  'TTddddddddddddppppppddddddddddBBBBdd',
  'TTgddddddggggdppppppddddddddddBBBBdd',
  'TTgggggggggggggpppppggggggggdgTwwTgT',
  'TTTgggggggggddggppgddgggggggdgTwwTgT',
  'TTgggggggggdgggggdggddgggggggdTwwggT',
  'TTddddddddgdgggggdggggdggggggdgwwTTT',
  'TTddddddddggggggddgggggddgggggBBwwTT',
  'TgddddddddggggggddggggggdggggggwwwTT',
  'TTddddddddggggggddgggggggggggggwwwTT',
  'TTddddddddggggggdggggggggggggggwwTTT',
  'TTggggggggggTgggdgggggggggggTgTwwwTT',
  'TTTTgTTgTTTTTTTgdggTTTTggggTTTTwwwTT',
  // Rows 22-23: the painted road runs on under the HUD bar, so it is blocked (portal sits at row 21).
  'TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTwwTTT',
  'TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTwwTTT',
];

// Stable IDs are quest-data keys. Each villager stands on open ground in front of their home/work.
const NPC_TILES = [
  { id: 'elderRowan', tx: 17, ty: 7 },
  { id: 'villagerMia', tx: 5, ty: 7 },
  { id: 'hunterLena', tx: 27, ty: 7 },
  { id: 'healerNina', tx: 23, ty: 9 },
  { id: 'merchantDale', tx: 8, ty: 10 },
  { id: 'childTim', tx: 19, ty: 13 },
  { id: 'farmerGuri', tx: 8, ty: 16 },
  { id: 'fishermanRoy', tx: 30, ty: 15 },
  { id: 'carpenterBrol', tx: 24, ty: 20 },
  { id: 'guardRook', tx: 18, ty: 20 },
];
const PLAYER_START = { tx: 16, ty: 19 };
const PORTAL = { tx: 16, ty: 21 };

// [type, tx, ty]: tx/ty = bottom-left footprint tile (see prop-definitions.js). Buildings sit over
// the end of their painted feeder road so the road visibly leads to the door.
const PROPS = [
  // Homes and workshops
  ['elderHouse', 14, 5], ['cottageRed', 4, 5], ['hunterLodge', 26, 5],
  ['healerCottage', 21, 8], ['carpenterWorkshop', 22, 19],
  // Plaza: well at the centre, lanterns at the edges, signpost toward the south gate
  ['well', 16, 11], ['lanternPost', 14, 8], ['lanternPost', 20, 12], ['villageSignpost', 18, 15],
  // Market square (west)
  ['marketStall', 7, 8], ['barrel', 2, 9], ['crate', 2, 10],
  // Farm (southwest): fence along the north edge, scarecrow in the field
  ['villageFence', 2, 14], ['villageFence', 4, 14], ['villageFence', 6, 14], ['scarecrow', 5, 17],
  // Carpenter's yard
  ['woodpile', 28, 18], ['campfire', 21, 19], ['barrel', 21, 18],
  // River: lantern at the bridge head, rowboat moored by the dock
  ['lanternPost', 29, 9], ['rowboat', 32, 18],
  // Greenery
  ['villageOak', 11, 5], ['villageOak', 12, 14], ['villageOak', 25, 14],
  ['villagePine', 21, 3], ['villagePine', 11, 20], ['villagePine', 2, 4],
  ['villageFlowerBush', 13, 7], ['villageBush', 20, 7], ['villageFlowerBush', 9, 13], ['villageBush', 26, 12],
];

function build() {
  const grid = {
    width: GROUND_ROWS[0].length, height: GROUND_ROWS.length,
    cells: GROUND_ROWS.map((r) => r.split('')), props: [], occupied: new Set(),
  };
  for (const n of NPC_TILES) reserve(grid, n.tx, n.ty, 1, 1);
  reserve(grid, PLAYER_START.tx, PLAYER_START.ty, 1, 1);
  grid.cells[PORTAL.ty][PORTAL.tx] = 'P';
  for (const [type, tx, ty] of PROPS) placeProp(grid, type, tx, ty);
  return grid;
}

const grid = build();

export const OAKHAVEN_VILLAGE_MAP = {
  id: 'oakhavenVillage',
  name: 'Oakhaven Village',
  backdrop: 'hdOakhavenGround',
  rows: gridToRows(grid),
  props: grid.props,
  npcs: NPC_TILES,
  playerStart: PLAYER_START,
  portals: [
    { ...PORTAL, targetMap: 'verdantMeadow', targetTx: 4, targetTy: 15, minLevel: 1 },
  ],
  spawnZones: [],
  boss: null,
};
