// Layout guarantees for the Oakhaven Village hub beyond the generic map invariants: the grid matches
// the painted backdrop's size, the forest frame is closed, the plaza ring around the well, the
// market, farm, bridge and dock are all reachable on foot, and open water is not.
// (Portal arrival tiles are checked for every map in map-invariants.test.js.)
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { OAKHAVEN_VILLAGE_MAP as MAP } from '../src/world/maps/oakhaven-village-map.js';
import { BACKDROP_FILES } from '../src/graphics/asset-atlas.js';
import { reachableFrom } from './helpers/map-reachability.js';

const seen = reachableFrom(MAP);
const reachable = (tx, ty) => seen.has(`${tx},${ty}`);

test('village is 36x24 and its backdrop covers it at 4 px per world unit', () => {
  assert.equal(MAP.rows.length, 24);
  assert.ok(MAP.rows.every((r) => r.length === 36));
  // Lossy WebP ("VP8 " chunk): 14-bit width/height at bytes 26/28 of the RIFF file.
  const webp = readFileSync('assets/' + BACKDROP_FILES[MAP.backdrop]);
  assert.equal(webp.toString('ascii', 12, 16), 'VP8 ');
  assert.equal(webp.readUInt16LE(26) & 0x3fff, 36 * 16 * 4);
  assert.equal(webp.readUInt16LE(28) & 0x3fff, 24 * 16 * 4);
});

test('the walkable area never touches the map edge except the east bridge exit', () => {
  for (const key of seen) {
    const [tx, ty] = key.split(',').map(Number);
    const onEdge = tx === 0 || ty === 0 || tx === 35 || ty === 23;
    const allowed = tx === 35 && (ty === 10 || ty === 11);
    if (onEdge) assert.ok(allowed, `walkable edge tile ${key}`);
  }
});

test('plaza: the well is ringed by walkable tiles', () => {
  for (const [tx, ty] of [[15, 11], [18, 11], [16, 10], [17, 10], [16, 12], [17, 12]]) {
    assert.ok(reachable(tx, ty), `well ring ${tx},${ty}`);
  }
});

test('districts: market, farm field, workshop yard and both river banks are reachable', () => {
  for (const [tx, ty, what] of [[5, 10, 'market'], [5, 18, 'farm'], [26, 21, 'workshop yard'],
    [33, 10, 'bridge'], [34, 8, 'east bank'], [31, 16, 'dock']]) {
    assert.ok(reachable(tx, ty), `${what} ${tx},${ty} unreachable`);
  }
  assert.ok(!reachable(32, 6) && !reachable(32, 20), 'walked onto open water');
});
