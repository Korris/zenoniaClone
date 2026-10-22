// Tile grid built from a map definition. Owns solidity lookups (tiles + prop footprints) and
// axis-separated movement resolution for entity collision boxes. Pure logic (no DOM).
import { TILE_SIZE } from '../core/constants.js';
import { getTileInfo } from './tile-char-info.js';

export class Tilemap {
  /** @param {string[]} rows one string per tile row, one char per tile */
  constructor(rows) {
    this.rows = rows;
    this.height = rows.length;
    this.width = rows.reduce((max, row) => Math.max(max, row.length), 0);
    this.blockedByProps = new Set(); // "tx,ty" keys covered by solid prop footprints
  }

  get pixelWidth() {
    return this.width * TILE_SIZE;
  }

  get pixelHeight() {
    return this.height * TILE_SIZE;
  }

  charAt(tx, ty) {
    if (ty < 0 || ty >= this.height || tx < 0 || tx >= this.width) return null;
    return this.rows[ty][tx] ?? null;
  }

  /** Mark a rectangle of tiles solid (used for tree trunks, houses, fences...). */
  addSolidFootprint(tx, ty, w, h) {
    for (let y = ty; y < ty + h; y++) for (let x = tx; x < tx + w; x++) this.blockedByProps.add(`${x},${y}`);
  }

  /** Out-of-bounds counts as solid so entities can never leave the map. */
  isSolidTile(tx, ty) {
    const ch = this.charAt(tx, ty);
    if (ch === null) return true;
    if (this.blockedByProps.has(`${tx},${ty}`)) return true;
    return getTileInfo(ch).solid;
  }

  /** Tile char under a world pixel position. */
  charAtWorld(x, y) {
    return this.charAt(Math.floor(x / TILE_SIZE), Math.floor(y / TILE_SIZE));
  }

  /** True if any solid tile overlaps the world-space rectangle. */
  isRectBlocked(x, y, w, h) {
    const left = Math.floor(x / TILE_SIZE);
    const right = Math.floor((x + w - 1) / TILE_SIZE);
    const top = Math.floor(y / TILE_SIZE);
    const bottom = Math.floor((y + h - 1) / TILE_SIZE);
    for (let ty = top; ty <= bottom; ty++) {
      for (let tx = left; tx <= right; tx++) {
        if (this.isSolidTile(tx, ty)) return true;
      }
    }
    return false;
  }

  /**
   * Move a box by (dx, dy), resolving X then Y, sliding along walls.
   * @returns {{x:number, y:number, blockedX:boolean, blockedY:boolean}} new top-left
   */
  moveBox(box, dx, dy) {
    const sweptX = this.sweepAxis(box.x, box.y, box.w, box.h, dx, true);
    const sweptY = this.sweepAxis(sweptX.pos, box.y, box.w, box.h, dy, false);
    return { x: sweptX.pos, y: sweptY.pos, blockedX: sweptX.blocked, blockedY: sweptY.blocked };
  }

  /** Advances one axis in 1px steps until the box would hit a solid tile. */
  sweepAxis(x, y, w, h, delta, isX) {
    let pos = isX ? x : y;
    if (delta === 0) return { pos, blocked: false };
    const sign = Math.sign(delta);
    let remaining = Math.abs(delta);
    while (remaining > 1e-9) {
      const step = Math.min(1, remaining);
      const next = pos + step * sign;
      const blocked = isX ? this.isRectBlocked(next, y, w, h) : this.isRectBlocked(x, next, w, h);
      if (blocked) return { pos, blocked: true };
      pos = next;
      remaining -= step;
    }
    return { pos, blocked: false };
  }

  /** Nearest walkable tile to (tx, ty) within radius, or null. */
  findWalkableNear(tx, ty, radius = 3) {
    for (let r = 0; r <= radius; r++) {
      for (let dy = -r; dy <= r; dy++) {
        for (let dx = -r; dx <= r; dx++) {
          if (!this.isSolidTile(tx + dx, ty + dy)) return { tx: tx + dx, ty: ty + dy };
        }
      }
    }
    return null;
  }
}
