// Maps the single characters used in map definition strings to ground tile metadata.
// `kind` tells the renderer how to draw it (see tilemap-renderer.js); `solid` blocks movement.
// Trees, rocks, fences, houses... are PROPS (see map definition `props`), not tiles.
export const TILE_INFO = Object.freeze({
  g: { kind: 'variants', sprite: 'grass', solid: false },
  q: { kind: 'flowers', sprite: 'grass', solid: false },
  d: { kind: 'block', sprite: 'dirtBlock', base: 'grass', solid: false },
  w: { kind: 'block', sprite: 'waterBlock', base: 'grass', solid: true },
  C: { kind: 'cliff', sprite: 'cliffWall', solid: true },
  s: { kind: 'variants', sprite: 'sand', solid: false },
  f: { kind: 'variants', sprite: 'castleFloor', solid: false },
  p: { kind: 'block', sprite: 'plazaStone', base: 'grass', solid: false }, // stone-on-grass auto-tile
  '#': { kind: 'variants', sprite: 'castleWall', solid: true },
  n: { kind: 'variants', sprite: 'caveFloor', solid: false },
  R: { kind: 'cliff', sprite: 'caveWall', top: 'caveWallTop', solid: true },
  x: { kind: 'variants', sprite: 'lava', solid: true },
  B: { kind: 'variants', sprite: 'bridge', solid: false },
  P: { kind: 'animated', sprite: 'portal', solid: false },
});

/** Returns tile info for a char, falling back to grass for unknown chars. */
export function getTileInfo(ch) {
  return TILE_INFO[ch] || TILE_INFO.g;
}

/** Chars that count as "same family" for block auto-tiling (a path next to a bridge has no rim). */
// Dirt and plaza stone are mutual family: a road meets the paved plaza with a butt joint, not a grass rim.
const EDGE_FAMILY = Object.freeze({ d: 'dBPp', w: 'wB', p: 'pdBP' }); // only block tiles need families

export function sameEdgeFamily(ch, neighbour) {
  if (neighbour === null) return true; // map border: no rim
  const family = EDGE_FAMILY[ch] || ch;
  return family.includes(neighbour);
}
