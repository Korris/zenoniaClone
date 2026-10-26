// Projectiles on impact (both owners) plus the ranged combo shots (wizard bolts, mechanic bullets). Split out of
// combat-system.js: hero skill projectiles, monster throws and wizard bolts all resolve here, and a
// projectile with `explodeRadius` (wizard combo finisher, fire skills) bursts on its first hit,
// damaging every monster within that radius once.
import { DIRECTION_VECTORS } from '../core/constants.js';
import { computeDamage, boxesOverlap } from './damage-formula.js';
import { Projectile } from '../entities/projectile.js';
import { damageMonster, hitPlayer } from './combat-system.js';
import { heroAttack, CRIT_COLOR } from './player-damage.js';

const MAGIC_HIT_COLOR = '#9fe0ff';

/** Ranged combo step `step` becomes active: launch that step's bolt/bullet in the facing direction. */
export function fireComboBolt(world, step) {
  const { player } = world;
  const hit = player.combo.currentHit;
  const { speed, range, spriteKey, scale, w, h, explodeRadius = 0, explodeFx, flashFx = 'hitSpark' } = hit.bolt;
  const v = DIRECTION_VECTORS[player.dir];
  // Same bonus stack as the melee combo: per-hit mult (+ finisher scaling) x combo-damage passives.
  const mult = player.combo.multiplierFor(step, player.level) * (1 + (player.passiveMods?.comboMult || 0));
  world.projectiles.push(new Projectile({
    x: player.x + v.x * 12, y: player.y - 4 + v.y * 12, vx: v.x * speed, vy: v.y * speed,
    owner: 'player', damage: heroAttack(player) * mult, spriteKey, scale,
    lifeMs: (range / speed) * 1000, w, h, explodeRadius, explodeFx, knockback: hit.knockback,
  }));
  // Small flash at the staff tip / gun barrel so the shot reads even before the projectile travels.
  world.addEffect({ x: player.x + v.x * 10, y: player.y - 10 + v.y * 6, spriteKey: flashFx, lifeMs: 140, scale: explodeRadius ? 1.6 : 1 });
}

/** One crit-rolled hit of a player projectile on a monster. */
function strikeMonster(world, p, monster, knockX, knockY) {
  const crit = Math.random() < (world.player.passiveMods?.critChance || 0);
  const damage = computeDamage(p.damage, monster.def.def, crit ? 1.6 : 1);
  damageMonster(world, monster, damage, knockX, knockY, crit ? CRIT_COLOR : MAGIC_HIT_COLOR);
}

/** Burst at the projectile's position: every live monster within the radius is hit once. */
function explode(world, p) {
  const r = p.explodeRadius;
  for (const monster of world.monsters) {
    if (monster.dead) continue;
    const dx = monster.x - p.x;
    const dy = monster.y - 6 - p.y;
    const dist = Math.hypot(dx, dy);
    if (dist > r) continue;
    const k = p.knockback / Math.max(dist, 1);
    strikeMonster(world, p, monster, dx * k, dy * k);
  }
  // Burst sprites are 40 world units across (build-wizard-fx.py): scale so the art spans the blast.
  world.addEffect({ x: p.x, y: p.y - 4, spriteKey: p.explodeFx, lifeMs: 320, scale: (2 * r) / 40, rotation: Math.random() * Math.PI * 2 });
  world.camera.shake(2, 120);
}

/** Projectile vs. target collisions for both owners. */
export function resolveProjectileHits(world) {
  const { player } = world;
  for (const p of world.projectiles) {
    if (p.dead) continue;
    if (p.owner === 'monster') {
      if (boxesOverlap(p.box, player.box)) {
        hitPlayer(world, computeDamage(p.damage, player.def, 1), p.x, p.y);
        p.dead = true;
      }
      continue;
    }
    for (const monster of world.monsters) {
      if (monster.dead || p.hitIds.has(monster.id) || !boxesOverlap(p.box, monster.box)) continue;
      if (p.explodeRadius) {
        explode(world, p);
        p.dead = true;
        break;
      }
      p.hitIds.add(monster.id);
      // p.damage was computed at cast time from heroAttack(); the crit roll happens on impact.
      strikeMonster(world, p, monster, p.vx * 0.4, p.vy * 0.4);
      if (!p.pierce) {
        p.dead = true;
        break;
      }
    }
  }
}
