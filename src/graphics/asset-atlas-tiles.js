// Tile, flower and prop cut tables for the Ninja Adventure pack + the HD painted ground tiles. Pure data,
// re-exported by asset-atlas.js (split out to keep that file small).

// Ground tiles: single cells or 3x3 auto-tile blocks (block = [image, cx, cy] of the top-left cell).
export const GROUND_TILES = Object.freeze({
  grass: { image: 'field', variants: [[3, 3], [4, 3], [3, 4], [4, 4]] },
  darkGrass: { image: 'field', variants: [[3, 6], [4, 6], [3, 7], [4, 7]] },
  sand: { image: 'field', variants: [[3, 0], [4, 0], [3, 1], [4, 1]] },
  dirtBlock: { image: 'field', block: [0, 0] },
  plazaStone: { image: 'field', block: [0, 0] }, // pixel fallback only: the plaza reads as a dirt block
  waterBlock: { image: 'water', block: [0, 6] },
  bridge: { image: 'water', variants: [[5, 13]] },
  cliffTop: { image: 'relief', variants: [[5, 5]] },
  cliffWall: { image: 'relief', variants: [[5, 6]] },
  cliffBottom: { image: 'relief', variants: [[5, 7]] },
  caveWallTop: { image: 'relief', variants: [[5, 0]] },
  caveWall: { image: 'relief', variants: [[5, 1]] },
  caveFloor: { image: 'interior', variants: [[14, 13], [15, 13], [14, 14], [15, 14]] },
  castleFloor: { image: 'interior', variants: [[13, 8], [12, 8], [13, 9], [12, 9]] },
  castleWall: { image: 'wall', variants: [[0, 8]] },
  lava: { image: 'floor', variants: [[16, 22]] },
});

// Decorations drawn over grass for the flower tile ('q')
export const FLOWER_OVERLAYS = Object.freeze([['nature', 0, 11], ['nature', 3, 11], ['nature', 7, 11], ['nature', 2, 10]]);

