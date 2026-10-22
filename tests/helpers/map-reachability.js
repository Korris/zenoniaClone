// Test helper: flood-fill the walkable tiles of a map definition from its player start, using the
// same solidity rules the game applies at load (tile solidity + solid prop footprints + NPC tiles).
import { Tilemap } from '../../src/world/tilemap.js';
import { getPropDefinition } from '../../src/world/prop-definitions.js';

/** Tilemap with every solid prop footprint and NPC tile blocked, as map-manager.js builds it. */
export function tilemapWithBlockers(map) {
  const tm = new Tilemap(map.rows);
  for (const p of map.props) {
    const d = getPropDefinition(p.type);
    if (d.solid) tm.addSolidFootprint(p.tx, p.ty - d.footprint.h + 1, d.footprint.w, d.footprint.h);
  }
  for (const n of map.npcs) tm.addSolidFootprint(n.tx, n.ty, 1, 1);
  return tm;
}

/** Set of "tx,ty" keys reachable on foot from the map's player start. */
export function reachableFrom(map) {
  const tm = tilemapWithBlockers(map);
  const seen = new Set([`${map.playerStart.tx},${map.playerStart.ty}`]);
  const queue = [[map.playerStart.tx, map.playerStart.ty]];
  while (queue.length) {
    const [x, y] = queue.shift();
    for (const [dx, dy] of [[1, 0], [-1, 0], [0, 1], [0, -1]]) {
      const key = `${x + dx},${y + dy}`;
      if (seen.has(key) || tm.isSolidTile(x + dx, y + dy)) continue;
      seen.add(key);
      queue.push([x + dx, y + dy]);
    }
  }
  return seen;
}
