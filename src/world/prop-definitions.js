// Pure metadata for map props (no DOM): sprite size in px, solid footprint in tiles at the
// base, and whether it blocks movement. The anchor tile is the prop's bottom-left footprint
// tile; the canvas is centered horizontally on the footprint and sits on its bottom edge.
const def = (w, h, fw, fh, solid = true) => ({ width: w * 16, height: h * 16, footprint: { w: fw, h: fh }, solid });

export const PROP_DEFINITIONS = Object.freeze({
  pine: def(2, 3, 2, 1), pineDark: def(2, 3, 2, 1), roundTree: def(3, 3, 3, 1),
  smallTree: def(2, 2, 2, 1), deadTree: def(2, 2, 2, 1), bigTree: def(3, 3, 3, 1),
  bush: def(1, 1, 1, 1), bush2: def(1, 1, 1, 1), stones: def(1, 1, 1, 1, false),
  rockBrown: def(3, 3, 3, 2), rockGrey: def(3, 3, 3, 2), rockSmallGrey: def(2, 2, 2, 1), rockSmallBrown: def(2, 2, 2, 1),
  stump: def(2, 2, 2, 1), palm: def(2, 3, 2, 1), cactus: def(1, 2, 1, 1), bones: def(2, 1, 2, 1, false),
  house: def(4, 3, 4, 2), house2: def(4, 3, 4, 2), house3: def(4, 3, 4, 2),
  // A larger focal building used for the village elder's residence. The sprite canvas is 6x4.5 tiles,
  // while the walk-blocking base remains 6x3 tiles so its front steps stay accessible.
  elderHouse: def(7, 5.25, 7, 3),
  // fence/signpost: footprint stays a flat 1x1 tile, but the HD art (a tall wooden post, see
  // tools/build-hd-props.py) reads as a squashed stub in a 16x16 box -- same "taller than its
  // footprint" convention already used below by pillar/torch/crystal, so height is 1.5 tiles (24
  // world units) instead of 1.
  fence: def(1, 1.5, 1, 1), signpost: def(1, 1.5, 1, 1), pillar: def(1, 3, 1, 1),
  torch: def(1, 2, 1, 1), crystal: def(1, 2, 1, 1),
  // Village props still WITHOUT art (no HD sheet, no pixel-art cut): sizes are the contract for
  // the raw sheet `assets/hd/props/village_props.png` (see tools/build-hd-props.py SHEETS). They are
  // not in HD_PROPS/PROP_RECTS yet, so placing one on a map draws nothing and fails
  // tests/hd-props-config.test.js until the art lands -- oakhaven-village-map.js only reserves
  // their spots. cropPlot is walkable (like stones/bones) so the six planned rows don't wall off
  // the farm interior; rowboat sits on water (already solid), so its flag only matters on land.
  well: def(2, 3, 2, 2), lanternPost: def(1, 2, 1, 1), marketStall: def(3, 3, 3, 2),
  bridgeArch: def(3, 2, 3, 1, false),
  cropPlot: def(2, 1, 2, 1, false), scarecrow: def(1, 2, 1, 1), woodpile: def(2, 2, 2, 1),
  campfire: def(1, 1, 1, 1), barrel: def(1, 1, 1, 1), crate: def(1, 1, 1, 1), rowboat: def(2, 1, 2, 1),
});

export function getPropDefinition(type) {
  const found = PROP_DEFINITIONS[type];
  if (!found) throw new Error(`Unknown prop type: ${type}`);
  return found;
}
