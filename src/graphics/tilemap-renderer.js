// Draws visible tiles. 'block' tiles (path, water, plaza stone) auto-tile via tile-block-renderer.js;
// 'cliff' tiles pick top/wall/bottom rows; 'variants' tiles pick a frame by position
// (tile-variant-picker.js -- seamless macro textures in HD, pseudo-random scatter in pixel art).
// HD tiles (registry.tilesHd) draw 64px source cells at the same TILE_SIZE dest size with
// smoothing enabled -- see docs/system-architecture.md "HD Assets".
import { TILE_SIZE, VIEW_WIDTH, VIEW_HEIGHT } from '../core/constants.js';
import { getTileInfo, sameEdgeFamily } from '../world/tile-char-info.js';
import { pickVariant } from './tile-variant-picker.js';
import { drawBlockTile } from './tile-block-renderer.js';
import { drawSmoothImageRegion } from './smooth-sprite-draw.js';

const PORTAL_FRAME_MS = 140;
// Matches the old procedural portal's opaque base fill (tile-art-generator.js PORTAL_COLORS.base).
// The HD portal frame has real (soft) alpha outside the magic circle -- game-controller.js clears
// the canvas to black every frame, so without this backdrop the circle would float on pure black
// instead of the intended dark-void vignette.
const PORTAL_BACKDROP = '#2b2b38';
const NEIGHBOURS = [[0, 1], [0, -1], [1, 0], [-1, 0]];

function drawVariants(ctx, frames, tx, ty) {
  ctx.drawImage(pickVariant(frames, tx, ty), tx * TILE_SIZE, ty * TILE_SIZE, TILE_SIZE, TILE_SIZE);
}

/**
 * The HD portal has real alpha: lay the surrounding ground under it so it sits on the path/floor
 * instead of a flat square. A neighbouring block tile (dirt road) contributes its open-center
 * look (`same` always true); falls back to the dark backdrop at map edges.
 */
function drawPortalGround(ctx, tilemap, tiles, tilesHd, tx, ty, elapsedMs) {
  const neighbour = NEIGHBOURS.map(([dx, dy]) => tilemap.charAt(tx + dx, ty + dy))
    .find((n) => n !== null && n !== 'P' && ['variants', 'block'].includes(getTileInfo(n).kind));
  if (!neighbour) {
    ctx.fillStyle = PORTAL_BACKDROP;
    ctx.fillRect(tx * TILE_SIZE, ty * TILE_SIZE, TILE_SIZE, TILE_SIZE);
    return;
  }
  const info = getTileInfo(neighbour);
  if (info.kind === 'block') drawBlockTile(ctx, tiles, tilesHd, info, tx, ty, () => true, elapsedMs);
  else drawVariants(ctx, tiles[info.sprite], tx, ty);
}

/** Blit only the on-screen slice of the map-sized backdrop (its scale = source px per world unit). */
function drawBackdrop(ctx, tilemap, camera, backdrop) {
  const scale = backdrop.width / tilemap.pixelWidth;
  const x = Math.max(0, Math.floor(camera.renderX));
  const y = Math.max(0, Math.floor(camera.renderY));
  const w = Math.min(VIEW_WIDTH + 1, tilemap.pixelWidth - x);
  const h = Math.min(VIEW_HEIGHT + 1, tilemap.pixelHeight - y);
  if (w > 0 && h > 0) drawSmoothImageRegion(ctx, backdrop, x * scale, y * scale, w * scale, h * scale, x, y, w, h);
}

/**
 * @param {CanvasRenderingContext2D} ctx context already transformed to world space
 * @param {import('../world/tilemap.js').Tilemap} tilemap
 * @param {import('../core/camera.js').Camera} camera
 * @param {object} registry sprite registry (tiles, tilesHd, flowers)
 * @param {number} elapsedMs
 * @param {HTMLImageElement|null} backdrop painted whole-map ground; when set, only animated tiles
 *   (portals) are drawn on top of it -- the tile rows then only drive collision and the minimap
 */
export function drawTilemap(ctx, tilemap, camera, registry, elapsedMs, backdrop = null) {
  const { tiles, flowers, tilesHd } = registry;
  const startX = Math.max(0, Math.floor(camera.renderX / TILE_SIZE));
  const startY = Math.max(0, Math.floor(camera.renderY / TILE_SIZE));
  const endX = Math.min(tilemap.width - 1, Math.ceil((camera.renderX + VIEW_WIDTH) / TILE_SIZE));
  const endY = Math.min(tilemap.height - 1, Math.ceil((camera.renderY + VIEW_HEIGHT) / TILE_SIZE));

  // One toggle for the whole tilemap (not per-tile): HD cells are 64px source drawn down to
  // TILE_SIZE, so they need smoothing; the rest of the world (entities) manages its own toggle.
  ctx.imageSmoothingEnabled = tilesHd || !!backdrop;
  if (backdrop) drawBackdrop(ctx, tilemap, camera, backdrop);

  for (let ty = startY; ty <= endY; ty++) {
    for (let tx = startX; tx <= endX; tx++) {
      const ch = tilemap.charAt(tx, ty);
      if (ch === null) continue;
      const info = getTileInfo(ch);
      if (backdrop && info.kind !== 'animated') continue;
      const px = tx * TILE_SIZE;
      const py = ty * TILE_SIZE;
      const same = (dx, dy) => sameEdgeFamily(ch, tilemap.charAt(tx + dx, ty + dy));

      if (info.kind === 'variants') {
        drawVariants(ctx, tiles[info.sprite], tx, ty);
      } else if (info.kind === 'flowers') {
        if (tilesHd && tiles.flowers) {
          drawVariants(ctx, tiles.flowers, tx, ty); // same grass quadrant + painted flowers, no seam
        } else {
          drawVariants(ctx, tiles[info.sprite], tx, ty);
          ctx.drawImage(flowers[(tx * 3 + ty * 5) % flowers.length], px, py, TILE_SIZE, TILE_SIZE);
        }
      } else if (info.kind === 'block') {
        drawBlockTile(ctx, tiles, tilesHd, info, tx, ty, same, elapsedMs);
      } else if (info.kind === 'cliff') {
        const above = same(0, -1);
        const below = same(0, 1);
        const frames = !above && info.top ? tiles[info.top] : !above ? tiles.cliffTop : !below && !info.top ? tiles.cliffBottom : tiles[info.sprite];
        ctx.drawImage(frames[0], px, py, TILE_SIZE, TILE_SIZE);
      } else if (info.kind === 'animated') {
        const frames = tiles[info.sprite];
        if (tilesHd && !backdrop) drawPortalGround(ctx, tilemap, tiles, tilesHd, tx, ty, elapsedMs);
        ctx.drawImage(frames[Math.floor(elapsedMs / PORTAL_FRAME_MS) % frames.length], px, py, TILE_SIZE, TILE_SIZE);
      }
    }
  }

  ctx.imageSmoothingEnabled = false; // restore: the rest of the world renders pixel-art crisp
}