// HD hand-painted ground tiles: same shape as GROUND_TILES (variants / 3x3 block) but cells are
// 64px (pixelsPerUnit 4) on the normalized sheets built by tools/build-hd-tiles.py. Keyed by the
// SAME sprite names as GROUND_TILES where there's a 1:1 equivalent, so tile-sprite-builder.js can
// merge HD over pixel per-name; `flowers` and `waterAnim` have no pixel-art equivalent (the pixel
// pack overlays flowers on grass and has no animated water variant).
// Seamless extras (see tile-block-renderer.js / tile-variant-picker.js):
// - `macro: [cols, rows]` -- the variants are quadrants of ONE seamless texture; the renderer picks
//   variant (ty % rows) * cols + (tx % cols) so neighbouring tiles continue each other's texture.
// - `center` -- macro variants for a block's open center cells (replaces the single 3x3 center).
// - `inner` -- 4 grass wedge masks [TL, TR, BL, BR] drawn over concave (inner) corners.
// - `fill` -- a macro material drawn THROUGH the block cells' alpha (stone through the dirt
//   silhouette): the block cells then only act as transition masks.
// - `shapes` -- pieces the 3x3 block lacks, keyed by which sides are OPEN (same family):
//   'TB'/'LR' 1-wide pipes, 'T'/'B'/'L'/'R' dead-end caps, '' a lone island.
export const HD_TILE_CELL = 64;
const PATH_SHAPES = Object.freeze({ TB: [0, 4], LR: [1, 4], T: [2, 4], B: [3, 4], L: [4, 4], R: [0, 5], '': [1, 5] });
const PATH_INNER = Object.freeze([[4, 0], [4, 1], [4, 2], [4, 3]]); // px per cell in the normalized sheets = 16 world units * pixelsPerUnit
export const HD_TILE_PIXELS_PER_UNIT = 4;
export const HD_TILES = Object.freeze({
  grass: { image: 'hdGrass', variants: [[0, 0], [1, 0], [2, 0], [3, 0]], macro: [2, 2] },
  // Same grass quadrants with painted flowers on top (kind 'flowers' draws these directly in HD).
  flowers: { image: 'hdGrass', variants: [[0, 1], [1, 1], [2, 1], [3, 1]], macro: [2, 2] },
  dirtBlock: {
    image: 'hdPath', block: [0, 0], inner: PATH_INNER, shapes: PATH_SHAPES,
    center: { variants: [[0, 3], [1, 3], [2, 3], [3, 3]], macro: [2, 2] },
  },
  waterBlock: { image: 'hdWater', block: [0, 0] },
  // Open-water animation: the block's own center cell + the 2 extra variants on row3 (col0/col1).
  waterAnim: { image: 'hdWater', variants: [[1, 1], [0, 3], [1, 3]] },
  bridge: { image: 'hdWater', variants: [[2, 3]] },
  sand: { image: 'hdFloors', variants: [[0, 0], [1, 0]], macro: [2, 1] },
  castleFloor: { image: 'hdFloors', variants: [[2, 0], [3, 0]], macro: [2, 1] },
  // Oakhaven plaza: stone-on-grass auto-tile. Uses the path block's cells purely as alpha masks and
  // draws the seamless stone macro (plaza.png) through them, sharing the dirt path's inner corners.
  plazaStone: {
    image: 'hdPath', block: [0, 0], inner: PATH_INNER, shapes: PATH_SHAPES,
    fill: { image: 'hdPlaza', variants: [[0, 0], [1, 0], [0, 1], [1, 1]], macro: [2, 2] },
  },
  caveFloor: { image: 'hdFloors', variants: [[0, 1], [1, 1]], macro: [2, 1] },
  lava: { image: 'hdFloors', variants: [[2, 1], [3, 1]], macro: [2, 1] },
  cliffTop: { image: 'hdWalls', variants: [[0, 0]] },
  cliffWall: { image: 'hdWalls', variants: [[1, 0]] },
  cliffBottom: { image: 'hdWalls', variants: [[2, 0]] },
  castleWall: { image: 'hdWalls', variants: [[3, 0]] },
  caveWall: { image: 'hdWalls', variants: [[4, 0]] },
  caveWallTop: { image: 'hdWalls', variants: [[4, 0]] }, // no distinct cave-top art; reuse caveWall
  portal: { image: 'hdPortal', variants: [[0, 0], [1, 0], [2, 0]] },
});

// Props: [image, cx, cy, w, h] in cells. Footprints live in world/prop-definitions.js.
export const PROP_RECTS = Object.freeze({
  pine: ['nature', 0, 2, 2, 3], pineDark: ['nature', 2, 2, 2, 3], roundTree: ['nature', 4, 2, 3, 3],
  smallTree: ['nature', 0, 0, 2, 2], deadTree: ['nature', 4, 0, 2, 2], bigTree: ['nature', 3, 18, 3, 3],
  bush: ['nature', 0, 10, 1, 1], bush2: ['nature', 1, 10, 1, 1], stones: ['nature', 15, 18, 1, 1],
  rockBrown: ['nature', 12, 5, 3, 3], rockGrey: ['nature', 16, 5, 3, 3],
  rockSmallGrey: ['nature', 16, 8, 2, 2], rockSmallBrown: ['nature', 13, 8, 2, 2], stump: ['nature', 0, 8, 2, 2],
  palm: ['desert', 10, 4, 2, 3], cactus: ['desert', 14, 10, 1, 2], bones: ['desert', 18, 11, 2, 1],
  house: ['house', 0, 0, 4, 3], house2: ['house', 4, 0, 4, 3], house3: ['house', 8, 0, 4, 3], elderHouse: ['house', 8, 0, 4, 3],
  fence: ['house', 11, 10, 1, 1], signpost: ['house', 4, 3, 1, 1], pillar: ['house', 0, 19, 1, 3],
  torch: ['dungeon', 3, 2, 1, 2], crystal: ['dungeon', 4, 2, 1, 2],
});
