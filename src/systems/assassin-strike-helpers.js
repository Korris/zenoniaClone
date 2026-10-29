// Shared building blocks for the Assassin's strikes: nearest-target search, "is the hero behind this
// monster", blinking next to / behind a monster, and a single guaranteed (no MISS) dagger hit.
import { DIRECTION_VECTORS } from '../core/constants.js';
import { HERO_HITBOX } from '../data/hero-stats-config.js';
import { entityBox } from './damage-formula.js';
import { damageMonster } from './combat-system.js';
import { rollPlayerDamage, CRIT_COLOR } from './player-damage.js';

export const SHADOW_HIT_COLOR = '#e3c4ff';

/** Living monsters within `range` px of the hero, nearest first. */
export function monstersInRange(world, range) {
  const { player } = world;
  return world.monsters
    .filter((m) => !m.dead && Math.hypot(m.x - player.x, m.y - player.y) <= range)
    .sort((a, b) => Math.hypot(a.x - player.x, a.y - player.y) - Math.hypot(b.x - player.x, b.y - player.y));
}

/** True when the hero stands on the far side of the monster's facing (i.e. behind its back). */
export function isBehind(monster, player) {
  const v = DIRECTION_VECTORS[monster.dir] || DIRECTION_VECTORS.down;
  return (player.x - monster.x) * v.x + (player.y - monster.y) * v.y < 0;
}

function facingToward(dx, dy, fallback) {
  if (dx === 0 && dy === 0) return fallback;
  return Math.abs(dx) > Math.abs(dy) ? (dx > 0 ? 'right' : 'left') : (dy > 0 ? 'down' : 'up');
}

/** Purple smoke puff used for vanish / appear. */
export function shadowPuff(world, x, y, scale = 1.6) {
  world.addEffect({ x, y: y - 8, spriteKey: 'circular', lifeMs: 380, scale, tint: '#8a5cd0', spin: 4 });
}

/**
 * Teleports the hero `offset` px behind the monster (opposite its facing), falling back to its sides
 * and then its front when walls block the spot, and turns the hero to face it.
 * @returns {boolean} false when every spot is blocked (hero stays where it is)
 */
export function blinkBehind(world, monster, offset = 12) {
  const { player, tilemap } = world;
  const v = DIRECTION_VECTORS[monster.dir] || DIRECTION_VECTORS.down;
  const spots = [[-v.x, -v.y], [-v.y, v.x], [v.y, -v.x], [v.x, v.y]];
  for (const [sx, sy] of spots) {
    const x = monster.x + sx * offset, y = monster.y + sy * offset;
    const box = entityBox({ x, y }, HERO_HITBOX);
    if (tilemap?.isRectBlocked(box.x, box.y, box.w, box.h)) continue;
    shadowPuff(world, player.x, player.y);
    player.x = x;
    player.y = y;
    player.dir = facingToward(monster.x - x, monster.y - y, player.dir);
    shadowPuff(world, x, y);
    return true;
  }
  return false;
}

/**
 * One dagger hit on `monster` (never MISSes, like other skills). Damage x `behindMult` when the hero is behind it;
 * `critBonus` adds crit chance for this hit. Returns {behind, crit}.
 */
export function strikeMonster(world, monster, mult, { knockback = 40, critBonus = 0, behindMult = 1 } = {}) {
  const { player } = world;
  const behind = isBehind(monster, player);
  const { damage, crit } = rollPlayerDamage(player, monster.def.def, mult * (behind ? behindMult : 1), Math.random, critBonus);
  const dx = monster.x - player.x, dy = monster.y - player.y, dist = Math.hypot(dx, dy) || 1;
  damageMonster(world, monster, damage, (dx / dist) * knockback, (dy / dist) * knockback, crit ? CRIT_COLOR : SHADOW_HIT_COLOR, crit);
  world.addEffect({ x: monster.x, y: monster.y - 7, spriteKey: 'slash', rotation: Math.atan2(dy, dx), lifeMs: 200, scale: 1.1, tint: '#b57cff' });
  return { behind, crit };
}
