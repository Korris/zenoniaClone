// Full-screen loading view used at boot (shared assets) and after the hero pick (class assets). Always paints an
// opaque background + label + progress bar, so the canvas is never a black screen while images stream in.
import { CANVAS_WIDTH, CANVAS_HEIGHT } from '../core/constants.js';
import { setScreenTransform } from '../core/canvas-display.js';

const BAR_W = 360;
const BAR_H = 12;

/**
 * @param {CanvasRenderingContext2D} ctx
 * @param {string} label e.g. "Loading assets" or "Preparing Wizard"
 * @param {number} loaded files done
 * @param {number} total files to load (0 => bar stays empty)
 */
export function drawLoadingScreen(ctx, label, loaded, total) {
  setScreenTransform(ctx);
  ctx.fillStyle = '#0b0b14';
  ctx.fillRect(0, 0, CANVAS_WIDTH, CANVAS_HEIGHT);
  ctx.fillStyle = '#ffe680';
  ctx.font = 'bold 24px "Courier New", monospace';
  ctx.textAlign = 'center';
  ctx.fillText(`${label} ${loaded}/${total}`, CANVAS_WIDTH / 2, CANVAS_HEIGHT / 2);
  const x = (CANVAS_WIDTH - BAR_W) / 2, y = CANVAS_HEIGHT / 2 + 24;
  ctx.fillStyle = 'rgba(255,255,255,0.15)';
  ctx.fillRect(x, y, BAR_W, BAR_H);
  ctx.fillStyle = '#ffe680';
  ctx.fillRect(x, y, total > 0 ? BAR_W * Math.min(1, loaded / total) : 0, BAR_H);
}
