// Bakes the enhance glow of a held weapon ONCE per (sprite, band): crisp stepped outline rings of the
// silhouette in the glow colour (anime outline, no blur/gradients, never shadowBlur at draw time).
// bands: 1 thin rim | 2 colour edge + white inner line | 3 adds two outer rings | 4 as 3, outer rings on a
// separate `aura` canvas so the renderer can pulse it.
export const GLOW_PAD = 12;
const SPECS = [
  null,
  { inner: [{ r: 2, a: 0.55, white: 0.5 }], outer: [] },
  { inner: [{ r: 3, a: 0.85 }, { r: 1, a: 0.9, white: 1 }], outer: [] },
  { inner: [{ r: 3, a: 0.85 }, { r: 1, a: 0.9, white: 1 }], outer: [{ r: 8, a: 0.2 }, { r: 5, a: 0.4 }] },
];
SPECS[4] = SPECS[3];
const cache = new WeakMap(); // image -> Map("color|band" -> baked)
const STEPS = 12;

function makeCanvas(w, h) {
  if (typeof OffscreenCanvas !== 'undefined') return new OffscreenCanvas(w, h);
  const c = document.createElement('canvas');
  c.width = w;
  c.height = h;
  return c;
}

function mixWhite(hex, amount) {
  const n = parseInt(hex.slice(1), 16);
  const mix = (v) => Math.round(v + (255 - v) * amount);
  return `rgb(${mix(n >> 16)},${mix((n >> 8) & 255)},${mix(n & 255)})`;
}

function tinted(sprite, color) {
  const c = makeCanvas(sprite.width, sprite.height);
  const x = c.getContext('2d');
  x.drawImage(sprite, 0, 0);
  x.globalCompositeOperation = 'source-in';
  x.fillStyle = color;
  x.fillRect(0, 0, c.width, c.height);
  return c;
}

function drawRings(ctx, sprite, rings, color) {
  for (const ring of rings) {
    const mask = tinted(sprite, ring.white ? mixWhite(color, ring.white) : color);
    ctx.globalAlpha = ring.a;
    for (let i = 0; i < STEPS; i++) {
      const a = (i / STEPS) * Math.PI * 2;
      ctx.drawImage(mask, GLOW_PAD + Math.cos(a) * ring.r, GLOW_PAD + Math.sin(a) * ring.r);
    }
  }
  ctx.globalAlpha = 1;
}

/**
 * @param {{img: CanvasImageSource, gripX: number, gripY: number}} art overlay art + grip
 * @returns {{body: CanvasImageSource, aura: CanvasImageSource|null, gripX: number, gripY: number}}
 *   band 0 returns the art untouched; bands 1..4 return canvases GLOW_PAD larger on every side.
 */
export function bakeStaffGlow(art, color, band) {
  if (!band) return { body: art.img, aura: null, gripX: art.gripX, gripY: art.gripY };
  let bands = cache.get(art.img);
  if (!bands) { bands = new Map(); cache.set(art.img, bands); }
  const key = `${color}|${band}`;
  if (bands.has(key)) return bands.get(key);
  const spec = SPECS[Math.min(4, band)];
  const w = art.img.width + GLOW_PAD * 2, h = art.img.height + GLOW_PAD * 2;
  const body = makeCanvas(w, h);
  const bctx = body.getContext('2d');
  drawRings(bctx, art.img, [...(band >= 4 ? [] : spec.outer), ...spec.inner], color); // band 4: outer rings go to the aura
  bctx.drawImage(art.img, GLOW_PAD, GLOW_PAD);
  let aura = null;
  if (band >= 4) {
    aura = makeCanvas(w, h);
    drawRings(aura.getContext('2d'), art.img, spec.outer, color);
  }
  const baked = { body, aura, gripX: art.gripX + GLOW_PAD, gripY: art.gripY + GLOW_PAD };
  bands.set(key, baked);
  return baked;
}
