// Layout guarantees for the Oakhaven Village hub beyond the generic map invariants: the plaza
// (well ring) can be walked around, the farm gate is open, the bridge reaches the east bank, the
// dock is walkable. (Portal arrival tiles are checked for every map in map-invariants.test.js.)
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { OAKHAVEN_VILLAGE_MAP as MAP } from '../src/world/maps/oakhaven-village-map.js';
import { reachableFrom, tilemapWithBlockers } from './helpers/map-reachability.js';

const seen = reachableFrom(MAP);
const reachable = (tx, ty) => seen.has(`${tx},${ty}`);

test('village is 36x26 with the cliff border intact', () => {
  assert.equal(MAP.rows.length, 26);
  assert.ok(MAP.rows.every((r) => r.length === 36));
  assert.ok(MAP.rows[0].split('').every((c) => c === 'C') && MAP.rows[25].split('').every((c) => c === 'C'));
});

test('plaza: every tile of the square around the well spot is walkable (except NPC / well spot)', () => {
  for (let ty = 13; ty <= 17; ty++) {
    for (let tx = 14; tx <= 18; tx++) {
      const isWellSpot = tx >= 15 && tx <= 16 && ty >= 14 && ty <= 15;
      const isNpc = MAP.npcs.some((n) => n.tx === tx && n.ty === ty);
      if (isWellSpot || isNpc) continue;
      assert.ok(reachable(tx, ty), `plaza tile ${tx},${ty} not reachable`);
    }
  }
  // The future 2x2 well footprint is ringed by walkable tiles on all four sides.
  for (const [tx, ty] of [[14, 14], [17, 15], [15, 13], [16, 16]]) assert.ok(reachable(tx, ty), `well ring ${tx},${ty}`);
});

test('farm: interior reachable through the east gate, fence otherwise closed', () => {
  assert.ok(reachable(10, 20) && reachable(10, 21), 'gate tiles blocked');
  assert.ok(reachable(5, 20), 'farm interior unreachable');
  const tm = tilemapWithBlockers(MAP);
  for (let tx = 3; tx <= 10; tx++) {
    assert.ok(tm.isSolidTile(tx, 18) && tm.isSolidTile(tx, 23), `fence row open at col ${tx}`);
  }
  for (let ty = 19; ty <= 22; ty++) {
    assert.ok(tm.isSolidTile(3, ty), `west fence open at row ${ty}`);
    if (ty !== 20 && ty !== 21) assert.ok(tm.isSolidTile(10, ty), `east fence open at row ${ty}`);
  }
});

test('river: bridge reaches the east bank and the dock planks are walkable, open water is not', () => {
  for (let tx = 31; tx <= 34; tx++) assert.ok(reachable(tx, 15), `bridge/bank ${tx},15 blocked`);
  for (const [tx, ty] of [[31, 20], [32, 20], [31, 21], [32, 21]]) assert.ok(reachable(tx, ty), `dock ${tx},${ty} blocked`);
  assert.ok(!reachable(33, 20) && !reachable(32, 19), 'walked onto open water');
});
