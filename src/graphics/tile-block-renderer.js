// Draws one auto-tile 'block' ground tile (dirt path, water, plaza stone): seamless base grass,
// then the 3x3 block cell picked from the four edge neighbours, then (HD only) grass wedges on
// concave corners picked from the diagonal neighbours. HD block cells are RGBA transition masks
// (grass transparent) -- see tools/build-hd-tiles.py -- so the base grass shows through them and
// never mismatches the grass around the road.
import { TILE_SIZE } from '../core/constants.js';
import { pickVariant } from './tile-variant-picker.js';

const WATER_ANIM_FRAME_MS = 450; // HD open-water center cell cycles through tiles.waterAnim
// Concave corner probes: [dx, dy, index into the `<sprite>Inner` wedge list (TL, TR, BL, BR)].
const INNER_CORNERS = [[-1, -1, 0], [1, -1, 1], [-1, 1, 2], [1, 1, 3]];

let scratch = null;

/** `material` canvas masked by `mask`'s alpha (destination-in) into a reused scratch canvas. */
function masked(material, mask) {
  if (!scratch) scratch = document.createElement('canvas');
  if (scratch.width !== material.width || scratch.height !== material.height) {
    scratch.width = material.width;
    scratch.height = material.height;
  }
  const c = scratch.getContext('2d');
  c.globalCompositeOperation = 'source-over';
  c.clearRect(0, 0, scratch.width, scratch.height);
  c.drawImage(material, 0, 0, scratch.width, scratch.height);
  c.globalCompositeOperation = 'destination-in';
  c.drawImage(mask, 0, 0, scratch.width, scratch.height);
  c.globalCompositeOperation = 'source-over';
  return scratch;
}

/** Column/row inside a 3x3 auto-tile block from which sides touch a different family. */
export function blockCell(top, bottom, left, right) {
  const col = left && right ? 1 : left ? 2 : right ? 0 : 1; // open on left -> use right-edge piece
  const row = top && bottom ? 1 : top ? 2 : bottom ? 0 : 1;
  return [row, col];
}

/** Open-sides key ('T'/'B'/'L'/'R' concatenated) that indexes an HD `<sprite>Shapes` extra piece. */
export function blockPieceKey(top, bottom, left, right) {
  return (top ? 'T' : '') + (bottom ? 'B' : '') + (left ? 'L' : '') + (right ? 'R' : '');
}

/**
 * @param {CanvasRenderingContext2D} ctx world-space context
 * @param {object} tiles sprite registry tiles
 * @param {boolean} tilesHd whether the HD tile set is active
 * @param {{sprite:string, base:string}} info tile info (kind 'block')
 * @param {(dx:number, dy:number) => boolean} same whether the neighbour at (dx, dy) is same-family
 */
export function drawBlockTile(ctx, tiles, tilesHd, info, tx, ty, same, elapsedMs) {
  const px = tx * TILE_SIZE;
  const py = ty * TILE_SIZE;
  const base = tiles[info.base];
  ctx.drawImage(pickVariant(base, tx, ty), px, py, TILE_SIZE, TILE_SIZE);

  const top = same(0, -1);
  const bottom = same(0, 1);
  const left = same(-1, 0);
  const right = same(1, 0);
  const [row, col] = blockCell(top, bottom, left, right);
  // 1-wide pipes, dead-end caps and islands have no 3x3 piece: the HD set carries them as extra
  // shapes keyed by the open sides (pixel-art pack: fall through to the plain center as before).
  const shapes = tiles[info.sprite + 'Shapes'];
  const shape = shapes ? shapes[blockPieceKey(top, bottom, left, right)] : undefined;
  const piece = shape || tiles[info.sprite][row][col];
  const fill = tiles[info.sprite + 'Fill'];
  const center = tiles[info.sprite + 'Center'];
  const isCenter = row === 1 && col === 1 && !shape;
  if (isCenter && tilesHd && info.sprite === 'waterBlock' && tiles.waterAnim) {
    const frames = tiles.waterAnim;
    ctx.drawImage(frames[Math.floor(elapsedMs / WATER_ANIM_FRAME_MS) % frames.length], px, py, TILE_SIZE, TILE_SIZE);
  } else if (isCenter && fill) {
    ctx.drawImage(pickVariant(fill, tx, ty), px, py, TILE_SIZE, TILE_SIZE);
  } else if (isCenter && center) {
    ctx.drawImage(pickVariant(center, tx, ty), px, py, TILE_SIZE, TILE_SIZE);
  } else if (fill) {
    ctx.drawImage(masked(pickVariant(fill, tx, ty), piece), px, py, TILE_SIZE, TILE_SIZE);
  } else {
    ctx.drawImage(piece, px, py, TILE_SIZE, TILE_SIZE);
  }

  // Concave corners: both orthogonal neighbours are same-family but the diagonal is not, so the
  // piece above has a hard square corner there -- round it off with a grass wedge.
  const inner = tiles[info.sprite + 'Inner'];
  if (!inner) return;
  for (const [dx, dy, i] of INNER_CORNERS) {
    if (same(dx, 0) && same(0, dy) && !same(dx, dy)) {
      ctx.drawImage(masked(pickVariant(base, tx, ty), inner[i]), px, py, TILE_SIZE, TILE_SIZE);
    }
  }
}
