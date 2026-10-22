// Pure config checks for the HD ground-tile pipeline (no DOM): every HD_TILES cell must fall
// inside its normalized sheet (read via the PNG IHDR chunk, no image-decoding dependency needed),
// and every tile sprite name tile-char-info.js can reach must have an HD equivalent so no tile
// silently stays pixel-art forever once the HD set is wired in.
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync, existsSync } from 'node:fs';
import { HD_TILES, HD_TILE_CELL, ASSET_FILES } from '../src/graphics/asset-atlas.js';
import { TILE_INFO } from '../src/world/tile-char-info.js';
import { pickVariant } from '../src/graphics/tile-variant-picker.js';
import { blockCell, blockPieceKey } from '../src/graphics/tile-block-renderer.js';

const ASSET_ROOT = 'assets/';

/** Width/height from a PNG's IHDR chunk (bytes 16-23) -- avoids a decode dependency in tests. */
function pngSize(path) {
  const buf = readFileSync(path);
  return { width: buf.readUInt32BE(16), height: buf.readUInt32BE(20) };
}

/** Every [cx, cy] cell a def reads from ITS OWN image (block + seamless extras; `fill` has its own image). */
function cellsOf(def) {
  if (!def.block) return def.variants;
  const [bx, by] = def.block;
  return [
    ...[0, 1, 2].flatMap((r) => [0, 1, 2].map((c) => [bx + c, by + r])),
    ...(def.center ? def.center.variants : []),
    ...(def.inner || []),
    ...Object.values(def.shapes || {}),
  ];
}

/** Defs to bounds-check: each HD_TILES entry plus any nested `fill` material (separate image). */
function allDefs() {
  return Object.entries(HD_TILES).flatMap(([name, def]) => (def.fill ? [[name, def], [name + '.fill', def.fill]] : [[name, def]]));
}

test('every HD_TILES image key resolves to a file in ASSET_FILES', () => {
  for (const [name, def] of allDefs()) {
    assert.ok(ASSET_FILES[def.image], `${name}: HD_TILES.image "${def.image}" is not a key in ASSET_FILES`);
  }
});

test('every HD_TILES cell is within its normalized sheet bounds', () => {
  const sizeCache = {};
  for (const [name, def] of allDefs()) {
    const path = ASSET_ROOT + ASSET_FILES[def.image];
    assert.ok(existsSync(path), `${def.image} (${path}) missing -- run npm run hd:tiles`);
    const size = sizeCache[def.image] || (sizeCache[def.image] = pngSize(path));
    for (const [cx, cy] of cellsOf(def)) {
      assert.ok((cx + 1) * HD_TILE_CELL <= size.width, `${name}: cell col ${cx} exceeds ${def.image} width ${size.width}`);
      assert.ok((cy + 1) * HD_TILE_CELL <= size.height, `${name}: cell row ${cy} exceeds ${def.image} height ${size.height}`);
    }
  }
});

test('every ground tile sprite name used by tile-char-info has an HD_TILES entry', () => {
  const spriteNames = new Set();
  for (const info of Object.values(TILE_INFO)) {
    if (info.sprite) spriteNames.add(info.sprite);
    if (info.base) spriteNames.add(info.base);
    if (info.top) spriteNames.add(info.top);
  }
  for (const name of spriteNames) {
    assert.ok(HD_TILES[name], `HD_TILES is missing an entry for "${name}" (referenced by tile-char-info.js)`);
  }
});

test('waterAnim and flowers (HD-only, no pixel-art equivalent) are present with the expected frame counts', () => {
  assert.equal(HD_TILES.waterAnim.variants.length, 3);
  assert.equal(HD_TILES.flowers.variants.length, 4);
  assert.equal(HD_TILES.grass.variants.length, 4);
});

test('seamless macro sets have cols * rows variants, and flowers share the grass macro layout', () => {
  for (const [name, def] of allDefs()) {
    if (!def.macro) continue;
    assert.equal(def.variants.length, def.macro[0] * def.macro[1], `${name}: macro ${def.macro} needs cols*rows variants`);
  }
  assert.deepEqual(HD_TILES.flowers.macro, HD_TILES.grass.macro);
});

test('block tiles with inner corners and extra shapes carry the full piece set', () => {
  for (const name of ['dirtBlock', 'plazaStone']) {
    assert.equal(HD_TILES[name].inner.length, 4, `${name}: 4 inner-corner wedges (TL, TR, BL, BR)`);
    assert.deepEqual(Object.keys(HD_TILES[name].shapes).sort(), ['', 'B', 'L', 'LR', 'R', 'T', 'TB'], `${name}: 7 extra shapes`);
  }
});

test('pickVariant maps macro sets by tile position and keeps the scatter for plain sets', () => {
  const macro = ['a', 'b', 'c', 'd'];
  macro.macro = [2, 2];
  assert.equal(pickVariant(macro, 0, 0), 'a');
  assert.equal(pickVariant(macro, 1, 0), 'b');
  assert.equal(pickVariant(macro, 0, 1), 'c');
  assert.equal(pickVariant(macro, 3, 5), 'd');
  assert.equal(pickVariant(macro, 2, 2), 'a'); // repeats every 2x2 tiles
  const plain = ['a', 'b', 'c', 'd'];
  assert.equal(pickVariant(plain, 0, 0), 'a');
  assert.equal(pickVariant(plain, 1, 0), plain[7 % 4]);
});

test('every cell a block def reads is distinct (no extra piece silently aliases another)', () => {
  for (const [name, def] of allDefs()) {
    const keys = cellsOf(def).map(([cx, cy]) => `${cx},${cy}`);
    assert.equal(new Set(keys).size, keys.length, `${name}: duplicate cell coordinates ${keys}`);
  }
});

test('blockPieceKey covers exactly the 7 shapes the 3x3 block lacks', () => {
  const needsShape = [];
  for (const t of [false, true]) for (const b of [false, true]) for (const l of [false, true]) for (const r of [false, true]) {
    if ((!t && !b) || (!l && !r)) needsShape.push(blockPieceKey(t, b, l, r)); // an opposite pair both closed
  }
  assert.deepEqual([...new Set(needsShape)].sort(), Object.keys(HD_TILES.dirtBlock.shapes).sort());
  assert.equal(blockPieceKey(true, true, true, true), 'TBLR'); // open all round -> no extra piece
  assert.equal(blockPieceKey(true, true, false, false), 'TB'); // vertical 1-wide pipe
});

test('blockCell picks the 3x3 piece from the open sides', () => {
  assert.deepEqual(blockCell(true, true, true, true), [1, 1]); // open all round -> center
  assert.deepEqual(blockCell(false, true, true, true), [0, 1]); // closed top -> top edge
  assert.deepEqual(blockCell(true, true, false, true), [1, 0]); // closed left -> left edge
  assert.deepEqual(blockCell(true, false, true, false), [2, 2]); // closed bottom+right -> BR corner
});
