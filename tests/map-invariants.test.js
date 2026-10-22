import { test } from 'node:test';
import assert from 'node:assert/strict';
import { MAP_REGISTRY } from '../src/world/map-registry.js';
import { getPropDefinition } from '../src/world/prop-definitions.js';
import { NPC_DEFINITIONS } from '../src/data/npc-definitions.js';
import { QUEST_DEFINITIONS } from '../src/data/quest-definitions.js';
import { reachableFrom } from './helpers/map-reachability.js';

/** Tiles a prop's sprite covers: footprint columns, from its base row up to its visual top. */
function coveredTiles(prop) {
  const def = getPropDefinition(prop.type);
  const rows = Math.ceil(def.height / 16);
  const tiles = [];
  for (let y = prop.ty - rows + 1; y <= prop.ty; y++) {
    for (let x = prop.tx; x < prop.tx + def.footprint.w; x++) tiles.push(`${x},${y}`);
  }
  return tiles;
}

for (const map of Object.values(MAP_REGISTRY)) {
  test(`${map.id}: no prop sprite covers a portal, NPC or the start tile`, () => {
    const covered = new Set(map.props.flatMap(coveredTiles));
    for (const p of map.portals) assert.equal(covered.has(`${p.tx},${p.ty}`), false, `portal ${p.tx},${p.ty} covered`);
    for (const n of map.npcs) assert.equal(covered.has(`${n.tx},${n.ty}`), false, `npc ${n.id} covered`);
    assert.equal(covered.has(`${map.playerStart.tx},${map.playerStart.ty}`), false, 'start covered');
  });

  test(`${map.id}: portals, NPCs and boss are reachable from the start`, () => {
    const seen = reachableFrom(map);
    for (const p of map.portals) assert.ok(seen.has(`${p.tx},${p.ty}`), `portal ${p.tx},${p.ty} unreachable`);
    for (const n of map.npcs) {
      assert.ok(NPC_DEFINITIONS[n.id], `unknown npc ${n.id}`);
      const adjacent = [[1, 0], [-1, 0], [0, 1], [0, -1]].some(([dx, dy]) => seen.has(`${n.tx + dx},${n.ty + dy}`));
      assert.ok(adjacent, `npc ${n.id} not reachable`);
    }
    if (map.boss) assert.ok(seen.has(`${map.boss.tx},${map.boss.ty}`), 'boss unreachable');
  });
}

test('every portal lands on a walkable, non-portal tile reachable from the target map start', () => {
  const reachableByMap = Object.fromEntries(Object.values(MAP_REGISTRY).map((m) => [m.id, reachableFrom(m)]));
  for (const map of Object.values(MAP_REGISTRY)) {
    for (const p of map.portals) {
      const target = MAP_REGISTRY[p.targetMap];
      assert.ok(target, `${map.id}: portal to unknown map ${p.targetMap}`);
      assert.notEqual(target.rows[p.targetTy][p.targetTx], 'P', `${map.id} -> ${p.targetMap}: arrival tile is a portal (would bounce)`);
      assert.ok(reachableByMap[target.id].has(`${p.targetTx},${p.targetTy}`), `${map.id} -> ${p.targetMap}: arrival ${p.targetTx},${p.targetTy} blocked`);
    }
  }
});

test('every quest giver exists and offers that quest; prerequisites exist', () => {
  for (const q of Object.values(QUEST_DEFINITIONS)) {
    assert.ok(NPC_DEFINITIONS[q.giver]?.quests.includes(q.id), `${q.id} giver mismatch`);
    if (q.prerequisite) assert.ok(QUEST_DEFINITIONS[q.prerequisite], `${q.id} prerequisite missing`);
  }
});
