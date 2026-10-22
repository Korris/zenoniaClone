// Loads maps into the world state (tiles, props, NPCs, monsters) and detects portal use.
import { TILE_SIZE } from '../core/constants.js';
import { Tilemap } from './tilemap.js';
import { MonsterSpawner } from './monster-spawner.js';
import { getMapDefinition } from './map-registry.js';
import { getPropDefinition } from './prop-definitions.js';
import { Npc } from '../entities/npc.js';

// Camera looks slightly above the feet; must match the follow() call in world-update.js
export const CAMERA_TARGET_Y_OFFSET = 8;
// Portal / walkability checks sample this far above the feet (inside the sprite's base)
const FEET_SAMPLE_OFFSET = 4;

/** Convert map prop entries to world-space props and block their footprints. */
function buildProps(mapDef, tilemap) {
  return (mapDef.props || []).map(({ type, tx, ty }) => {
    const def = getPropDefinition(type);
    if (def.solid) tilemap.addSolidFootprint(tx, ty - def.footprint.h + 1, def.footprint.w, def.footprint.h);
    return {
      type,
      x: (tx + def.footprint.w / 2) * TILE_SIZE, // horizontal center of the footprint
      y: (ty + 1) * TILE_SIZE, // bottom edge of the footprint = sort key
    };
  });
}

/**
 * Build tilemap + props + NPCs + monsters for `mapId` and place the hero at a tile.
 * @param {import('../game/world-state.js').WorldState} world
 */
export function loadMap(world, mapId, spawnTx = null, spawnTy = null) {
  const mapDef = getMapDefinition(mapId);
  const tilemap = new Tilemap(mapDef.rows);
  world.mapId = mapDef.id;
  world.mapDef = mapDef;
  world.tilemap = tilemap;
  world.clearMapEntities();
  world.props = buildProps(mapDef, tilemap);
  world.npcs = (mapDef.npcs || []).map((n) => new Npc(n.id, (n.tx + 0.5) * TILE_SIZE, (n.ty + 1) * TILE_SIZE - 2));
  for (const npc of world.npcs) tilemap.addSolidFootprint(Math.floor(npc.x / TILE_SIZE), Math.floor((npc.y - 2) / TILE_SIZE), 1, 1);
  world.spawner = new MonsterSpawner(mapDef, tilemap);
  world.monsters = world.spawner.spawnInitial(world.bossDefeated);

  const tx = spawnTx ?? mapDef.playerStart.tx;
  const ty = spawnTy ?? mapDef.playerStart.ty;
  const safe = tilemap.findWalkableNear(tx, ty) || { tx, ty };
  placePlayerAtTile(world, safe.tx, safe.ty);
  world.camera.setMapSize(tilemap.width, tilemap.height);
  world.camera.snapTo(world.player.x, world.player.y - CAMERA_TARGET_Y_OFFSET);
  return mapDef;
}

export function placePlayerAtTile(world, tx, ty) {
  world.player.x = (tx + 0.5) * TILE_SIZE;
  world.player.y = (ty + 1) * TILE_SIZE - 2;
  world.player.knockback.timer = 0;
  world.player.dash = null;
}

/** Place the hero at exact world coordinates if they are walkable, else at map start. */
export function placePlayerAtWorld(world, x, y) {
  const tilemap = world.tilemap;
  const tx = Math.floor(x / TILE_SIZE);
  const ty = Math.floor((y - FEET_SAMPLE_OFFSET) / TILE_SIZE);
  if (tilemap.charAt(tx, ty) !== null && !tilemap.isSolidTile(tx, ty) && tilemap.charAt(tx, ty) !== 'P') {
    world.player.x = x;
    world.player.y = y;
  } else {
    placePlayerAtTile(world, world.mapDef.playerStart.tx, world.mapDef.playerStart.ty);
  }
  world.camera.snapTo(world.player.x, world.player.y - CAMERA_TARGET_Y_OFFSET);
}

/** Returns the portal definition the hero is standing on, or null. */
export function findPortalUnderPlayer(world) {
  const { player, tilemap, mapDef } = world;
  const tx = Math.floor(player.x / TILE_SIZE);
  const ty = Math.floor((player.y - FEET_SAMPLE_OFFSET) / TILE_SIZE);
  if (tilemap.charAt(tx, ty) !== 'P') return null;
  return mapDef.portals.find((p) => p.tx === tx && p.ty === ty) || null;
}
