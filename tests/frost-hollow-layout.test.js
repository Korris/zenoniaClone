// Layout guarantees for the Frost Hollow battle map beyond the generic map invariants: the grid
// matches the painted backdrop's size, the HUD rows are blocked, every spawn zone has open ground,
// the stream is only crossable on the bridge, and the village <-> Frost Hollow <-> meadow chain holds.
// (Portal arrival tiles are checked for every map in map-invariants.test.js.)
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { FROST_HOLLOW_MAP as MAP } from '../src/world/maps/frost-hollow-map.js';
import { MAP_REGISTRY } from '../src/world/map-registry.js';
import { MONSTER_DEFINITIONS } from '../src/data/monster-definitions.js';
import { BACKDROP_FILES } from '../src/graphics/asset-atlas.js';
import { reachableFrom, tilemapWithBlockers } from './helpers/map-reachability.js';

const seen = reachableFrom(MAP);
const reachable = (tx, ty) => seen.has(`${tx},${ty}`);

test('Frost Hollow is 36x24 and its backdrop covers it at 4 px per world unit', () => {
  assert.equal(MAP.rows.length, 24);
  assert.ok(MAP.rows.every((r) => r.length === 36));
  // Lossy WebP ("VP8 " chunk): 14-bit width/height at bytes 26/28 of the RIFF file.
  const webp = readFileSync('assets/' + BACKDROP_FILES[MAP.backdrop]);
  assert.equal(webp.toString('ascii', 12, 16), 'VP8 ');
  assert.equal(webp.readUInt16LE(26) & 0x3fff, 36 * 16 * 4);
  assert.equal(webp.readUInt16LE(28) & 0x3fff, 24 * 16 * 4);
});

test('the two HUD rows are fully blocked and nothing is placed on them', () => {
  for (const ty of [22, 23]) assert.ok(!MAP.rows[ty].includes('s') && !MAP.rows[ty].includes('P'), `row ${ty} open`);
  for (const p of MAP.portals) assert.ok(p.ty < 22, `portal ${p.tx},${p.ty} under the HUD`);
  assert.ok(MAP.playerStart.ty < 22);
});

test('every spawn zone names a defined monster and has reachable open ground', () => {
  for (const zone of MAP.spawnZones) {
    assert.ok(MONSTER_DEFINITIONS[zone.type], `unknown monster ${zone.type}`);
    const { tx, ty, w, h } = zone.area;
    let open = 0;
    for (let y = ty; y < ty + h; y++) for (let x = tx; x < tx + w; x++) if (reachable(x, y)) open++;
    assert.ok(open >= zone.count, `zone ${zone.type}@${tx},${ty}: ${open} open tiles for ${zone.count} monsters`);
  }
});

test('the start stairs are monster-free and the stream is only crossable on the bridge', () => {
  for (const zone of MAP.spawnZones) {
    const { tx, ty, w, h } = zone.area;
    assert.ok(tx + w <= 2 || tx > 6 || ty + h <= 18, `zone ${zone.type}@${tx},${ty} overlaps the entrance circle/stairs`);
  }
  const tm = tilemapWithBlockers(MAP);
  for (let ty = 0; ty < 24; ty++) for (let tx = 0; tx < 36; tx++) if (MAP.rows[ty][tx] === 'w') assert.ok(tm.isSolidTile(tx, ty));
  for (let tx = 17; tx <= 21; tx++) assert.ok(reachable(tx, 12), `bridge ${tx},12`);
  // all three painted clearings are on foot from the entrance
  for (const [tx, ty] of [[9, 6], [10, 14], [28, 7], [27, 16]]) assert.ok(reachable(tx, ty), `clearing ${tx},${ty}`);
});

test('village -> Frost Hollow -> meadow chain, and both ways back', () => {
  const village = MAP_REGISTRY.oakhavenVillage;
  const meadow = MAP_REGISTRY.verdantMeadow;
  assert.equal(village.portals[0].targetMap, 'frostHollow');
  assert.equal(MAP.portals.find((p) => p.targetMap === 'oakhavenVillage').minLevel, 1);
  assert.ok(MAP.portals.some((p) => p.targetMap === 'verdantMeadow'));
  assert.ok(meadow.portals.some((p) => p.targetMap === 'frostHollow'));
});
