// Camera in world pixels. Follows a target and clamps to map bounds; supports screen shake.
// Pure logic (no DOM) so it is unit-testable.
import { VIEW_WIDTH, VIEW_HEIGHT, TILE_SIZE, PIXEL_SCALE } from './constants.js';

// Snap to whole SCREEN pixels (1/PIXEL_SCALE world px), not whole world pixels: a 4px screen step
// with an uneven 1-1-2 cadence made the painted HD backdrop judder while the hero stayed sub-pixel.
const snapToScreenPixel = (v) => Math.round(v * PIXEL_SCALE) / PIXEL_SCALE;

export class Camera {
  constructor() {
    this.x = 0;
    this.y = 0;
    this.mapPixelWidth = VIEW_WIDTH;
    this.mapPixelHeight = VIEW_HEIGHT;
    this.shakeTime = 0;
    this.shakeStrength = 0;
    this.shakeOffsetX = 0;
    this.shakeOffsetY = 0;
  }

  setMapSize(tilesWide, tilesHigh) {
    this.mapPixelWidth = tilesWide * TILE_SIZE;
    this.mapPixelHeight = tilesHigh * TILE_SIZE;
  }

  /** Center on (targetX, targetY), clamped so the view never leaves the map. */
  follow(targetX, targetY) {
    this.x = this.clampAxis(targetX - VIEW_WIDTH / 2, this.mapPixelWidth, VIEW_WIDTH);
    this.y = this.clampAxis(targetY - VIEW_HEIGHT / 2, this.mapPixelHeight, VIEW_HEIGHT);
  }

  clampAxis(value, mapSize, viewSize) {
    if (mapSize <= viewSize) return (mapSize - viewSize) / 2; // center small maps
    return Math.max(0, Math.min(value, mapSize - viewSize));
  }

  /** Snap directly to a position (used on map change to avoid a visible pan). */
  snapTo(targetX, targetY) {
    this.follow(targetX, targetY);
  }

  shake(strength = 2, durationMs = 150) {
    this.shakeStrength = Math.max(this.shakeStrength, strength);
    this.shakeTime = Math.max(this.shakeTime, durationMs);
  }

  update(dt, rand = Math.random) {
    if (this.shakeTime > 0) {
      this.shakeTime -= dt;
      this.shakeOffsetX = (rand() * 2 - 1) * this.shakeStrength;
      this.shakeOffsetY = (rand() * 2 - 1) * this.shakeStrength;
      if (this.shakeTime <= 0) this.shakeStrength = 0;
    } else {
      this.shakeOffsetX = 0;
      this.shakeOffsetY = 0;
    }
  }

  /** Render offsets aligned to screen pixels (setWorldTransform then aligns to device pixels). */
  get renderX() {
    return snapToScreenPixel(this.x + this.shakeOffsetX);
  }

  get renderY() {
    return snapToScreenPixel(this.y + this.shakeOffsetY);
  }

  /** Whether a world-space rectangle is at least partially visible. */
  isVisible(x, y, w, h) {
    return x + w >= this.x && x <= this.x + VIEW_WIDTH && y + h >= this.y && y <= this.y + VIEW_HEIGHT;
  }
}
