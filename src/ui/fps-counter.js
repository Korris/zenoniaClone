// Always-on FPS readout (top-left, under the portrait). Counts real presented frames from the
// rAF timestamps and refreshes the number twice a second so it is readable, not flickering.
// Also shows the worst frame of the window: a single long frame is what players feel as a hitch.
import { setScreenTransform } from '../core/canvas-display.js';

const REFRESH_MS = 500;
const X = 14;
const Y = 118;

export class FpsCounter {
  constructor() {
    this.windowStart = 0;
    this.lastFrame = 0;
    this.frames = 0;
    this.worstMs = 0;
    this.fps = 0;
    this.shownWorstMs = 0;
  }

  /** Call once per presented frame with the rAF / performance.now() timestamp. */
  tick(now) {
    if (this.lastFrame) this.worstMs = Math.max(this.worstMs, now - this.lastFrame);
    this.lastFrame = now;
    if (!this.windowStart) this.windowStart = now;
    this.frames++;
    const elapsed = now - this.windowStart;
    if (elapsed >= REFRESH_MS) {
      this.fps = Math.round((this.frames * 1000) / elapsed);
      this.shownWorstMs = Math.round(this.worstMs);
      this.frames = 0;
      this.worstMs = 0;
      this.windowStart = now;
    }
  }

  /** Colour by health: green near 60, amber when dipping, red when clearly laggy. */
  color() {
    if (this.fps >= 55) return '#7dff9a';
    if (this.fps >= 40) return '#ffd66b';
    return '#ff6b6b';
  }

  render(ctx) {
    setScreenTransform(ctx);
    const text = `FPS ${this.fps}  ·  max ${this.shownWorstMs}ms`;
    ctx.font = '600 11px "Segoe UI", Arial, sans-serif';
    ctx.textAlign = 'left';
    ctx.textBaseline = 'middle';
    const w = ctx.measureText(text).width + 12;
    ctx.fillStyle = '#07111ecc';
    ctx.fillRect(X - 6, Y - 9, w, 18);
    ctx.fillStyle = this.color();
    ctx.fillText(text, X, Y);
  }
}
