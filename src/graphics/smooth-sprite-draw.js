// Cheap high-quality sprite drawing. Chrome's imageSmoothingQuality 'high' (and 'medium') re-filter
// the whole source on every drawImage; measured, it was over half the GPU process time per frame
// (walk: 19% GPU with 'high' vs 8% with 'low'). Instead, each source gets a lazily built chain of
// half-size copies ("mip levels"), each downscaled ONCE with 'high' quality. Per frame we draw from
// the smallest level that is still >= the on-screen size, with cheap bilinear ('low') filtering --
// so the GPU never shrinks by more than 2x and the result matches the 'high' look.

const MIN_LEVEL_SIZE = 8;
const levelCache = new WeakMap(); // source image/canvas -> [source, 1/2, 1/4, ...]

function makeCanvas(w, h) {
  if (typeof OffscreenCanvas !== 'undefined') return new OffscreenCanvas(w, h);
  const canvas = document.createElement('canvas');
  canvas.width = w;
  canvas.height = h;
  return canvas;
}

function halfOf(level) {
  const half = makeCanvas(Math.max(1, Math.round(level.width / 2)), Math.max(1, Math.round(level.height / 2)));
  const c = half.getContext('2d');
  c.imageSmoothingEnabled = true;
  c.imageSmoothingQuality = 'high';
  c.drawImage(level, 0, 0, half.width, half.height);
  return half;
}

/**
 * Smallest cached level whose width/height still cover `deviceW` x `deviceH` physical pixels.
 * @returns {{level: CanvasImageSource, factor: number}} factor = level size / source size
 */
export function pickSpriteLevel(source, deviceW, deviceH) {
  const srcW = source.width, srcH = source.height;
  let levels = levelCache.get(source);
  if (!levels) { levels = [source]; levelCache.set(source, levels); }
  let i = 0;
  // Step down while the NEXT level would still be at least as large as the destination.
  while (srcW / 2 ** (i + 1) >= deviceW && srcH / 2 ** (i + 1) >= deviceH
    && Math.min(srcW, srcH) / 2 ** (i + 1) >= MIN_LEVEL_SIZE) {
    if (!levels[i + 1]) levels[i + 1] = halfOf(levels[i]);
    i++;
  }
  return { level: levels[i], factor: levels[i].width / srcW };
}

/** Physical pixels per world/logical unit along x and y for the current transform (rotation-safe). */
function deviceScale(ctx) {
  const t = ctx.getTransform?.();
  if (!t) return [Infinity, Infinity]; // unknown transform (test doubles): keep the full-size source
  return [Math.hypot(t.a, t.b), Math.hypot(t.c, t.d)];
}

/** drawImage(source, dx, dy, dw, dh) with smoothing, at 'high' look and 'low' cost. */
export function drawSmoothImage(ctx, source, dx, dy, dw = source.width, dh = source.height) {
  const [sx, sy] = deviceScale(ctx);
  const { level } = pickSpriteLevel(source, Math.abs(dw) * sx, Math.abs(dh) * sy);
  ctx.imageSmoothingEnabled = true;
  ctx.imageSmoothingQuality = 'low';
  ctx.drawImage(level, dx, dy, dw, dh);
}

/** Region form: drawImage(source, sx, sy, sw, sh, dx, dy, dw, dh) via the same level chain. */
export function drawSmoothImageRegion(ctx, source, sx, sy, sw, sh, dx, dy, dw, dh) {
  const [scaleX, scaleY] = deviceScale(ctx);
  // Pick by the whole source scaled to the same ratio as the region.
  const ratioX = Math.abs(dw) * scaleX / sw, ratioY = Math.abs(dh) * scaleY / sh;
  const { level, factor } = pickSpriteLevel(source, source.width * ratioX, source.height * ratioY);
  ctx.imageSmoothingEnabled = true;
  ctx.imageSmoothingQuality = 'low';
  ctx.drawImage(level, sx * factor, sy * factor, sw * factor, sh * factor, dx, dy, dw, dh);
}
