// Resolves damage between hero and monsters: combo swings, projectiles, monster attacks, deaths.
import { DIRECTION_VECTORS } from '../core/constants.js';
import { computeDamage, boxesOverlap, circleOverlapsBox } from './damage-formula.js';
import { spawnDrops } from './loot-system.js';
import { rollEquipmentDrop } from './equipment-system.js';
import { Projectile } from '../entities/projectile.js';
import { ItemDrop } from '../entities/item-drop.js';
import { rollPlayerDamage, CRIT_COLOR } from './player-damage.js';
import { playImpactSound } from '../audio/combat-audio.js';

const DIR_ANGLE = { right: 0, down: Math.PI / 2, left: Math.PI, up: -Math.PI / 2 };

/** Rectangle in front of the hero for melee hits. */
export function attackBoxFor(player, reach, width) {
  const v = DIRECTION_VECTORS[player.dir];
  const cx = player.x + v.x * (reach / 2 + 4);
  const cy = player.y - 6 + v.y * (reach / 2 + 4);
  const w = v.x !== 0 ? reach : width;
  const h = v.y !== 0 ? reach : width;
  return { x: cx - w / 2, y: cy - h / 2, w, h };
}

/** Apply damage to a monster, spawn feedback, and handle death rewards. */
export function damageMonster(world, monster, damage, knockX = 0, knockY = 0, color = '#ffffff', critical = false) {
  if (!monster.dead) playImpactSound();
  if (!monster.dead) world.combatTarget = monster;
  const impulse = critical ? 2 : 1;
  const died = monster.takeDamage(damage, knockX * impulse, knockY * impulse);
  world.addText(monster.x, monster.y - monster.def.hitbox.h - 10, damage, color, {
    damageStyle: 'outgoing', critical, lifeMs: 850, vy: -34,
  });
  world.addSparks(monster.x, monster.y - 6, 4);
  world.addEffect({ x: monster.x, y: monster.y - 8, spriteKey: 'hitSpark', lifeMs: 220, scale: 1.2, rotation: Math.random() * Math.PI });
  if (died) onMonsterDeath(world, monster);
  return died;
}

function onMonsterDeath(world, monster) {
  const { player } = world;
  const levels = player.gainExp(monster.def.exp);
  world.drops.push(...spawnDrops(monster.def, monster.x, monster.y));
  const equipDrop = rollEquipmentDrop(monster.def, Math.random, player.classId);
  if (equipDrop) world.drops.push(new ItemDrop(monster.x, monster.y, 'equipment', equipDrop));
  world.addSparks(monster.x, monster.y - 6, 10);
  if (world.spawner) world.spawner.scheduleRespawn(monster);
  world.events.push({ type: 'monsterKilled', monsterType: monster.type });
  if (levels > 0) {
    world.addText(player.x, player.y - 30, 'LEVEL UP!', '#8fff8f', { lifeMs: 1400, big: true, vy: -18 });
    world.events.push({ type: 'levelUp', levels });
  }
  if (monster.isBoss) {
    world.bossDefeated = true;
    world.events.push({ type: 'bossDefeated' });
  }
}

/** Called when a combo swing becomes active: hit every monster in the arc once. */
export function performComboHit(world, step) {
  const { player } = world;
  const hit = player.combo.currentHit;
  const box = attackBoxFor(player, hit.reach, hit.width);
  const swingId = world.nextSwingId();
  // Power Strike passive adds a flat combo-damage bonus on top of the per-hit multiplier.
  const mult = player.combo.multiplierFor(step, player.level) * (1 + (player.passiveMods?.comboMult || 0));
  const v = DIRECTION_VECTORS[player.dir];
  const isFinisher = step === player.combo.config.hits.length;

  // Slash arc in front of the hero; alternate hits mirror the arc so the combo reads left-right-overhead
  world.addEffect({
    x: player.x + v.x * 14, y: player.y - 7 + v.y * 14, spriteKey: isFinisher ? 'arc' : 'slash',
    rotation: DIR_ANGLE[player.dir] + (step === 2 ? Math.PI : 0), lifeMs: isFinisher ? 240 : 170,
    scale: isFinisher ? 1.1 : 0.8,
  });
  if (isFinisher) world.addEffect({ x: player.x, y: player.y - 8, spriteKey: 'arc', rotation: DIR_ANGLE[player.dir] + Math.PI, lifeMs: 260, scale: 0.8, spin: 9, follow: player, offsetY: -8 });

  let landed = 0;
  for (const monster of world.monsters) {
    if (monster.dead || monster.hitBySwing.has(swingId) || !boxesOverlap(box, monster.box)) continue;
    monster.hitBySwing.add(swingId);
    const { damage, crit } = rollPlayerDamage(player, monster.def.def, mult);
    damageMonster(world, monster, damage, v.x * hit.knockback, v.y * hit.knockback, crit ? CRIT_COLOR : isFinisher ? '#ffb13b' : '#ffffff', crit);
    landed++;
  }
  if (landed > 0 && isFinisher) {
    world.camera.shake(2.5, 140);
    world.hitStopMs = 60;
  } else if (landed > 0) {
    world.hitStopMs = 25;
  }
}

