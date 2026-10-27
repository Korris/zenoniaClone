// Fills spawn zones with monsters on map load and respawns dead ones after a delay.
// The boss spawns once per map load and never respawns once defeated (tracked by caller).
import { TILE_SIZE } from '../core/constants.js';
import { Monster } from '../entities/monster.js';

const MIN_SPAWN_DISTANCE_FROM_PLAYER = 60;

export class MonsterSpawner {
  /**
   * @param {object} mapDef map definition (spawnZones, boss)
   * @param {import('./tilemap.js').Tilemap} tilemap
   */
  constructor(mapDef, tilemap, rand = Math.random) {
    this.mapDef = mapDef;
    this.tilemap = tilemap;
    this.rand = rand;
    // one respawn timer per zone; counts how many monsters are pending
    this.pendingRespawns = mapDef.spawnZones.map(() => []);
  }

  /** Spawn every zone's full count. */
  spawnInitial(bossDefeated) {
    const monsters = [];
    this.mapDef.spawnZones.forEach((zone, zoneIndex) => {
      for (let i = 0; i < zone.count; i++) {
        const m = this.spawnInZone(zone, zoneIndex, null);
        if (m) monsters.push(m);
      }
    });
    if (this.mapDef.boss && (this.mapDef.boss.localBoss || !bossDefeated)) {
      const { type, tx, ty } = this.mapDef.boss;
      monsters.push(new Monster(type, (tx + 0.5) * TILE_SIZE, (ty + 1) * TILE_SIZE, -1));
    }
    return monsters;
  }

  /** Pick a random walkable tile inside the zone, away from the player. */
  spawnInZone(zone, zoneIndex, player) {
    const { tx, ty, w, h } = zone.area;
    for (let attempt = 0; attempt < 30; attempt++) {
      const cx = tx + Math.floor(this.rand() * w);
      const cy = ty + Math.floor(this.rand() * h);
      if (this.tilemap.isSolidTile(cx, cy)) continue;
      const x = (cx + 0.5) * TILE_SIZE;
      const y = (cy + 1) * TILE_SIZE - 2;
      if (player && Math.hypot(player.x - x, player.y - y) < MIN_SPAWN_DISTANCE_FROM_PLAYER) continue;
      return new Monster(zone.type, x, y, zoneIndex);
    }
    return null;
  }

  /** Call when a zone monster dies to schedule its replacement. */
  scheduleRespawn(monster) {
    if (monster.zoneIndex < 0) return;
    const zone = this.mapDef.spawnZones[monster.zoneIndex];
    this.pendingRespawns[monster.zoneIndex].push(zone.respawnMs);
  }

  /** Tick respawn timers; returns newly spawned monsters. */
  update(dt, player) {
    const spawned = [];
    this.pendingRespawns.forEach((timers, zoneIndex) => {
      for (let i = timers.length - 1; i >= 0; i--) {
        timers[i] -= dt;
        if (timers[i] > 0) continue;
        const m = this.spawnInZone(this.mapDef.spawnZones[zoneIndex], zoneIndex, player);
        if (m) {
          spawned.push(m);
          timers.splice(i, 1);
        } else {
          timers[i] = 500; // retry shortly if no free spot
        }
      }
    });
    return spawned;
  }
}
