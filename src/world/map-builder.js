// Helpers for composing tile maps in code: fill, borders, paths, seeded scatter, and a prop
// layer (trees, rocks, fences...) that tracks occupied tiles so props never overlap.
import { seededRandom } from '../core/seeded-random.js';
import { getPropDefinition } from './prop-definitions.js';

export function createGrid(width, height, fill) {
  const cells = [];
  for (let y = 0; y < height; y++) cells.push(new Array(width).fill(fill));
  return { width, height, cells, props: [], occupied: new Set() };
}

export function fillRect(grid, x, y, w, h, ch) {
  for (let ty = y; ty < y + h; ty++) {
    for (let tx = x; tx < x + w; tx++) {
      if (ty >= 0 && ty < grid.height && tx >= 0 && tx < grid.width) grid.cells[ty][tx] = ch;
    }
  }
}

export function drawBorder(grid, ch, thickness = 1) {
  fillRect(grid, 0, 0, grid.width, thickness, ch);
  fillRect(grid, 0, grid.height - thickness, grid.width, thickness, ch);
  fillRect(grid, 0, 0, thickness, grid.height, ch);
  fillRect(grid, grid.width - thickness, 0, thickness, grid.height, ch);
}

/** Randomly place `count` copies of `ch` on cells whose current char is in `onlyOn`. */
export function scatter(grid, ch, count, seed, onlyOn) {
  const rand = seededRandom(seed);
  const allowed = new Set(onlyOn);
  let placed = 0;
  for (let attempts = 0; placed < count && attempts < count * 20; attempts++) {
    const x = Math.floor(rand() * grid.width);
    const y = Math.floor(rand() * grid.height);
    if (!allowed.has(grid.cells[y][x]) || grid.occupied.has(`${x},${y}`)) continue;
    grid.cells[y][x] = ch;
    placed++;
  }
}

/** Carve an L-shaped path (horizontal then vertical) between consecutive points. */
export function carvePath(grid, points, ch, width = 2) {
  for (let i = 0; i < points.length - 1; i++) {
    const a = points[i];
    const b = points[i + 1];
    const x0 = Math.min(a.x, b.x);
    const x1 = Math.max(a.x, b.x);
    fillRect(grid, x0, a.y, x1 - x0 + width, width, ch);
    const y0 = Math.min(a.y, b.y);
    const y1 = Math.max(a.y, b.y);
    fillRect(grid, b.x, y0, width, y1 - y0 + width, ch);
  }
}

/** Place a portal char with a walkable clearing around it so it is reachable. */
export function placePortal(grid, tx, ty, floorChar) {
  fillRect(grid, tx - 1, ty - 1, 3, 3, floorChar);
  grid.cells[ty][tx] = 'P';
  reserve(grid, tx - 1, ty - 1, 3, 3);
}

/** Mark tiles as taken so scatter/props skip them (spawn points, NPC spots, paths). */
export function reserve(grid, x, y, w, h) {
  for (let ty = y; ty < y + h; ty++) for (let tx = x; tx < x + w; tx++) grid.occupied.add(`${tx},${ty}`);
}

/**
 * A prop may be placed when its footprint tiles are free and on allowed ground AND the tiles
 * its sprite visually covers above the footprint are not reserved (so a tall pine never hides
 * an NPC, a portal or the start clearing).
 */
function footprintFree(grid, tx, ty, def, allowed) {
  const { w, h } = def.footprint;
  const visualRows = Math.ceil(def.height / 16);
  for (let y = ty - visualRows + 1; y <= ty; y++) {
    const inFootprint = y > ty - h;
    for (let x = tx; x < tx + w; x++) {
      if (x < 0 || y < 0 || x >= grid.width || y >= grid.height) return false;
      if (grid.occupied.has(`${x},${y}`)) return false;
      if (inFootprint && allowed && !allowed.has(grid.cells[y][x])) return false;
    }
  }
  return true;
}

/** Place one prop with its bottom-left footprint tile at (tx, ty). */
export function placeProp(grid, type, tx, ty) {
  const def = getPropDefinition(type);
  grid.props.push({ type, tx, ty });
  reserve(grid, tx, ty - def.footprint.h + 1, def.footprint.w, def.footprint.h);
}

/** Randomly place `count` props of `type` on free cells whose char is in `onlyOn`. */
export function scatterProps(grid, type, count, seed, onlyOn) {
  const rand = seededRandom(seed);
  const allowed = new Set(onlyOn);
  const def = getPropDefinition(type);
  let placed = 0;
  for (let attempts = 0; placed < count && attempts < count * 30; attempts++) {
    const tx = Math.floor(rand() * grid.width);
    const ty = Math.floor(rand() * grid.height);
    if (!footprintFree(grid, tx, ty, def, allowed)) continue;
    placeProp(grid, type, tx, ty);
    placed++;
  }
}

/** Row of identical props (fences, pillars) from (tx, ty) stepping by (dx, dy). */
export function propLine(grid, type, tx, ty, count, dx, dy) {
  for (let i = 0; i < count; i++) placeProp(grid, type, tx + dx * i, ty + dy * i);
}

/** Dense tree band along the map border (inside the cliff ring) for a forest feel. */
export function forestBorder(grid, type, inset, seed, onlyOn) {
  const rand = seededRandom(seed);
  const allowed = new Set(onlyOn);
  const def = getPropDefinition(type);
  for (let y = 0; y < grid.height; y++) {
    for (let x = 0; x < grid.width; x++) {
      const nearEdge = x < inset || y < inset || x >= grid.width - inset || y >= grid.height - inset;
      if (!nearEdge || rand() > 0.55 || !footprintFree(grid, x, y, def, allowed)) continue;
      placeProp(grid, type, x, y);
    }
  }
}

export function gridToRows(grid) {
  return grid.cells.map((row) => row.join(''));
}