/** Area hit around the hero (Whirlwind, Blade Storm). */
export function performRadialHit(world, radius, mult, knockback, center = null) {
  const { player } = world;
  const x = center?.x ?? player.x, y = center?.y ?? player.y - 6;
  for (const monster of world.monsters) {
    if (monster.dead || !circleOverlapsBox(x, y, radius, monster.box)) continue;
    const dx = monster.x - (center?.x ?? player.x);
    const dy = monster.y - (center?.y ?? player.y);
    const dist = Math.hypot(dx, dy) || 1;
    const { damage, crit } = rollPlayerDamage(player, monster.def.def, mult);
    damageMonster(world, monster, damage, (dx / dist) * knockback, (dy / dist) * knockback, crit ? CRIT_COLOR : '#bfe9ff', crit);
  }
}

/** Single hit in a box in front of the hero (Heavy Blow). */
export function performSkillStrike(world, reach, width, mult, knockback) {
  const { player } = world;
  const box = attackBoxFor(player, reach, width);
  const v = DIRECTION_VECTORS[player.dir];
  for (const monster of world.monsters) {
    if (monster.dead || !boxesOverlap(box, monster.box)) continue;
    const { damage, crit } = rollPlayerDamage(player, monster.def.def, mult);
    damageMonster(world, monster, damage, v.x * knockback, v.y * knockback, crit ? CRIT_COLOR : '#ffb13b', crit);
  }
}

/** Monster strikes: melee box check, ranged projectile, or boss combo. */
export function performMonsterAttack(world, monster) {
  const { player } = world;
  const def = monster.def;
  if (def.behavior === 'ranged' || (def.behavior === 'boss' && monster.attackCount % 3 === 2)) {
    fireMonsterProjectiles(world, monster, def.behavior === 'boss' ? 8 : 1);
  } else {
    const dist = Math.hypot(player.x - monster.x, player.y - monster.y);
    if (dist <= def.attackRange + 10) hitPlayer(world, computeDamage(def.atk, player.def, 1), monster.x, monster.y);
    if (def.behavior === 'boss') world.camera.shake(3, 200);
  }
  monster.attackCount = (monster.attackCount || 0) + 1;
}

function fireMonsterProjectiles(world, monster, count) {
  const { player } = world;
  const baseAngle = Math.atan2(player.y - monster.y, player.x - monster.x);
  for (let i = 0; i < count; i++) {
    const angle = count === 1 ? baseAngle : (i / count) * Math.PI * 2;
    world.projectiles.push(new Projectile({
      x: monster.x, y: monster.y - 6, vx: Math.cos(angle) * monster.def.projectileSpeed,
      vy: Math.sin(angle) * monster.def.projectileSpeed, owner: 'monster', damage: monster.def.atk,
      spriteKey: 'spark', lifeMs: 2000, w: 6, h: 6,
    }));
  }
}

export function hitPlayer(world, damage, sourceX, sourceY) {
  const { player } = world;
  const dealt = player.takeDamage(damage);
  if (dealt <= 0) return;
  // HP regenerates in fractions, so round for display
  world.addText(player.x, player.y - 24, `-${Math.ceil(dealt)}`, '#60bdff', {
    damageStyle: 'incoming', lifeMs: 850, vy: -34,
  });
  world.camera.shake(2, 120);
  const dx = player.x - sourceX;
  const dy = player.y - sourceY;
  const dist = Math.hypot(dx, dy) || 1;
  player.knockback = { vx: (dx / dist) * 90, vy: (dy / dist) * 90, timer: 100 };
  if (player.dead) world.events.push({ type: 'playerDied' });
}
