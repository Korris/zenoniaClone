// Builds the ground-tile canvas registry: the pixel-art cut (GROUND_TILES) merged with the HD
// hand-painted cut (HD_TILES) where its source sheet loaded -- HD wins per tile name, same
// merge-by-name convention as sprite-registry.js's buildHero(). See docs/system-architecture.md
// "HD Assets" for why a missing HD sheet is a silent per-name fallback, not a boot failure.
import { cropSprite, cell } from './sprite-cell.js';
import { GROUND_TILES, HD_TILES, HD_TILE_CELL, HD_TILE_PIXELS_PER_UNIT } from './asset-atlas.js';
import { generatePortalTileFrames } from './tile-art-generator.js';

const hdCell = (img, cx, cy) => cropSprite(img, cx * HD_TILE_CELL, cy * HD_TILE_CELL, HD_TILE_CELL, HD_TILE_CELL, false, HD_TILE_PIXELS_PER_UNIT);

/** Variant frames as an array; `macro` (if any) rides along so tile-variant-picker.js can see it. */
function variantFrames(img, def, cropFn) {
  const frames = def.variants.map(([cx, cy]) => cropFn(img, cx, cy));
  if (def.macro) frames.macro = def.macro;
  return frames;
}

function buildFromDefs(defs, images, cropFn) {
  const tiles = {};
  for (const [name, def] of Object.entries(defs)) {
    const img = images[def.image];
    if (!img) continue; // sheet not loaded/generated yet -- caller falls back per name
    if (def.block) {
      const [bx, by] = def.block;
      tiles[name] = [0, 1, 2].map((row) => [0, 1, 2].map((col) => cropFn(img, bx + col, by + row)));
      // Seamless extras (HD only): see HD_TILES in asset-atlas.js for what each one means.
      if (def.center) tiles[name + 'Center'] = variantFrames(img, def.center, cropFn);
      if (def.inner) tiles[name + 'Inner'] = def.inner.map(([cx, cy]) => cropFn(img, cx, cy));
      if (def.shapes) tiles[name + 'Shapes'] = Object.fromEntries(Object.entries(def.shapes).map(([k, [cx, cy]]) => [k, cropFn(img, cx, cy)]));
      if (def.fill && images[def.fill.image]) tiles[name + 'Fill'] = variantFrames(images[def.fill.image], def.fill, cropFn);
    } else {
      tiles[name] = variantFrames(img, def, cropFn);
    }
  }
  return tiles;
}

/**
 * @returns {{tiles: object, tilesHd: boolean}} `tiles` keyed by tile name (GROUND_TILES ∪
 *   HD-only names like `flowers`/`waterAnim`), each either a flat frame array (variants) or a
 *   3x3 nested array (block). `tilesHd` is true when at least one HD sheet loaded -- the renderer
 *   uses it to toggle smoothing and to pick the HD-only draw paths (flower tiles, water animation).
 */
export function buildTiles(images) {
  const pixel = buildFromDefs(GROUND_TILES, images, cell);
  pixel.portal = generatePortalTileFrames(); // only the pixel pack needs a procedural fallback
  const hd = buildFromDefs(HD_TILES, images, hdCell);
  return { tiles: { ...pixel, ...hd }, tilesHd: Object.keys(hd).length > 0 };
}
