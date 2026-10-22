// The only procedurally drawn tile left: the animated portal (no equivalent in the asset pack).
import { TILE_SIZE as T } from '../core/constants.js';
import { makeCanvas, px, rect } from './pixel-painter.js';

const PORTAL_COLORS = { base: '#2b2b38', rim: '#1b1b2a', ring: '#b43aff', ringLight: '#e0a0ff', core: '#ffffff' };

function portalTile(frame) {
  const c = makeCanvas(T, T);
  const ctx = c.getContext('2d');
  rect(ctx, 0, 0, T, T, PORTAL_COLORS.base);
  for (let y = 0; y < T; y++) {
    for (let x = 0; x < T; x++) {
      const d = Math.hypot(x - 7.5, y - 7.5);
      if (d > 7.5) continue;
      const band = (Math.floor(d) + frame) % 3;
      px(ctx, x, y, d > 6.5 ? PORTAL_COLORS.rim : band === 0 ? PORTAL_COLORS.ring : band === 1 ? PORTAL_COLORS.ringLight : PORTAL_COLORS.core);
    }
  }
  return c;
}

export function generatePortalTileFrames() {
  return [0, 1, 2].map(portalTile);
}
