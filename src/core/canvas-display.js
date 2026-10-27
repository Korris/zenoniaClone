import { CANVAS_WIDTH, CANVAS_HEIGHT, PIXEL_SCALE } from './constants.js';

// Keep all gameplay/UI coordinates logical; only the physical drawing buffer changes.
export function configureCanvasDisplay(canvas, viewportWidth, viewportHeight, pixelRatio = 1) {
  const width = Math.max(1, Math.min(CANVAS_WIDTH, viewportWidth, viewportHeight * CANVAS_WIDTH / CANVAS_HEIGHT));
  const height = width * CANVAS_HEIGHT / CANVAS_WIDTH;
  const density = Math.max(1, Math.min(2, pixelRatio || 1));
  const physicalWidth = Math.max(1, Math.round(width * density));
  const physicalHeight = Math.max(1, Math.round(height * density));
  if (canvas.style.width !== `${width}px`) canvas.style.width = `${width}px`;
  if (canvas.style.height !== `${height}px`) canvas.style.height = `${height}px`;
  if (canvas.width !== physicalWidth) canvas.width = physicalWidth;
  if (canvas.height !== physicalHeight) canvas.height = physicalHeight;
}

export function setScreenTransform(ctx) {
  ctx.setTransform(ctx.canvas.width / CANVAS_WIDTH, 0, 0, ctx.canvas.height / CANVAS_HEIGHT, 0, 0);
}

export function setWorldTransform(ctx, camera) {
  const sx = ctx.canvas.width / CANVAS_WIDTH * PIXEL_SCALE;
  const sy = ctx.canvas.height / CANVAS_HEIGHT * PIXEL_SCALE;
  ctx.setTransform(sx, 0, 0, sy, -Math.round(camera.renderX * sx), -Math.round(camera.renderY * sy));
}

export function logicalPointerPosition(canvas, clientX, clientY) {
  const rect = canvas.getBoundingClientRect();
  return { x: (clientX - rect.left) * CANVAS_WIDTH / rect.width,
    y: (clientY - rect.top) * CANVAS_HEIGHT / rect.height };
}
