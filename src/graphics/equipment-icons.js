import { cropSprite } from './sprite-cell.js';

// Find transparent gutters independently per row, so uneven painted spacing never clips icons.
function splitBounds(profile, count) {
  const bounds = [0];
  for (let i = 1; i < count; i++) {
    const guess = Math.round(i * profile.length / count);
    const range = Math.floor(profile.length / count * 0.25);
    let best = guess;
    for (let p = Math.max(bounds.at(-1) + 1, guess - range); p <= Math.min(profile.length - 1, guess + range); p++) {
      if (profile[p] < profile[best] || (profile[p] === profile[best] && Math.abs(p - guess) < Math.abs(best - guess))) best = p;
    }
    bounds.push(best);
  }
  bounds.push(profile.length);
  return bounds;
}

/** Five columns by two rows, with isolated items (including multi-part equipment). */
export function buildArmorIcons(sheet) {
  if (!sheet) return [];
  const canvas = cropSprite(sheet, 0, 0, sheet.width, sheet.height);
  const { width, height } = canvas;
  const data = canvas.getContext('2d').getImageData(0, 0, width, height).data;
  const profile = new Uint32Array(height);
  for (let y = 0; y < height; y++) for (let x = 0; x < width; x++) {
    if (data[(y * width + x) * 4 + 3] >= 32) profile[y]++;
  }
  const rows = splitBounds(profile, 2), icons = [];
  for (let row = 0; row < 2; row++) {
    const columns = new Uint32Array(width);
    for (let y = rows[row]; y < rows[row + 1]; y++) for (let x = 0; x < width; x++) {
      if (data[(y * width + x) * 4 + 3] >= 32) columns[x]++;
    }
    const bounds = splitBounds(columns, 5);
    for (let col = 0; col < 5; col++) {
      let left = width, right = -1, top = height, bottom = -1;
      for (let y = rows[row]; y < rows[row + 1]; y++) for (let x = bounds[col]; x < bounds[col + 1]; x++) {
        if (data[(y * width + x) * 4 + 3] < 32) continue;
        left = Math.min(left, x); right = Math.max(right, x);
        top = Math.min(top, y); bottom = Math.max(bottom, y);
      }
      if (right < left) return []; // existing inventory artwork remains available
      icons.push(cropSprite(sheet, left, top, right - left + 1, bottom - top + 1));
    }
  }
  return icons;
}
