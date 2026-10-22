// Shared crop-to-canvas helper used by every sprite builder (characters, props, tiles, FX).
// Split out from sprite-registry.js so tile-sprite-builder.js can import it without a circular
// dependency (sprite-registry.js imports buildTiles() from tile-sprite-builder.js).
import { TILE_SIZE } from '../core/constants.js';

/**
 * Copy a rectangle of an image into its own canvas (optionally mirrored).
 * @param {number} pixelsPerUnit source px per world unit -- 1 for the pixel-art pack (default),
 *   higher (e.g. 4) for HD sheets. Stamped onto the returned canvas so renderers know whether to
 *   draw it at its natural size (pixel art) or at an explicit world-unit dest size (HD).
 */
export function cropSprite(img, sx, sy, w, h, flipX = false, pixelsPerUnit = 1) {
  const canvas = document.createElement('canvas');
  canvas.width = w;
  canvas.height = h;
  const ctx = canvas.getContext('2d');
  if (flipX) {
    ctx.translate(w, 0);
    ctx.scale(-1, 1);
  }
  ctx.drawImage(img, sx, sy, w, h, 0, 0, w, h);
  canvas.pixelsPerUnit = pixelsPerUnit;
  return canvas;
}

/** 16px-tile crop (w, h in tile cells) from the pixel-art pack. */
export const cell = (img, cx, cy, w = 1, h = 1) => cropSprite(img, cx * TILE_SIZE, cy * TILE_SIZE, w * TILE_SIZE, h * TILE_SIZE);
