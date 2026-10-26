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
  // fence/signpost: footprint stays a flat 1x1 tile, but the HD art (a tall wooden post, see
  // tools/build-hd-props.py) reads as a squashed stub in a 16x16 box -- same "taller than its
  // footprint" convention already used below by pillar/torch/crystal, so height is 1.5 tiles (24
  // world units) instead of 1.
  fence: def(1, 1.5, 1, 1), signpost: def(1, 1.5, 1, 1), pillar: def(1, 3, 1, 1),
  torch: def(1, 2, 1, 1), crystal: def(1, 2, 1, 1),
  // Oakhaven Village (v6) art, HD-only, cut from ChatGPT sheets by tools/build-oakhaven-village-art.py
  // (sizes MUST match its PROP_TILES). Buildings block a 2-row base so the painted doorstep in front
  // stays walkable; trees block only their trunk tile so the canopy can be walked behind.
  elderHouse: def(5.75, 5, 6, 2), cottageRed: def(3.5, 3.75, 3, 2), hunterLodge: def(4.25, 3.75, 3, 2),
  healerCottage: def(4.75, 3.75, 5, 2), carpenterWorkshop: def(6, 3.5, 6, 2),
  well: def(2.25, 2.5, 2, 1), lanternPost: def(1.25, 2.5, 1, 1), marketStall: def(2.75, 2.5, 3, 1),
  scarecrow: def(1.75, 2, 1, 1), woodpile: def(2.25, 1.875, 2, 1), campfire: def(1.75, 1.375, 1, 1),
  barrel: def(1, 1.25, 1, 1), crate: def(1.25, 1.125, 1, 1),
  rowboat: def(1.25, 2.25, 1, 2), // moored on water (already solid), so its flag only matters on land
  villageFence: def(2, 1, 2, 1), villageOak: def(3.5, 3.75, 1, 1), villagePine: def(2.25, 3.75, 1, 1),
  villageBush: def(1.75, 1.75, 1, 1), villageFlowerBush: def(2, 1.75, 1, 1), villageSignpost: def(1.5, 2.25, 1, 1),
});

export function getPropDefinition(type) {
  const found = PROP_DEFINITIONS[type];
  if (!found) throw new Error(`Unknown prop type: ${type}`);
  return found;
}
