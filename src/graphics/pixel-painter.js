// Low-level pixel drawing helpers for procedural sprite generation. Everything draws
// 1px rectangles on a tiny canvas so the result stays crisp when scaled up 3x.
export function makeCanvas(width, height) {
  const canvas = document.createElement('canvas');
  canvas.width = width;
  canvas.height = height;
  return canvas;
}

export function px(ctx, x, y, color) {
  ctx.fillStyle = color;
  ctx.fillRect(x, y, 1, 1);
}

export function rect(ctx, x, y, w, h, color) {
  ctx.fillStyle = color;
  ctx.fillRect(x, y, w, h);
}

/** Filled ellipse by scanning rows; `shadeFn(x, y, nx, ny)` can override color per pixel. */
export function ellipse(ctx, cx, cy, rx, ry, color, shadeFn = null) {
  for (let y = Math.floor(cy - ry); y <= Math.ceil(cy + ry); y++) {
    for (let x = Math.floor(cx - rx); x <= Math.ceil(cx + rx); x++) {
      const nx = (x + 0.5 - cx) / rx;
      const ny = (y + 0.5 - cy) / ry;
      if (nx * nx + ny * ny <= 1) px(ctx, x, y, shadeFn ? shadeFn(x, y, nx, ny) : color);
    }
  }
}

/** Isosceles triangle pointing up with apex (ax, ay), base width w at y = ay + h. */
export function triangle(ctx, ax, ay, w, h, color, shadeFn = null) {
  for (let row = 0; row < h; row++) {
    const half = ((row + 1) / h) * (w / 2);
    for (let x = Math.ceil(ax - half); x < Math.floor(ax + half); x++) {
      const nx = (x + 0.5 - ax) / half;
      px(ctx, x, ay + row, shadeFn ? shadeFn(x, ay + row, nx, row / h) : color);
    }
  }
}

/** Sprinkle single pixels using a seeded RNG. */
export function speckle(ctx, w, h, color, density, rand) {
  for (let y = 0; y < h; y++) {
    for (let x = 0; x < w; x++) if (rand() < density) px(ctx, x, y, color);
  }
}

/** 1px outline around every non-transparent pixel of a canvas (gives props a clean rim). */
export function outline(canvas, color) {
  const ctx = canvas.getContext('2d');
  const { width, height } = canvas;
  const data = ctx.getImageData(0, 0, width, height).data;
  const solid = (x, y) => x >= 0 && y >= 0 && x < width && y < height && data[(y * width + x) * 4 + 3] > 0;
  const out = makeCanvas(width, height);
  const octx = out.getContext('2d');
  for (let y = 0; y < height; y++) {
    for (let x = 0; x < width; x++) {
      if (solid(x, y)) continue;
      if (solid(x - 1, y) || solid(x + 1, y) || solid(x, y - 1) || solid(x, y + 1)) px(octx, x, y, color);
    }
  }
  octx.drawImage(canvas, 0, 0);
  return out;
}

/** Shade helper: picks light / base / dark by horizontal position (light from the left). */
export function sideShade(light, base, dark) {
  return (x, y, nx) => (nx < -0.35 ? light : nx > 0.45 ? dark : base);
}
