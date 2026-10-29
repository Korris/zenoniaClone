// Converts string pixel maps into offscreen canvases. Each string row is one pixel row,
// each char is a palette key. Rendered once, then drawn with drawImage (fast + crisp).
import { PALETTE } from './palette.js';

/**
 * Build a canvas from rows of palette characters.
 * @param {string[]} rows pixel map
 * @param {{flipX?:boolean, remap?:Record<string,string>, palette?:Record<string,string|null>}} opts
 *   remap substitutes characters before lookup (cheap recolor, e.g. fire slime from slime).
 */
export function createSprite(rows, opts = {}) {
  const { flipX = false, remap = null, palette = PALETTE } = opts;
  const height = rows.length;
  const width = rows.reduce((max, row) => Math.max(max, row.length), 0);
  const canvas = document.createElement('canvas');
  canvas.width = width;
  canvas.height = height;
  const ctx = canvas.getContext('2d');

  for (let y = 0; y < height; y++) {
    const row = rows[y];
    for (let x = 0; x < row.length; x++) {
      let ch = row[x];
      if (remap && remap[ch] !== undefined) ch = remap[ch];
      const color = palette[ch];
      if (!color) continue; // transparent or unknown char
      ctx.fillStyle = color;
      const drawX = flipX ? width - 1 - x : x;
      ctx.fillRect(drawX, y, 1, 1);
    }
  }
  return canvas;
}

/** Build an array of frame canvases from an array of pixel maps. */
export function createFrames(frameRows, opts = {}) {
  return frameRows.map((rows) => createSprite(rows, opts));
}

/** Compose an overlay pixel map on top of an existing base canvas (used for tiles). */
export function composeSprite(baseCanvas, overlayRows, opts = {}) {
  const overlay = createSprite(overlayRows, opts);
  const canvas = document.createElement('canvas');
  canvas.width = baseCanvas.width;
  canvas.height = baseCanvas.height;
  const ctx = canvas.getContext('2d');
  ctx.drawImage(baseCanvas, 0, 0);
  ctx.drawImage(overlay, 0, 0);
  return canvas;
}

/** Tint an existing canvas by overlaying a color (used for hit flash). */
// Hit-flash tint shared by the pixel and HD sprite builders.
// Fully opaque white: a crisp silhouette reads as a hit; 0.85 left the sprite pale/greyish.
export const HIT_FLASH_COLOR = 'rgb(255,255,255)';
// Warm rim drawn slightly larger behind the white flash so the silhouette still reads on snow/white ground.
export const HIT_RIM_COLOR = 'rgb(255,120,40)';

export function createTintedSprite(sourceCanvas, color) {
  const canvas = document.createElement('canvas');
  canvas.width = sourceCanvas.width;
  canvas.height = sourceCanvas.height;
  const ctx = canvas.getContext('2d');
  ctx.drawImage(sourceCanvas, 0, 0);
  ctx.globalCompositeOperation = 'source-atop';
  ctx.fillStyle = color;
  ctx.fillRect(0, 0, canvas.width, canvas.height);
  return canvas;
}
