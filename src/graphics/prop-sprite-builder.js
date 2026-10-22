// Builds the prop canvas registry: the pixel-art cut (PROP_RECTS) merged with the HD hand-painted
// cut (HD_PROPS) where its normalized file loaded -- HD wins per prop type, same merge-by-name
// convention as tile-sprite-builder.js's buildTiles() and sprite-registry.js's buildHero(). See
// docs/system-architecture.md "HD Assets" for why a missing HD file is a silent per-name fallback.
import { cropSprite, cell } from './sprite-cell.js';
import { PROP_RECTS, HD_PROPS, HD_PROP_PIXELS_PER_UNIT } from './asset-atlas.js';

function buildPixelProps(images) {
  const props = {};
  for (const [name, [image, cx, cy, w, h]] of Object.entries(PROP_RECTS)) props[name] = cell(images[image], cx, cy, w, h);
  return props;
}

/** Each HD prop file IS the whole normalized canvas (already cropped/fit by tools/build-hd-props.py
 * to that prop type's exact world-size x4 box) -- just stamp pixelsPerUnit, no further slicing. */
function buildHdProps(images) {
  const props = {};
  for (const [name, imageKey] of Object.entries(HD_PROPS)) {
    const img = images[imageKey];
    if (!img) continue; // not generated yet -- buildProps() falls back to the pixel-art cut
    props[name] = cropSprite(img, 0, 0, img.width, img.height, false, HD_PROP_PIXELS_PER_UNIT);
  }
  return props;
}

export function buildProps(images) {
  return { ...buildPixelProps(images), ...buildHdProps(images) };
}
