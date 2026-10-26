// Per-frame simulation of the current map: hero, monsters, projectiles, pickups, portals.
import { updatePlayer } from './player-controller.js';
import { updateMonsterAi } from '../systems/monster-ai-system.js';
import { performMonsterAttack } from '../systems/combat-system.js';
import { resolveProjectileHits } from '../systems/projectile-hit-system.js';
import { applyPickup } from '../systems/loot-system.js';
import { addToInventory } from '../systems/equipment-system.js';
import { RARITIES } from '../data/item-definitions.js';
import { boxesOverlap } from '../systems/damage-formula.js';
import { findPortalUnderPlayer, CAMERA_TARGET_Y_OFFSET } from '../world/map-manager.js';
import { facingFromVector } from './player-controller.js';

function updateMonsters(world, dt) {
  const { player, tilemap } = world;
  for (const monster of world.monsters) {
    if (monster.dead) continue;
    monster.animTimer += dt;
    if (monster.hitFlashTimer > 0) monster.hitFlashTimer -= dt;
    for (const id of monster.hitBySwing) if (id < world.swingCounter - 3) monster.hitBySwing.delete(id);

    let dx = 0;
    let dy = 0;
    if (monster.knockback.timer > 0) {
      monster.knockback.timer -= dt;
      dx = (monster.knockback.vx * dt) / 1000;
      dy = (monster.knockback.vy * dt) / 1000;
    } else if (!player.dead) {
      const intent = updateMonsterAi(monster, player, dt);
      const speed = monster.def.speed * intent.speedFactor;
      dx = (intent.moveX * speed * dt) / 1000;
      dy = (intent.moveY * speed * dt) / 1000;
      if (intent.attack) performMonsterAttack(world, monster);
      if (intent.moveX !== 0 || intent.moveY !== 0) monster.dir = facingFromVector(intent.moveX, intent.moveY, monster.dir);
      else if (monster.ai.state === 'windup') monster.dir = facingFromVector(player.x - monster.x, player.y - monster.y, monster.dir);
    }
    if (dx !== 0 || dy !== 0) {
      const box = monster.box;
      const moved = tilemap.moveBox(box, dx, dy);
      monster.x = moved.x + box.w / 2;
      monster.y = moved.y + box.h;
      // Wandering into a wall picks a new direction next idle cycle
      if ((moved.blockedX || moved.blockedY) && monster.ai.state === 'wander') monster.ai.timer = 0;
    }
  }
}

function updatePickups(world) {
  const { player } = world;
  if (player.dead) return;
  const box = player.box;
  for (const drop of world.drops) {
    if (drop.dead || drop.age < 250 || !boxesOverlap(box, drop.box)) continue;
    if (drop.kind === 'equipment') {
      if (addToInventory(player, drop.item)) {
        drop.dead = true;
        player.hasUnseenItem = true;
        world.addText(player.x, player.y - 22, `+ ${drop.item.name}`, RARITIES[drop.item.rarity].color, { lifeMs: 900 });
      } else if (drop.pickupMsgCooldown <= 0) {
        drop.pickupMsgCooldown = 1000; // avoid spamming the message every frame while standing on it
        world.addText(player.x, player.y - 22, 'Inventory full', '#ff6b6b', { lifeMs: 700 });
      }
      continue;
    }
    drop.dead = true;
    const feedback = applyPickup(player, drop);
    world.addText(player.x, player.y - 22, feedback.text, feedback.color, { lifeMs: 800 });
  }
}

function updateLists(world, dt) {
  for (const p of world.projectiles) {
    p.update(dt);
    // Projectiles shatter on walls/trees instead of flying through them
    if (!p.dead && world.tilemap.isRectBlocked(p.x - 2, p.y - 2, 4, 4)) {
      p.dead = true;
      world.addSparks(p.x, p.y, 3);
    }
  }
  for (const d of world.drops) d.update(dt);
  for (const e of world.effects) e.update(dt);
  for (const s of world.particles) s.update(dt);
  for (const t of world.texts) t.update(dt);
  for (const sp of world.skillPulses) sp.update(dt);
}

/**
 * Advance the world one step. Returns gameplay events collected during the frame.
 * @returns {{type:string}[]} e.g. {type:'portal', portal}, {type:'playerDied'}, {type:'levelUp'}
 */
export function updateWorld(world, input, dt, banner) {
  world.events = [];
  world.elapsedMs += dt;

  // Hit-stop: freeze the simulation briefly for impact, but keep text/effects alive
  if (world.hitStopMs > 0) {
    world.hitStopMs -= dt;
    // Presses are cleared at end of frame, so buffer attack input instead of eating it
    if (input.wasPressed('attack')) world.player.combo.pressAttack();
    for (const t of world.texts) t.update(dt);
    for (const s of world.particles) s.update(dt);
    return world.events;
  }

  updatePlayer(world, input, dt, banner);
  for (const npc of world.npcs) npc.animTimer += dt; // drives the idle loop + marker bob (never reset)
  updateMonsters(world, dt);
  resolveProjectileHits(world);
  updateLists(world, dt);
  updatePickups(world);
  world.monsters.push(...world.spawner.update(dt, world.player));
  world.removeDead();

  const portal = findPortalUnderPlayer(world);
  if (portal && !world.player.dead) world.events.push({ type: 'portal', portal });

  world.camera.follow(world.player.x, world.player.y - CAMERA_TARGET_Y_OFFSET);
  world.camera.update(dt);
  return world.events;
}
